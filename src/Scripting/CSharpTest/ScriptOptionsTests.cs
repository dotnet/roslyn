// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests
{
    public class ScriptOptionsTests : TestBase
    {
        [Fact]
        public void WithLanguageVersion()
        {
            var options = ScriptOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);
            Assert.Equal(LanguageVersion.CSharp8, ((CSharpParseOptions)options.ParseOptions).LanguageVersion);
        }

        [Fact]
        public void WithLanguageVersion_SameValueTwice_DoesNotCreateNewInstance()
        {
            var options = ScriptOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);
            Assert.Same(options, options.WithLanguageVersion(LanguageVersion.CSharp8));
        }

        [Fact]
        public void WithLanguageVersion_NonCSharpParseOptions_Throws()
        {
            var options = ScriptOptions.Default.WithParseOptions(new VisualBasicParseOptions(kind: SourceCodeKind.Script, languageVersion: VisualBasic.LanguageVersion.Latest));
            Assert.Throws<InvalidOperationException>(() => options.WithLanguageVersion(LanguageVersion.CSharp8));
        }
    }
}
