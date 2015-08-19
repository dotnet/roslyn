' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
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
        Public Sub Creation()
            Test(<code>
$$</code>, <code>$$</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub [Imports]()
            Test(<code>Imports _
    $$
</code>, <code>Imports$$
</code>)
        End Sub

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub [Namespace]()
            Test(<code>Namespace NS
    $$
End Namespace</code>, <code>Namespace NS$$</code>)
        End Sub

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub [Class]()
            Test(<code>Class C
    $$
End Class</code>, <code>Class C$$</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub Method()
            Test(<code>Class C
    Sub Method()
        $$
    End Sub
End Class</code>, <code>Class C
    Sub Method()$$
End Class</code>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub [Dim]()
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
        Public Sub Dim1()
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
        Public Sub Dim2()
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

        <WorkItem(712977)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub Dim3()
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

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub Dim_After_MalformedStatement()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub [If]()
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

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub If_Trivia()
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

        <WorkItem(530591)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub If_Trivia2()
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

        <WorkItem(577920)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub EndOfFile_SkippedToken()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub WithLineContinuation()
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

        Protected Overrides Function CreateWorkspace(code() As String) As TestWorkspace
            Return VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
        End Function
    End Class
End Namespace
