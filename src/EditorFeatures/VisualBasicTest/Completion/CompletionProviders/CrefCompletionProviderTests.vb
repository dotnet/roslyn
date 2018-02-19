' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class CrefCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New CrefCompletionProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotOutsideCref() As Task
            Dim text = <File>
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
</File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotOutsideCref2() As Task
            Dim text = <File>
Class C
    $$
    Sub Goo()
    End Sub
End Class
</File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotOutsideCref3() As Task
            Dim text = <File>
Class C
    Sub Goo()
        Me.$$
    End Sub
End Class
</File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterCrefOpenQuote() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="$$
''' </summary>
Module Program
    Sub Goo()
    End Sub
End Module]]></File>.Value

            Await VerifyAnyItemExistsAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRightSideOfQualifiedName() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program.$$"
''' </summary>
Module Program
    Sub Goo()
    End Sub
End Module]]></File>.Value

            Await VerifyItemExistsAsync(text, "Goo()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInTypeParameterContext() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of $$
''' </summary>
Class Program(Of T)
    Sub Goo()
    End Sub
End Class]]></File>.Value

            Await VerifyItemIsAbsentAsync(text, "Integer")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInSignature_FirstParameter() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Goo($$"
''' </summary>
Class Program(Of T)
    Sub Goo(z as Integer)
    End Sub
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Integer")
            Await VerifyItemIsAbsentAsync(text, "Goo(Integer")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInSignature_SecondParameter() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Goo(Integer, $$"
''' </summary>
Class Program(Of T)
    Sub Goo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Integer")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAfterSignature() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Goo(Integer, Integer)$$"
''' </summary>
Class Program(Of T)
    Sub Goo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAfterDotAfterSignature() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Goo(Integer, Integer).$$"
''' </summary>
Class Program(Of T)
    Sub Goo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMethodParametersIncluded() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="Program(Of T).$$"
''' </summary>
Class Program(Of T)
    Sub Goo(ByRef z As Integer, ByVal x As Integer, ParamArray xx As Integer())
    End Sub
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Goo(ByRef Integer, Integer, Integer())")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypesSuggestedWithTypeParameters() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="$$"
''' </summary>
Class Program(Of TTypeParameter)
End Class

Class Program
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Program")
            Await VerifyItemExistsAsync(text, "Program(Of TTypeParameter)")
        End Function
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOperators() As Task
            Dim text = <File><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Class C
    ''' <summary>
    '''  <see cref="<see cref="C.$$"
    ''' </summary>
    ''' <param name="c"></param>
    ''' <returns></returns>
    Public Shared Operator +(c As C)

    End Operator
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Operator +(C)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestModOperator() As Task
            Dim text = <File><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Class C
    ''' <summary>
    '''  <see cref="<see cref="C.$$"
    ''' </summary>
    ''' <param name="c"></param>
    ''' <returns></returns>
    Public Shared Operator Mod (c As C, a as Integer)

    End Operator
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Operator Mod(C, Integer)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestConstructorsShown() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
    Sub New(x as Integer)
    End Sub
End Class
]]></File>.Value

            Await VerifyItemExistsAsync(text, "New(Integer)")
        End Function
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterNamespace() As Task
            Dim text = <File><![CDATA[
Imports System
''' <summary>
''' <see cref="System.$$"/>
''' </summary>
Class C
    Sub New(x as Integer)
    End Sub
End Class
]]></File>.Value

            Await VerifyItemExistsAsync(text, "String")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterizedProperties() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
    Public Property Item(x As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Property Item(x As Integer, y As String) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
 

]]></File>.Value

            Await VerifyItemExistsAsync(text, "Item(Integer)")
            Await VerifyItemExistsAsync(text, "Item(Integer, String)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoIdentifierEscaping() As Task
            Dim text = <File><![CDATA[
''' <see cref="A.$$"/>
''' </summary>
Class A
End Class

]]></File>.Value

            Await VerifyItemExistsAsync(text, "GetType()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoCommitOnParen() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Dim expected = <File><![CDATA[
''' <summary>
''' <see cref="C.("/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Await VerifyProviderCommitAsync(text, "bar(Integer, Integer)", expected, "("c, "bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowTypingTypeParameters() As Task
            Dim text = <File><![CDATA[
Imports System.Collections.Generic
''' <summary>
''' <see cref="$$"/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Dim expected = <File><![CDATA[
Imports System.Collections.Generic
''' <summary>
''' <see cref=" "/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Await VerifyProviderCommitAsync(text, "List(Of T)", expected, " "c, "List(Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOfAfterParen() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Goo($$
''' </summary>
Module Program
    Sub Goo()
    End Sub
End Module]]></File>.Value

            Await VerifyItemExistsAsync(text, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOfNotAfterComma() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Goo(a, $$
''' </summary>
Module Program
    Sub Goo()
    End Sub
End Module]]></File>.Value

            Await VerifyItemIsAbsentAsync(text, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCrefCompletionSpeculatesOutsideTrivia() As Task
            Dim text = <a><![CDATA[
Class C
    ''' <see cref="$$
    Sub goo()
    End Sub
End Class]]></a>.Value.NormalizeLineEndings()

            Using workspace = TestWorkspace.Create(LanguageNames.VisualBasic, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication), New VisualBasicParseOptions(), {text})
                Dim called = False

                Dim completionProvider = New CrefCompletionProvider(
                    Sub(node As SyntaxNode)
                        ' asserts that we aren't be asked speculate on nodes inside documentation trivia.
                        ' This verifies that the provider Is asking for a speculative SemanticModel
                        ' by walking to the node the documentation Is attached to. 

                        called = True
                        Dim trivia = node.GetAncestor(Of DocumentationCommentTriviaSyntax)
                        Assert.Null(trivia)
                    End Sub)

                Dim hostDocument = workspace.DocumentWithCursor
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim service = CreateCompletionService(
                    workspace,
                    ImmutableArray.Create(Of CompletionProvider)(completionProvider))
                Dim completionList = Await GetCompletionListAsync(service, document, hostDocument.CursorPosition.Value, CompletionTrigger.Invoke)

                Assert.True(called)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoSuggestionAfterEmptyCref() As Task
            Dim text = "
Class C
    ''' <see cref="""" $$
    Sub Goo()
    End Sub
End Class
"

            Await VerifyNoItemsExistAsync(text)
        End Function
    End Class
End Namespace
