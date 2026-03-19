' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentationComments
    <Trait(Traits.Feature, Traits.Features.DocumentationComments)>
    Public Class DocumentationCommentTests
        Inherits AbstractDocumentationCommentTests

        Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures.AddParts(GetType(CommitConnectionListener))

        <WpfFact>
        Public Sub TestTypingCharacter_Class_AutoGenerateXmlDocCommentsOff()
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
            VerifyTypingCharacter(code, expected, globalOptions:=New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, False}
            })
        End Sub

        <WpfFact>
        Public Sub TestTypingCharacter_Class()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestTypingCharacter_Method()
            Const code = "
Class C
    ''$$
    Function M(Of T)(goo As Integer, i() As Integer) As Integer
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
    ''' <param name=""goo""></param>
    ''' <param name=""i""></param>
    ''' <returns></returns>
    Function M(Of T)(goo As Integer, i() As Integer) As Integer
        Return 0
    End Function
End Class
"
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538715")>
        Public Sub TestTypingCharacter_NoReturnType()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestTypingCharacter_NotWhenDocCommentExists1()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestTypingCharacter_NotWhenDocCommentExists2()
            Const code = "
Class C
    ''$$
    ''' <summary></summary>
    Function M(Of T)(goo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Const expected = "
Class C
    '''$$
    ''' <summary></summary>
    Function M(Of T)(goo As Integer) As Integer
        Return 0
    End Function
End Class
"
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537506")>
        Public Sub TestTypingCharacter_NotAfterClassName()
            Const code = "
Class C''$$
End Class
"
            Const expected = "
Class C'''$$
End Class
"
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537508")>
        Public Sub TestTypingCharacter_NotInsideClass()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537510")>
        Public Sub TestTypingCharacter_NotAfterConstructorName()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537511")>
        Public Sub TestTypingCharacter_NotInsideConstructor()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537512")>
        Public Sub TestTypingCharacter_NotInsideMethodBody()
            Const code = "
Class C
    Sub Goo()
    ''$$
    End Sub
End Class
"
            Const expected = "
Class C
    Sub Goo()
    '''$$
    End Sub
End Class
"
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540004")>
        Public Sub TestTypingCharacter_NoReturnsOnWriteOnlyProperty()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_Class1()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_Class1_AutoGenerateXmlDocCommentsOff()
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
            VerifyPressingEnter(code, expected, globalOptions:=New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, False}
            })
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_Class2()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_Class3()
            Const code = "
'''$$<Goo()> Class C
End Class
"
            Const expected = "
''' <summary>
''' $$
''' </summary>
<Goo()> Class C
End Class
"
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538717")>
        Public Sub TestPressingEnter_Module()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_Method1()
            Const code = "
Class C
    '''$$
    Function M(Of T)(goo As Integer) As Integer
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
    ''' <param name=""goo""></param>
    ''' <returns></returns>
    Function M(Of T)(goo As Integer) As Integer
        Return 0
    End Function
End Class
"
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_Method2()
            Const code = "
Class C
    '''$$Function M(Of T)(goo As Integer) As Integer
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
    ''' <param name=""goo""></param>
    ''' <returns></returns>
    Function M(Of T)(goo As Integer) As Integer
        Return 0
    End Function
End Class
"
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_InsertApostrophes1()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_InsertApostrophes2()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_InsertApostrophes3()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_InsertApostrophes4()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_InsertApostrophes5()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_InsertApostrophes6()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_InsertApostrophes7()
            Const code = "
Class C
    '''$$
    ''' <summary></summary>
    Function M(Of T)(goo As Integer) As Integer
        Return 0
    End Function
End Class
"
            Const expected = "
