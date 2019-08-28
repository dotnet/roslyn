' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class PreprocessorIfTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterHashIf()
            VerifyStatementEndConstructApplied(
                before:="#If True Then",
                beforeCaret:={0, -1},
                after:="#If True Then

#End If",
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashIfWhenEndIfExists()
            VerifyStatementEndConstructNotApplied(
                text:="#If True Then
#End If",
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration), WorkItem(537976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537976")>
        Public Sub DontApplyAfterHashElseIfWhenEndIfExists()
            VerifyStatementEndConstructNotApplied(
                text:="#If True Then
#ElseIf True Then
#End If",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
