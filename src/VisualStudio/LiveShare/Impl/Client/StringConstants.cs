// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client;

internal sealed class StringConstants
{
    public const string BaseRemoteAssemblyTitle = "Base Remote Language Service";

    // The service name for an LSP server implemented using Roslyn designed to be used with the Roslyn client
    public const string RoslynContractName = "Roslyn";
    // The service name for an LSP server implemented using Roslyn designed to be used with the LSP SDK client
    public const string RoslynLspSdkContractName = "RoslynLSPSDK";

    // LSP server provider names.
    public const string RoslynProviderName = "Roslyn";
    public const string CSharpProviderName = "RoslynCSharp";
    public const string VisualBasicProviderName = "RoslynVisualBasic";
    public const string TypeScriptProviderName = "RoslynTypeScript";
    public const string AnyProviderName = "any";

    public const string TypeScriptLanguageName = "TypeScript";
}
