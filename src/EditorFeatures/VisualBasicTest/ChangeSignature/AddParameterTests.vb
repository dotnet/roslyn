' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    <Trait(Traits.Feature, Traits.Features.ChangeSignature)>
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <Fact>
        Public Async Function TestAddRemoveParameters() As Task

            Dim markup = <Text><![CDATA[
Module Program
    ''' <summary>
    ''' See <see cref="M(String, Integer, String, Boolean, Integer, String)"/>
    ''' </summary>
    ''' <param name="o">o!</param>
    ''' <param name="a">a!</param>
    ''' <param name="b">b!</param>
    ''' <param name="c">c!</param>
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    <System.Runtime.CompilerServices.Extension>
    Sub $$M(ByVal o As String, a As Integer, b As String, c As Boolean, Optional x As Integer = 0, Optional y As String = "Zero")
        Dim t = "Test"

        M(t, 1, "Two", True, 3, "Four")
        t.M(1, "Two", True, 3, "Four")

        M(t, 1, "Two", True, 3)
        M(t, 1, "Two", True)

        M(t, 1, "Two", True, 3, y:="Four")
        M(t, 1, "Two", c:=True)

        M(t, 1, "Two", True, y:="Four")
        M(t, 1, "Two", True, x:=3)

        M(t, 1, "Two", True, y:="Four", x:=3)
        M(t, 1, y:="Four", x:=3, b:="Two", c:=True)
        M(t, y:="Four", x:=3, c:=True, b:="Two", a:=1)
        M(y:="Four", x:=3, c:=True, b:="Two", a:=1, o:=t)
    End Sub
End Module

]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(0),
                New AddedParameterOrExistingIndex(3),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(5)}
            Dim updatedCode = <Text><![CDATA[
Module Program
    ''' <summary>
    ''' See <see cref="M(String, Boolean, Integer, Integer, String)"/>
    ''' </summary>
    ''' <param name="o">o!</param>
    ''' <param name="c">c!</param>
    ''' <param name="newIntegerParameter"></param>
    ''' <param name="a">a!</param>
    ''' <param name="y">y!</param>
    ''' 
    <System.Runtime.CompilerServices.Extension>
    Sub M(ByVal o As String, c As Boolean, newIntegerParameter As Integer, a As Integer, Optional y As String = "Zero")
        Dim t = "Test"

        M(t, True, 12345, 1, "Four")
        t.M(True, 12345, 1, "Four")

        M(t, True, 12345, 1)
        M(t, True, 12345, 1)

        M(t, True, 12345, 1, y:="Four")
        M(t, c:=True, newIntegerParameter:=12345, a:=1)

        M(t, True, 12345, 1, y:="Four")
        M(t, True, 12345, 1)

        M(t, True, 12345, 1, y:="Four")
        M(t, a:=1, newIntegerParameter:=12345, y:="Four", c:=True)
        M(t, y:="Four", newIntegerParameter:=12345, c:=True, a:=1)
        M(y:="Four", c:=True, newIntegerParameter:=12345, a:=1, o:=t)
    End Sub
End Module

]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameterToParameterlessMethod() As Task

            Dim markup = <Text><![CDATA[
Module Program
    Sub $$M()
        M()
    End Sub
End Module

]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer")}
            Dim updatedCode = <Text><![CDATA[
Module Program
    Sub M(newIntegerParameter As Integer)
        M(12345)
    End Sub
End Module

]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Parameters() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub $$M(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(y As String, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)

        End Function

        <Fact>
        Public Async Function TestAddParameter_ParametersAndArguments() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub $$M(x As Integer, y As String)
        M(3, "hello")
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(y As String, newIntegerParameter As Integer, x As Integer)
        M("hello", 12345, 3)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ParametersAndArgumentsOfNestedCalls() As Task

            Dim markup = <Text><![CDATA[
Class C
    $$Function M(x As Integer, y As String) As Integer
        Return M(M(4, "inner"), "outer")
    End Function
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Function M(y As String, newIntegerParameter As Integer, x As Integer) As Integer
        Return M("outer", 12345, M("inner", 12345, 4))
    End Function
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderConstructorParametersAndArguments() As Task

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
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class D
    Inherits C

    Sub New()
        MyBase.New("two", 12345, 1)
    End Sub
End Class

Class C
    Sub New()
        Me.New("two", 12345, 1)
    End Sub

    Sub New(y As String, newIntegerParameter As Integer, x As Integer)
        Dim t = New C("two", 12345, 1)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderAttributeConstructorParametersAndArguments() As Task

            Dim markup = <Text><![CDATA[
<C(1, "two")>
Class C
    Inherits Attribute

    $$Sub New(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
<C("two", 12345, 1)>
Class C
    Inherits Attribute

    Sub New(y As String, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ExtensionMethodParametersAndArguments_StaticCall() As Task

            Dim markup = <Text><![CDATA[
Class C
    Shared Sub Test()
        CExt.M(New C(), 1, 2, "three", "four", "five")
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M($$ByVal this As C, x As Integer, y As Integer, Optional a As String = "test_a", Optional b As String = "test_b", Optional c As String = "test_c")
    End Sub
End Module]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(0),
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(5),
                New AddedParameterOrExistingIndex(4),
                New AddedParameterOrExistingIndex(3)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Shared Sub Test()
        CExt.M(New C(), 2, 12345, 1, "five", "four", "three")
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(ByVal this As C, y As Integer, newIntegerParameter As Integer, x As Integer, Optional c As String = "test_c", Optional b As String = "test_b", Optional a As String = "test_a")
    End Sub
End Module]]></Text>.NormalizedValue()

            ' Although the `ParameterConfig` has 0 for the `SelectedIndex`, the UI dialog will make an adjustment
            ' and select parameter `y` instead because the `this` parameter cannot be moved or removed.
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation,
                                                     expectedUpdatedInvocationDocumentCode:=updatedCode, expectedSelectedIndex:=0)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderExtensionMethodParametersAndArguments_ExtensionCall() As Task

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
            Dim permutation = {
                New AddedParameterOrExistingIndex(0),
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(5),
                New AddedParameterOrExistingIndex(4),
                New AddedParameterOrExistingIndex(3)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Shared Sub Test()
        Dim c = New C()
        c.M(2, 12345, 1, "five", "four", "three")
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(ByVal this As C, y As Integer, newIntegerParameter As Integer, x As Integer, Optional c As String = "test_c", Optional b As String = "test_b", Optional a As String = "test_a")
    End Sub
End Module]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamsMethodParametersAndArguments_ParamsAsArray() As Task

            Dim markup = <Text><![CDATA[
Class C
    $$Sub M(x As Integer, y As Integer, ParamArray p As Integer())
        M(x, y, {1, 2, 3})
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(0),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(2)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(y As Integer, x As Integer, newIntegerParameter As Integer, ParamArray p As Integer())
        M(y, x, 12345, {1, 2, 3})
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamsMethodParametersAndArguments_ParamsExpanded() As Task

            Dim markup = <Text><![CDATA[
Class C
    $$Sub M(x As Integer, y As Integer, ParamArray p As Integer())
        M(x, y, 1, 2, 3)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(0),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(2)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(y As Integer, x As Integer, newIntegerParameter As Integer, ParamArray p As Integer())
        M(y, x, 12345, 1, 2, 3)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderExtensionAndParamsMethodParametersAndArguments_VariedCallsites() As Task

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
            Dim permutation = {
                New AddedParameterOrExistingIndex(0),
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(3)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Shared Sub Test()
        Dim c = New C()
        c.M(2, 12345, 1)
        c.M(2, 12345, 1, {3, 4})
        c.M(2, 12345, 1, 3, 4)
        CExt.M(c, 2, 12345, 1)
        CExt.M(c, 2, 12345, 1, {3, 4})
        CExt.M(c, 2, 12345, 1, 3, 4)
    End Sub
End Class

Module CExt
    <System.Runtime.CompilerServices.Extension()>
    Public Sub M(ByVal this As C, y As Integer, newIntegerParameter As Integer, x As Integer, ParamArray p As Integer())
    End Sub
End Module]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderIndexerParametersAndArguments() As Task

            Dim markup = <Text><![CDATA[
Class C
    Default Public $$Property Item(ByVal index1 As Integer, ByVal index2 As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub Goo()
        Dim c = New C()
        Dim x = c(1, 2)
        c(3, 4) = x
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Default Public Property Item(ByVal index2 As Integer, newIntegerParameter As Integer, ByVal index1 As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub Goo()
        Dim c = New C()
        Dim x = c(2, 12345, 1)
        c(4, 12345, 3) = x
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_OnIndividualLines() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Sub Goo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    ''' <param name="newIntegerParameter"></param>
    ''' <param name="x">x!</param>
    Sub Goo(z As Integer, y As Integer, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_OnSameLine() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param><param name="y">y!</param><param name="z">z!</param>
    $$Sub Goo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param><param name="y">y!</param><param name="newIntegerParameter"></param>
    ''' <param name="x">x!</param>
    Sub Goo(z As Integer, y As Integer, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_OutOfOrder_MaintainsOrder() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    $$Sub Goo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    Sub Goo(z As Integer, y As Integer, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_InsufficientTags_MaintainsOrder() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="z">z!</param>
    $$Sub Goo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="z">z!</param>
    Sub Goo(z As Integer, y As Integer, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_ExcessiveTags_MaintainsOrder() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="w">w!</param>
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Sub Goo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="w">w!</param>
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    Sub Goo(z As Integer, y As Integer, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_IncorrectlyNamedTags_MaintainsOrder() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x2">x2!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Sub Goo(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="x2">x2!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    Sub Goo(z As Integer, y As Integer, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_OnFunctions() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Function Goo(x As Integer, y As Integer, z As Integer) As Integer
        Return 1
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    ''' <param name="newIntegerParameter"></param>
    ''' <param name="x">x!</param>
    Function Goo(z As Integer, y As Integer, newIntegerParameter As Integer, x As Integer) As Integer
        Return 1
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_OnConstructors() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    ''' <param name="z">z!</param>
    $$Sub New(x As Integer, y As Integer, z As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    ''' <param name="newIntegerParameter"></param>
    ''' <param name="x">x!</param>
    Sub New(z As Integer, y As Integer, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParamTagsInDocComments_OnProperties() As Task

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
            Dim permutation = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <param name="z">z!</param>
    ''' <param name="y">y!</param>
    ''' <param name="newIntegerParameter"></param>
    ''' <param name="x">x!</param>
    Default Public Property Item(ByVal z As Integer, ByVal y As Integer, newIntegerParameter As Integer, ByVal x As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_ReorderParametersInCrefs() As Task

            Dim markup = <Text><![CDATA[
Class C
    ''' <summary>
    ''' See <see cref="M(Integer, String)"/> and <see cref="M"/>
    ''' </summary>
    $$Sub M(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <summary>
    ''' See <see cref="M(String, Integer, Integer)"/> and <see cref="M"/>
    ''' </summary>
    Sub M(y As String, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49941")>
        Public Async Function TestAddParameter_AddToInvocationWithoutParens() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub M()
        $$M
        M()
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer")}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(newIntegerParameter As Integer)
        M(12345)
        M(12345)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49941")>
        Public Async Function TestAddParameter_AddToInvocationWithoutParens_WithOptionalParam() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub M(Optional s As String = "str")
        $$M
        M()
        M("test")
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(newIntegerParameter As Integer, Optional s As String = "str")
        M(12345)
        M(12345)
        M(12345, "test")
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49941")>
        Public Async Function TestAddParameter_NoLastWhitespaceTrivia() As Task

            Dim markup = <Text><![CDATA[
Class C
''' <summary>
''' </summary>
''' <param name="a"></param>
Sub $$M(a As Integer)
End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation =
            {
                New AddedParameterOrExistingIndex(0),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "b", CallSiteKind.Value), "Integer")
            }

            Dim updatedCode = <Text><![CDATA[
Class C
    ''' <summary>
    ''' </summary>
    ''' <param name="a"></param>
    ''' <param name="b"></param>
    Sub M(a As Integer, b As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
    End Class
End Namespace
