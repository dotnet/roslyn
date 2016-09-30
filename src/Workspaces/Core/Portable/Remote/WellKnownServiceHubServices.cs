// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class WellKnownServiceHubServices
    {
        public const string SnapshotService = "snapshotService";
        public const string SnapshotService_Done = "Done";

        public const string CodeAnalysisService = "codeAnalysisService";
        public const string CodeAnalysisService_CalculateDiagnosticsAsync = "CalculateDiagnosticsAsync";

        // CodeLens methods.
        public const string CodeAnalysisService_GetReferenceCountAsync = "GetReferenceCountAsync";
        public const string CodeAnalysisService_FindReferenceLocationsAsync = "FindReferenceLocationsAsync";
        public const string CodeAnalysisService_FindReferenceMethodsAsync = "FindReferenceMethodsAsync";
        public const string CodeAnalysisService_GetFullyQualifiedName = "GetFullyQualifiedName";

        public const string RemoteSymbolSearchUpdateEngine = "remoteSymbolSearchUpdateEngine";

        public const string AssetService_RequestAssetAsync = "RequestAssetAsync";
    }
}