' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentationComments
    Public Class DocumentationCommentTests
        Inherits AbstractDocumentationCommentTests

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_Class()
            Dim code =
                StringFromLines("''$$",
                                "Class C",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary>",
                                "''' $$",
                                "''' </summary>",
                                "Class C",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_Method()
            Dim code =
                StringFromLines("Class C",
                                "    ''$$",
                                "    Function M(Of T)(foo As Integer, i() As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    ''' <summary>",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    ''' <typeparam name=""T""></typeparam>",
                                "    ''' <param name=""foo""></param>",
                                "    ''' <param name=""i""></param>",
                                "    ''' <returns></returns>",
                                "    Function M(Of T)(foo As Integer, i() As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(538715)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NoReturnType()
            Dim code =
                StringFromLines("Class C",
                                "   ''$$",
                                "   Function F()",
                                "   End Function",
                                "End Class")
            Dim expected =
                StringFromLines("Class C",
                                "   ''' <summary>",
                                "   ''' $$",
                                "   ''' </summary>",
                                "   ''' <returns></returns>",
                                "   Function F()",
                                "   End Function",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotWhenDocCommentExists1()
            Dim code =
                StringFromLines("''$$",
                                "''' <summary></summary>",
                                "Class C",
                                "End Class")

            Dim expected =
                StringFromLines("'''$$",
                                "''' <summary></summary>",
                                "Class C",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotWhenDocCommentExists2()
            Dim code =
                StringFromLines("Class C",
                                "    ''$$",
                                "    ''' <summary></summary>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    '''$$",
                                "    ''' <summary></summary>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(537506)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotAfterClassName()
            Dim code =
                StringFromLines("Class C''$$",
                                "End Class")

            Dim expected =
                StringFromLines("Class C'''$$",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(537508)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotInsideClass()
            Dim code =
                StringFromLines("Class C",
                                "    ''$$",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    '''$$",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(537510)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotAfterConstructorName()
            Dim code =
                StringFromLines("Class C",
                                "    Sub New()''$$",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    Sub New()'''$$",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(537511)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotInsideConstructor()
            Dim code =
                StringFromLines("Class C",
                                "    Sub New()",
                                "    ''$$",
                                "    End Sub",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    Sub New()",
                                "    '''$$",
                                "    End Sub",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(537512)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NotInsideMethodBody()
            Dim code =
                StringFromLines("Class C",
                                "    Sub Foo()",
                                "    ''$$",
                                "    End Sub",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    Sub Foo()",
                                "    '''$$",
                                "    End Sub",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <WorkItem(540004)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub TypingCharacter_NoReturnsOnWriteOnlyProperty()
            Dim code =
                StringFromLines("Class C",
                                "    ''$$",
                                "    WriteOnly Property Prop As Integer",
                                "        Set(ByVal value As Integer",
                                "        End Set",
                                "    End Property",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    ''' <summary>",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    WriteOnly Property Prop As Integer",
                                "        Set(ByVal value As Integer",
                                "        End Set",
                                "    End Property",
                                "End Class")

            VerifyTypingCharacter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Class1()
            Dim code =
                StringFromLines("'''$$",
                                "Class C",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary>",
                                "''' $$",
                                "''' </summary>",
                                "Class C",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Class2()
            Dim code =
                StringFromLines("'''$$Class C",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary>",
                                "''' $$",
                                "''' </summary>",
                                "Class C",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Class3()
            Dim code =
                StringFromLines("'''$$<Foo()> Class C",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary>",
                                "''' $$",
                                "''' </summary>",
                                "<Foo()> Class C",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(538717)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Module()
            Dim code =
                StringFromLines("'''$$Module M",
                                "   dim x as Integer",
                                "End Module")
            Dim expected =
                StringFromLines("''' <summary>",
                                "''' $$",
                                "''' </summary>",
                                "Module M",
                                "    Dim x As Integer",
                                "End Module")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Method1()
            Dim code =
                StringFromLines("Class C",
                                "    '''$$",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    ''' <summary>",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    ''' <typeparam name=""T""></typeparam>",
                                "    ''' <param name=""foo""></param>",
                                "    ''' <returns></returns>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_Method2()
            Dim code =
                StringFromLines("Class C",
                                "    '''$$Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    ''' <summary>",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    ''' <typeparam name=""T""></typeparam>",
                                "    ''' <param name=""foo""></param>",
                                "    ''' <returns></returns>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes1()
            Dim code =
                StringFromLines("'''$$",
                                "''' <summary></summary>",
                                "Class C",
                                "End Class")

            Dim expected =
                StringFromLines("'''",
                                "''' $$",
                                "''' <summary></summary>",
                                "Class C",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes2()
            Dim code =
                StringFromLines("''' <summary>",
                                "''' $$",
                                "''' </summary>",
                                "Class C",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary>",
                                "''' ",
                                "''' $$",
                                "''' </summary>",
                                "Class C",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes3()
            Dim code =
                StringFromLines("''' <summary>$$</summary>",
                                "Class C",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary>",
                                "''' $$</summary>",
                                "Class C",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes4()
            Dim code =
                StringFromLines("    '''$$",
                                "    ''' <summary></summary>",
                                "    Class C",
                                "    End Class")

            Dim expected =
                StringFromLines("    '''",
                                "    ''' $$",
                                "    ''' <summary></summary>",
                                "    Class C",
                                "    End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes5()
            Dim code =
                StringFromLines("    ''' <summary>",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    Class C",
                                "    End Class")

            Dim expected =
                StringFromLines("    ''' <summary>",
                                "    ''' ",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    Class C",
                                "    End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes6()
            Dim code =
                StringFromLines("    ''' <summary>$$</summary>",
                                "    Class C",
                                "    End Class")

            Dim expected =
                StringFromLines("    ''' <summary>",
                                "    ''' $$</summary>",
                                "    Class C",
                                "    End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes7()
            Dim code =
                StringFromLines("Class C",
                                "    '''$$",
                                "    ''' <summary></summary>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    '''",
                                "    ''' $$",
                                "    ''' <summary></summary>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(540017)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_InsertApostrophes8()
            Dim code =
                StringFromLines("''' <summary></summary>$$",
                                "Class C",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary></summary>",
                                "''' $$",
                                "Class C",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(540017)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_DontInsertApostrophes1()
            Dim code =
                StringFromLines("''' <summary></summary>",
                                "''' $$",
                                "Class C",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary></summary>",
                                "''' ",
                                "$$",
                                "Class C",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_NotInsideConstructor()
            Dim code =
                StringFromLines("Class C",
                                "    Sub New()",
                                "    '''$$",
                                "    End Sub",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    Sub New()",
                                "    '''",
                                "$$",
                                "    End Sub",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(537534)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_NotInsideMethodBody()
            Dim code =
                StringFromLines("Class C",
                                "    Sub Foo()",
                                "    '''$$",
                                "    End Sub",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    Sub Foo()",
                                "    '''",
                                "$$",
                                "    End Sub",
                                "End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <WorkItem(537550)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub PressingEnter_NotBeforeDocComment()
            Dim code =
                StringFromLines("    Class c1",
                                "$$''' <summary>",
                                "        ''' ",
                                "        ''' </summary>",
                                "        ''' <returns></returns>",
                                "        Public Sub Foo()",
                                "            Dim x = 1",
                                "        End Sub",
                                "    End Class")

            Dim expected =
                StringFromLines("    Class c1",
                                "",
                                "$$''' <summary>",
                                "        ''' ",
                                "        ''' </summary>",
                                "        ''' <returns></returns>",
                                "        Public Sub Foo()",
                                "            Dim x = 1",
                                "        End Sub",
                                "    End Class")

            VerifyPressingEnter(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Class()
            Dim code =
                StringFromLines("Class C",
                                "    $$",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary>",
                                "''' $$",
                                "''' </summary>",
                                "Class C",
                                "",
                                "End Class")

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Class_NotIfCommentExists()
            Dim code =
                StringFromLines("''' <summary></summary>",
                                "Class C",
                                "    $$",
                                "End Class")

            Dim expected =
                StringFromLines("''' <summary></summary>",
                                "Class C",
                                "    $$",
                                "End Class")

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WorkItem(538715)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Method1()
            Dim code =
                StringFromLines("Class C",
                                "    Function F()$$",
                                "    End Function",
                                "End Class")
            Dim expected =
                StringFromLines("Class C",
                                "    ''' <summary>",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    ''' <returns></returns>",
                                "    Function F()",
                                "    End Function",
                                "End Class")
            VerifyInsertCommentCommand(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Method2()
            Dim code =
                StringFromLines("Class C",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        $$Return 0",
                                "    End Function",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    ''' <summary>",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    ''' <typeparam name=""T""></typeparam>",
                                "    ''' <param name=""foo""></param>",
                                "    ''' <returns></returns>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        Return 0",
                                "    End Function",
                                "End Class")

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_Method_NotIfCommentExists()
            Dim code =
                StringFromLines("Class C",
                                "    ''' <summary></summary>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        $$Return 0",
                                "    End Function",
                                "End Class")

            Dim expected =
                StringFromLines("Class C",
                                "    ''' <summary></summary>",
                                "    Function M(Of T)(foo As Integer) As Integer",
                                "        $$Return 0",
                                "    End Function",
                                "End Class")

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WorkItem(538482)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_FirstModuleOnLine()
            Dim code = "$$Module M : End Module : Module N : End Module"

            Dim expected =
                StringFromLines("''' <summary>",
                                "''' $$",
                                "''' </summary>",
                                "Module M : End Module : Module N : End Module")

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WorkItem(538482)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_NotOnSecondModuleOnLine()
            Dim code = "Module M : End Module : $$Module N : End Module"
            Dim expected = "Module M : End Module : $$Module N : End Module"

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WorkItem(538482)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_FirstPropertyOnLine()
            Dim code =
                StringFromLines("Module M",
                                "    Property $$i As Integer : Property j As Integer",
                                "End Module")

            Dim expected =
                StringFromLines("Module M",
                                "    ''' <summary>",
                                "    ''' $$",
                                "    ''' </summary>",
                                "    ''' <returns></returns>",
                                "    Property i As Integer : Property j As Integer",
                                "End Module")

            VerifyInsertCommentCommand(code, expected)
        End Sub

        <WorkItem(538482)>
        <Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)>
        Public Sub Command_NotOnSecondPropertyOnLine()
            Dim code =
                StringFromLines("Module M",
                                "    Property i As Integer : Property $$j As Integer",
                                "End Module")

            Dim expected =
                StringFromLines("Module M",
                                "    Property i As Integer : Property $$j As Integer",
                                "End Module")

            VerifyInsertCommentCommand(code, expected)
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
