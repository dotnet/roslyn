// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal sealed class CSharpEditAndContinueTestHelpers : EditAndContinueTestHelpers
    {
        private readonly ImmutableArray<MetadataReference> _fxReferences;
        private readonly CSharpEditAndContinueAnalyzer _analyzer;

        public CSharpEditAndContinueTestHelpers(TargetFramework targetFramework, Action<SyntaxNode> faultInjector = null)
        {
            _fxReferences = TargetFrameworkUtil.GetReferences(targetFramework);
            _analyzer = new CSharpEditAndContinueAnalyzer(faultInjector);
        }

        internal static CSharpEditAndContinueTestHelpers CreateInstance(Action<SyntaxNode> faultInjector = null)
            => new CSharpEditAndContinueTestHelpers(TargetFramework.Mscorlib46Extended, faultInjector);

        internal static CSharpEditAndContinueTestHelpers CreateInstance40(Action<SyntaxNode> faultInjector = null)
            => new CSharpEditAndContinueTestHelpers(TargetFramework.Mscorlib40AndSystemCore, faultInjector);

        public override AbstractEditAndContinueAnalyzer Analyzer => _analyzer;

        public override Compilation CreateLibraryCompilation(string name, IEnumerable<SyntaxTree> trees)
            => CSharpCompilation.Create("New", trees, _fxReferences, TestOptions.UnsafeReleaseDll);

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
