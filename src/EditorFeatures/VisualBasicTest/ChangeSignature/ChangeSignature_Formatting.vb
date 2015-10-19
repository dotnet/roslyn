' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_KeepCountsPerLine()
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
            Dim updatedSignature = {5, 4, 3, 2, 1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Sub Method(f As Integer, e As Integer, d As Integer,
        c As Integer, b As Integer,
        a As Integer)

        Method(6,
            5, 4,
            3, 2, 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_SubMethods()
            Dim markup = <Text><![CDATA[
Class C
    Sub $$Method(x As Integer,
        y As Integer)
        Method(1,
            2)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Sub Method(y As Integer,
        x As Integer)
        Method(2,
            1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_FunctionMethods()
            Dim markup = <Text><![CDATA[
Class C
    Sub $$Method(x As Integer,
        y As Integer)
        Method(1,
            2)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Sub Method(y As Integer,
        x As Integer)
        Method(2,
            1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_Events()
            Dim markup = <Text><![CDATA[
Class C
    Public Event $$MyEvent(a As Integer,
        b As Integer)
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Public Event MyEvent(b As Integer,
        a As Integer)
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_CustomEvents()
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
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(b As Integer,
        a As Integer)

    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(b As Integer,
            a As Integer)
        End RaiseEvent
    End Event
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_Constructors()
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
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Sub New(b As Integer,
        a As Integer)
    End Sub

    Sub M()
        Dim x = New C(2,
            1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_Properties()
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
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Public Property NewProperty(y As Integer,
        x As Integer) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub M()
        Dim x = NewProperty(2,
            1)
        NewProperty(2,
            1) = x
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_Attribute()
            Dim markup = <Text><![CDATA[
<Custom(1,
    2)>
Class CustomAttribute
    Inherits Attribute
    Sub $$New(x As Integer, y As Integer)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
<Custom(2,
    1)>
Class CustomAttribute
    Inherits Attribute
    Sub New(y As Integer, x As Integer)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_DelegateFunction()
            Dim markup = <Text><![CDATA[
Class C
    Delegate Function $$MyDelegate(x As Integer,
        y As Integer)
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Function MyDelegate(y As Integer,
        x As Integer)
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_MultilineSubLambda()
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
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(b As Integer, a As Integer)
    Sub M(del As MyDelegate)
        M(Sub(b As Integer,
            a As Integer)
          End Sub)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_MultilineFunctionLambda()
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
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Function MyDelegate(b As Integer, a As Integer) As Integer
    Sub M(del As MyDelegate)
        M(Function(b As Integer,
            a As Integer)
              Return 1
          End Function)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_SingleLineSubLambda()
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub $$MyDelegate(a As Integer, b As Integer)
    Sub M(del As MyDelegate)
        M(Sub(a As Integer,
            b As Integer) System.Console.WriteLine("Test"))
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(b As Integer, a As Integer)
    Sub M(del As MyDelegate)
        M(Sub(b As Integer,
            a As Integer) System.Console.WriteLine("Test"))
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Formatting_SingleLineFunctionLambda()
            Dim markup = <Text><![CDATA[
Class C
    Delegate Function $$MyDelegate(a As Integer, b As Integer) As Integer
    Sub M(del As MyDelegate)
        M(Function(a As Integer,
            b As Integer) 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {1, 0}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Function MyDelegate(b As Integer, a As Integer) As Integer
    Sub M(del As MyDelegate)
        M(Function(b As Integer,
            a As Integer) 1)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub
    End Class
End Namespace
