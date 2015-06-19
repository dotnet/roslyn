// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class CSharpFormattingTestBase : FormattingTestBase
    {
        protected static readonly Workspace DefaultWorkspace = new AdhocWorkspace();

        protected override SyntaxNode ParseCompilation(string text, ParseOptions parseOptions)
        {
            return SyntaxFactory.ParseCompilationUnit(text, options: (CSharpParseOptions)parseOptions);
        }

        protected void AssertFormat(
            string expected,
            string code,
            bool debugMode = false,
            Dictionary<OptionKey, object> changedOptionSet = null,
            bool testWithTransformation = true,
            ParseOptions parseOptions = null)
        {
            AssertFormat(expected, code, SpecializedCollections.SingletonEnumerable(new TextSpan(0, code.Length)), debugMode, changedOptionSet, testWithTransformation, parseOptions);
        }

        protected void AssertFormat(
            string expected,
            string code,
            IEnumerable<TextSpan> spans,
            bool debugMode = false,
            Dictionary<OptionKey, object> changedOptionSet = null,
            bool testWithTransformation = true,
            ParseOptions parseOptions = null)
        {
            AssertFormat(expected, code, spans, LanguageNames.CSharp, debugMode, changedOptionSet, testWithTransformation, parseOptions);
        }
    }
}
