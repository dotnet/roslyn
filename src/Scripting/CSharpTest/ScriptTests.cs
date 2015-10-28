// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

#pragma warning disable RS0003 // Do not directly await a Task

namespace Microsoft.CodeAnalysis.Scripting.CSharp.UnitTests
{
    public class ScriptTests : TestBase
    {
        public class Globals
        {
            public int X;
            public int Y;
        }

        [Fact]
        public void TestCreateScript()
        {
            var script = CSharpScript.Create("1 + 2");
            Assert.Equal("1 + 2", script.Code);
        }

        [Fact]
        public async Task TestGetCompilation()
        {
            var state = await CSharpScript.RunAsync("1 + 2", globals: new ScriptTests());
            var compilation = state.Script.GetCompilation();
            Assert.Equal(state.Script.Code, compilation.SyntaxTrees.First().GetText().ToString());
        }

        [Fact]
        public void TestCreateScriptDelegate()
        {
            // create a delegate for the entire script
            var script = CSharpScript.Create("1 + 2");
            var fn = script.CreateDelegate();

            Assert.Equal(3, fn().Result);
            AssertEx.ThrowsArgumentException("globals", () => fn(new object()));
        }

        [Fact]
        public void TestCreateScriptDelegateWithGlobals()
        {
            // create a delegate for the entire script
            var script = CSharpScript.Create<int>("X + Y", globalsType: typeof(Globals));
            var fn = script.CreateDelegate();

            AssertEx.ThrowsArgumentException("globals", () => fn());
            AssertEx.ThrowsArgumentException("globals", () => fn(new object()));
            Assert.Equal(4, fn(new Globals { X = 1, Y = 3 }).Result);
        }

        [Fact]
        public async Task TestRunScript()
        {
            var state = await CSharpScript.RunAsync("1 + 2");
            Assert.Equal(3, state.ReturnValue);
        }

        [Fact]
        public async Task TestCreateAndRunScript()
        {
            var script = CSharpScript.Create("1 + 2");
            var state = await script.RunAsync();
            Assert.Same(script, state.Script);
            Assert.Equal(3, state.ReturnValue);
        }

        [Fact]
        public async Task TestEvalScript()
        {
            var value = await CSharpScript.EvaluateAsync("1 + 2");
            Assert.Equal(3, value);
        }

        [Fact]
        public async Task TestRunScriptWithSpecifiedReturnType()
        {
            var state = await CSharpScript.RunAsync("1 + 2");
            Assert.Equal(3, state.ReturnValue);
        }

        [Fact]
        public async Task TestRunVoidScript()
        {
            var state = await CSharpScript.RunAsync("System.Console.WriteLine(0);");
            Assert.Null(state.ReturnValue);
        }

