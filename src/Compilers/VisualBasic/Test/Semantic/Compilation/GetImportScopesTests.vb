' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GetImportScopesTests
        Inherits SemanticModelTestBase

        Private Shared Function GetImportScopes(text As String) As ImmutableArray(Of IImportScope)
            Dim tree = Parse(text)
            Dim comp = CreateCompilation(tree)
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Return scopes
        End Function

        <Fact>
        Public Sub TestEmptyFile()
            Dim text = "'pos"
            Dim scopes = GetImportScopes(text)
            Assert.Empty(scopes)
        End Sub

        <Fact>
        Public Sub TestNoImportsBeforeMemberDeclaration()
            Dim Text = "'pos
class C
end class"
            Dim scopes = GetImportScopes(Text)
            Assert.Empty(scopes)
        End Sub

        Private Shared Function IsNamespaceWithName(symbol As INamespaceOrTypeSymbol, name As String) As Boolean
            Return TryCast(symbol, INamespaceSymbol)?.Name = name
        End Function

        Private Shared Function IsAliasWithName(symbol As IAliasSymbol, aliasName As String, targetName As String, inGlobalNamespace As Boolean) As Boolean
            Return symbol.Name = aliasName AndAlso symbol.Target.Name = targetName AndAlso TypeOf symbol.Target Is INamespaceSymbol AndAlso DirectCast(symbol.Target, INamespaceSymbol).ContainingNamespace.IsGlobalNamespace = inGlobalNamespace
        End Function

        Private Shared Function IsSimpleImportsClauseWithName(declaringSyntaxReference As SyntaxReference, name As String) As Boolean
            Dim syntax = declaringSyntaxReference.GetSyntax()
            Return TypeOf syntax Is IdentifierNameSyntax AndAlso
                TypeOf syntax.Parent Is SimpleImportsClauseSyntax AndAlso
                name = DirectCast(syntax, IdentifierNameSyntax).Identifier.Text
        End Function

        Private Shared Function IsAliasImportsClauseWithName(aliasSymbol As IAliasSymbol, name As String) As Boolean
            Dim syntax = aliasSymbol.DeclaringSyntaxReferences.Single().GetSyntax()
            Return TypeOf syntax Is SimpleImportsClauseSyntax AndAlso DirectCast(syntax, SimpleImportsClauseSyntax).Alias.Identifier.Text = name
        End Function

#Region "normal Imports"

        <Fact>
        Public Sub TestBeforeImports()
            Dim Text = "'pos
imports System"
            Dim scopes = GetImportScopes(Text)
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
            Dim scopes = GetImportScopes(Text)
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
            Dim scopes = GetImportScopes(Text)
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
            Dim scopes = GetImportScopes(Text)
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
            Dim scopes = GetImportScopes(Text)
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
            Dim scopes = GetImportScopes(Text)
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
            Dim scopes = GetImportScopes(Text)
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
            Dim Text = "'pos
imports S = System"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Single(scopes.Single().Aliases)
            Assert.True(IsAliasWithName(scopes.Single.Aliases().Single(), "S", NameOf(System), inGlobalNamespace:=True))
            Assert.True(IsAliasImportsClauseWithName(scopes.Single.Aliases().Single(), "S"))

            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterAliasNoContent()
            Dim Text = "
imports S = System
'pos"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Single(scopes.Single().Aliases)
            Assert.True(IsAliasWithName(scopes.Single.Aliases().Single(), "S", NameOf(System), inGlobalNamespace:=True))
            Assert.True(IsAliasImportsClauseWithName(scopes.Single.Aliases().Single(), "S"))

            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterAliasBeforeMemberDeclaration()
            Dim Text = "
imports S = System
'pos
class C
end class
"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Single(scopes.Single().Aliases)
            Assert.True(IsAliasWithName(scopes.Single.Aliases().Single(), "S", NameOf(System), inGlobalNamespace:=True))
            Assert.True(IsAliasImportsClauseWithName(scopes.Single.Aliases().Single(), "S"))

            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAfterMultipleAliasesNoContent()
            Dim Text = "
