// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using static TestReferences;

namespace Roslyn.Test.Utilities
{
    public enum TargetFramework
    {
        /// <summary>
        /// Explicit pick a target framework that has no references
        /// </summary>
        None,

        NetStandard20,
        WinRT,

        /// <summary>
        /// Eventually this will be deleted and replaced with NetStandard20. Short term this creates the "standard"
        /// API set across destkop and coreclr 
        /// </summary>
        Standard,

        /// <summary>
        /// This is represents the set of tests which must be mscorlib40 on desktop but full net standard on coreclr.
        /// </summary>
        StandardCompat,

        // The flavors of mscorlib we support + extending them with LINQ and dynamic.
        Mscorlib40,
        Mscorlib40Extended, 
        Mscorlib45,
        Mscorlib45Extended,
        Mscorlib46,
        Mscorlib46Extended,
    }

    public static class TargetFrameworkUtil
    {
        public static readonly ImmutableArray<MetadataReference> NoneReferences = ImmutableArray<MetadataReference>.Empty;
        public static readonly ImmutableArray<MetadataReference> Mscorlib40References = ImmutableArray.Create(TestBase.MscorlibRef);
        public static readonly ImmutableArray<MetadataReference> Mscorlib40ExtendedReferences = ImmutableArray.Create(TestBase.MscorlibRef, TestBase.SystemRef, TestBase.SystemCoreRef, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static readonly ImmutableArray<MetadataReference> Mscorlib45References = ImmutableArray.Create(TestBase.MscorlibRef_v4_0_30316_17626);
        public static readonly ImmutableArray<MetadataReference> Mscorlib45ExtendedReferences = ImmutableArray.Create(TestBase.MscorlibRef_v4_0_30316_17626, TestBase.SystemRef, TestBase.SystemCoreRef, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static readonly ImmutableArray<MetadataReference> Mscorlib46References = ImmutableArray.Create(TestBase.MscorlibRef_v46);
        public static readonly ImmutableArray<MetadataReference> Mscorlib46ExtendedReferences = ImmutableArray.Create(TestBase.MscorlibRef_v46, TestBase.SystemRef_v46, TestBase.SystemCoreRef_v46, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static readonly ImmutableArray<MetadataReference> NetStandard20References = ImmutableArray.Create<MetadataReference>(NetStandard20.NetStandard, NetStandard20.MscorlibRef, NetStandard20.SystemRuntimeRef, NetStandard20.SystemDynamicRuntimeRef);
        public static readonly ImmutableArray<MetadataReference> WinRTReferences = ImmutableArray.Create(TestBase.WinRtRefs);
        public static readonly ImmutableArray<MetadataReference> StandardReferences = CoreClrShim.IsRunningOnCoreClr ? NetStandard20References : Mscorlib46ExtendedReferences;
        public static readonly ImmutableArray<MetadataReference> StandardCompatReferences = CoreClrShim.IsRunningOnCoreClr ? NetStandard20References : Mscorlib40References;

        public static ImmutableArray<MetadataReference> GetReferences(TargetFramework tf)
        {
            switch (tf)
            {
                case TargetFramework.None: return NoneReferences;
                case TargetFramework.Mscorlib40: return Mscorlib40References;
                case TargetFramework.Mscorlib40Extended: return Mscorlib40ExtendedReferences;
                case TargetFramework.Mscorlib45: return Mscorlib45References;
                case TargetFramework.Mscorlib45Extended: return Mscorlib45ExtendedReferences;
                case TargetFramework.Mscorlib46: return Mscorlib46References;
                case TargetFramework.Mscorlib46Extended: return Mscorlib46ExtendedReferences;
                case TargetFramework.NetStandard20: return NetStandard20References;
                case TargetFramework.WinRT: return WinRTReferences;
                case TargetFramework.Standard: return StandardReferences;
                case TargetFramework.StandardCompat: return StandardCompatReferences;
                default: throw new InvalidOperationException($"Unexpected target framework {tf}");
            }
        }

        public static ImmutableArray<MetadataReference> GetReferences(TargetFramework tf, IEnumerable<MetadataReference> additionalReferences)
        {
            var references = GetReferences(tf);
            if (additionalReferences == null)
            {
                return references;
            }

            foreach (var r in additionalReferences)
            {
                if (references.Contains(r))
                {
                    throw new Exception($"Duplicate reference detected {r.Display}");
                }
            }

            return references.AddRange(additionalReferences);
        }
    }
}