        [WorkItem(5279, "https://github.com/dotnet/roslyn/issues/5279")]
        [Fact]
        public async void TestRunExpressionStatement()
        {
            var state = await CSharpScript.RunAsync(
@"int F() { return 1; }
F();");
            Assert.Null(state.ReturnValue);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/170")]
        public void TestRunDynamicVoidScriptWithTerminatingSemicolon()
        {
            var result = CSharpScript.RunAsync(@"
class SomeClass
{
    public void Do()
    {
    }
}
dynamic d = new SomeClass();
d.Do();"
, ScriptOptions.Default.WithReferences(MscorlibRef, SystemRef, SystemCoreRef, CSharpRef));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/170")]
        public void TestRunDynamicVoidScriptWithoutTerminatingSemicolon()
        {
            var result = CSharpScript.RunAsync(@"
class SomeClass
{
    public void Do()
    {
    }
}
dynamic d = new SomeClass();
d.Do()"
, ScriptOptions.Default.WithReferences(MscorlibRef, SystemRef, SystemCoreRef, CSharpRef));
        }

        [Fact]
        public async Task TestRunScriptWithGlobals()
        {
            var state = await CSharpScript.RunAsync("X + Y", globals: new Globals { X = 1, Y = 2 });
            Assert.Equal(3, state.ReturnValue);
        }

        [Fact]
        public async Task TestRunCreatedScriptWithExpectedGlobals()
        {
            var script = CSharpScript.Create("X + Y", globalsType: typeof(Globals));
            var state = await script.RunAsync(new Globals { X = 1, Y = 2 });
            Assert.Equal(3, state.ReturnValue);
            Assert.Same(script, state.Script);
        }

        [Fact]
        public void TestRunCreatedScriptWithUnexpectedGlobals()
        {
            var script = CSharpScript.Create("X + Y");

            // Global variables passed to a script without a global type
            AssertEx.ThrowsArgumentException("globals", () => script.RunAsync(new Globals { X = 1, Y = 2 }));
        }

        [Fact]
        public void TestRunCreatedScriptWithoutGlobals()
        {
            var script = CSharpScript.Create("X + Y", globalsType: typeof(Globals));
           
            //  The script requires access to global variables but none were given
            AssertEx.ThrowsArgumentException("globals", () => script.RunAsync());
        }

        [Fact]
        public void TestRunCreatedScriptWithMismatchedGlobals()
        {
            var script = CSharpScript.Create("X + Y", globalsType: typeof(Globals));

            //  The globals of type 'System.Object' is not assignable to 'Microsoft.CodeAnalysis.Scripting.CSharp.Test.ScriptTests+Globals'
            AssertEx.ThrowsArgumentException("globals", () => script.RunAsync(new object()));
        }

        [Fact]
        public async Task ContinueAsync_Error1()
        {
            var state = await CSharpScript.RunAsync("X + Y", globals: new Globals());

            AssertEx.ThrowsArgumentNull("previousState", () => state.Script.ContinueAsync(null));
        }

        [Fact]
        public async Task ContinueAsync_Error2()
        {
            var state1 = await CSharpScript.RunAsync("X + Y + 1", globals: new Globals());
            var state2 = await CSharpScript.RunAsync("X + Y + 2", globals: new Globals());

            AssertEx.ThrowsArgumentException("previousState", () => state1.Script.ContinueAsync(state2));
        }

        [Fact]
        public async Task TestRunScriptWithScriptState()
        {
            // run a script using another scripts end state as the starting state (globals)
            var state = await CSharpScript.RunAsync("int X = 100;").ContinueWith("X + X");
            Assert.Equal(200, state.ReturnValue);
        }

        [Fact]
        public async Task TestRepl()
        {
            string[] submissions = new[]
            {
                "int x = 100;",
                "int y = x * x;",
                "x + y"
            };

            var state = await CSharpScript.RunAsync("");
            foreach (var submission in submissions)
            {
                state = await state.ContinueWithAsync(submission);
            }

            Assert.Equal(10100, state.ReturnValue);
        }

#if TODO // https://github.com/dotnet/roslyn/issues/3720
        [Fact]
        public void TestCreateMethodDelegate()
        {
            // create a delegate to a method declared in the script
            var state = CSharpScript.Run("int Times(int x) { return x * x; }");
            var fn = state.CreateDelegate<Func<int, int>>("Times");
            var result = fn(5);
            Assert.Equal(25, result);
        }
#endif

        [Fact]
        public async Task ScriptVariables_Chain()
        {
            var globals = new Globals { X = 10, Y = 20 };

            var script = 
                CSharpScript.Create(
                    "var a = '1';",
                    globalsType: globals.GetType()).
                ContinueWith("var b = 2u;").
                ContinueWith("var a = 3m;").
                ContinueWith("var x = a + b;").
                ContinueWith("var X = Y;");

            var state = await script.RunAsync(globals);

            AssertEx.Equal(new[] { "a", "b", "a", "x", "X" }, state.Variables.Select(v => v.Name));
            AssertEx.Equal(new object[] { '1', 2u, 3m, 5m, 20 }, state.Variables.Select(v => v.Value));
            AssertEx.Equal(new Type[] { typeof(char), typeof(uint), typeof(decimal), typeof(decimal), typeof(int) }, state.Variables.Select(v => v.Type));

            Assert.Equal(3m, state.GetVariable("a").Value);
            Assert.Equal(2u, state.GetVariable("b").Value);
            Assert.Equal(5m, state.GetVariable("x").Value);
            Assert.Equal(20, state.GetVariable("X").Value);

            Assert.Equal(null, state.GetVariable("A"));
            Assert.Same(state.GetVariable("X"), state.GetVariable("X"));
        }

        [Fact]
        public async Task ScriptVariable_SetValue()
        {
            var script = CSharpScript.Create("var x = 1;");

            var s1 = await script.RunAsync();
            s1.GetVariable("x").Value = 2;
            Assert.Equal(2, s1.GetVariable("x").Value);

            // rerunning the script from the beginning rebuilds the state:
            var s2 = await s1.Script.RunAsync();
            Assert.Equal(1, s2.GetVariable("x").Value);

            // continuing preserves the state:
            var s3 = await s1.ContinueWithAsync("x");
            Assert.Equal(2, s3.GetVariable("x").Value);
            Assert.Equal(2, s3.ReturnValue);
        }

        [Fact]
        public async Task ScriptVariable_SetValue_Errors()
        {
            var state = await CSharpScript.RunAsync(@"
var x = 1;
readonly var y = 2;
const int z = 3;
");

            Assert.False(state.GetVariable("x").IsReadOnly);
            Assert.True(state.GetVariable("y").IsReadOnly);
            Assert.True(state.GetVariable("z").IsReadOnly);

            Assert.Throws<ArgumentException>(() => state.GetVariable("x").Value = "str");
            Assert.Throws<InvalidOperationException>(() => state.GetVariable("y").Value = "str");
            Assert.Throws<InvalidOperationException>(() => state.GetVariable("z").Value = "str");
            Assert.Throws<InvalidOperationException>(() => state.GetVariable("y").Value = 0);
            Assert.Throws<InvalidOperationException>(() => state.GetVariable("z").Value = 0);
        }

        [Fact]
        public async Task TestBranchingSubscripts()
        {
            // run script to create declaration of M
            var state1 = await CSharpScript.RunAsync("int M(int x) { return x + x; }");

            // run second script starting from first script's end state
            // this script's new declaration should hide the old declaration
            var state2 = await state1.ContinueWithAsync("int M(int x) { return x * x; } M(5)");
            Assert.Equal(25, state2.ReturnValue);

            // run third script also starting from first script's end state
            // it should not see any declarations made by the second script.
            var state3 = await state1.ContinueWithAsync("M(5)");
            Assert.Equal(10, state3.ReturnValue);
        }
    }
}
