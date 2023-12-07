// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Collections.Generic;

    /// <summary>
    /// Known VS response kind modifiers.
    /// </summary>
    internal static class VSInternalKnownKindModifiers
    {
        /// <summary>
        /// Response kind modifier string for 'public'.
        /// </summary>
        public const string Public = "public";

        /// <summary>
        /// Response kind modifier string for 'private'.
        /// </summary>
        public const string Private = "private";

        /// <summary>
        /// Response kind modifier string for 'protected'.
        /// </summary>
        public const string Protected = "protected";

        /// <summary>
        /// Response kind modifier string for 'internal'.
        /// </summary>
        public const string Internal = "internal";

        /// <summary>
        /// Response kind modifier string for 'sealed'.
        /// </summary>
        public const string Sealed = "sealed";

        /// <summary>
        /// Response kind modifier string for 'shortcut'.
        /// </summary>
        public const string Shortcut = "shortcut";

        /// <summary>
        /// Response kind modifier string for 'snippet'.
        /// </summary>
        public const string Snippet = "snippet";

        /// <summary>
        /// Response kind modifier string for 'friend'.
        /// </summary>
        public const string Friend = "friend";

        /// <summary>
        /// Response kind modifier string for 'declaration'.
        /// </summary>
        public const string Declaration = "declaration";

        /// <summary>
        /// Collection of known response kind modifier strings.
        /// </summary>
        public static readonly IReadOnlyCollection<string> AllModifiers = new[]
        {
            Public,
            Private,
            Protected,
            Internal,
            Sealed,
            Shortcut,
            Snippet,
            Friend,
            Declaration,
        };
    }
}
