' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentationComments
    <Trait(Traits.Feature, Traits.Features.XmlTagCompletion)>
    Public Class XmlTagCompletionTests
        Inherits AbstractXmlTagCompletionTests

        Private Protected Overrides Function CreateCommandHandler(testWorkspace As TestWorkspace) As IChainedCommandHandler(Of TypeCharCommandArgs)
            Return testWorkspace.ExportProvider.GetCommandHandler(Of XmlTagCompletionCommandHandler)("XmlTagCompletionCommandHandler", ContentTypeNames.VisualBasicContentType)
        End Function

        Private Protected Overrides Function CreateTestWorkspace(initialMarkup As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(initialMarkup)
        End Function

        <WpfFact>
        Public Sub SimpleTagCompletion()
            Dim text = "
''' <goo$$
class c
end class"

            Dim expected = "
''' <goo>$$</goo>
class c
end class"

            Verify(text, expected, ">"c)
        End Sub

        <WpfFact>
        Public Sub NestedTagCompletion()
            Dim text = "
''' <summary>
''' <goo$$
''' </summary>
class c
end class"

            Dim expected = "
''' <summary>
''' <goo>$$</goo>
''' </summary>
class c
end class"

            Verify(text, expected, ">"c)
        End Sub

        <WpfFact>
        Public Sub CompleteBeforeIncompleteTag()
            Dim text = "
''' <goo$$
''' </summary>
class c
end class"

            Dim expected = "
''' <goo>$$</goo>
''' </summary>
class c
end class"

            Verify(text, expected, ">"c)
        End Sub

        <WpfFact>
        Public Sub NotEmptyElement()
            Dim text = "
''' <$$
class c
end class"

            Dim expected = "
''' <>$$
class c
end class"

            Verify(text, expected, ">"c)
        End Sub

        <WpfFact>
        Public Sub NotAlreadyCompleteTag()
            Dim text = "
''' <goo$$</goo>
class c
end class"

            Dim expected = "
''' <goo>$$</goo>
class c
end class"

            Verify(text, expected, ">"c)
        End Sub

        <WpfFact>
        Public Sub NotAlreadyCompleteTag2()
            Dim text = "
''' <goo$$
'''
''' </goo>
class c
end class"

            Dim expected = "
''' <goo>$$
'''
''' </goo>
class c
end class"

            Verify(text, expected, ">"c)
        End Sub

        <WpfFact>
        Public Sub SimpleSlashCompletion()
            Dim text = "
''' <goo><$$
class c
end class"

            Dim expected = "
''' <goo></goo>$$
class c
end class"

            Verify(text, expected, "/"c)
        End Sub

        <WpfFact>
        Public Sub NestedSlashTagCompletion()
            Dim text = "
''' <summary>
''' <goo><$$
''' </summary>
class c
end class"

            Dim expected = "
''' <summary>
''' <goo></goo>$$
''' </summary>
class c
end class"

            Verify(text, expected, "/"c)
        End Sub

        <WpfFact>
        Public Sub SlashCompleteBeforeIncompleteTag()
            Dim text = "
''' <goo><$$
''' </summary>
class c
end class"

            Dim expected = "
''' <goo></goo>$$
''' </summary>
class c
end class"

            Verify(text, expected, "/"c)
        End Sub

        <WpfFact>
        Public Sub SlashNotEmptyElement()
            Dim text = "
''' <><$$
class c
end class"

            Dim expected = "
''' <></$$
class c
end class"

            Verify(text, expected, "/"c)
        End Sub

        <WpfFact>
        Public Sub SlashNotAlreadyCompleteTag()
            Dim text = "
''' <goo><$$goo>
class c
end class"

            Dim expected = "
''' <goo></$$goo>
class c
end class"

            Verify(text, expected, "/"c)
        End Sub

        <WpfFact>
        Public Sub SlashNotAlreadyCompleteTag2()
            Dim text = "
''' <goo>
'''
''' <$$goo>
class c
end class"

            Dim expected = "
''' <goo>
'''
''' </$$goo>
class c
end class"

            Verify(text, expected, "/"c)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638800")>
        Public Sub NestedIdenticalTags()
            Dim text = "
''' <goo><goo$$</goo>
class c
end class"

            Dim expected = "
''' <goo><goo>$$</goo></goo>
class c
end class"

            Verify(text, expected, ">"c)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638800")>
        Public Sub MultipleNestedIdenticalTags()
            Dim text = "
''' <goo><goo><goo$$</goo></goo>
class c
end class"

            Dim expected = "
''' <goo><goo><goo>$$</goo></goo></goo>
class c
end class"

            Verify(text, expected, ">"c)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")>
        Public Sub SlashNotIfCloseTagFollows()
            Dim text = "
''' <summary>
''' <$$
''' </summary>
class c
end class"

            Dim expected = "
''' <summary>
''' </$$
''' </summary>
class c
end class"

            Verify(text, expected, "/"c)
        End Sub

        <WpfFact>
        Public Sub TestSimpleTagCompletion()

            Dim text = <File><![CDATA[
''' <goo$$
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <goo>$$</goo>
Class C 
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact>
        Public Sub TestNestedTagCompletion()

            Dim text = <File><![CDATA[
''' <summary>
''' <goo$$
''' </summary>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <summary>
''' <goo>$$</goo>
''' </summary>
Class C 
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact>
        Public Sub TestCompleteBeforeIncompleteTag()

            Dim text = <File><![CDATA[
''' <goo$$
''' </summary>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <goo>$$</goo>
''' </summary>
Class C 
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact>
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

        <WpfFact>
        Public Sub TestNotAlreadyCompleteTag()

            Dim text = <File><![CDATA[
''' <goo$$</goo>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <goo>$$</goo>
Class C 
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact>
        Public Sub TestNotAlreadyCompleteTag2()

            Dim text = <File><![CDATA[
''' <goo$$
'''
''' </goo>
Class C 
End Class]]></File>

            Dim expected = <File><![CDATA[
''' <goo>$$
'''
''' </goo>
Class C 
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact>
        Public Sub TestNotOutsideDocComment()

            Dim text = <File><![CDATA[
Class C
    DIm z = <goo$$
End Class]]></File>

            Dim expected = <File><![CDATA[
Class C
    DIm z = <goo>$$
End Class]]></File>

            Verify(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), ">"c)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")>
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
