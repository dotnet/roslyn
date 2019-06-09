' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticCompletion
    Public Class AutomaticLineEnderTests
        Inherits AbstractAutomaticLineEnderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestCreation()
            Test(<code>
$$</code>, <code>$$</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestImports()
            Test(<code>Imports _
    $$
</code>, <code>Imports$$
</code>)
        End Sub

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestNamespace()
            Test(<code>Namespace NS
    $$
End Namespace</code>, <code>Namespace NS$$</code>)
        End Sub

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestClass()
            Test(<code>Class C
    $$
End Class</code>, <code>Class C$$</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestMethod()
            Test(<code>Class C
    Sub Method()
        $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()$$
End Class</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestDim()
            Test(<code>Class C
    Sub Method()
        Dim _
            $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()
        Dim$$
    End Sub
End Class</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestDim1()
            Test(<code>Class C
    Sub Method()
        Dim i =
            $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()
        Dim i =$$
    End Sub
End Class</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestDim2()
            Test(<code>Class C
    Sub Method()
        Dim i
        $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()
        Dim i$$
    End Sub
End Class</code>)
        End Sub

        <WorkItem(712977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/712977")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestDim3()
            Test(<code>Class C
    Sub Method()
        Dim _
 _
        $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()
        Dim _
$$
    End Sub
End Class</code>)
        End Sub

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestDim_After_MalformedStatement()
            Test(<code>Class C
    Sub Method()
        Dim _  test

        $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()
        Dim _  test
$$
    End Sub
End Class</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestIf()
            Test(
<code>
Class C
    Sub Method()
        If True Then
            $$
        End If
    End Sub
End Class
</code>,
<code>
Class C
    Sub Method()
        If True$$
    End Sub
End Class
</code>)
        End Sub

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestIf_Trivia()
            Test(
<code>
Class C
    Sub Method()
        If True Then ' comment
            $$
        End If
    End Sub
End Class
</code>,
<code>
Class C
    Sub Method()
        If True $$' comment
    End Sub
End Class
</code>)
        End Sub

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestIf_Trivia2()
            Test(
<code>
Class C
    Sub Method()
        If True Then ' comment
            $$
        End If
    End Sub
End Class
</code>,
<code>
Class C
    Sub Method()
        If True ' comment$$
    End Sub
End Class
</code>)
        End Sub

        <WorkItem(577920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577920")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestEndOfFile_SkippedToken()
            Test(
<code>
Module M
    Sub Main()
    End Sub
End Module
"
$$
</code>,
<code>
Module M
    Sub Main()
    End Sub
End Module
"$$
</code>)
        End Sub

        ' The test verifies the integrated behavior which keeps the space '_'.
        ' This corresponds to the actual VS behavior.
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestWithLineContinuation()
            Test(
<code>
Module M
    Sub Main()
        Dim _ 
            $$
    End Sub
End Module
</code>,
<code>
Module M
    Sub Main()
        Dim _ $$
    End Sub
End Module
</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestWithLineContinuationCommentsAfterLineContinuation()
            Test(
<code>
Module M
    Sub Main()
        Dim _ ' Test
            $$
    End Sub
End Module
</code>,
<code>
Module M
    Sub Main()
        Dim _ ' Test$$
    End Sub
End Module
</code>)
        End Sub

        Private Overloads Sub Test(expected As XElement, code As XElement)
            Test(expected.NormalizedValue(), code.NormalizedValue())
        End Sub

        Friend Overrides Function CreateCommandHandler(
            undoRegistry As ITextUndoHistoryRegistry,
            editorOperations As IEditorOperationsFactoryService
        ) As IChainedCommandHandler(Of AutomaticLineEnderCommandArgs)

            Return New AutomaticLineEnderCommandHandler(undoRegistry, editorOperations)
        End Function

        Protected Overrides Function CreateNextHandler(workspace As TestWorkspace) As Action
            Dim endConstructor = New EndConstructCommandHandler(
                                    GetExportedValue(Of IEditorOperationsFactoryService)(workspace),
                                    GetExportedValue(Of ITextUndoHistoryRegistry)(workspace))

            Dim view = workspace.Documents.Single().GetTextView()
            Dim buffer = workspace.Documents.Single().GetTextBuffer()

            Return Sub()
                       endConstructor.ExecuteCommand_AutomaticLineEnderCommandHandler(
                           New AutomaticLineEnderCommandArgs(view, buffer), Sub() Exit Sub, TestCommandExecutionContext.Create())
                   End Sub
        End Function

        Protected Overrides Function CreateWorkspace(code As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(code)
        End Function
    End Class
End Namespace
