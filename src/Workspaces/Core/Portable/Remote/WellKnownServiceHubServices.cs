// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class WellKnownServiceHubServices
    {
        public const string NamePrefix = "roslyn";

        public static void Set64bit(bool x64)
        {
            var bit = x64 ? "64" : "";

            RemoteHostService = "roslynRemoteHost" + bit;
            CodeAnalysisService = NamePrefix + "CodeAnalysis" + bit;
            RemoteDesignerAttributeService = NamePrefix + "RemoteDesignerAttributeService" + bit;
            RemoteProjectTelemetryService = NamePrefix + "RemoteProjectTelemetryService" + bit;
            RemoteSymbolSearchUpdateEngine = NamePrefix + "RemoteSymbolSearchUpdateEngine" + bit;
            RemoteTodoCommentsService = NamePrefix + "RemoteTodoCommentsService" + bit;
            LanguageServer = NamePrefix + "LanguageServer" + bit;
        }

        public static string RemoteHostService { get; private set; } = NamePrefix + "RemoteHost";
        public static string CodeAnalysisService { get; private set; } = NamePrefix + "CodeAnalysis";
        public static string RemoteSymbolSearchUpdateEngine { get; private set; } = NamePrefix + "RemoteSymbolSearchUpdateEngine";
        public static string RemoteDesignerAttributeService { get; private set; } = NamePrefix + "RemoteDesignerAttributeService";
        public static string RemoteProjectTelemetryService { get; private set; } = NamePrefix + "RemoteProjectTelemetryService";
        public static string RemoteTodoCommentsService { get; private set; } = NamePrefix + "RemoteTodoCommentsService";
        public static string LanguageServer { get; private set; } = NamePrefix + "LanguageServer";

        // these are OOP implementation itself should care. not features that consume OOP care
        public const string ServiceHubServiceBase_Initialize = "Initialize";
    }
}
