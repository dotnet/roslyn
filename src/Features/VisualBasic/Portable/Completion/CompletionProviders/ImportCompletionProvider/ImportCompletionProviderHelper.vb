' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.[Shared].Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class ImportCompletionProviderHelper

        Public Shared Function GetImportedNamespaces(syntaxContext As SyntaxContext, token As CancellationToken) As ImmutableArray(Of String)
            Dim scopes = syntaxContext.SemanticModel.GetImportScopes(syntaxContext.Position, token)

            Dim usingsBuilder = ArrayBuilder(Of String).GetInstance()

            For Each scope In scopes
                For Each import In scope.Imports
                    If TypeOf import.NamespaceOrType Is INamespaceSymbol Then
                        usingsBuilder.Add(DirectCast(import.NamespaceOrType, INamespaceSymbol).ToDisplayString(SymbolDisplayFormats.NameFormat))
                    End If
                Next
            Next

            Return usingsBuilder.ToImmutableAndFree()
        End Function
    End Class
End Namespace
