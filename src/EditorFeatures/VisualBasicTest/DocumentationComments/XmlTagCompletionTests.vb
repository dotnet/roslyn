﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentationComments
    Public Class XmlTagCompletionTests
        Inherits AbstractXmlTagCompletionTests

        Friend Overrides Function CreateCommandHandler(undoHistory As ITextUndoHistoryRegistry) As ICommandHandler(Of TypeCharCommandArgs)
            Return New XmlTagCompletionCommandHandler(undoHistory, TestWaitIndicator.Default)
        End Function

        Protected Overrides Function CreateTestWorkspace(initialMarkup As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(initialMarkup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Sub TestSimpleTagCompletion()

            Dim text = <File><![CDATA[
''' <foo$$
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <foo>$$</foo>
Class C 
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Sub TestNestedTagCompletion()

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

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Sub TestCompleteBeforeIncompleteTag()

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

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Sub TestNotEmptyElement()

            Dim text = <File><![CDATA[
''' <$$
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <>$$
Class C 
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Sub TestNotAlreadyCompleteTag()

            Dim text = <File><![CDATA[
''' <foo$$</foo>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <foo>$$</foo>
Class C 
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Sub TestNotAlreadyCompleteTag2()

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

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Sub TestNotOutsideDocComment()

            Dim text = <File><![CDATA[
Class C
    DIm z = <foo$$
End Class]]></File>

            Dim expected = <File><![CDATA[
Class C
    DIm z = <foo>$$
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WorkItem(638235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
        Public Sub TestNotCloseClosedTag()
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

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), "/"c)
        End Sub
    End Class
End Namespace
