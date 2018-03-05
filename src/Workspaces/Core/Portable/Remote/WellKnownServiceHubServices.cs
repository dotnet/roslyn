// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class WellKnownServiceHubServices
    {
        public static void Set64bit(bool x64)
        {
            var bit = x64 ? "64" : "";

            SnapshotService = "snapshotService" + bit;
            CodeAnalysisService = "codeAnalysisService" + bit;
            RemoteSymbolSearchUpdateEngine = "remoteSymbolSearchUpdateEngine" + bit;
        }

        public static string SnapshotService { get; private set; } = "snapshotService";
        public static string CodeAnalysisService { get; private set; } = "codeAnalysisService";
        public static string RemoteSymbolSearchUpdateEngine { get; private set; } = "remoteSymbolSearchUpdateEngine";

        // CodeLens methods.
        public const string CodeAnalysisService_GetReferenceCountAsync = "GetReferenceCountAsync";
        public const string CodeAnalysisService_FindReferenceLocationsAsync = "FindReferenceLocationsAsync";
        public const string CodeAnalysisService_FindReferenceMethodsAsync = "FindReferenceMethodsAsync";
        public const string CodeAnalysisService_GetFullyQualifiedName = "GetFullyQualifiedName";

        public const string ServiceHubServiceBase_Initialize = "Initialize";
        public const string AssetService_RequestAssetAsync = "RequestAssetAsync";

        public const string CodeAnalysisService_CalculateDiagnosticsAsync = "CalculateDiagnosticsAsync";
    }
}
