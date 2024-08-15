// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Basic.Reference.Assemblies;
using static TestReferences;
using static Roslyn.Test.Utilities.TestMetadata;
using Microsoft.CodeAnalysis.CodeGen;
using System.Reflection;
using System.Collections.Concurrent;

namespace Roslyn.Test.Utilities
{
    public enum TargetFramework
    {
        /// <summary>
        /// Explicit pick a target framework that has no references
        /// </summary>
        Empty,

        NetStandard20,

        /// <summary>
        /// The latest .NET Core target framework
        /// </summary>
        NetCoreApp,

        /// <summary>
        /// The latest .NET Framework
        /// </summary>
        NetFramework,

        /// <summary>
        /// This will be <see cref="NetCoreApp" /> when running on .NET Core and <see cref="NetFramework"/>
        /// when running on .NET Framework.
        /// </summary>
        NetLatest,

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
        Net70,
        Net80,
        Net90,
    }

    /// <summary>
    /// This type holds the reference information for the latest .NET Core platform. Tests
    /// targeting .NET core specifically should use the references here. As the platform moves
    /// forward these will be moved to target the latest .NET Core supported by the compiler
    /// </summary>
    public static class NetCoreApp
    {
        public static ImmutableArray<Net70.ReferenceInfo> AllReferenceInfos { get; } = ImmutableArray.CreateRange(Net70.ReferenceInfos.All);
        public static ImmutableArray<MetadataReference> References { get; } = ImmutableArray.CreateRange<MetadataReference>(Net70.References.All);

        public static PortableExecutableReference netstandard { get; } = Net70.References.netstandard;
        public static PortableExecutableReference mscorlib { get; } = Net70.References.mscorlib;
        public static PortableExecutableReference SystemRuntime { get; } = Net70.References.SystemRuntime;
    }

    /// <summary>
    /// This type holds the reference information for the latest .NET Framework. These should be
    /// used by tests that are specific to .NET Framework. This moves forward much more rarely but
    /// when it does move forward these will change
    /// </summary>
    public static class NetFramework
    {
        /// <summary>
        /// This is the full set of references provided by default on the .NET Framework TFM
        /// </summary>
        /// <remarks>
        /// Need to special case tuples until we move to net472
        /// </remarks>
        public static ImmutableArray<MetadataReference> References { get; } =
            ImmutableArray
                .CreateRange<MetadataReference>(Net461.References.All)
                .Add(NetFx.ValueTuple.tuplelib);

        /// <summary>
        /// This is a limited set of references on this .NET Framework TFM. This should be avoided in new code 
        /// as it represents the way reference hookup used to work.
        /// </summary>
        /// <remarks>
        /// Need to special case tuples until we move to net472
        /// </remarks>
        public static ImmutableArray<MetadataReference> Standard { get; } =
            ImmutableArray.Create<MetadataReference>(
                Net461.References.mscorlib,
                Net461.References.System,
                Net461.References.SystemCore,
                Net461.References.SystemData,
                NetFx.ValueTuple.tuplelib,
                Net461.References.SystemRuntime);

        public static PortableExecutableReference mscorlib { get; } = Net461.References.mscorlib;
        public static PortableExecutableReference System { get; } = Net461.References.System;
        public static PortableExecutableReference SystemRuntime { get; } = Net461.References.SystemRuntime;
        public static PortableExecutableReference SystemCore { get; } = Net461.References.SystemCore;
        public static PortableExecutableReference SystemData { get; } = Net461.References.SystemData;
        public static PortableExecutableReference SystemThreadingTasks { get; } = Net461.References.SystemThreadingTasks;
        public static PortableExecutableReference SystemXml { get; } = Net461.References.SystemXml;
        public static PortableExecutableReference MicrosoftCSharp { get; } = Net461.References.MicrosoftCSharp;
        public static PortableExecutableReference MicrosoftVisualBasic { get; } = Net461.References.MicrosoftVisualBasic;
    }

