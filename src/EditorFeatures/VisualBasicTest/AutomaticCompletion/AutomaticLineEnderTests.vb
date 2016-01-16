' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports System.Threading.Tasks
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticCompletion
    Public Class AutomaticLineEnderTests
        Inherits AbstractAutomaticLineEnderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestCreation() As Task
            Await TestAsync(<code>
$$</code>, <code>$$</code>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestImports() As Task
            Await TestAsync(<code>Imports _
    $$
</code>, <code>Imports$$
</code>)
        End Function

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestNamespace() As Task
            Await TestAsync(<code>Namespace NS
    $$
End Namespace</code>, <code>Namespace NS$$</code>)
        End Function

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestClass() As Task
            Await TestAsync(<code>Class C
    $$
End Class</code>, <code>Class C$$</code>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestMethod() As Task
            Await TestAsync(<code>Class C
    Sub Method()
        $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()$$
End Class</code>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDim() As Task
            Await TestAsync(<code>Class C
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
            Await TestAsync(<code>Class C
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
            Await TestAsync(<code>Class C
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

        <WorkItem(712977)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDim3() As Task
            Await TestAsync(<code>Class C
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

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDim_After_MalformedStatement() As Task
            Await TestAsync(<code>Class C
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
            Await TestAsync(
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

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestIf_Trivia() As Task
            Await TestAsync(
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

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestIf_Trivia2() As Task
            Await TestAsync(
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

        <WorkItem(577920)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestEndOfFile_SkippedToken() As Task
            Await TestAsync(
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
            Await TestAsync(
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

        Private Overloads Async Function TestAsync(expected As XElement, code As XElement) As Task
            Await TestAsync(expected.NormalizedValue(), code.NormalizedValue())
        End Function

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

        Protected Overrides Function CreateWorkspaceAsync(code As String) As Task(Of TestWorkspace)
            Return TestWorkspaceFactory.CreateVisualBasicAsync(code)
        End Function
    End Class
End Namespace
