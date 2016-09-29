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

        #region RemoteSymbolSearchUpdateEngine

        public const string RemoteSymbolSearchUpdateEngine = "remoteSymbolSearchUpdateEngine";

        public const string RemoteSymbolSearchUpdateEngine_UpdateContinuouslyAsync = "UpdateContinuouslyAsync";
        public const string RemoteSymbolSearchUpdateEngine_FindPackagesWithTypeAsync = "FindPackagesWithTypeAsync";
        public const string RemoteSymbolSearchUpdateEngine_FindReferenceAssembliesWithTypeAsync = "FindReferenceAssembliesWithTypeAsync";

        #endregion

        #region NavigateTo

        public const string CodeAnalysisService_SearchDocumentAsync = "SearchDocumentAsync";
        public const string CodeAnalysisService_SearchProjectAsync = "SearchProjectAsync";

        #endregion

        #region FindReferences

        public const string CodeAnalysisService_FindReferencesAsync = "FindReferencesAsync";

        #endregion 

        public const string AssetService_RequestAssetAsync = "RequestAssetAsync";
    }
}