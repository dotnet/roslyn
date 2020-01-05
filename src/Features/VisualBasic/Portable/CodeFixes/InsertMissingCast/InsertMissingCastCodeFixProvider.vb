' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.InsertMissingCast
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.InsertMissingCast), [Shared]>
    Partial Friend Class InsertMissingCastCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30512 As String = "BC30512" ' Option Strict On disallows implicit conversions from '{0}' to '{1}'.
        Friend Const BC42016 As String = "BC42016" ' Implicit conversions from '{0}' to '{1}'.

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30512, BC42016)
            End Get
        End Property

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            ' Fix All is not supported by this code fix
            ' https://github.com/dotnet/roslyn/issues/34469
            Return Nothing
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim node = TryCast(root.FindNode(span, getInnermostNodeForTie:=True), ExpressionSyntax)

            If node Is Nothing Then
                Return
            End If

            Dim model = Await document.GetSemanticModelForNodeAsync(node, cancellationToken).ConfigureAwait(False)
            Dim inferenceService = document.GetLanguageService(Of ITypeInferenceService)()
            Dim targetType = inferenceService.InferType(model, node, False, cancellationToken)

            If targetType Is Nothing Then
                Return
            End If

            context.RegisterCodeFix(New InsertMissingCastCodeAction(document, node, node.Cast(targetType, Nothing)), context.Diagnostics)
        End Function
    End Class
End Namespace
