' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Tests
    Public Class XmlDocCommentCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New XmlDocCommentCompletionProvider()
        End Function

        Protected Overrides Async Function VerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?) As Task
            Await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority)
            Await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority)
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

            Await VerifyItemsExistAsync(text, "code", "list", "para")
        End Function

        <WorkItem(17872, "https://github.com/dotnet/roslyn/issues/17872")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableNestedParamRefAndTypeParamRefTagsOnMethod() As Task
            Dim text = "
Class Outer(Of TOuter)
    Class Inner(Of TInner)
        ''' <summary>
        ''' $$
        ''' </summary>
        Sub Foo(Of TMethod)(i as Integer)
        End Sub
    End Class
End Class
"

            Await VerifyItemsExistAsync(
                text,
                "paramref name=""i""",
                "typeparamref name=""TOuter""",
                "typeparamref name=""TInner""",
                "typeparamref name=""TMethod""")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRepeatableNestedTypeParamRefTagOnClass() As Task
            Dim text = "
''' <summary>
''' <$$
''' </summary>
Class C(Of T)
End Class
"

            Await VerifyItemsExistAsync(text, "typeparamref name=""T""")
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
Class C(Of TClass)
    ''' <$$
    Sub Foo(Of TMethod)(bar as TMethod)
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "param name=""bar""", "typeparam name=""TMethod""")
            Await VerifyItemsAbsentAsync(text, "typeparam name=""TClass""")
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

        <WorkItem(11487, "https://github.com/dotnet/roslyn/issues/11487")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoRepeatTypeParam() As Task
            Dim text = "
Class C(Of T)
    ''' <typeparam name=""T""></param>
    ''' <$$
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemIsAbsentAsync(text, "typeparam name=""T""")
        End Function

        <WorkItem(11487, "https://github.com/dotnet/roslyn/issues/11487")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoNestedParam() As Task
            Dim text = "
Class C(Of T)
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemIsAbsentAsync(text, "param name=""bar""")
        End Function

        <WorkItem(11487, "https://github.com/dotnet/roslyn/issues/11487")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoNestedTypeParam() As Task
            Dim text = "
Class C(Of T)
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Foo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemIsAbsentAsync(text, "typeparam name=""T""")
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

            Await VerifyItemExistsAsync(
                text, "foo",
                usePreviousCharAsTrigger:=True)
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

            Await VerifyItemsExistAsync(text, "code", "list", "para")
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

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReturnsAlreadyOnMethod() As Task
            Dim text = "
Class C
    ''' <returns></returns>
    ''' <$$
    Function M() As Integer
    End Function
End Class
"

            Await VerifyItemsAbsentAsync(text, "returns")
        End Function

        <WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ReadWritePropertyReturns() As Task
            Dim text = "
Class C
    ''' <$$
    Public Property P As Integer
        Get
        End Get
        Set
        End Set
    End Property
End Class
"

            Await VerifyItemExistsAsync(text, "returns")
        End Function

        <WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ReadOnlyPropertyReturns() As Task
            Dim text = "
Class C
    ''' <$$
    Public ReadOnly Property P As Integer
        Get
        End Get
    End Property
End Class
"

            Await VerifyItemExistsAsync(text, "returns")
        End Function

        <WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function WriteOnlyPropertyReturns() As Task
            Dim text = "
Class C
    ''' <$$
    Public WriteOnly Property P As Integer
        Set
        End Set
    End Property
End Class
"

            Await VerifyItemIsAbsentAsync(Text, "returns")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestValueAlreadyOnProperty() As Task
            Dim text = "
Class C
    ''' <value></value>
    ''' <$$
    Property P() As Integer
    End Property
End Class
"

            Await VerifyItemsAbsentAsync(text, "value")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestListTypes() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <list type=""$$""
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "number", "bullet", "table")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSeeCommitOnSpace() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"
            Dim after = "
Class C
    ''' <summary>
    ''' <see$$/>
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"
            Await VerifyCustomCommitProviderAsync(text, "see", after, commitChar:=" "c)
        End Function

        <WorkItem(11490, "https://github.com/dotnet/roslyn/issues/11490")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSeeLangwordValues() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see langword=""$$""
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "Await", "Class")
        End Function

        <WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSeeAttributesOnSpace() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see $$
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"
            Await VerifyItemExistsAsync(text, "langword", usePreviousCharAsTrigger:=True)
        End Function

        <WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SeeLangwordValuesOnQuote() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see langword=""$$
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"
            Await VerifyItemExistsAsync(text, "Await", usePreviousCharAsTrigger:=True)
        End Function

        <WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SeeLangwordValuesOnStartOfWord() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see langword=""A$$""
    ''' </summary>
    Sub Foo()
    End Sub
End Class
"
            Await VerifyItemExistsAsync(text, "Await", usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParamNames() As Task
            Dim text = "
Class C
    ''' <param name=""$$""
    Sub Foo(Of T)(i as Integer)
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "i")
            Await VerifyItemsAbsentAsync(text, "T")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParamRefNames() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <paramref name=""$$""
    ''' </summary>
    Sub Foo(Of T)(i as Integer)
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "i")
            Await VerifyItemsAbsentAsync(text, "T")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeParamNames() As Task
            Dim text = "
Class C(Of TClass)
    ''' <typeparam name=""$$""
    Sub Foo(Of TMethod)(i as TMethod)
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "TMethod")
            Await VerifyItemsAbsentAsync(text, "TClass", "i")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeParamNamesPartiallyTyped() As Task
            Dim text = "
Class C(Of TClass)
    ''' <typeparam name=""T$$""
    Sub Foo(Of TMethod)(i as TMethod)
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "TMethod")
            Await VerifyItemsAbsentAsync(text, "TClass", "i")
        End Function

        <WorkItem(17872, "https://github.com/dotnet/roslyn/issues/17872")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeParamRefNames() As Task
            Dim text = "
Class Outer(Of TOuter)
    Class Inner(Of TInner)
        ''' <summary>
        ''' <typeparamref name=""$$""
        ''' </summary>
        Sub Foo(Of TMethod)(i as Integer)
        End Sub
    End Class
End Class
"
            Await VerifyItemsExistAsync(text, "TOuter", "TInner", "TMethod")
            Await VerifyItemsAbsentAsync(text, "i")
        End Function

        <WorkItem(17872, "https://github.com/dotnet/roslyn/issues/17872")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeParamRefNamesPartiallyTyped() As Task
            Dim text = "
Class Outer(Of TOuter)
    Class Inner(Of TInner)
        ''' <summary>
        ''' <typeparamref name=""T$$""
        ''' </summary>
        Sub Foo(Of TMethod)(i as Integer)
        End Sub
    End Class
End Class
"
            Await VerifyItemsExistAsync(text, "TOuter", "TInner", "TMethod")
            Await VerifyItemsAbsentAsync(text, "i")
        End Function

    End Class
End Namespace