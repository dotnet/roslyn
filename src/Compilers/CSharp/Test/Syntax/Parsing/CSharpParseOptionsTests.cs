// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
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
        public void WithXxx()
        {
            TestProperty((old, value) => old.WithKind(value), opt => opt.Kind, SourceCodeKind.Script);
            TestProperty((old, value) => old.WithLanguageVersion(value), opt => opt.LanguageVersion, LanguageVersion.CSharp3);
            TestProperty((old, value) => old.WithDocumentationMode(value), opt => opt.DocumentationMode, DocumentationMode.None);
            TestProperty((old, value) => old.WithPreprocessorSymbols(value), opt => opt.PreprocessorSymbols, ImmutableArray.Create<string>("A", "B", "C"));

            Assert.Throws<ArgumentOutOfRangeException>(() => CSharpParseOptions.Default.WithKind((SourceCodeKind)Int32.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => CSharpParseOptions.Default.WithLanguageVersion((LanguageVersion)1000));

            Assert.Equal(0, CSharpParseOptions.Default.WithPreprocessorSymbols(ImmutableArray.Create<string>("A", "B")).WithPreprocessorSymbols(default(ImmutableArray<string>)).PreprocessorSymbols.Length);
            Assert.Equal(0, CSharpParseOptions.Default.WithPreprocessorSymbols(ImmutableArray.Create<string>("A", "B")).WithPreprocessorSymbols((IEnumerable<string>)null).PreprocessorSymbols.Length);
            Assert.Equal(0, CSharpParseOptions.Default.WithPreprocessorSymbols(ImmutableArray.Create<string>("A", "B")).WithPreprocessorSymbols((string[])null).PreprocessorSymbols.Length);
        }

        [Fact]
        public void ConstructorValidation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CSharpParseOptions(kind: (SourceCodeKind)Int32.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CSharpParseOptions(languageVersion: (LanguageVersion)1000));
        }

        [Fact(), WorkItem(546206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546206")]
        public void InvalidDefineSymbols()
        {
            // command line gives CS2029: Invalid value for '/define'; 'xxx' is not a valid identifier
            Assert.Throws<ArgumentException>(() => new CSharpParseOptions(preprocessorSymbols: ImmutableArray.Create<string>("")));
            Assert.Throws<ArgumentException>(() => new CSharpParseOptions(preprocessorSymbols: ImmutableArray.Create<string>(" ")));
            Assert.Throws<ArgumentException>(() => new CSharpParseOptions(preprocessorSymbols: ImmutableArray.Create<string>("Good", "Bad.Symbol")));
            Assert.Throws<ArgumentException>(() => new CSharpParseOptions(preprocessorSymbols: ImmutableArray.Create<string>("123", "Good")));
            Assert.Throws<ArgumentException>(() => new CSharpParseOptions(preprocessorSymbols: ImmutableArray.Create<string>("Good", null, @"Bad\Symbol")));
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
                "LanguageVersion",
                "PreprocessorSymbolNames",
                "PreprocessorSymbols");
        }
    }
}
