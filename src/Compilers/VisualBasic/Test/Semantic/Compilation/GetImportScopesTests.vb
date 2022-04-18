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

#Region "aliases"

        <Fact>
        Public Sub TestBeforeAlias()
            dim Text = "'pos
using S = System"
            dim tree = Parse(Text)
            dim comp = CreateCompilation(tree)
            dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Aliases)
            'Assert.True(scopes.Single().Aliases.Single() Is {Name:  "S", Target: INamespaceSymbol {ContainingNamespace.IsGlobalNamespace:  true, Name: NameOf(System) } })
            'Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() Is UsingDirectiveSyntax)
            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterAliasNoContent()
            Dim Text = "
using S = System
'pos"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Aliases)
            'Assert.True(scopes.Single().Aliases.Single() Is {Name:  "S", Target: { ContainingNamespace.IsGlobalNamespace: true, Name: NameOf(System) } })
            'Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() Is UsingDirectiveSyntax)
            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterAliasBeforeMemberDeclaration()
            Dim Text = "
using S = System
'pos
class C
{
}"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Empty(scopes)
        End Sub

        <Fact>
        Public Sub TestBeforeAliasTopLevelStatements()
            Dim Text = "
'pos
using S = System

return"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Aliases)
            'Assert.True(scopes.Single().Aliases.Single() Is {Name:  "S", Target: INamespaceSymbol {ContainingNamespace.IsGlobalNamespace:  true, Name: NameOf(System) } })
            'Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() Is UsingDirectiveSyntax)
            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterAliasTopLevelStatements1()
            Dim Text = "
using S = System
'pos
return"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Empty(scopes)
        End Sub

        <Fact>
        Public Sub TestAfterAliasTopLevelStatements2()
            Dim Text = "
using S = System

return 'pos"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Aliases)
            'Assert.True(scopes.Single().Aliases.Single() Is {Name:  "S", Target: { ContainingNamespace.IsGlobalNamespace: true, Name: NameOf(System) } })
            'Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() Is UsingDirectiveSyntax)
            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterMultipleAliasesNoContent()
            Dim Text = "
using S = System
using M = Microsoft
'pos"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Equal(2, scopes.Single().Aliases.Length)
            'Assert.True(scopes.Single().Aliases.Any(a >= a Is {Name:  "S", Target: INamespaceSymbol {ContainingNamespace.IsGlobalNamespace:  true, Name: NameOf(System) } }))
            'Assert.True(scopes.Single().Aliases.Any(a >= a Is {Name:  "M", Target: INamespaceSymbol {ContainingNamespace.IsGlobalNamespace:  true, Name: NameOf(Microsoft) } }))
            'Assert.True(scopes.Single().Aliases.Any(a >= a.DeclaringSyntaxReferences.Single().GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:  NameOf(System) } }))
            'Assert.True(scopes.Single().Aliases.Any(a >= a.DeclaringSyntaxReferences.Single().GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:  NameOf(Microsoft) } }))
            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAliasNestedNamespaceOuterPosition()
            dim Text = "
using S = System

class C
{
    'pos
}

namespace N
{
    using M = Microsoft
}
"
            dim tree = Parse(Text)
            dim comp = CreateCompilation(tree)
            dim model = comp.GetSemanticModel(tree)
            dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)
            Assert.Single(scopes.Single().Aliases)
            'Assert.True(scopes.Single().Aliases.Single() Is {Name:  "S", Target: INamespaceSymbol {ContainingNamespace.IsGlobalNamespace:  true, Name: NameOf(System) } })
            'Assert.True(scopes.Single().Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() Is UsingDirectiveSyntax { Name: IdentifierNameSyntax {Identifier.Text:  NameOf(System) } })
        End Sub

        <Fact>
        Public Sub TestAliasNestedNamespaceInnerPosition()
            dim text = "
using S = System

namespace N
{
    using M = Microsoft
    class C
    {
        'pos
    }
}
"
            dim tree = Parse(text)
            dim comp = CreateCompilation(tree)
            dim model = comp.GetSemanticModel(tree)
            dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Equal(2, scopes.Length)
            'Assert.Single(scopes[0].Aliases)
            'Assert.Single(scopes[1].Aliases)
            'Assert.True(scopes[0].Aliases.Single() is { Name: "M", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(Microsoft) } })
            'Assert.True(scopes[0].Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(Microsoft) } })
            'Assert.True(scopes[1].Aliases.Single() is { Name: "S", Target: INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true, Name: nameof(System) } })
            'Assert.True(scopes[1].Aliases.Single().DeclaringSyntaxReferences.Single().GetSyntax() is UsingDirectiveSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(System) } })
        End Sub

#End Region

    End Class
End Namespace
