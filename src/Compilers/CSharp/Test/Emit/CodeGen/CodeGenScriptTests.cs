// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                CSharpCompilation.Create(
                    assemblyName: "Test",
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef },
                    syntaxTrees: new[] { tree }),
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
                CSharpCompilation.Create(
                    assemblyName: "Test",
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef },
                    syntaxTrees: new[] { tree }),
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
                CSharpCompilation.Create(
                    assemblyName: "Test",
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef },
                    syntaxTrees: new[] { tree }),
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
                CSharpCompilation.Create(
                    assemblyName: "Test",
                    options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                    references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef },
                    syntaxTrees: new[] { tree }),
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

            var compilation = CSharpCompilation.Create(
                                assemblyName: "Test",
                                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                references: new[] { MscorlibRef },
                                syntaxTrees: new[] { tree });

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

            var compilation = CSharpCompilation.Create(
                                assemblyName: "Test",
                                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                references: new[] { MscorlibRef },
                                syntaxTrees: new[] { tree });

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

            var compilation = CSharpCompilation.Create(
                                assemblyName: "Test",
                                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                references: new[] { MscorlibRef },
                                syntaxTrees: new[] { tree });

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

            var compilation = CSharpCompilation.Create(
                                assemblyName: "Test",
                                options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                references: new[] { MscorlibRef },
                                syntaxTrees: new[] { tree });

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
            var s0 = CreateSubmission("int Foo() { return 2;}");
            var s1 = CreateSubmission("(4 + 5) * Foo()", previous: s0);

            s0.VerifyEmitDiagnostics();
            s1.VerifyEmitDiagnostics();
        }
    }
}
