' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class TypeDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of TypeStatementSyntax)

        Private Shared Function GetBannerText(typeDeclaration As TypeStatementSyntax) As String
            Dim builder As New BannerTextBuilder()

            For Each modifier In typeDeclaration.Modifiers
                builder.Append(modifier.ToString())
                builder.Append(" "c)
            Next

            builder.Append(typeDeclaration.DeclarationKeyword.ToString())
            builder.Append(" "c)
            builder.Append(typeDeclaration.Identifier.ToString())

            builder.AppendTypeParameterList(typeDeclaration.TypeParameterList)

            builder.Append(" "c)
            builder.Append(Ellipsis)

            Return builder.ToString()
        End Function

        Protected Overrides Sub CollectOutliningSpans(typeDeclaration As TypeStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(typeDeclaration, spans)

            Dim typeBlock = TryCast(typeDeclaration.Parent, TypeBlockSyntax)
            If typeBlock IsNot Nothing Then
                If Not typeBlock.EndBlockStatement.IsMissing Then

                    spans.Add(
                            VisualBasicOutliningHelpers.CreateRegionFromBlock(
                                typeBlock,
                                GetBannerText(typeDeclaration),
                                autoCollapse:=False))

                    VisualBasicOutliningHelpers.CollectCommentsRegions(typeBlock.EndBlockStatement, spans)
                End If
            End If
        End Sub
    End Class
End Namespace
