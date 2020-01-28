﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class CodeGenScriptTests : CSharpTestBase
    {
        [Fact]
        public void AnonymousTypes_TopLevelVar()
        {
            string test = @"
using System;
var o = new { a = 1 };
Console.WriteLine(o.ToString());
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            CompileAndVerify(
                CreateCompilationWithMscorlib45(
                    new[] { tree },
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { SystemCoreRef }),
                expectedOutput: "{ a = 1 }"
            );
        }

        [Fact]
        public void AnonymousTypes_TopLevel_Object()
        {
            string test = @"
using System;
object o = new { a = 1 };
Console.WriteLine(o.ToString());
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            CompileAndVerify(
                CreateCompilationWithMscorlib45(
                    new[] { tree },
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { SystemCoreRef }),
                expectedOutput: "{ a = 1 }"
            );
        }

        [Fact]
        public void AnonymousTypes_TopLevel_NoLocal()
        {
            string test = @"
using System;
Console.WriteLine(new { a = 1 }.ToString());
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            CompileAndVerify(
                CreateCompilationWithMscorlib45(
                    new[] { tree },
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { SystemCoreRef }),
                expectedOutput: "{ a = 1 }"
            );
        }

        [Fact]
        public void AnonymousTypes_NestedClass_Method()
        {
            string test = @"
using System;
class CLS 
{
    public void M()
    {
        Console.WriteLine(new { a = 1 }.ToString());
    }
}

new CLS().M();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            CompileAndVerify(
                CreateCompilationWithMscorlib45(
                    new[] { tree },
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { SystemCoreRef }),
                expectedOutput: "{ a = 1 }"
            );
        }

        [Fact]
        public void AnonymousTypes_NestedClass_MethodParamDefValue()
        {
            string test = @"
using System;
class CLS 
{
    public void M(object p = new { a = 1 })
    {
        Console.WriteLine(""OK"");
    }
}
new CLS().M();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib45(
                new[] { tree },
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (5,30): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //     public void M(object p = new { a = 1 })
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new { a = 1 }").WithArguments("p"));
        }

        [Fact]
        public void AnonymousTypes_TopLevel_MethodParamDefValue()
        {
            string test = @"
using System;

public void M(object p = new { a = 1 })
{
    Console.WriteLine(""OK"");
}

M();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib45(
                new[] { tree },
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (4,26): error CS1736: Default parameter value for 'p' must be a compile-time constant
                // public void M(object p = new { a = 1 })
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new { a = 1 }").WithArguments("p"));
        }

        [Fact]
        public void AnonymousTypes_TopLevel_MethodAttribute()
        {
            string test = @"
using System;

class A: Attribute
{
    public object P;
}

[A(P = new { a = 1 })]
public void M()
{
    Console.WriteLine(""OK"");
}

M();
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib45(
                new[] { tree },
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (9,8): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(P = new { a = 1 })]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new { a = 1 }"));
        }

        [Fact]
        public void AnonymousTypes_NestedTypeAttribute()
        {
            string test = @"
using System;

class A: Attribute
{
    public object P;
}

[A(P = new { a = 1 })]
class CLS 
{
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib45(
                new[] { tree },
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (9,8): error CS0836: Cannot use anonymous type in a constant expression
                // [A(P = new { a = 1 })]
                Diagnostic(ErrorCode.ERR_AnonymousTypeNotAvailable, "new"));
        }

        [Fact]
        public void CompilationChain_AnonymousTypeTemplates()
        {
            var s0 = CreateSubmission("var x = new { a = 1 }; ");
            var sx = CreateSubmission("var y = new { b = 2 }; ", previous: s0);
            var s1 = CreateSubmission("var y = new { b = new { a = 3 } };", previous: s0);
            var s2 = CreateSubmission("x = y.b; ", previous: s1);

            s2.VerifyDiagnostics();
            s2.EmitToArray();

            Assert.True(s2.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(0, s2.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            Assert.True(s1.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(1, s1.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            Assert.True(s0.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(1, s0.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            Assert.False(sx.AnonymousTypeManager.AreTemplatesSealed);
        }

        [Fact]
        public void CompilationChain_DynamicSiteDelegates()
        {
            // TODO: references should be inherited
            MetadataReference[] references = { SystemCoreRef, CSharpRef };

            var s0 = CreateSubmission("var i = 1; dynamic d = null; d.m(ref i);", references);
            var sx = CreateSubmission("var i = 1; dynamic d = null; d.m(ref i, ref i);", references, previous: s0);
            var s1 = CreateSubmission("var i = 1; dynamic d = null; d.m(out i);", references, previous: s0);

            s1.VerifyDiagnostics();
            s1.EmitToArray();

            // no new delegates should have been created:
            Assert.True(s1.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(0, s1.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            // delegate for (ref)
            Assert.True(s0.AnonymousTypeManager.AreTemplatesSealed);
            Assert.Equal(1, s0.AnonymousTypeManager.GetAllCreatedTemplates().Length);

            Assert.False(sx.AnonymousTypeManager.AreTemplatesSealed);
        }

        [Fact]
        public void Submissions_EmitToPeStream()
        {
            var s0 = CreateSubmission("int a = 1;");
            var s11 = CreateSubmission("a + 1", previous: s0);
            var s12 = CreateSubmission("a + 2", previous: s0);

            s11.VerifyEmitDiagnostics();
            s12.VerifyEmitDiagnostics();
        }

        [Fact]
        public void CrossSubmissionGenericInterfaceImplementation_Generic()
        {
            var c0 = CreateSubmission(@"
public interface I<T>
{
    void m<TT>(T x, TT y);
}
");

            var c1 = CreateSubmission(@"
abstract public class C : I<int>
{
    public void m<TT>(int x, TT y)
    {
    }
}", previous: c0);

            c0.VerifyEmitDiagnostics();
            c1.VerifyEmitDiagnostics();
        }

        [Fact]
        public void CrossSubmissionGenericInterfaceImplementation_Explicit_GenericMethod()
        {
            var c0 = CreateSubmission(@"
public interface I<T>
{
    void m<S>(T x, S y);
}
");

            var c1 = CreateSubmission(@"
abstract public class C : I<int>
{
    void I<int>.m<S>(int x, S y)
    {
    }
}", previous: c0);

            c0.VerifyEmitDiagnostics();
            c1.VerifyEmitDiagnostics();
        }

        [Fact]
        public void CrossSubmissionGenericInterfaceImplementation_Explicit()
        {
            var c0 = CreateSubmission(@"
public interface I<T>
{
    void m(T x);
}
");

            var c1 = CreateSubmission(@"
abstract public class C : I<int>
{
    void I<int>.m(int x)
    {
    }
}", previous: c0);

            c0.VerifyEmitDiagnostics();
            c1.VerifyEmitDiagnostics();
        }

        [Fact]
        public void CrossSubmissionGenericInterfaceImplementation_Explicit_NoGenericParametersInSignature()
        {
            var c0 = CreateSubmission(@"
public interface I<T>
{
    void m(byte x);
}
");

            var c1 = CreateSubmission(@"
abstract public class C : I<int>
{
    void I<int>.m(byte x)
    {
    }
}", previous: c0);

            c0.VerifyEmitDiagnostics();
            c1.VerifyEmitDiagnostics();
        }

        [Fact]
        public void GenericInterfaceImplementation_Explicit_NoGenericParametersInSignature()
        {
            var c0 = CreateSubmission(@"
public interface I<T>
{
    void m(byte x);
}
abstract public class C : I<int>
{
    void I<int>.m(byte x)
    {
    }
}");
            c0.VerifyEmitDiagnostics();
        }

        [Fact]
        public void CrossSubmissionInterfaceImplementation_Explicit_NoGenericParametersInSignature()
        {
            var c0 = CreateSubmission(@"
public interface I
{
    void m(byte x);
}
");

            var c1 = CreateSubmission(@"
abstract public class C : I
{
    void I.m(byte x)
    {
    }
}", previous: c0);

            c0.VerifyEmitDiagnostics();
            c1.VerifyEmitDiagnostics();
        }

        [Fact]
        public void CrossSubmissionNestedGenericInterfaceImplementation_Explicit()
        {
            var c0 = CreateSubmission(@"
class C<T>
{
    public interface I
    {
        void m(T x);
    }
}
");

            var c1 = CreateSubmission(@"
abstract public class D : C<int>.I
{
    void C<int>.I.m(int x)
    {
    }
}", previous: c0);

            c0.VerifyEmitDiagnostics();
            c1.VerifyEmitDiagnostics();
        }

        [Fact]
        public void NestedGenericInterfaceImplementation_Explicit()
        {
            var c0 = CreateSubmission(@"
class C<T>
{
    public interface I
    {
        void m(T x);
    }
}
abstract public class D : C<int>.I
{
    void C<int>.I.m(int x)
    {
    }
}");
            c0.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ExternalInterfaceImplementation_Explicit()
        {
            var c0 = CreateSubmission(@"
using System.Collections;
using System.Collections.Generic;

abstract public class C : IEnumerable<int>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        return null;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return null;
    }
}");
            c0.VerifyEmitDiagnostics();
        }

        [Fact]
        public void AbstractAccessors()
        {
            var c0 = CreateSubmission(@"
public abstract class C
{
    public abstract event System.Action vEv;
    public abstract int prop { get; set; }
}
");
            c0.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ExprStmtWithMethodCall()
        {
            var s0 = CreateSubmission("int Goo() { return 2;}");
            var s1 = CreateSubmission("(4 + 5) * Goo()", previous: s0);

            s0.VerifyEmitDiagnostics();
            s1.VerifyEmitDiagnostics();
        }

        /// <summary>
        /// The script entry point should complete synchronously.
        /// </summary>
        [WorkItem(4495, "https://github.com/dotnet/roslyn/issues/4495")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void ScriptEntryPoint()
        {
            var source =
@"{
    await System.Threading.Tasks.Task.Delay(100);
    System.Console.Write(""complete"");
}";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(compilation, expectedOutput: @"complete");
            var methodData = verifier.TestData.GetMethodData("<Initialize>");
            Assert.Equal("System.Threading.Tasks.Task<object>", ((MethodSymbol)methodData.Method).ReturnType.ToDisplayString());
            methodData.VerifyIL(
@"{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (<<Initialize>>d__0 V_0)
  IL_0000:  newobj     ""<<Initialize>>d__0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""Script <<Initialize>>d__0.<>4__this""
  IL_000d:  ldloc.0
  IL_000e:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Create()""
  IL_0013:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int <<Initialize>>d__0.<>1__state""
  IL_001f:  ldloc.0
  IL_0020:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<<<Initialize>>d__0>(ref <<Initialize>>d__0)""
  IL_002c:  nop
  IL_002d:  ldloc.0
  IL_002e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_0033:  call       ""System.Threading.Tasks.Task<object> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Task.get""
  IL_0038:  ret
}");
            methodData = verifier.TestData.GetMethodData("<Main>");
            Assert.True(((MethodSymbol)methodData.Method).ReturnsVoid);
            methodData.VerifyIL(
@"{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (System.Runtime.CompilerServices.TaskAwaiter<object> V_0)
  IL_0000:  newobj     "".ctor()""
  IL_0005:  callvirt   ""System.Threading.Tasks.Task<object> <Initialize>()""
  IL_000a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<object> System.Threading.Tasks.Task<object>.GetAwaiter()""
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""object System.Runtime.CompilerServices.TaskAwaiter<object>.GetResult()""
  IL_0017:  pop
  IL_0018:  ret
}");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void SubmissionEntryPoint()
        {
            var references = new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef };
            var source0 =
@"{
    await System.Threading.Tasks.Task.Delay(100);
    System.Console.Write(""complete"");
}";
            var s0 = CSharpCompilation.CreateScriptCompilation(
                "s0.dll",
                SyntaxFactory.ParseSyntaxTree(source0, options: TestOptions.Script),
                references);
            var verifier = CompileAndVerify(s0, verify: Verification.Fails);
            var methodData = verifier.TestData.GetMethodData("<Initialize>");
            Assert.Equal("System.Threading.Tasks.Task<object>", ((MethodSymbol)methodData.Method).ReturnType.ToDisplayString());
            methodData.VerifyIL(
@"{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (<<Initialize>>d__0 V_0)
  IL_0000:  newobj     ""<<Initialize>>d__0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""Script <<Initialize>>d__0.<>4__this""
  IL_000d:  ldloc.0
  IL_000e:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Create()""
  IL_0013:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int <<Initialize>>d__0.<>1__state""
  IL_001f:  ldloc.0
  IL_0020:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<<<Initialize>>d__0>(ref <<Initialize>>d__0)""
  IL_002c:  nop
  IL_002d:  ldloc.0
  IL_002e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object> <<Initialize>>d__0.<>t__builder""
  IL_0033:  call       ""System.Threading.Tasks.Task<object> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Task.get""
  IL_0038:  ret
}");
            methodData = verifier.TestData.GetMethodData("<Factory>");
            Assert.Equal("System.Threading.Tasks.Task<object>", ((MethodSymbol)methodData.Method).ReturnType.ToDisplayString());
            methodData.VerifyIL(
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     "".ctor(object[])""
  IL_0006:  callvirt   ""System.Threading.Tasks.Task<object> <Initialize>()""
  IL_000b:  ret
}");
        }

        [Fact]
        public void ScriptEntryPoint_MissingMethods()
        {
            var source = "System.Console.WriteLine(1);";
            var compilation = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            compilation.VerifyEmitDiagnostics(
                // error CS1061: 'Task<object>' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'Task<object>' could be found (are you missing a using directive or an assembly reference?)
                //
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "").WithArguments("System.Threading.Tasks.Task<object>", "GetAwaiter").WithLocation(1, 1),
                // (1,1): error CS0518: Predefined type 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1' is not defined or imported
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "System.Console.WriteLine(1);").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1").WithLocation(1, 1),
                // (1,1): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.Create'
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "System.Console.WriteLine(1);").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1", "Create").WithLocation(1, 1),
                // (1,1): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.Task'
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "System.Console.WriteLine(1);").WithArguments("System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1", "Task").WithLocation(1, 1),
                // (1,1): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext'
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "System.Console.WriteLine(1);").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "MoveNext").WithLocation(1, 1),
                // (1,1): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine'
                // System.Console.WriteLine(1);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "System.Console.WriteLine(1);").WithArguments("System.Runtime.CompilerServices.IAsyncStateMachine", "SetStateMachine").WithLocation(1, 1));
        }

        [Fact]
        public void ExplicitImplementation()
        {
            string test = @"
interface I1
{
    void M();
}

void I1.M() {}
";
            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script);

            var compilation = CreateCompilationWithMscorlib45(
                new[] { tree },
                options: TestOptions.ReleaseExe.WithScriptClassName("Script"));

            compilation.VerifyDiagnostics(
                // (7,6): error CS0540: 'I1.M()': containing type does not implement interface 'I1'
                // void I1.M() {}
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "I1").WithArguments("I1.M()", "I1").WithLocation(7, 6)
                );

            var s = CreateSubmission(test);

            s.VerifyDiagnostics(
                // (7,9): error CS0541: 'M()': explicit interface declaration can only be declared in a class, struct or interface
                // void I1.M() {}
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, "M").WithArguments("M()").WithLocation(7, 9)
                );
        }
    }
}