    public static class TargetFrameworkUtil
    {
        private static readonly ConcurrentDictionary<string, ImmutableArray<PortableExecutableReference>> s_dynamicReferenceMap = new ConcurrentDictionary<string, ImmutableArray<PortableExecutableReference>>(StringComparer.Ordinal);

        public static ImmutableArray<MetadataReference> NetLatest => RuntimeUtilities.IsCoreClrRuntime ? NetCoreApp.References : NetFramework.References;
        public static ImmutableArray<MetadataReference> StandardReferences => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20References : NetFramework.Standard;
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
        public static ImmutableArray<MetadataReference> WinRTReferences =>
        [
            .. TestBase.WinRtRefs
        ];
        public static ImmutableArray<MetadataReference> MinimalReferences =>
        [
            TestBase.MinCorlibRef
        ];
        public static ImmutableArray<MetadataReference> MinimalAsyncReferences =>
        [
            TestBase.MinAsyncCorlibRef
        ];
        public static ImmutableArray<MetadataReference> Mscorlib45ExtendedReferences =>
        [
            NetFramework.mscorlib,
            NetFramework.System,
            NetFramework.SystemCore,
            TestBase.ValueTupleRef,
            NetFramework.SystemRuntime
        ];
        public static ImmutableArray<MetadataReference> Mscorlib46ExtendedReferences =>
        [
            Net461.References.mscorlib,
            Net461.References.System,
            Net461.References.SystemCore,
            TestBase.ValueTupleRef,
            Net461.References.SystemRuntime
        ];
        /*
         * ⚠ Dev note ⚠: TestBase properties end here.
         */

        public static ImmutableArray<MetadataReference> Mscorlib40References { get; } =
        [
            Net40.mscorlib
        ];
        public static ImmutableArray<MetadataReference> Mscorlib40ExtendedReferences { get; } =
        [
            Net40.mscorlib,
            Net40.System,
            Net40.SystemCore
        ];
        public static ImmutableArray<MetadataReference> Mscorlib40andSystemCoreReferences { get; } =
        [
            Net40.mscorlib,
            Net40.SystemCore
        ];
        public static ImmutableArray<MetadataReference> Mscorlib40andVBRuntimeReferences { get; } =
        [
            Net40.mscorlib,
            Net40.System,
            Net40.MicrosoftVisualBasic
        ];
        public static ImmutableArray<MetadataReference> Mscorlib45References { get; } =
        [
            NetFramework.mscorlib
        ];
        public static ImmutableArray<MetadataReference> Mscorlib45AndCSharpReferences { get; } =
        [
            NetFramework.mscorlib,
            NetFramework.SystemCore,
            NetFramework.MicrosoftCSharp
        ];
        public static ImmutableArray<MetadataReference> Mscorlib45AndVBRuntimeReferences { get; } =
        [
            NetFramework.mscorlib,
            NetFramework.System,
            NetFramework.MicrosoftVisualBasic
        ];
        public static ImmutableArray<MetadataReference> Mscorlib46References { get; } =
        [
            Net461.References.mscorlib
        ];
        public static ImmutableArray<MetadataReference> Mscorlib461References { get; } =
        [
            Net461.References.mscorlib
        ];
        public static ImmutableArray<MetadataReference> Mscorlib461ExtendedReferences { get; } =
        [
            Net461.References.mscorlib,
            Net461.References.System,
            Net461.References.SystemCore,
            NetFx.ValueTuple.tuplelib,
            Net461.References.SystemRuntime
        ];
        public static ImmutableArray<MetadataReference> NetStandard20References { get; } =
        [
            NetStandard20.References.netstandard,
            NetStandard20.References.mscorlib,
            NetStandard20.References.SystemRuntime,
            NetStandard20.References.SystemCore,
            NetStandard20.References.SystemDynamicRuntime,
            NetStandard20.References.SystemLinq,
            NetStandard20.References.SystemLinqExpressions
        ];
        public static ImmutableArray<MetadataReference> DefaultVbReferences { get; } =
        [
            NetFramework.mscorlib,
            NetFramework.System,
            NetFramework.SystemCore,
            NetFramework.MicrosoftVisualBasic
        ];

#if DEBUG

