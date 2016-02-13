' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
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

        Private Async Function VerifyItemsExistAsync(markup As String, ParamArray items() As String) As Task
            For Each item In items
                Await VerifyItemExistsAsync(markup, item)
            Next
        End Function

        Private Async Function VerifyItemsAbsentAsync(markup As String, ParamArray items() As String) As Task
            For Each item In items
                Await VerifyItemIsAbsentAsync(markup, item)
            Next
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAnyLevelTags1() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAnyLevelTags2() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAnyLevelTags3() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see></see>;
    ''' <$$
    ''' </summary>;
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableNestedTags1() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsAbsentAsync(text, "code", "list", "para", "paramref", "typeparamref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableNestedTags2() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "code", "list", "para", "paramref", "typeparamref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableTopLevelOnlyTags1() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsAbsentAsync(text, "exception", "include", "permission")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableTopLevelOnlyTags2() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "exception", "include", "permission")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTopLevelOnlyTags1() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsAbsentAsync(text, "example", "remarks", "summary", "value")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTopLevelOnlyTags2() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "example", "remarks", "summary")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTopLevelOnlyTags3() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsAbsentAsync(text, "example", "remarks", "summary", "value")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestListOnlyTags() As Task
            Dim text = "
Class C
    ''' <list><$$</list>
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "listheader", "item", "term", "description")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestListHeaderTags() As Task
            Dim text = "
Class C
    ''' <list>  <listheader> <$$  </listheader>  </list>
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "term", "description")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMethodParamTypeParam() As Task
            Dim text = "
Class C(Of T)
    ''' <$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "param name=""bar""", "typeparam name=""T""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIndexerParamTypeParam() As Task
            Dim text = "
Class C(Of T)
    ''' <$$
    Default Public Property Item(bar as T)
        Get
        End Get
        Set
        End Set
    End Sub
End Property
"

            Await VerifyItemsExistAsync(text, "param name=""bar""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeTypeParam() As Task
            Dim text = "
    ''' <$$
Class C(Of T)
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "typeparam name=""T""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoRepeatParam() As Task
            Dim text = "
Class C(Of T)
    ''' <param name=""bar""></param>
    ''' <$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemIsAbsentAsync(text, "param name=""bar""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeAfterName() As Task
            Dim text = "
Class C(Of T)
    ''' <exception $$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeAfterNamePartiallyTyped() As Task
            Dim text = "
Class C(Of T)
    ''' <exception c$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAttributeAfterAttribute() As Task
            Dim text = "
Class C(Of T)
    ''' <exception name="""" $$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterNameInsideAttribute() As Task
            Dim text = "
Class C(Of T)
    ''' <param name = ""$$""
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "bar")
        End Function

        <WorkItem(623219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623219")>
        <WorkItem(746919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746919")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitParam() As Task
            Dim text = "
Class C(Of T)
    ''' <param$$
    Sub Foo(Of T)(bar As T)
    End Sub
End Class
"

            Dim expected = "
Class C(Of T)
    ''' <param name=""bar""$$
    Sub Foo(Of T)(bar As T)
    End Sub
End Class
"

            Await VerifyCustomCommitProviderAsync(text, "param name=""bar""", expected)
        End Function

        <WorkItem(623158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623158")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCloseTag() As Task
            Dim text = "
Class C
    ''' <foo></$$
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "foo", usePreviousCharAsTrigger:=True)
        End Function

        <WorkItem(638805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638805")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoParentElement() As Task
            Dim text = "
''' <summary>
''' </summary>
''' $$
Module Program
End Module
"

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNestedTagsOnSameLineAsCompletedTag() As Task
            Dim text = "
''' <summary>
''' <foo></foo>$$
''' 
''' </summary>
Module Program
End Module
"

            Await VerifyItemsExistAsync(text, "code", "list", "para", "paramref", "typeparamref")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInCref() As Task
            Dim text = "
''' <summary>
''' <see cref=""$$
''' </summary>
Module Program
End Module
"

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowTypingDoubleQuote() As Task
            Dim text = "
Class C(Of T)
    ''' <param$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Dim expected = "$$
Class C(Of T)
    ''' <param
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyCustomCommitProviderAsync(text, "param name=""bar""", expected, Microsoft.CodeAnalysis.SourceCodeKind.Regular, commitChar:=""""c)
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowTypingSpace() As Task
            Dim text = "
Class C(Of T)
    ''' <param$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Dim expected = "$$
Class C(Of T)
    ''' <param
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyCustomCommitProviderAsync(text, "param name=""bar""", expected, Microsoft.CodeAnalysis.SourceCodeKind.Regular, commitChar:=" "c)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionList() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Foo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "completionlist")
        End Function

        <WorkItem(8546, "https://github.com/dotnet/roslyn/issues/8546")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReturnsOnMethod() As Task
            Dim text = "
Class C
    ''' <$$
    Function M() As Integer
    End Function
End Class
"

            Await VerifyItemsExistAsync(text, "returns")
        End Function

    End Class
End Namespace