imports S = System
imports M = Microsoft
'pos"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Equal(2, scopes.Single().Aliases.Length)

            Assert.True(scopes.Single().Aliases.Any(Function(a) IsAliasWithName(a, "S", NameOf(System), inGlobalNamespace:=True)))
            Assert.True(scopes.Single().Aliases.Any(Function(a) IsAliasImportsClauseWithName(a, "S")))
            Assert.True(scopes.Single().Aliases.Any(Function(a) IsAliasWithName(a, "M", NameOf(Microsoft), inGlobalNamespace:=True)))
            Assert.True(scopes.Single().Aliases.Any(Function(a) IsAliasImportsClauseWithName(a, "M")))

            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAliasNestedNamespaceOuterPosition()
            Dim Text = "
imports S = System

class C
    'pos
end lass

namespace N
end namespace
"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Single(scopes.Single().Aliases)
            Assert.True(IsAliasWithName(scopes.Single.Aliases().Single(), "S", NameOf(System), inGlobalNamespace:=True))
            Assert.True(IsAliasImportsClauseWithName(scopes.Single.Aliases().Single(), "S"))

            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

        <Fact>
        Public Sub TestAliasNestedNamespaceInnerPosition()
            Dim text = "
imports S = System

namespace N
    class C
        'pos
    end class
end namespace
"
            Dim scopes = GetImportScopes(text)

            Assert.Single(scopes)

            Assert.Single(scopes.Single().Aliases)
            Assert.True(IsAliasWithName(scopes.Single.Aliases().Single(), "S", NameOf(System), inGlobalNamespace:=True))
            Assert.True(IsAliasImportsClauseWithName(scopes.Single.Aliases().Single(), "S"))

            Assert.Empty(scopes.Single().Imports)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().XmlNamespaces)
        End Sub

#End Region

        ' Imports <xmlns:r1="http://roslyn">

#Region "xml namespace"

        <Fact>
        Public Sub TestBeforeXmlNamespace()
            Dim Text = "'pos
Imports <xmlns:r1=""http://roslyn"">"
            Dim scopes = GetImportScopes(Text)

            Assert.Single(scopes)

            Assert.Single(scopes.Single().XmlNamespaces)
            Assert.Equal("http://roslyn", scopes.Single().XmlNamespaces.Single().XmlNamespace)
            Assert.True(TypeOf scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.Equal("<xmlns:r1=""http://roslyn"">", scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax().ToString())

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().Imports)
        End Sub

        <Fact>
        Public Sub TestAfterXmlNamespaceNoContent()
            Dim Text = "
Imports <xmlns:r1=""http://roslyn"">
'pos"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Single(scopes.Single().XmlNamespaces)
            Assert.Equal("http://roslyn", scopes.Single().XmlNamespaces.Single().XmlNamespace)
            Assert.True(TypeOf scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.Equal("<xmlns:r1=""http://roslyn"">", scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax().ToString())

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().Imports)
        End Sub

        <Fact>
        Public Sub TestAfterXmlNamespaceBeforeMemberDeclaration()
            Dim Text = "
Imports <xmlns:r1=""http://roslyn"">
'pos
class C
end class
"
            Dim scopes = GetImportScopes(Text)

            Assert.Single(scopes)

            Assert.Single(scopes.Single().XmlNamespaces)
            Assert.Equal("http://roslyn", scopes.Single().XmlNamespaces.Single().XmlNamespace)
            Assert.True(TypeOf scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.Equal("<xmlns:r1=""http://roslyn"">", scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax().ToString())

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().Imports)
        End Sub

        <Fact>
        Public Sub TestAfterMultipleXmlNamespacesNoContent()
            Dim Text = "
