﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Tests that should take a long time, perhaps even exceeding
    // test timeout, without shortcuts in overload resolution.
    public class OverloadResolutionPerfTests : CSharpTestBase
    {
        [WorkItem(13685, "https://github.com/dotnet/roslyn/issues/13685")]
        [Fact]
        public void Overloads()
        {
            const int n = 3000;
            var builder = new StringBuilder();
            builder.AppendLine("class C");
            builder.AppendLine("{");
            builder.AppendLine($"    static void F() {{ F(null); }}"); // matches n overloads: F(C0), F(C1), ...
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    static void F(C{i} c) {{ }}");
            }
            builder.AppendLine("}");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            comp.VerifyDiagnostics(
                // (3,23): error CS0121: The call is ambiguous between the following methods or properties: 'C.F(C0)' and 'C.F(C1)'
                //     static void F() { F(null); }
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("C.F(C0)", "C.F(C1)").WithLocation(3, 23));
        }

        [WorkItem(13685, "https://github.com/dotnet/roslyn/issues/13685")]
        [Fact]
        public void BinaryOperatorOverloads()
        {
            const int n = 3000;
            var builder = new StringBuilder();
            builder.AppendLine("class C");
            builder.AppendLine("{");
            builder.AppendLine($"    static object F(C x) => x + null;"); // matches n overloads: +(C, C0), +(C, C1), ...
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    public static object operator+(C x, C{i} y) => null;");
            }
            builder.AppendLine("}");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            comp.VerifyDiagnostics(
                // (3,29): error CS0034: Operator '+' is ambiguous on operands of type 'C' and '<null>'
                //     static object F(C x) => x + null;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x + null").WithArguments("+", "C", "<null>").WithLocation(3, 29));
        }

        [Fact]
        public void StaticMethodsWithLambda()
        {
            const int n = 100;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("static class S");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    internal static void F(C{i} x, Action<C{i}> a) {{ F(x, y => F(y, z => F(z, w => {{ }}))); }}");
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ConstructorsWithLambdaAndParams()
        {
            const int n = 100;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("class C");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    internal static C F(C{i} x, params object[] args) => new C(x, y => F(y, args[1]), args[0]);");
                builder.AppendLine($"    internal C(C{i} x, Func<C{i}, C> f, params object[] args) {{ }}");
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionMethodsWithLambda()
        {
            const int n = 100;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("static class S");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    internal static void F(this C{i} x, Action<C{i}> a) {{ x.F(y => y.F(z => z.F(w => {{ }}))); }}");
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionMethodsWithLambdaAndParams()
        {
            const int n = 100;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("static class S");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    internal static void F(this C{i} x, Action<C{i}> a, params object[] args) {{ x.F(y => y.F(z => z.F(w => {{ }}), args[1]), args[0]); }}");
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionMethodsWithLambdaAndErrors()
        {
            const int n = 200;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("static class S");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                if (i % 2 == 0)
                {
                    builder.AppendLine($"    internal static void F(this C{i} x) {{ x.G(y => y.G(z => z.F())); }}"); // No match for x.G(...).
                }
                else
                {
                    builder.AppendLine($"    internal static void G(this C{i} x, Action<C{i}> a) {{ }}");
                }
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            // error CS1929: 'Ci' does not contain a definition for 'G' and the best extension method overload 'S.G(C1, Action<C1>)' requires a receiver of type 'C1'
            var diagnostics = Enumerable.Range(0, n / 2).
                Select(i => Diagnostic(ErrorCode.ERR_BadInstanceArgType, "x").WithArguments($"C{i * 2}", "G", "S.G(C1, System.Action<C1>)", "C1")).
                ToArray();
            comp.VerifyDiagnostics(diagnostics);
        }
    }
}
