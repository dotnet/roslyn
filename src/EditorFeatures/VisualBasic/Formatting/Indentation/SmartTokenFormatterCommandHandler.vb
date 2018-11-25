' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Formatting.Indentation
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation
    <Export(GetType(VSCommanding.ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.Indent)>
    <Order(After:=PredefinedCommandHandlerNames.Rename)>
    Friend Class SmartTokenFormatterCommandHandler
        Inherits AbstractSmartTokenFormatterCommandHandler

        Private ReadOnly _formattingRules As IEnumerable(Of IFormattingRule)

        <ImportingConstructor()>
        Public Sub New(undoHistoryRegistry As ITextUndoHistoryRegistry,
                       editorOperationsFactoryService As IEditorOperationsFactoryService)

            MyBase.New(undoHistoryRegistry,
                       editorOperationsFactoryService)
        End Sub

        Protected Overrides Function GetFormattingRules(document As Document, position As Integer) As IEnumerable(Of IFormattingRule)
            Dim ws = document.Project.Solution.Workspace
            Dim formattingRuleFactory = ws.Services.GetService(Of IHostDependentFormattingRuleFactoryService)()
            Return {New SpecialFormattingRule(), formattingRuleFactory.CreateRule(document, position)}.Concat(Formatter.GetDefaultFormattingRules(document))
        End Function

        Protected Overrides Function CreateSmartTokenFormatter(optionSet As OptionSet, formattingRules As IEnumerable(Of IFormattingRule), root As SyntaxNode) As ISmartTokenFormatter
            Return New SmartTokenFormatter(optionSet, formattingRules, DirectCast(root, CompilationUnitSyntax))
        End Function

        Protected Overrides Function UseSmartTokenFormatter(root As SyntaxNode,
                                                            line As TextLine,
                                                            formattingRules As IEnumerable(Of IFormattingRule),
                                                            options As OptionSet,
                                                            cancellationToken As CancellationToken) As Boolean
            Return VisualBasicIndentationService.ShouldUseSmartTokenFormatterInsteadOfIndenter(
                formattingRules, DirectCast(root, CompilationUnitSyntax), line, options, cancellationToken, neverUseWhenHavingMissingToken:=False)
        End Function

        Protected Overrides Function IsInvalidToken(token As SyntaxToken) As Boolean
            ' invalid token to be formatted
            Return token.Kind = SyntaxKind.None OrElse
                   token.Kind = SyntaxKind.EndOfFileToken
        End Function
    End Class
End Namespace
