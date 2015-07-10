// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public sealed class CSharpCommandLineParserServiceTests
    {
        private static readonly string s_directory = Path.GetTempPath();
        private readonly CSharpCommandLineParserService _parser = new CSharpCommandLineParserService();

        private CSharpCommandLineArguments GetArguments(params string[] args)
        {
            return (CSharpCommandLineArguments)_parser.Parse(args, baseDirectory: s_directory, isInteractive: false, sdkDirectory: s_directory);
        }

        private CSharpParseOptions GetParseOptions(params string[] args)
        {
            return GetArguments(args).ParseOptions;
        }

        [Fact]
        public void FeaturesSingle()
        {
            var options = GetParseOptions("/features:test");
            Assert.Equal("true", options.Features["test"]);
        }

        [Fact]
        public void FeaturesSingleWithValue()
        {
            var options = GetParseOptions("/features:test=dog");
            Assert.Equal("dog", options.Features["test"]);
        }

        [Fact]
        public void FeaturesMultiple()
        {
            var options = GetParseOptions("/features:test1", "/features:test2");
            Assert.Equal("true", options.Features["test1"]);
            Assert.Equal("true", options.Features["test2"]);
        }

        [Fact]
        public void FeaturesMultipleWithValue()
        {
            var options = GetParseOptions("/features:test1=dog", "/features:test2=cat");
            Assert.Equal("dog", options.Features["test1"]);
            Assert.Equal("cat", options.Features["test2"]);
        }
    }
}
