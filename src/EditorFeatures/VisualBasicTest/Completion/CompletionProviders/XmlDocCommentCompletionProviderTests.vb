' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data

Namespace Tests
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class XmlDocCommentCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(XmlDocCommentCompletionProvider)
        End Function

        Private Protected Overrides Async Function VerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?,
                hasSuggestionItem As Boolean?, displayTextSuffix As String, displayTextPrefix As String, inlineDescription As String,
                isComplexTextEdit As Boolean?, matchingFilters As List(Of CompletionFilter), flags As CompletionItemFlags?,
                options As CompletionOptions, Optional skipSpeculation As Boolean = False) As Task

            Await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, options, skipSpeculation)
            Await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, options)
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

        <Fact>
        Public Async Function TestAnyLevelTags1() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact>
        Public Async Function TestAnyLevelTags2() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact>
        Public Async Function TestAnyLevelTags3() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see></see>;
    ''' <$$
    ''' </summary>;
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "see", "seealso", "![CDATA[", "!--")
        End Function

        <Fact>
        Public Async Function TestRepeatableNestedTags1() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsAbsentAsync(text, "code", "list", "para", "paramref", "typeparamref")
        End Function

        <Fact>
        Public Async Function TestRepeatableNestedTags2() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "code", "list", "para")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17872")>
        Public Async Function TestRepeatableNestedParamRefAndTypeParamRefTagsOnMethod() As Task
            Dim text = "
Class Outer(Of TOuter)
    Class Inner(Of TInner)
        ''' <summary>
        ''' $$
        ''' </summary>
        Sub Goo(Of TMethod)(i as Integer)
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

        <Fact>
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

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/69293")>
        Public Async Function TestRepeatableNestedParamRefAndTypeParamRefTagsOnDelegate() As Task
            Dim text = "
Class Outer(Of TOuter)
    Class Inner(Of TInner)
        ''' <summary>
        ''' $$
        ''' </summary>
        Delegate Sub Goo(Of TMethod)(i as Integer)
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

        <Fact>
        Public Async Function TestRepeatableTopLevelOnlyTags1() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsAbsentAsync(text, "exception", "include", "permission")
        End Function

        <Fact>
        Public Async Function TestRepeatableTopLevelOnlyTags2() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "exception", "include", "permission")
        End Function

        <Fact>
        Public Async Function TestTopLevelOnlyTags1() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsAbsentAsync(text, "example", "remarks", "summary", "value")
        End Function

        <Fact>
        Public Async Function TestTopLevelOnlyTags2() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "example", "remarks", "summary")
        End Function

        <Fact>
        Public Async Function TestTopLevelOnlyTags3() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <$$
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsAbsentAsync(text, "example", "remarks", "summary", "value")
        End Function

        <Fact>
        Public Async Function TestListOnlyTags() As Task
            Dim text = "
Class C
    ''' <list><$$</list>
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "listheader", "item", "term", "description")
        End Function

        <Fact>
        Public Async Function TestListHeaderTags() As Task
            Dim text = "
Class C
    ''' <list>  <listheader> <$$  </listheader>  </list>
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "term", "description")
        End Function

        <Fact>
        Public Async Function TestMethodParamTypeParam() As Task
            Dim text = "
Class C(Of TClass)
    ''' <$$
    Sub Goo(Of TMethod)(bar as TMethod)
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "param name=""bar""", "typeparam name=""TMethod""")
            Await VerifyItemsAbsentAsync(text, "typeparam name=""TClass""")
        End Function

        <Fact>
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

        <Fact>
        Public Async Function TestTypeTypeParam() As Task
            Dim text = "
    ''' <$$
Class C(Of T)
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "typeparam name=""T""")
        End Function

        <Fact>
        Public Async Function TestNoRepeatParam() As Task
            Dim text = "
Class C(Of T)
    ''' <param name=""bar""></param>
    ''' <$$
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemIsAbsentAsync(text, "param name=""bar""")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11487")>
        Public Async Function TestNoRepeatTypeParam() As Task
            Dim text = "
Class C(Of T)
    ''' <typeparam name=""T""></param>
    ''' <$$
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemIsAbsentAsync(text, "typeparam name=""T""")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11487")>
        Public Async Function TestNoNestedParam() As Task
            Dim text = "
Class C(Of T)
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemIsAbsentAsync(text, "param name=""bar""")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11487")>
        Public Async Function TestNoNestedTypeParam() As Task
            Dim text = "
