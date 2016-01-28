' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWith1ParameterUnterminated() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWith1ParameterTerminated() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWith2ParametersOn1() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWith2ParametersOn2() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWith2ParametersOn1XmlDoc() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWith2ParametersOn2XmlDoc() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <WorkItem(827031)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWith2ParametersOn1XmlDocReferencingTypeParams() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <WorkItem(827031)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWith2ParametersOn2XmlDocReferencingTypeParams() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

#End Region

#Region "Constraints on generic types"
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsStructure() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsClass() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsNew() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsBase() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsBaseGenericWithGeneric() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsBaseGenericWithNonGeneric() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsBaseGenericWithGenericNested() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsDeriveFromAnotherGenericParameter() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsMixed1() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDeclaringGenericTypeWithConstraintsMixed2() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

#End Region

#Region "Generic member invocation"

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvokingGenericMethodWith1ParameterUnterminated() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvokingGenericMethodWith1ParameterTerminated() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvokingGenericMethodWith2ParametersOn1() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvokingGenericMethodWith2ParametersOn2() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvokingGenericMethodWith2ParametersOn1XmlDoc() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvokingGenericMethodWith2ParametersOn2XmlDoc() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <WorkItem(544124)>
        <WorkItem(544123)>
        <WorkItem(684631)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestCallingGenericExtensionMethod() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

#End Region

#Region "Constraints on generic methods"
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvokingGenericMethodTypeWithConstraintsMixed1() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvokingGenericMethodWithConstraintsMixed2() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function
#End Region

#Region "Trigger tests"

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnTriggerSpace() As Task

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

            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnTriggerComma() As Task

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

            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestTriggerCharacters()
            Dim expectedTriggerCharacters() As Char = {","c, " "c}
            Dim unexpectedTriggerCharacters() As Char = {"["c, "<"c, "("c}

            VerifyTriggerCharacters(expectedTriggerCharacters, unexpectedTriggerCharacters)
        End Sub

#End Region

#Region "EditorBrowsable tests"
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestEditorBrowsable_GenericMethod_BrowsableAlways() As Task
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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)

        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestEditorBrowsable_GenericMethod_BrowsableNever() As Task
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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)

        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestEditorBrowsable_GenericMethod_BrowsableAdvanced() As Task
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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic,
                                                       hideAdvancedMembers:=False)

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic,
                                                       hideAdvancedMembers:=True)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestEditorBrowsable_GenericMethod_BrowsableMixed() As Task
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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestEditorBrowsable_GenericType_BrowsableAlways() As Task
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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestEditorBrowsable_GenericType_BrowsableNever() As Task
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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                       expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                       sourceLanguage:=LanguageNames.VisualBasic,
                                                       referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestEditorBrowsable_GenericType_BrowsableAdvanced() As Task
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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                       referencedCode:=referencedCode,
                                                       expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
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
#End Region

    End Class
End Namespace