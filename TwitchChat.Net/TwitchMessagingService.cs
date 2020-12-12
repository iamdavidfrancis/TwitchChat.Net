// <copyright file="TwitchMessagingService.cs" company="David Francis">
// Copyright (c) David Francis. All rights reserved.
// </copyright>

namespace TwitchChatNet
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Connections;
    using Microsoft.AspNetCore.Http.Connections;
    using Microsoft.AspNetCore.Http.Connections.Client;
    using Microsoft.Extensions.Logging;
    using TwitchChatNet.Models;

    /// <summary>
    /// Twitch Messaging Service.
    /// </summary>
    public class TwitchMessagingService : ITwitchMessagingService, IAsyncDisposable
    {
        private const string PingValue = "PING :tmi.twitch.tv";
        private const string PongValue = "PONG :tmi.twitch.tv";

        private static readonly Uri TwitchWebsocketUrl = new Uri("wss://irc-ws.chat.twitch.tv:443");

        private readonly TwitchConfig twitchConfig;
        private readonly HttpConnection httpConnection;
        private readonly ILogger logger;

        private bool isConnected = false;
        private Task? receiveTask;

        private object? stopLock;
        private TaskCompletionSource? stopTcs = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwitchMessagingService"/> class.
        /// </summary>
        /// <param name="twitchConfig">The config for the Twitch Account to use.</param>
        public TwitchMessagingService(TwitchConfig twitchConfig)
            : this(twitchConfig, loggerFactory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwitchMessagingService"/> class.
        /// </summary>
        /// <param name="twitchConfig">The config for the Twitch Account to use.</param>
        /// <param name="loggerFactory">Optional Logger Factory.</param>
        public TwitchMessagingService(TwitchConfig twitchConfig, ILoggerFactory? loggerFactory)
        {
            twitchConfig.ThrowIfNull(nameof(twitchConfig));

            this.twitchConfig = twitchConfig;
            this.logger = loggerFactory.CreateLogger<TwitchMessagingService>();
            this.httpConnection = new HttpConnection(
                new HttpConnectionOptions
                {
                    Url = TwitchWebsocketUrl,
                    DefaultTransferFormat = TransferFormat.Text,
                    SkipNegotiation = true,
                    Transports = HttpTransportType.WebSockets,
                }, loggerFactory);
        }

        /// <summary>
        /// Handler for incoming Twitch messages.
        /// </summary>
        /// <param name="receivedMessage">The message that was received.</param>
        public delegate void MessageReceivedHandler(TwitchReceivedMessage receivedMessage);

        /// <inheritdoc/>
        public event MessageReceivedHandler? MessageReceived;

        private enum IrcCommand
        {
            JoinChannel,
            LeaveChannel,
            SendMessage,
        }

        /// <inheritdoc/>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (this.isConnected)
            {
                throw new InvalidOperationException("Cannot connect to twitch multiple times. If restarting a connection, a new instance of this class must be created.");
            }

            if (this.MessageReceived == null)
            {
                // Can't start until a MessageReceived handler is registered.
                throw new InvalidOperationException($"{nameof(this.MessageReceived)} was not set. Cannot connect to Twitch without a receive handler.");
            }

            await this.httpConnection.StartAsync(TransferFormat.Text, cancellationToken);

            this.isConnected = true;

            // Send initial connect messages.
            await this.SendAsync($"PASS oauth:{this.twitchConfig.OAuthToken}");
            await this.SendAsync($"NICK {this.twitchConfig.LoginName}");

            // Start Reading
            this.receiveTask = this.ReceiveLoop(cancellationToken);

            // Join channels
            foreach (var channel in this.twitchConfig.InitialChannels)
            {
                await this.JoinChannel(channel);
            }
        }

        /// <inheritdoc/>
        public Task StopAsync()
        {
            lock (this.stopLock!)
            {
                if (this.stopTcs != null)
                {
                    return this.stopTcs.Task;
                }
                else
                {
                    this.stopTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    return this.StopAsyncCore();
                }
            }
        }

        /// <inheritdoc/>
        public async Task LeaveChannel(string channel, CancellationToken cancellationToken = default)
        {
            var ircMessage = GetSendMessage(IrcCommand.LeaveChannel, channel);
            await this.SendAsync(ircMessage, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task JoinChannel(string channel, CancellationToken cancellationToken = default)
        {
            var ircMessage = GetSendMessage(IrcCommand.JoinChannel, channel);
            await this.SendAsync(ircMessage, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SendMessage(string channel, string message, CancellationToken cancellationToken = default)
        {
            var ircMessage = GetSendMessage(IrcCommand.SendMessage, channel, message);
            await this.SendAsync(ircMessage, cancellationToken);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await this.StopAsync();

            if (this.httpConnection != null)
            {
                await this.httpConnection.DisposeAsync();
            }
        }

        private static string GetSendMessage(IrcCommand command, string channel, string data = "") => command switch
        {
            IrcCommand.JoinChannel => $"JOIN #{channel}",
            IrcCommand.LeaveChannel => $"PART #{channel}",
            IrcCommand.SendMessage => $"PRIVMSG #{channel} :{data}",
            _ => throw new NotImplementedException(),
        };

        private async Task StopAsyncCore()
        {
            this.httpConnection.Transport.Input.CancelPendingRead();

            await this.receiveTask!;

            this.stopTcs!.TrySetResult();
        }

        private async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await this.httpConnection.Transport.Output.WriteAsync(buffer, cancellationToken);
        }

        private async Task ReceiveLoop(CancellationToken cancellationToken = default)
        {
            var input = this.httpConnection.Transport.Input;

            while (true)
            {
                var result = await input.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                try
                {
                    if (result.IsCanceled)
                    {
                        break;
                    }
                    else if (!buffer.IsEmpty)
                    {
                        var messages = Encoding.UTF8.GetString(buffer);

                        this.logger.LogInformation(messages);

                        await this.ProcessMessages(messages);
                    }

                    if (result.IsCompleted)
                    {
                        if (!buffer.IsEmpty)
                        {
                            this.logger.LogError("Connection to twitch terminated while reading a message.");
                        }

                        break;
                    }
                }
                finally
                {
                    input.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }

        private async Task ProcessMessages(string messageBatch)
        {
            var messages = messageBatch.Replace("\r\n", "\n").Split("\n").Where(m => !string.IsNullOrEmpty(m));

            foreach (var message in messages)
            {
                // We need to keep connected, so hard code the PING-PONG response here.
                if (message.Equals(PingValue, StringComparison.OrdinalIgnoreCase))
                {
                    await this.SendAsync(PongValue);
                    continue;
                }

                try
                {
                    var parsedMessage = IrcMessageParser.ParseIrcMessage(message);

                    if (parsedMessage == null)
                    {
                        continue;
                    }

                    this.MessageReceived!(parsedMessage);
                }
                catch (Exception ex)
                {
                    // Don't throw as that would stop the message processor.
                    this.logger.LogError(ex.ToString());
                }
            }
        }
    }
}
