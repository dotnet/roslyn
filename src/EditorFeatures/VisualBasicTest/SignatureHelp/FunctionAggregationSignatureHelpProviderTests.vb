' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.VBFeaturesResources

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class FunctionAggregationSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New FunctionAggregationSignatureHelpProvider()
        End Function

        <WorkItem(529682)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub AggregateFunctionInAggregateClause()
            Dim markup = <Text><![CDATA[
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim lambda = Aggregate i In New Integer() {1} Into Count($$
    End Sub
End Module
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{Extension}> Count() As Integer", String.Empty, Nothing, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{Extension}> Count({Expression1} As Boolean) As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

#Region "EditorBrowsable tests"
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_FunctionAggregation_BrowsableStateAlways()
            Dim markup = <Text><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Linq

Class C
    Sub M()
        Dim numbers As IEnumerable(Of Integer)
        Dim query = Aggregate num In numbers Into GetRandomNumber($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Public Module Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    <Extension()>
    Public Function GetRandomNumber(ByVal values As IEnumerable(Of Integer)) As Integer
        Return 4
    End Function
End Module
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{Extension}> GetRandomNumber() As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_FunctionAggregation_BrowsableStateNever()
            Dim markup = <Text><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Linq

Class C
    Sub M()
        Dim numbers As IEnumerable(Of Integer)
        Dim query = Aggregate num In numbers Into GetRandomNumber($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Public Module Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    <Extension()>
    Public Function GetRandomNumber(ByVal values As IEnumerable(Of Integer)) As Integer
        Return 4
    End Function
End Module
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{Extension}> GetRandomNumber() As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_FunctionAggregation_BrowsableStateAdvanced()
            Dim markup = <Text><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Linq

Class C
    Sub M()
        Dim numbers As IEnumerable(Of Integer)
        Dim query = Aggregate num In numbers Into GetRandomNumber($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Public Module Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    <Extension()>
    Public Function GetRandomNumber(ByVal values As IEnumerable(Of Integer)) As Integer
        Return 4
    End Function
End Module
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{Extension}> GetRandomNumber() As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=True)

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=False)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_FunctionAggregation_BrowsableStateMixed()
            Dim markup = <Text><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports System.Linq

Class C
    Sub M()
        Dim numbers As IEnumerable(Of Integer)
        Dim query = Aggregate num In numbers Into [|GetRandomNumber($$
    |]End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic
Imports System

Public Module Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    <Extension()>
    Public Function GetRandomNumber(ByVal values As IEnumerable(Of Integer)) As Integer
        Return 4
    End Function
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    <Extension()>
    Public Function GetRandomNumber(Of T)(ByVal values As IEnumerable(Of T), ByVal selector As Func(Of T, Double)) As Integer
        Return 4
    End Function
End Module
]]></Text>.Value
            Dim expectedOrderedItemsMetadataOnly = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataOnly.Add(New SignatureHelpTestItem($"<{Extension}> GetRandomNumber() As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem($"<{Extension}> GetRandomNumber() As Integer", String.Empty, Nothing, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem($"<{Extension}> GetRandomNumber({Expression1} As Double) As Integer", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataOnly,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub
#End Region
    End Class
End Namespace
