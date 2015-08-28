' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructor
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GenerateFromMembers.GenerateConstructor
    <ExportLanguageService(GetType(IGenerateConstructorService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGenerateConstructorService
        Inherits AbstractGenerateConstructorService(Of VisualBasicGenerateConstructorService, StatementSyntax)

        Protected Overloads Overrides Async Function GetSelectedMembersAsync(
                document As Document, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of IList(Of StatementSyntax))

            If cancellationToken.IsCancellationRequested Then
                Return SpecializedCollections.EmptyList(Of StatementSyntax)()
            Else
                Return Await GenerateFromMembersHelpers.GetSelectedMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(False)
            End If
        End Function

        Protected Overrides Function GetDeclaredSymbols(
                semanticModel As SemanticModel,
                memberDeclaration As StatementSyntax,
                cancellationToken As CancellationToken) As IEnumerable(Of ISymbol)
            Return GenerateFromMembersHelpers.GetDeclaredSymbols(semanticModel, memberDeclaration, cancellationToken)
        End Function
    End Class
End Namespace
