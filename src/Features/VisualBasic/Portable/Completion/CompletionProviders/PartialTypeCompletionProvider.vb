' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class PartialTypeCompletionProvider
        Inherits AbstractPartialTypeCompletionProvider

        Private Shared ReadOnly s_insertionFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                genericsOptions:=
                    SymbolDisplayGenericsOptions.IncludeTypeParameters Or
                    SymbolDisplayGenericsOptions.IncludeVariance Or
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints)

        Private Shared ReadOnly s_displayFormat As SymbolDisplayFormat =
            s_insertionFormat.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

        Private Shared ReadOnly s_completionFormat As SymbolCompletionFormat =
            New SymbolCompletionFormat(s_displayFormat, s_insertionFormat)

        Public Sub New()
            MyBase.New(s_completionFormat)
        End Sub

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Protected Overrides Function GetPartialTypeSyntaxNode(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxNode
            Dim statement As TypeStatementSyntax = Nothing
            Return If(tree.IsPartialTypeDeclarationNameContext(position, cancellationToken, statement), statement, Nothing)
        End Function

        Protected Overrides Async Function CreateSyntaxContextAsync(document As Document, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
