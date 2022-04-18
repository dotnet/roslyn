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

        Private Shared Function IsNamespaceWithName(symbol As INamespaceOrTypeSymbol, name As String) As Boolean
            Return TryCast(symbol, INamespaceSymbol)?.Name = name
        End Function

        Private Shared Function IsSimpleImportsClauseWithName(declaringSyntaxReference As SyntaxReference, name As String) As Boolean
            Dim syntax = declaringSyntaxReference.GetSyntax()
            Return TypeOf syntax Is IdentifierNameSyntax AndAlso
                TypeOf syntax.Parent Is SimpleImportsClauseSyntax AndAlso
                name = DirectCast(syntax, IdentifierNameSyntax).Identifier.Text
        End Function

#Region "normal Imports"

        <Fact>
        Public Sub TestBeforeImports()
            Dim Text = "'pos
imports System"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)

            Assert.Single(scopes.Single().Imports)
            Assert.True(IsNamespaceWithName(scopes.Single().Imports.Single().NamespaceOrType, NameOf(System)))
            Assert.True(IsSimpleImportsClauseWithName(scopes.Single().Imports.Single().DeclaringSyntaxReference, NameOf(System)))

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
            Assert.True(IsNamespaceWithName(scopes.Single().Imports.Single().NamespaceOrType, NameOf(System)))
            Assert.True(IsSimpleImportsClauseWithName(scopes.Single().Imports.Single().DeclaringSyntaxReference, NameOf(System)))

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
            Assert.True(IsNamespaceWithName(scopes.Single().Imports.Single().NamespaceOrType, NameOf(System)))
            Assert.True(IsSimpleImportsClauseWithName(scopes.Single().Imports.Single().DeclaringSyntaxReference, NameOf(System)))
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

            Assert.True(scopes.Single().Imports.Any(Function(i) IsNamespaceWithName(i.NamespaceOrType, NameOf(System))))
            Assert.True(scopes.Single().Imports.Any(Function(i) IsNamespaceWithName(i.NamespaceOrType, NameOf(Microsoft))))

            Assert.True(scopes.Single().Imports.Any(Function(i) IsSimpleImportsClauseWithName(i.DeclaringSyntaxReference, NameOf(System))))
            Assert.True(scopes.Single().Imports.Any(Function(i) IsSimpleImportsClauseWithName(i.DeclaringSyntaxReference, NameOf(Microsoft))))

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
            Assert.True(IsNamespaceWithName(scopes.Single().Imports.Single().NamespaceOrType, NameOf(System)))
            Assert.True(IsSimpleImportsClauseWithName(scopes.Single().Imports.Single().DeclaringSyntaxReference, NameOf(System)))

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
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
            Assert.True(IsNamespaceWithName(scopes.Single().Imports.Single().NamespaceOrType, NameOf(System)))
            Assert.True(IsSimpleImportsClauseWithName(scopes.Single().Imports.Single().DeclaringSyntaxReference, NameOf(System)))

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
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
            Assert.True(IsNamespaceWithName(scopes.Single().Imports.Single().NamespaceOrType, NameOf(System)))
            Assert.True(IsSimpleImportsClauseWithName(scopes.Single().Imports.Single().DeclaringSyntaxReference, NameOf(System)))

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

#End Region

    End Class
End Namespace
