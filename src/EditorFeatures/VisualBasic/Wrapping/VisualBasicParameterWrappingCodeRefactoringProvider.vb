' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.Wrapping
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Editor.Wrapping
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicParameterWrappingCodeRefactoringProvider
        Inherits AbstractWrappingCodeRefactoringProvider(Of ParameterListSyntax, ParameterSyntax)

        Protected Overrides ReadOnly Property ListName As String = FeaturesResources.parameter_list
        Protected Overrides ReadOnly Property ItemNamePlural As String = FeaturesResources.parameters
        Protected Overrides ReadOnly Property ItemNameSingular As String = FeaturesResources.parameter

        Protected Overrides Function GetListItems(listSyntax As ParameterListSyntax) As SeparatedSyntaxList(Of ParameterSyntax)
            Return listSyntax.Parameters
        End Function

        Protected Overrides Function GetApplicableList(node As SyntaxNode) As ParameterListSyntax
            Return VisualBasicSyntaxGenerator.GetParameterList(node)
        End Function

        Protected Overrides Function PositionIsApplicable(position As Integer, listSyntax As ParameterListSyntax) As Boolean
            Dim declaration = listSyntax.Parent

            Dim generator = VisualBasicSyntaxGenerator.Instance
            Dim attributes = generator.GetAttributes(declaration)

            ' We want to offer this feature in the header of the member.  For now, we consider
            ' the header to be the part after the attributes, to the end of the parameter list.
            Dim firstToken = If(attributes?.Count > 0,
                attributes.Last().GetLastToken().GetNextToken(),
                declaration.GetFirstToken())

            Dim lastToken = listSyntax.GetLastToken()

            Dim headerSpan = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End)
            Return headerSpan.IntersectsWith(position)
        End Function
    End Class
End Namespace
