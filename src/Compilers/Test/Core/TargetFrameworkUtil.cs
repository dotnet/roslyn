// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using static TestReferences;
using static Roslyn.Test.Utilities.TestMetadata;
using Basic.Reference.Assemblies;

namespace Roslyn.Test.Utilities
{
    public enum TargetFramework
    {
        /// <summary>
        /// Explicit pick a target framework that has no references
        /// </summary>
        Empty,

        // These are the preferred values that we should be targeting 
        NetStandard20,
        NetCoreApp,
        NetFramework,
        StandardLatest,

        // Eventually these will be deleted and replaced with NetStandard20. Short term this creates the "standard"
        // API set across desktop and coreclr. It's also helpful because there are no null annotations hence error
        // messages have consistent signatures across .NET Core / Framework tests.
        Standard,
        StandardAndCSharp,
        StandardAndVBRuntime,

        /// <summary>
        /// Compat framework for the default set of references many vb compilations get.
        /// </summary>
        DefaultVb,

        /// <summary>
        /// This will eventually be folded into NetCoreApp. The default experience for compiling .NET Core code 
        /// includes the Microsoft.CSharp reference hence it should be the default for our tests
        /// </summary>
        NetCoreAppAndCSharp,

        /// <summary>
        /// Used for building tests against WinRT scenarios
        /// </summary>
        WinRT,

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

        Net50,
        Net60,
    }

    /// <summary>
    /// This type holds the reference information for the latest .NET Core platform. Tests
    /// targeting .NET core specifically should use the references here. As the platform moves
    /// forward these will be moved to target the latest .NET Core supported by the compiler
    /// </summary>
    public static class NetCoreApp
    {
        public static ImmutableArray<Net50.ReferenceInfo> AllReferenceInfos { get; } = ImmutableArray.CreateRange(Net50.References.All);
        public static ImmutableArray<MetadataReference> References { get; } = ImmutableArray.CreateRange<MetadataReference>(Net50.All);

        /// <summary>
        /// A subset of <see cref="References"/> that can compile 99% of our test code.
        /// </summary>
        public static ImmutableArray<MetadataReference> StandardReferences { get; } = ImmutableArray.Create<MetadataReference>(
            Net50.netstandard,
            Net50.mscorlib,
            Net50.SystemRuntime,
            Net50.SystemCore,
            Net50.SystemConsole,
            Net50.SystemLinq,
            Net50.SystemLinqExpressions,
            Net50.SystemThreadingTasks,
            Net50.SystemCollections);

        public static PortableExecutableReference netstandard { get; } = Net50.netstandard;
        public static PortableExecutableReference mscorlib { get; } = Net50.mscorlib;
        public static PortableExecutableReference SystemRuntime { get; } = Net50.SystemRuntime;
        public static PortableExecutableReference SystemCore { get; } = Net50.SystemCore;
        public static PortableExecutableReference SystemConsole { get; } = Net50.SystemConsole;
        public static PortableExecutableReference SystemLinq { get; } = Net50.SystemLinq;
        public static PortableExecutableReference SystemLinqExpressions { get; } = Net50.SystemLinqExpressions;
        public static PortableExecutableReference SystemThreadingTasks { get; } = Net50.SystemThreadingTasks;
        public static PortableExecutableReference SystemCollections { get; } = Net50.SystemCollections;
        public static PortableExecutableReference SystemRuntimeInteropServices { get; } = Net50.SystemRuntimeInteropServices;
        public static PortableExecutableReference MicrosoftCSharp { get; } = Net50.MicrosoftCSharp;
        public static PortableExecutableReference MicrosoftVisualBasic { get; } = Net50.MicrosoftVisualBasic;
    }

    /// <summary>
    /// This type holds the reference information for the latest .NET Framework. These should be
    /// used by tests that are specific to .NET Framework. This moves forward much more rarely but
    /// when it does move forward these will change
    /// </summary>
    public static class NetFramework
    {
        public static ImmutableArray<MetadataReference> StandardReferences => ImmutableArray.Create<MetadataReference>(
            Net461.mscorlib,
            Net461.System,
            Net461.SystemCore,
            NetFx.ValueTuple.tuplelib,
            Net461.SystemRuntime);

        public static PortableExecutableReference mscorlib { get; } = Net461.mscorlib;
        public static PortableExecutableReference System { get; } = Net461.System;
        public static PortableExecutableReference SystemRuntime { get; } = Net461.SystemRuntime;
        public static PortableExecutableReference SystemCore { get; } = Net461.SystemCore;
        public static PortableExecutableReference SystemThreadingTasks { get; } = Net461.SystemThreadingTasks;
        public static PortableExecutableReference MicrosoftCSharp { get; } = Net461.MicrosoftCSharp;
        public static PortableExecutableReference MicrosoftVisualBasic { get; } = Net461.MicrosoftVisualBasic;
    }

    public static class TargetFrameworkUtil
    {
        public static ImmutableArray<MetadataReference> StandardLatestReferences => RuntimeUtilities.IsCoreClrRuntime ? NetCoreApp.StandardReferences : NetFramework.StandardReferences;
        public static ImmutableArray<MetadataReference> StandardReferences => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20References : NetFramework.StandardReferences;
        public static MetadataReference StandardCSharpReference => RuntimeUtilities.IsCoreClrRuntime ? MicrosoftCSharp.Netstandard13Lib : NetFramework.MicrosoftCSharp;
        public static MetadataReference StandardVisualBasicReference => RuntimeUtilities.IsCoreClrRuntime ? MicrosoftVisualBasic.Netstandard11 : NetFramework.MicrosoftVisualBasic;
        public static ImmutableArray<MetadataReference> StandardAndCSharpReferences => StandardReferences.Add(StandardCSharpReference);
        public static ImmutableArray<MetadataReference> StandardAndVBRuntimeReferences => StandardReferences.Add(StandardVisualBasicReference);

