// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.CSharp;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.CSharp.Test
{
    public class ScriptTests : CSharpTestBase
    {
        [Fact]
        public void TestCreateScript()
        {
            var script = CSharpScript.Create<int>("1 + 2");
            Assert.Equal("1 + 2", script.Code);
        }

        [Fact]
        public void TestGetCompilation()
        {
            var script = CSharpScript.Create<int>("1 + 2");
            var compilation = script.GetCompilation();
            Assert.Equal(script.Code, compilation.SyntaxTrees.First().GetText().ToString());
        }

        [Fact]
        public void TestCreateScriptDelegate()
        {
            // create a delegate for the entire script
            var script = CSharpScript.Create<int>("1 + 2");
            var fn = script.CreateDelegate();
            var value = fn();
            Assert.Equal(3, value.Result);
        }

        [Fact]
        public void TestRunScript()
        {
            var result = CSharpScript.Run<object>("1 + 2");
            Assert.Equal(3, result.ReturnValue.Result);
        }

        [Fact]
        public void TestCreateAndRunScript()
        {
            var script = CSharpScript.Create<int>("1 + 2");
            var result = script.Run();
            Assert.Same(script, result.Script);
            Assert.Equal(3, result.ReturnValue.Result);
        }

        [Fact]
        public void TestEvalScript()
        {
            var value = CSharpScript.Eval<int>("1 + 2");
            Assert.Equal(3, value.Result);
        }

        [Fact]
        public void TestRunScriptWithSpecifiedReturnType()
        {
            var result = CSharpScript.Run<int>("1 + 2");
            Assert.Equal(3, result.ReturnValue.Result);
        }

        [Fact]
        public void TestRunVoidScript()
        {
            var result = CSharpScript.Run<object>("Console.WriteLine(0);");
        }

        [Fact(Skip = "Bug 170")]
        public void TestRunDynamicVoidScriptWithTerminatingSemicolon()
        {
            var result = CSharpScript.Run<object>(@"
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

        [Fact(Skip = "Bug 170")]
        public void TestRunDynamicVoidScriptWithoutTerminatingSemicolon()
        {
            var result = CSharpScript.Run<object>(@"
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

        public class Globals
        {
            public int X;
            public int Y;
        }

        [Fact]
        public void TestRunScriptWithGlobals()
        {
            var result = CSharpScript.Run<int>("X + Y", new Globals { X = 1, Y = 2 });
            Assert.Equal(3, result.ReturnValue.Result);
        }

        [Fact]
        public void TestRunCreatedScriptWithExpectedGlobals()
        {
            var script = (Script<int>)CSharpScript.Create<int>("X + Y").WithGlobalsType(typeof(Globals));
            var result = script.Run(new Globals { X = 1, Y = 2 });
            Assert.Equal(3, result.ReturnValue.Result);
            Assert.Same(script, result.Script);
        }

        [Fact]
        public void TestRunCreatedScriptWithUnexpectedGlobals()
        {
            var script = CSharpScript.Create<int>("X + Y");
            var result = script.Run(new Globals { X = 1, Y = 2 });
            Assert.Equal(3, result.ReturnValue.Result);

            // the end state of running the script should be based on a different script instance because of the globals
            // not matching the original script definition.
            Assert.NotSame(script, result.Script);
        }

        [Fact]
        public void TestRunScriptWithScriptState()
        {
            // run a script using another scripts end state as the starting state (globals)
            var result = CSharpScript.Run<int>("int X = 100;");
            var result2 = CSharpScript.Run<int>("X + X", result);
            Assert.Equal(200, result2.ReturnValue.Result);
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
            ScriptState<int> result = null;
            foreach (var submission in submissions)
            {
                result = CSharpScript.Run<int>(submission, input);
                input = result;
            }

            Assert.Equal(10100, result.ReturnValue.Result);
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
        public void TestGetScriptVariableAfterRunningScript()
        {
            var result = CSharpScript.Run<object>("int x = 100;");
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
            var result1 = CSharpScript.Run<int>("int M(int x) { return x + x; }");

            // run second script starting from first script's end state
            // this script's new declaration should hide the old declaration
            var result2 = CSharpScript.Run<int>("int M(int x) { return x * x; } M(5)", result1);
            Assert.Equal(25, result2.ReturnValue.Result);

            // run third script also starting from first script's end state
            // it should not see any declarations made by the second script.
            var result3 = CSharpScript.Run<int>("M(5)", result1);
            Assert.Equal(10, result3.ReturnValue.Result);
        }
    }
}
