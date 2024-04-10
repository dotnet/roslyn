' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class VisualBasicSemanticFacts
        Implements ISemanticFacts

        Public Shared ReadOnly Instance As New VisualBasicSemanticFacts()

        Private Sub New()
        End Sub

        Public ReadOnly Property SyntaxFacts As ISyntaxFacts Implements ISemanticFacts.SyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Public ReadOnly Property SupportsImplicitInterfaceImplementation As Boolean Implements ISemanticFacts.SupportsImplicitInterfaceImplementation
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property ExposesAnonymousFunctionParameterNames As Boolean Implements ISemanticFacts.ExposesAnonymousFunctionParameterNames
            Get
                Return True
            End Get
        End Property

        Public Function IsOnlyWrittenTo(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.IsOnlyWrittenTo
            Return TryCast(node, ExpressionSyntax).IsOnlyWrittenTo()
        End Function

        Public Function IsWrittenTo(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.IsWrittenTo
            Return TryCast(node, ExpressionSyntax).IsWrittenTo(semanticModel, cancellationToken)
        End Function

        Public Function IsInOutContext(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.IsInOutContext
            Return TryCast(node, ExpressionSyntax).IsInOutContext()
        End Function

        Public Function IsInRefContext(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.IsInRefContext
            Return TryCast(node, ExpressionSyntax).IsInRefContext(semanticModel, cancellationToken)
        End Function

        Public Function IsInInContext(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.IsInInContext
            Return TryCast(node, ExpressionSyntax).IsInInContext()
        End Function

        Public Function CanReplaceWithRValue(semanticModel As SemanticModel, expression As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.CanReplaceWithRValue
            Return TryCast(expression, ExpressionSyntax).CanReplaceWithRValue(semanticModel, cancellationToken)
        End Function

        Public Function GetDeclaredSymbol(semanticModel As SemanticModel, token As SyntaxToken, cancellationToken As CancellationToken) As ISymbol Implements ISemanticFacts.GetDeclaredSymbol
            Dim location = token.GetLocation()

            For Each ancestor In token.GetAncestors(Of SyntaxNode)()
                If Not TypeOf ancestor Is AggregationRangeVariableSyntax AndAlso
                   Not TypeOf ancestor Is CollectionRangeVariableSyntax AndAlso
                   Not TypeOf ancestor Is ExpressionRangeVariableSyntax AndAlso
                   Not TypeOf ancestor Is InferredFieldInitializerSyntax Then

                    Dim symbol = semanticModel.GetDeclaredSymbol(ancestor, cancellationToken)

                    If symbol IsNot Nothing Then
                        If symbol.Locations.Contains(location) Then
                            Return symbol
                        End If

                        ' We found some symbol, but it defined something else. We're not going to have a higher node defining _another_ symbol with this token, so we can stop now.
                        Return Nothing
                    End If

                    ' If we hit an executable statement syntax and didn't find anything yet, we can just stop now -- anything higher would be a member declaration which won't be defined by something inside a statement.
                    If VisualBasicSyntaxFacts.Instance.IsExecutableStatement(ancestor) Then
                        Return Nothing
                    End If
                End If
            Next

            Return Nothing
        End Function

        Public Function LastEnumValueHasInitializer(namedTypeSymbol As INamedTypeSymbol) As Boolean Implements ISemanticFacts.LastEnumValueHasInitializer
            Dim enumStatement = namedTypeSymbol.DeclaringSyntaxReferences.Select(Function(r) r.GetSyntax()).OfType(Of EnumStatementSyntax).FirstOrDefault()
            If enumStatement IsNot Nothing Then
                Dim enumBlock = DirectCast(enumStatement.Parent, EnumBlockSyntax)

                Dim lastMember = TryCast(enumBlock.Members.LastOrDefault(), EnumMemberDeclarationSyntax)
                If lastMember IsNot Nothing Then
                    Return lastMember.Initializer IsNot Nothing
                End If
            End If

            Return False
        End Function

        Public ReadOnly Property SupportsParameterizedProperties As Boolean = True Implements ISemanticFacts.SupportsParameterizedProperties

        Public Function TryGetSpeculativeSemanticModel(oldSemanticModel As SemanticModel, oldNode As SyntaxNode, newNode As SyntaxNode, <Out> ByRef speculativeModel As SemanticModel) As Boolean Implements ISemanticFacts.TryGetSpeculativeSemanticModel
            Debug.Assert(oldNode.Kind = newNode.Kind)

            Dim model = oldSemanticModel

            ' currently we only support method. field support will be added later.
            Dim oldMethod = TryCast(oldNode, MethodBlockBaseSyntax)
            Dim newMethod = TryCast(newNode, MethodBlockBaseSyntax)
            If oldMethod Is Nothing OrElse newMethod Is Nothing Then
                speculativeModel = Nothing
                Return False
            End If

            ' No method body?
            If oldMethod.Statements.IsEmpty AndAlso oldMethod.EndBlockStatement.IsMissing Then
                speculativeModel = Nothing
                Return False
            End If

            Dim position As Integer
            If model.IsSpeculativeSemanticModel Then
                ' Chaining speculative semantic model is not supported, use the original model.
                position = model.OriginalPositionForSpeculation
                model = model.ParentModel
                Contract.ThrowIfNull(model)
                Contract.ThrowIfTrue(model.IsSpeculativeSemanticModel)
            Else
                position = oldMethod.BlockStatement.FullSpan.End
            End If

            Dim vbSpeculativeModel As SemanticModel = Nothing
            Dim success = model.TryGetSpeculativeSemanticModelForMethodBody(position, newMethod, vbSpeculativeModel)
            speculativeModel = vbSpeculativeModel
            Return success
        End Function

        Public Function GetAliasNameSet(model As SemanticModel, cancellationToken As CancellationToken) As ImmutableHashSet(Of String) Implements ISemanticFacts.GetAliasNameSet
            Dim original = model.GetOriginalSemanticModel()

            If Not original.SyntaxTree.HasCompilationUnitRoot Then
                Return ImmutableHashSet.Create(Of String)()
            End If

            Dim root = original.SyntaxTree.GetCompilationUnitRoot()

            Dim builder = ImmutableHashSet.CreateBuilder(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each globalImport In original.Compilation.AliasImports
                globalImport.Name.AppendToAliasNameSet(builder)
            Next

            For Each importsClause In root.GetAliasImportsClauses()
                importsClause.Alias.Identifier.ValueText.AppendToAliasNameSet(builder)
            Next

            Return builder.ToImmutable()
        End Function

        Public Function GetForEachSymbols(model As SemanticModel, forEachStatement As SyntaxNode) As ForEachSymbols Implements ISemanticFacts.GetForEachSymbols
            Dim vbForEachStatement = TryCast(forEachStatement, ForEachStatementSyntax)
            If vbForEachStatement IsNot Nothing Then
                Dim info = model.GetForEachStatementInfo(vbForEachStatement)
                Return New ForEachSymbols(
                    info.GetEnumeratorMethod,
                    info.MoveNextMethod,
                    info.CurrentProperty,
                    info.DisposeMethod,
                    info.ElementType)
            End If

            Dim vbForBlock = TryCast(forEachStatement, ForEachBlockSyntax)
            If vbForBlock IsNot Nothing Then
                Dim info = model.GetForEachStatementInfo(vbForBlock)
                Return New ForEachSymbols(
                    info.GetEnumeratorMethod,
                    info.MoveNextMethod,
                    info.CurrentProperty,
                    info.DisposeMethod,
                    info.ElementType)
            End If

            Return Nothing
        End Function

        Public Function GetCollectionInitializerSymbolInfo(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As SymbolInfo Implements ISemanticFacts.GetCollectionInitializerSymbolInfo
            Return semanticModel.GetCollectionInitializerSymbolInfo(DirectCast(node, ExpressionSyntax), cancellationToken)
        End Function

        Public Function GetGetAwaiterMethod(model As SemanticModel, node As SyntaxNode) As IMethodSymbol Implements ISemanticFacts.GetGetAwaiterMethod
            If node.IsKind(SyntaxKind.AwaitExpression) Then
                Dim awaitExpression = DirectCast(node, AwaitExpressionSyntax)
                Dim info = model.GetAwaitExpressionInfo(awaitExpression)
                Return info.GetAwaiterMethod
            End If

            Return Nothing
        End Function

        Public Function GetDeconstructionAssignmentMethods(model As SemanticModel, deconstruction As SyntaxNode) As ImmutableArray(Of IMethodSymbol) Implements ISemanticFacts.GetDeconstructionAssignmentMethods
            Return ImmutableArray(Of IMethodSymbol).Empty
        End Function

        Public Function GetDeconstructionForEachMethods(model As SemanticModel, deconstruction As SyntaxNode) As ImmutableArray(Of IMethodSymbol) Implements ISemanticFacts.GetDeconstructionForEachMethods
            Return ImmutableArray(Of IMethodSymbol).Empty
        End Function

        Public Function IsPartial(typeSymbol As INamedTypeSymbol, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.IsPartial
            Dim syntaxRefs = typeSymbol.DeclaringSyntaxReferences
            Return syntaxRefs.Any(
                Function(n As SyntaxReference)
                    Return DirectCast(n.GetSyntax(cancellationToken), TypeStatementSyntax).Modifiers.Any(SyntaxKind.PartialKeyword)
                End Function)
        End Function

        Public Function GetDeclaredSymbols(semanticModel As SemanticModel, memberDeclaration As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of ISymbol) Implements ISemanticFacts.GetDeclaredSymbols
            If TypeOf memberDeclaration Is FieldDeclarationSyntax Then
                Return DirectCast(memberDeclaration, FieldDeclarationSyntax).Declarators.
                    SelectMany(Function(d) d.Names.AsEnumerable()).
                    Select(Function(n) semanticModel.GetDeclaredSymbol(n, cancellationToken))
            End If

            Return SpecializedCollections.SingletonEnumerable(semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken))
        End Function

        Public Function FindParameterForArgument(semanticModel As SemanticModel, argument As SyntaxNode, allowUncertainCandidates As Boolean, allowParams As Boolean, cancellationToken As CancellationToken) As IParameterSymbol Implements ISemanticFacts.FindParameterForArgument
            Return DirectCast(argument, ArgumentSyntax).DetermineParameter(semanticModel, allowUncertainCandidates, allowParams, cancellationToken)
        End Function

        Public Function FindParameterForAttributeArgument(semanticModel As SemanticModel, argument As SyntaxNode, allowUncertainCandidates As Boolean, allowParams As Boolean, cancellationToken As CancellationToken) As IParameterSymbol Implements ISemanticFacts.FindParameterForAttributeArgument
            Return Nothing
        End Function

        Public Function FindFieldOrPropertyForArgument(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As ISymbol Implements ISemanticFacts.FindFieldOrPropertyForArgument
            Dim argument = TryCast(node, SimpleArgumentSyntax)
            If argument?.NameColonEquals IsNot Nothing AndAlso
               TypeOf argument.Parent Is ArgumentListSyntax AndAlso
               TypeOf argument.Parent.Parent Is AttributeSyntax Then

                Dim symbol = semanticModel.GetSymbolInfo(argument.NameColonEquals.Name, cancellationToken).GetAnySymbol()
                If symbol?.Kind = SymbolKind.Field OrElse symbol?.Kind = SymbolKind.Property Then
                    Return symbol
                End If
            End If

            Return Nothing
        End Function

        Public Function FindFieldOrPropertyForAttributeArgument(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As ISymbol Implements ISemanticFacts.FindFieldOrPropertyForAttributeArgument
            Return Nothing
        End Function

        Public Function GetBestOrAllSymbols(semanticModel As SemanticModel, node As SyntaxNode, token As SyntaxToken, cancellationToken As CancellationToken) As ImmutableArray(Of ISymbol) Implements ISemanticFacts.GetBestOrAllSymbols
            Return If(node Is Nothing,
                      ImmutableArray(Of ISymbol).Empty,
                      semanticModel.GetSymbolInfo(node, cancellationToken).GetBestOrAllSymbols())
        End Function

        Public Function IsInsideNameOfExpression(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.IsInsideNameOfExpression
            Return node.FirstAncestorOrSelf(Of NameOfExpressionSyntax) IsNot Nothing
        End Function

        Public Function GetLocalFunctionSymbols(compilation As Compilation, symbol As ISymbol, cancellationToken As CancellationToken) As ImmutableArray(Of IMethodSymbol) Implements ISemanticFacts.GetLocalFunctionSymbols
            Return ImmutableArray(Of IMethodSymbol).Empty
        End Function

        Public Function IsInExpressionTree(semanticModel As SemanticModel, node As SyntaxNode, expressionTypeOpt As INamedTypeSymbol, cancellationToken As CancellationToken) As Boolean Implements ISemanticFacts.IsInExpressionTree
            Return node.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken)
        End Function

        Public Function GenerateNameForExpression(semanticModel As SemanticModel,
                                                  expression As SyntaxNode,
                                                  capitalize As Boolean,
                                                  cancellationToken As CancellationToken) As String Implements ISemanticFacts.GenerateNameForExpression
            Return semanticModel.GenerateNameForExpression(
                DirectCast(expression, ExpressionSyntax), capitalize, cancellationToken)
        End Function
    End Class
End Namespace
