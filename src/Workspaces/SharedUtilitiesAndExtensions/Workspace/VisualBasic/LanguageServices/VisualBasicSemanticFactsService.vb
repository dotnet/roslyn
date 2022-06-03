' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageServiceFactory(GetType(ISemanticFactsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSemanticFactsServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return VisualBasicSemanticFactsService.Instance
        End Function
    End Class

    Partial Friend NotInheritable Class VisualBasicSemanticFactsService
        Inherits AbstractSemanticFactsService
        Implements ISemanticFactsService

        Public Shared ReadOnly Instance As New VisualBasicSemanticFactsService()

        Public Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance
        Public Overrides ReadOnly Property BlockFacts As IBlockFacts = VisualBasicBlockFacts.Instance

        Protected Overrides ReadOnly Property SemanticFacts As ISemanticFacts = VisualBasicSemanticFacts.Instance

        Private Sub New()
        End Sub

        Protected Overrides Function ToIdentifierToken(identifier As String) As SyntaxToken
            Return identifier.ToIdentifierToken
        End Function

        Public Function IsExpressionContext(semanticModel As SemanticModel,
                                            position As Integer,
                                            cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsExpressionContext
            Dim token = semanticModel.SyntaxTree.GetTargetToken(position, cancellationToken)
            Return semanticModel.SyntaxTree.IsExpressionContext(position, token, cancellationToken, semanticModel)
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

        Public Function IsNamespaceDeclarationNameContext(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISemanticFactsService.IsNamespaceDeclarationNameContext
            Return semanticModel.SyntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken)
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

        Private Function ISemanticFactsService_GenerateUniqueLocalName(
            semanticModel As SemanticModel, location As SyntaxNode, containerOpt As SyntaxNode, baseName As String, usedName As IEnumerable(Of String), cancellationToken As CancellationToken) As SyntaxToken Implements ISemanticFactsService.GenerateUniqueLocalName
            Return MyBase.GenerateUniqueLocalName(semanticModel, location, containerOpt, baseName, usedName, cancellationToken)
        End Function

        Private Function ISemanticFactsService_GenerateUniqueName(semanticModel As SemanticModel, location As SyntaxNode, containerOpt As SyntaxNode, baseName As String, filter As Func(Of ISymbol, Boolean), usedNames As IEnumerable(Of String), cancellationToken As CancellationToken) As SyntaxToken Implements ISemanticFactsService.GenerateUniqueName
            Return MyBase.GenerateUniqueName(semanticModel, location, containerOpt, baseName, filter, usedNames, cancellationToken)
        End Function

        Private Function ISemanticFactsService_GenerateUniqueName(baseName As String, usedNames As IEnumerable(Of String)) As SyntaxToken Implements ISemanticFactsService.GenerateUniqueName
            Return MyBase.GenerateUniqueName(baseName, usedNames)
        End Function

        Public Function ClassifyConversion(semanticModel As SemanticModel, expression As SyntaxNode, destination As ITypeSymbol) As CommonConversion Implements ISemanticFactsService.ClassifyConversion
            Return semanticModel.ClassifyConversion(DirectCast(expression, ExpressionSyntax), destination).ToCommonConversion()
        End Function
    End Class
End Namespace
