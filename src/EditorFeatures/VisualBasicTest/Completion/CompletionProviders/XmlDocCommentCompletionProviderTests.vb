' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

Public Class XmlDocCommentCompletionProviderTests
    Inherits AbstractVisualBasicCompletionProviderTests

    Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
        MyBase.New(workspaceFixture)
    End Sub

    Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
        Return New XmlDocCommentCompletionProvider()
    End Function

    Private Sub VerifyItemsExist(markup As String, ParamArray items() As String)
        For Each item In items
            VerifyItemExists(markup, item)
        Next
    End Sub

    Private Sub VerifyItemsAbsent(markup As String, ParamArray items() As String)
        For Each item In items
            VerifyItemIsAbsent(markup, item)
        Next
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AnyLevelTags1()
        Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "see", "seealso", "![CDATA[", "!--")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AnyLevelTags2()
        Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt;$$
    ''' &lt;/summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "see", "seealso", "![CDATA[", "!--")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AnyLevelTags3()
        Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt; &lt;see&gt;&lt;/see&gt;
    ''' &lt;$$
    ''' &lt;/summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "see", "seealso", "![CDATA[", "!--")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub RepeatableNestedTags1()
        Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsAbsent(text, "code", "list", "para", "paramref", "typeparamref")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub RepeatableNestedTags2()
        Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt;$$
    ''' &lt;summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "code", "list", "para", "paramref", "typeparamref")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub RepeatableTopLevelOnlyTags1()
        Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt;$$
    ''' &lt;summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsAbsent(text, "exception", "include", "permission")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub RepeatableTopLevelOnlyTags2()
        Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "exception", "include", "permission")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub TopLevelOnlyTags1()
        Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt;$$
    ''' &lt;summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsAbsent(text, "example", "remarks", "summary", "value")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub TopLevelOnlyTags2()
        Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "example", "remarks", "summary")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub TopLevelOnlyTags3()
        Dim text = <File>
Class C
    ''' &lt;summary&gt;&lt;summary&gt;
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsAbsent(text, "example", "remarks", "summary", "value")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub ListOnlyTags()
        Dim text = <File>
Class C
    ''' &lt;list&gt;&lt;$$&lt;/list&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "listheader", "item", "term", "description")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub ListHeaderTags()
        Dim text = <File>
Class C
    ''' &lt;list&gt;  &lt;listheader&gt; &lt;$$  &lt;/listheader&gt;  &lt;/list&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "term", "description")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub MethodParamTypeParam()
        Dim text = <File>
Class C(Of T)
    ''' &lt;$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "param name=""bar""", "typeparam name=""T""")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub IndexerParamTypeParam()
        Dim text = <File>
Class C(Of T)
    ''' &lt;$$
    Default Public Property Item(bar as T)
        Get
        End Get
        Set
        End Set
    End Sub
End Property
</File>.Value

        VerifyItemsExist(text, "param name=""bar""")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub TypeTypeParam()
        Dim text = <File>
    ''' &lt;$$
Class C(Of T)
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "typeparam name=""T""")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NoRepeatParam()
        Dim text = <File>
Class C(Of T)
    ''' &lt;param name="bar"&gt;&lt;/param;&gt;
    ''' &lt;$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

        VerifyItemIsAbsent(text, "param name=""bar""")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AttributeAfterName()
        Dim text = <File>
Class C(Of T)
    ''' &lt;exception $$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

        VerifyItemExists(text, "cref")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AttributeAfterNamePartiallyTyped()
        Dim text = <File>
Class C(Of T)
    ''' &lt;exception c$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

        VerifyItemExists(text, "cref")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AttributeAfterAttribute()
        Dim text = <File>
Class C(Of T)
    ''' &lt;exception name="" $$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

        VerifyItemExists(text, "cref")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub ParameterNameInsideAttribute()
        Dim text = <File>
Class C(Of T)
    ''' &lt;param name = "$$"
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

        VerifyItemExists(text, "bar")
    End Sub

    <WorkItem(623219)>
    <WorkItem(746919)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CommitParam()
        Dim text = <File>
Class C(Of T)
    ''' &lt;param$$
    Sub Foo(Of T)(bar As T)
    End Sub
End Class
</File>.NormalizedValue

        Dim expected = <File>
Class C(Of T)
    ''' &lt;param name="bar"$$
    Sub Foo(Of T)(bar As T)
    End Sub
End Class
</File>.NormalizedValue

        VerifyCustomCommitProvider(text, "param name=""bar""", expected)
    End Sub

    <WorkItem(623158)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CloseTag()
        Dim text = <File>
Class C
    ''' &lt;foo&gt;&lt;/$$
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemExists(text, "foo", usePreviousCharAsTrigger:=True)
    End Sub

    <WorkItem(638805)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NoParentElement()
        Dim text = <File><![CDATA[
''' <summary>
''' </summary>
''' $$
Module Program
End Module]]></File>.Value

        VerifyItemsExist(text, "see", "seealso", "![CDATA[", "!--")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NestedTagsOnSameLineAsCompletedTag()
        Dim text = <File><![CDATA[
''' <summary>
''' <foo></foo>$$
''' 
''' </summary>
Module Program
End Module]]></File>.Value

        VerifyItemsExist(text, "code", "list", "para", "paramref", "typeparamref")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub NotInCref()
        Dim text = <File><![CDATA[
''' <summary>
''' <see cref="$$
''' </summary>
Module Program
End Module]]></File>.Value

        VerifyNoItemsExist(text)
    End Sub

    <WorkItem(638653)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AllowTypingDoubleQuote()
        Dim text = <File>
Class C(Of T)
    ''' &lt;param$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.NormalizedValue

        Dim expected = <File>$$
Class C(Of T)
    ''' &lt;param
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.NormalizedValue

        VerifyCustomCommitProvider(text, "param name=""bar""", expected, Microsoft.CodeAnalysis.SourceCodeKind.Regular, commitChar:=""""c)
    End Sub

    <WorkItem(638653)>
    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub AllowTypingSpace()
        Dim text = <File>
Class C(Of T)
    ''' &lt;param$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.NormalizedValue

        Dim expected = <File>$$
Class C(Of T)
    ''' &lt;param
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.NormalizedValue

        VerifyCustomCommitProvider(text, "param name=""bar""", expected, Microsoft.CodeAnalysis.SourceCodeKind.Regular, commitChar:=" "c)
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
    Public Sub CompletionList()
        Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

        VerifyItemsExist(text, "completionlist")
    End Sub
End Class
