// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Editing
{
    [UseExportProvider]
    public class SyntaxEditorTests
    {
        private Workspace _emptyWorkspace;

        private Workspace EmptyWorkspace
            => _emptyWorkspace ?? (_emptyWorkspace = new AdhocWorkspace());

        private async Task VerifySyntaxAsync<TSyntax>(SyntaxNode node, string expectedText) where TSyntax : SyntaxNode
        {
            Assert.IsAssignableFrom(typeof(TSyntax), node);
            var formatted = await Formatter.FormatAsync(node, EmptyWorkspace);
            var actualText = formatted.ToFullString();
            Assert.Equal(expectedText, actualText);
        }

        private SyntaxEditor GetEditor(SyntaxNode root)
        {
            return new SyntaxEditor(root, EmptyWorkspace);
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
}");
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
}");
        }

        [Fact]
        public async Task TestInsertAfter()
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
}");
        }

        [Fact]
        public async Task TestInsertBefore()
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
}");
        }

        [Fact]
        public async Task TestReplaceWithTracking()
        {
            // ReplaceNode overload #1
            await TestReplaceWithTrackingCoreAsync((SyntaxNode node, SyntaxNode newNode, SyntaxEditor editor) =>
            {
                editor.ReplaceNode(node, newNode);
            });

            // ReplaceNode overload #2
            await TestReplaceWithTrackingCoreAsync((SyntaxNode node, SyntaxNode newNode, SyntaxEditor editor) =>
            {
                editor.ReplaceNode(node, computeReplacement: (originalNode, generator) => newNode);
            });

            // ReplaceNode overload #3
            await TestReplaceWithTrackingCoreAsync((SyntaxNode node, SyntaxNode newNode, SyntaxEditor editor) =>
            {
                editor.ReplaceNode(node,
                     computeReplacement: (originalNode, generator, argument) => newNode,
                     argument: (object)null);
            });
        }

        private async Task TestReplaceWithTrackingCoreAsync(Action<SyntaxNode, SyntaxNode, SyntaxEditor> replaceNodeWithTracking)
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
            var newFieldY = editor.Generator.FieldDeclaration("Y", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public);
            replaceNodeWithTracking(fieldX, newFieldY, editor);

            var newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public string Y;
}");
            var newFieldYType = newFieldY.DescendantNodes().Single(n => n.ToString() == "string");
            var newType = editor.Generator.TypeExpression(SpecialType.System_Char);
            replaceNodeWithTracking(newFieldYType, newType, editor);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public char Y;
}");

            editor.RemoveNode(newFieldY);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
}");
        }

        [Fact]
        public async Task TestReplaceWithTracking_02()
        {
            var code = @"
public class C
{
    public int X;
    public string X2;
    public char X3;
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            
            var editor = GetEditor(cu);

            var cls = cu.Members[0];
            var fieldX = editor.Generator.GetMembers(cls)[0];
            var fieldX2 = editor.Generator.GetMembers(cls)[1];
            var fieldX3 = editor.Generator.GetMembers(cls)[2];

            var newFieldY = editor.Generator.FieldDeclaration("Y", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public);
            editor.ReplaceNode(fieldX, newFieldY);

            var newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public string Y;
    public string X2;
    public char X3;
}");
            var newFieldYType = newFieldY.DescendantNodes().Single(n => n.ToString() == "string");
            var newType = editor.Generator.TypeExpression(SpecialType.System_Char);
            editor.ReplaceNode(newFieldYType, newType);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public char Y;
    public string X2;
    public char X3;
}");

            var newFieldY2 = editor.Generator.FieldDeclaration("Y2", editor.Generator.TypeExpression(SpecialType.System_Boolean), Accessibility.Private);
            editor.ReplaceNode(fieldX2, newFieldY2);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public char Y;
    private bool Y2;
    public char X3;
}");

            var newFieldZ = editor.Generator.FieldDeclaration("Z", editor.Generator.TypeExpression(SpecialType.System_Boolean), Accessibility.Public);
            editor.ReplaceNode(newFieldY, newFieldZ);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public bool Z;
    private bool Y2;
    public char X3;
}");

            var originalFieldX3Type = fieldX3.DescendantNodes().Single(n => n.ToString() == "char");
            newType = editor.Generator.TypeExpression(SpecialType.System_Boolean);
            editor.ReplaceNode(originalFieldX3Type, newType);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public bool Z;
    private bool Y2;
    public bool X3;
}");

            editor.RemoveNode(newFieldY2);
            editor.RemoveNode(fieldX3);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public bool Z;
}");
        }

        [Fact]
        public async Task TestInsertAfterWithTracking()
        {
            // InsertAfter overload #1
            await TestInsertAfterWithTrackingCoreAsync((SyntaxNode node, SyntaxNode newNode, SyntaxEditor editor) =>
            {
                editor.InsertAfter(node, newNode);
            });

            // InsertAfter overload #2
            await TestInsertAfterWithTrackingCoreAsync((SyntaxNode node, SyntaxNode newNode, SyntaxEditor editor) =>
            {
                editor.InsertAfter(node, new[] { newNode });
            });
        }

        private async Task TestInsertAfterWithTrackingCoreAsync(Action<SyntaxNode, SyntaxNode, SyntaxEditor> insertAfterWithTracking)
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
            var newFieldY = editor.Generator.FieldDeclaration("Y", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public);
            insertAfterWithTracking(fieldX, newFieldY, editor);

            var newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public int X;
    public string Y;
}");

            var newFieldZ = editor.Generator.FieldDeclaration("Z", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public);
            editor.ReplaceNode(newFieldY, newFieldZ);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public int X;
    public string Z;
}");
        }

        [Fact]
        public async Task TestInsertBeforeWithTracking()
        {
            // InsertBefore overload #1
            await TestInsertBeforeWithTrackingCoreAsync((SyntaxNode node, SyntaxNode newNode, SyntaxEditor editor) =>
            {
                editor.InsertBefore(node, newNode);
            });

            // InsertBefore overload #2
            await TestInsertBeforeWithTrackingCoreAsync((SyntaxNode node, SyntaxNode newNode, SyntaxEditor editor) =>
            {
                editor.InsertBefore(node, new[] { newNode });
            });
        }

        private async Task TestInsertBeforeWithTrackingCoreAsync(Action<SyntaxNode, SyntaxNode, SyntaxEditor> insertBeforeWithTracking)
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
            var newFieldY = editor.Generator.FieldDeclaration("Y", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public);
            insertBeforeWithTracking(fieldX, newFieldY, editor);

            var newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public string Y;
    public int X;
}");

            var newFieldZ = editor.Generator.FieldDeclaration("Z", editor.Generator.TypeExpression(SpecialType.System_String), Accessibility.Public);
            editor.ReplaceNode(newFieldY, newFieldZ);

            newRoot = editor.GetChangedRoot();
            await VerifySyntaxAsync<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public string Z;
    public int X;
}");
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
}");
        }
    }
}
