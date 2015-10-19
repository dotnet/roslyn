' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentationComments
    Public Class DocumentationCommentTests
        Inherits AbstractDocumentationCommentTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_Class_AutoGenerateXmlDocCommentsOff()
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
            VerifyTypingCharacter(code, expected, autoGenerateXmlDocComments:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_Class()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_Method()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(538715)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NoReturnType()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotWhenDocCommentExists1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotWhenDocCommentExists2()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(537506)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotAfterClassName()
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

        <WorkItem(537508)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotInsideClass()
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

        <WorkItem(537510)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotAfterConstructorName()
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

        <WorkItem(537511)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotInsideConstructor()
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

        <WorkItem(537512)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotInsideMethodBody()
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
            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(540004)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NoReturnsOnWriteOnlyProperty()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Class1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Class1_AutoGenerateXmlDocCommentsOff()
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
            VerifyPressingEnter(code, expected, autoGenerateXmlDocComments:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Class2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Class3()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(538717)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Module()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Method1()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Method2()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes4()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes5()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes6()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes7()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(540017)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes8()
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

        <WorkItem(4817, "https://github.com/dotnet/roslyn/issues/4817")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes9_AutoGenerateXmlDocCommentsOff()
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
            VerifyPressingEnter(code, expected, autoGenerateXmlDocComments:=False)
        End Sub

        <WorkItem(540017)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_DontInsertApostrophes1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_NotInsideConstructor()
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

        <WorkItem(537534)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_NotInsideMethodBody()
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
            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(537550)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_NotBeforeDocComment()
            Const code = "
    Class c1
$$''' <summary>
        ''' 
        ''' </summary>
        ''' <returns></returns>
        Public Sub Foo()
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
        Public Sub Foo()
            Dim x = 1
        End Sub
    End Class
"
            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(2091, "https://github.com/dotnet/roslyn/issues/2091")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InTextBeforeSpace()
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

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Indentation1()
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

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Indentation2()
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

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Indentation3()
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

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Indentation4()
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

        <WorkItem(2108, "https://github.com/dotnet/roslyn/issues/2108")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Indentation5_UseTabs()
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

        <WorkItem(5486, "https://github.com/dotnet/roslyn/issues/5486")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Selection1()
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

        <WorkItem(5486, "https://github.com/dotnet/roslyn/issues/5486")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Selection2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Class()
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

        <WorkItem(4817, "https://github.com/dotnet/roslyn/issues/4817")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Class_AutoGenerateXmlDocCommentsOff()
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
            VerifyInsertCommentCommand(code, expected, autoGenerateXmlDocComments:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Class_NotIfCommentExists()
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

        <WorkItem(538715)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Method1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Method2()
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
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Method_NotIfCommentExists()
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
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WorkItem(538482)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_FirstModuleOnLine()
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

        <WorkItem(538482)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_NotOnSecondModuleOnLine()
            Const code = "Module M : End Module : $$Module N : End Module"
            Const expected = "Module M : End Module : $$Module N : End Module"

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WorkItem(538482)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_FirstPropertyOnLine()
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

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
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

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
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

        <WorkItem(538482)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_NotOnSecondPropertyOnLine()
            Dim code =
                StringFromLines("Module M",
                                "    Property i As Integer : Property $$j As Integer",
                                "End Module")
        End Sub

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
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

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
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

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
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

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
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

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
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

        <WorkItem(2090, "https://github.com/dotnet/roslyn/issues/2090")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
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

        Friend Overrides Function CreateCommandHandler(
            waitIndicator As IWaitIndicator,
            undoHistoryRegistry As ITextUndoHistoryRegistry,
            editorOperationsFactoryService As IEditorOperationsFactoryService,
            completionService As IAsyncCompletionService) As ICommandHandler

            Return New DocumentationCommentCommandHandler(waitIndicator, undoHistoryRegistry, editorOperationsFactoryService, completionService)
        End Function

        Protected Overrides Function CreateTestWorkspace(code As String) As TestWorkspace
            Return VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
        End Function

        Protected Overrides ReadOnly Property DocumentationCommentCharacter As Char
            Get
                Return "'"c
            End Get
        End Property
    End Class
End Namespace
