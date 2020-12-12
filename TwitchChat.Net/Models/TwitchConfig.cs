// <copyright file="TwitchConfig.cs" company="David Francis">
// Copyright (c) David Francis. All rights reserved.
// </copyright>

namespace TwitchChatNet.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// The Twitch Bot/User Connection info.
    /// </summary>
    public class TwitchConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwitchConfig"/> class.
        /// </summary>
        /// <param name="loginName">The login name of the Twitch Account.</param>
        /// <param name="oauthToken">The OAuth token for the Twitch Account.</param>
        /// <param name="initialChannels">Optional, the initial set of channel names to connect to.</param>
        public TwitchConfig(string? loginName, string? oauthToken, IList<string>? initialChannels)
        {
            loginName.ThrowIfNullOrEmpty(nameof(loginName));
            oauthToken.ThrowIfNullOrEmpty(nameof(oauthToken));

            this.LoginName = loginName!;
            this.OAuthToken = oauthToken!;
            this.InitialChannels = initialChannels ?? new List<string>();
        }

        /// <summary>
        /// Gets the twitch account login name.
        /// </summary>
        public string LoginName { get; init; }

        /// <summary>
        /// Gets the twitch account oauth token.
        /// </summary>
        public string OAuthToken { get; init; }

        /// <summary>
        /// Gets the initial list of twitch channels to connect to.
        /// </summary>
        public IList<string> InitialChannels { get; init; }
    }
}
