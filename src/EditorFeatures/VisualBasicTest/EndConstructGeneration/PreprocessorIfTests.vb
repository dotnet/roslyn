' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class PreprocessorIfTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function ApplyAfterHashIf() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="#If True Then",
                beforeCaret:={0, -1},
                after:="#If True Then

#End If",
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterHashIfWhenEndIfExists() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#If True Then
#End If",
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration), WorkItem(537976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537976")>
        Public Async Function DontApplyAfterHashElseIfWhenEndIfExists() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#If True Then
#ElseIf True Then
#End If",
                caret:={1, -1})
        End Function
    End Class
End Namespace
