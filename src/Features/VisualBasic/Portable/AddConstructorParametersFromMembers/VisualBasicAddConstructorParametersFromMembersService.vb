' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.GenerateFromMembers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AddConstructorParametersFromMembers
    <ExportLanguageService(GetType(IAddConstructorParametersFromMembersService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddConstructorParametersFromMembersService
        Inherits AbstractAddConstructorParametersFromMembersService(Of VisualBasicAddConstructorParametersFromMembersService, StatementSyntax)

        Protected Overloads Overrides Function GetSelectedMembersAsync(
                document As Document, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of IList(Of StatementSyntax))
            Return GenerateFromMembersHelpers.GetSelectedMembersAsync(document, textSpan, cancellationToken)
        End Function

        Protected Overrides Function GetDeclaredSymbols(
                semanticModel As SemanticModel,
                memberDeclaration As StatementSyntax,
                cancellationToken As CancellationToken) As IEnumerable(Of ISymbol)
            Return GenerateFromMembersHelpers.GetDeclaredSymbols(semanticModel, memberDeclaration, cancellationToken)
        End Function
    End Class
End Namespace