Class C(Of T)
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemIsAbsentAsync(text, "typeparam name=""T""")
        End Function

        <Fact>
        Public Async Function TestAttributeNameAfterTagNameInIncompleteTag() As Task
            Dim text = "
Class C(Of T)
    ''' <exception $$
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact>
        Public Async Function TestAttributeNameAfterTagNameInElementStartTag() As Task
            Dim text = "
Class C(Of T)
    ''' <exception $$>
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact>
        Public Async Function TestAttributeNameAfterTagNameInEmptyElement() As Task
            Dim text = "
Class C(Of T)
    ''' <see $$/>
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact>
        Public Async Function TestAttributeNameAfterTagNamePartiallyTyped() As Task
            Dim text = "
Class C(Of T)
    ''' <exception c$$
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "cref")
        End Function

        <Fact>
        Public Async Function TestAttributeNameAfterSpecialCrefAttribute() As Task
            Dim text = "
Class C(Of T)
    ''' <summary>
    ''' <list cref=""String"" $$
    ''' </summary>
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "type")
        End Function

        <Fact>
        Public Async Function TestAttributeNameAfterSpecialNameAttributeNonEmpty() As Task
            Dim text = "
Class C(Of T)
    ''' <summary>
    ''' <list name=""goo"" $$
    ''' </summary>
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "type")
        End Function

        <Fact>
        Public Async Function TestAttributeNameAfterTextAttribute() As Task
            Dim text = "