Imports <xmlns:r1=""http://roslyn"">
Imports <xmlns:r2=""http://roslyn2"">
'pos"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Equal(2, scopes.Single().XmlNamespaces.Length)

            Assert.True(scopes.Single().XmlNamespaces.Any(Function(x) x.XmlNamespace = "http://roslyn"))
            Assert.True(TypeOf scopes.Single().XmlNamespaces.First().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.True(scopes.Single().XmlNamespaces.Any(Function(x) x.DeclaringSyntaxReference.GetSyntax().ToString() = "<xmlns:r1=""http://roslyn"">"))

            Assert.True(scopes.Single().XmlNamespaces.Any(Function(X) X.XmlNamespace = "http://roslyn2"))
            Assert.True(TypeOf scopes.Single().XmlNamespaces.Last().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.True(scopes.Single().XmlNamespaces.Any(Function(x) x.DeclaringSyntaxReference.GetSyntax().ToString() = "<xmlns:r2=""http://roslyn2"">"))

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().Imports)
        End Sub

        <Fact>
        Public Sub TestXmlNamespaceNestedNamespaceOuterPosition()
            Dim Text = "
Imports <xmlns:r1=""http://roslyn"">

class C
    'pos
end class

namespace N
    class D
    end class
end namespace
"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Single(scopes.Single().XmlNamespaces)
            Assert.Equal("http://roslyn", scopes.Single().XmlNamespaces.Single().XmlNamespace)
            Assert.True(TypeOf scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.Equal("<xmlns:r1=""http://roslyn"">", scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax().ToString())

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().Imports)
        End Sub

        <Fact>
        Public Sub TestXmlNamespaceNestedNamespaceInnerPosition()
            Dim Text = "
Imports <xmlns:r1=""http://roslyn"">

namespace N
    class C
        'pos
    end class
end namespace
"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Single(scopes.Single().XmlNamespaces)
            Assert.Equal("http://roslyn", scopes.Single().XmlNamespaces.Single().XmlNamespace)
            Assert.True(TypeOf scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.Equal("<xmlns:r1=""http://roslyn"">", scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax().ToString())

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().Imports)
        End Sub

        <Fact>
        Public Sub TestXmlNamespaceNestedNamespaceInnerPositionIntermediaryEmptyNamespace()
            Dim Text = "
Imports <xmlns:r1=""http://roslyn"">

namespace Outer
    namespace N
        class C
            'pos
        end class
    end namespace
end namespace
"
            Dim scopes = GetImportScopes(Text)
            Assert.Single(scopes)

            Assert.Single(scopes.Single().XmlNamespaces)
            Assert.Equal("http://roslyn", scopes.Single().XmlNamespaces.Single().XmlNamespace)
            Assert.True(TypeOf scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.Equal("<xmlns:r1=""http://roslyn"">", scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax().ToString())

            Assert.Empty(scopes.Single().Aliases)
            Assert.Empty(scopes.Single().ExternAliases)
            Assert.Empty(scopes.Single().Imports)
        End Sub

#End Region

#Region "global imports"

        Public Shared Function GetGlobalImportsOptions(ParamArray values As String()) As VisualBasicCompilationOptions
            Return TestOptions.ReleaseDll.WithGlobalImports(
                values.Select(Function(v) GlobalImport.Parse(v)))
        End Function

        <Fact>
        Public Sub TestEmptyFile_WithGlobalImports()
            Dim Text = "
'pos"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree, options:=GetGlobalImportsOptions("System", "M = Microsoft", "<xmlns:r1=""http://roslyn"">"))
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))

            Assert.Single(scopes)

            Assert.Single(scopes.Single().Aliases)
            Assert.True(IsAliasWithName(scopes.Single().Aliases.Single(), "M", "Microsoft", inGlobalNamespace:=True))
            Assert.Empty(scopes.Single.Aliases().Single().DeclaringSyntaxReferences)

            Assert.Single(scopes.Single().Imports)
            Assert.True(IsNamespaceWithName(scopes.Single().Imports.Single().NamespaceOrType, "System"))
            Assert.Null(scopes.Single().Imports.Single.DeclaringSyntaxReference)

            Assert.Single(scopes.Single().XmlNamespaces)
            Assert.Equal("http://roslyn", scopes.Single().XmlNamespaces.Single().XmlNamespace)
            Assert.Null(scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference)

            Assert.Empty(scopes.Single().ExternAliases)
        End Sub

        <Fact>
        Public Sub TestInsideDeclaration_WithGlobalImports()
            Dim Text = "