Class C
    '''
    ''' $$
    ''' <summary></summary>
    Function M(Of T)(goo As Integer) As Integer
        Return 0
    End Function
End Class
"
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540017")>
        Public Sub TestPressingEnter_InsertApostrophes8()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4817")>
        Public Sub TestPressingEnter_InsertApostrophes9_AutoGenerateXmlDocCommentsOff()
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
            VerifyPressingEnter(code, expected, globalOptions:=New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, False}
            })
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540017")>
        Public Sub TestPressingEnter_DoNotInsertApostrophes1()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestPressingEnter_NotInsideConstructor()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537534")>
        Public Sub TestPressingEnter_NotInsideMethodBody()
            Const code = "
Class C
    Sub Goo()
    '''$$
    End Sub
End Class
"
            Const expected = "
Class C
    Sub Goo()
    '''
$$
    End Sub
End Class
"
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537550")>
        Public Sub TestPressingEnter_NotBeforeDocComment()
            Const code = "
    Class c1
$$''' <summary>
        ''' 
        ''' </summary>
        ''' <returns></returns>
Public Async Function TestGoo() As Task
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
Public Async Function TestGoo() As Task
            Dim x = 1
        End Sub
    End Class
"
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2091")>
        Public Sub TestPressingEnter_InTextBeforeSpace()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")>
        Public Sub TestPressingEnter_Indentation1()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")>
        Public Sub TestPressingEnter_Indentation2()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")>
        Public Sub TestPressingEnter_Indentation3()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")>
        Public Sub TestPressingEnter_Indentation4()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2108")>
        Public Sub TestPressingEnter_Indentation5_UseTabs()
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
            VerifyPressingEnter(code, expected, useTabs:=True)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5486")>
        Public Sub TestPressingEnter_Selection1()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5486")>
        Public Sub TestPressingEnter_Selection2()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestCommand_Class()
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
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4817")>
        Public Sub TestCommand_Class_AutoGenerateXmlDocCommentsOff()
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
            VerifyInsertCommentCommand(code, expected, globalOptions:=New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, False}
            })
        End Sub

        <WpfFact>
        Public Sub TestCommand_Class_NotIfCommentExists()
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
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538715")>
        Public Sub TestCommand_Method1()
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
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestCommand_Method2()
            Const code = "
Class C
    Function M(Of T)(goo As Integer) As Integer
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
    ''' <param name=""goo""></param>
    ''' <returns></returns>
    Function M(Of T)(goo As Integer) As Integer
        Return 0
    End Function
End Class
"
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact>
        Public Sub TestCommand_Method_NotIfCommentExists()
            Const code = "
Class C
    ''' <summary></summary>
    Function M(Of T)(goo As Integer) As Integer
        $$Return 0
    End Function
End Class
"
            Const expected = "
