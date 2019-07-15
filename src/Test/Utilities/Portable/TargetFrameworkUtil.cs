// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using static TestReferences;

namespace Roslyn.Test.Utilities
{
    public enum TargetFramework
    {
        /// <summary>
        /// Explicit pick a target framework that has no references
        /// </summary>
        Empty,

        NetStandard20,
        NetCoreApp30,
        NetStandardLatest = NetCoreApp30,
        WinRT,

        /// <summary>
        /// Eventually this will be deleted and replaced with NetStandard20. Short term this creates the "standard"
        /// API set across destkop and coreclr 
        /// </summary>
        Standard,
        StandardLatest,
        StandardAndCSharp,
        StandardAndVBRuntime,

        /// <summary>
        /// This is represents the set of tests which must be mscorlib40 on desktop but full net standard on coreclr.
        /// </summary>
        StandardCompat,

        /// <summary>
        /// Compat framework for the default set of references many vb compilations get.
        /// </summary>
        DefaultVb,

        // The flavors of mscorlib we support + extending them with LINQ and dynamic.
        Mscorlib40,
        Mscorlib40Extended,
        Mscorlib40AndSystemCore,
        Mscorlib40AndVBRuntime,
        Mscorlib45,
        Mscorlib45Extended,
        Mscorlib45AndCSharp,
        Mscorlib45AndVBRuntime,
        Mscorlib46,
        Mscorlib46Extended,
        Mscorlib461,
        Mscorlib461Extended,
        DesktopLatestExtended = Mscorlib461Extended,

        /// <summary>
        /// Minimal set of required types (<see cref="NetFx.Minimal.mincorlib"/>).
        /// </summary>
        Minimal,

        /// <summary>
        /// Minimal set of required types and Task implementation (<see cref="NetFx.Minimal.minasync"/>).
        /// </summary>
        MinimalAsync,
    }

    public static class TargetFrameworkUtil
    {
        public static MetadataReference StandardCSharpReference => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20.MicrosoftCSharpRef : TestBase.CSharpDesktopRef;

        /*
         * ⚠ Dev note ⚠: properties in TestBase are backed by Lazy<T>. Avoid changes to the following properties
         * which would force the initialization of these properties in the static constructor, since the stack traces
         * for a TypeLoadException are missing important information for resolving problems if/when they occur.
         * https://github.com/dotnet/roslyn/issues/25961
         */

