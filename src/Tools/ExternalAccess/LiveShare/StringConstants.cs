// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare
{
    internal class StringConstants
    {
        public const string BaseRemoteAssemblyTitle = "Base Remote Language Service";

        // The service name for an LSP server implemented using Roslyn designed to be used with the Roslyn client
        public const string RoslynContractName = "Roslyn";
        // The service name for an LSP server implemented using Roslyn designed to be used with the LSP SDK client
        public const string RoslynLspSdkContractName = "RoslynLSPSDK";

        public const string CSharpLspLanguageName = "C#_LSP";
        public const string CSharpLspContentTypeName = "C#_LSP";
        public const string TypeScriptLanguageName = "TypeScript";
        public const string VBLspLanguageName = "VB_LSP";
        public const string VBLspContentTypeName = "VB_LSP";

        public const string CSharpLspPackageGuidString = "9d42b837-b615-4574-80e1-f48828b6954d";
        public const string CSharpLspLanguageServiceGuidString = "69463241-e026-48b2-a08b-a1a83f0cbafe";
        public const string CSharpLspDebuggerLanguageGuidString = "8D07D1C6-5DE2-45CE-AEBF-7E21E03F9B10";

        // Note: this workspace kind is defined in Roslyn's:
        // Implementation\DebuggerIntelliSense\DebuggerIntellisenseWorkspace.cs
        // It is misspelled there as "DebbugerIntellisense"
        public const string DebuggerIntellisenseWorkspaceKind = "DebbugerIntellisense";
    }
}
