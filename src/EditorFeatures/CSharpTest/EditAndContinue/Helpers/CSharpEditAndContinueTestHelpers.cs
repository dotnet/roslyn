// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue
{
    internal sealed class CSharpEditAndContinueTestHelpers : EditAndContinueTestHelpers
    {
        private readonly ImmutableArray<MetadataReference> _fxReferences;

        internal static CSharpEditAndContinueTestHelpers Instance
            => new CSharpEditAndContinueTestHelpers(TargetFramework.Mscorlib46Extended);

        internal static CSharpEditAndContinueTestHelpers Instance40
            => new CSharpEditAndContinueTestHelpers(TargetFramework.Mscorlib40AndSystemCore);

        private static readonly CSharpEditAndContinueAnalyzer s_analyzer = new CSharpEditAndContinueAnalyzer();

        public CSharpEditAndContinueTestHelpers(TargetFramework targetFramework)
        {
            _fxReferences = TargetFrameworkUtil.GetReferences(targetFramework);
        }

        public override AbstractEditAndContinueAnalyzer Analyzer => s_analyzer;

        public override Compilation CreateLibraryCompilation(string name, IEnumerable<SyntaxTree> trees)
        {
            return CSharpCompilation.Create("New", trees, _fxReferences, TestOptions.UnsafeReleaseDll);
        }

        public override SyntaxTree ParseText(string source)
            => SyntaxFactory.ParseSyntaxTree(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));

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
