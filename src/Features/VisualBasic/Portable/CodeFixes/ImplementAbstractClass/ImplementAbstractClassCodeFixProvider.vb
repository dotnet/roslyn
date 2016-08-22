' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ImplementAbstractClass
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ImplementAbstractClass
    <ExportCodeFixProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeFixProviderNames.ImplementAbstractClass), [Shared]>
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
            Dim cancellationToken = context.CancellationToken
            Dim document = context.Document

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(context.Span.Start)
            If Not token.Span.IntersectsWith(context.Span) Then
                Return
            End If

            Dim classNode = token.GetAncestors(Of ClassBlockSyntax)() _
                                 .FirstOrDefault(Function(c) c.Span.IntersectsWith(context.Span))

            If classNode Is Nothing Then
                Return
            End If

            Dim service = document.GetLanguageService(Of IImplementAbstractClassService)()

            If Await service.CanImplementAbstractClassAsync(
                document, classNode, cancellationToken).ConfigureAwait(False) Then

                Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
                Dim classSymbol = model.GetDeclaredSymbol(classNode)

                Dim typeName = classSymbol.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                Dim id = GetCodeActionId(classSymbol.BaseType.ContainingAssembly.Name, typeName)
                context.RegisterCodeFix(
                    New MyCodeAction(
                        Function(c) ImplementAbstractClassAsync(document, classNode, c),
                        id),
                    context.Diagnostics)
                Return
            End If
        End Function

        Friend Shared Function GetCodeActionId(assemblyName As String, abstractTypeFullyQualifiedName As String) As String
            Return VBFeaturesResources.Implement_Abstract_Class + ";" +
                assemblyName + ";" +
                abstractTypeFullyQualifiedName
        End Function

        Private Function ImplementAbstractClassAsync(
                document As Document, classBlock As ClassBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim service = document.GetLanguageService(Of IImplementAbstractClassService)()
            Return service.ImplementAbstractClassAsync(document, classBlock, cancellationToken)
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(createChangedDocument As Func(Of CancellationToken, Task(Of Document)), id As String)
                MyBase.New(VBFeaturesResources.Implement_Abstract_Class, createChangedDocument, id)
            End Sub
        End Class
    End Class
End Namespace