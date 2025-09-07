// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class SyntaxPathTests
{
    [Fact]
    public void RecoverSingle()
    {
        var node = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Hi"));

        var path = new SyntaxPath(node);
        Assert.True(path.TryResolve(node, out SyntaxNode recovered));
        Assert.Equal(node, recovered);
    }

    [Fact]
    public void FailFirstType()
    {
        var node = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Hi"));

        var path = new SyntaxPath(node);
        Assert.False(path.TryResolve(SyntaxFactory.ParseExpression("Goo()"), out SyntaxNode _));
    }

    [Fact]
    public void RecoverChild()
    {
        var node = SyntaxFactory.ParseExpression("Goo()");
        var child = ((InvocationExpressionSyntax)node).ArgumentList;
        var path = new SyntaxPath(child);
        Assert.True(path.TryResolve(node, out SyntaxNode recovered));
        Assert.Equal(child, recovered);
    }

    [Fact]
    public void FailChildCount()
    {
        var root = SyntaxFactory.ParseExpression("Goo(a, b)");
        var path = new SyntaxPath(((InvocationExpressionSyntax)root).ArgumentList.Arguments.Last());

        var root2 = SyntaxFactory.ParseExpression("Goo(a)");
        Assert.False(path.TryResolve(root2, out SyntaxNode _));
    }

    [Fact]
    public void FailChildType()
    {
        var root = SyntaxFactory.ParseExpression("Goo(a)");
        var path = new SyntaxPath(((InvocationExpressionSyntax)root).ArgumentList.Arguments.First().Expression);

        var root2 = SyntaxFactory.ParseExpression("Goo(3)");
        Assert.False(path.TryResolve(root2, out SyntaxNode _));
    }

    [Fact]
    public void RecoverGeneric()
    {
        var root = SyntaxFactory.ParseExpression("Goo()");
        var node = ((InvocationExpressionSyntax)root).ArgumentList;
        var path = new SyntaxPath(node);
        Assert.True(path.TryResolve(root, out ArgumentListSyntax recovered));
        Assert.Equal(node, recovered);
    }

    [Fact]
    public void TestRoot1()
    {
        var tree = SyntaxFactory.ParseSyntaxTree(string.Empty);
        var root = tree.GetRoot();
        var path = new SyntaxPath(root);
        Assert.True(path.TryResolve(tree, CancellationToken.None, out SyntaxNode node));
        Assert.Equal(root, node);
    }

    [Fact]
    public void TestRoot2()
    {
        var text = SourceText.From(string.Empty);
        var tree = SyntaxFactory.ParseSyntaxTree(string.Empty);
        var root = tree.GetCompilationUnitRoot();
        var path = new SyntaxPath(root);

        var newText = text.WithChanges(new TextChange(new TextSpan(0, 0), "class C {}"));
        var newTree = tree.WithChangedText(newText);
        Assert.True(path.TryResolve(newTree, CancellationToken.None, out SyntaxNode node));
        Assert.Equal(SyntaxKind.CompilationUnit, node.Kind());
    }

    [Fact]
    public void TestRoot3()
    {
        var text = SourceText.From("class C {}");
        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var root = tree.GetRoot();
        var path = new SyntaxPath(root);

        var newText = text.WithChanges(new TextChange(new TextSpan(0, text.Length), ""));
        var newTree = tree.WithChangedText(newText);
        Assert.True(path.TryResolve(newTree, CancellationToken.None, out SyntaxNode node));
        Assert.Equal(SyntaxKind.CompilationUnit, node.Kind());
    }

    [Fact]
    public void TestRoot4()
    {
        var text = "class C {}";
        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var root = tree.GetRoot();
        var path = new SyntaxPath(root);

        tree = WithReplaceFirst(tree, "C", "D");
        Assert.True(path.TryResolve(tree, CancellationToken.None, out SyntaxNode node));
        Assert.Equal(SyntaxKind.CompilationUnit, node.Kind());
    }

    [Fact]
    public void TestMethodBodyChange()
    {
        var text =
            """
            namespace N {
                class C {
                  void M1() {
                    int i1;
                  }
                  void M2() {
                    int i2;
                  }
                  void M3() {
                    int i3;
                  }
                }
              }
            """;

        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var namespaceDecl = (NamespaceDeclarationSyntax)(tree.GetRoot() as CompilationUnitSyntax).Members[0];
        var classDecl = (TypeDeclarationSyntax)namespaceDecl.Members[0];

        var member1 = classDecl.Members[0];
        var member2 = classDecl.Members[1];
        var member3 = classDecl.Members[2];

        var path1 = new SyntaxPath(member1);
        var path2 = new SyntaxPath(member2);
        var path3 = new SyntaxPath(member3);

        tree = WithReplaceFirst(WithReplaceFirst(WithReplaceFirst(tree, "i1", "j1"), "i2", "j2"), "i3", "j3");
        Assert.True(path1.TryResolve(tree, CancellationToken.None, out SyntaxNode n1));
        Assert.True(path2.TryResolve(tree, CancellationToken.None, out SyntaxNode n2));
        Assert.True(path3.TryResolve(tree, CancellationToken.None, out SyntaxNode n3));

        Assert.Equal(SyntaxKind.MethodDeclaration, n1.Kind());
        Assert.Equal("M1", ((MethodDeclarationSyntax)n1).Identifier.ValueText);

        Assert.Equal(SyntaxKind.MethodDeclaration, n2.Kind());
        Assert.Equal("M2", ((MethodDeclarationSyntax)n2).Identifier.ValueText);

        Assert.Equal(SyntaxKind.MethodDeclaration, n3.Kind());
        Assert.Equal("M3", ((MethodDeclarationSyntax)n3).Identifier.ValueText);
    }

    [Fact]
    public void TestAddBase()
    {
        var text =
            """
            namespace N {
                class C {
                }
                class D {
                }
              }
            """;

        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var namespaceDecl = (NamespaceDeclarationSyntax)(tree.GetRoot() as CompilationUnitSyntax).Members[0];
        var class1 = (TypeDeclarationSyntax)namespaceDecl.Members[0];
        var class2 = (TypeDeclarationSyntax)namespaceDecl.Members[1];

        var path1 = new SyntaxPath(class1);
        var path2 = new SyntaxPath(class2);

        tree = WithReplaceFirst(tree, "C {", "C : Goo {");
        Assert.True(path1.TryResolve(tree, CancellationToken.None, out SyntaxNode n1));
        Assert.True(path2.TryResolve(tree, CancellationToken.None, out SyntaxNode n2));

        Assert.Equal(SyntaxKind.ClassDeclaration, n1.Kind());
        Assert.Equal("C", ((TypeDeclarationSyntax)n1).Identifier.ValueText);
        Assert.Equal(SyntaxKind.ClassDeclaration, n2.Kind());
        Assert.Equal("D", ((TypeDeclarationSyntax)n2).Identifier.ValueText);
    }

    [Fact]
    public void TestAddFieldBefore()
    {
        var text =
            """
            namespace N {
                class C {
                  void M1() {
                    int i1;
                  }
                  bool M2() {
                    int i2;
                  }
                }
              }
            """;

        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var namespaceDecl = (NamespaceDeclarationSyntax)((CompilationUnitSyntax)tree.GetRoot()).Members[0];
        var classDecl = (TypeDeclarationSyntax)namespaceDecl.Members[0];

        var member1 = classDecl.Members[0];
        var member2 = classDecl.Members[1];

        var path1 = new SyntaxPath(member1);
        var path2 = new SyntaxPath(member2);

        tree = WithReplaceFirst(tree, "bool", "int field; bool");
        Assert.True(path1.TryResolve(tree, CancellationToken.None, out SyntaxNode n1));
        Assert.True(path2.TryResolve(tree, CancellationToken.None, out SyntaxNode n2));

        Assert.Equal(SyntaxKind.MethodDeclaration, n1.Kind());
        Assert.Equal("M1", ((MethodDeclarationSyntax)n1).Identifier.ValueText);

        Assert.Equal(SyntaxKind.MethodDeclaration, n2.Kind());
        Assert.Equal("M2", ((MethodDeclarationSyntax)n2).Identifier.ValueText);
    }

    [Fact]
    public void TestChangeType()
    {
        var text =
            """
            namespace N {
                class C {
                }
                class D {
                }
              }
            """;

        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var namespaceDecl = (NamespaceDeclarationSyntax)((CompilationUnitSyntax)tree.GetRoot()).Members[0];
        var class1 = (TypeDeclarationSyntax)namespaceDecl.Members[0];
        var class2 = (TypeDeclarationSyntax)namespaceDecl.Members[1];

        var path1 = new SyntaxPath(class1);
        var path2 = new SyntaxPath(class2);

        tree = WithReplaceFirst(tree, "class", "struct");
        Assert.True(path1.TryResolve(tree, CancellationToken.None, out SyntaxNode n1));
        Assert.False(path2.TryResolve(tree, CancellationToken.None, out SyntaxNode _));

        Assert.Equal(SyntaxKind.ClassDeclaration, n1.Kind());
        Assert.Equal("D", ((TypeDeclarationSyntax)n1).Identifier.ValueText);
    }

    [Fact]
    public void TestChangeType1()
    {
        var text =
            """
            namespace N {
                class C {
                  void Goo();
                }
                class D {
                }
              }
            """;

        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var namespaceDecl = (NamespaceDeclarationSyntax)((CompilationUnitSyntax)tree.GetRoot()).Members[0];
        var class1 = (TypeDeclarationSyntax)namespaceDecl.Members[0];
        var class2 = (TypeDeclarationSyntax)namespaceDecl.Members[1];
        var method1 = class1.Members[0];

        var path1 = new SyntaxPath(class1);
        var path2 = new SyntaxPath(class2);
        var path3 = new SyntaxPath(method1);

        tree = WithReplaceFirst(tree, "class", "struct");
        Assert.True(path1.TryResolve(tree, CancellationToken.None, out SyntaxNode n1));
        Assert.False(path2.TryResolve(tree, CancellationToken.None, out SyntaxNode _));
        Assert.False(path3.TryResolve(tree, CancellationToken.None, out SyntaxNode _));

        Assert.Equal(SyntaxKind.ClassDeclaration, n1.Kind());
        Assert.Equal("D", ((TypeDeclarationSyntax)n1).Identifier.ValueText);
    }

    [Fact]
    public void TestWhitespace1()
    {
        var text =
            """
            namespace N {
                class C {
                }
                class D {
                }
              }
            """;
        var text2 = text.Replace(" ", "  ");

        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var namespaceDecl = (NamespaceDeclarationSyntax)((CompilationUnitSyntax)tree.GetRoot()).Members[0];
        var class1 = (TypeDeclarationSyntax)namespaceDecl.Members[0];
        var class2 = (TypeDeclarationSyntax)namespaceDecl.Members[1];

        var path1 = new SyntaxPath(class1);
        var path2 = new SyntaxPath(class2);

        tree = WithReplace(tree, 0, text.Length, text2);
        Assert.True(path1.TryResolve(tree, CancellationToken.None, out SyntaxNode n1));
        Assert.True(path2.TryResolve(tree, CancellationToken.None, out SyntaxNode n2));

        Assert.Equal(SyntaxKind.ClassDeclaration, n1.Kind());
        Assert.Equal("C", ((TypeDeclarationSyntax)n1).Identifier.ValueText);
        Assert.Equal(SyntaxKind.ClassDeclaration, n2.Kind());
        Assert.Equal("D", ((TypeDeclarationSyntax)n2).Identifier.ValueText);
    }

    [Fact]
    public void TestComment()
    {
        var text =
            """
            namespace N {
                class C {
                }
                struct D {
                }
              }
            """;

        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var namespaceDecl = (NamespaceDeclarationSyntax)((CompilationUnitSyntax)tree.GetRoot()).Members[0];
        var class1 = (TypeDeclarationSyntax)namespaceDecl.Members[0];
        var class2 = (TypeDeclarationSyntax)namespaceDecl.Members[1];

        var path1 = new SyntaxPath(class1);
        var path2 = new SyntaxPath(class2);

        tree = WithReplaceFirst(WithReplaceFirst(tree, "class", "/* goo */ class"), "struct", "/* bar */ struct");
        Assert.True(path1.TryResolve(tree, CancellationToken.None, out SyntaxNode n1));
        Assert.True(path2.TryResolve(tree, CancellationToken.None, out SyntaxNode n2));

        Assert.Equal(SyntaxKind.ClassDeclaration, n1.Kind());
        Assert.Equal("C", ((TypeDeclarationSyntax)n1).Identifier.ValueText);
        Assert.Equal(SyntaxKind.StructDeclaration, n2.Kind());
        Assert.Equal("D", ((TypeDeclarationSyntax)n2).Identifier.ValueText);
    }

    [Fact]
    public void TestPP1()
    {
        var text =
            """
            namespace N {
                class C {
                }
                struct D {
                }
              }
            """;
        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var namespaceDecl = (NamespaceDeclarationSyntax)((CompilationUnitSyntax)tree.GetRoot()).Members[0];
        var class1 = (TypeDeclarationSyntax)namespaceDecl.Members[0];
        var class2 = (TypeDeclarationSyntax)namespaceDecl.Members[1];

        var path1 = new SyntaxPath(class1);
        var path2 = new SyntaxPath(class2);

        tree = WithReplace(tree, 0, text.Length, """
            namespace N {
            #if true
                class C {
                }
                struct D {
                }
            #endif
              }
            """);
        Assert.True(path1.TryResolve(tree, CancellationToken.None, out SyntaxNode n1));
        Assert.True(path2.TryResolve(tree, CancellationToken.None, out SyntaxNode n2));

        Assert.Equal(SyntaxKind.ClassDeclaration, n1.Kind());
        Assert.Equal("C", ((TypeDeclarationSyntax)n1).Identifier.ValueText);
        Assert.Equal(SyntaxKind.StructDeclaration, n2.Kind());
        Assert.Equal("D", ((TypeDeclarationSyntax)n2).Identifier.ValueText);
    }

    [Fact]
    public void TestRemoveUsing()
    {
        var text = SourceText.From("using X; class C {}");
        var tree = SyntaxFactory.ParseSyntaxTree(text);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var path = new SyntaxPath(root.Members[0]);

        var newText = WithReplaceFirst(text, "using X;", "");
        var newTree = tree.WithChangedText(newText);
        Assert.True(path.TryResolve(newTree, CancellationToken.None, out SyntaxNode node));

        Assert.Equal(SyntaxKind.ClassDeclaration, node.Kind());
    }

    internal static SourceText WithReplaceFirst(SourceText text, string oldText, string newText)
    {
        var oldFullText = text.ToString();
        var offset = oldFullText.IndexOf(oldText, StringComparison.Ordinal);
        var length = oldText.Length;
        var span = new TextSpan(offset, length);
        var newFullText = oldFullText[..offset] + newText + oldFullText[span.End..];
        return SourceText.From(newFullText);
    }

    public static SyntaxTree WithReplaceFirst(SyntaxTree syntaxTree, string oldText, string newText)
    {
        return WithReplace(syntaxTree,
            startIndex: 0,
            oldText: oldText,
            newText: newText);
    }

    public static SyntaxTree WithReplace(SyntaxTree syntaxTree, int offset, int length, string newText)
    {
        var oldFullText = syntaxTree.GetText();
        var newFullText = oldFullText.WithChanges(new TextChange(new TextSpan(offset, length), newText));
        return syntaxTree.WithChangedText(newFullText);
    }

    public static SyntaxTree WithReplace(SyntaxTree syntaxTree, int startIndex, string oldText, string newText)
    {            // Use the offset to find the first element to replace at
        return WithReplace(syntaxTree,
            offset: syntaxTree.GetText().ToString().IndexOf(oldText, startIndex, StringComparison.Ordinal),
            length: oldText.Length,
            newText: newText);
    }
}
