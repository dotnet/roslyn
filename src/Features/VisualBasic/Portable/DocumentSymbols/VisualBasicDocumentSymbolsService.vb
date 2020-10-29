' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentSymbols
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.DocumentSymbols

    <ExportLanguageService(GetType(IDocumentSymbolsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDocumentSymbolsService
        Inherits AbstractDocumentSymbolsService

        Private Shared ReadOnly s_typeFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance)

        Private Shared ReadOnly s_memberFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
            parameterOptions:=SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared ReadOnly s_constantTag As ImmutableArray(Of String) = ImmutableArray.Create(WellKnownTags.Constant)

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
            If options = DocumentSymbolsOptions.TypesAndMembersOnly Then
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

        Protected Overrides Function GetMemberInfoForType(type As INamedTypeSymbol, tree As SyntaxTree, declarationService As ISymbolDeclarationService, cancellationToken As CancellationToken) As DocumentSymbolInfo
            If type.TypeKind = TypeKind.Enum Then
                Return GetInfoForEnum(type, tree, declarationService, cancellationToken)
            Else
                Return GetInfoForNamedType(type, tree, declarationService, cancellationToken)
            End If
        End Function

        Private Function GetInfoForEnum(type As INamedTypeSymbol, syntaxTree As SyntaxTree, declarationService As ISymbolDeclarationService, cancellationToken As CancellationToken) As DocumentSymbolInfo
            Dim members = type.GetMembers().SelectAsArray(
                Function(member) member.IsShared AndAlso member.Kind = SymbolKind.Field,
                Function(member) CreateInfo(member, syntaxTree, declarationService, ImmutableArray(Of DocumentSymbolInfo).Empty, cancellationToken))

            Return CreateInfo(type, syntaxTree, declarationService, members.Sort(Function(d1, d2) d1.Text.CompareTo(d2.Text)), cancellationToken)
        End Function

        Private Function GetInfoForNamedType(type As INamedTypeSymbol, syntaxTree As SyntaxTree, declarationService As ISymbolDeclarationService, cancellationToken As CancellationToken) As DocumentSymbolInfo
            Dim membersBuilder = ArrayBuilder(Of DocumentSymbolInfo).GetInstance()

            ' Don't include constructors if they're all implicitly declared
            Dim constructors = type.Constructors
            If constructors.Any(Function(c) Not c.IsImplicitlyDeclared) Then
                For Each c In constructors.OrderBy(Function(c1) c1.ToDisplayString(s_memberFormat))
                    If c.IsImplicitlyDeclared Then
                        Continue For
                    End If

                    membersBuilder.Add(CreateInfo(c, syntaxTree, declarationService, ImmutableArray(Of DocumentSymbolInfo).Empty, cancellationToken))
                Next
            End If

            ' Get any of the methods named "Finalize" in this class, and list them first. The legacy
            ' behavior that we will consider a method a finalizer even if it is shadowing the real
            ' Finalize method instead of overriding it, so this code is actually correct!
            Dim finalizeMethods = type.GetMembers(WellKnownMemberNames.DestructorName)
            If finalizeMethods.Any() Then
                For Each f In finalizeMethods
                    membersBuilder.Add(CreateInfo(f, syntaxTree, declarationService, ImmutableArray(Of DocumentSymbolInfo).Empty, cancellationToken))
                Next
            End If

            If type.TypeKind <> TypeKind.Delegate Then
                Dim memberGroups = type.GetMembers().Where(AddressOf IncludeMember).
                                                     GroupBy(Function(m) m.Name, CaseInsensitiveComparison.Comparer).
                                                     OrderBy(Function(g) g.Key)

                For Each memberGroup In memberGroups
                    If Not CaseInsensitiveComparison.Equals(memberGroup.Key, WellKnownMemberNames.DestructorName) Then
                        For Each member In memberGroup.OrderBy(Function(m) m.ToDisplayString(s_memberFormat))
                            membersBuilder.Add(CreateInfo(member, syntaxTree, declarationService, ImmutableArray(Of DocumentSymbolInfo).Empty, cancellationToken))
                        Next
                    End If
                Next
            End If

            Return CreateInfo(type, syntaxTree, declarationService, membersBuilder.ToImmutableAndFree(), cancellationToken)

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

        Protected Overrides Function CreateInfo(
            symbol As ISymbol,
            tree As SyntaxTree,
            declarationService As ISymbolDeclarationService,
            childrenSymbols As ImmutableArray(Of DocumentSymbolInfo), cancellationToken As CancellationToken) As DocumentSymbolInfo

            Dim enclosingSpans = GetEnclosingSpansForSymbol(symbol, tree, declarationService)
            Dim declaringSpans = GetDeclaringSpans(symbol, tree)

            Dim text = If(TypeOf symbol Is ITypeSymbol,
                symbol.ToDisplayString(s_typeFormat),
                symbol.ToDisplayString(s_memberFormat))

            Dim isConstant = False

            If symbol.Kind = SymbolKind.Field Then
                isConstant = DirectCast(symbol, IFieldSymbol).IsConst
            ElseIf symbol.Kind = SymbolKind.Local Then
                isConstant = DirectCast(symbol, ILocalSymbol).IsConst
            End If

            Dim obsolete = symbol.GetAttributes().Any(Function(attr) attr.AttributeClass?.MetadataName = "ObsoleteAttribute")
            Dim name = symbol.Name

            Return New DocumentSymbolInfo(
                text,
                name,
                symbol.GetGlyph(),
                obsolete,
                If(isConstant, s_constantTag, ImmutableArray(Of String).Empty),
                ImmutableDictionary(Of String, String).Empty,
                enclosingSpans,
                declaringSpans,
                childrenSymbols)
        End Function

        Private Shared Function GetEnclosingSpansForSymbol(
                symbol As ISymbol,
            tree As SyntaxTree,
            declarationService As ISymbolDeclarationService) As ImmutableArray(Of TextSpan)
            Return declarationService.GetDeclarations(symbol).SelectAsArray(Function(s) s.SyntaxTree.Equals(tree),
                                                                            Function(s) s.GetSyntax().FullSpan)
        End Function
    End Class

End Namespace
