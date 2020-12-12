// <copyright file="TwitchReceivedMessage.cs" company="David Francis">
// Copyright (c) David Francis. All rights reserved.
// </copyright>

namespace TwitchChatNet.Models
{
    /// <summary>
    /// An incoming message received from Twitch.
    /// </summary>
    public class TwitchReceivedMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwitchReceivedMessage"/> class.
        /// </summary>
        /// <param name="channel">The channel name.</param>
        /// <param name="sender">The sender name.</param>
        /// <param name="message">The message.</param>
        internal TwitchReceivedMessage(string? channel = null, string? sender = null, string? message = null)
        {
            channel.ThrowIfNullOrEmpty(nameof(channel));
            sender.ThrowIfNullOrEmpty(nameof(sender));
            message.ThrowIfNullOrEmpty(nameof(message));

            this.Channel = channel!;
            this.Sender = sender!;
            this.Message = message!;
        }

        /// <summary>
        /// Gets the Channel Name.
        /// </summary>
        public string Channel { get; init; }

        /// <summary>
        /// Gets the Sender Name.
        /// </summary>
        public string Sender { get; init; }

        /// <summary>
        /// Gets the Message.
        /// </summary>
        public string Message { get; init; }
    }
}
