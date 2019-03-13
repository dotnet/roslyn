// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Linq;
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
        WinRT,

        /// <summary>
        /// Eventually this will be deleted and replaced with NetStandard20. Short term this creates the "standard"
        /// API set across destkop and coreclr 
        /// </summary>
        Standard,
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
        public static ImmutableArray<MetadataReference> Mscorlib461ExtendedReferences => ImmutableArray.Create<MetadataReference>(Net461.mscorlibRef, Net461.SystemRef, Net461.SystemCoreRef, Net461.SystemValueTupleRef, Net461.SystemRuntimeRef);
        public static ImmutableArray<MetadataReference> NetStandard20References => ImmutableArray.Create<MetadataReference>(NetStandard20.NetStandard, NetStandard20.MscorlibRef, NetStandard20.SystemRuntimeRef, NetStandard20.SystemCoreRef, NetStandard20.SystemDynamicRuntimeRef);
        public static ImmutableArray<MetadataReference> WinRTReferences => ImmutableArray.Create(TestBase.WinRtRefs);
        public static ImmutableArray<MetadataReference> StandardReferences => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20References : Mscorlib46ExtendedReferences;
        public static ImmutableArray<MetadataReference> StandardAndCSharpReferences => StandardReferences.Add(StandardCSharpReference);
        public static ImmutableArray<MetadataReference> StandardAndVBRuntimeReferences => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20References.Add(NetStandard20.MicrosoftVisualBasicRef) : Mscorlib46ExtendedReferences.Add(TestBase.MsvbRef_v4_0_30319_17929);
        public static ImmutableArray<MetadataReference> StandardCompatReferences => RuntimeUtilities.IsCoreClrRuntime ? NetStandard20References : Mscorlib40References;
        public static ImmutableArray<MetadataReference> DefaultVbReferencs => ImmutableArray.Create(TestBase.MscorlibRef, TestBase.SystemRef, TestBase.SystemCoreRef, TestBase.MsvbRef);

        public static ImmutableArray<MetadataReference> GetReferences(TargetFramework tf)
        {
            switch (tf)
            {
                case TargetFramework.Empty: return ImmutableArray<MetadataReference>.Empty;
                case TargetFramework.Mscorlib40: return Mscorlib40References;
                case TargetFramework.Mscorlib40Extended: return Mscorlib40ExtendedReferences;
                case TargetFramework.Mscorlib40AndSystemCore: return Mscorlib40andSystemCoreReferences;
                case TargetFramework.Mscorlib40AndVBRuntime: return Mscorlib40andVBRuntimeReferences;
                case TargetFramework.Mscorlib45: return Mscorlib45References;
                case TargetFramework.Mscorlib45Extended: return Mscorlib45ExtendedReferences;
                case TargetFramework.Mscorlib45AndCSharp: return Mscorlib45AndCSharpReferences;
                case TargetFramework.Mscorlib45AndVBRuntime: return Mscorlib45AndVBRuntimeReferences;
                case TargetFramework.Mscorlib46: return Mscorlib46References;
                case TargetFramework.Mscorlib46Extended: return Mscorlib46ExtendedReferences;
                case TargetFramework.Mscorlib461: return Mscorlib46References;
                case TargetFramework.Mscorlib461Extended: return Mscorlib461ExtendedReferences;
                case TargetFramework.NetStandard20: return NetStandard20References;
                case TargetFramework.WinRT: return WinRTReferences;
                case TargetFramework.Standard: return StandardReferences;
                case TargetFramework.StandardAndCSharp: return StandardAndCSharpReferences;
                case TargetFramework.StandardAndVBRuntime: return StandardAndVBRuntimeReferences;
                case TargetFramework.StandardCompat: return StandardCompatReferences;
                case TargetFramework.DefaultVb: return DefaultVbReferencs;
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

            string getName(MetadataReference m) => GetAssemblyIdentity(m)?.Name;
        }

        /// <summary>
        /// Verification only succeeds when the cor library is named exactly mscorlib. This function will calculate the 
        /// best default for <see cref="Verification"/> based on the framework / references that we are targeting. If there
        /// is no mscorlib then the assumption is that verification will fail.
        /// </summary>
        public static Verification GetVerification(TargetFramework tf, IEnumerable<MetadataReference> additionalReferences)
        {
            switch (tf)
            {
                case TargetFramework.Empty:
                    return additionalReferences.Any(x => GetAssemblyIdentity(x)?.Name == "mscorlib")
                        ? Verification.Passes
                        : Verification.Fails;
                case TargetFramework.Mscorlib40:
                case TargetFramework.Mscorlib40Extended:
                case TargetFramework.Mscorlib40AndSystemCore:
                case TargetFramework.Mscorlib40AndVBRuntime:
                case TargetFramework.Mscorlib45:
                case TargetFramework.Mscorlib45Extended:
                case TargetFramework.Mscorlib45AndCSharp:
                case TargetFramework.Mscorlib45AndVBRuntime:
                case TargetFramework.Mscorlib46:
                case TargetFramework.Mscorlib46Extended:
                case TargetFramework.Mscorlib461:
                case TargetFramework.Mscorlib461Extended:
                case TargetFramework.WinRT:
                case TargetFramework.DefaultVb:
                    // Verification is fully supported on desktop and hence these should pass unless explicitly marked
                    // otherwise.
                    return Verification.Passes;
                case TargetFramework.NetStandard20:
                    // On CoreCLR and NetStandard PEVerify will often fail because it's not supported. Hence it is 
                    // skipped.
                    return Verification.Skipped;
                case TargetFramework.Standard:
                case TargetFramework.StandardAndCSharp:
                case TargetFramework.StandardAndVBRuntime:
                case TargetFramework.StandardCompat:
                    return RuntimeUtilities.IsDesktopRuntime ? Verification.Passes : Verification.Skipped;
                default: throw new InvalidOperationException($"Unexpected target framework {tf}");
            }
        }

        private static AssemblyIdentity GetAssemblyIdentity(MetadataReference m)
        {
            if (m is PortableExecutableReference p &&
                p.GetMetadata() is AssemblyMetadata assemblyMetadata)
            {
                try
                {
                    return assemblyMetadata.GetAssembly().Identity;
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
