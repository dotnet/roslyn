// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal interface IVSTypeScriptCapabilitiesProvider
{
    /// <summary>
    /// Returns the <see cref="ClientCapabilities"/> provided by typescript.
    /// This is specified as a string to allow us and TS to depend on different versions of the
    /// LSP protocol definitions.
    /// </summary>
    string GetServerCapabilities(string clientCapabilities);
}
