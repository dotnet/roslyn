' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Public Class DeclarationTests
    Private Function ParseFile(text As String) As SyntaxTree
        Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.From(text), VisualBasicParseOptions.Default, "")
        Assert.False(tree.GetRoot().ContainsDiagnostics)
        Return tree
    End Function

    <Fact>
    Public Sub TestSimpleDeclarations()
        Dim text1 = <literal>
namespace NA.NB
  partial class C(Of T)
    partial class D
      private F As Integer
    end class
  end class
  class C 
  end class
end namespace
</literal>.Value

        Dim text2 = <literal>
namespace Na
  namespace nB
    partial class C(Of T)
      partial class d
        Sub G() 
        End Sub
      end class
    end class
  end namespace
end namespace
</literal>.Value

        Dim rootNamespace = ImmutableArray.Create(Of String)()
        Dim tree1 = ParseFile(text1)
        Dim tree2 = ParseFile(text2)
        Assert.NotNull(tree1)
        Assert.NotNull(tree2)
        Dim decl1 = DeclarationTreeBuilder.ForTree(tree1, rootNamespace, TestOptions.ReleaseDll.ScriptClassName, isSubmission:=False)
        Dim decl2 = DeclarationTreeBuilder.ForTree(tree2, rootNamespace, TestOptions.ReleaseDll.ScriptClassName, isSubmission:=False)
        Assert.Equal(DeclarationKind.Namespace, decl1.Kind)
        Assert.Equal(DeclarationKind.Namespace, decl2.Kind)
        Assert.NotNull(decl1)
        Assert.NotNull(decl2)
        Assert.Equal("", decl1.Name)
        Assert.Equal("", decl2.Name)
        Assert.Equal(1, decl1.Children.Length())
        Assert.Equal(1, decl2.Children.Length())
        Dim na1 = decl1.Children.Single()
        Dim na2 = decl2.Children.Single()
        Assert.NotNull(na1)
        Assert.NotNull(na2)
        Assert.Equal(DeclarationKind.Namespace, na1.Kind)
        Assert.Equal(DeclarationKind.Namespace, na2.Kind)
        Assert.Equal("NA", na1.Name)
        Assert.Equal("Na", na2.Name)
        Assert.Equal(1, na1.Children.Length())
        Assert.Equal(1, na2.Children.Length())
        Dim nb1 = na1.Children.Single()
        Dim nb2 = na2.Children.Single()
        Assert.NotNull(nb1)
        Assert.NotNull(nb2)
        Assert.Equal(DeclarationKind.Namespace, nb1.Kind)
        Assert.Equal(DeclarationKind.Namespace, nb2.Kind)
        Assert.Equal("NB", nb1.Name)
        Assert.Equal("nB", nb2.Name)
        Assert.Equal(2, nb1.Children.Length())
        Assert.Equal(1, nb2.Children.Length())
        Dim ct1 = nb1.Children.First()
        Dim ct2 = nb2.Children.Single()
        Assert.Equal(DeclarationKind.Class, ct1.Kind)
        Assert.Equal(DeclarationKind.Class, ct2.Kind)
        Assert.NotNull(ct1)
        Assert.NotNull(ct2)
        Assert.Equal("C", ct1.Name)
        Assert.Equal("C", ct2.Name)
        Assert.Equal(1, ct1.Children.Length())
        Assert.Equal(1, ct2.Children.Length())
        Dim c1 = DirectCast(nb1.Children.AsEnumerable().Skip(1).Single(), SingleTypeDeclaration)
        Assert.NotNull(c1)
        Assert.Equal(DeclarationKind.Class, c1.Kind)
        Assert.Equal("C", c1.Name)
        Assert.Equal(0, c1.Arity)
        Dim d1 = ct1.Children.Single()
        Dim d2 = ct2.Children.Single()
        Assert.NotNull(d1)
        Assert.NotNull(d2)
        Assert.Equal(DeclarationKind.Class, d1.Kind)
        Assert.Equal(DeclarationKind.Class, d2.Kind)
        Assert.Equal("D", d1.Name)
        Assert.Equal("d", d2.Name)
        Assert.Equal(0, d1.Children.Length())
        Assert.Equal(0, d2.Children.Length())

        Dim table As DeclarationTable = DeclarationTable.Empty
        Assert.False(table.AllRootNamespaces().Any)

        Dim mr = table.CalculateMergedRoot(Nothing)
        Assert.NotNull(mr)
        Assert.True(mr.Declarations.IsEmpty)
        Assert.True(table.TypeNames.IsEmpty())

        table = table.AddRootDeclaration(Lazy(decl1))
        mr = table.CalculateMergedRoot(Nothing)

        Assert.Equal(mr.Declarations, {decl1})

        Assert.Equal(DeclarationKind.Namespace, mr.Kind)
        Assert.Equal("", mr.Name)

        Dim na = mr.Children.Single()
        Assert.Equal(DeclarationKind.Namespace, na.Kind)
        Assert.Equal("NA", na.Name)

        Dim nb = na.Children.Single()
        Assert.Equal(DeclarationKind.Namespace, nb.Kind)
        Assert.Equal("NB", nb.Name)

        Dim ct = nb.Children.OfType(Of MergedTypeDeclaration).Where(Function(x) x.Arity = 1).Single()
        Assert.Equal(1, ct.Arity)
        Assert.Equal(DeclarationKind.Class, ct.Kind)
        Assert.Equal("C", ct.Name)

        Dim c = nb.Children.OfType(Of MergedTypeDeclaration).Where(Function(x) x.Arity = 0).Single()
        Assert.Equal(0, c.Arity)
        Assert.Equal(DeclarationKind.Class, c.Kind)
        Assert.Equal("C", c.Name)

        Dim d = ct.Children.Single()
        Assert.Equal(0, d.Arity)
        Assert.Equal(DeclarationKind.Class, d.Kind)
        Assert.Equal("D", d.Name)

        table = table.AddRootDeclaration(Lazy(decl2))
        mr = table.CalculateMergedRoot(Nothing)

        Assert.Equal(mr.Declarations, {decl1, decl2})

        Assert.Equal(DeclarationKind.Namespace, mr.Kind)
        Assert.Equal("", mr.Name)

        na = mr.Children.Single()
        Assert.Equal(DeclarationKind.Namespace, na.Kind)
        Assert.True(IdentifierComparison.Equals(na.Name, "NA"))

        nb = na.Children.Single()
        Assert.Equal(DeclarationKind.Namespace, nb.Kind)
        Assert.True(IdentifierComparison.Equals(nb.Name, "NB"))

        ct = nb.Children.OfType(Of MergedTypeDeclaration).Where(Function(x) x.Arity = 1).Single()
        Assert.Equal(1, ct.Arity)
        Assert.Equal(DeclarationKind.Class, ct.Kind)
        Assert.True(IdentifierComparison.Equals(ct.Name, "C"))

        c = nb.Children.OfType(Of MergedTypeDeclaration).Where(Function(x) x.Arity = 0).Single()
        Assert.Equal(0, c.Arity)
        Assert.Equal(DeclarationKind.Class, c.Kind)
        Assert.True(IdentifierComparison.Equals(c.Name, "C"))

        d = ct.Children.Single()
        Assert.Equal(0, d.Arity)
        Assert.Equal(DeclarationKind.Class, d.Kind)
        Assert.True(IdentifierComparison.Equals(d.Name, "D"))
    End Sub

    Private Function Lazy(decl As RootSingleNamespaceDeclaration) As DeclarationTableEntry
        Return New DeclarationTableEntry(New Lazy(Of RootSingleNamespaceDeclaration)(Function() decl), isEmbedded:=False)
    End Function

    <Fact>
    Public Sub TestRootNamespace()
        Dim text1 = <literal>
