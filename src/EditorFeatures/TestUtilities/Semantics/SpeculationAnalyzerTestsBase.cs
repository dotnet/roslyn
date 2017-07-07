﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Semantics
{
    public abstract class SpeculationAnalyzerTestsBase : TestBase
    {
        protected const string CompilationName = "SemanticModelTestCompilation";

        protected readonly Regex UnderTestRegex = new Regex(@"\[\|(?<content>.*?)\|\]");

        protected readonly MetadataReference[] References = new[]
        {
            MscorlibRef,
            SystemRef,
            SystemCoreRef,
            MsvbRef
        };

        protected void Test(string code, string replacementExpression, bool semanticChanges, string expressionToAnalyze = null, bool isBrokenCode = false)
        {
            var initialMatch = UnderTestRegex.Match(code);
            Assert.Equal(true, initialMatch.Success);
            var initialExpression = initialMatch.Groups["content"].Value;

            var initialTree = Parse(UnderTestRegex.Replace(code, m => m.Groups["content"].Value));
            var initialNode = initialTree.GetRoot().DescendantNodes().First(n => IsExpressionNode(n) && n.ToString() == (expressionToAnalyze ?? initialExpression));

            var replacementTree = Parse(UnderTestRegex.Replace(code, replacementExpression));
            var replacementNode = replacementTree.GetRoot().DescendantNodes().First(n => IsExpressionNode(n) && n.ToString() == (expressionToAnalyze ?? replacementExpression));

            var initialCompilation = CreateCompilation(initialTree);
            var initialModel = initialCompilation.GetSemanticModel(initialTree);

            if (!isBrokenCode)
            {
                CheckCompilation(initialCompilation);
                CheckCompilation(CreateCompilation(replacementTree));
            }

            Assert.Equal(semanticChanges, ReplacementChangesSemantics(initialNode, replacementNode, initialModel));
        }

        private void CheckCompilation(Compilation compilation)
        {
            using (var temporaryStream = new MemoryStream())
            {
                Assert.Equal(true, CompilationSucceeded(compilation, temporaryStream));
            }
        }

        protected abstract SyntaxTree Parse(string text);

        protected abstract bool IsExpressionNode(SyntaxNode node);

        protected abstract Compilation CreateCompilation(SyntaxTree tree);

        protected abstract bool CompilationSucceeded(Compilation compilation, Stream temporaryStream);

        protected abstract bool ReplacementChangesSemantics(SyntaxNode initialNode, SyntaxNode replacementNode, SemanticModel initialModel);
    }
}
