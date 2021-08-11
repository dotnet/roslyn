' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class VisualBasicCompletionCommandHandlerTests_Await
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
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

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
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

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
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

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
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

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
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

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
                Await state.AssertSelectedCompletionItem(displayText:="Await", isHardSelected:=True, inlineDescription:=FeaturesResources.Make_containing_scope_async)

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
    End Class
End Namespace
