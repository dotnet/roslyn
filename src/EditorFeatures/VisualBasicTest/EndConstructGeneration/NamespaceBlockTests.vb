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
    Public Class NamespaceBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterNamespace() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Namespace foo",
                beforeCaret:={0, -1},
                after:="Namespace foo

End Namespace",
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterNestedNamespace() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Namespace foo
Namespace bar
End Namespace",
                beforeCaret:={1, -1},
                after:="Namespace foo
Namespace bar

End Namespace
End Namespace",
                afterCaret:={2, -1})
        End Function


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyRecommit() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="NameSpace Bar
End Namespace",
                caret:={0, -1})
        End Function


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidNSInMethod() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        NameSpace T
    End Sub
End Class",
                caret:={2, -1})
        End Function


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidNSInModule() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Module M
    Namespace n
End Module",
                caret:={1, -1})
        End Function
    End Class
End Namespace