' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Scripting.TestUtilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.UnitTests

    Public Class ScriptTests
        Inherits VisualBasicScriptTestBase

        ''' <summary>
        ''' Need to create a <see cref="PortableExecutableReference"/> without a file path here. Scripting
        ''' will attempt to validate file paths and one does not exist for this reference as it's an in
        ''' memory item.
        ''' </summary>
        Private Shared ReadOnly s_msvbReference As PortableExecutableReference = AssemblyMetadata.CreateFromImage(Net461.Resources.MicrosoftVisualBasic).GetReference()

        ' It shouldn't be necessary to include VB runtime assembly
        ' explicitly in VisualBasicScript.Create.
        Private ReadOnly DefaultOptions As ScriptOptions = ScriptOptions.AddReferences(s_msvbReference)

        <Fact>
        Public Sub TestCreateScript()
            Dim script = VisualBasicScript.Create("? 1 + 2", ScriptOptions)
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
            Dim script = VisualBasicScript.Create("? 1 + 2", ScriptOptions)
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
