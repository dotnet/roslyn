' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.FindSymbols

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.SolutionExplorer
    <ExportLanguageService(GetType(ISolutionExplorerSymbolTreeItemProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSolutionExplorerSymbolTreeItemProvider
        Inherits AbstractSolutionExplorerSymbolTreeItemProvider(Of
            CompilationUnitSyntax,
            StatementSyntax,
            NamespaceBlockSyntax,
            EnumBlockSyntax,
            TypeBlockSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetMembers(root As CompilationUnitSyntax) As SyntaxList(Of StatementSyntax)
            Return root.Members
        End Function

        Protected Overrides Function GetMembers(baseNamespace As NamespaceBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return baseNamespace.Members
        End Function

        Protected Overrides Function GetMembers(typeDeclaration As TypeBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return typeDeclaration.Members
        End Function

        Protected Overrides Function TryAddType(member As StatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder, cancellationToken As CancellationToken) As Boolean
            Dim typeBlock = TryCast(member, TypeBlockSyntax)
            If typeBlock IsNot Nothing Then
                AddTypeBlock(typeBlock, items, nameBuilder, cancellationToken)
                Return True
            End If

            Dim enumBlock = TryCast(member, EnumBlockSyntax)
            If enumBlock IsNot Nothing Then
                AddEnumBlock(enumBlock, items, nameBuilder, cancellationToken)
                Return True
            End If

            Dim delegateStatement = TryCast(member, DelegateStatementSyntax)
            If delegateStatement IsNot Nothing Then
                AddDelegateStatement(delegateStatement, items, nameBuilder, cancellationToken)
                Return True
            End If

            Return False
        End Function

        Private Shared Sub AddEnumBlock(enumBlock As EnumBlockSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder, cancellationToken As CancellationToken)
            nameBuilder.Append(enumBlock.EnumStatement.Identifier.ValueText)

            Dim accessibility = GetAccessibility(enumBlock.Parent, enumBlock.EnumStatement, enumBlock.EnumStatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Enum, accessibility),
                hasItems:=False,
                enumBlock,
                enumBlock.EnumStatement.Identifier))
        End Sub

        Private Shared Sub AddDelegateStatement(delegateStatement As DelegateStatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder, cancellationToken As CancellationToken)
            nameBuilder.Append(delegateStatement.Identifier.ValueText)
            AppendTypeParameterList(nameBuilder, delegateStatement.TypeParameterList)
            AppendParameterList(nameBuilder, delegateStatement.ParameterList)
            AppendAsClause(nameBuilder, delegateStatement.AsClause)

            Dim accessibility = GetAccessibility(delegateStatement.Parent, delegateStatement, delegateStatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Delegate, accessibility),
                hasItems:=False,
                delegateStatement,
                delegateStatement.Identifier))
        End Sub

        Protected Overrides Sub AddEnumDeclarationMembers(enumDeclaration As EnumBlockSyntax, items As ArrayBuilder(Of SymbolTreeItemData), cancellationToken As CancellationToken)
            For Each member In enumDeclaration.Members
                Dim enumMember = TryCast(member, EnumMemberDeclarationSyntax)
                items.Add(New SymbolTreeItemData(
                enumMember.Identifier.ValueText,
                Glyph.EnumMemberPublic,
                hasItems:=False,
                enumMember,
                enumMember.Identifier))
            Next
        End Sub

        Protected Overrides Sub AddMemberDeclaration(member As StatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            Throw New NotImplementedException()
        End Sub

        Private Shared Sub AppendTypeParameterList(
            builder As StringBuilder,
            typeParameterList As TypeParameterListSyntax)

            AppendCommaSeparatedList(
            builder, "(Of ", ")", typeParameterList,
            Function(list) list.Parameters,
            Sub(parameter, innherBuilder) innherBuilder.Append(parameter.Identifier.ValueText))
        End Sub

        Private Shared Sub AppendParameterList(
            builder As StringBuilder,
            parameterList As ParameterListSyntax)

            AppendCommaSeparatedList(
                builder, "(", ")", parameterList,
                Function(list) list.Parameters,
                Sub(parameter, innerBuilder) AppendType(parameter?.AsClause?.Type, builder))
        End Sub

        Private Shared Sub AppendAsClause(nameBuilder As StringBuilder, asClause As SimpleAsClauseSyntax)
            If asClause IsNot Nothing Then
                nameBuilder.Append(" As ")
                AppendType(asClause.Type, nameBuilder)
            End If
        End Sub

        Private Shared Sub AppendType(typeSyntax As TypeSyntax, builder As StringBuilder)
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace
