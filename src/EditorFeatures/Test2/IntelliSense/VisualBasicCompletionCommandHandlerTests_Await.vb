' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class VisualBasicCompletionCommandHandlerTests_Await

        Private Shared Function GetTestClassDocument(containerHasAsyncModifier As Boolean, testExpression As String) As XElement
            Return _
<Document>
Imports System
Imports System.Threading.Tasks

Class C
    Public ReadOnly Property Self As C
        Get
            Return Me
        End Get
    End Property

    Public Field As Task = Task.CompletedTask

    Public Function Method() As Task
        Return Task.CompletedTask
    End Function

    Public ReadOnly Property [Property] As Task
        Get
            Return Task.CompletedTask
        End Get
    End Property

    Default Public ReadOnly Property Item(ByVal i As Integer) As Task
        Get
            Return Task.CompletedTask
        End Get
    End Property

    Public Function Func() As Func(Of Task)
        Return Function() Task.CompletedTask
    End Function
End Class

Module Program
    Shared StaticField As Task = Task.CompletedTask

    Private Shared ReadOnly Property StaticProperty As Task
        Get
            Return Task.CompletedTask
        End Get
    End Property

    Private Function StaticMethod() As Task
        Return Task.CompletedTask
    End Function

    Private<%= If(containerHasAsyncModifier, " Async", "") %> Function Main(ByVal parameter As Task) As Task
        Dim local As Task = Task.CompletedTask
        Dim c = New C()

        <%= testExpression %>
    End Function
End Module
</Document>
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_FunctionDeclaration() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Function Main() As Task
        $$
    End Function
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Async Function Main() As Task
        Await
    End Function
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SubDeclaration() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Public Class C
    Public Shared Sub Main()
        $$
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Public Class C
    Public Shared Async Sub Main()
        Await
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_MultiLineFunctionLambdaExpression() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System

Public Class C
    Public Shared Sub Main()
        Dim x As Func(Of Boolean) = Function()
                                        $$
                                    End Function
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System

Public Class C
    Public Shared Sub Main()
        Dim x As Func(Of Boolean) = Async Function()
                                        Await
                                    End Function
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_MultiLineSubLambdaExpression() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System

Public Class C
    Public Shared Sub Main()
        Dim x As Action = Sub()
                              $$
                          End Sub
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System

Public Class C
    Public Shared Sub Main()
        Dim x As Action = Async Sub()
                              Await
                          End Sub
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SingleLineFunctionLambdaExpression() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Sub Main()
        Dim x As Func(Of Boolean) = Function() $$
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Sub Main()
        Dim x As Func(Of Boolean) = Async Function() Await
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SingleLineSubLambdaExpression() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Sub Main()
        Dim x As Action = Sub() $$
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Sub Main()
        Dim x As Action = Async Sub() Await
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_FunctionDeclaration_AlreadyAsync() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Async Function Main() As Task
        $$
    End Function
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:="")

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Async Function Main() As Task
        Await
    End Function
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SubDeclaration_AlreadyAsync() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Public Class C
    Public Shared Async Sub Main()
        $$
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:="")

                state.SendTab()
                Assert.Equal("
Public Class C
    Public Shared Async Sub Main()
        Await
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_MultiLineFunctionLambdaExpression_AlreadyAsync() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System

Public Class C
    Public Shared Sub Main()
        Dim x As Func(Of Boolean) = Async Function()
                                        $$
                                    End Function
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:="")

                state.SendTab()
                Assert.Equal("
Imports System

Public Class C
    Public Shared Sub Main()
        Dim x As Func(Of Boolean) = Async Function()
                                        Await
                                    End Function
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_MultiLineSubLambdaExpression_AlreadyAsync() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System

Public Class C
    Public Shared Sub Main()
        Dim x As Action = Async Sub()
                              $$
                          End Sub
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:="")

                state.SendTab()
                Assert.Equal("
Imports System

Public Class C
    Public Shared Sub Main()
        Dim x As Action = Async Sub()
                              Await
                          End Sub
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SingleLineFunctionLambdaExpression_AlreadyAsync() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Sub Main()
        Dim x As Func(Of Boolean) = Async Function() $$
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:="")

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Sub Main()
        Dim x As Func(Of Boolean) = Async Function() Await
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function AwaitCompletionAddsAsync_SingleLineSubLambdaExpression_AlreadyAsync() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Sub Main()
        Dim x As Action = Async Sub() $$
    End Sub
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:="")

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Sub Main()
        Dim x As Action = Async Sub() Await
    End Sub
End Class
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpression() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Async Function Main() As Task
        Task.CompletedTask.$$
    End Function
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Async Function Main() As Task
        Await Task.CompletedTask
    End Function
End Class
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        Await Task.CompletedTask", "")
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpressionAndAsyncModifier() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Function Main() As Task
        Task.CompletedTask.$$
    End Function
End Class
]]>
                </Document>)
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Async Function Main() As Task
        Await Task.CompletedTask
    End Function
End Class
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        Await Task.CompletedTask", "")
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpressionAndAppendsConfigureAwait() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Threading.Tasks

Public Class C
    Public Shared Async Function Main() As Task
        Task.CompletedTask.$$
    End Function
