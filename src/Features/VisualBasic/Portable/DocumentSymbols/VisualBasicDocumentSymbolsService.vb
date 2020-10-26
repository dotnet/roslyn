' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentSymbols
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.DocumentSymbols

    <ExportLanguageService(GetType(IDocumentSymbolsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDocumentSymbolsService
        Inherits AbstractDocumentSymbolsService
        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetSymbol(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As ISymbol
            Dim syntaxKind As SyntaxKind = node.Kind()

            If syntaxKind = SyntaxKind.NamespaceStatement OrElse
                syntaxKind = SyntaxKind.NamespaceBlock Then
                Return Nothing
            ElseIf TypeOf node Is TypeBlockSyntax OrElse
                   TypeOf node Is MethodBlockBaseSyntax OrElse
                   TypeOf node Is EnumBlockSyntax OrElse
                   TypeOf node Is EnumMemberDeclarationSyntax OrElse
                   TypeOf node Is PropertyBlockSyntax OrElse
                   TypeOf node Is PropertyStatementSyntax OrElse
                   TypeOf node Is FieldDeclarationSyntax OrElse
                   TypeOf node Is EventBlockSyntax OrElse
                   TypeOf node Is EventStatementSyntax OrElse
                   TypeOf node Is DelegateStatementSyntax OrElse
                   TypeOf node Is TypeParameterSyntax Then
                Return semanticModel.GetDeclaredSymbol(node, cancellationToken)
            ElseIf TypeOf node Is ModifiedIdentifierSyntax Then
                Dim symbol As ISymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken)
                If symbol.Kind = SymbolKind.Parameter Then
                    Return Nothing
                End If
                Return symbol
            End If

            Return Nothing
        End Function

        Protected Overrides Function ShouldSkipSyntaxChildren(node As SyntaxNode, options As DocumentSymbolsOptions) As Boolean
            ' If we're not looking for the full hierarchy, we don't care about things nested inside type members
            If options = DocumentSymbolsOptions.TypesAndMethodsOnly Then
                Return TypeOf node Is MethodBlockBaseSyntax OrElse
                       TypeOf node Is PropertyBlockSyntax OrElse
                       TypeOf node Is EventBlockSyntax OrElse
                       TypeOf node Is FieldDeclarationSyntax OrElse
                       TypeOf node Is ExecutableStatementSyntax OrElse
                       TypeOf node Is ExpressionSyntax
            End If

            ' VB doesn't declare anything inside expressions. We skip lambdas (and their locals) for simplicity
            Return TypeOf node Is ExpressionSyntax OrElse
                   TypeOf node Is AttributeListSyntax OrElse
                   TypeOf node Is TypeParameterConstraintClauseSyntax OrElse
                   TypeOf node Is EventBlockSyntax OrElse
                   TypeOf node Is PropertyBlockSyntax
        End Function

        Protected Overrides Function GetInfoForType(type As INamedTypeSymbol) As DocumentSymbolInfo
            If type.TypeKind = TypeKind.Enum Then
                Return GetInfoForEnum(type)
            Else
                Return GetInfoForNamedType(type)
            End If
        End Function

        Private Shared Function GetInfoForEnum(type As INamedTypeSymbol) As DocumentSymbolInfo
            Dim members = type.GetMembers().SelectAsArray(
                Function(member) member.IsShared AndAlso member.Kind = SymbolKind.Field,
                Function(member) DirectCast(New VisualBasicDocumentSymbolInfo(member, ImmutableArray(Of DocumentSymbolInfo).Empty), DocumentSymbolInfo))

            Return New VisualBasicDocumentSymbolInfo(type, members.Sort(Function(d1, d2) d1.Text.CompareTo(d2.Text)))
        End Function

        Private Shared Function GetInfoForNamedType(type As INamedTypeSymbol) As DocumentSymbolInfo
            Dim membersBuilder = ArrayBuilder(Of DocumentSymbolInfo).GetInstance()

            ' Don't include constructors if they're all implicitly declared
            Dim constructors = type.Constructors
            If constructors.Any(Function(c) Not c.IsImplicitlyDeclared) Then
                For Each c In constructors.OrderBy(Function(c1) c1.ToDisplayString(VisualBasicDocumentSymbolInfo.MemberFormat))
                    If c.IsImplicitlyDeclared Then
                        Continue For
                    End If

                    membersBuilder.Add(New VisualBasicDocumentSymbolInfo(c))
                Next
            End If

            ' Get any of the methods named "Finalize" in this class, and list them first. The legacy
            ' behavior that we will consider a method a finalizer even if it is shadowing the real
            ' Finalize method instead of overriding it, so this code is actually correct!
            Dim finalizeMethods = type.GetMembers(WellKnownMemberNames.DestructorName)
            If finalizeMethods.Any() Then
                For Each f In finalizeMethods
                    membersBuilder.Add(New VisualBasicDocumentSymbolInfo(f))
                Next
            End If

            If type.TypeKind <> TypeKind.Delegate Then
                Dim memberGroups = type.GetMembers().Where(AddressOf IncludeMember).
                                                     GroupBy(Function(m) m.Name, CaseInsensitiveComparison.Comparer).
                                                     OrderBy(Function(g) g.Key)

                For Each memberGroup In memberGroups
                    If Not CaseInsensitiveComparison.Equals(memberGroup.Key, WellKnownMemberNames.DestructorName) Then
                        For Each member In memberGroup.OrderBy(Function(m) m.ToDisplayString(VisualBasicDocumentSymbolInfo.MemberFormat))
                            membersBuilder.Add(New VisualBasicDocumentSymbolInfo(member))
                        Next
                    End If
                Next
            End If

            Return New VisualBasicDocumentSymbolInfo(type, membersBuilder.ToImmutableAndFree())

        End Function

        Private Shared Function IncludeMember(symbol As ISymbol) As Boolean
            If symbol.Kind = SymbolKind.Method Then
                Dim method = DirectCast(symbol, IMethodSymbol)

                Return method.MethodKind = MethodKind.Ordinary OrElse
                       method.MethodKind = MethodKind.UserDefinedOperator OrElse
                       method.MethodKind = MethodKind.Conversion
            End If

            If symbol.Kind = SymbolKind.Property Then
                Return True
            End If

            If symbol.Kind = SymbolKind.Event Then
                Return True
            End If

            If symbol.Kind = SymbolKind.Field AndAlso Not symbol.IsImplicitlyDeclared Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function ConsiderNestedNodesChildren(node As ISymbol) As Boolean
            Select Case node.Kind
                Case SymbolKind.Label, SymbolKind.Local
                    Return False
                Case Else
                    Return True
            End Select
        End Function

        Protected Overrides Function CreateInfo(symbol As ISymbol, childrenSymbols As ImmutableArray(Of DocumentSymbolInfo)) As DocumentSymbolInfo
            Return New VisualBasicDocumentSymbolInfo(symbol, childrenSymbols)
        End Function
    End Class

End Namespace
