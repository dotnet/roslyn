// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal sealed class CSharpEditAndContinueTestHelpers : EditAndContinueTestHelpers
    {
        private readonly CSharpEditAndContinueAnalyzer _analyzer;

        public CSharpEditAndContinueTestHelpers(Action<SyntaxNode> faultInjector = null)
        {
            _analyzer = new CSharpEditAndContinueAnalyzer(faultInjector);
        }

        public override AbstractEditAndContinueAnalyzer Analyzer => _analyzer;
        public override string LanguageName => LanguageNames.CSharp;
        public override TreeComparer<SyntaxNode> TopSyntaxComparer => SyntaxComparer.TopLevel;

        public override SyntaxNode FindNode(SyntaxNode root, TextSpan span)
        {
            var result = root.FindToken(span.Start).Parent;
            while (result.Span != span)
            {
                result = result.Parent;
                Assert.NotNull(result);
            }

            return result;
        }

        public override ImmutableArray<SyntaxNode> GetDeclarators(ISymbol method)
        {
            Assert.True(method is MethodSymbol, "Only methods should have a syntax map.");
            return LocalVariableDeclaratorsCollector.GetDeclarators((SourceMemberMethodSymbol)method);
        }
    }
}
