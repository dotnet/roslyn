// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class StringConstants
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

        public const string CSharpLspLanguageName = "C#_LSP";
        public const string CSharpLspContentTypeName = "C#_LSP";
        public const string TypeScriptLanguageName = "TypeScript";
        public const string VBLspLanguageName = "VB_LSP";
        public const string VBLspContentTypeName = "VB_LSP";

        public const string CSharpLspPackageGuidString = "BB7E83F4-EAF6-456C-B140-F8C027A7ED8A";
        public const string CSharpLspLanguageServiceGuidString = "B7B576C5-24AE-4FBB-965E-382125FD4889";
        public const string CSharpLspDebuggerLanguageGuidString = "8F3CFD75-9F45-4092-A944-48E21265D19B";

        // Note: this workspace kind is defined in Roslyn's:
        // Implementation\DebuggerIntelliSense\DebuggerIntellisenseWorkspace.cs
        // It is misspelled there as "DebbugerIntellisense"
        public const string DebuggerIntellisenseWorkspaceKind = "DebbugerIntellisense";
    }
}
