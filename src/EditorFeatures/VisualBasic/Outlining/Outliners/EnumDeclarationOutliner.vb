' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class EnumDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of EnumStatementSyntax)

        Private Shared Function GetBannerText(enumDeclaration As EnumStatementSyntax) As String
            Dim builder As New BannerTextBuilder()

            For Each modifier In enumDeclaration.Modifiers
                builder.Append(modifier.ToString())
                builder.Append(" "c)
            Next

            builder.Append(enumDeclaration.EnumKeyword.ToString())
            builder.Append(" "c)
            builder.Append(enumDeclaration.Identifier.ToString())

            builder.AppendAsClause(enumDeclaration.UnderlyingType)

            builder.Append(" "c)
            builder.Append(Ellipsis)

            Return builder.ToString()
        End Function

        Protected Overrides Sub CollectOutliningSpans(enumDeclaration As EnumStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(enumDeclaration, spans)

            Dim enumBlock = TryCast(enumDeclaration.Parent, EnumBlockSyntax)
            If enumBlock IsNot Nothing Then
                If Not enumBlock.EndEnumStatement.IsMissing Then

                    spans.Add(
                            VisualBasicOutliningHelpers.CreateRegionFromBlock(
                                enumBlock,
                                GetBannerText(enumDeclaration),
                                autoCollapse:=True))

                    VisualBasicOutliningHelpers.CollectCommentsRegions(enumBlock.EndEnumStatement, spans)
                End If
            End If
        End Sub
    End Class
End Namespace
