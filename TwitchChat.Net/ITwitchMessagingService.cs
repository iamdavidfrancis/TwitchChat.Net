// <copyright file="ITwitchMessagingService.cs" company="David Francis">
// Copyright (c) David Francis. All rights reserved.
// </copyright>

namespace TwitchChatNet
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using static TwitchChatNet.TwitchMessagingService;

    /// <summary>
    /// Twitch Messaging Service.
    /// </summary>
    public interface ITwitchMessagingService
    {
        /// <summary>
        /// Event for when a message is received.
        /// </summary>
        event MessageReceivedHandler? MessageReceived;

        /// <summary>
        /// Connect to twitch.
        /// </summary>
        /// <remarks>
        /// The service cannot be restarted. To restart the service a new instance should be created using the same options.
        /// </remarks>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when attempting to connect before setting the <see cref="MessageReceived"/> handler.</exception>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop the receive loop and disconnect.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task StopAsync();

        /// <summary>
        /// Leave a channel.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task LeaveChannel(string channel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Join a channel.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task JoinChannel(string channel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a message to a channel.
        /// </summary>
        /// <param name="channel">The channel name.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task SendMessage(string channel, string message, CancellationToken cancellationToken = default);
    }
}
