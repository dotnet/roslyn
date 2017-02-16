' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.GenerateFromMembers
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GenerateFromMembers
    <ExportLanguageService(GetType(IGenerateFromMembersHelperService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGenerateFromMembersHelpersService
        Implements IGenerateFromMembersHelperService

        Public Async Function GetSelectedMembersAsync(
                document As Document, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SyntaxNode)) Implements IGenerateFromMembersHelperService.GetSelectedMembersAsync
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Return ImmutableArray(Of SyntaxNode).CastUp(tree.GetMembersInSpan(textSpan, cancellationToken))
        End Function

        Public Function GetDeclaredSymbols(semanticModel As SemanticModel, memberDeclaration As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of ISymbol) Implements IGenerateFromMembersHelperService.GetDeclaredSymbols
            If TypeOf memberDeclaration Is FieldDeclarationSyntax Then
                Return DirectCast(memberDeclaration, FieldDeclarationSyntax).Declarators.
                    SelectMany(Function(d) d.Names.AsEnumerable()).
                    Select(Function(n) semanticModel.GetDeclaredSymbol(n, cancellationToken))
            End If

            Return {semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken)}
        End Function
    End Class
End Namespace
