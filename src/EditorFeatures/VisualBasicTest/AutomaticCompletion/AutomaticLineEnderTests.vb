' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
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
        Public Async Function TestNamespace() As Task
            Test(<code>Namespace NS
    $$
End Namespace</code>, <code>Namespace NS$$</code>)
        End Function

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestClass() As Task
            Test(<code>Class C
    $$
End Class</code>, <code>Class C$$</code>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestMethod() As Task
            Test(<code>Class C
    Sub Method()
        $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()$$
End Class</code>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDim() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDim1() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDim2() As Task
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
        End Function

        <WorkItem(712977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/712977")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDim3() As Task
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
        End Function

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDim_After_MalformedStatement() As Task
            Test(<code>Class C
    Sub Method()
        Dim _ ' test

        $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()
        Dim _ ' test
$$
    End Sub
End Class</code>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestIf() As Task
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
        End Function

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestIf_Trivia() As Task
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
        End Function

        <WorkItem(530591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530591")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestIf_Trivia2() As Task
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
        End Function

        <WorkItem(577920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577920")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestEndOfFile_SkippedToken() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestWithLineContinuation() As Task
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
        End Function

        Private Overloads Sub Test(expected As XElement, code As XElement)
            Test(expected.NormalizedValue(), code.NormalizedValue())
        End Sub

        Friend Overrides Function CreateCommandHandler(
            waitIndicator As Microsoft.CodeAnalysis.Editor.Host.IWaitIndicator,
            undoRegistry As ITextUndoHistoryRegistry,
            editorOperations As IEditorOperationsFactoryService
        ) As ICommandHandler(Of AutomaticLineEnderCommandArgs)

            Return New AutomaticLineEnderCommandHandler(waitIndicator, undoRegistry, editorOperations)
        End Function

        Protected Overrides Function CreateNextHandler(workspace As TestWorkspace) As Action
            Dim endConstructor = New EndConstructCommandHandler(
                                    GetExportedValue(Of IEditorOperationsFactoryService)(workspace),
                                    GetExportedValue(Of ITextUndoHistoryRegistry)(workspace))

            Dim view = workspace.Documents.Single().GetTextView()
            Dim buffer = workspace.Documents.Single().GetTextBuffer()

            Return Sub()
                       endConstructor.ExecuteCommand_AutomaticLineEnderCommandHandler(
                           New AutomaticLineEnderCommandArgs(view, buffer), Sub() Exit Sub)
                   End Sub
        End Function

        Protected Overrides Function CreateWorkspace(code As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(code)
        End Function
    End Class
End Namespace
