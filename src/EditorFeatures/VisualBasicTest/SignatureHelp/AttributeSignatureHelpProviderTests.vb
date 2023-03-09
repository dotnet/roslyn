' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class AttributeSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function GetSignatureHelpProviderType() As Type
            Return GetType(AttributeSignatureHelpProvider)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_AttributeConstructor_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
<My($$
Public Class Goo
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class MyAttribute
    Inherits System.Attribute
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_AttributeConstructor_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
<My($$
Public Class Goo
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class MyAttribute
    Inherits System.Attribute
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_AttributeConstructor_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
<My($$
Public Class Goo
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class MyAttribute
    Inherits System.Attribute
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Sub New()
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=True)

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                    referencedCode:=referencedCode,
                                    expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                    expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                    sourceLanguage:=LanguageNames.VisualBasic,
                                    referencedLanguage:=LanguageNames.VisualBasic,
                                    hideAdvancedMembers:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_AttributeConstructor_BrowsableStateMixed() As Task

            Dim markup = <Text><![CDATA[
<My($$
Public Class Goo
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class MyAttribute
    Inherits System.Attribute
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub New()
    End Sub
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New(x As Integer)
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("MyAttribute()", String.Empty, Nothing, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("MyAttribute()", String.Empty, Nothing, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("MyAttribute(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25830")>
        Public Async Function PickCorrectOverload_PickInt() As Task

            Dim markup = <Text><![CDATA[
<My(1$$)>
Public Class Goo
End Class

Public Class MyAttribute
    Inherits System.Attribute

    Public Sub New(i As String)
    End Sub
    Public Sub New(i As Integer)
    End Sub
    Public Sub New(i As Byte)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute(i As Byte)", String.Empty, Nothing, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute(i As Integer)", String.Empty, Nothing, currentParameterIndex:=0, isSelected:=True))
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute(i As String)", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25830")>
        Public Async Function PickCorrectOverload_PickString() As Task

            Dim markup = <Text><![CDATA[
<My("Hello"$$)>
Public Class Goo
End Class

Public Class MyAttribute
    Inherits System.Attribute

    Public Sub New(i As String)
    End Sub
    Public Sub New(i As Integer)
    End Sub
    Public Sub New(i As Byte)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute(i As Byte)", String.Empty, Nothing, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute(i As Integer)", String.Empty, Nothing, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("MyAttribute(i As String)", String.Empty, Nothing, currentParameterIndex:=0, isSelected:=True))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeConstructor_OnInvocation() As Task
            Dim markup = <Text><![CDATA[
Class SomethingAttribute
    Inherits System.Attribute
    Public Sub New(x As Integer, y As String, Optional obj As Object = Nothing)

    End Sub
End Class

<[|Something(0, ""$$|])>
Class D

End Class
]]></Text>.Value

            Dim expectedOrderedItems As New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("SomethingAttribute(x As Integer, y As String, [obj As Object = Nothing])",
                                                               String.Empty,
                                                               String.Empty,
                                                               currentParameterIndex:=1))
            Await TestAsync(markupWithPositionAndOptSpan:=markup, expectedOrderedItemsOrNull:=expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeConstructor_CurrentParameterName() As Task
            Dim markup = <Text><![CDATA[
Class SomethingAttribute
    Inherits System.Attribute
    Public x As Integer
    Public y As String
End Class

<Something(x:=0, y:=$$"")>
Class D

End Class
]]></Text>.Value

            Await VerifyCurrentParameterNameAsync(markupWithPosition:=markup, expectedParameterName:="y")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094379")>
        Public Async Function TestAttributeSigHelpWithNoArgumentList() As Task
            Dim markup = "
Imports System

<AttributeUsage$$>
Class C
End Class
"

            Await TestAsync(markup)
        End Function
    End Class
End Namespace
