// <copyright file="ValidatorExtensions.cs" company="David Francis">
// Copyright (c) David Francis. All rights reserved.
// </copyright>

namespace TwitchChatNet
{
    using System;

    /// <summary>
    /// Extensions for validating inputs.
    /// </summary>
    internal static class ValidatorExtensions
    {
        /// <summary>
        /// Throws if the input object is null.
        /// </summary>
        /// <param name="source">The source object to check.</param>
        /// <param name="name">The name of the parameter.</param>
        public static void ThrowIfNull(this object? source, string name)
        {
            if (source == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Throws if the input object is null or empty.
        /// </summary>
        /// <param name="source">The source object to check.</param>
        /// <param name="name">The name of the parameter.</param>
        public static void ThrowIfNullOrEmpty(this string? source, string name)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
