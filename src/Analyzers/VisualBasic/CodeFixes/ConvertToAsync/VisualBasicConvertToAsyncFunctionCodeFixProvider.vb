' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ConvertToAsync
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ConvertToAsync
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ConvertToAsync), [Shared]>
    Friend Class VisualBasicConvertToAsyncFunctionCodeFixProvider
        Inherits AbstractConvertToAsyncCodeFixProvider

        Friend Const BC37001 As String = "BC37001" ' error BC37001: 'Blah' Does not return a Task and is not awaited consider changing to an Async Function.

        Friend ReadOnly Ids As ImmutableArray(Of String) = ImmutableArray.Create(Of String)(BC37001)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return Ids
            End Get
        End Property

        Protected Overrides Async Function GetDescriptionAsync(diagnostic As Diagnostic, node As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Task(Of String)
            Dim methodNode = Await GetMethodFromExpressionAsync(node, semanticModel, cancellationToken).ConfigureAwait(False)
            Return String.Format(VisualBasicCodeFixesResources.Make_0_an_Async_Function, methodNode.Item2.BlockStatement)
        End Function

        Protected Overrides Async Function GetRootInOtherSyntaxTreeAsync(node As SyntaxNode, semanticModel As SemanticModel, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Tuple(Of SyntaxTree, SyntaxNode))
            Dim tuple = Await GetMethodFromExpressionAsync(node, semanticModel, cancellationToken).ConfigureAwait(False)
            If tuple Is Nothing Then
                Return Nothing
            End If

            Dim oldRoot = tuple.Item1
            Dim methodBlock = tuple.Item2

            Dim newRoot = oldRoot.ReplaceNode(methodBlock, ConvertToAsyncFunction(methodBlock))
            Return System.Tuple.Create(oldRoot.SyntaxTree, newRoot)
        End Function

        Private Shared Async Function GetMethodFromExpressionAsync(oldNode As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Task(Of Tuple(Of SyntaxNode, MethodBlockSyntax))
            If oldNode Is Nothing Then
                Return Nothing
            End If

            Dim methodSymbol = TryCast(semanticModel.GetSymbolInfo(oldNode, cancellationToken).Symbol, IMethodSymbol)
            If methodSymbol Is Nothing Then
                Return Nothing
            End If

            Dim methodReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()
            If methodReference Is Nothing Then
                Return Nothing
            End If

            Dim root = Await methodReference.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)

            Dim methodDeclaration = TryCast(Await methodReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(False), MethodStatementSyntax)
            If methodDeclaration Is Nothing Then
                Return Nothing
            End If

            If Not methodDeclaration.IsKind(SyntaxKind.SubStatement) Then
                Return Nothing
            End If

            Dim methodBlock = methodDeclaration.GetAncestor(Of MethodBlockSyntax)
            If methodBlock Is Nothing Then
                Return Nothing
            End If

            Return Tuple.Create(root, methodBlock)
        End Function

        Private Shared Function ConvertToAsyncFunction(methodBlock As MethodBlockSyntax) As MethodBlockSyntax
            Dim methodNode = methodBlock.SubOrFunctionStatement

            Dim blockBegin = SyntaxFactory.FunctionStatement(
                methodNode.AttributeLists,
                methodNode.Modifiers,
                methodNode.Identifier,
                methodNode.TypeParameterList,
                methodNode.ParameterList.WithoutTrailingTrivia(),
                SyntaxFactory.SimpleAsClause(SyntaxFactory.ParseTypeName("Task")) _
                    .WithTrailingTrivia(methodNode.ParameterList.GetTrailingTrivia()),
                methodNode.HandlesClause,
                methodNode.ImplementsClause) _
                .WithAdditionalAnnotations(Formatter.Annotation)

            Dim blockEnd = SyntaxFactory.EndBlockStatement(SyntaxKind.EndFunctionStatement, SyntaxFactory.Token(SyntaxKind.FunctionKeyword)) _
                .WithLeadingTrivia(methodBlock.EndBlockStatement.GetLeadingTrivia()) _
                .WithTrailingTrivia(methodBlock.EndBlockStatement.GetTrailingTrivia()) _
                .WithAdditionalAnnotations(Formatter.Annotation)

            Return SyntaxFactory.FunctionBlock(blockBegin, methodBlock.Statements, blockEnd)
        End Function
    End Class
End Namespace

