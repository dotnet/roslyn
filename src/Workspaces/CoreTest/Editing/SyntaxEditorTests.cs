// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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
            => _emptyWorkspace ??= new AdhocWorkspace();

        private void VerifySyntax<TSyntax>(SyntaxNode node, string expectedText) where TSyntax : SyntaxNode
        {
            Assert.IsAssignableFrom<TSyntax>(node);

            var options = CSharpSyntaxFormattingOptions.Default;
            var formatted = Formatter.Format(node, EmptyWorkspace.Services.SolutionServices, options, CancellationToken.None);
            var actualText = formatted.ToFullString();
            Assert.Equal(expectedText, actualText);
        }

        private SyntaxEditor GetEditor(SyntaxNode root)
            => new SyntaxEditor(root, EmptyWorkspace.Services.SolutionServices);

        [Fact]
        public void TestReplaceNode()
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

            VerifySyntax<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public string Y;
}");
        }

        [Fact]
        public void TestRemoveNode()
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

            VerifySyntax<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
}");
        }

        [Fact]
        public void TestInsertAfter()
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

            VerifySyntax<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public int X;
    public string Y;
}");
        }

        [Fact]
        public void TestInsertBefore()
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

            VerifySyntax<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public string Y;
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
        public void TestMultipleEdits()
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

            VerifySyntax<CompilationUnitSyntax>(
                newRoot,
                @"
public class C
{
    public object Z;
    public string Y;
}");
        }

        [Fact]
        public void TestAddAttribute()
        {
            var code = """
using System;
using System.CodeAnalysis;

public class C
{
    Type Main(Type t)
    {
    }
}
""";
            var fixedCode = """
using System;
using System.CodeAnalysis;

public class C
{
    Type Main([Example(Sample.Attribute)] Type t)
    {
    }
}
""";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var methodX = (MethodDeclarationSyntax)editor.Generator.GetMembers(cls)[0];

            var param = methodX.ParameterList.Parameters[0];

            var syntaxGenerator = editor.Generator;
            var args = new[] { syntaxGenerator.AttributeArgument(syntaxGenerator.MemberAccessExpression(syntaxGenerator.DottedName("Sample"), "Attribute")) };
            var attribute = syntaxGenerator.Attribute("Example", args);
            editor.AddAttribute(param, attribute);
            var newRoot = editor.GetChangedRoot();

            VerifySyntax<CompilationUnitSyntax>(
                newRoot, fixedCode);
        }

        [Fact]
        public void TestAddGenericAttribute()
        {
            var code = """
using System;
using System.CodeAnalysis;

public class C
{
    Type Main<T>()
    {
    }
}
""";
            var fixedCode = """
using System;
using System.CodeAnalysis;

public class C
{
    Type Main<[Example(Sample.Attribute)] T>()
    {
    }
}
""";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var methodX = (MethodDeclarationSyntax)editor.Generator.GetMembers(cls)[0];

            var typeParam = methodX.TypeParameterList.Parameters[0];

            var syntaxGenerator = editor.Generator;
            var args = new[] { syntaxGenerator.AttributeArgument(syntaxGenerator.MemberAccessExpression(syntaxGenerator.DottedName("Sample"), "Attribute")) };
            var attribute = syntaxGenerator.Attribute("Example", args);
            editor.AddAttribute(typeParam, attribute);
            var newRoot = editor.GetChangedRoot();

            VerifySyntax<CompilationUnitSyntax>(
                newRoot, fixedCode);
        }

        [Fact]
        public void TestAddReturnAttribute()
        {
            var code = """
using System;
using System.CodeAnalysis;

public class C
{
    Type Main(Type t)
    {
    }
}
""";
            var fixedCode = """
using System;
using System.CodeAnalysis;

public class C
{
    [return: Example(Sample.Attribute)]
    Type Main(Type t)
    {
    }
}
""";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];

            var editor = GetEditor(cu);
            var methodX = editor.Generator.GetMembers(cls)[0];

            var syntaxGenerator = editor.Generator;
            var args = new[] { syntaxGenerator.AttributeArgument(syntaxGenerator.MemberAccessExpression(syntaxGenerator.DottedName("Sample"), "Attribute")) };
            var attribute = syntaxGenerator.Attribute("Example", args);
            editor.AddReturnAttribute(methodX, attribute);
            var newRoot = editor.GetChangedRoot();

            VerifySyntax<CompilationUnitSyntax>(
                newRoot, fixedCode);
        }
    }
}
