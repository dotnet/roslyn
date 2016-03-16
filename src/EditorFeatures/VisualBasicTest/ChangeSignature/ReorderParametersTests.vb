' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub $$M(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParametersAndArguments() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub $$M(x As Integer, y As String)
        M(3, "hello")
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(y As String, x As Integer)
        M("hello", 3)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParametersAndArgumentsOfNestedCalls() As Task

            Dim markup = <Text><![CDATA[
Class C
    $$Function M(x As Integer, y As String) As Integer
        Return M(M(4, "inner"), "outer")
    End Function
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Function M(y As String, x As Integer) As Integer
        Return M("outer", M("inner", 4))
    End Function
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderConstructorParametersAndArguments() As Task

            Dim markup = <Text><![CDATA[
Class D
    Inherits C

    Sub New()
        MyBase.New(1, "two")
    End Sub
End Class

Class C
    Sub New()
        Me.New(1, "two")
    End Sub

    $$Sub New(x As Integer, y As String)
        Dim t = New C(1, "two")
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class D
    Inherits C

    Sub New()
        MyBase.New("two", 1)
    End Sub
End Class

Class C
    Sub New()
        Me.New("two", 1)
    End Sub

    Sub New(y As String, x As Integer)
        Dim t = New C("two", 1)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderAttributeConstructorParametersAndArguments() As Task

            Dim markup = <Text><![CDATA[
<C(1, "two")>
Class C
    Inherits Attribute

    $$Sub New(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
<C("two", 1)>
Class C
    Inherits Attribute

    Sub New(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderExtensionMethodParametersAndArguments_StaticCall() As Task

            Dim markup = <Text><![CDATA[
Class C
    Shared Sub Test()
        CExt.M(New C(), 1, 2, "three", "four", "five")
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    $$Public Sub M(ByVal this As C, x As Integer, y As Integer, Optional a As String = "test_a", Optional b As String = "test_b", Optional c As String = "test_c")
    End Sub
End Module]]></Text>.NormalizedValue()
            Dim permutation = {0, 2, 1, 5, 4, 3}
            Dim updatedCode = <Text><![CDATA[
Class C
    Shared Sub Test()
        CExt.M(New C(), 2, 1, "five", "four", "three")
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(ByVal this As C, y As Integer, x As Integer, Optional c As String = "test_c", Optional b As String = "test_b", Optional a As String = "test_a")
    End Sub
End Module]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderExtensionMethodParametersAndArguments_ExtensionCall() As Task

            Dim markup = <Text><![CDATA[
Class C
    Shared Sub Test()
        Dim c = New C()
        c.M(1, 2, "three", "four", "five")
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    $$Public Sub M(ByVal this As C, x As Integer, y As Integer, Optional a As String = "test_a", Optional b As String = "test_b", Optional c As String = "test_c")
    End Sub
End Module]]></Text>.NormalizedValue()
            Dim permutation = {0, 2, 1, 5, 4, 3}
            Dim updatedCode = <Text><![CDATA[
Class C
    Shared Sub Test()
        Dim c = New C()
        c.M(2, 1, "five", "four", "three")
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(ByVal this As C, y As Integer, x As Integer, Optional c As String = "test_c", Optional b As String = "test_b", Optional a As String = "test_a")
    End Sub
End Module]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamsMethodParametersAndArguments_ParamsAsArray() As Task

            Dim markup = <Text><![CDATA[
Class C
    $$Sub M(x As Integer, y As Integer, ParamArray p As Integer())
        M(x, y, {1, 2, 3})
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0, 2}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(y As Integer, x As Integer, ParamArray p As Integer())
        M(y, x, {1, 2, 3})
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamsMethodParametersAndArguments_ParamsExpanded() As Task

            Dim markup = <Text><![CDATA[
Class C
    $$Sub M(x As Integer, y As Integer, ParamArray p As Integer())
        M(x, y, 1, 2, 3)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0, 2}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(y As Integer, x As Integer, ParamArray p As Integer())
        M(y, x, 1, 2, 3)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderExtensionAndParamsMethodParametersAndArguments_VariedCallsites() As Task

            Dim markup = <Text><![CDATA[
Class C
    Shared Sub Test()
        Dim c = New C()
        c.M(1, 2)
        c.M(1, 2, {3, 4})
        c.M(1, 2, 3, 4)
        CExt.M(c, 1, 2)
        CExt.M(c, 1, 2, {3, 4})
        CExt.M(c, 1, 2, 3, 4)
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    $$Public Sub M(ByVal this As C, x As Integer, y As Integer, ParamArray p As Integer())
    End Sub
End Module]]></Text>.NormalizedValue()
            Dim permutation = {0, 2, 1, 3}
            Dim updatedCode = <Text><![CDATA[
Class C
    Shared Sub Test()
        Dim c = New C()
        c.M(2, 1)
        c.M(2, 1, {3, 4})
        c.M(2, 1, 3, 4)
        CExt.M(c, 2, 1)
        CExt.M(c, 2, 1, {3, 4})
        CExt.M(c, 2, 1, 3, 4)
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(ByVal this As C, y As Integer, x As Integer, ParamArray p As Integer())
    End Sub
End Module]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderIndexerParametersAndArguments() As Task

            Dim markup = <Text><![CDATA[
Class C
    Default Public $$Property Item(ByVal index1 As Integer, ByVal index2 As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub Foo()
        Dim c = New C()
        Dim x = c(1, 2)
        c(3, 4) = x
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Default Public Property Item(ByVal index2 As Integer, ByVal index1 As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub Foo()
        Dim c = New C()
        Dim x = c(2, 1)
        c(4, 3) = x
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_OnIndividualLines() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Sub Foo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    ''' <param name="x">x!</param>
    Sub Foo(z As Integer, y As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_OnSameLine() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param><param name="y">y!</param><param name="z">z!</param>
    $$Sub Foo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param><param name="y">y!</param><param name="x">x!</param>
    Sub Foo(z As Integer, y As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_OutOfOrder_MaintainsOrder() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    $$Sub Foo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    Sub Foo(z As Integer, y As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_InsufficientTags_MaintainsOrder() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="z">z!</param>
    $$Sub Foo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="z">z!</param>
    Sub Foo(z As Integer, y As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_ExcessiveTags_MaintainsOrder() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="w">w!</param>
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Sub Foo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="w">w!</param>
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    Sub Foo(z As Integer, y As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_IncorrectlyNamedTags_MaintainsOrder() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x2">x2!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Sub Foo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="x2">x2!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    Sub Foo(z As Integer, y As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_OnFunctions() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Function Foo(x As Integer, y As Integer, z As Integer) As Integer
        Return 1
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    ''' <param name="x">x!</param>
    Function Foo(z As Integer, y As Integer, x As Integer) As Integer
        Return 1
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_OnConstructors() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Sub New(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    ''' <param name="x">x!</param>
    Sub New(z As Integer, y As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParamTagsInDocComments_OnProperties() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Default Public Property Item(ByVal x As Integer, ByVal y As Integer, ByVal z As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class]]></Text>.NormalizedValue()
            Dim permutation = {2, 1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    ''' <param name="x">x!</param>
    Default Public Property Item(ByVal z As Integer, ByVal y As Integer, ByVal x As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParametersInCrefs() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <summary>
    ''' See <see cref="M(Integer, String)"/> and <see cref="M"/>
    ''' </summary>
    $$Sub M(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <summary>
    ''' See <see cref="M(String, Integer)"/> and <see cref="M"/>
    ''' </summary>
    Sub M(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
    End Class
End Namespace
