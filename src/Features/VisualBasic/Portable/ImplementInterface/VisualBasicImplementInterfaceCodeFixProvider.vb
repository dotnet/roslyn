' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ImplementInterface
Imports Microsoft.CodeAnalysis.ImplementType
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ImplementInterface

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ImplementInterface), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ImplementAbstractClass)>
    Friend Class VisualBasicImplementInterfaceCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30149 As String = "BC30149" ' Class 'bar' must implement 'Sub goo()' for interface 'igoo'.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30149)
            End Get
        End Property

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span) Then
                Return
            End If

            Dim implementsNode = token.GetAncestors(Of ImplementsStatementSyntax) _
                                 .FirstOrDefault(Function(c) c.Span.IntersectsWith(span))
            If implementsNode Is Nothing Then
                Return
            End If

            Dim typeNode = implementsNode.Types.Where(Function(c) c.Span.IntersectsWith(span)) _
                           .FirstOrDefault(Function(c) c.Span.IntersectsWith(span))

            If typeNode Is Nothing Then
                Return
            End If

            Dim service = document.GetLanguageService(Of IImplementInterfaceService)()
            Dim actions = service.GetCodeActions(
                document,
                context.Options.GetImplementTypeGenerationOptions(document.Project.LanguageServices),
                Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False),
                typeNode,
                cancellationToken)

            context.RegisterFixes(actions, context.Diagnostics)
        End Function
    End Class
End Namespace
