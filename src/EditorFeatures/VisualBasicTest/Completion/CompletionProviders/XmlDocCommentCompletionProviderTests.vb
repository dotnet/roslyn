' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

Namespace Tests
    Public Class XmlDocCommentCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New XmlDocCommentCompletionProvider()
        End Function

        Private Async Function VerifyItemsExistAsync(markup As String, ParamArray items() As String) As Threading.Tasks.Task
            For Each item In items
                Await VerifyItemExistsAsync(markup, item)
            Next
        End Function

        Private Async Function VerifyItemsAbsentAsync(markup As String, ParamArray items() As String) As Threading.Tasks.Task
            For Each item In items
                Await VerifyItemIsAbsentAsync(markup, item)
            Next
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAnyLevelTags1() As Task
            Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAnyLevelTags2() As Task
            Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt;$$
    ''' &lt;/summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAnyLevelTags3() As Task
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

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableNestedTags1() As Task
            Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsAbsentAsync(text, "code", "list", "para", "paramref", "typeparamref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableNestedTags2() As Task
            Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt;$$
    ''' &lt;summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "code", "list", "para", "paramref", "typeparamref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableTopLevelOnlyTags1() As Task
            Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt;$$
    ''' &lt;summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsAbsentAsync(text, "exception", "include", "permission")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableTopLevelOnlyTags2() As Task
            Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "exception", "include", "permission")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTopLevelOnlyTags1() As Task
            Dim text = <File>
Class C
    ''' &lt;summary&gt;
    ''' &lt;$$
    ''' &lt;summary&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsAbsentAsync(text, "example", "remarks", "summary", "value")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTopLevelOnlyTags2() As Task
            Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "example", "remarks", "summary")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTopLevelOnlyTags3() As Task
            Dim text = <File>
Class C
    ''' &lt;summary&gt;&lt;summary&gt;
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsAbsentAsync(text, "example", "remarks", "summary", "value")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestListOnlyTags() As Task
            Dim text = <File>
Class C
    ''' &lt;list&gt;&lt;$$&lt;/list&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "listheader", "item", "term", "description")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestListHeaderTags() As Task
            Dim text = <File>
Class C
    ''' &lt;list&gt;  &lt;listheader&gt; &lt;$$  &lt;/listheader&gt;  &lt;/list&gt;
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "term", "description")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMethodParamTypeParam() As Task
            Dim text = <File>
Class C(Of T)
    ''' &lt;$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "param name=""bar""", "typeparam name=""T""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIndexerParamTypeParam() As Task
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

            Await VerifyItemsExistAsync(text, "param name=""bar""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeTypeParam() As Task
            Dim text = <File>
    ''' &lt;$$
Class C(Of T)
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "typeparam name=""T""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoRepeatParam() As Task
            Dim text = <File>
Class C(Of T)
    ''' &lt;param name="bar"&gt;&lt;/param;&gt;
    ''' &lt;$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

            Await VerifyItemIsAbsentAsync(text, "param name=""bar""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeAfterName() As Task
            Dim text = <File>
Class C(Of T)
    ''' &lt;exception $$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeAfterNamePartiallyTyped() As Task
            Dim text = <File>
Class C(Of T)
    ''' &lt;exception c$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeAfterAttribute() As Task
            Dim text = <File>
Class C(Of T)
    ''' &lt;exception name="" $$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterNameInsideAttribute() As Task
            Dim text = <File>
Class C(Of T)
    ''' &lt;param name = "$$"
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
</File>.Value

            Await VerifyItemExistsAsync(text, "bar")
        End Function

        <WorkItem(623219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623219")>
        <WorkItem(746919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746919")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitParam() As Task
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

            Await VerifyCustomCommitProviderAsync(text, "param name=""bar""", expected)
        End Function

        <WorkItem(623158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623158")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCloseTag() As Task
            Dim text = <File>
Class C
    ''' &lt;foo&gt;&lt;/$$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemExistsAsync(text, "foo", usePreviousCharAsTrigger:=True)
        End Function

        <WorkItem(638805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638805")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoParentElement() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' </summary>
''' $$
Module Program
End Module]]></File>.Value

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNestedTagsOnSameLineAsCompletedTag() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <foo></foo>$$
''' 
''' </summary>
Module Program
End Module]]></File>.Value

            Await VerifyItemsExistAsync(text, "code", "list", "para", "paramref", "typeparamref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInCref() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="$$
''' </summary>
Module Program
End Module]]></File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowTypingDoubleQuote() As Task
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

            Await VerifyCustomCommitProviderAsync(text, "param name=""bar""", expected, Microsoft.CodeAnalysis.SourceCodeKind.Regular, commitChar:=""""c)
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowTypingSpace() As Task
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

            Await VerifyCustomCommitProviderAsync(text, "param name=""bar""", expected, Microsoft.CodeAnalysis.SourceCodeKind.Regular, commitChar:=" "c)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionList() As Task
            Dim text = <File>
Class C
    ''' &lt;$$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyItemsExistAsync(text, "completionlist")
        End Function
    End Class
End Namespace