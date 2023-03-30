' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class PreprocessorIfTests
        <WpfFact>
        Public Sub ApplyAfterHashIf()
            VerifyStatementEndConstructApplied(
                before:="#If True Then",
                beforeCaret:={0, -1},
                after:="#If True Then

#End If",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyAfterHashIfWhenEndIfExists()
            VerifyStatementEndConstructNotApplied(
                text:="#If True Then
#End If",
                caret:={0, -1})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537976")>
        Public Sub DoNotApplyAfterHashElseIfWhenEndIfExists()
            VerifyStatementEndConstructNotApplied(
                text:="#If True Then
#ElseIf True Then
#End If",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
