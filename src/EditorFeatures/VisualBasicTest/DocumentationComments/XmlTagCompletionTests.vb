' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentationComments
    Public Class XmlTagCompletionTests
        Inherits AbstractXmlTagCompletionTests

        Friend Overrides Function CreateCommandHandler(undoHistory As ITextUndoHistoryRegistry) As ICommandHandler(Of TypeCharCommandArgs)
            Return New XmlTagCompletionCommandHandler(undoHistory, TestWaitIndicator.Default)
        End Function

        Protected Overrides Function CreateTestWorkspaceAsync(initialMarkup As String) As Task(Of TestWorkspace)
            Return TestWorkspace.CreateVisualBasicAsync(initialMarkup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Async Function TestSimpleTagCompletion() As Task

            Dim text = <File><![CDATA[
''' <foo$$
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <foo>$$</foo>
Class C 
End Class]]></File>

            Await VerifyAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Async Function TestNestedTagCompletion() As Task

            Dim text = <File><![CDATA[
''' <summary>
''' <foo$$
''' </summary>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <summary>
''' <foo>$$</foo>
''' </summary>
Class C 
End Class]]></File>

            Await VerifyAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Async Function TestCompleteBeforeIncompleteTag() As Task

            Dim text = <File><![CDATA[
''' <foo$$
''' </summary>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <foo>$$</foo>
''' </summary>
Class C 
End Class]]></File>

            Await VerifyAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Async Function TestNotEmptyElement() As Task

            Dim text = <File><![CDATA[
''' <$$
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <>$$
Class C 
End Class]]></File>

            Await VerifyAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Async Function TestNotAlreadyCompleteTag() As Task

            Dim text = <File><![CDATA[
''' <foo$$</foo>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <foo>$$</foo>
Class C 
End Class]]></File>

            Await VerifyAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Async Function TestNotAlreadyCompleteTag2() As Task

            Dim text = <File><![CDATA[
''' <foo$$
'''
''' </foo>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <foo>$$
'''
''' </foo>
Class C 
End Class]]></File>

            Await VerifyAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Async Function TestNotOutsideDocComment() As Task

            Dim text = <File><![CDATA[
Class C
    DIm z = <foo$$
End Class]]></File>

            Dim expected = <File><![CDATA[
Class C
    DIm z = <foo>$$
End Class]]></File>

            Await VerifyAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Function

        <WorkItem(638235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Async Function TestNotCloseClosedTag() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <$$
''' </summary>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <summary>
''' </$$
''' </summary>
Class C 
End Class]]></File>

            Await VerifyAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), "/"c)
        End Function
    End Class
End Namespace