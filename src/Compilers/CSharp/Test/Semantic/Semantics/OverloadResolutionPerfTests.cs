// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Tests that should take a long time, perhaps even exceeding
    // test timeout, without shortcuts in overload resolution.
    public class OverloadResolutionPerfTests : CSharpTestBase
    {
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
