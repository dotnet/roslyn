' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GenerateFromMembers
    Friend Class GenerateFromMembersHelpers
        Public Shared Async Function GetSelectedMembersAsync(
                document As Document, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of IList(Of StatementSyntax))
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Return tree.GetMembersInSpan(textSpan, cancellationToken)
        End Function

        Public Shared Function GetDeclaredSymbols(semanticModel As SemanticModel, memberDeclaration As StatementSyntax, cancellationToken As CancellationToken) As IEnumerable(Of ISymbol)
            If TypeOf memberDeclaration Is FieldDeclarationSyntax Then
                Return DirectCast(memberDeclaration, FieldDeclarationSyntax).Declarators.
                    SelectMany(Function(d) d.Names.AsEnumerable()).
                    Select(Function(n) semanticModel.GetDeclaredSymbol(n, cancellationToken))
            End If

            Return {semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken)}
        End Function
    End Class
End Namespace
