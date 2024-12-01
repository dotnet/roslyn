// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface ILspServiceFactory
{
    /// <summary>
    /// Some LSP services need to know the client capabilities on construction or
    /// need to know about other <see cref="ILspService"/> instances to be constructed.
    /// </summary>
    ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind);
}
