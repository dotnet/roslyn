// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue
{
    internal sealed class CSharpEditAndContinueTestHelpers : EditAndContinueTestHelpers
    {
        private readonly ImmutableArray<PortableExecutableReference> _fxReferences;

        internal static readonly CSharpEditAndContinueTestHelpers Instance = new CSharpEditAndContinueTestHelpers(
            ImmutableArray.Create(TestReferences.NetFx.v4_0_30316_17626.mscorlib, TestReferences.NetFx.v4_0_30319.System_Core));
        
        internal static CSharpEditAndContinueTestHelpers Instance40 => new CSharpEditAndContinueTestHelpers(
            ImmutableArray.Create(TestReferences.NetFx.v4_0_30319.mscorlib, TestReferences.NetFx.v4_0_30319.System_Core));

        internal static CSharpEditAndContinueTestHelpers InstanceMinAsync => new CSharpEditAndContinueTestHelpers(
            ImmutableArray.Create(TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync));

        private static readonly CSharpEditAndContinueAnalyzer s_analyzer = new CSharpEditAndContinueAnalyzer();

        public CSharpEditAndContinueTestHelpers(ImmutableArray<PortableExecutableReference> fxReferences)
        {
            _fxReferences = fxReferences;
        }

        public override AbstractEditAndContinueAnalyzer Analyzer { get { return s_analyzer; } }

        public override Compilation CreateLibraryCompilation(string name, IEnumerable<SyntaxTree> trees)
        {
            return CSharpCompilation.Create("New", trees, _fxReferences, TestOptions.UnsafeReleaseDll);
        }

        public override SyntaxTree ParseText(string source)
        {
            return SyntaxFactory.ParseSyntaxTree(source);
        }

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
            return LocalVariableDeclaratorsCollector.GetDeclarators((SourceMethodSymbol)method);
        }
    }
}