class C
    'pos
end class"
            Dim tree = Parse(Text)
            Dim comp = CreateCompilation(tree, options:=GetGlobalImportsOptions("System", "M = Microsoft", "<xmlns:r1=""http://roslyn"">"))
            Dim model = comp.GetSemanticModel(tree)
            Dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))
            Assert.Single(scopes)

            Assert.Single(scopes.Single().Aliases)
            Assert.True(IsAliasWithName(scopes.Single().Aliases.Single(), "M", "Microsoft", inGlobalNamespace:=True))
            Assert.Empty(scopes.Single.Aliases().Single().DeclaringSyntaxReferences)

            Assert.Single(scopes.Single().Imports)
            Assert.True(IsNamespaceWithName(scopes.Single().Imports.Single().NamespaceOrType, "System"))
            Assert.Null(scopes.Single().Imports.Single.DeclaringSyntaxReference)

            Assert.Single(scopes.Single().XmlNamespaces)
            Assert.Equal("http://roslyn", scopes.Single().XmlNamespaces.Single().XmlNamespace)
            Assert.Null(scopes.Single().XmlNamespaces.Single().DeclaringSyntaxReference)

            Assert.Empty(scopes.Single().ExternAliases)
        End Sub

        <Fact>
        Public Sub TestGlobalImportsAndFileImports()
            Dim text = "
imports System.IO
imports T = System.Threading
imports <xmlns:r2=""http://roslyn2"">

class C
    'pos
end class
"
            Dim tree = Parse(text)
            Dim comp = CreateCompilation(tree, options:=GetGlobalImportsOptions("System", "M = Microsoft", "<xmlns:r1=""http://roslyn"">"))
            Dim model = comp.GetSemanticModel(tree)
            dim scopes = model.GetImportScopes(FindPositionFromText(tree, "'pos"))

            Assert.Equal(2, scopes.Length)

            Assert.Single(scopes(0).Aliases)
            Assert.True(IsAliasWithName(scopes(0).Aliases.Single(), "T", "Threading", inGlobalNamespace:=False))
            Assert.True(IsAliasImportsClauseWithName(scopes(0).Aliases().Single(), "T"))

            Assert.Single(scopes(0).Imports)
            Assert.True(IsNamespaceWithName(scopes(0).Imports.Single().NamespaceOrType, "IO"))
            Dim syntax = scopes(0).Imports.Single.DeclaringSyntaxReference.GetSyntax()
            Assert.True(TypeOf syntax Is QualifiedNameSyntax)
            Assert.True(TypeOf syntax.Parent Is SimpleImportsClauseSyntax)
            Assert.Equal("System.IO", syntax.ToString())

            Assert.Single(scopes(0).XmlNamespaces)
            Assert.Equal("http://roslyn2", scopes(0).XmlNamespaces.Single().XmlNamespace)
            Assert.True(TypeOf scopes(0).XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax() Is XmlNamespaceImportsClauseSyntax)
            Assert.Equal("<xmlns:r2=""http://roslyn2"">", scopes(0).XmlNamespaces.Single().DeclaringSyntaxReference.GetSyntax().ToString())

            Assert.Single(scopes(1).Aliases)
            Assert.True(IsAliasWithName(scopes(1).Aliases.Single(), "M", "Microsoft", inGlobalNamespace:=True))
            Assert.Empty(scopes(1).Aliases().Single().DeclaringSyntaxReferences)

            Assert.Single(scopes(1).Imports)
            Assert.True(IsNamespaceWithName(scopes(1).Imports.Single().NamespaceOrType, "System"))
            Assert.Null(scopes(1).Imports.Single.DeclaringSyntaxReference)

            Assert.Single(scopes(1).XmlNamespaces)
            Assert.Equal("http://roslyn", scopes(1).XmlNamespaces.Single().XmlNamespace)
            Assert.Null(scopes(1).XmlNamespaces.Single().DeclaringSyntaxReference)

            Assert.Empty(scopes(0).ExternAliases)
            Assert.Empty(scopes(1).ExternAliases)
        End Sub

#End Region

    End Class
End Namespace