        static TargetFrameworkUtil()
        {
            // Asserts to ensure these two values keep in sync
            Debug.Assert(GetReferences(TargetFramework.NetCoreApp).SequenceEqual(NetCoreApp.References));
            Debug.Assert(GetReferences(TargetFramework.NetFramework).SequenceEqual(NetFramework.References));
        }

#endif

        public static ImmutableArray<MetadataReference> GetReferences(TargetFramework targetFramework) => targetFramework switch
        {
            // Primary
            // Note: NetCoreApp should behave like latest Core TFM
            TargetFramework.Empty => ImmutableArray<MetadataReference>.Empty,
            TargetFramework.NetStandard20 => NetStandard20References,
            TargetFramework.Net50 => ImmutableArray.CreateRange<MetadataReference>(LoadDynamicReferences("Net50")),
            TargetFramework.Net60 => ImmutableArray.CreateRange<MetadataReference>(LoadDynamicReferences("Net60")),
            TargetFramework.NetCoreApp or TargetFramework.Net70 => ImmutableArray.CreateRange<MetadataReference>(Net70.References.All),
            TargetFramework.Net80 => ImmutableArray.CreateRange<MetadataReference>(LoadDynamicReferences("Net80")),
            TargetFramework.Net90 => ImmutableArray.CreateRange<MetadataReference>(LoadDynamicReferences("Net90")),
            TargetFramework.NetFramework => NetFramework.References,
            TargetFramework.NetLatest => NetLatest,
            TargetFramework.Standard => StandardReferences,

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
            TargetFramework.StandardAndCSharp => StandardAndCSharpReferences,
            TargetFramework.StandardAndVBRuntime => StandardAndVBRuntimeReferences,
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

        public static IEnumerable<MetadataReference> GetReferencesWithout(TargetFramework targetFramework, params string[] excludeReferenceNames) =>
            GetReferences(targetFramework)
            .Where(x => !(x is PortableExecutableReference pe && excludeReferenceNames.Contains(pe.FilePath)));

        /// <summary>
        /// Many of our reference assemblies are only used by a subset of compiler unit tests. Having a PackageReference
        /// to the assemblies here would cause them to be deployed to every unit test we write though. These are non-trivial 
        /// in size (4+ MB) and we have ~50 test projects so this adds up fast. To keep size down we just add 
        /// PackageReference on the few projects that and dynamically load here.
        /// </summary>
        private static ImmutableArray<PortableExecutableReference> LoadDynamicReferences(string targetFrameworkName)
        {
            var assemblyName = $"Basic.Reference.Assemblies.{targetFrameworkName}";
            if (s_dynamicReferenceMap.TryGetValue(assemblyName, out var references))
            {
                return references;
            }

            try
            {
                var name = new AssemblyName(assemblyName);
                var assembly = Assembly.Load(name);

                var type = assembly.GetType(assemblyName, throwOnError: true);
                var prop = type.GetProperty("All", BindingFlags.Public | BindingFlags.Static);
                if (prop is null)
                {
                    type = assembly.GetType(assemblyName + "+References", throwOnError: true);
                    prop = type.GetProperty("All", BindingFlags.Public | BindingFlags.Static);
                }
                var obj = prop.GetGetMethod()!.Invoke(obj: null, parameters: null);
                references = ((IEnumerable<PortableExecutableReference>)obj).ToImmutableArray();

                // This method can de called in parallel. Who wins this TryAdd isn't important, it's the same 
                // values. 
                _ = s_dynamicReferenceMap.TryAdd(assemblyName, references);
                return references;
            }
            catch (Exception ex)
            {
                var message = $"Error loading {assemblyName}. Make sure the test project has a <PackageReference> for this assembly";
                throw new Exception(message, ex);
            }
        }
    }
}
