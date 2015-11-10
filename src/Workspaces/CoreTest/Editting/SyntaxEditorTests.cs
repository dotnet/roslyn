// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Editting
{
    public class SyntaxEditorTests
    {
        private readonly Workspace _emptyWorkspace = new AdhocWorkspace();

        private async Task VerifySyntaxAsync<TSyntax>(SyntaxNode node, string expectedText) where TSyntax : SyntaxNode
        {
            Assert.IsAssignableFrom(typeof(TSyntax), node);
            var formatted = await Formatter.FormatAsync(node, _emptyWorkspace).ConfigureAwait(true);
            var actualText = formatted.ToFullString();
            Assert.Equal(expectedText, actualText);
        }

        private SyntaxEditor GetEditor(SyntaxNode root)
        {
            return new SyntaxEditor(root, _emptyWorkspace);
        }

        [Fact]
        public async Task TestReplaceNode()
        {
            var code = @"
public class C
{
    public int X;
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var fieldX = editor.Generator.GetMembers(cls)[0];
            editor.ReplaceNode(fieldX, editor.Generator.FieldDeclaration("Y", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public));
            var newRoot = editor.GetChangedRoot();

            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public string Y;
}").ConfigureAwait(true);
        }

        [Fact]
        public async Task TestRemoveNode()
        {
            var code = @"
public class C
{
    public int X;
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var fieldX = editor.Generator.GetMembers(cls)[0];
            editor.RemoveNode(fieldX);
            var newRoot = editor.GetChangedRoot();

            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
}").ConfigureAwait(true);
        }

        [Fact]
        public async Task TestInterAfter()
        {
            var code = @"
public class C
{
    public int X;
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var fieldX = editor.Generator.GetMembers(cls)[0];
            editor.InsertAfter(fieldX, editor.Generator.FieldDeclaration("Y", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public));
            var newRoot = editor.GetChangedRoot();

            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public int X;
    public string Y;
}").ConfigureAwait(true);
        }

        [Fact]
        public async Task TestInterBefore()
        {
            var code = @"
public class C
{
    public int X;
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var fieldX = editor.Generator.GetMembers(cls)[0];
            editor.InsertBefore(fieldX, editor.Generator.FieldDeclaration("Y", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public));
            var newRoot = editor.GetChangedRoot();

            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public string Y;
    public int X;
}").ConfigureAwait(true);
        }

        [Fact]
        public void TestTrackNode()
        {
            var code = @"
public class C
{
    public int X;
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var fieldX = editor.Generator.GetMembers(cls)[0];
            editor.TrackNode(fieldX);
            var newRoot = editor.GetChangedRoot();

            var currentFieldX = newRoot.GetCurrentNode(fieldX);
            Assert.NotNull(currentFieldX);
        }

        [Fact]
        public async Task TestMultipleEdits()
        {
            var code = @"
public class C
{
    public int X;
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var fieldX = editor.Generator.GetMembers(cls)[0];
            editor.InsertAfter(fieldX, editor.Generator.FieldDeclaration("Y", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public));
            editor.InsertBefore(fieldX, editor.Generator.FieldDeclaration("Z", editor.Generator.TypeExpression(SpecialType.System_Object), Accessibility.Public));
            editor.RemoveNode(fieldX);
            var newRoot = editor.GetChangedRoot();

            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public object Z;
    public string Y;
}").ConfigureAwait(true);
        }
    }
}
