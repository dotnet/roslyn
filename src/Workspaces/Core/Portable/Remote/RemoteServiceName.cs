// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Abstract the name of a remote service.
    /// </summary>
    /// <remarks>
    /// Allows partner teams to specify bitness-specific service name, while we can use bitness agnostic id for well-known services.
    /// TODO: Update LUT and SBD to use well-known ids and remove this abstraction (https://github.com/dotnet/roslyn/issues/44327).
    /// </remarks>
    internal readonly struct RemoteServiceName : IEquatable<RemoteServiceName>
    {
        internal const string Prefix = "roslyn";
        internal const string IntelliCodeServiceName = "pythia";
        internal const string RazorServiceName = "razorLanguageService";
        internal const string UnitTestingAnalysisServiceName = "UnitTestingAnalysis";
        internal const string LiveUnitTestingBuildServiceName = "LiveUnitTestingBuild";
        internal const string UnitTestingSourceLookupServiceName = "UnitTestingSourceLookup";

        public readonly WellKnownServiceHubService WellKnownService;
        public readonly string? CustomServiceName;

        public RemoteServiceName(WellKnownServiceHubService wellKnownService)
        {
            WellKnownService = wellKnownService;
            CustomServiceName = null;
        }

        /// <summary>
        /// Exact service name - must be reflect the bitness of the ServiceHub process.
        /// </summary>
        public RemoteServiceName(string customServiceName)
        {
            WellKnownService = WellKnownServiceHubService.None;
            CustomServiceName = customServiceName;
        }

        public string ToString(bool isRemoteHost64Bit)
        {
            const string Suffix64 = "64";

            return CustomServiceName ?? (WellKnownService, isRemoteHost64Bit) switch
            {
                (WellKnownServiceHubService.RemoteHost, false) => Prefix + nameof(WellKnownServiceHubService.RemoteHost),
                (WellKnownServiceHubService.RemoteHost, true) => Prefix + nameof(WellKnownServiceHubService.RemoteHost) + Suffix64,
                (WellKnownServiceHubService.CodeAnalysis, false) => Prefix + nameof(WellKnownServiceHubService.CodeAnalysis),
                (WellKnownServiceHubService.CodeAnalysis, true) => Prefix + nameof(WellKnownServiceHubService.CodeAnalysis) + Suffix64,
                (WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine, false) => Prefix + nameof(WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine),
                (WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine, true) => Prefix + nameof(WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine) + Suffix64,
                (WellKnownServiceHubService.RemoteDesignerAttributeService, false) => Prefix + nameof(WellKnownServiceHubService.RemoteDesignerAttributeService),
                (WellKnownServiceHubService.RemoteDesignerAttributeService, true) => Prefix + nameof(WellKnownServiceHubService.RemoteDesignerAttributeService) + Suffix64,
                (WellKnownServiceHubService.RemoteProjectTelemetryService, false) => Prefix + nameof(WellKnownServiceHubService.RemoteProjectTelemetryService),
                (WellKnownServiceHubService.RemoteProjectTelemetryService, true) => Prefix + nameof(WellKnownServiceHubService.RemoteProjectTelemetryService) + Suffix64,
                (WellKnownServiceHubService.RemoteTodoCommentsService, false) => Prefix + nameof(WellKnownServiceHubService.RemoteTodoCommentsService),
                (WellKnownServiceHubService.RemoteTodoCommentsService, true) => Prefix + nameof(WellKnownServiceHubService.RemoteTodoCommentsService) + Suffix64,
                (WellKnownServiceHubService.LanguageServer, false) => Prefix + nameof(WellKnownServiceHubService.LanguageServer),
                (WellKnownServiceHubService.LanguageServer, true) => Prefix + nameof(WellKnownServiceHubService.LanguageServer) + Suffix64,

                (WellKnownServiceHubService.IntelliCode, false) => IntelliCodeServiceName,
                (WellKnownServiceHubService.IntelliCode, true) => IntelliCodeServiceName + Suffix64,
                (WellKnownServiceHubService.Razor, false) => RazorServiceName,
                (WellKnownServiceHubService.Razor, true) => RazorServiceName + Suffix64,
                (WellKnownServiceHubService.UnitTestingAnalysisService, false) => UnitTestingAnalysisServiceName,
                (WellKnownServiceHubService.UnitTestingAnalysisService, true) => UnitTestingAnalysisServiceName + Suffix64,
                (WellKnownServiceHubService.LiveUnitTestingBuildService, false) => LiveUnitTestingBuildServiceName,
                (WellKnownServiceHubService.LiveUnitTestingBuildService, true) => LiveUnitTestingBuildServiceName + Suffix64,
                (WellKnownServiceHubService.UnitTestingSourceLookupService, false) => UnitTestingSourceLookupServiceName,
                (WellKnownServiceHubService.UnitTestingSourceLookupService, true) => UnitTestingSourceLookupServiceName + Suffix64,

                _ => throw ExceptionUtilities.UnexpectedValue(WellKnownService),
            };
        }

        public override bool Equals(object? obj)
            => obj is RemoteServiceName name && Equals(name);

        public override int GetHashCode()
            => Hash.Combine(CustomServiceName, (int)WellKnownService);

        public bool Equals(RemoteServiceName other)
            => CustomServiceName == other.CustomServiceName && WellKnownService == other.WellKnownService;

        public static bool operator ==(RemoteServiceName left, RemoteServiceName right)
            => left.Equals(right);

        public static bool operator !=(RemoteServiceName left, RemoteServiceName right)
            => !(left == right);

        public static implicit operator RemoteServiceName(WellKnownServiceHubService wellKnownService)
            => new RemoteServiceName(wellKnownService);
    }
}
