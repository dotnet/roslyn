// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.UnitTests.Symbols
{
    public class DocumentationCommentIdTests : CommonTestBase
    {
        private CSharpCompilation CreateCompilation(string code) =>
            CreateCSharpCompilation(
                code, referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.NetStandard20));

        [Fact]
        public void TupleReturnMethod()
        {
            string code = @"
class C
{
    (int i, int) DoStuff() => default;
}";

            var comp = CreateCompilation(code);
            comp.VerifyDiagnostics();

            var symbol = comp.GetSymbolsWithName("DoStuff").Single();

            var actualDocId = DocumentationCommentId.CreateDeclarationId(symbol);

            string expectedDocId = "M:C.DoStuff~System.ValueTuple{System.Int32,System.Int32}";

            Assert.Equal(expectedDocId, actualDocId);

            var foundSymbols = DocumentationCommentId.GetSymbolsForDeclarationId(expectedDocId, comp);

            Assert.Equal(new[] { symbol }, foundSymbols);
        }

        [Theory]
        [InlineData(
            "void DoStuff((int i, int) tuple) {}",
            "M:C.DoStuff(System.ValueTuple{System.Int32,System.Int32})")]
        [InlineData(
            "void DoStuff((int, int, int, int, int, int, int, int, int) tuple) { }",
            "M:C.DoStuff(System.ValueTuple{System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.ValueTuple{System.Int32,System.Int32}})")]
        public void TupleParameterMethod(string methodCode, string expectedDocId)
        {
            string code = $@"
class C
{{
    {methodCode}
}}";

            var comp = CreateCompilation(code);
            comp.VerifyDiagnostics();

            var symbol = comp.GetSymbolsWithName("DoStuff").Single();
            var actualDocId = DocumentationCommentId.CreateDeclarationId(symbol);
            Assert.Equal(expectedDocId, actualDocId);
            var foundSymbols = DocumentationCommentId.GetSymbolsForDeclarationId(expectedDocId, comp);

            Assert.Equal(new[] { symbol }, foundSymbols);
        }

        [Fact]
        public void DynamicParameterMethod()
        {
            string code = @"
class C
{
    int DoStuff(dynamic dynamic) => 0;
}";

            var comp = CreateCSharpCompilation(code);

            var symbol = comp.GetSymbolsWithName("DoStuff").Single();

            var actualDocId = DocumentationCommentId.CreateDeclarationId(symbol);

            var expectedDocId = "M:C.DoStuff(System.Object)~System.Int32";

            Assert.Equal(expectedDocId, actualDocId);

            var foundSymbols = DocumentationCommentId.GetSymbolsForDeclarationId(expectedDocId, comp);

            Assert.Equal(new[] { symbol }, foundSymbols);
        }

        [Fact]
        public void NintParameterMethod()
        {
            string code = @"
class C
{
    void DoStuff(nint nint) {}
}";

            var comp = CreateCSharpCompilation(code);

            var symbol = comp.GetSymbolsWithName("DoStuff").Single();

            var actualDocId = DocumentationCommentId.CreateDeclarationId(symbol);

            var expectedDocId = "M:C.DoStuff(System.IntPtr)";

            Assert.Equal(expectedDocId, actualDocId);

            var foundSymbols = DocumentationCommentId.GetSymbolsForDeclarationId(expectedDocId, comp);

            Assert.Equal(new[] { symbol }, foundSymbols);
        }

        [Fact, WorkItem(65396, "https://github.com/dotnet/roslyn/issues/65396")]
        public void InvalidTypeParameterIndex_CSharp()
        {
            var comp = CreateCSharpCompilation("""
                namespace N;
                class C
                {
                    static void Main() { }
                    public void M<T>(T[] ts) { }
                }
                """).VerifyDiagnostics();
            Assert.NotNull(DocumentationCommentId.GetFirstSymbolForDeclarationId("M:N.C.M``1(``0[])", comp));
            Assert.Null(DocumentationCommentId.GetFirstSymbolForDeclarationId("M:N.C.M``1(`0[])", comp));
            Assert.Null(DocumentationCommentId.GetFirstSymbolForDeclarationId("M:N.C.M``1(``1[])", comp));
        }

        [Fact, WorkItem(65396, "https://github.com/dotnet/roslyn/issues/65396")]
        public void InvalidTypeParameterIndex_VisualBasic()
        {
            var comp = CreateVisualBasicCompilation("""
                Namespace N
                    Class C
                        Shared Sub Main()
                        End Sub

                        Public Sub M(Of T)(ByVal ts As T())
                        End Sub
                    End Class
                End Namespace
                """).VerifyDiagnostics();
            Assert.NotNull(DocumentationCommentId.GetFirstSymbolForDeclarationId("M:N.C.M``1(``0[])", comp));
            Assert.Null(DocumentationCommentId.GetFirstSymbolForDeclarationId("M:N.C.M``1(`0[])", comp));
            Assert.Null(DocumentationCommentId.GetFirstSymbolForDeclarationId("M:N.C.M``1(``1[])", comp));
        }

        internal override string VisualizeRealIL(
            IModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers, bool areLocalsZeroed)
            => throw new NotImplementedException();
    }
}
