' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderParameters_Cascade_ToImplementedMethod()
            Dim markup = <Text><![CDATA[
Interface I
    Sub Foo(x As Integer, y As String)
End Interface

Class C
    Implements I

    $$Public Sub Foo(x As Integer, y As String) Implements I.Foo
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Interface I
    Sub Foo(y As String, x As Integer)
End Interface

Class C
    Implements I

    Public Sub Foo(y As String, x As Integer) Implements I.Foo
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderParameters_Cascade_ToImplementingMethod()
            Dim markup = <Text><![CDATA[
Interface I
    $$Sub Foo(x As Integer, y As String)
End Interface

Class C
    Implements I

    Public Sub Foo(x As Integer, y As String) Implements I.Foo
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Interface I
    Sub Foo(y As String, x As Integer)
End Interface

Class C
    Implements I

    Public Sub Foo(y As String, x As Integer) Implements I.Foo
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderParameters_Cascade_ToOverriddenMethod()
            Dim markup = <Text><![CDATA[
Class B
    Overridable Sub Foo(x As Integer, y As String)
    End Sub
End Class

Class D
    Inherits B

    $$Public Overrides Sub Foo(x As Integer, y As String)
        MyBase.Foo(x, y)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class B
    Overridable Sub Foo(y As String, x As Integer)
    End Sub
End Class

Class D
    Inherits B

    Public Overrides Sub Foo(y As String, x As Integer)
        MyBase.Foo(y, x)
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderParameters_Cascade_ToOverridingMethod()
            Dim markup = <Text><![CDATA[
Class B
    $$Overridable Sub Foo(x As Integer, y As String)
    End Sub
End Class

Class D
    Inherits B

    Public Overrides Sub Foo(x As Integer, y As String)
        MyBase.Foo(x, y)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class B
    Overridable Sub Foo(y As String, x As Integer)
    End Sub
End Class

Class D
    Inherits B

    Public Overrides Sub Foo(y As String, x As Integer)
        MyBase.Foo(y, x)
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderParameters_Cascade_ToOverriddenMethod_Transitive()

            Dim markup = <Text><![CDATA[
Class B
    Overridable Sub Foo(x As Integer, y As String)
    End Sub
End Class

Class C
    Inherits B
    Public Overrides Sub Foo(x As Integer, y As String)
        MyBase.Foo(x, y)
    End Sub
End Class

Class D
    Inherits C
    $$Public Overrides Sub Foo(x As Integer, y As String)
        MyBase.Foo(x, y)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class B
    Overridable Sub Foo(y As String, x As Integer)
    End Sub
End Class

Class C
    Inherits B
    Public Overrides Sub Foo(y As String, x As Integer)
        MyBase.Foo(y, x)
    End Sub
End Class

Class D
    Inherits C
    Public Overrides Sub Foo(y As String, x As Integer)
        MyBase.Foo(y, x)
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderParameters_Cascade_ToOverridingMethod_Transitive()

            Dim markup = <Text><![CDATA[
Class B
    $$Overridable Sub Foo(x As Integer, y As String)
    End Sub
End Class

Class C
    Inherits B
    Public Overrides Sub Foo(x As Integer, y As String)
        MyBase.Foo(x, y)
    End Sub
End Class

Class D
    Inherits C
    Public Overrides Sub Foo(x As Integer, y As String)
        MyBase.Foo(x, y)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class B
    Overridable Sub Foo(y As String, x As Integer)
    End Sub
End Class

Class C
    Inherits B
    Public Overrides Sub Foo(y As String, x As Integer)
        MyBase.Foo(y, x)
    End Sub
End Class

Class D
    Inherits C
    Public Overrides Sub Foo(y As String, x As Integer)
        MyBase.Foo(y, x)
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ReorderParameters_Cascade_ToMethods_Complex()

            '     B   I   I2
            '      \ / \ / 
            '       D  (I3)
            '      / \   \
            '   $$D2  D3  C

            Dim markup = <Text><![CDATA[
Class B
    Overridable Sub M(x As Integer, y As String)
    End Sub
End Class

Class D
    Inherits B
    Implements I
    Public Overrides Sub M(x As Integer, y As String) Implements I.M
    End Sub
End Class

Class D2
    Inherits D
    Overrides Sub $$M(x As Integer, y As String)
    End Sub
End Class

Class D3
    Inherits D
    Overrides Sub M(x As Integer, y As String)
    End Sub
End Class

Interface I
    Sub M(x As Integer, y As String)
End Interface

Interface I2
    Sub M(x As Integer, y As String)
End Interface

Interface I3
    Inherits I, I2
End Interface

Class C
    Implements I3
    Public Sub M(x As Integer, y As String) Implements I.M, I2.M
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class B
    Overridable Sub M(y As String, x As Integer)
    End Sub
End Class

Class D
    Inherits B
    Implements I
    Public Overrides Sub M(y As String, x As Integer) Implements I.M
    End Sub
End Class

Class D2
    Inherits D
    Overrides Sub M(y As String, x As Integer)
    End Sub
End Class

Class D3
    Inherits D
    Overrides Sub M(y As String, x As Integer)
    End Sub
End Class

Interface I
    Sub M(y As String, x As Integer)
End Interface

Interface I2
    Sub M(y As String, x As Integer)
End Interface

Interface I3
    Inherits I, I2
End Interface

Class C
    Implements I3
    Public Sub M(y As String, x As Integer) Implements I.M, I2.M
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Sub

        Public Sub ReorderParameters_Cascade_ToOverridingMethod_IncludeParamTags()

            Dim markup = <Text><![CDATA[
Class B
    ''' <param name="a"></param>
    ''' <param name="b"></param>
    Overridable Sub Foo(a As Integer, b As Integer)
    End Sub
End Class

Class D
    Inherits B

    ''' <param name="x"></param>
    ''' <param name="y"></param>
    Public Overrides Sub $$Foo(x As Integer, y As Integer)
        MyBase.Foo(x, y)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class B
    ''' <param name="b"></param>
    ''' <param name="a"></param>
    Overridable Sub Foo(b As Integer, a As Integer)
    End Sub
End Class

Class D
    Inherits B

    ''' <param name="y"></param>
    ''' <param name="x"></param>
    Public Overrides Sub Foo(y As Integer, x As Integer)
        MyBase.Foo(y, x)
    End Sub
End Class]]></Text>.NormalizedValue()

            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)

        End Sub
    End Class
End Namespace
