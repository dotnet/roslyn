' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Scripting
Imports Roslyn.Test.Utilities
Imports Xunit

#Disable Warning RS0003 ' Do not directly await a Task

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.UnitTests

    Public Class ScriptTests
        Inherits TestBase

        ' It shouldn't be necessary to include VB runtime assembly
        ' explicitly in VisualBasicScript.Create.
        Private Shared ReadOnly DefaultOptions As ScriptOptions = ScriptOptions.Default.AddReferences(MsvbRef)

        <Fact>
        Public Sub TestCreateScript()
            Dim script = VisualBasicScript.Create("? 1 + 2")
            Assert.Equal("? 1 + 2", script.Code)
        End Sub

        <Fact>
        Public Sub TestEvalScript()
            Dim value = VisualBasicScript.EvaluateAsync("? 1 + 2", DefaultOptions)
            Assert.Equal(3, value.Result)
        End Sub

        <Fact>
        Public Async Function TestRunScript() As Task
            Dim state = Await VisualBasicScript.RunAsync("? 1 + 2", DefaultOptions)
            Assert.Equal(3, state.ReturnValue)
        End Function

        <Fact>
        Public Async Function TestCreateAndRunScript() As Task
            Dim script = VisualBasicScript.Create("? 1 + 2", DefaultOptions)
            Dim state = Await script.RunAsync()
            Assert.Same(script, state.Script)
            Assert.Equal(3, state.ReturnValue)
        End Function

        <Fact>
        Public Async Function TestRunScriptWithSpecifiedReturnType() As Task
            Dim state = Await VisualBasicScript.RunAsync("? 1 + 2", DefaultOptions)
            Assert.Equal(3, state.ReturnValue)
        End Function

        <Fact>
        Public Sub TestGetCompilation()
            Dim script = VisualBasicScript.Create("? 1 + 2")
            Dim compilation = script.GetCompilation()
            Assert.Equal(script.Code, compilation.SyntaxTrees.First().GetText().ToString())
        End Sub

        <Fact>
        Public Async Function TestRunVoidScript() As Task
            Dim state = Await VisualBasicScript.RunAsync("System.Console.WriteLine(0)", DefaultOptions)
            Assert.Null(state.ReturnValue)
        End Function

        <Fact>
        Public Sub TestDefaultNamespaces()
            ' If this ever changes, it is important to ensure that the 
            ' IDE is also updated with the same default namespaces.
            Assert.Empty(ScriptOptions.Default.Imports)
        End Sub

        ' TODO: port C# tests
    End Class

End Namespace
