' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageServiceFactory(GetType(ISemanticFactsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSemanticFactsServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return VisualBasicSemanticFactsService.Instance
        End Function
    End Class

    Friend Class VisualBasicSemanticFactsService
        Inherits AbstractSemanticFactsService
        Implements ISemanticFactsService

        Public Shared ReadOnly Instance As New VisualBasicSemanticFactsService()

        Protected Overrides ReadOnly Property SyntaxFactsService As ISyntaxFactsService = VisualBasicSyntaxFactsService.Instance

        Private Sub New()
        End Sub

        Public ReadOnly Property SupportsImplicitInterfaceImplementation As Boolean Implements ISemanticFactsService.SupportsImplicitInterfaceImplementation
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property ExposesAnonymousFunctionParameterNames As Boolean Implements ISemanticFactsService.ExposesAnonymousFunctionParameterNames
            Get
                Return True
            End Get
        End Property

        Public Function IsExpressionContext(semanticModel As SemanticModel,
                                            position As Integer,
                                            cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsExpressionContext
            Dim token = semanticModel.SyntaxTree.GetTargetToken(position, cancellationToken)
            Return semanticModel.SyntaxTree.IsExpressionContext(position, token, cancellationToken, semanticModel)
        End Function

        Public Function IsInExpressionTree(semanticModel As SemanticModel, node As SyntaxNode, expressionTypeOpt As INamedTypeSymbol, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsInExpressionTree
            Return node.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken)
        End Function

        Public Function IsMemberDeclarationContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsMemberDeclarationContext
            Dim token = semanticModel.SyntaxTree.GetTargetToken(position, cancellationToken)
            Return semanticModel.SyntaxTree.IsInterfaceMemberDeclarationKeywordContext(position, token, cancellationToken) OrElse
                semanticModel.SyntaxTree.IsTypeMemberDeclarationKeywordContext(position, token, cancellationToken)
        End Function

        Public Function IsNamespaceContext(semanticModel As SemanticModel,
                                           position As Integer,
                                           cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsNamespaceContext
            Dim token = semanticModel.SyntaxTree.GetTargetToken(position, cancellationToken)
            Return semanticModel.SyntaxTree.IsNamespaceContext(position, token, cancellationToken, semanticModel)
        End Function

        Public Function IsStatementContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsStatementContext
            Dim token = semanticModel.SyntaxTree.GetTargetToken(position, cancellationToken)
            Return semanticModel.SyntaxTree.IsSingleLineStatementContext(position, token, cancellationToken) OrElse
                semanticModel.SyntaxTree.IsMultiLineStatementStartContext(position, token, cancellationToken)
        End Function

        Public Function IsTypeContext(semanticModel As SemanticModel,
                                      position As Integer,
                                      cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsTypeContext
            Dim token = semanticModel.SyntaxTree.GetTargetToken(position, cancellationToken)
            Return semanticModel.SyntaxTree.IsTypeContext(position, token, cancellationToken, semanticModel)
        End Function

        Public Function IsTypeDeclarationContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsTypeDeclarationContext
            Dim token = semanticModel.SyntaxTree.GetTargetToken(position, cancellationToken)
            Return semanticModel.SyntaxTree.IsTypeDeclarationContext(position, token, cancellationToken)
        End Function

        Public Function IsPreProcessorDirectiveContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsPreProcessorDirectiveContext
            Return semanticModel.SyntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken)
        End Function

        Public Function IsGlobalStatementContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsGlobalStatementContext
            Return False
        End Function

        Public Function IsLabelContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsLabelContext
            Dim tree = semanticModel.SyntaxTree
            Dim token = tree.GetTargetToken(position, cancellationToken)
            Return tree.IsLabelContext(position, token, cancellationToken)
        End Function

        Public Function IsAttributeNameContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsAttributeNameContext
            Dim tree = semanticModel.SyntaxTree
            Dim token = tree.GetTargetToken(position, cancellationToken)
            Return tree.IsAttributeNameContext(position, token, cancellationToken)
        End Function

        Public Function IsOnlyWrittenTo(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsOnlyWrittenTo
            Return TryCast(node, ExpressionSyntax).IsOnlyWrittenTo(semanticModel, cancellationToken)
        End Function

        Public Function IsWrittenTo(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsWrittenTo
            Return TryCast(node, ExpressionSyntax).IsWrittenTo(semanticModel, cancellationToken)
        End Function

        Public Function IsInOutContext(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsInOutContext
            Return TryCast(node, ExpressionSyntax).IsInOutContext(semanticModel, cancellationToken)
        End Function

        Public Function IsInRefContext(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsInRefContext
            Return TryCast(node, ExpressionSyntax).IsInRefContext(semanticModel, cancellationToken)
        End Function

        Public Function IsInInContext(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsInInContext
            Return TryCast(node, ExpressionSyntax).IsInInContext(semanticModel, cancellationToken)
        End Function

        Public Function CanReplaceWithRValue(semanticModel As SemanticModel, expression As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.CanReplaceWithRValue
            Return TryCast(expression, ExpressionSyntax).CanReplaceWithRValue(semanticModel, cancellationToken)
        End Function

        Public Function GenerateNameForExpression(semanticModel As SemanticModel,
                                                  expression As SyntaxNode,
                                                  capitalize As Boolean,
                                                  cancellationToken As CancellationToken) As String Implements ISemanticFactsService.GenerateNameForExpression
            Return semanticModel.GenerateNameForExpression(
                DirectCast(expression, ExpressionSyntax), capitalize, cancellationToken)
        End Function

        Public Function GetDeclaredSymbol(semanticModel As SemanticModel, token As SyntaxToken, cancellationToken As CancellationToken) As ISymbol Implements ISemanticFactsService.GetDeclaredSymbol
            Dim location = token.GetLocation()

            Dim q = From node In token.GetAncestors(Of SyntaxNode)()
                    Where Not TypeOf node Is AggregationRangeVariableSyntax AndAlso
                          Not TypeOf node Is CollectionRangeVariableSyntax AndAlso
                          Not TypeOf node Is ExpressionRangeVariableSyntax AndAlso
                          Not TypeOf node Is InferredFieldInitializerSyntax
                    Let symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken)
                    Where symbol IsNot Nothing AndAlso symbol.Locations.Contains(location)
                    Select symbol

            Return q.FirstOrDefault()
        End Function

        Public Function LastEnumValueHasInitializer(namedTypeSymbol As INamedTypeSymbol) As Boolean Implements ISemanticFactsService.LastEnumValueHasInitializer
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

        Public ReadOnly Property SupportsParameterizedProperties As Boolean Implements ISemanticFactsService.SupportsParameterizedProperties
            Get
                Return True
            End Get
        End Property

        Public Function TryGetSpeculativeSemanticModel(oldSemanticModel As SemanticModel, oldNode As SyntaxNode, newNode As SyntaxNode, <Out> ByRef speculativeModel As SemanticModel) As Boolean Implements ISemanticFactsService.TryGetSpeculativeSemanticModel
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

        Public Function GetAliasNameSet(model As SemanticModel, cancellationToken As CancellationToken) As ImmutableHashSet(Of String) Implements ISemanticFactsService.GetAliasNameSet
            Dim original = DirectCast(model.GetOriginalSemanticModel(), SemanticModel)

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

        Public Function GetForEachSymbols(model As SemanticModel, forEachStatement As SyntaxNode) As ForEachSymbols Implements ISemanticFactsService.GetForEachSymbols

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

        Public Function GetGetAwaiterMethod(model As SemanticModel, node As SyntaxNode) As IMethodSymbol Implements ISemanticFactsService.GetGetAwaiterMethod
            If node.IsKind(SyntaxKind.AwaitExpression) Then
                Dim awaitExpression = DirectCast(node, AwaitExpressionSyntax)
                Dim info = model.GetAwaitExpressionInfo(awaitExpression)
                Return info.GetAwaiterMethod
            End If

            Return Nothing
        End Function

        Public Function GetDeconstructionAssignmentMethods(model As SemanticModel, deconstruction As SyntaxNode) As ImmutableArray(Of IMethodSymbol) Implements ISemanticFactsService.GetDeconstructionAssignmentMethods
            Return ImmutableArray(Of IMethodSymbol).Empty
        End Function

        Public Function GetDeconstructionForEachMethods(model As SemanticModel, deconstruction As SyntaxNode) As ImmutableArray(Of IMethodSymbol) Implements ISemanticFactsService.GetDeconstructionForEachMethods
            Return ImmutableArray(Of IMethodSymbol).Empty
        End Function

        Public Function IsNamespaceDeclarationNameContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsNamespaceDeclarationNameContext
            Return semanticModel.SyntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken)
        End Function

        Public Function IsPartial(typeSymbol As ITypeSymbol, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsPartial
            Dim syntaxRefs = typeSymbol.DeclaringSyntaxReferences
            Return syntaxRefs.Any(
                Function(n As SyntaxReference)
                    Return DirectCast(n.GetSyntax(cancellationToken), TypeStatementSyntax).Modifiers.Any(SyntaxKind.PartialKeyword)
                End Function)
        End Function

        Public Function GetDeclaredSymbols(semanticModel As SemanticModel, memberDeclaration As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of ISymbol) Implements ISemanticFactsService.GetDeclaredSymbols
            If TypeOf memberDeclaration Is FieldDeclarationSyntax Then
                Return DirectCast(memberDeclaration, FieldDeclarationSyntax).Declarators.
                    SelectMany(Function(d) d.Names.AsEnumerable()).
                    Select(Function(n) semanticModel.GetDeclaredSymbol(n, cancellationToken))
            End If

            Return {semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken)}
        End Function

        Public Function FindParameterForArgument(semanticModel As SemanticModel, argumentNode As SyntaxNode, cancellationToken As CancellationToken) As IParameterSymbol Implements ISemanticFactsService.FindParameterForArgument
            Return DirectCast(argumentNode, ArgumentSyntax).DetermineParameter(semanticModel, allowParamArray:=False, cancellationToken)
        End Function

        Public Function GetBestOrAllSymbols(semanticModel As SemanticModel, node As SyntaxNode, token As SyntaxToken, cancellationToken As CancellationToken) As ImmutableArray(Of ISymbol) Implements ISemanticFactsService.GetBestOrAllSymbols
            Return semanticModel.GetSymbolInfo(node, cancellationToken).GetBestOrAllSymbols()
        End Function

        Private Function ISemanticFactsService_GenerateUniqueName(
            semanticModel As SemanticModel, location As SyntaxNode, containerOpt As SyntaxNode, baseName As String, cancellationToken As CancellationToken) As SyntaxToken Implements ISemanticFactsService.GenerateUniqueName
            Return MyBase.GenerateUniqueName(semanticModel, location, containerOpt, baseName, cancellationToken)
        End Function

        Private Function ISemanticFactsService_GenerateUniqueName(
            semanticModel As SemanticModel, location As SyntaxNode, containerOpt As SyntaxNode, baseName As String, usedNames As IEnumerable(Of String), cancellationToken As CancellationToken) As SyntaxToken Implements ISemanticFactsService.GenerateUniqueName
            Return MyBase.GenerateUniqueName(semanticModel, location, containerOpt, baseName, usedNames, cancellationToken)
        End Function

        Private Function ISemanticFactsService_GenerateUniqueLocalName(
            semanticModel As SemanticModel, location As SyntaxNode, containerOpt As SyntaxNode, baseName As String, cancellationToken As CancellationToken) As SyntaxToken Implements ISemanticFactsService.GenerateUniqueLocalName
            Return MyBase.GenerateUniqueLocalName(semanticModel, location, containerOpt, baseName, cancellationToken)
        End Function

        Public Function IsInsideNameOfExpression(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsInsideNameOfExpression
            Return node.FirstAncestorOrSelf(Of NameOfExpressionSyntax) IsNot Nothing
        End Function

        Private Function ISemanticFactsService_GenerateUniqueName(semanticModel As SemanticModel, location As SyntaxNode, containerOpt As SyntaxNode, baseName As String, filter As Func(Of ISymbol, Boolean), usedNames As IEnumerable(Of String), cancellationToken As CancellationToken) As SyntaxToken Implements ISemanticFactsService.GenerateUniqueName
            Return MyBase.GenerateUniqueName(semanticModel, location, containerOpt, baseName, filter, usedNames, cancellationToken)
        End Function
    End Class
End Namespace
