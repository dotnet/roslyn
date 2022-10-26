' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities.CommandHandlers
Imports Microsoft.CodeAnalysis.ImplementInterface
Imports Microsoft.CodeAnalysis.ImplementType
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ImplementInterface
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name("ImplementInterfaceCommandHandler")>
    <Order(Before:=PredefinedCommandHandlerNames.EndConstruct)>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    Friend Class ImplementInterfaceCommandHandler
        Inherits AbstractImplementAbstractClassOrInterfaceCommandHandler

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(editorOperationsFactoryService As IEditorOperationsFactoryService,
                       globalOptions As IGlobalOptionService)
            MyBase.New(editorOperationsFactoryService, globalOptions)
        End Sub

        Protected Overrides Function TryGetNewDocument(
            document As Document,
            options As ImplementTypeGenerationOptions,
            typeSyntax As TypeSyntax,
            cancellationToken As CancellationToken
        ) As Document

            If typeSyntax.Parent.Kind <> SyntaxKind.ImplementsStatement Then
                Return Nothing
            End If

            Dim service = document.GetLanguageService(Of IImplementInterfaceService)()
            Dim updatedDocument = service.ImplementInterfaceAsync(
                document,
                options,
                typeSyntax.Parent,
                cancellationToken).WaitAndGetResult(cancellationToken)
            If updatedDocument.GetTextChangesAsync(document, cancellationToken).WaitAndGetResult(cancellationToken).Count = 0 Then
                Return Nothing
            End If

            Return updatedDocument
        End Function
    End Class
End Namespace
