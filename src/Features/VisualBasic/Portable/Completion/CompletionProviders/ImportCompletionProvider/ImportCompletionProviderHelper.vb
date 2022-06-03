' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.[Shared].Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class ImportCompletionProviderHelper

        Public Shared Async Function GetImportedNamespacesAsync(syntaxContext As SyntaxContext, token As CancellationToken) As Task(Of ImmutableArray(Of String))

            ' The location Is the containing node of the LeftToken, Or the compilation unit itself if LeftToken
            ' indicates the beginning of the document (i.e. no parent).
            Dim Location = If(syntaxContext.LeftToken.Parent, Await syntaxContext.SyntaxTree.GetRootAsync(token).ConfigureAwait(False))

            Dim builder = ArrayBuilder(Of String).GetInstance()

            ' Get namespaces from import directives
            Dim importsInScope = syntaxContext.SemanticModel.GetImportNamespacesInScope(Location)
            For Each import As INamespaceSymbol In importsInScope
                builder.Add(import.ToDisplayString(SymbolDisplayFormats.NameFormat))
            Next

            ' Get global imports from compilation option
            Dim vbOptions = DirectCast(syntaxContext.SemanticModel.Compilation.Options, VisualBasicCompilationOptions)
            For Each globalImport As GlobalImport In vbOptions.GlobalImports
                builder.Add(globalImport.Name)
            Next

            Return builder.ToImmutableAndFree()
        End Function
    End Class
End Namespace
