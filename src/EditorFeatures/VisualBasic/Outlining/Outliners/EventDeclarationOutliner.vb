' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class EventDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of EventStatementSyntax)

        Private Shared Function GetBannerText(eventDeclaration As EventStatementSyntax) As String
            Dim builder As New BannerTextBuilder()

            For Each modifier In eventDeclaration.Modifiers
                builder.Append(modifier.ToString())
                builder.Append(" "c)
            Next

            If Not eventDeclaration.CustomKeyword.Kind = SyntaxKind.None Then
                builder.Append(eventDeclaration.CustomKeyword.ToString())
                builder.Append(" "c)
            End If

            builder.Append(eventDeclaration.DeclarationKeyword.ToString())
            builder.Append(" "c)
            builder.Append(eventDeclaration.Identifier.ToString())

            builder.AppendParameterList(eventDeclaration.ParameterList, emptyParentheses:=False)
            builder.AppendAsClause(eventDeclaration.AsClause)
            builder.AppendImplementsClause(eventDeclaration.ImplementsClause)

            builder.Append(" "c)
            builder.Append(Ellipsis)

            Return builder.ToString()
        End Function

        Protected Overrides Sub CollectOutliningSpans(eventDeclaration As EventStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(eventDeclaration, spans)

            Dim eventBlock = TryCast(eventDeclaration.Parent, EventBlockSyntax)
            If eventBlock IsNot Nothing AndAlso
               Not eventBlock.EndEventStatement.IsMissing Then
                spans.Add(VisualBasicOutliningHelpers.CreateRegionFromBlock(
                                eventBlock,
                                GetBannerText(eventDeclaration),
                                autoCollapse:=True))

                VisualBasicOutliningHelpers.CollectCommentsRegions(eventBlock.EndEventStatement, spans)
            End If
        End Sub
    End Class
End Namespace
