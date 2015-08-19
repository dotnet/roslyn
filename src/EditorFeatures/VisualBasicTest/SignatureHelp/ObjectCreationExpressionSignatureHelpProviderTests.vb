' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp

    Public Class ObjectCreationExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New ObjectCreationExpressionSignatureHelpProvider()
        End Function

#Region "Regular tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutParameters()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim obj = [|new C($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C()", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutParametersMethodXmlComments()
            Dim markup = <a><![CDATA[
Class C

    ''' <summary>
    ''' Summary for Foo. See <see cref="System.Object"/>
    ''' </summary>
    Sub New()
    End Sub

    Sub Foo()
        Dim obj = [|new C($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C()", "Summary for Foo. See Object", Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithParametersOn1()
            Dim markup = <a><![CDATA[
Class C
    Sub New(a As Integer, b As Integer)
    End Sub

    Sub Foo()
        Dim obj = [|new C($$2, 4|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithParametersXmlCommentsOn1()
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' Summary for Foo
    ''' </summary>
    ''' <param name="a">Param a</param>
    ''' <param name="b">Param b</param>
    Sub New(a As Integer, b As Integer)
    End Sub

    Sub Foo()
        Dim obj = [|new C($$2, 4|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(a As Integer, b As Integer)", "Summary for Foo", "Param a", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(545931)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestUnsupportedParameters()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim obj = [|new String($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("String(value As Char())", currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("String(c As Char, count As Integer)", currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("String(value As Char(), startIndex As Integer, length As Integer)", currentParameterIndex:=0))

            ' All the unsafe pointer overloads should be missing in VB
            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithParametersOn2()
            Dim markup = <a><![CDATA[
Class C
    Sub New(a As Integer, b As Integer)
    End Sub

    Sub Foo()
        Dim obj = [|new C(2, $$4|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithParametersXmlComentsOn2()
            Dim markup = <a><![CDATA[
Imports System
Class C
    ''' <summary>
    ''' Summary for Foo
    ''' </summary>
    ''' <param name="a">Param a</param>
    ''' <param name="b">Param b. See <see cref="System.IAsyncResult"/></param>
    Sub New(a As Integer, b As Integer)
    End Sub

    Sub Foo()
        Dim obj = [|new C(2, $$4|])
    End Sub
End Class]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(a As Integer, b As Integer)", "Summary for Foo", "Param b. See IAsyncResult", currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutClosingParen()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim obj = [|new C($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C()", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutClosingParenWithParameters()
            Dim markup = <a><![CDATA[
Class C
    Sub New(a As Integer, b As Integer)
    End Sub

    Sub Foo()
        Dim obj = [|new C($$2, 4
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutClosingParenWithParametersOn2()
            Dim markup = <a><![CDATA[
Class C
    Sub New(a As Integer, b As Integer)
    End Sub

    Sub Foo()
        Dim obj = [|new C(2, $$4
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnLambda()
            Dim markup = <a><![CDATA[
Imports System

Class C
    Sub Foo()
        Dim obj = [|new Action(Of Integer, Integer)($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Action(Of Integer, Integer)(Sub (Integer, Integer))", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

#End Region

#Region "Current Parameter Name"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestCurrentParameterName()
            Dim markup = <a><![CDATA[
Class C
    Sub New(int a, string b)
    End Sub

    Sub Foo()
        Dim obj = [|new C(b:=String.Empty, $$a:=2|])
    End Sub
End Class
]]></a>.Value

            VerifyCurrentParameterName(markup, "a")
        End Sub

#End Region

#Region "Trigger tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnTriggerParens()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim obj = [|new C($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C()", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnTriggerComma()
            Dim markup = <a><![CDATA[
Class C
    Sub New(a As Integer, b As Integer)
    End Sub
    Sub Foo()
        Dim obj = [|new C(2,$$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestNoInvocationOnSpace()
            Dim markup = <a><![CDATA[
Class C
    Sub New(a As Integer, b As Integer)
    End Sub
    Sub Foo()
        Dim obj = [|new C(2, $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestTriggerCharacters()
            Dim expectedCharacters() As Char = {","c, "("c}
            Dim unexpectedCharacters() As Char = {" "c, "["c, "<"c}

            VerifyTriggerCharacters(expectedCharacters, unexpectedCharacters)
        End Sub

#End Region

#Region "EditorBrowsable tests"
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_ObjectCreation_BrowsableAlways()
            Dim markup = <Text><![CDATA[
Class Program
    Sub Main(args As String())
        Dim x = New C($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub New(x As Integer)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_ObjectCreation_BrowsableNever()
            Dim markup = <Text><![CDATA[
Class Program
    Sub Main(args As String())
        Dim x = New C($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New(x As Integer)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_ObjectCreation_BrowsableAdvanced()
            Dim markup = <Text><![CDATA[
Class Program
    Sub Main(args As String())
        Dim x = New C($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Sub New(x As Integer)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic,
                                                       hideAdvancedMembers:=True)

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic,
                                                       hideAdvancedMembers:=False)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_ObjectCreation_BrowsableMixed()
            Dim markup = <Text><![CDATA[
Class Program
    Sub Main(args As String())
        Dim x = New C($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub New(x As Integer)
    End Sub

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub New(x As Integer, y As Integer)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("C(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(x As Integer, y As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Sub
#End Region
    End Class
End Namespace

