// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
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

        public string ToString(RemoteHostPlatform remoteHostPlatform)
        {
            const string Suffix64 = "64";

            var (suffix, isRemoteHost64Bit) = remoteHostPlatform switch
            {
                RemoteHostPlatform.Desktop32 => ("Desktop", false),
                RemoteHostPlatform.Desktop64 => ("Desktop64", true),
                RemoteHostPlatform.Core64 => ("Core64", true),

                _ => throw ExceptionUtilities.UnexpectedValue(remoteHostPlatform),
            };


            return CustomServiceName ?? (WellKnownService, isRemoteHost64Bit) switch
            {
                (WellKnownServiceHubService.RemoteHost, _) => Prefix + nameof(WellKnownServiceHubService.RemoteHost) + suffix,
                (WellKnownServiceHubService.CodeAnalysis, _) => Prefix + nameof(WellKnownServiceHubService.CodeAnalysis) + suffix,
                (WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine, _) => Prefix + nameof(WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine) + suffix,
                (WellKnownServiceHubService.RemoteDesignerAttributeService, _) => Prefix + nameof(WellKnownServiceHubService.RemoteDesignerAttributeService) + suffix,
                (WellKnownServiceHubService.RemoteProjectTelemetryService, _) => Prefix + nameof(WellKnownServiceHubService.RemoteProjectTelemetryService) + suffix,
                (WellKnownServiceHubService.RemoteTodoCommentsService, _) => Prefix + nameof(WellKnownServiceHubService.RemoteTodoCommentsService) + suffix,
                (WellKnownServiceHubService.LanguageServer, _) => Prefix + nameof(WellKnownServiceHubService.LanguageServer) + suffix,

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

        public bool Equals([AllowNull] RemoteServiceName other)
            => CustomServiceName == other.CustomServiceName && WellKnownService == other.WellKnownService;

        public static bool operator ==(RemoteServiceName left, RemoteServiceName right)
            => left.Equals(right);

        public static bool operator !=(RemoteServiceName left, RemoteServiceName right)
            => !(left == right);

        public static implicit operator RemoteServiceName(WellKnownServiceHubService wellKnownService)
            => new RemoteServiceName(wellKnownService);
    }

    internal enum RemoteHostPlatform
    {
        Desktop32,
        Desktop64,
        Core64
    }
}
