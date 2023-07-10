// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class HoistedStateMachineLocalTests : ExpressionCompilerTestBase
    {
        private const string asyncLambdaSourceTemplate = @"
using System;
using System.Threading.Tasks;

public class D
{{
    private double t;

    public {0} void M(char u)
    {{
        int x = 1;
        Func<char, Task<int>> f = async ch => {1};
    }}
}}
";

        private const string genericAsyncLambdaSourceTemplate = @"
using System;
using System.Threading.Tasks;

public class D<T>
{{
    private T t;

    public {0} void M<U>(U u)
    {{
        int x = 1;
        Func<char, Task<int>> f = async ch => {1};
    }}
}}
";

        [Fact]
        public void Iterator()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> M()
    {

#line 500
        DummySequencePoint();

        {
#line 550
            int x = 0;
            yield return x;
            x++;
#line 600
        }

#line 650
        DummySequencePoint();

        {
#line 700
            int x = 0;
            yield return x;
            x++;
#line 750
        }

#line 800
        DummySequencePoint();
    }

    static void DummySequencePoint()
    {
    }
}
";

            var expectedError = "error CS0103: The name 'x' does not exist in the current context";

            var expectedIlTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.{0}""
  IL_0006:  ret
}}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                EvaluationContext context;
                CompilationTestData testData;
                string error;

                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 500);
                context.CompileExpression("x", out error);
                Assert.Equal(expectedError, error);

                testData = new CompilationTestData();
                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 550);
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(string.Format(expectedIlTemplate, "<x>5__1"));

                testData = new CompilationTestData();
                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 600);
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(string.Format(expectedIlTemplate, "<x>5__1"));

                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 650);
                context.CompileExpression("x", out error);
                Assert.Equal(expectedError, error);

                testData = new CompilationTestData();
                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 700);
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(string.Format(expectedIlTemplate, "<x>5__2"));

                testData = new CompilationTestData();
                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 750);
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(string.Format(expectedIlTemplate, "<x>5__2"));

                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 800);
                context.CompileExpression("x", out error);
                Assert.Equal(expectedError, error);
            });
        }

        [Fact]
        public void Async()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static async Task M()
    {

#line 500
        DummySequencePoint();

        {
#line 550
            int x = 0;
            await M();
            x++;
#line 600
        }

#line 650
        DummySequencePoint();

        {
#line 700
            int x = 0;
            await M();
            x++;
#line 750
        }

#line 800
        DummySequencePoint();
    }

    static void DummySequencePoint()
    {
    }
}
";

            var expectedError = "error CS0103: The name 'x' does not exist in the current context";

            var expectedIlTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<M>d__0 V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<M>d__0.{0}""
  IL_0006:  ret
}}
";

            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                EvaluationContext context;
                CompilationTestData testData;
                string error;

                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 500);
                context.CompileExpression("x", out error);
                Assert.Equal(expectedError, error);

                testData = new CompilationTestData();
                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 550);
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(string.Format(expectedIlTemplate, "<x>5__1"));

                testData = new CompilationTestData();
                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 600);
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(string.Format(expectedIlTemplate, "<x>5__1"));

                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 650);
                context.CompileExpression("x", out error);
                Assert.Equal(expectedError, error);

                testData = new CompilationTestData();
                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 700);
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(string.Format(expectedIlTemplate, "<x>5__2"));

                testData = new CompilationTestData();
                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 750);
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(string.Format(expectedIlTemplate, "<x>5__2"));

                context = CreateMethodContext(runtime, "C.<M>d__0.MoveNext", atLineNumber: 800);
                context.CompileExpression("x", out error);
                Assert.Equal(expectedError, error);
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Instance_CaptureNothing()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "/*instance*/", "1");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0027: Keyword 'this' is not available in the current context", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "ch");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Instance_CaptureLocal()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "/*instance*/", "x");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0027: Keyword 'this' is not available in the current context", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D.<>c__DisplayClass1_0 D.<>c__DisplayClass1_0.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""int D.<>c__DisplayClass1_0.x""
  IL_000b:  ret
}
");

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c__DisplayClass1_0.<<M>b__0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "ch", "x");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Instance_CaptureParameter()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "/*instance*/", "u.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0027: Keyword 'this' is not available in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("u", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D.<>c__DisplayClass1_0 D.<>c__DisplayClass1_0.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""char D.<>c__DisplayClass1_0.u""
  IL_000b:  ret
}
");

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c__DisplayClass1_0.<<M>b__0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "ch", "u");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Instance_CaptureLambdaParameter()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "/*instance*/", "ch.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0027: Keyword 'this' is not available in the current context", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "ch");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Instance_CaptureThis()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "/*instance*/", "t.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                testData = new CompilationTestData();
                context.CompileExpression("t", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D D.<<M>b__1_0>d.<>4__this""
  IL_0006:  ldfld      ""double D.t""
  IL_000b:  ret
}
");

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "this", "ch");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Instance_CaptureThisAndLocal()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "/*instance*/", "x + t.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                testData = new CompilationTestData();
                context.CompileExpression("t", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D.<>c__DisplayClass1_0 D.<>c__DisplayClass1_0.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""D D.<>c__DisplayClass1_0.<>4__this""
  IL_000b:  ldfld      ""double D.t""
  IL_0010:  ret
}
");

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D.<>c__DisplayClass1_0 D.<>c__DisplayClass1_0.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""int D.<>c__DisplayClass1_0.x""
  IL_000b:  ret
}
");

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c__DisplayClass1_0.<<M>b__0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "this", "ch", "x");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Static_CaptureNothing()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "static", "1");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0120: An object reference is required for the non-static field, method, or property 'D.t'", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "ch");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Static_CaptureLocal()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "static", "x");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0120: An object reference is required for the non-static field, method, or property 'D.t'", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D.<>c__DisplayClass1_0 D.<>c__DisplayClass1_0.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""int D.<>c__DisplayClass1_0.x""
  IL_000b:  ret
}
");

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c__DisplayClass1_0.<<M>b__0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "ch", "x");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Static_CaptureParameter()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "static", "u.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0120: An object reference is required for the non-static field, method, or property 'D.t'", error);

                testData = new CompilationTestData();
                context.CompileExpression("u", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D.<>c__DisplayClass1_0 D.<>c__DisplayClass1_0.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""char D.<>c__DisplayClass1_0.u""
  IL_000b:  ret
}
");

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c__DisplayClass1_0.<<M>b__0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "ch", "u");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void AsyncLambda_Static_CaptureLambdaParameter()
        {
            var source = string.Format(asyncLambdaSourceTemplate, "static", "ch.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0120: An object reference is required for the non-static field, method, or property 'D.t'", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D.<>c.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");
                AssertEx.SetEqual(GetLocalNames(context), "ch");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Instance_CaptureNothing()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "/*instance*/", "1");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__1.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0027: Keyword 'this' is not available in the current context", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__1<U>.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "ch", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Instance_CaptureLocal()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "/*instance*/", "x");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0027: Keyword 'this' is not available in the current context", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D<T>.<>c__DisplayClass1_0<U> D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""int D<T>.<>c__DisplayClass1_0<U>.x""
  IL_000b:  ret
}
");

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "ch", "x", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Instance_CaptureParameter()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "/*instance*/", "u.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0027: Keyword 'this' is not available in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("u", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D<T>.<>c__DisplayClass1_0<U> D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""U D<T>.<>c__DisplayClass1_0<U>.u""
  IL_000b:  ret
}
");

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "ch", "u", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Instance_CaptureLambdaParameter()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "/*instance*/", "ch.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__1.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0027: Keyword 'this' is not available in the current context", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__1<U>.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "ch", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Instance_CaptureThis()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "/*instance*/", "t.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                testData = new CompilationTestData();
                context.CompileExpression("t", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D<T> D<T>.<<M>b__1_0>d<U>.<>4__this""
  IL_0006:  ldfld      ""T D<T>.t""
  IL_000b:  ret
}
");

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<<M>b__1_0>d<U>.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "this", "ch", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Instance_CaptureThisAndLocal()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "/*instance*/", "x + t.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                testData = new CompilationTestData();
                context.CompileExpression("t", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D<T>.<>c__DisplayClass1_0<U> D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""D<T> D<T>.<>c__DisplayClass1_0<U>.<>4__this""
  IL_000b:  ldfld      ""T D<T>.t""
  IL_0010:  ret
}
");

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D<T>.<>c__DisplayClass1_0<U> D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""int D<T>.<>c__DisplayClass1_0<U>.x""
  IL_000b:  ret
}
");

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "this", "ch", "x", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Static_CaptureNothing()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "static", "1");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__1.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0120: An object reference is required for the non-static field, method, or property 'D<T>.t'", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__1<U>.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "ch", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Static_CaptureLocal()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "static", "x");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0120: An object reference is required for the non-static field, method, or property 'D<T>.t'", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("x", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D<T>.<>c__DisplayClass1_0<U> D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""int D<T>.<>c__DisplayClass1_0<U>.x""
  IL_000b:  ret
}
");

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "ch", "x", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Static_CaptureParameter()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "static", "u.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__DisplayClass1_0.<<M>b__0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0120: An object reference is required for the non-static field, method, or property 'D<T>.t'", error);

                testData = new CompilationTestData();
                context.CompileExpression("u", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D<T>.<>c__DisplayClass1_0<U> D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.<>4__this""
  IL_0006:  ldfld      ""U D<T>.<>c__DisplayClass1_0<U>.u""
  IL_000b:  ret
}
");

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__DisplayClass1_0<U>.<<M>b__0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "ch", "u", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")]
        public void GenericAsyncLambda_Static_CaptureLambdaParameter()
        {
            var source = string.Format(genericAsyncLambdaSourceTemplate, "static", "ch.GetHashCode()");
            var comp = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: GetUniqueName());
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "D.<>c__1.<<M>b__1_0>d.MoveNext");

                string error;
                CompilationTestData testData;

                context.CompileExpression("t", out error);
                Assert.Equal("error CS0120: An object reference is required for the non-static field, method, or property 'D<T>.t'", error);

                context.CompileExpression("u", out error);
                Assert.Equal("error CS0103: The name 'u' does not exist in the current context", error);

                context.CompileExpression("x", out error);
                Assert.Equal("error CS0103: The name 'x' does not exist in the current context", error);

                testData = new CompilationTestData();
                context.CompileExpression("ch", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x<T, U>.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""char D<T>.<>c__1<U>.<<M>b__1_0>d.ch""
  IL_0006:  ret
}
");

                context.CompileExpression("typeof(T)", out error);
                Assert.Null(error);
                context.CompileExpression("typeof(U)", out error);
                Assert.Null(error);

                AssertEx.SetEqual(GetLocalNames(context), "ch", "<>TypeVariables");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1134746")]
        public void CacheInvalidation()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> M()
    {
#line 100
        int x = 1;
        yield return x;

        {
#line 200
            int y = x + 1;
            yield return y;
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                ImmutableArray<MetadataBlock> blocks;
                Guid moduleVersionId;
                ISymUnmanagedReader symReader;
                int methodToken;
                int localSignatureToken;
                GetContextState(runtime, "C.<M>d__0.MoveNext", out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);

                var appDomain = new AppDomain();
                uint ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader, atLineNumber: 100);
                var context = CreateMethodContext(
                    appDomain,
                    blocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ilOffset,
                    localSignatureToken: localSignatureToken,
                    kind: MakeAssemblyReferencesKind.AllAssemblies);

                string error;
                context.CompileExpression("x", out error);
                Assert.Null(error);
                context.CompileExpression("y", out error);
                Assert.Equal("error CS0103: The name 'y' does not exist in the current context", error);

                ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader, atLineNumber: 200);
                context = CreateMethodContext(
                    appDomain,
                    blocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ilOffset,
                    localSignatureToken: localSignatureToken,
                    kind: MakeAssemblyReferencesKind.AllAssemblies);

                context.CompileExpression("x", out error);
                Assert.Null(error);
                context.CompileExpression("y", out error);
                Assert.Null(error);
            });
        }

        private static string[] GetLocalNames(EvaluationContext context)
        {
            string unused;
            var locals = new ArrayBuilder<LocalAndMethod>();
            context.CompileGetLocals(locals, argumentsOnly: false, typeName: out unused, testData: null);
            return locals.Select(l => l.LocalName).ToArray();
        }
    }
}