Class C
    ''' <summary></summary>
    Function M(Of T)(goo As Integer) As Integer
        $$Return 0
    End Function
End Class
"
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")>
        Public Sub TestCommand_FirstModuleOnLine()
            Const code = "
$$Module M : End Module : Module N : End Module
"

            Const expected = "
''' <summary>
''' $$
''' </summary>
Module M : End Module : Module N : End Module
"
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")>
        Public Sub TestCommand_NotOnSecondModuleOnLine()
            Const code = "Module M : End Module : $$Module N : End Module"
            Const expected = "Module M : End Module : $$Module N : End Module"

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")>
        Public Sub TestCommand_FirstPropertyOnLine()
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
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")>
        Public Sub TestOpenLineAbove1()
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
            VerifyOpenLineAbove(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")>
        Public Sub TestOpenLineAbove2()
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
            VerifyOpenLineAbove(code, expected)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538482")>
        Public Sub TestCommand_NotOnSecondPropertyOnLine()
            Dim code =
                StringFromLines("Module M",
                                "    Property i As Integer : Property $$j As Integer",
                                "End Module")
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")>
        Public Sub TestOpenLineAbove3()
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
            VerifyOpenLineAbove(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")>
        Public Sub TestOpenLineAbove4_Tabs()
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
            VerifyOpenLineAbove(code, expected, useTabs:=True)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")>
        Public Sub TestOpenLineBelow1()
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
            VerifyOpenLineBelow(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")>
        Public Sub TestOpenLineBelow2()
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
            VerifyOpenLineBelow(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")>
        Public Sub TestOpenLineBelow3()
            Const code = "
''' <summary>
''' stuff
''' $$</summary>"
            Const expected = "
''' <summary>
''' stuff
''' </summary>
''' $$"
            VerifyOpenLineBelow(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2090")>
        Public Sub TestOpenLineBelow4_Tabs()
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
            VerifyOpenLineBelow(code, expected, useTabs:=True)
        End Sub

        <WpfFact>
        Public Sub TestTypingCharacter_WithExistingComment()
            Const code = "
''$$ This is a comment for this class
Class C
End Class
"
            Const expected = "
''' <summary>
''' $$This is a comment for this class
''' </summary>
Class C
End Class
"
            VerifyTypingCharacter(code, expected)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")>
        Public Sub TypingCharacter_Class_Collapsed()
            Dim globalOptions = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, True}
            }

            Const code = "
''$$
Class C
End Class
"
            Const expected = "
''' <summary>$$</summary>
Class C
End Class
"
            VerifyTypingCharacter(code, expected, globalOptions:=globalOptions)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")>
        Public Sub TypingCharacter_Method_Collapsed()
            Dim globalOptions = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, True}
            }

            Const code = "
Class C
    ''$$
    Sub M()
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>$$</summary>
    Sub M()
    End Sub
End Class
"
            VerifyTypingCharacter(code, expected, globalOptions:=globalOptions)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")>
        Public Sub TypingCharacter_MethodWithParameters_Collapsed()
            Dim globalOptions = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, True}
            }

            Const code = "
Class C
    ''$$
    Sub M(x As Integer, y As String)
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>$$</summary>
    ''' <param name=""x""></param>
    ''' <param name=""y""></param>
    Sub M(x As Integer, y As String)
    End Sub
End Class
"
            VerifyTypingCharacter(code, expected, globalOptions:=globalOptions)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")>
        Public Sub TypingCharacter_Property_Collapsed()
            Dim globalOptions = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, True}
            }

            Const code = "
Class C
    ''$$
    Public Property P As Integer
End Class
"
            Const expected = "
Class C
    ''' <summary>$$</summary>
    ''' <returns></returns>
    Public Property P As Integer
End Class
"
            VerifyTypingCharacter(code, expected, globalOptions:=globalOptions)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")>
        Public Sub TypingCharacter_MethodWithParameters_OnlySummary()
            Dim globalOptions = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.GenerateOnlySummaryTag, True}
            }

            Const code = "
Class C
    ''$$
    Sub M(x As Integer, y As String)
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    Sub M(x As Integer, y As String)
    End Sub
End Class
"
            VerifyTypingCharacter(code, expected, globalOptions:=globalOptions)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")>
        Public Sub TypingCharacter_MethodWithParameters_OnlySummaryAndSingleLine()
            Dim globalOptions = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, True},
                {DocumentationCommentOptionsStorage.GenerateOnlySummaryTag, True}
            }

            Const code = "
Class C
    ''$$
    Sub M(x As Integer, y As String)
    End Sub
End Class
"
            Const expected = "
Class C
    ''' <summary>$$</summary>
    Sub M(x As Integer, y As String)
    End Sub
End Class
"
            VerifyTypingCharacter(code, expected, globalOptions:=globalOptions)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/10968")>
        Public Sub PressingEnter_InsideSingleLineSummary_Expands()
            Dim globalOptions = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, True}
            }

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
            VerifyPressingEnter(code, expected, globalOptions:=globalOptions)
        End Sub

        Friend Overrides Function CreateCommandHandler(workspace As EditorTestWorkspace) As ICommandHandler
            Return workspace.ExportProvider.GetCommandHandler(Of DocumentationCommentCommandHandler)(PredefinedCommandHandlerNames.DocumentationComments, ContentTypeNames.VisualBasicContentType)
        End Function

        Protected Overrides Function CreateTestWorkspace(code As String) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(code, composition:=s_composition)
        End Function

        Protected Overrides ReadOnly Property DocumentationCommentCharacter As Char
            Get
                Return "'"c
            End Get
        End Property
    End Class
End Namespace
