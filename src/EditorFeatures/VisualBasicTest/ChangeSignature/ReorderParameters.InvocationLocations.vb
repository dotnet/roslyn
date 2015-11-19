' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests

#Region "Methods"
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeBeforeMethodName() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub $$Foo(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub Foo(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function



        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeInParameterList() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub Foo(x As Integer, $$y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub Foo(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeAfterParameterList() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub Foo(x As Integer, y As String)$$
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub Foo(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeBeforeMethodDeclaration() As Task
            Dim markup = <Text><![CDATA[
Class C
    $$Public Sub Foo(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub Foo(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnMetadataReference_InIdentifier_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        Dim m = DirectCast(Nothing, System.IFormattable).To$$String("test", Nothing)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.TheMemberIsDefinedInMetadata)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnMetadataReference_AtBeginningOfInvocation_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        Dim m = $$DirectCast(Nothing, System.IFormattable).ToString("test", Nothing)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.TheMemberIsDefinedInMetadata)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnMetadataReference_InArgumentsOfInvocation_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        Dim m = DirectCast(Nothing, System.IFormattable).ToString("test", $$Nothing)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.TheMemberIsDefinedInMetadata)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnMetadataReference_AfterInvocation_ShouldFail() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        Dim m = DirectCast(Nothing, System.IFormattable).ToString("test", Nothing)$$
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, expectedSuccess:=False, expectedErrorText:=FeaturesResources.TheMemberIsDefinedInMetadata)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeInMethodBody() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        $$
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub M(y As String, x As Integer)

    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnReference_BeginningOfIdentifier() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        $$T(x, y)
    End Sub

    Public Sub T(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        T(y, x)
    End Sub

    Public Sub T(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnReference_ArgumentList() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        T(x, $$y)
    End Sub

    Public Sub T(x As Integer, y As String)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        T(y, x)
    End Sub

    Public Sub T(y As String, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnReference_NestedCalls1() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        D($$J(x, y), y)
    End Sub

    Public Sub D(x As Integer, y As String)
    End Sub

    Public Function J(x As Integer, y As String) As Integer
        Return 1
    End Function
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        D(J(y, x), y)
    End Sub

    Public Sub D(x As Integer, y As String)
    End Sub

    Public Function J(y As String, x As Integer) As Integer
        Return 1
    End Function
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnReference_NestedCalls2() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        D$$(J(x, y), y)
    End Sub

    Public Sub D(x As Integer, y As String)
    End Sub

    Public Function J(x As Integer, y As String) As Integer
        Return 1
    End Function
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        D(y, J(x, y))
    End Sub

    Public Sub D(y As String, x As Integer)
    End Sub

    Public Function J(x As Integer, y As String) As Integer
        Return 1
    End Function
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnReference_NestedCalls3() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        D(J(x, y), $$y)
    End Sub

    Public Sub D(x As Integer, y As String)
    End Sub

    Public Function J(x As Integer, y As String) As Integer
        Return 1
    End Function
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
        D(y, J(x, y))
    End Sub

    Public Sub D(y As String, x As Integer)
    End Sub

    Public Function J(x As Integer, y As String) As Integer
        Return 1
    End Function
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeOnReference_OnlyCandidateSymbols() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(x As Integer, y As String)
    End Sub
    Public Sub M(x As Integer, y As Double)
    End Sub
    Public Sub Test()
        $$M("Test", 5)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub M(y As String, x As Integer)
    End Sub
    Public Sub M(x As Integer, y As Double)
    End Sub
    Public Sub Test()
        M(5, "Test")
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderMethodParameters_InvokeInConstructor() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub New(x As Integer, y As String)
        Dim a = 5$$
        Dim b = 6
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Public Sub New(y As String, x As Integer)
        Dim a = 5
        Dim b = 6
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
#End Region

#Region "Properties"
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderIndexerParameters_InvokeAtBeginningOfDeclaration() As Task
            Dim markup = <Text><![CDATA[
Class C
    $$Default Public Property Item(ByVal index1 As Integer, ByVal index2 As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
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
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderIndexerParameters_InParameters() As Task
            Dim markup = <Text><![CDATA[
Class C
    Default Public Property Item(ByVal index1 As Integer, $$ByVal index2 As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
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
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderIndexerParameters_InvokeAtEndOfDeclaration() As Task
            Dim markup = <Text><![CDATA[
Class C
    Default Public Property Item(ByVal index1 As Integer, ByVal index2 As Integer) As Integer$$
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
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
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderIndexerParameters_InvokeInAccessor() As Task
            Dim markup = <Text><![CDATA[
Class C
    Default Public Property Item(ByVal index1 As Integer, ByVal index2 As Integer) As Integer
        Get
            Return 5$$
        End Get
        Set(value As Integer)
        End Set
    End Property
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
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderIndexerParameters_InvokeOnReference_BeforeTarget() As Task
            Dim markup = <Text><![CDATA[
Class C
    Default Public Property Item(ByVal index1 As Integer, ByVal index2 As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub Foo()
        Dim c = New C()
        Dim x = $$c(1, 2)
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
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderIndexerParameters_InvokeOnReference_InArgumentList() As Task
            Dim markup = <Text><![CDATA[
Class C
    Default Public Property Item(ByVal index1 As Integer, ByVal index2 As Integer) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Sub Foo()
        Dim c = New C()
        Dim x = c(1, 2$$)
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
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
#End Region

#Region "Delegates"
        <WpfFact(Skip:="860578"), Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderDelegateParameters_ObjectCreation1() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub Del(x As Integer, y As Integer)

    Sub T()
        Dim x = New $$Del(Sub(a, b)
                        End Sub)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Delegate Sub Del(y As Integer, x As Integer)

    Sub T()
        Dim x = New Del(Sub(b, a)
                        End Sub)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function

        <WpfFact(Skip:="860578"), Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderDelegateParameters_ObjectCreation2() As Task
            Dim markup = <Text><![CDATA[
Class C(Of T)
    Delegate Sub Del(x As T, y As T)
End Class

Class Test
    Sub M()
        Dim x = New C(Of Integer).$$Del(Sub(a, b)
                                      End Sub)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C(Of T)
    Delegate Sub Del(y As T, x As T)
End Class

Class Test
    Sub M()
        Dim x = New C(Of Integer).Del(Sub(b, a)
                                      End Sub)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)
        End Function
#End Region

#Region "Code Refactoring"
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderIndexerParameters_CodeRefactoring_InMethodDeclaration() As Threading.Tasks.Task
            Dim markup = <Text><![CDATA[
Class C
    Sub Foo(x As Integer[||], y As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub Foo(y As Integer, x As Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction:=True, updatedSignature:=permutation, expectedCode:=updatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderIndexerParameters_CodeRefactoring_NotInMethodBody() As Threading.Tasks.Task
            Dim markup = <Text><![CDATA[
Class C
    Sub Foo(x As Integer, y As Integer)
        [||]
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function ReorderIndexerParameters_CodeRefactoring_InCallSite() As Threading.Tasks.Task
            Dim markup = <Text><![CDATA[
Class C
    Sub Foo(x As Integer, y As Integer)
        Foo([|1|], 2)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1, 0}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub Foo(y As Integer, x As Integer)
        Foo(2, 1)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction:=True, updatedSignature:=permutation, expectedCode:=updatedCode)
        End Function
#End Region

    End Class
End Namespace
