' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.VBFeaturesResources

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class GenericNameSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New GenericNameSignatureHelpProvider()
        End Function

#Region "Declaring generic type objects"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWith1ParameterUnterminated()
            Dim markup = <a><![CDATA[
Class G(Of T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWith1ParameterTerminated()
            Dim markup = <a><![CDATA[
Class G(Of T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWith2ParametersOn1()
            Dim markup = <a><![CDATA[
Class G(Of S, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S, T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWith2ParametersOn2()
            Dim markup = <a><![CDATA[
Class G(Of S, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of Integer, $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S, T)", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWith2ParametersOn1XmlDoc()
            Dim markup = <a><![CDATA[
''' <summary>
''' SummaryG
''' </summary>
''' <typeparam name="S">ParamS. Also see <see cref="C"/></typeparam>
''' <typeparam name="T">ParamT</typeparam>
Class G(Of S, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S, T)", "SummaryG", "ParamS. Also see C", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWith2ParametersOn2XmlDoc()
            Dim markup = <a><![CDATA[
''' <summary>
''' SummaryG
''' </summary>
''' <typeparam name="S">ParamS</typeparam>
''' <typeparam name="T">ParamT. Also see <see cref="C"/></typeparam>
Class G(Of S, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of Integer, $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S, T)", "SummaryG", "ParamT. Also see C", currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(827031)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWith2ParametersOn1XmlDocReferencingTypeParams()
            Dim markup = <a><![CDATA[
''' <summary>
''' SummaryG
''' </summary>
''' <typeparam name="S">ParamS. Also see <see cref="T"/></typeparam>
''' <typeparam name="T">ParamT. Also see <see cref="S"/></typeparam>
Class G(Of S, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S, T)", "SummaryG", "ParamS. Also see T", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(827031)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWith2ParametersOn2XmlDocReferencingTypeParams()
            Dim markup = <a><![CDATA[
''' <summary>
''' SummaryG
''' </summary>
''' <typeparam name="S">ParamS. Also see <see cref="T"/></typeparam>
''' <typeparam name="T">ParamT. Also see <see cref="S"/></typeparam>
Class G(Of S, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of Integer, $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S, T)", "SummaryG", "ParamT. Also see S", currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

#End Region

#Region "Constraints on generic types"
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsStructure()
            Dim markup = <a><![CDATA[
Class G(Of S As Structure, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As Structure, T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsClass()
            Dim markup = <a><![CDATA[
Class G(Of S As Class, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As Class, T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsNew()
            Dim markup = <a><![CDATA[
Class G(Of S As New, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As New, T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsBase()
            Dim markup = <a><![CDATA[
Class SomeBaseClass
End Class

Class G(Of S As SomeBaseClass, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As SomeBaseClass, T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsBaseGenericWithGeneric()
            Dim markup = <a><![CDATA[
Class SomeBaseClass(Of X)
End Class

Class G(Of S As SomeBaseClass(Of S), T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As SomeBaseClass(Of S), T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsBaseGenericWithNonGeneric()
            Dim markup = <a><![CDATA[
Class SomeBaseClass(Of X)
End Class

Class G(Of S As SomeBaseClass(Of Integer), T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As SomeBaseClass(Of Integer), T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsBaseGenericWithGenericNested()
            Dim markup = <a><![CDATA[
Class SomeBaseClass(Of X)
End Class

Class G(Of S As SomeBaseClass(Of SomeBaseClass(Of S)), T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As SomeBaseClass(Of SomeBaseClass(Of S)), T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsDeriveFromAnotherGenericParameter()
            Dim markup = <a><![CDATA[
Class G(Of S As T, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As T, T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsMixed1()
            Dim markup = <a><![CDATA[
Class SomeBaseClass
End Class

Interface IFoo
End Interface

''' <summary>
''' SummaryG
''' </summary>
''' <typeparam name="S">ParamS</typeparam>
''' <typeparam name="T">ParamT</typeparam>
Class G(Of S As {SomeBaseClass, New}, T As {Class, S, IFoo, New})
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As {SomeBaseClass, New}, T As {Class, S, IFoo, New})", "SummaryG", "ParamS", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DeclaringGenericTypeWithConstraintsMixed2()
            Dim markup = <a><![CDATA[
Class SomeBaseClass
End Class

Interface IFoo
End Interface

''' <summary>
''' SummaryG
''' </summary>
''' <typeparam name="S">ParamS</typeparam>
''' <typeparam name="T">ParamT</typeparam>
Class G(Of S As {SomeBaseClass, New}, T As {Class, S, IFoo, New})
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of Bar, $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S As {SomeBaseClass, New}, T As {Class, S, IFoo, New})", "SummaryG", "ParamT", currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

#End Region

#Region "Generic member invocation"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokingGenericMethodWith1ParameterUnterminated()
            Dim markup = <a><![CDATA[
Class C
    Function Foo(Of T)(arg As T) As T
    End Function
   
    Sub Bar()
        [|Foo(Of $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of T)(arg As T) As T", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokingGenericMethodWith1ParameterTerminated()
            Dim markup = <a><![CDATA[
Class C
    Function Foo(Of T)(arg As T) As T
    End Function
   
    Sub Bar()
        [|Foo(Of $$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of T)(arg As T) As T", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokingGenericMethodWith2ParametersOn1()
            Dim markup = <a><![CDATA[
Class C
    Function Foo(Of S, T)(arg As T) As S
    End Function
   
    Sub Bar()
        [|Foo(Of $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of S, T)(arg As T) As S", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokingGenericMethodWith2ParametersOn2()
            Dim markup = <a><![CDATA[
Class C
    Function Foo(Of S, T)(arg As T) As T
    End Function
   
    Sub Bar()
        [|Foo(Of Integer, $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of S, T)(arg As T) As T", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokingGenericMethodWith2ParametersOn1XmlDoc()
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' FooSummary
    ''' </summary>
    ''' <typeparam name="S">ParamS</typeparam>
    ''' <typeparam name="T">ParamT</typeparam>
    Function Foo(Of S, T)(arg As T) As S
    End Function
   
    Sub Bar()
        [|Foo(Of $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of S, T)(arg As T) As S", "FooSummary", "ParamS", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokingGenericMethodWith2ParametersOn2XmlDoc()
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' FooSummary
    ''' </summary>
    ''' <typeparam name="S">ParamS</typeparam>
    ''' <typeparam name="T">ParamT</typeparam>
    Function Foo(Of S, T)(arg As T) As S
    End Function
   
    Sub Bar()
        [|Foo(Of Integer, $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of S, T)(arg As T) As S", "FooSummary", "ParamT", currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(544124)>
        <WorkItem(544123)>
        <WorkItem(684631)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub CallingGenericExtensionMethod()
            Dim markup = <a><![CDATA[
Imports System

Class D
End Class

Module ExtnMethods
    <Runtime.CompilerServices.Extension()>
    Function Foo(Of S, T)(ByRef dClass As D, objS as S, objT As T) As S
    End Function
End Module

Class C
    Sub Bar()
        Dim obj As D = Nothing
        obj.[|Foo(Of $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{Extension}> D.Foo(Of S, T)(objS As S, objT As T) As S", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

#End Region

#Region "Constraints on generic methods"
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokingGenericMethodTypeWithConstraintsMixed1()
            Dim markup = <a><![CDATA[
Class SomeBaseClass
End Class

Interface IFoo
End Interface

Class C
    ''' <summary>
    ''' FooSummary
    ''' </summary>
    ''' <typeparam name="S">ParamS</typeparam>
    ''' <typeparam name="T">ParamT</typeparam>
    Function Foo(Of S As {SomeBaseClass, New}, T As {Class, S, IFoo, New})(objS As S, objT As T) As T
    End Function

    Sub Bar()
        [|Foo(Of $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of S As {SomeBaseClass, New}, T As {Class, S, IFoo, New})(objS As S, objT As T) As T", "FooSummary", "ParamS", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokingGenericMethodWithConstraintsMixed2()
            Dim markup = <a><![CDATA[
Class SomeBaseClass
End Class

Interface IFoo
End Interface

Class C
    ''' <summary>
    ''' FooSummary
    ''' </summary>
    ''' <typeparam name="S">ParamS</typeparam>
    ''' <typeparam name="T">ParamT</typeparam>
    Function Foo(Of S As {SomeBaseClass, New}, T As {Class, S, IFoo, New})(objS As S, objT As T) As T
    End Function

    Sub Bar()
        [|Foo(Of Bas, $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of S As {SomeBaseClass, New}, T As {Class, S, IFoo, New})(objS As S, objT As T) As T", "FooSummary", "ParamT", currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub
#End Region

#Region "Trigger tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnTriggerSpace()

            Dim markup = <a><![CDATA[
Class G(Of T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnTriggerComma()

            Dim markup = <a><![CDATA[
Class G(Of S, T)
End Class

Class C
    Sub Foo()
        Dim q As [|G(Of Integer,$$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("G(Of S, T)", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestTriggerCharacters()
            Dim expectedTriggerCharacters() As Char = {","c, " "c}
            Dim unexpectedTriggerCharacters() As Char = {"["c, "<"c, "("c}

            VerifyTriggerCharacters(expectedTriggerCharacters, unexpectedTriggerCharacters)
        End Sub

#End Region

#Region "EditorBrowsable tests"
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_GenericMethod_BrowsableAlways()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As C
        cc.Foo(Of $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub Foo(Of T)(x As T)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of T)(x As T)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)

        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_GenericMethod_BrowsableNever()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As C
        cc.Foo(Of $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(Of T)(x As T)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of T)(x As T)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)

        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_GenericMethod_BrowsableAdvanced()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As C
        cc.Foo(Of $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Sub Foo(Of T)(x As T)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(Of T)(x As T)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic,
                                                       hideAdvancedMembers:=False)

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic,
                                                       hideAdvancedMembers:=True)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_GenericMethod_BrowsableMixed()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As C
        cc.Foo(Of $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub Foo(Of T)(x As T)
    End Sub

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(Of T, U)(x As T, y As U)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("C.Foo(Of T)(x As T)", String.Empty, String.Empty, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C.Foo(Of T)(x As T)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C.Foo(Of T, U)(x As T, y As U)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_GenericType_BrowsableAlways()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim c As C(Of $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Class C(Of T)
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of T)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_GenericType_BrowsableNever()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim c As C(Of $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class C(Of T)
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of T)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EditorBrowsable_GenericType_BrowsableAdvanced()
            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim c As C(Of $$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
Public Class C(Of T)
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of T)", String.Empty, String.Empty, currentParameterIndex:=0))

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
#End Region

    End Class
End Namespace
