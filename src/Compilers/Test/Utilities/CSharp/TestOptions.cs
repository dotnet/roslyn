// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public static class TestOptions
    {
        public static readonly CSharpParseOptions Regular = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);
        public static readonly CSharpParseOptions Script = Regular.WithKind(SourceCodeKind.Script);
        public static readonly CSharpParseOptions Regular1 = Regular.WithLanguageVersion(LanguageVersion.CSharp1);
        public static readonly CSharpParseOptions Regular2 = Regular.WithLanguageVersion(LanguageVersion.CSharp2);
        public static readonly CSharpParseOptions Regular3 = Regular.WithLanguageVersion(LanguageVersion.CSharp3);
        public static readonly CSharpParseOptions Regular4 = Regular.WithLanguageVersion(LanguageVersion.CSharp4);
        public static readonly CSharpParseOptions Regular5 = Regular.WithLanguageVersion(LanguageVersion.CSharp5);
        public static readonly CSharpParseOptions Regular6 = Regular.WithLanguageVersion(LanguageVersion.CSharp6);
        public static readonly CSharpParseOptions Regular7 = Regular.WithLanguageVersion(LanguageVersion.CSharp7);
        public static readonly CSharpParseOptions Regular7_1 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_1);
        public static readonly CSharpParseOptions Regular7_2 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_2);
        public static readonly CSharpParseOptions Regular7_3 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_3);
        public static readonly CSharpParseOptions RegularDefault = Regular.WithLanguageVersion(LanguageVersion.Default);

        /// <summary>
        /// Usages of <see cref="TestOptions.RegularNext"/> and <see cref="LanguageVersionFacts.CSharpNext"/>
        /// will be replaced with TestOptions.RegularN and LanguageVersion.CSharpN when language version N is introduced.
        /// </summary>
        public static readonly CSharpParseOptions RegularNext = Regular.WithLanguageVersion(LanguageVersion.Preview);

        public static readonly CSharpParseOptions RegularPreview = Regular.WithLanguageVersion(LanguageVersion.Preview);
        public static readonly CSharpParseOptions Regular8 = Regular.WithLanguageVersion(LanguageVersion.CSharp8);
        public static readonly CSharpParseOptions Regular9 = Regular.WithLanguageVersion(LanguageVersion.CSharp9);
        public static readonly CSharpParseOptions Regular10 = Regular.WithLanguageVersion(LanguageVersion.CSharp10);
        public static readonly CSharpParseOptions Regular11 = Regular.WithLanguageVersion(LanguageVersion.CSharp11);
        public static readonly CSharpParseOptions Regular12 = Regular.WithLanguageVersion(LanguageVersion.CSharp12);
        public static readonly CSharpParseOptions Regular13 = Regular.WithLanguageVersion(LanguageVersion.CSharp13);
        public static readonly CSharpParseOptions Regular14 = Regular.WithLanguageVersion(LanguageVersion.CSharp14);
        public static readonly CSharpParseOptions RegularWithDocumentationComments = Regular.WithDocumentationMode(DocumentationMode.Diagnose);
        public static readonly CSharpParseOptions RegularPreviewWithDocumentationComments = RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose);
        public static readonly CSharpParseOptions RegularWithLegacyStrongName = Regular.WithFeature("UseLegacyStrongNameProvider");
        public static readonly CSharpParseOptions WithoutImprovedOverloadCandidates = Regular.WithLanguageVersion(MessageID.IDS_FeatureImprovedOverloadCandidates.RequiredVersion() - 1);
        public static readonly CSharpParseOptions WithCovariantReturns = Regular.WithLanguageVersion(MessageID.IDS_FeatureCovariantReturnsForOverrides.RequiredVersion());
        public static readonly CSharpParseOptions WithoutCovariantReturns = Regular.WithLanguageVersion(LanguageVersion.CSharp8);

        public static readonly CSharpParseOptions RegularWithExtendedPartialMethods = RegularPreview;
        public static readonly CSharpParseOptions RegularWithFileScopedNamespaces = Regular.WithLanguageVersion(MessageID.IDS_FeatureFileScopedNamespace.RequiredVersion());

        private static readonly SmallDictionary<string, string> s_experimentalFeatures = new SmallDictionary<string, string> { };
        public static readonly CSharpParseOptions ExperimentalParseOptions =
            new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.None, languageVersion: LanguageVersion.Preview).WithFeatures(s_experimentalFeatures);

        // Enable pattern-switch translation even for switches that use no new syntax. This is used
        // to help ensure compatibility of the semantics of the new switch binder with the old switch
        // binder, so that we may eliminate the old one in the future.
        public static readonly CSharpParseOptions Regular6WithV7SwitchBinder = Regular6.WithFeatures(new Dictionary<string, string>() { { "testV7SwitchBinder", "true" } });

        public static readonly CSharpParseOptions RegularWithoutRecursivePatterns = Regular7_3;
        public static readonly CSharpParseOptions RegularWithRecursivePatterns = Regular8;
        public static readonly CSharpParseOptions RegularWithoutPatternCombinators = Regular8;
        public static readonly CSharpParseOptions RegularWithPatternCombinators = RegularPreview;
        public static readonly CSharpParseOptions RegularWithExtendedPropertyPatterns = RegularPreview;
        public static readonly CSharpParseOptions RegularWithListPatterns = RegularPreview;

        public static readonly CSharpCompilationOptions ReleaseDll = CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions ReleaseExe = CreateTestOptions(OutputKind.ConsoleApplication, OptimizationLevel.Release);

        public static readonly CSharpCompilationOptions ReleaseDebugDll = ReleaseDll.WithDebugPlusMode(true);

        public static readonly CSharpCompilationOptions ReleaseDebugExe = ReleaseExe.WithDebugPlusMode(true);

        public static readonly CSharpCompilationOptions DebugDll = CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, OptimizationLevel.Debug);
        public static readonly CSharpCompilationOptions DebugExe = CreateTestOptions(OutputKind.ConsoleApplication, OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions DebugDllThrowing = DebugDll.WithMetadataReferenceResolver(new ThrowingMetadataReferenceResolver());
        public static readonly CSharpCompilationOptions DebugExeThrowing = DebugExe.WithMetadataReferenceResolver(new ThrowingMetadataReferenceResolver());

        public static readonly CSharpCompilationOptions ReleaseWinMD = CreateTestOptions(OutputKind.WindowsRuntimeMetadata, OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions DebugWinMD = CreateTestOptions(OutputKind.WindowsRuntimeMetadata, OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions ReleaseModule = CreateTestOptions(OutputKind.NetModule, OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions DebugModule = CreateTestOptions(OutputKind.NetModule, OptimizationLevel.Debug);

        public static readonly CSharpCompilationOptions UnsafeReleaseDll = ReleaseDll.WithAllowUnsafe(true);
        public static readonly CSharpCompilationOptions UnsafeReleaseExe = ReleaseExe.WithAllowUnsafe(true);

        public static readonly CSharpCompilationOptions UnsafeDebugDll = DebugDll.WithAllowUnsafe(true);
        public static readonly CSharpCompilationOptions UnsafeDebugExe = DebugExe.WithAllowUnsafe(true);

        public static readonly CSharpCompilationOptions SigningReleaseDll = ReleaseDll.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningReleaseExe = ReleaseExe.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningReleaseModule = ReleaseModule.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);
        public static readonly CSharpCompilationOptions SigningDebugDll = DebugDll.WithStrongNameProvider(SigningTestHelpers.DefaultDesktopStrongNameProvider);

        public static readonly EmitOptions NativePdbEmit = EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Pdb);

        public static readonly GeneratorDriverOptions GeneratorDriverOptions = new GeneratorDriverOptions(trackIncrementalGeneratorSteps: true, baseDirectory: TempRoot.Root);

        public static CSharpParseOptions WithStrictFeature(this CSharpParseOptions options)
        {
            return options.WithFeatures(options.Features.Concat(new[] { new KeyValuePair<string, string>("strict", "true") }));
        }

        public static CSharpParseOptions WithPEVerifyCompatFeature(this CSharpParseOptions options)
        {
            return options.WithFeatures(options.Features.Concat(new[] { new KeyValuePair<string, string>("peverify-compat", "true") }));
        }

        public static CSharpParseOptions WithLocalFunctionsFeature(this CSharpParseOptions options)
        {
            return options;
        }

        public static CSharpParseOptions WithRefsFeature(this CSharpParseOptions options)
        {
            return options;
        }

        public static CSharpParseOptions WithTuplesFeature(this CSharpParseOptions options)
        {
            return options;
        }

        public static CSharpParseOptions WithNullablePublicOnly(this CSharpParseOptions options)
        {
            return options.WithFeature("nullablePublicOnly");
        }

        public static CSharpParseOptions WithNoRefSafetyRulesAttribute(this CSharpParseOptions options)
        {
            return options.WithFeature("noRefSafetyRulesAttribute");
        }

        public static CSharpParseOptions WithDisableLengthBasedSwitch(this CSharpParseOptions options)
        {
            return options.WithFeature("disable-length-based-switch");
        }

        public static CSharpParseOptions WithFeature(this CSharpParseOptions options, string feature, string value = "true")
        {
            return options.WithFeatures(options.Features.Concat(new[] { new KeyValuePair<string, string>(feature, value) }));
        }

        internal static CSharpParseOptions WithExperimental(this CSharpParseOptions options, params MessageID[] features)
        {
            if (features.Length == 0)
            {
                throw new InvalidOperationException("Need at least one feature to enable");
            }

            var list = new List<KeyValuePair<string, string>>();
            foreach (var feature in features)
            {
                var name = feature.RequiredFeature();
                if (name == null)
                {
                    throw new InvalidOperationException($"{feature} is not a valid experimental feature");
                }
                list.Add(new KeyValuePair<string, string>(name, "true"));
            }

            return options.WithFeatures(options.Features.Concat(list));
        }

        public static CSharpCompilationOptions WithSpecificDiagnosticOptions(this CSharpCompilationOptions options, string key, ReportDiagnostic value)
        {
            return options.WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty.Add(key, value));
        }

        public static CSharpCompilationOptions WithSpecificDiagnosticOptions(this CSharpCompilationOptions options, string key1, string key2, ReportDiagnostic value)
        {
            return options.WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty.Add(key1, value).Add(key2, value));
        }

        /// <summary>
        /// Create <see cref="CSharpCompilationOptions"/> with the maximum warning level.
        /// </summary>
        /// <param name="outputKind">The output kind of the created compilation options.</param>
        /// <param name="optimizationLevel">The optimization level of the created compilation options.</param>
        /// <param name="allowUnsafe">A boolean specifying whether to allow unsafe code. Defaults to false.</param>
        /// <returns>A CSharpCompilationOptions with the specified <paramref name="outputKind"/>, <paramref name="optimizationLevel"/>, and <paramref name="allowUnsafe"/>.</returns>
        internal static CSharpCompilationOptions CreateTestOptions(OutputKind outputKind, OptimizationLevel optimizationLevel, bool allowUnsafe = false)
            => new CSharpCompilationOptions(outputKind, optimizationLevel: optimizationLevel, warningLevel: Diagnostic.MaxWarningLevel, allowUnsafe: allowUnsafe);
    }
}

