// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TypeofTests : CSharpTestBase
    {
        [Fact, WorkItem(1720, "https://github.com/dotnet/roslyn/issues/1720")]
        public void GetSymbolsOnResultOfTypeof()
        {
            var source = @"
class C
{
    public C(int i)
    {
        typeof(C).GetField("" "").SetValue(null, new C(0));
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = (ObjectCreationExpressionSyntax)tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "new C(0)").Last();
            var identifierName = node.Type;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("C..ctor(System.Int32 i)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("C", typeInfo.Type.ToTestDisplayString());
        }

        [Fact]
        public void TypeofPointer()
        {
            CreateCompilation("""
                class C
                {
                    unsafe void M()
                    {
                        var v = typeof(int*);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void TypeofFunctionPointer1()
        {
            CreateCompilation("""
                class C
                {
                    unsafe void M()
                    {
                        var v = typeof(delegate*<int,int>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void TypeofFunctionPointer2()
        {
            CreateCompilation("""
                using System.Collections.Generic;

                class C
                {
                    unsafe void M()
                    {
                        var v = typeof(delegate*<List<int>,int>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void TypeofFunctionPointer3()
        {
            CreateCompilation("""
                using System.Collections.Generic;

                class C
                {
                    unsafe void M()
                    {
                        var v = typeof(delegate*<List<>,int>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,34): error CS7003: Unexpected use of an unbound generic name
                //         var v = typeof(delegate*<List<>,int>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "List<>").WithLocation(7, 34));
        }

        [Fact]
        public void TypeofFunctionPointer4()
        {
            CreateCompilation("""
                using System.Collections.Generic;

                class D<A, B, C>
                {
                    unsafe void M()
                    {
                        var v = typeof(D<, delegate*<int>, List<>>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,26): error CS1031: Type expected
                //         var v = typeof(D<, delegate*<int>, List<>>);
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(7, 26),
                // (7,44): error CS7003: Unexpected use of an unbound generic name
                //         var v = typeof(D<, delegate*<int>, List<>>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "List<>").WithLocation(7, 44));
        }
    }
}
