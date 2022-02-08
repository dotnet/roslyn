' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects

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
    End Class
End Namespace
