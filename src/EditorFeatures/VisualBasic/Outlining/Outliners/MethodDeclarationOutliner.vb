' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class MethodDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of MethodStatementSyntax)

        Private Shared Function GetBannerText(methodDeclaration As MethodStatementSyntax) As String
            Dim builder As New BannerTextBuilder()
            For Each modifier In methodDeclaration.Modifiers
                builder.Append(modifier.ToString())
                builder.Append(" "c)
            Next

            builder.Append(methodDeclaration.DeclarationKeyword.ToString())
            builder.Append(" "c)
            builder.Append(methodDeclaration.Identifier.ToString())

            builder.AppendTypeParameterList(methodDeclaration.TypeParameterList)
            builder.AppendParameterList(methodDeclaration.ParameterList, emptyParentheses:=True)
            builder.AppendAsClause(methodDeclaration.AsClause)
            builder.AppendHandlesClause(methodDeclaration.HandlesClause)
            builder.AppendImplementsClause(methodDeclaration.ImplementsClause)

            builder.Append(" "c)
            builder.Append(Ellipsis)

            Return builder.ToString()
        End Function

        Protected Overrides Sub CollectOutliningSpans(methodDeclaration As MethodStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(methodDeclaration, spans)

            Dim methodBlock = TryCast(methodDeclaration.Parent, MethodBlockSyntax)
            If methodBlock IsNot Nothing Then
                If Not methodBlock.EndBlockStatement.IsMissing Then
                    spans.Add(
                        VisualBasicOutliningHelpers.CreateRegionFromBlock(
                            methodBlock,
                            GetBannerText(methodDeclaration),
                            autoCollapse:=True))
                End If
            End If
        End Sub
    End Class
End Namespace
