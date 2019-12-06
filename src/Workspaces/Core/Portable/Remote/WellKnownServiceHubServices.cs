// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class WellKnownServiceHubServices
    {
        public static void Set64bit(bool x64)
        {
            var bit = x64 ? "64" : "";

            SnapshotService = "roslynSnapshot" + bit;
            CodeAnalysisService = "roslynCodeAnalysis" + bit;
            RemoteSymbolSearchUpdateEngine = "roslynRemoteSymbolSearchUpdateEngine" + bit;
            CSharpLanguageServer = "roslynCSharpLanguageServer" + bit;
            VisualBasicLanguageServer = "roslynVisualBasicLanguageServer" + bit;
        }

        public static string SnapshotService { get; private set; } = "roslynSnapshot";
        public static string CodeAnalysisService { get; private set; } = "roslynCodeAnalysis";
        public static string RemoteSymbolSearchUpdateEngine { get; private set; } = "roslynRemoteSymbolSearchUpdateEngine";
        public static string CSharpLanguageServer { get; private set; } = "roslynCSharpLanguageServer";
        public static string VisualBasicLanguageServer { get; private set; } = "roslynVisualBasicLanguageServer";

        // these are OOP implementation itself should care. not features that consume OOP care
        public const string ServiceHubServiceBase_Initialize = "Initialize";
        public const string AssetService_RequestAssetAsync = "RequestAssetAsync";
        public const string AssetService_IsExperimentEnabledAsync = "IsExperimentEnabledAsync";
    }
}
