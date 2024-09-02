// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Class representing the parameters sent for a textDocument/linkedEditingRange request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#linkedEditingRangeParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class LinkedEditingRangeParams : TextDocumentPositionParams
    {
    }
}
