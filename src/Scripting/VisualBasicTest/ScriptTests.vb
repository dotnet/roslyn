' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Xunit

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic.UnitTests

    Public Class ScriptTests
        Inherits BasicTestBase

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
        Public Sub TestRunScript()
            Dim result = VisualBasicScript.RunAsync("? 1 + 2", DefaultOptions)
            Assert.Equal(3, result.ReturnValue.Result)
        End Sub

        <Fact>
        Public Sub TestCreateAndRunScript()
            Dim script = VisualBasicScript.Create("? 1 + 2", DefaultOptions)
            Dim result = script.RunAsync()
            Assert.Same(script, result.Script)
            Assert.Equal(3, result.ReturnValue.Result)
        End Sub

        <Fact>
        Public Sub TestRunScriptWithSpecifiedReturnType()
            Dim result = VisualBasicScript.RunAsync("? 1 + 2", DefaultOptions)
            Assert.Equal(3, result.ReturnValue.Result)
        End Sub

        <Fact>
        Public Sub TestGetCompilation()
            Dim script = VisualBasicScript.Create("? 1 + 2")
            Dim compilation = script.GetCompilation()
            Assert.Equal(script.Code, compilation.SyntaxTrees.First().GetText().ToString())
        End Sub

        <Fact>
        Public Sub TestCreateScriptDelegate()
            '' create a delegate for the entire script
            Dim script = VisualBasicScript.Create("? 1 + 2", DefaultOptions)
            Dim fn = script.CreateDelegate()
            Dim value = fn()
            Assert.Equal(3, value.Result)
        End Sub

        <Fact>
        Public Sub TestRunVoidScript()
            Dim result = VisualBasicScript.RunAsync("Console.WriteLine(0)", DefaultOptions)
            Dim task = result.ReturnValue
            Assert.Null(task.Result)
        End Sub

        Public Class Globals
            Public X As Integer
            Public Y As Integer
        End Class

#If False Then

        <Fact>
        Public Sub TestRunScriptWithGlobals()
            Dim g = New Globals With {.X = 1, .Y = 2}
            Dim result = VisualBasicScript.RunAsync("? X + Y", g)
            Assert.Equal(3, result.ReturnValue)
        End Sub


        [Fact]
        public void TestRunCreatedScriptWithExpectedGlobals()
        {
            var script = CSharpScript.Create("X + Y").WithGlobalsType(typeof(Globals));
            var result = script.RunAsync(new Globals { X = 1, Y = 2 });
            Assert.Equal(3, result.ReturnValue);
            Assert.Same(script, result.Script);
        }

        [Fact]
        public void TestRunCreatedScriptWithUnexpectedGlobals()
        {
            var script = CSharpScript.Create("X + Y");
            var result = script.RunAsync(new Globals { X = 1, Y = 2 });
            Assert.Equal(3, result.ReturnValue);

            // the end state of running the script should be based on a different script instance because of the globals
            // not matching the original script definition.
            Assert.NotSame(script, result.Script);
        }

        [Fact]
        public void TestRunScriptWithScriptState()
        {
            // run a script using another scripts end state as the starting state (globals)
            var result = CSharpScript.Run("int X = 100;");
            var result2 = CSharpScript.Run("X + X", result);
            Assert.Equal(200, result2.ReturnValue);
        }

        [Fact]
        public void TestRepl()
        {
            string[] submissions = new[]
            {
                "int x = 100;",
                "int y = x * x;",
                "x + y"
            };

            object input = null;
            ScriptState result = null;
            foreach (var submission in submissions)
            {
                result = CSharpScript.Run(submission, input);
                input = result;
            }

            Assert.Equal(10100, result.ReturnValue);
        }

        [Fact]
        public void TestCreateMethodDelegate()
        {
            // create a delegate to a method declared in the script
            var state = CSharpScript.Run("int Times(int x) { return x * x; }");
            var fn = state.CreateDelegate<Func<int, int>>("Times");
            var result = fn(5);
            Assert.Equal(25, result);
        }

        [Fact]
        public void TestGetScriptVariableAfterRunningScript()
        {
            var result = CSharpScript.Run("int x = 100;");
            var globals = result.Variables.Names.ToList();
            Assert.Equal(1, globals.Count);
            Assert.Equal(true, globals.Contains("x"));
            Assert.Equal(true, result.Variables.ContainsVariable("x"));
            Assert.Equal(100, (int)result.Variables["x"].Value);
        }

        [Fact]
        public void TestBranchingSubscripts()
        {
            // run script to create declaration of M
            var result1 = CSharpScript.Run("int M(int x) { return x + x; }");

            // run second script starting from first script's end state
            // this script's new declaration should hide the old declaration
            var result2 = CSharpScript.Run("int M(int x) { return x * x; } M(5)", result1);
            Assert.Equal(25, result2.ReturnValue);

            // run third script also starting from first script's end state
            // it should not see any declarations made by the second script.
            var result3 = CSharpScript.Run("M(5)", result1);
            Assert.Equal(10, result3.ReturnValue);
        }
#End If
    End Class
End Namespace
