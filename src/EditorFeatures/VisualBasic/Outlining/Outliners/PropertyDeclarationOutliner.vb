' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class PropertyDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of PropertyStatementSyntax)

        Private Shared Function GetBannerText(propertyDeclaration As PropertyStatementSyntax) As String
            Dim builder As New BannerTextBuilder()

            For Each modifier In propertyDeclaration.Modifiers
                builder.Append(modifier.ToString())
                builder.Append(" "c)
            Next

            builder.Append(propertyDeclaration.DeclarationKeyword.ToString())
            builder.Append(" "c)
            builder.Append(propertyDeclaration.Identifier.ToString())

            builder.AppendParameterList(propertyDeclaration.ParameterList, emptyParentheses:=False)
            builder.AppendAsClause(propertyDeclaration.AsClause)
            builder.AppendImplementsClause(propertyDeclaration.ImplementsClause)

            builder.Append(" "c)
            builder.Append(Ellipsis)

            Return builder.ToString()
        End Function

        Protected Overrides Sub CollectOutliningSpans(propertyDeclaration As PropertyStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(propertyDeclaration, spans)

            Dim propertyBlock = TryCast(propertyDeclaration.Parent, PropertyBlockSyntax)
            If propertyBlock IsNot Nothing AndAlso
               Not propertyBlock.EndPropertyStatement.IsMissing Then
                spans.Add(
                    VisualBasicOutliningHelpers.CreateRegionFromBlock(
                        propertyBlock,
                        GetBannerText(propertyDeclaration),
                        autoCollapse:=True))

                VisualBasicOutliningHelpers.CollectCommentsRegions(propertyBlock.EndPropertyStatement, spans)
            End If
        End Sub
    End Class
End Namespace
