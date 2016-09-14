' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SyntaxDifferencing

Namespace Microsoft.CodeAnalysis.VisualBasic.SyntaxDifferencing
    <ExportLanguageService(GetType(SyntaxDifferenceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxDifferenceService
        Inherits SyntaxDifferenceService

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides Function ComputeBodyLevelMatch(oldBody As SyntaxNode, newBody As SyntaxNode) As SyntaxMatch
            Return New SyntaxMatch(StatementSyntaxComparer.Default.ComputeMatch(oldBody, newBody))
        End Function

        Public Overrides Function ComputeTopLevelMatch(oldRoot As SyntaxNode, newRoot As SyntaxNode) As SyntaxMatch
            Return New SyntaxMatch(TopSyntaxComparer.Instance.ComputeMatch(oldRoot, newRoot))
        End Function
    End Class
End Namespace