namespace NA
end namespace
Class C2
End Class
</literal>.Value

        Dim tree1 = ParseFile(text1)
        Assert.NotNull(tree1)
        Dim decl1 = DeclarationTreeBuilder.ForTree(tree1, {"Goo", "Bar"}.AsImmutableOrNull(), TestOptions.ReleaseDll.ScriptClassName, isSubmission:=False)

        Assert.Equal(DeclarationKind.Namespace, decl1.Kind)
        Assert.NotNull(decl1)
        Assert.Equal("", decl1.Name)
        Assert.Equal(1, decl1.Children.Length())

        Dim goo = decl1.Children.Single()
        Assert.NotNull(goo)
        Assert.Equal(DeclarationKind.Namespace, goo.Kind)
        Assert.Equal("Goo", goo.Name)
        Assert.Equal(1, goo.Children.Length())

        Dim bar = goo.Children.Single()
        Assert.NotNull(bar)
        Assert.Equal(DeclarationKind.Namespace, bar.Kind)
        Assert.Equal("Bar", bar.Name)
        Assert.Equal(2, bar.Children.Length())

        Dim childs = bar.Children.AsEnumerable().ToArray()
        Assert.Equal(DeclarationKind.Namespace, childs(0).Kind)
        Assert.Equal("NA", childs(0).Name)
        Assert.Equal(DeclarationKind.Class, childs(1).Kind)
        Assert.Equal("C2", childs(1).Name)
    End Sub

End Class
