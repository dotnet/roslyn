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
        Public Async Function AddOptionalParameter_CallsiteInferred_NoOptions() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub M$$()
        M()
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(a As Integer)
        M(TODO)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function AddOptionalParameter_CallsiteInferred_SingleLocal() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub M$$()
        Dim x = 7
        M()
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(a As Integer)
        Dim x = 7
        M(x)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function AddOptionalParameter_CallsiteInferred_MultipleLocals() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub M$$()
        Dim x = 7
        Dim y = 8
        M()
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(a As Integer)
        Dim x = 7
        Dim y = 8
        M(y)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function AddOptionalParameter_CallsiteInferred_SingleParameter() As Task
            Dim markup = <Text><![CDATA[
Class C
    Sub M$$(x As Integer)
        M(1)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(x As Integer, a As Integer)
        M(1, x)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function AddOptionalParameter_CallsiteInferred_SingleField() As Task
            Dim markup = <Text><![CDATA[
Class C
    Dim x As Integer = 7

    Sub M$$()
        M()
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Dim x As Integer = 7

    Sub M(a As Integer)
        M(x)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function AddOptionalParameter_CallsiteInferred_SingleProperty() As Task
            Dim markup = <Text><![CDATA[
Class C
    Property X As Integer

    Sub M$$()
        M()
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred)}
            Dim updatedCode = <Text><![CDATA[
Class C
    Property X As Integer

    Sub M(a As Integer)
        M(X)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <Fact>
        Public Async Function AddOptionalParameter_CallsiteInferred_ImplicitlyConvertable() As Task
            Dim markup = <Text><![CDATA[
Class B
End Class

Class D
    Inherits B
End Class

Class C
    Sub M$$()
        Dim d As D
        M()
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                AddedParameterOrExistingIndex.CreateAdded("B", "b", CallSiteKind.Inferred)}
            Dim updatedCode = <Text><![CDATA[
Class B
End Class

Class D
    Inherits B
End Class

Class C
    Sub M(b As B)
        Dim d As D
        M(d)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
    End Class
End Namespace
