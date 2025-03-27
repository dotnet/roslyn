// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    public class CSharpParseOptionsTests : CSharpTestBase
    {
        private void TestProperty<T>(Func<CSharpParseOptions, T, CSharpParseOptions> factory, Func<CSharpParseOptions, T> getter, T validValue)
        {
            var oldOpt1 = CSharpParseOptions.Default;
            var newOpt1 = factory(oldOpt1, validValue);
            var newOpt2 = factory(newOpt1, validValue);

            Assert.Equal(validValue, getter(newOpt1));
            Assert.Same(newOpt2, newOpt1);
        }

        [Fact]
        [WorkItem(15358, "https://github.com/dotnet/roslyn/issues/15358")]
        public void WithDocumentationModeDoesntChangeFeatures()
        {
            var kvp = new KeyValuePair<string, string>("IOperation", "true");
            var po = new CSharpParseOptions().WithFeatures(new[] { kvp });
            Assert.Equal(po.Features.AsSingleton(), kvp);
            var po2 = po.WithDocumentationMode(DocumentationMode.Diagnose);
            Assert.Equal(po2.Features.AsSingleton(), kvp);
        }

        [Fact]
        public void WithXxx()
        {
            TestProperty((old, value) => old.WithKind(value), opt => opt.Kind, SourceCodeKind.Script);
            TestProperty((old, value) => old.WithLanguageVersion(value), opt => opt.LanguageVersion, LanguageVersion.CSharp3);
            TestProperty((old, value) => old.WithDocumentationMode(value), opt => opt.DocumentationMode, DocumentationMode.None);
            TestProperty((old, value) => old.WithPreprocessorSymbols(value), opt => opt.PreprocessorSymbols, ImmutableArray.Create<string>("A", "B", "C"));

            Assert.Equal(0, CSharpParseOptions.Default.WithPreprocessorSymbols(ImmutableArray.Create<string>("A", "B")).WithPreprocessorSymbols(default(ImmutableArray<string>)).PreprocessorSymbols.Length);
            Assert.Equal(0, CSharpParseOptions.Default.WithPreprocessorSymbols(ImmutableArray.Create<string>("A", "B")).WithPreprocessorSymbols((IEnumerable<string>)null).PreprocessorSymbols.Length);
            Assert.Equal(0, CSharpParseOptions.Default.WithPreprocessorSymbols(ImmutableArray.Create<string>("A", "B")).WithPreprocessorSymbols((string[])null).PreprocessorSymbols.Length);
        }

        /// <summary>
        /// If this test fails, please update the <see cref="CSharpParseOptions.GetHashCode"/>
        /// and <see cref="CSharpParseOptions.Equals(CSharpParseOptions)"/> methods to
        /// make sure they are doing the right thing with your new field and then update the baseline
        /// here.
        /// </summary>
        [Fact]
        public void TestFieldsForEqualsAndGetHashCode()
        {
            ReflectionAssert.AssertPublicAndInternalFieldsAndProperties(
                typeof(CSharpParseOptions),
                "Features",
                "Language",
                "LanguageVersion",
                "InterceptorsNamespaces",
                "PreprocessorSymbolNames",
                "PreprocessorSymbols",
                "SpecifiedLanguageVersion");
        }

        [Fact]
        public void SpecifiedKindIsMappedCorrectly()
        {
            var options = new CSharpParseOptions();
            Assert.Equal(SourceCodeKind.Regular, options.Kind);
            Assert.Equal(SourceCodeKind.Regular, options.SpecifiedKind);

            options.Errors.Verify();

            options = new CSharpParseOptions(kind: SourceCodeKind.Regular);
            Assert.Equal(SourceCodeKind.Regular, options.Kind);
            Assert.Equal(SourceCodeKind.Regular, options.SpecifiedKind);

            options.Errors.Verify();

            options = new CSharpParseOptions(kind: SourceCodeKind.Script);
            Assert.Equal(SourceCodeKind.Script, options.Kind);
            Assert.Equal(SourceCodeKind.Script, options.SpecifiedKind);

            options.Errors.Verify();

#pragma warning disable CS0618 // SourceCodeKind.Interactive is obsolete
            options = new CSharpParseOptions(kind: SourceCodeKind.Interactive);
            Assert.Equal(SourceCodeKind.Script, options.Kind);
            Assert.Equal(SourceCodeKind.Interactive, options.SpecifiedKind);
#pragma warning restore CS0618 // SourceCodeKind.Interactive is obsolete

            options.Errors.Verify(
                // error CS8190: Provided source code kind is unsupported or invalid: 'Interactive'.
                Diagnostic(ErrorCode.ERR_BadSourceCodeKind).WithArguments("Interactive").WithLocation(1, 1));

            options = new CSharpParseOptions(kind: (SourceCodeKind)int.MinValue);
            Assert.Equal(SourceCodeKind.Regular, options.Kind);
            Assert.Equal((SourceCodeKind)int.MinValue, options.SpecifiedKind);

            options.Errors.Verify(
                // warning CS8190: Provided source code kind is unsupported or invalid: '-2147483648'
                Diagnostic(ErrorCode.ERR_BadSourceCodeKind).WithArguments("-2147483648").WithLocation(1, 1));
        }

        [Fact]
        public void TwoOptionsWithDifferentSpecifiedKindShouldNotHaveTheSameHashCodes()
        {
            var options1 = new CSharpParseOptions(kind: SourceCodeKind.Script);
            var options2 = new CSharpParseOptions(kind: SourceCodeKind.Script);

            Assert.Equal(options1.GetHashCode(), options2.GetHashCode());

            // They both map internally to SourceCodeKind.Script
#pragma warning disable CS0618 // SourceCodeKind.Interactive is obsolete
            options1 = new CSharpParseOptions(kind: SourceCodeKind.Script);
            options2 = new CSharpParseOptions(kind: SourceCodeKind.Interactive);
#pragma warning restore CS0618 // SourceCodeKind.Interactive is obsolete

            Assert.NotEqual(options1.GetHashCode(), options2.GetHashCode());
        }

        [Fact]
        public void TwoOptionsWithDifferentSpecifiedKindShouldNotBeEqual()
        {
            var options1 = new CSharpParseOptions(kind: SourceCodeKind.Script);
            var options2 = new CSharpParseOptions(kind: SourceCodeKind.Script);

            Assert.True(options1.Equals(options2));

            // They both map internally to SourceCodeKind.Script
#pragma warning disable CS0618 // SourceCodeKind.Interactive is obsolete
            options1 = new CSharpParseOptions(kind: SourceCodeKind.Script);
            options2 = new CSharpParseOptions(kind: SourceCodeKind.Interactive);
#pragma warning restore CS0618 // SourceCodeKind.Interactive is obsolete

            Assert.False(options1.Equals(options2));
        }

        [Fact]
        public void BadSourceCodeKindShouldProduceDiagnostics()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var options = new CSharpParseOptions(kind: SourceCodeKind.Interactive);
#pragma warning restore CS0618 // Type or member is obsolete

            options.Errors.Verify(
                // error CS8190: Provided source code kind is unsupported or invalid: 'Interactive'.
                Diagnostic(ErrorCode.ERR_BadSourceCodeKind).WithArguments("Interactive").WithLocation(1, 1));
        }

        [Fact]
        public void BadDocumentationModeShouldProduceDiagnostics()
        {
            var options = new CSharpParseOptions(documentationMode: unchecked((DocumentationMode)100));

            options.Errors.Verify(
                // error CS8191: Provided documentation mode is unsupported or invalid: '100'.
                Diagnostic(ErrorCode.ERR_BadDocumentationMode).WithArguments("100").WithLocation(1, 1));
        }

        [Fact]
        public void BadLanguageVersionShouldProduceDiagnostics()
        {
            var options = new CSharpParseOptions(languageVersion: unchecked((LanguageVersion)10000));

            options.Errors.Verify(
                // error CS8191: Provided language version is unsupported or invalid: '10000'.
                Diagnostic(ErrorCode.ERR_BadLanguageVersion).WithArguments("10000").WithLocation(1, 1));
        }

        [Fact]
        public void BadPreProcessorSymbolsShouldProduceDiagnostics()
        {
            var options = new CSharpParseOptions(preprocessorSymbols: new[] { "test", "1" });

            options.Errors.Verify(
                // error CS8301: Invalid name for a preprocessing symbol; '1' is not a valid identifier
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol).WithArguments("1").WithLocation(1, 1));
        }

        [Fact]
        public void BadSourceCodeKindShouldProduceDiagnostics_WithVariation()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var options = new CSharpParseOptions().WithKind(SourceCodeKind.Interactive);
