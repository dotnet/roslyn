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

        <WpfFact>
        Public Async Function AddOptionalParameter_ToConstructor() As Task
            Dim markup = <Text><![CDATA[
Class B
    Public Sub New()
        Me.New(1)
    End Sub

    Public Sub New$$(a As Integer)
        Dim q = New B(1)
    End Sub
End Class

Class D
    Inherits B
    Public Sub New()
        MyBase.New(1)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim updatedSignature As AddedParameterOrExistingIndex() = {
                New AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "x", CallSiteKind.Value, callSiteValue:="100", isRequired:=False, defaultValue:="10"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "y", CallSiteKind.Omitted, isRequired:=False, defaultValue:="11"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "z", CallSiteKind.Value, callSiteValue:="102", isRequired:=False, defaultValue:="12")}

            Dim updatedCode = <Text><![CDATA[
Class B
    Public Sub New()
        Me.New(1, 100, z:=102)
    End Sub

    Public Sub New(a As Integer, Optional x As Integer = 10, Optional y As Integer = 11, Optional z As Integer = 12)
        Dim q = New B(1, 100, z:=102)
    End Sub
End Class

Class D
    Inherits B
    Public Sub New()
        MyBase.New(1, 100, z:=102)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact>
        Public Async Function AddOptionalParameter_ToRaiseEvent() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Event E$$(a As Integer)

    Public Sub M()
        RaiseEvent E(1)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim updatedSignature As AddedParameterOrExistingIndex() = {
                New AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "x", CallSiteKind.Value, callSiteValue:="10"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "y", CallSiteKind.Value, callSiteValue:="11")}

            Dim updatedCode = <Text><![CDATA[
Class C
    Public Event E(a As Integer, x As Integer, y As Integer)

    Public Sub M()
        RaiseEvent E(1, 10, 11)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact>
        Public Async Function AddOptionalParameter_ToAttribute() As Task
            Dim markup = <Text><![CDATA[
<Some(1)>
Class SomeAttribute
    Inherits System.Attribute

    Public Sub New$$(a As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim updatedSignature As AddedParameterOrExistingIndex() = {
                New AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "x", CallSiteKind.Value, callSiteValue:="100", isRequired:=False, defaultValue:="10")}

            Dim updatedCode = <Text><![CDATA[
<Some(1, 100)>
Class SomeAttribute
    Inherits System.Attribute

    Public Sub New(a As Integer, Optional x As Integer = 10)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
    End Class
End Namespace
