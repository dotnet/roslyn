' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class ImportCompletionProviderHelper

        Public Shared Function GetImportedNamespaces(location As SyntaxNode, semanticModel As SemanticModel) As ImmutableArray(Of String)
            Dim builder = ArrayBuilder(Of String).GetInstance()

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

            Return builder.ToImmutableAndFree()
        End Function

        Public Shared Async Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            ' Need regular semantic model because we will use it to get imported namespace symbols. Otherwise we will try to 
            ' reach outside of the span And ended up with "node not within syntax tree" error from the speculative model.
            ' Also we use partial model so that we don't have to wait for all semantics to be computed.
            Dim sourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim frozenDocument = sourceText.GetDocumentWithFrozenPartialSemantics(cancellationToken)
            Contract.ThrowIfNull(frozenDocument)

            Dim semanticModel = Await frozenDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Return VisualBasicSyntaxContext.CreateContext(frozenDocument.Project.Solution.Workspace, semanticModel, position, cancellationToken)
        End Function
    End Class
End Namespace
