// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal const string Suffix64 = "64";
        internal const string SuffixServerGC = "S";
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

        public string ToString(bool isRemoteHost64Bit, bool isRemoteHostServerGC)
        {
            return CustomServiceName ?? (WellKnownService, isRemoteHost64Bit, isRemoteHostServerGC) switch
            {
                (WellKnownServiceHubService.RemoteHost, false, _) => Prefix + nameof(WellKnownServiceHubService.RemoteHost),
                (WellKnownServiceHubService.RemoteHost, true, false) => Prefix + nameof(WellKnownServiceHubService.RemoteHost) + Suffix64,
                (WellKnownServiceHubService.RemoteHost, true, true) => Prefix + nameof(WellKnownServiceHubService.RemoteHost) + Suffix64 + SuffixServerGC,

                (WellKnownServiceHubService.IntelliCode, false, _) => IntelliCodeServiceName,
                (WellKnownServiceHubService.IntelliCode, true, false) => IntelliCodeServiceName + Suffix64,
                (WellKnownServiceHubService.IntelliCode, true, true) => IntelliCodeServiceName + Suffix64 + SuffixServerGC,
                (WellKnownServiceHubService.Razor, false, _) => RazorServiceName,
                (WellKnownServiceHubService.Razor, true, false) => RazorServiceName + Suffix64,
                (WellKnownServiceHubService.Razor, true, true) => RazorServiceName + Suffix64 + SuffixServerGC,
                (WellKnownServiceHubService.UnitTestingAnalysisService, false, _) => UnitTestingAnalysisServiceName,
                (WellKnownServiceHubService.UnitTestingAnalysisService, true, false) => UnitTestingAnalysisServiceName + Suffix64,
                (WellKnownServiceHubService.UnitTestingAnalysisService, true, true) => UnitTestingAnalysisServiceName + Suffix64 + SuffixServerGC,
                (WellKnownServiceHubService.LiveUnitTestingBuildService, false, _) => LiveUnitTestingBuildServiceName,
                (WellKnownServiceHubService.LiveUnitTestingBuildService, true, false) => LiveUnitTestingBuildServiceName + Suffix64,
                (WellKnownServiceHubService.LiveUnitTestingBuildService, true, true) => LiveUnitTestingBuildServiceName + Suffix64 + SuffixServerGC,
                (WellKnownServiceHubService.UnitTestingSourceLookupService, false, _) => UnitTestingSourceLookupServiceName,
                (WellKnownServiceHubService.UnitTestingSourceLookupService, true, false) => UnitTestingSourceLookupServiceName + Suffix64,
                (WellKnownServiceHubService.UnitTestingSourceLookupService, true, true) => UnitTestingSourceLookupServiceName + Suffix64 + SuffixServerGC,

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
            => new(wellKnownService);
    }
}
