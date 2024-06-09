// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Class which contains the string values for all common language protocol methods.
    /// </summary>
    internal static partial class Methods
    {
        // NOTE: these are sorted/grouped in the order used by the spec

        /// <summary>
        /// Method name for '$/progress' notifications.
        /// </summary>
        public const string ProgressNotificationName = "$/progress";

        /// <summary>
        /// Name of the progress token in the request.
        /// </summary>
        public const string PartialResultTokenName = "partialResultToken";

        /// <summary>
        /// Name of the work done token in the request.
        /// </summary>
        public const string WorkDoneTokenName = "workDoneToken";

        /// <summary>
        /// Name of the progress token in the $/progress notification.
        /// </summary>
        public const string ProgressNotificationTokenName = "token";
    }
}
