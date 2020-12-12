// <copyright file="IrcMessageParser.cs" company="David Francis">
// Copyright (c) David Francis. All rights reserved.
// </copyright>

namespace TwitchChatNet
{
    using System;
    using System.Text.RegularExpressions;
    using TwitchChatNet.Models;

    /// <summary>
    /// Utility for parsing IRC-style messages into a more readable format.
    /// </summary>
    internal class IrcMessageParser
    {
        private const string PrivateMessageCommand = "PRIVMSG";
        private const string PrivateMessagePattern = @"#(\w+?) :(.+)";
        private const string ReplyCommandPattern = @":(.+?) (\d{3}) (.+?) :(.+)"; // :{prefix} {commandCode} {username} :{payload}
        private const string MessagePattern = @":(\w+)!(.+?) ([A-Z]+) (.+)"; // :{senderNickname}!{otherstuff} {COMMAND} {payload}

        private static readonly Regex PrivateMessageRegex = new Regex(PrivateMessagePattern, RegexOptions.Compiled);
        private static readonly Regex ReplyCommandRegex = new Regex(ReplyCommandPattern, RegexOptions.Compiled);
        private static readonly Regex MessageRegex = new Regex(MessagePattern, RegexOptions.Compiled);

        private enum IrcResponseCommand
        {
            RPL_WELCOME = 001, // Welcome message
            RPL_YOURHOST = 002, // "Your host is {servername}
            RPL_CREATED = 003,
            RPL_MYINFO = 004, // -
            RPL_NAMREPLY = 353, // {botname}.{servername} 353 {botname} = #{channel} :{botname}
            RPL_ENDOFNAMES = 366, // {botname}.{servername} 366 {botname} #{channel} :End of /NAMES list
            RPL_MOTD = 372, // Message of the Day - :
            RPL_MOTDSTART = 375, // Beginning of MOTD
            RPL_ENDOFMOTD = 376, // End of MOTD
        }

        /// <summary>
        /// Parses the IRC-Style message into something useful.
        /// </summary>
        /// <param name="message">The IRC style message.</param>
        /// <returns>The parsed message.</returns>
        public static TwitchReceivedMessage? ParseIrcMessage(string message)
        {
            if (ReplyCommandRegex.IsMatch(message))
            {
                // Control Reply to a Command we issued. Usually received after NICK, JOIN, or PART
                var matches = ReplyCommandRegex.Match(message);

                if (matches.Groups.Count != 5)
                {
                    // Something has gone wrong.
                    throw new InvalidOperationException($"Expected 5 elements, found {matches.Groups.Count}");
                }

                var prefix = matches.Groups[1].Value;
                var commandCodeString = matches.Groups[2].Value;
                var commandCodeInt = Convert.ToInt32(commandCodeString);
                var commandCode = (IrcResponseCommand)commandCodeInt;
                var username = matches.Groups[3].Value;
                var payload = matches.Groups[4].Value;

                return ReplyCommandCodeHandler(prefix, commandCode, username, payload);
            }
            else if (MessageRegex.IsMatch(message))
            {
                // Incoming Message from IRC.
                var matches = MessageRegex.Match(message);

                if (matches.Groups.Count != 5)
                {
                    // Something has gone wrong.
                    throw new InvalidOperationException($"Expected 5 elements, found {matches.Groups.Count}");
                }

                var senderNickname = matches.Groups[1].Value;
                var command = matches.Groups[3].Value;
                var payload = matches.Groups[4].Value;

                return MessageHandler(senderNickname, command, payload);
            }

            return null;
        }

        private static TwitchReceivedMessage? ReplyCommandCodeHandler(string prefix, IrcResponseCommand command, string username, string payload)
        {
            // Don't currently care about these.
            return null;
        }

        private static TwitchReceivedMessage? MessageHandler(string senderNickname, string command, string payload)
        {
            // Only handle PRIVMSG right now.
            if (command == PrivateMessageCommand)
            {
                return PrivateMessageHandler(senderNickname, payload);
            }

            return null;
        }

        private static TwitchReceivedMessage? PrivateMessageHandler(string senderNickname, string payload)
        {
            var match = PrivateMessageRegex.Match(payload);

            if (!match.Success)
            {
                // Malformed input
                throw new InvalidOperationException($"Invalid Private Message: {payload}");
            }
            else if (match.Groups.Count != 3)
            {
                throw new InvalidOperationException($"Expected 3 elements, found {match.Groups.Count}");
            }

            return new TwitchReceivedMessage(
                channel: match.Groups[1].Value,
                message: match.Groups[2].Value,
                sender: senderNickname);
        }
    }
}
