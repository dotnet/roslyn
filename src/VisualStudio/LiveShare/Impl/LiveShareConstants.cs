// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.LiveShare;

internal sealed class LiveShareConstants
{
    // The service name for an LSP server implemented using Roslyn designed to be used with the Roslyn client
    public const string RoslynContractName = "Roslyn";
    // The service name for an LSP server implemented using Roslyn designed to be used with the LSP SDK client
    public const string RoslynLSPSDKContractName = "RoslynLSPSDK";
    public const string TypeScriptLanguageName = "TypeScript";

    public const string CSharpContractName = "CSharp";
    public const string VisualBasicContractName = "VisualBasic";
    public const string TypeScriptContractName = "TypeScript";
}