        /*
         * ⚠ Dev note ⚠: properties in TestBase are backed by Lazy<T>. Avoid changes to the following properties
         * which would force the initialization of these properties in the static constructor, since the stack traces
         * for a TypeLoadException are missing important information for resolving problems if/when they occur.
         * https://github.com/dotnet/roslyn/issues/25961
         */

        public static ImmutableArray<MetadataReference> Mscorlib40References => ImmutableArray.Create<MetadataReference>(Net40.mscorlib);
        public static ImmutableArray<MetadataReference> Mscorlib40ExtendedReferences => ImmutableArray.Create<MetadataReference>(Net40.mscorlib, Net40.System, Net40.SystemCore);
        public static ImmutableArray<MetadataReference> Mscorlib40andSystemCoreReferences => ImmutableArray.Create<MetadataReference>(Net40.mscorlib, Net40.SystemCore);
        public static ImmutableArray<MetadataReference> Mscorlib40andVBRuntimeReferences => ImmutableArray.Create<MetadataReference>(Net40.mscorlib, Net40.System, Net40.MicrosoftVisualBasic);
        public static ImmutableArray<MetadataReference> Mscorlib45References => ImmutableArray.Create<MetadataReference>(Net451.mscorlib);
        public static ImmutableArray<MetadataReference> Mscorlib45ExtendedReferences => ImmutableArray.Create<MetadataReference>(Net451.mscorlib, Net451.System, Net451.SystemCore, TestBase.ValueTupleRef, Net451.SystemRuntime);
        public static ImmutableArray<MetadataReference> Mscorlib45AndCSharpReferences => ImmutableArray.Create<MetadataReference>(Net451.mscorlib, Net451.SystemCore, Net451.MicrosoftCSharp);
        public static ImmutableArray<MetadataReference> Mscorlib45AndVBRuntimeReferences => ImmutableArray.Create<MetadataReference>(Net451.mscorlib, Net451.System, Net451.MicrosoftVisualBasic);
        public static ImmutableArray<MetadataReference> Mscorlib46References => ImmutableArray.Create<MetadataReference>(Net461.mscorlib);
        public static ImmutableArray<MetadataReference> Mscorlib46ExtendedReferences => ImmutableArray.Create<MetadataReference>(Net461.mscorlib, Net461.System, TestMetadata.Net461.SystemCore, TestBase.ValueTupleRef, Net461.SystemRuntime);
        public static ImmutableArray<MetadataReference> Mscorlib461References => ImmutableArray.Create<MetadataReference>(Net461.mscorlib);
        public static ImmutableArray<MetadataReference> Mscorlib461ExtendedReferences => ImmutableArray.Create<MetadataReference>(Net461.mscorlib, Net461.System, Net461.SystemCore, NetFx.ValueTuple.tuplelib, Net461.SystemRuntime);
        public static ImmutableArray<MetadataReference> NetStandard20References => ImmutableArray.Create<MetadataReference>(NetStandard20.netstandard, NetStandard20.mscorlib, NetStandard20.SystemRuntime, NetStandard20.SystemCore, NetStandard20.SystemDynamicRuntime, NetStandard20.SystemLinq, NetStandard20.SystemLinqExpressions);
        public static ImmutableArray<MetadataReference> WinRTReferences => ImmutableArray.Create(TestBase.WinRtRefs);
        public static ImmutableArray<MetadataReference> DefaultVbReferences => ImmutableArray.Create<MetadataReference>(Net451.mscorlib, Net451.System, Net451.SystemCore, Net451.MicrosoftVisualBasic);
        public static ImmutableArray<MetadataReference> MinimalReferences => ImmutableArray.Create(TestBase.MinCorlibRef);
        public static ImmutableArray<MetadataReference> MinimalAsyncReferences => ImmutableArray.Create(TestBase.MinAsyncCorlibRef);

        public static ImmutableArray<MetadataReference> GetReferences(TargetFramework targetFramework) => targetFramework switch
        {
            // Primary
            TargetFramework.Empty => ImmutableArray<MetadataReference>.Empty,
            TargetFramework.NetStandard20 => NetStandard20References,
            TargetFramework.NetCoreApp or TargetFramework.Net50 => NetCoreApp.StandardReferences,
            TargetFramework.Net60 => ImmutableArray.CreateRange<MetadataReference>(Net60.All),
            TargetFramework.NetCoreAppAndCSharp => NetCoreApp.StandardReferences.Add(NetCoreApp.MicrosoftCSharp),
            TargetFramework.NetFramework => NetFramework.StandardReferences,

            // Legacy we should be phasing out
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
            TargetFramework.WinRT => WinRTReferences,
            TargetFramework.Standard => StandardReferences,
            TargetFramework.StandardAndCSharp => StandardAndCSharpReferences,
            TargetFramework.StandardAndVBRuntime => StandardAndVBRuntimeReferences,
            TargetFramework.DefaultVb => DefaultVbReferences,
            TargetFramework.Minimal => MinimalReferences,
            TargetFramework.MinimalAsync => MinimalAsyncReferences,
            TargetFramework.StandardLatest => StandardLatestReferences,
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
            // pass multiple versions of a DLL should manually construct the reference list and not use this helper.
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
