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
        Public Async Function TestAddParameter_Formatting_KeepCountsPerLine() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub $$Method(a As Integer, b As Integer, c As Integer,
        d As Integer, e As Integer,
        f As Integer)

        Method(1,
            2, 3,
            4, 5, 6)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(5),
                New AddedParameterOrExistingIndex(4),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(3),
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Sub Method(f As Integer, e As Integer, newIntegerParameter As Integer,
        d As Integer, c As Integer,
        b As Integer, a As Integer)

        Method(6,
            5, 12345, 4,
            3, 2, 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_SubMethods() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub $$Method(x As Integer,
        y As Integer)
        Method(1,
            2)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Sub Method(y As Integer,
        newIntegerParameter As Integer, x As Integer)
        Method(2,
            12345, 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_FunctionMethods() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub $$Method(x As Integer,
        y As Integer)
        Method(1,
            2)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Sub Method(y As Integer,
        newIntegerParameter As Integer, x As Integer)
        Method(2,
            12345, 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_Events() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Event $$MyEvent(a As Integer,
        b As Integer)
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Public Event MyEvent(b As Integer,
        newIntegerParameter As Integer, a As Integer)
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_CustomEvents() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub $$MyDelegate(a As Integer,
        b As Integer)

    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(a As Integer,
            b As Integer)
        End RaiseEvent
    End Event
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(b As Integer,
        newIntegerParameter As Integer, a As Integer)

    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(b As Integer,
            newIntegerParameter As Integer, a As Integer)
        End RaiseEvent
    End Event
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_Constructors() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub $$New(a As Integer,
        b As Integer)
    End Sub

    Sub M()
        Dim x = New C(1,
            2)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Sub New(b As Integer,
        newIntegerParameter As Integer, a As Integer)
    End Sub

    Sub M()
        Dim x = New C(2,
            12345, 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_Properties() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Property $$NewProperty(x As Integer, 
        y As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub M()
        Dim x = NewProperty(1,
            2)
        NewProperty(1,
            2) = x
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Public Property NewProperty(y As Integer,
        newIntegerParameter As Integer, x As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub M()
        Dim x = NewProperty(2,
            12345, 1)
        NewProperty(2,
            12345, 1) = x
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_Attribute() As Task
            Dim markup = <Text><![CDATA[
<Custom(1,
    2)>
Class CustomAttribute
    Inherits Attribute
    Sub $$New(x As Integer, y As Integer)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
<Custom(2,
    12345, 1)>
Class CustomAttribute
    Inherits Attribute
    Sub New(y As Integer, newIntegerParameter As Integer, x As Integer)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_DelegateFunction() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Function $$MyDelegate(x As Integer,
        y As Integer)
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Function MyDelegate(y As Integer,
        newIntegerParameter As Integer, x As Integer)
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_MultilineSubLambda() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub $$MyDelegate(a As Integer, b As Integer)
    Sub M(del As MyDelegate)
        M(Sub(a As Integer,
            b As Integer)
          End Sub)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(b As Integer, newIntegerParameter As Integer, a As Integer)
    Sub M(del As MyDelegate)
        M(Sub(b As Integer,
            newIntegerParameter As Integer, a As Integer)
          End Sub)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_MultilineFunctionLambda() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Function $$MyDelegate(a As Integer, b As Integer) As Integer
    Sub M(del As MyDelegate)
        M(Function(a As Integer,
            b As Integer)
              Return 1
          End Function)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Function MyDelegate(b As Integer, newIntegerParameter As Integer, a As Integer) As Integer
    Sub M(del As MyDelegate)
        M(Function(b As Integer,
            newIntegerParameter As Integer, a As Integer)
              Return 1
          End Function)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_SingleLineSubLambda() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub $$MyDelegate(a As Integer, b As Integer)
    Sub M(del As MyDelegate)
        M(Sub(a As Integer,
            b As Integer) System.Console.WriteLine("Test"))
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(b As Integer, newIntegerParameter As Integer, a As Integer)
    Sub M(del As MyDelegate)
        M(Sub(b As Integer,
            newIntegerParameter As Integer, a As Integer) System.Console.WriteLine("Test"))
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <Fact>
        Public Async Function TestAddParameter_Formatting_SingleLineFunctionLambda() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Function $$MyDelegate(a As Integer, b As Integer) As Integer
    Sub M(del As MyDelegate)
        M(Function(a As Integer,
            b As Integer) 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(1),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(0)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Function MyDelegate(b As Integer, newIntegerParameter As Integer, a As Integer) As Integer
    Sub M(del As MyDelegate)
        M(Function(b As Integer,
            newIntegerParameter As Integer, a As Integer) 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function
    End Class
End Namespace
