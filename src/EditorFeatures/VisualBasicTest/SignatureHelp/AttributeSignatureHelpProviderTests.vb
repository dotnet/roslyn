' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class AttributeSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New AttributeSignatureHelpProvider()
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_AttributeConstructor_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
<My($$
Public Class Foo
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

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_AttributeConstructor_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
<My($$
Public Class Foo
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

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_AttributeConstructor_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
<My($$
Public Class Foo
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

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_AttributeConstructor_BrowsableStateMixed() As Task

            Dim markup = <Text><![CDATA[
<My($$
Public Class Foo
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WorkItem(1094379)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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