Class C(Of T)
    ''' <summary>
    ''' <list goo="""" $$
    ''' </summary>
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "type")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")>
        Public Async Function TestAttributeNameInWrongTagTypeEmptyElement() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <list $$/>
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemExistsAsync(text, "type", usePreviousCharAsTrigger:=True)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")>
        Public Async Function TestAttributeNameInWrongTagTypeElementStartTag() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see $$>
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemExistsAsync(text, "cref", usePreviousCharAsTrigger:=True)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")>
        Public Async Function TestAttributeValueOnQuote() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see langword=""$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemExistsAsync(text, "Await", usePreviousCharAsTrigger:=True)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")>
        Public Async Function TestAttributeValueOnStartOfWord() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see langword=""A$$""
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemExistsAsync(text, "Await", usePreviousCharAsTrigger:=True)
        End Function

        <Fact>
        Public Async Function TestParameterNameInsideAttribute() As Task
            Dim text = "
Class C(Of T)
    ''' <param name = ""$$""
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
"

            Await VerifyItemExistsAsync(text, "bar")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623158")>
        Public Async Function TestCloseTag() As Task
            Dim text = "
Class C
    ''' <goo></$$
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemExistsAsync(
                text, "goo",
                usePreviousCharAsTrigger:=True)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638805")>
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

        <Fact>
        Public Async Function TestNestedTagsOnSameLineAsCompletedTag() As Task
            Dim text = "
''' <summary>
''' <goo></goo>$$
''' 
''' </summary>
Module Program
End Module
"

            Await VerifyItemsExistAsync(text, "code", "list", "para")
        End Function

        <Fact>
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

        <Fact>
        Public Async Function TestCompletionList() As Task
            Dim text = "
Class C
    ''' <$$
    Sub Goo()
    End Sub
End Class
"

            Await VerifyItemsExistAsync(text, "completionlist")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8546")>
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

        <Fact>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")>
        Public Async Function ReadWritePropertyNoReturns() As Task
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

            Await VerifyItemIsAbsentAsync(text, "returns")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")>
        Public Async Function ReadWritePropertyValue() As Task
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

            Await VerifyItemExistsAsync(text, "value")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")>
        Public Async Function ReadOnlyPropertyNoReturns() As Task
            Dim text = "
Class C
    ''' <$$
    Public ReadOnly Property P As Integer
        Get
        End Get
    End Property
End Class
"

            Await VerifyItemIsAbsentAsync(text, "returns")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")>
        Public Async Function ReadOnlyPropertyValue() As Task
            Dim text = "
Class C
    ''' <$$
    Public ReadOnly Property P As Integer
        Get
        End Get
    End Property
End Class
"

            Await VerifyItemExistsAsync(text, "value")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")>
        Public Async Function WriteOnlyPropertyNoReturns() As Task
            Dim text = "
Class C
    ''' <$$
    Public WriteOnly Property P As Integer
        Set
        End Set
    End Property
End Class
"

            Await VerifyItemIsAbsentAsync(text, "returns")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")>
        Public Async Function WriteOnlyPropertyValue() As Task
            Dim text = "
Class C
    ''' <$$
    Public WriteOnly Property P As Integer
        Set
        End Set
    End Property
End Class
"

            Await VerifyItemExistsAsync(text, "value")
        End Function

        <Fact>
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

        <Fact>
        Public Async Function TestListAttributeNames() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <list $$></list>
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "type")
        End Function

        <Fact>
        Public Async Function TestListTypeAttributeValue() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <list type=""$$""></list>
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "number", "bullet", "table")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11490")>
        Public Async Function TestSeeAttributeNames() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see $$/>
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "cref", "langword")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22789")>
        Public Async Function TestLangwordCompletionInPlainText() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' Some text $$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "Nothing", "Shared", "True", "False", "Await")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22789")>
        Public Async Function LangwordCompletionAfterAngleBracket1() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' Some text <$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemsAbsentAsync(text, "Nothing", "Shared", "True", "False", "Await")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22789")>
        Public Async Function LangwordCompletionAfterAngleBracket2() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' Some text <s$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemsAbsentAsync(text, "Nothing", "Shared", "True", "False", "Await")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22789")>
        Public Async Function LangwordCompletionAfterAngleBracket3() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' Some text < $$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemsAbsentAsync(text, "Nothing", "Shared", "True", "False", "Await")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11490")>
        Public Async Function TestSeeLangwordAttributeValue() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <see langword=""$$""/>
    ''' </summary>
    Sub Goo()
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "Nothing", "True", "False", "Await")
        End Function

        <Fact>
        Public Async Function TestParamNames() As Task
            Dim text = "
Class C
    ''' <param name=""$$""
    Sub Goo(Of T)(i as Integer)
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "i")
            Await VerifyItemsAbsentAsync(text, "T")
        End Function

        <Fact>
        Public Async Function TestParamRefNames() As Task
            Dim text = "
Class C
    ''' <summary>
    ''' <paramref name=""$$""
    ''' </summary>
    Sub Goo(Of T)(i as Integer)
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "i")
            Await VerifyItemsAbsentAsync(text, "T")
        End Function

        <Fact>
        Public Async Function TestTypeParamNames() As Task
            Dim text = "
Class C(Of TClass)
    ''' <typeparam name=""$$""
    Sub Goo(Of TMethod)(i as TMethod)
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "TMethod")
            Await VerifyItemsAbsentAsync(text, "TClass", "i")
        End Function

        <Fact>
        Public Async Function TestTypeParamNamesPartiallyTyped() As Task
            Dim text = "
Class C(Of TClass)
    ''' <typeparam name=""T$$""
    Sub Goo(Of TMethod)(i as TMethod)
    End Sub
End Class
"
            Await VerifyItemsExistAsync(text, "TMethod")
            Await VerifyItemsAbsentAsync(text, "TClass", "i")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17872")>
        Public Async Function TestTypeParamRefNames() As Task
            Dim text = "
Class Outer(Of TOuter)
    Class Inner(Of TInner)
        ''' <summary>
        ''' <typeparamref name=""$$""
        ''' </summary>
        Sub Goo(Of TMethod)(i as Integer)
        End Sub
    End Class
End Class
"
            Await VerifyItemsExistAsync(text, "TOuter", "TInner", "TMethod")
            Await VerifyItemsAbsentAsync(text, "i")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17872")>
        Public Async Function TestTypeParamRefNamesPartiallyTyped() As Task
            Dim text = "
Class Outer(Of TOuter)
    Class Inner(Of TInner)
        ''' <summary>
        ''' <typeparamref name=""T$$""
        ''' </summary>
        Sub Goo(Of TMethod)(i as Integer)
        End Sub
    End Class
End Class
"
            Await VerifyItemsExistAsync(text, "TOuter", "TInner", "TMethod")
            Await VerifyItemsAbsentAsync(text, "i")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/757")>
        Public Async Function TermAndDescriptionInsideItem() As Task
            Dim text = "
class C
    ''' <summary>
    '''     <list type=""table"">
    '''         <item>
    '''             $$
    '''         </item>
    '''     </list>
    ''' </summary>
    sub Goo()
    end sub
end class"
            Await VerifyItemExistsAsync(text, "term")
            Await VerifyItemExistsAsync(text, "description")
        End Function
    End Class
End Namespace
