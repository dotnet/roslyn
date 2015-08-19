' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class ErrorKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ErrorOptionsAfterOn()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsAreExactly(<MethodBody>On |</MethodBody>, "Error Resume Next", "Error GoTo")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ErrorStatementInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Error")
        End Sub

        <WorkItem(899057)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ErrorStatementInLambda()
            Dim code = <File>
Public Class Z
    Public Sub Main()
        Dim c = Sub() |
    End Sub
End Class</File>

            VerifyRecommendationsContain(code, "Error")
        End Sub
    End Class
End Namespace