End Class
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("Await", "Awaitf")
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)
                state.SendTypeChars("f")
                Await state.AssertSelectedCompletionItem(displayText:="Awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System.Threading.Tasks

Public Class C
    Public Shared Async Function Main() As Task
        Await Task.CompletedTask.ConfigureAwait(False)
    End Function
End Class
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        Await Task.CompletedTask.ConfigureAwait(False)", "")
            End Using
        End Function

        <WpfTheory>
        <InlineData(' static
            "StaticField.$$",
            "Await StaticField")>
        <InlineData(
            "StaticProperty.$$",
            "Await StaticProperty")>
        <InlineData(
            "StaticMethod().$$",
            "Await StaticMethod()")>
        <InlineData(' parameters, locals
            "parameter.$$",
            "Await parameter")>
        <InlineData(
            "local.$$",
            "Await local")>
        <InlineData(' members
            "c.Field.$$",
            "Await c.Field")>
        <InlineData(
            "c.Property.$$",
            "Await c.Property")>
        <InlineData(
            "c.Method().$$",
            "Await c.Method()")>
        <InlineData(
            "c.Self.Field.$$",
            "Await c.Self.Field")>
        <InlineData(
            "c.Self.Property.$$",
            "Await c.Self.Property")>
        <InlineData(
            "c.Self.Method().$$",
            "Await c.Self.Method()")>
        <InlineData(
            "c.Func()().$$",
            "Await c.Func()()")>
        <InlineData(' indexer, operator, conversion
            "c(0).$$",
            "Await c(0)")>
        <InlineData(
            "c.Self(0).$$",
            "Await c.Self(0)")>
        <InlineData(
            "Dim t = (CType(c, Task)).$$",
            "Dim t = Await (CType(c, Task))")>
        <InlineData(
            "Dim t = (TryCast(c, Task)).$$",
            "Dim t = Await (TryCast(c, Task))")>
        <InlineData(' parenthesized
            "Dim t = (parameter).$$",
            "Dim t = Await (parameter)")>
        <InlineData(
            "Dim t = ((parameter)).$$",
            "Dim t = Await ((parameter))")>
        <InlineData(
            "Dim t = if(true, parameter, parameter).$$",
            "Dim t = Await if(true, parameter, parameter)")>
        <InlineData(
            "Dim t = if(null, Task.CompletedTask).$$",
            "Dim t = Await if(null, Task.CompletedTask)")>
        Public Async Function DotAwaitCompletionAddsAwaitInFrontOfExpressionForDifferentExpressions(expression As String, committed As String) As Task
            ' place await in front of expression
            Using state = TestStateFactory.CreateVisualBasicTestState(GetTestClassDocument(containerHasAsyncModifier:=True, expression))
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using

            ' place await in front of expression and make container async
            Using state = TestStateFactory.CreateVisualBasicTestState(GetTestClassDocument(containerHasAsyncModifier:=False, expression))
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using

            ' ConfigureAwait(false) starts here
            committed += ".ConfigureAwait(False)"
            ' place await in front of expression and append ConfigureAwait(false)
            Using state = TestStateFactory.CreateVisualBasicTestState(GetTestClassDocument(containerHasAsyncModifier:=True, expression))
                state.SendTypeChars("awf")
                Await state.AssertSelectedCompletionItem(displayText:="Awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using

            ' place await in front of expression, append ConfigureAwait(false) and make container async
            Using state = TestStateFactory.CreateVisualBasicTestState(GetTestClassDocument(containerHasAsyncModifier:=False, expression))
                state.SendTypeChars("awf")
                Await state.AssertSelectedCompletionItem(displayText:="Awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal(GetTestClassDocument(containerHasAsyncModifier:=True, committed).Value.NormalizeLineEndings(), state.GetDocumentText().NormalizeLineEndings())
                Await state.AssertLineTextAroundCaret($"        {committed}", "")
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionInQueryInFirstFromClause() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim arrayTask1 = Task.FromResult(new Integer() {})
        Dim qry = From i in arrayTask1.$$
    End Function
End Class
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("Await", "Awaitf")
                state.SendTypeChars("aw")
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim arrayTask1 = Task.FromResult(new Integer() {})
        Dim qry = From i in Await arrayTask1
    End Function
End Class
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        Dim qry = From i in Await arrayTask1", "")
            End Using
        End Function

        <WpfFact>
        Public Async Function DotAwaitCompletionInQueryInFirstFromClauseConfigureAwait() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim arrayTask1 = Task.FromResult(new Integer() {})
        Dim qry = From i in arrayTask1.$$
    End Function
End Class
]]>
                </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("Await", "Awaitf")
                state.SendTypeChars("af")
                Await state.AssertSelectedCompletionItem(displayText:="Awaitf", isHardSelected:=True)

                state.SendTab()
                Assert.Equal("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim arrayTask1 = Task.FromResult(new Integer() {})
        Dim qry = From i in Await arrayTask1.ConfigureAwait(False)
    End Function
End Class
", state.GetDocumentText())
                Await state.AssertLineTextAroundCaret("        Dim qry = From i in Await arrayTask1.ConfigureAwait(False)", "")
            End Using
        End Function
    End Class
End Namespace
