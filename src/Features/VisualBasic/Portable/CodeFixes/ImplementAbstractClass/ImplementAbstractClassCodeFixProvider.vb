' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ImplementAbstractClass
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ImplementAbstractClass

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ImplementAbstractClass), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateType)>
    Friend Class ImplementAbstractClassCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30610 As String = "BC30610" ' Class 'foo' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30610)
            End Get
        End Property

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(context.Span.Start)
            If Not token.Span.IntersectsWith(context.Span) Then
                Return
            End If

            Dim classNode = token.GetAncestors(Of ClassBlockSyntax)() _
                            .FirstOrDefault(Function(c) c.Span.IntersectsWith(context.Span))

            If classNode Is Nothing Then
                Return
            End If

            Dim service = context.Document.GetLanguageService(Of IImplementAbstractClassService)()
            Dim model = Await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(False)
            For Each inheritsNode In classNode.Inherits
                For Each node In inheritsNode.Types
                    If service.CanImplementAbstractClass(
                        context.Document,
                        model,
                        node,
                        context.CancellationToken) Then

                        Dim title = VBFeaturesResources.ImplementAbstractClass
                        Dim abstractType = model.GetTypeInfo(node, context.CancellationToken).Type
                        Dim typeName = abstractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        Dim id = GetCodeActionId(abstractType.ContainingAssembly.Name, typeName)
                        context.RegisterCodeFix(
                            New MyCodeAction(title,
                                             Function(c) ImplementAbstractClassAsync(context.Document, node, c),
                                             id),
                            context.Diagnostics)
                        Return
                    End If
                Next
            Next
        End Function

        Friend Shared Function GetCodeActionId(assemblyName As String, abstractTypeFullyQualifiedName As String) As String
            Return VBFeaturesResources.ImplementAbstractClass + ";" +
                assemblyName + ";" +
                abstractTypeFullyQualifiedName
        End Function

        Private Async Function ImplementAbstractClassAsync(document As Document, node As TypeSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim service = document.GetLanguageService(Of IImplementAbstractClassService)()
            Dim updatedDocument = Await service.ImplementAbstractClassAsync(
                document,
                Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False),
                node,
                cancellationToken).ConfigureAwait(False)

            Return updatedDocument
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)), id As String)
                MyBase.New(title, createChangedDocument, id)
            End Sub
        End Class
    End Class
End Namespace