        public static ImmutableArray<MetadataReference> Mscorlib40References => ImmutableArray.Create(TestBase.MscorlibRef);
        public static ImmutableArray<MetadataReference> Mscorlib40ExtendedReferences => ImmutableArray.Create(TestBase.MscorlibRef, TestBase.SystemRef, TestBase.SystemCoreRef, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static ImmutableArray<MetadataReference> Mscorlib40andSystemCoreReferences => ImmutableArray.Create(TestBase.MscorlibRef, TestBase.SystemCoreRef);
        public static ImmutableArray<MetadataReference> Mscorlib40andVBRuntimeReferences => ImmutableArray.Create(TestBase.MscorlibRef, TestBase.SystemRef, TestBase.MsvbRef);
        public static ImmutableArray<MetadataReference> Mscorlib45References => ImmutableArray.Create(TestBase.MscorlibRef_v4_0_30316_17626);
        public static ImmutableArray<MetadataReference> Mscorlib45ExtendedReferences => ImmutableArray.Create(TestBase.MscorlibRef_v4_0_30316_17626, TestBase.SystemRef, TestBase.SystemCoreRef_v4_0_30319_17929, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static ImmutableArray<MetadataReference> Mscorlib45AndCSharpReferences => ImmutableArray.Create(TestBase.MscorlibRef_v4_0_30316_17626, TestBase.SystemCoreRef_v4_0_30319_17929, TestBase.CSharpRef);
        public static ImmutableArray<MetadataReference> Mscorlib45AndVBRuntimeReferences => ImmutableArray.Create(TestBase.MscorlibRef_v4_0_30316_17626, TestBase.SystemRef, TestBase.MsvbRef_v4_0_30319_17929);
        public static ImmutableArray<MetadataReference> Mscorlib46References => ImmutableArray.Create(TestBase.MscorlibRef_v46);
        public static ImmutableArray<MetadataReference> Mscorlib46ExtendedReferences => ImmutableArray.Create(TestBase.MscorlibRef_v46, TestBase.SystemRef_v46, TestBase.SystemCoreRef_v46, TestBase.ValueTupleRef, TestBase.SystemRuntimeFacadeRef);
        public static ImmutableArray<MetadataReference> Mscorlib461References => ImmutableArray.Create<MetadataReference>(Net461.mscorlibRef);
        public static ImmutableArray<MetadataReference> Mscorlib461ExtendedReferences => ImmutableArray.Create<MetadataReference>(Net461.mscorlibRef, Net461.SystemRef, Net461.SystemCoreRef, Net461.SystemValueTupleRef, Net461.SystemRuntimeRef, Net461.netstandardRef);
        public static ImmutableArray<MetadataReference> NetStandard20References => ImmutableArray.Create<MetadataReference>(NetStandard20.NetStandard, NetStandard20.MscorlibRef, NetStandard20.SystemRuntimeRef, NetStandard20.SystemCoreRef, NetStandard20.SystemDynamicRuntimeRef);
        public static ImmutableArray<MetadataReference> NetCoreApp30References => ImmutableArray.Create<MetadataReference>(NetCoreApp30.NetStandard, NetCoreApp30.MscorlibRef, NetCoreApp30.SystemRuntimeRef, NetCoreApp30.SystemCollectionsRef, NetCoreApp30.SystemCoreRef, NetCoreApp30.SystemDynamicRuntimeRef, NetCoreApp30.SystemConsoleRef, NetCoreApp30.SystemLinqRef, NetCoreApp30.SystemLinqExpressionsRef);
        public static ImmutableArray<MetadataReference> WinRTReferences => ImmutableArray.Create(TestBase.WinRtRefs);
        public static ImmutableArray<MetadataReference> StandardReferences => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20References : Mscorlib46ExtendedReferences;
        public static ImmutableArray<MetadataReference> StandardLatestReferences => RuntimeUtilities.IsCoreClrRuntime ? NetCoreApp30References : Mscorlib46ExtendedReferences;
        public static ImmutableArray<MetadataReference> StandardAndCSharpReferences => StandardReferences.Add(StandardCSharpReference);
        public static ImmutableArray<MetadataReference> StandardAndVBRuntimeReferences => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20References.Add(NetStandard20.MicrosoftVisualBasicRef) : Mscorlib46ExtendedReferences.Add(TestBase.MsvbRef_v4_0_30319_17929);
        public static ImmutableArray<MetadataReference> StandardCompatReferences => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20References : Mscorlib40References;
        public static ImmutableArray<MetadataReference> DefaultVbReferences => ImmutableArray.Create(TestBase.MscorlibRef, TestBase.SystemRef, TestBase.SystemCoreRef, TestBase.MsvbRef);
        public static ImmutableArray<MetadataReference> MinimalReferences => ImmutableArray.Create(TestBase.MinCorlibRef);
        public static ImmutableArray<MetadataReference> MinimalAsyncReferences => ImmutableArray.Create(TestBase.MinAsyncCorlibRef);

        public static ImmutableArray<MetadataReference> GetReferences(TargetFramework targetFramework) => targetFramework switch
        {
            TargetFramework.Empty => ImmutableArray<MetadataReference>.Empty,
            TargetFramework.Mscorlib40 => Mscorlib40References,
            TargetFramework.Mscorlib40Extended => Mscorlib40ExtendedReferences,
            TargetFramework.Mscorlib40AndSystemCore => Mscorlib40andSystemCoreReferences,
            TargetFramework.Mscorlib40AndVBRuntime => Mscorlib40andVBRuntimeReferences,
            TargetFramework.Mscorlib45 => Mscorlib45References,
            TargetFramework.Mscorlib45Extended => Mscorlib45ExtendedReferences,
            TargetFramework.Mscorlib45AndCSharp => Mscorlib45AndCSharpReferences,
            TargetFramework.Mscorlib45AndVBRuntime => Mscorlib45AndVBRuntimeReferences,
            TargetFramework.Mscorlib46 => Mscorlib46References,
            TargetFramework.Mscorlib46Extended => Mscorlib46ExtendedReferences,
            TargetFramework.Mscorlib461 => Mscorlib46References,
            TargetFramework.Mscorlib461Extended => Mscorlib461ExtendedReferences,
            TargetFramework.NetStandard20 => NetStandard20References,
            TargetFramework.NetCoreApp30 => NetCoreApp30References,
            TargetFramework.WinRT => WinRTReferences,
            TargetFramework.Standard => StandardReferences,
            TargetFramework.StandardLatest => StandardLatestReferences,
            TargetFramework.StandardAndCSharp => StandardAndCSharpReferences,
            TargetFramework.StandardAndVBRuntime => StandardAndVBRuntimeReferences,
            TargetFramework.StandardCompat => StandardCompatReferences,
            TargetFramework.DefaultVb => DefaultVbReferences,
            TargetFramework.Minimal => MinimalReferences,
            TargetFramework.MinimalAsync => MinimalAsyncReferences,
            _ => throw new InvalidOperationException($"Unexpected target framework {targetFramework}"),
        };

        public static ImmutableArray<MetadataReference> GetReferences(TargetFramework tf, IEnumerable<MetadataReference> additionalReferences)
        {
            var references = GetReferences(tf);
            if (additionalReferences == null)
            {
                return references;
            }

            checkForDuplicateReferences();
            return references.AddRange(additionalReferences);

            // Check to see if there are any duplicate references. This guards against tests inadvertently passing multiple copies of 
            // say System.Core to the tests and implicitly depending on the higher one to win. The few tests which actually mean to 
            // pass multiple verisons of a DLL should manually construct the reference list and not use this helper.
            void checkForDuplicateReferences()
            {
                var nameSet = new HashSet<string>(getNames(references), StringComparer.OrdinalIgnoreCase);
                foreach (var r in additionalReferences)
                {
                    if (references.Contains(r))
                    {
                        throw new Exception($"Duplicate reference detected {r.Display}");
                    }

                    var name = getName(r);
                    if (name != null && !nameSet.Add(name))
                    {
                        throw new Exception($"Duplicate reference detected {r.Display} - {name}");
                    }
                }
            }

            IEnumerable<string> getNames(IEnumerable<MetadataReference> e)
            {
                foreach (var r in e)
                {
                    var name = getName(r);
                    if (name != null)
                    {
                        yield return name;
                    }
                }
            }

            string getName(MetadataReference m)
            {
                if (m is PortableExecutableReference p &&
                    p.GetMetadata() is AssemblyMetadata assemblyMetadata)
                {
                    try
                    {
                        var identity = assemblyMetadata.GetAssembly().Identity;
                        return identity?.Name;
                    }
                    catch (BadImageFormatException)
                    {
                        // Happens when a native image is incorrectly passed as a PE.
                        return null;
                    }
                }

                return null;
            }
        }
    }
}