#pragma warning restore CS0618 // Type or member is obsolete

            options.Errors.Verify(
                // error CS8190: Provided source code kind is unsupported or invalid: 'Interactive'.
                Diagnostic(ErrorCode.ERR_BadSourceCodeKind).WithArguments("Interactive").WithLocation(1, 1));
        }

        [Fact]
        public void BadDocumentationModeShouldProduceDiagnostics_WithVariation()
        {
            var options = new CSharpParseOptions().WithDocumentationMode(unchecked((DocumentationMode)100));

            options.Errors.Verify(
                // error CS8191: Provided documentation mode is unsupported or invalid: '100'.
                Diagnostic(ErrorCode.ERR_BadDocumentationMode).WithArguments("100").WithLocation(1, 1));
        }

        [Fact]
        public void BadLanguageVersionShouldProduceDiagnostics_WithVariation()
        {
            var options = new CSharpParseOptions().WithLanguageVersion(unchecked((LanguageVersion)10000));

            options.Errors.Verify(
                // error CS8191: Provided language version is unsupported or invalid: '10000'.
                Diagnostic(ErrorCode.ERR_BadLanguageVersion).WithArguments("10000").WithLocation(1, 1));
        }

        [Fact]
        public void BadPreProcessorSymbolsShouldProduceDiagnostics_EmptyString()
        {
            var options = new CSharpParseOptions().WithPreprocessorSymbols(new[] { "" });

            options.Errors.Verify(
                // error CS8301: Invalid name for a preprocessing symbol; '' is not a valid identifier
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol).WithArguments("").WithLocation(1, 1));
        }

        [Fact]
        public void BadPreProcessorSymbolsShouldProduceDiagnostics_WhiteSpaceString()
        {
            var options = new CSharpParseOptions().WithPreprocessorSymbols(new[] { " " });

            options.Errors.Verify(
                // error CS8301: Invalid name for a preprocessing symbol; ' ' is not a valid identifier
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol).WithArguments(" ").WithLocation(1, 1));
        }

        [Fact]
        public void BadPreProcessorSymbolsShouldProduceDiagnostics_SymbolWithDots()
        {
            var options = new CSharpParseOptions().WithPreprocessorSymbols(new[] { "Good", "Bad.Symbol" });

            options.Errors.Verify(
                // error CS8301: Invalid name for a preprocessing symbol; 'Bad.Symbol' is not a valid identifier
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol).WithArguments("Bad.Symbol").WithLocation(1, 1));
        }

        [Fact]
        public void BadPreProcessorSymbolsShouldProduceDiagnostics_SymbolWithSlashes()
        {
            var options = new CSharpParseOptions().WithPreprocessorSymbols(new[] { "Good", "Bad\\Symbol" });

            options.Errors.Verify(
                // error CS8301: Invalid name for a preprocessing symbol; 'Bad\Symbol' is not a valid identifier
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol).WithArguments("Bad\\Symbol").WithLocation(1, 1));
        }

        [Fact]
        public void BadPreProcessorSymbolsShouldProduceDiagnostics_NullSymbol()
        {
            var options = new CSharpParseOptions().WithPreprocessorSymbols(new[] { "Good", null });

            options.Errors.Verify(
                // error CS8301: Invalid name for a preprocessing symbol; 'null' is not a valid identifier
                Diagnostic(ErrorCode.ERR_InvalidPreprocessingSymbol).WithArguments("null").WithLocation(1, 1));
        }
    }
}
