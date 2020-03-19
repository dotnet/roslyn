﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class WellKnownServiceHubServices
    {
        public static void Set64bit(bool x64)
        {
            var bit = x64 ? "64" : "";

            SnapshotService = "roslynSnapshot" + bit;
            CodeAnalysisService = "roslynCodeAnalysis" + bit;
            RemoteDesignerAttributeService = "roslynRemoteDesignerAttributeService" + bit;
            RemoteProjectTelemetryService = "roslynRemoteProjectTelemetryService" + bit;
            RemoteSymbolSearchUpdateEngine = "roslynRemoteSymbolSearchUpdateEngine" + bit;
            RemoteTodoCommentsService = "roslynRemoteTodoCommentsService" + bit;
            LanguageServer = "roslynLanguageServer" + bit;
        }

        public static string SnapshotService { get; private set; } = "roslynSnapshot";
        public static string CodeAnalysisService { get; private set; } = "roslynCodeAnalysis";
        public static string RemoteDesignerAttributeService { get; private set; } = "roslynRemoteDesignerAttributeService";
        public static string RemoteProjectTelemetryService { get; private set; } = "roslynRemoteProjectTelemetryService";
        public static string RemoteSymbolSearchUpdateEngine { get; private set; } = "roslynRemoteSymbolSearchUpdateEngine";
        public static string RemoteTodoCommentsService { get; private set; } = "roslynRemoteTodoCommentsService";
        public static string LanguageServer { get; private set; } = "roslynLanguageServer";

        // these are OOP implementation itself should care. not features that consume OOP care
        public const string ServiceHubServiceBase_Initialize = "Initialize";
        public const string AssetService_RequestAssetAsync = "RequestAssetAsync";
        public const string AssetService_IsExperimentEnabledAsync = "IsExperimentEnabledAsync";
    }
}
