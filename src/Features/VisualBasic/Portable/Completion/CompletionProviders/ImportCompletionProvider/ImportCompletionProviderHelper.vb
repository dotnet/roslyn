' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class ImportCompletionProviderHelper

        Public Shared Function GetImportedNamespaces(location As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ImmutableArray(Of String)
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
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Public Shared Async Function IsInImportsDirectiveAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of Boolean)
            Dim syntaxTree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)
            Return leftToken.GetAncestor(Of ImportsStatementSyntax)() IsNot Nothing
        End Function
    End Class
End Namespace
