// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Completion item tags are extra annotations that tweak the rendering of a completion item.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionItemTag">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal enum CompletionItemTag
    {
        /// <summary>
        /// Render a completion as obsolete, usually using a strike-out.
        /// </summary>
        Deprecated = 1,
    }
}
