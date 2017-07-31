﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_Cascade_ToImplementedMethod() As Task
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

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_Cascade_ToImplementedMethod_WithTuples() As Task
            Dim markup = <Text><![CDATA[
Interface I
    Sub Foo(x As (Integer, Integer), y As (String, String))
End Interface

Class C
    Implements I

    $$Public Sub Foo(x As (Integer, Integer), y As (String, String)) Implements I.Foo
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Interface I
    Sub Foo(y As (String, String), x As (Integer, Integer))
End Interface

Class C
    Implements I

    Public Sub Foo(y As (String, String), x As (Integer, Integer)) Implements I.Foo
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_Cascade_ToImplementingMethod() As Task
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

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_Cascade_ToOverriddenMethod() As Task
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

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_Cascade_ToOverridingMethod() As Task
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

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_Cascade_ToOverriddenMethod_Transitive() As Task

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

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_Cascade_ToOverridingMethod_Transitive() As Task

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

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_Cascade_ToMethods_Complex() As Task

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

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        Public Async Function TestReorderParameters_Cascade_ToOverridingMethod_IncludeParamTags() As Task

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

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
    End Class
End Namespace
