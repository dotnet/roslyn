// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#errorCodes
/// </summary>
internal static class LspErrorCodes
{
    /// <summary>
    /// Signals that the server detected the contents of the document were modified
    /// outside of normal conditions.
    /// </summary>
    public const int ContentModified = -32801;
}
