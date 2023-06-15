// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.NativeAOT.UnitTests
{
    public class InterceptorUtilsTests : CSharpTestBase
    {
        [Fact]
        public void GetInterceptor_01()
        {
            var source1 = """
                C.Original();

                class C
                {
                    public static void Original() => throw null!;
                }
                """;

            var source2 = """
                using System;
                using System.Runtime.CompilerServices;

                class D
                {
                    [InterceptsLocation("Program.cs", 1, 3)]
                    public static void Interceptor() => Console.Write(1);
                }
                """;

            var attributeSource = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
            public sealed class InterceptsLocationAttribute : Attribute
            {
                public InterceptsLocationAttribute(string filePath, int line, int character)
                {
                }
            }
            """;

            var comp = CreateCompilation(new[] { (source1, "Program.cs"), (source2, "Interceptor.cs"), (attributeSource, "InterceptsLocationAttribute.cs") }, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular);

            var tree = comp.SyntaxTrees[0];
            var callSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var semanticModel = comp.GetSemanticModel(tree);
            Assert.Equal("", semanticModel.GetSymbolInfo(callSyntax.Expression).Symbol.ToTestDisplayString());
            Assert.Equal("", InterceptorUtils.GetInterceptor(callSyntax, comp, cancellationToken: default).ToTestDisplayString());

            var verifier = CompileAndVerify(comp, expectedOutput: "1");
            verifier.VerifyDiagnostics();

        }
    }
}
