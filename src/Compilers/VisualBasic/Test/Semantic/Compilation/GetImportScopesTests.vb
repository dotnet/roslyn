' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GetImportScopesTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub TestEmptyFile()
            Dim text = "'pos"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Empty(scopes)
        End Sub

        <Fact>
        Public Sub TestNoImportsBeforeMemberDeclaration()
            Dim Text = "'pos
class C
end class"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Empty(scopes)
        End Sub

#Region "normal Imports"

        <Fact>
        Public Sub TestBeforeImports()
            dim Text = "'pos
imports System"
            dim tree = Parse(Text)
            dim comp = CreateCompilation(tree)
            dim model = comp.GetSemanticModel(tree)
            dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Imports)
            Assert.True(TypeOf scopes.Single().Imports.Single().NamespaceOrType Is INamespaceSymbol)
            Assert.True(TypeOf scopes.Single().Imports.Single().DeclaringSyntaxReference.GetSyntax().Parent Is SimpleImportsClauseSyntax)
            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterImportsNoContent()
            Dim Text = "
imports System
'pos"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Imports)
            'Assert.True(scopes.Single().Imports.Single().NamespaceOrType Is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace:   true, Name: NameOf(System) })
            'Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference.GetSyntax() Is UsingDirectiveSyntax)
            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterImportsBeforeMemberDeclaration()
            Dim Text = "
imports System
'pos
class C
end class
"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
        End Sub

        <Fact>
        Public Sub TestAfterMultipleImportsNoContent()
            Dim Text = "
imports System
imports Microsoft
'pos"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Equal(2, scopes.Single().Imports.Length)
            'Assert.True(scopes.Single().Imports.First().NamespaceOrType Is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace:   true, Name: NameOf(System) })
            'Assert.True(scopes.Single().Imports.Last().NamespaceOrType Is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace:   true, Name: NameOf(Microsoft) })
            'Assert.True(scopes.Single().Imports.First().DeclaringSyntaxReference.GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:   NameOf(System) } })
            'Assert.True(scopes.Single().Imports.Last().DeclaringSyntaxReference.GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:   NameOf(Microsoft) } })
            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestNestedNamespaceOuterPosition()
            Dim Text = "
imports System

class C
    'pos
end class

namespace N
    class D
    end class
end namespace
"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Imports)
            'Assert.True(scopes.Single().Imports.Single().NamespaceOrType Is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace:   true, Name: NameOf(System) })
            'Assert.True(scopes.Single().Imports.Single().DeclaringSyntaxReference.GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:   NameOf(System) } })
        End Sub

        <Fact>
        Public Sub TestNestedNamespaceInnerPosition()
            Dim Text = "
imports System

namespace N
    class C
        'pos
    end class
end namespace
"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single.Imports)
            'Assert.True(scopes(0).Imports.Single().NamespaceOrType Is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: NameOf(Microsoft) })
            'Assert.True(scopes(0).Imports.Single().DeclaringSyntaxReference.GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:   NameOf(Microsoft) } })
            'Assert.True(scopes(1).Imports.Single().NamespaceOrType Is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: NameOf(System) })
            'Assert.True(scopes(1).Imports.Single().DeclaringSyntaxReference.GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:   NameOf(System) } })
        End Sub

        <Fact>
        Public Sub TestNestedNamespaceInnerPositionIntermediaryEmptyNamespace()
            Dim Text = "
imports System

namespace Outer
    namespace N
        class C
            'pos
        end class
    end namespace
end namespace
"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Imports)
            'Assert.True(scopes(0).Imports.Single().NamespaceOrType Is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: NameOf(Microsoft) })
            'Assert.True(scopes(0).Imports.Single().DeclaringSyntaxReference.GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:  NameOf(Microsoft) } })
            'Assert.True(scopes(1).Imports.Single().NamespaceOrType Is INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: NameOf(System) })
            'Assert.True(scopes(1).Imports.Single().DeclaringSyntaxReference.GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:  NameOf(System) } })
        End Sub

#End Region

    End Class
End Namespace
