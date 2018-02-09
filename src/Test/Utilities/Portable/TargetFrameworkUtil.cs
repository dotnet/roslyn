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

        Net40,
        Net40Extended, 
        Net45,
        Net45Extended,
        Net46,
        Net46Extended,
        NetStandard20,

        /// <summary>
        /// Eventually this will be deleted and replaced with NetStandard20. Short term this creates the "standard"
        /// API set across destkop and coreclr 
        /// </summary>
        Standard
    }

    public static class TargetFrameworkUtil
    {
        public static readonly ImmutableArray<MetadataReference> NoneReferences = ImmutableArray<MetadataReference>.Empty;
        public static readonly ImmutableArray<MetadataReference> Net40References = ImmutableArray.Create(TestBase.MscorlibRef);
        public static readonly ImmutableArray<MetadataReference> Net40ExtendedReferences = ImmutableArray.Create(TestBase.MscorlibRef, TestBase.SystemRef, TestBase.SystemCoreRef, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static readonly ImmutableArray<MetadataReference> Net45References = ImmutableArray.Create(TestBase.MscorlibRef_v4_0_30316_17626);
        public static readonly ImmutableArray<MetadataReference> Net45ExtendedReferences = ImmutableArray.Create(TestBase.MscorlibRef_v4_0_30316_17626, TestBase.SystemRef, TestBase.SystemCoreRef, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static readonly ImmutableArray<MetadataReference> Net46References = ImmutableArray.Create(TestBase.MscorlibRef_v46);
        public static readonly ImmutableArray<MetadataReference> Net46ExtendedReferences = ImmutableArray.Create(TestBase.MscorlibRef_v46, TestBase.SystemRef_v46, TestBase.SystemCoreRef_v46, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static readonly ImmutableArray<MetadataReference> NetStandard20References = ImmutableArray.Create<MetadataReference>(NetStandard20.NetStandard, NetStandard20.MscorlibRef, NetStandard20.SystemRuntimeRef, NetStandard20.SystemDynamicRuntimeRef);
        public static readonly ImmutableArray<MetadataReference> StandardReferences = CoreClrShim.IsRunningOnCoreClr ? NetStandard20References : Net46ExtendedReferences;

        public static ImmutableArray<MetadataReference> GetReferences(TargetFramework tf)
        {
            switch (tf)
            {
                case TargetFramework.None: return NoneReferences;
                case TargetFramework.Net40: return Net40References;
                case TargetFramework.Net40Extended: return Net40ExtendedReferences;
                case TargetFramework.Net45: return Net45References;
                case TargetFramework.Net45Extended: return Net45ExtendedReferences;
                case TargetFramework.Net46: return Net46References;
                case TargetFramework.Net46Extended: return Net46ExtendedReferences;
                case TargetFramework.NetStandard20: return NetStandard20References;
                case TargetFramework.Standard: return StandardReferences;
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
