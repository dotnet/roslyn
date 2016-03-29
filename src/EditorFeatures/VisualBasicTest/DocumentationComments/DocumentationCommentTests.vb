' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentationComments
    Public Class DocumentationCommentTests
        Inherits AbstractDocumentationCommentTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_Class_AutoGenerateXmlDocCommentsOff() As Task
            Const code = "
''$$
Class C
End Class
"
            Const expected = "
'''$$
Class C
End Class
"
            Await VerifyTypingCharacterAsync(code, expected, autoGenerateXmlDocComments:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_Class() As Task
            Const code = "
''$$
Class C
End Class
"
            Const expected = "
''' <summary>
''' $$
''' </summary>
Class C
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_Method() As Task
            Const code = "
Class C
    ''$$
    Function M(Of T)(foo As Integer, i() As Integer) As Integer
        Return 0
    End Function
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    ''' <typeparam name=""T""></typeparam>
    ''' <param name=""foo""></param>
    ''' <param name=""i""></param>
    ''' <returns></returns>
    Function M(Of T)(foo As Integer, i() As Integer) As Integer
        Return 0
    End Function
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WorkItem(538715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538715")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NoReturnType() As Task
            Const code = "
Class C
   ''$$
   Function F()
   End Function
End Class
"
            Const expected = "
Class C
   ''' <summary>
   ''' $$
   ''' </summary>
   ''' <returns></returns>
   Function F()
   End Function
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NotWhenDocCommentExists1() As Task
            Const code = "
''$$
''' <summary></summary>
Class C
End Class
"
            Const expected = "
'''$$
''' <summary></summary>
Class C
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NotWhenDocCommentExists2() As Task
            Const code = "
Class C
    ''$$
    ''' <summary></summary>
    Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Const expected = "
Class C
    '''$$
    ''' <summary></summary>
    Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WorkItem(537506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537506")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NotAfterClassName() As Task
            Const code = "
Class C''$$
End Class
"
            Const expected = "
Class C'''$$
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WorkItem(537508, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537508")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NotInsideClass() As Task
            Const code = "
Class C
    ''$$
End Class
"
            Const expected = "
Class C
    '''$$
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WorkItem(537510, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537510")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NotAfterConstructorName() As Task
            Const code = "
Class C
    Sub New() ''$$
End Class
"
            Const expected = "
Class C
    Sub New() '''$$
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WorkItem(537511, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537511")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NotInsideConstructor() As Task
            Const code = "
Class C
    Sub New()
    ''$$
    End Sub
End Class
"
            Const expected = "
Class C
    Sub New()
    '''$$
    End Sub
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WorkItem(537512, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537512")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NotInsideMethodBody() As Task
            Const code = "
Class C
    Sub Foo()
    ''$$
    End Sub
End Class
"
            Const expected = "
Class C
    Sub Foo()
    '''$$
    End Sub
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WorkItem(540004, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540004")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestTypingCharacter_NoReturnsOnWriteOnlyProperty() As Task
            Const code = "
Class C
    ''$$
    WriteOnly Property Prop As Integer
        Set(ByVal value As Integer
        End Set
    End Property
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    WriteOnly Property Prop As Integer
        Set(ByVal value As Integer
        End Set
    End Property
End Class
"
            Await VerifyTypingCharacterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Class1() As Task
            Const code = "
'''$$
Class C
End Class
"
            Const expected = "
''' <summary>
''' $$
''' </summary>
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Class1_AutoGenerateXmlDocCommentsOff() As Task
            Const code = "
'''$$
Class C
End Class
"
            Const expected = "
'''
$$
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected, autoGenerateXmlDocComments:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Class2() As Task
            Const code = "
'''$$Class C
End Class
"
            Const expected = "
''' <summary>
''' $$
''' </summary>
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Class3() As Task
            Const code = "
'''$$<Foo()> Class C
End Class
"
            Const expected = "
''' <summary>
''' $$
''' </summary>
<Foo()> Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(538717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538717")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Module() As Task
            Const code = "
'''$$Module M
   Dim x As Integer
End Module
"
            Const expected = "
''' <summary>
''' $$
''' </summary>
Module M
    Dim x As Integer
End Module
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Method1() As Task
            Const code = "
Class C
    '''$$
    Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    ''' <typeparam name=""T""></typeparam>
    ''' <param name=""foo""></param>
    ''' <returns></returns>
    Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Method2() As Task
            Const code = "
Class C
    '''$$Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    ''' <typeparam name=""T""></typeparam>
    ''' <param name=""foo""></param>
    ''' <returns></returns>
    Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes1() As Task
            Const code = "
'''$$
''' <summary></summary>
Class C
End Class
"
            Const expected = "
'''
''' $$
''' <summary></summary>
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes2() As Task
            Const code = "
''' <summary>
''' $$
''' </summary>
Class C
End Class
"
            Const expected = "
''' <summary>
''' 
''' $$
''' </summary>
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes3() As Task
            Const code = "
''' <summary>$$</summary>
Class C
End Class
"
            Const expected = "
''' <summary>
''' $$</summary>
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes4() As Task
            Const code = "
    '''$$
    ''' <summary></summary>
    Class C
    End Class
"
            Const expected = "
    '''
    ''' $$
    ''' <summary></summary>
    Class C
    End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes5() As Task
            Const code = "
    ''' <summary>
    ''' $$
    ''' </summary>
    Class C
    End Class
"
            Const expected = "
    ''' <summary>
    ''' 
    ''' $$
    ''' </summary>
    Class C
    End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes6() As Task
            Const code = "
    ''' <summary>$$</summary>
    Class C
    End Class
"
            Const expected = "
    ''' <summary>
    ''' $$</summary>
    Class C
    End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes7() As Task
            Const code = "
Class C
    '''$$
    ''' <summary></summary>
    Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Const expected = "
Class C
    '''
    ''' $$
    ''' <summary></summary>
    Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(540017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540017")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes8() As Task
            Const code = "
''' <summary></summary>$$
Class C
End Class
"
            Const expected = "
''' <summary></summary>
''' $$
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(4817, "https://github.com/dotnet/roslyn/issues/4817")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InsertApostrophes9_AutoGenerateXmlDocCommentsOff() As Task
            Const code = "
''' <summary></summary>$$
Class C
End Class
"
            Const expected = "
''' <summary></summary>
''' $$
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected, autoGenerateXmlDocComments:=False)
        End Function

        <WorkItem(540017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540017")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_DontInsertApostrophes1() As Task
            Const code = "
''' <summary></summary>
''' $$
Class C
End Class
"
            Const expected = "
''' <summary></summary>
''' 
$$
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_NotInsideConstructor() As Task
            Const code = "
Class C
    Sub New()
    '''$$
    End Sub
End Class
"
            Const expected = "
Class C
    Sub New()
    '''
$$
    End Sub
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(537534, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537534")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_NotInsideMethodBody() As Task
            Const code = "
Class C
    Sub Foo()
    '''$$
    End Sub
End Class
"
            Const expected = "
Class C
    Sub Foo()
    '''
$$
    End Sub
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(537550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537550")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_NotBeforeDocComment() As Task
            Const code = "
    Class c1
$$''' <summary>
        ''' 
        ''' </summary>
        ''' <returns></returns>
Public Async Function TestFoo() As Task
            Dim x = 1
        End Sub
    End Class
"
            Const expected = "
    Class c1

$$''' <summary>
        ''' 
        ''' </summary>
        ''' <returns></returns>
Public Async Function TestFoo() As Task
            Dim x = 1
        End Sub
    End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(2091, "https://github.com/dotnet/roslyn/issues/2091")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_InTextBeforeSpace() As Task
            Const code = "
Class C
    ''' <summary>
    ''' hello$$ world
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' hello
    ''' $$world
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Indentation1() As Task
            Const code = "
Class C
    ''' <summary>
    '''     hello world$$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    '''     hello world
    '''     $$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Indentation2() As Task
            Const code = "
Class C
    ''' <summary>
    '''     hello $$world
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    '''     hello 
    '''     $$world
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Indentation3() As Task
            Const code = "
Class C
    ''' <summary>
    '''     hello$$ world
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    '''     hello
    '''     $$world
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Indentation4() As Task
            Const code = "
Class C
    ''' <summary>
    '''     $$hello world
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    '''     
    ''' $$hello world
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Indentation5_UseTabs() As Task
            Const code = "
Class C
    ''' <summary>
	'''     hello world$$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
	'''     hello world
	'''     $$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyPressingEnterAsync(code, expected, useTabs:=True)
        End Function

        <WorkItem(5486, "https://github.com/dotnet/roslyn/issues/5486")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Selection1() As Task
            Const code = "
''' <summary>
''' Hello [|World|]$$!
''' </summary>
Class C
End Class
"
            Const expected = "
''' <summary>
''' Hello 
''' $$!
''' </summary>
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WorkItem(5486, "https://github.com/dotnet/roslyn/issues/5486")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestPressingEnter_Selection2() As Task
            Const code = "
''' <summary>
''' Hello $$[|World|]!
''' </summary>
Class C
End Class
"
            Const expected = "
''' <summary>
''' Hello 
''' $$!
''' </summary>
Class C
End Class
"
            Await VerifyPressingEnterAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_Class() As Task
            Const code = "
Class C
    $$
End Class
"
            Const expected = "
''' <summary>
''' $$
''' </summary>
Class C

End Class
"
            Await VerifyInsertCommentCommandAsync(code, expected)
        End Function

        <WorkItem(4817, "https://github.com/dotnet/roslyn/issues/4817")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_Class_AutoGenerateXmlDocCommentsOff() As Task
            Const code = "
Class C
    $$
End Class
"
            Const expected = "
''' <summary>
''' $$
''' </summary>
Class C

End Class
"
            Await VerifyInsertCommentCommandAsync(code, expected, autoGenerateXmlDocComments:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_Class_NotIfCommentExists() As Task
            Const code = "
''' <summary></summary>
Class C
    $$
End Class
"
            Const expected = "
''' <summary></summary>
Class C
    $$
End Class
"
            Await VerifyInsertCommentCommandAsync(code, expected)
        End Function

        <WorkItem(538715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538715")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_Method1() As Task
            Const code = "
Class C
    Function F()$$
    End Function
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    ''' <returns></returns>
    Function F()
    End Function
End Class
"
            Await VerifyInsertCommentCommandAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_Method2() As Task
            Const code = "
Class C
    Function M(Of T)(foo As Integer) As Integer
        $$Return 0
    End Function
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    ''' <typeparam name=""T""></typeparam>
    ''' <param name=""foo""></param>
    ''' <returns></returns>
    Function M(Of T)(foo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Await VerifyInsertCommentCommandAsync(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_Method_NotIfCommentExists() As Task
            Const code = "
Class C
    ''' <summary></summary>
    Function M(Of T)(foo As Integer) As Integer
        $$Return 0
    End Function
End Class
"
            Const expected = "
Class C
    ''' <summary></summary>
    Function M(Of T)(foo As Integer) As Integer
        $$Return 0
    End Function
End Class
"
            Await VerifyInsertCommentCommandAsync(code, expected)
        End Function

        <WorkItem(538482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_FirstModuleOnLine() As Task
            Const code = "
$$Module M : End Module : Module N : End Module
"

            Const expected = "
''' <summary>
''' $$
''' </summary>
Module M : End Module : Module N : End Module
"
            Await VerifyInsertCommentCommandAsync(code, expected)
        End Function

        <WorkItem(538482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_NotOnSecondModuleOnLine() As Task
            Const code = "Module M : End Module : $$Module N : End Module"
            Const expected = "Module M : End Module : $$Module N : End Module"

            Await VerifyInsertCommentCommandAsync(code, expected)
        End Function

        <WorkItem(538482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestCommand_FirstPropertyOnLine() As Task
            Const code = "
Module M
    Property $$i As Integer : Property j As Integer
End Module
"
            Const expected = "
Module M
    ''' <summary>
    ''' $$
    ''' </summary>
    ''' <returns></returns>
    Property i As Integer : Property j As Integer
End Module
"
            Await VerifyInsertCommentCommandAsync(code, expected)
        End Function

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestOpenLineAbove1() As Task
            Const code = "
Class C
    ''' <summary>
    ''' stuff$$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyOpenLineAboveAsync(code, expected)
        End Function

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestOpenLineAbove2() As Task
            Const code = "
Class C
    ''' <summary>
    ''' $$stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyOpenLineAboveAsync(code, expected)
        End Function

        <WorkItem(538482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TestCommand_NotOnSecondPropertyOnLine()
            Dim code =
                StringFromLines("Module M",
                                "    Property i As Integer : Property $$j As Integer",
                                "End Module")
        End Sub

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestOpenLineAbove3() As Task
            Const code = "
Class C
    ''' $$<summary>
    ''' stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            ' Note that the caret position specified below does Not look correct because
            ' it Is in virtual space in this case.
            Const expected = "
Class C
$$
    ''' <summary>
    ''' stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyOpenLineAboveAsync(code, expected)
        End Function

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestOpenLineAbove4_Tabs() As Task
            Const code = "
Class C
		  ''' <summary>
    ''' $$stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
		  ''' <summary>
		  ''' $$
    ''' stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyOpenLineAboveAsync(code, expected, useTabs:=True)
        End Function

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestOpenLineBelow1() As Task
            Const code = "
Class C
    ''' <summary>
    ''' stuff$$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' stuff
    ''' $$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyOpenLineBelowAsync(code, expected)
        End Function

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestOpenLineBelow2() As Task
            Const code = "
Class C
    ''' <summary>
    ''' $$stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' stuff
    ''' $$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyOpenLineBelowAsync(code, expected)
        End Function

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestOpenLineBelow3() As Task
            Const code = "
''' <summary>
''' stuff
''' $$</summary>"
            Const expected = "
''' <summary>
''' stuff
''' </summary>
''' $$"
            Await VerifyOpenLineBelowAsync(code, expected)
        End Function

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Async Function TestOpenLineBelow4_Tabs() As Task
            Const code = "
Class C
    ''' <summary>
		  ''' $$stuff
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
		  ''' stuff
		  ''' $$
    ''' </summary>
    Sub M()
    End Sub
End Class
"
            Await VerifyOpenLineBelowAsync(code, expected, useTabs:=True)
        End Function

        Friend Overrides Function CreateCommandHandler(
            waitIndicator As IWaitIndicator,
            undoHistoryRegistry As ITextUndoHistoryRegistry,
            editorOperationsFactoryService As IEditorOperationsFactoryService,
            completionService As IAsyncCompletionService) As ICommandHandler

            Return New DocumentationCommentCommandHandler(waitIndicator, undoHistoryRegistry, editorOperationsFactoryService, completionService)
        End Function

        Protected Overrides Function CreateTestWorkspaceAsync(code As String) As Task(Of TestWorkspace)
            Return TestWorkspace.CreateVisualBasicAsync(code)
        End Function

        Protected Overrides ReadOnly Property DocumentationCommentCharacter As Char
            Get
                Return "'"c
            End Get
        End Property
    End Class
End Namespace
