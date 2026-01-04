' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    <ExportCompletionProvider(NameOf(YieldCompletionProvider), LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(After:=NameOf(KeywordCompletionProvider))>
    Friend NotInheritable Class YieldCompletionProvider
        Inherits AbstractYieldCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New("Yield", VBFeaturesResources.Produces_an_element_of_an_IEnumerable_or_IEnumerator)
        End Sub

        Friend Overrides ReadOnly Property Language As String = LanguageNames.VisualBasic

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CommonTriggerChars

        Protected Overrides Function IsYieldKeywordContext(syntaxContext As SyntaxContext) As Boolean
            Return DirectCast(syntaxContext, VisualBasicSyntaxContext).IsStatementContext
        End Function

        Protected Overrides Function GetAsyncSupportingDeclaration(leftToken As SyntaxToken, position As Integer) As SyntaxNode
            Dim parent = leftToken.Parent
            If parent Is Nothing Then Return Nothing

            Dim node = parent.FirstAncestorOrSelf(Of SyntaxNode)(
                Function(n) n.IsAsyncSupportedFunctionSyntax() OrElse n.IsKind(SyntaxKind.GetAccessorBlock))

            If node Is Nothing Then Return Nothing

            ' Yield is not allowed in Sub
            If node.IsKind(SyntaxKind.SubBlock,
                           SyntaxKind.MultiLineSubLambdaExpression,
                           SyntaxKind.SingleLineSubLambdaExpression) Then
                Return Nothing
            End If

            Return node
        End Function

        Protected Overrides Function GetAsyncKeywordInsertionPosition(declaration As SyntaxNode) As Integer
            Select Case declaration.Kind()
                Case SyntaxKind.FunctionBlock
                    ' For a method block, we want to insert 'Async' before 'Function' keyword in the header.
                    Return DirectCast(declaration, MethodBlockSyntax).SubOrFunctionStatement.DeclarationKeyword.SpanStart
                Case SyntaxKind.GetAccessorBlock
                    Return DirectCast(declaration, AccessorBlockSyntax).AccessorStatement.AccessorKeyword.SpanStart
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression
                    Return DirectCast(declaration, LambdaExpressionSyntax).SubOrFunctionHeader.SubOrFunctionKeyword.SpanStart
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(declaration.Kind())
            End Select
        End Function

        Protected Overrides Function GetReturnTypeChangeAsync(solution As Solution, semanticModel As SemanticModel, declaration As SyntaxNode, cancellationToken As CancellationToken) As Task(Of TextChange?)
            Return SpecializedTasks.Default(Of TextChange?)()
        End Function

        Protected Overrides Function ShouldAddModifiers(syntaxContext As SyntaxContext, declaration As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Dim semanticModel = syntaxContext.SemanticModel
            Dim methodSymbol = TryCast(semanticModel.GetDeclaredSymbol(declaration, cancellationToken), IMethodSymbol)
            If methodSymbol Is Nothing Then Return False

            If methodSymbol.IsIterator AndAlso (methodSymbol.IsAsync OrElse Not CanBeAsync(declaration)) Then Return False

            Dim returnType = methodSymbol.ReturnType
            If returnType Is Nothing OrElse TypeOf returnType Is IErrorTypeSymbol Then Return False

            Select Case returnType.Name
                Case "IAsyncEnumerable", "IAsyncEnumerator", "IEnumerable", "IEnumerator"
                    Dim taskLikeTypes = New KnownTaskTypes(semanticModel.Compilation)
                    If returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumerableOfTType) OrElse
                       returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumeratorOfTType) OrElse
                       returnType.OriginalDefinition.Equals(semanticModel.Compilation.IEnumerableOfTType()) OrElse
                       returnType.OriginalDefinition.Equals(semanticModel.Compilation.IEnumeratorOfTType()) OrElse
                       returnType.Equals(semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable)) OrElse
                       returnType.Equals(semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerator)) Then
                        Return True
                    End If
            End Select

            Return False
        End Function

        Protected Overrides Async Function GetPrefixTextChangesAsync(document As Document, declaration As SyntaxNode, insertionPosition As Integer, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of TextChange))
            Dim semanticModel = Await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim methodSymbol = TryCast(semanticModel.GetDeclaredSymbol(declaration, cancellationToken), IMethodSymbol)
            If methodSymbol Is Nothing Then Return ImmutableArray(Of TextChange).Empty

            Dim builder = ArrayBuilder(Of TextChange).GetInstance()
            Dim modifiersToAdd = ""

            Dim returnType = methodSymbol.ReturnType
            Dim taskLikeTypes = New KnownTaskTypes(semanticModel.Compilation)
            Dim isAsyncIterator = returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumerableOfTType) OrElse
                                  returnType.OriginalDefinition.Equals(taskLikeTypes.IAsyncEnumeratorOfTType)

            If isAsyncIterator AndAlso Not methodSymbol.IsAsync AndAlso CanBeAsync(declaration) Then
                modifiersToAdd &= "Async "
            End If

            If Not methodSymbol.IsIterator Then
                modifiersToAdd &= "Iterator "
            End If

            If modifiersToAdd <> "" Then
                builder.Add(New TextChange(New TextSpan(insertionPosition, 0), modifiersToAdd))
            End If

            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Function CanBeAsync(declaration As SyntaxNode) As Boolean
            ' Properties cannot be Async in VB
            Return Not declaration.IsKind(SyntaxKind.GetAccessorBlock)
        End Function
    End Class
End Namespace
