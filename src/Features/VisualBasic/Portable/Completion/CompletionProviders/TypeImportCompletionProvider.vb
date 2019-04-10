' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class TypeImportCompletionProvider
        Inherits AbstractTypeImportCompletionProvider

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Protected Overrides Async Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            ' Need regular semantic model because we will use it to get imported namespace symbols. Otherwise we will try to 
            ' reach outside of the span And ended up with "node not within syntax tree" error from the speculative model.
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Sub GetImportedNamespaces(location As SyntaxNode, semanticModel As SemanticModel, builder As ImmutableHashSet(Of String).Builder, cancellationToken As CancellationToken)
            ' Get namespaces from import directives
            Dim importsInScope = semanticModel.GetImportNamespacesInScope(location)
            For Each import As INamespaceSymbol In importsInScope
                builder.Add(import.ToDisplayString(SymbolDisplayFormats.NameFormat))
            Next

            ' Get global imports from compilation option
            Dim vbOptions = DirectCast(semanticModel.Compilation.Options, VisualBasicCompilationOptions)
            For Each globalImport As GlobalImport In vbOptions.GlobalImports
                builder.Add(globalImport.Name)
            Next
        End Sub
    End Class
End Namespace
