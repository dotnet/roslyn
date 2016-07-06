// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class WellKnownServiceHubServices
    {
        public const string RemoteHostService = "remotehostService";
        public const string RemoteHostService_Connect = "Connect";

        public const string SolutionSnapshotService = "solutionSnapshotService";
        public const string SolutionSnapshotService_Done = "Done";

        public const string CodeAnalysisService = "codeAnalysisService";
        public const string CodeAnalysisService_GetDiagnostics = "CalculateDiagnosticsAsync";

        public const string AssetService_RequestAsset = "RequestAssetAsync";
    }
}
