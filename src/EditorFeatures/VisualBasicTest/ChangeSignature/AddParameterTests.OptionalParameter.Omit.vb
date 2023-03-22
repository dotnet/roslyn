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
        Public Async Function AddOptionalParameter_ToEmptySignature_CallsiteOmitted() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub M$$()
        M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim updatedSignature As AddedParameterOrExistingIndex() = {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Omitted, isRequired:=False, defaultValue:="1")}

            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(Optional a As Integer = 1)
        M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact>
        Public Async Function AddOptionalParameter_AfterRequiredParameter_CallsiteOmitted() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub M$$(x As Integer)
        M(1)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim updatedSignature As AddedParameterOrExistingIndex() = {
                        New AddedParameterOrExistingIndex(0),
                        AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Omitted, isRequired:=False, defaultValue:="1")}

            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(x As Integer, Optional a As Integer = 1)
        M(1)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact>
        Public Async Function AddOptionalParameter_BeforeOptionalParameter_CallsiteOmitted() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub M$$(Optional x As Integer = 2)
        M()
        M(2)
        M(x:=2)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim updatedSignature As AddedParameterOrExistingIndex() = {
                        AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Omitted, isRequired:=False, defaultValue:="1"),
                        New AddedParameterOrExistingIndex(0)}

            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(Optional a As Integer = 1, Optional x As Integer = 2)
        M()
        M(x:=2)
        M(x:=2)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact>
        Public Async Function AddOptionalParameter_BeforeExpandedParamsArray_CallsiteOmitted() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub M$$(ParamArray p As Integer())
        M()
        M(1)
        M(1, 2)
        M(1, 2, 3)
    End Sub
End Class]]></Text>.NormalizedValue()

            ' This is an illegal configuration in VB, but can happen if cascaded from the legal C#
            ' version of this configuration. We reinterpret OMIT as TODO in this case.
            Dim updatedSignature As AddedParameterOrExistingIndex() = {
                        AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Omitted, isRequired:=False, defaultValue:="1"),
                        New AddedParameterOrExistingIndex(0)}

            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(Optional a As Integer = 1, ParamArray p As Integer())
        M(TODO)
        M(TODO, 1)
        M(TODO, 1, 2)
        M(TODO, 1, 2, 3)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact>
        Public Async Function AddOptionalParameterWithOmittedCallsiteToAttributeConstructor() As Task
            Dim markup = <Text><![CDATA[
<Some(1, 2, 4)>
Class SomeAttribute
    Inherits System.Attribute
    Sub New$$(a As Integer, b As Integer, Optional y As Integer = 4)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim permutation = {
                New AddedParameterOrExistingIndex(0),
                New AddedParameterOrExistingIndex(1),
                AddedParameterOrExistingIndex.CreateAdded("Integer", "x", CallSiteKind.Omitted, isRequired:=False, defaultValue:="3"),
                New AddedParameterOrExistingIndex(2)}

            Dim updatedCode = <Text><![CDATA[
<Some(1, 2, y:=4)>
Class SomeAttribute
    Inherits System.Attribute
    Sub New(a As Integer, b As Integer, Optional x As Integer = 3, Optional y As Integer = 4)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
    End Class
End Namespace
