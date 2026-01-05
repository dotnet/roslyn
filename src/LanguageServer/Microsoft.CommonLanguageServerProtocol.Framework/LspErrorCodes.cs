// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CommonLanguageServerProtocol.Framework;

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

    /// <summary>
    /// This is the end range of LSP reserved error codes.
	/// It doesn't denote a real error code.
    /// </summary>
    public const int LspReservedErrorRangeEnd = -32800;

}

/// <summary>
/// Error codes used by the Roslyn LSP, but not standardized in general LSP.
/// </summary>
internal static class RoslynLspErrorCodes
{
    /// <summary>
    /// Signals that the server could not process the request, but that the failure shouldn't be surfaced to the user.
    /// (It's expected that the failure is still logged, however.)
    /// </summary>
    /// <remarks>
    /// This is only meant to be used under conditions where we can't fulfill the request, but we think that the failure
    /// is unlikely to be significant to the user (i.e. surface as an actual editor feature failing to function properly.)
    /// For example, if pull diagnostics are requested for a virtual document that was already closed.
    /// </remarks>
    public const int NonFatalRequestFailure = -32799;
}
