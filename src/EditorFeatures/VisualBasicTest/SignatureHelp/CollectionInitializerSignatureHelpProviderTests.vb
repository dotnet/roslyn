' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class CollectionInitializerSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New CollectionInitializerSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function ForSingleParamAddMethods() As Task
            Dim markup = "
imports System.Collections.Generic

class C
    sub Goo()
        dim a = new List(of integer) from { { $$
    end sub
end class"

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("List(Of Integer).Add(item As Integer)", currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function ForMultiParamAddMethods() As Task
            Dim markup = "
imports System.Collections.Generic

class C
    sub Goo()
        dim a = new Dictionary(of integer, string) from { { $$
    end sub
end class"

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Dictionary(Of Integer, String).Add(key As Integer, value As String)", currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function ForSecondParam() As Task
            Dim markup = "
imports System.Collections.Generic

class C
    sub Goo()
        dim a = new Dictionary(of integer, string) from { { 0, $$
    end sub
end class"

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Dictionary(Of Integer, String).Add(key As Integer, value As String)", currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function WithoutClosingConstructs() As Task
            Dim markup = "
imports System.Collections.Generic

class C
    sub Goo()
        dim a = new Dictionary(of integer, string) from { { 0, $$
"

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Dictionary(Of Integer, String).Add(key As Integer, value As String)", currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function WithMultipleAddMethods() As Task
            Dim markup = "
imports System.Collections

class Bar 
    implements IEnumerable

    public sub Add(i as integer)
    end sub
    public sub Add(i as integer, s as string)
    end sub
    public sub Add(i as integer, s as string, b as boolean)
    end sub
end class

class C
    sub Goo()
        dim as = new Bar from { { $$
"

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Bar.Add(i As Integer)", currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("Bar.Add(i As Integer, s As String)", currentParameterIndex:=0, isSelected:=True))
            expectedOrderedItems.Add(New SignatureHelpTestItem("Bar.Add(i As Integer, s As String, b As Boolean)", currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function DoesNotImplementIEnumerable() As Task
            Dim markup = "
imports System.Collections

class Bar
    public sub Add(i as integer)
    end sub
    public sub Add(i as integer, s as string)
    end sub
    public sub Add(i as integer, s as string, b as boolean)
    end sub
end class

class C
    sub Goo()
        dim as = new Bar from { { $$
"

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function WithExtensionAddMethods() As Task
            Dim markup = "
imports System.Collections

class Bar
    implements IEnumerable
end class

module Extensions
    <System.Runtime.CompilerServices.Extension>
    public sub Add(b as bar, i as integer)
    end sub
    <System.Runtime.CompilerServices.Extension>
    public sub Add(b as bar, i as integer, s as string)
    end sub
    <System.Runtime.CompilerServices.Extension>
    public sub Add(b as bar, i as integer, s as string, b as boolean)
    end sub
end module

class C
    sub Goo()
        dim a = new Bar from { { $$
"

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{VBFeaturesResources.Extension}> Extensions.Add(i As Integer)", currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{VBFeaturesResources.Extension}> Extensions.Add(i As Integer, s As String)", currentParameterIndex:=0, isSelected:=True))
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{VBFeaturesResources.Extension}> Extensions.Add(i As Integer, s As String, b As Boolean)", currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems, sourceCodeKind:=SourceCodeKind.Regular)
        End Function
    End Class
End Namespace
