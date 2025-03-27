' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class NamespaceBlockTests
        <WpfFact>
        Public Async Function TestApplyAfterNamespace() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Namespace goo",
                beforeCaret:={0, -1},
                after:="Namespace goo

End Namespace",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterNestedNamespace() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Namespace goo
Namespace bar
End Namespace",
                beforeCaret:={1, -1},
                after:="Namespace goo
Namespace bar

End Namespace
End Namespace",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyRecommit() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="NameSpace Bar
End Namespace",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidNSInMethod() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        NameSpace T
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidNSInModule() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Module M
    Namespace n
End Module",
                caret:={1, -1})
        End Function
    End Class
End Namespace
