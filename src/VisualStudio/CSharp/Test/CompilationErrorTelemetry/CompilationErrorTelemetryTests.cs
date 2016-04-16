// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices.CSharp.CompilationErrorTelemetry;
using Xunit;


namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CompilationErrorTelemetry
{
    public class CompilationErrorTelemetryTests
    {
        public TestWorkspace CreateCSharpWorkspaceWithLineOfCode(string lineOfCode, string otherContent, string usings)
        {
            string code = usings +
                Environment.NewLine +
                Environment.NewLine + @"
namespace CompilationErrorTest
{
" +
@"    class Program
    {"
+ otherContent +
Environment.NewLine
 + Environment.NewLine + @"
        static void Main(string[] args)
        {" + Environment.NewLine +
            lineOfCode +
            Environment.NewLine + @"
        }
    }
}
";
            return CSharpWorkspaceFactory.CreateWorkspaceFromFile(code, exportProvider: null);
        }

        private async Task<List<CompilationErrorDetails>> GetErrorDetailsFromLineOfCodeAsync(string lineOfCode)
        {
            return await GetErrorDetailsFromLineOfCodeAsync(lineOfCode, "").ConfigureAwait(false);
        }

        private async Task<List<CompilationErrorDetails>> GetErrorDetailsFromLineOfCodeAsync(string lineOfCode, string otherContent)
        {
            string defaultUsings = "using System.Collections.Generic; using System.IO";
            return await GetErrorDetailsFromLineOfCodeAsync(lineOfCode, otherContent, defaultUsings).ConfigureAwait(false);
        }

        private async Task<List<CompilationErrorDetails>> GetErrorDetailsFromLineOfCodeAsync(string lineOfCode, string otherContent, string usings)
        {
            using (var workspace = CreateCSharpWorkspaceWithLineOfCode(lineOfCode, otherContent, usings))
            {
                var errorDetailDiscoverer = new CompilationErrorDetailDiscoverer();
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                var errorDetails = await errorDetailDiscoverer.GetCompilationErrorDetails(document, null, CancellationToken.None).ConfigureAwait(false);
                return errorDetails;
            }
        }

        [Fact]
        public async Task TestErrorCS0103()
        {
            string lineOfCode = "if (notdeclared != null) { }";

            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);

            var errorDetail = errorDetails[0];
            Assert.Equal("CS0103", errorDetail.ErrorId);
            Assert.Equal("notdeclared", errorDetail.UnresolvedMemberName);
            Assert.Equal(null, errorDetail.LeftExpressionDocId);
            Assert.Equal(null, errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(null, errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS1061()
        {
            string lineOfCode = "new FileStream(\"test.txt\", FileMode.Truncate).MissingMethod();";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);

            var errorDetail = errorDetails[0];
            Assert.Equal("CS1061", errorDetail.ErrorId);
            Assert.Equal("MissingMethod", errorDetail.UnresolvedMemberName);
            Assert.Equal("T:System.IO.FileStream", errorDetail.LeftExpressionDocId);
            Assert.Equal(new string[] { "T:System.IO.Stream", "T:System.MarshalByRefObject", "T:System.Object" }, errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(null, errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS1061Generic()
        {
            string lineOfCode = "new List<string>().Length;";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);

            var errorDetail = errorDetails[0];
            Assert.Equal("CS1061", errorDetail.ErrorId);
            Assert.Equal("Length", errorDetail.UnresolvedMemberName);
            Assert.Equal("T:System.Collections.Generic.List`1", errorDetail.LeftExpressionDocId);
            Assert.Equal(new string[] { "T:System.Object" }, errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(null, errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS0246()
        {
            string lineOfCode = "NotAType x;";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);

            var errorDetail = errorDetails[0];
            Assert.Equal("CS0246", errorDetail.ErrorId);
            Assert.Equal("NotAType", errorDetail.UnresolvedMemberName);
            Assert.Equal(null, errorDetail.LeftExpressionDocId);
            Assert.Equal(null, errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(null, errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS0305()
        {
            string lineOfCode = "IEnumerable<string, string> wrongTypeArgCount;";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);
            var errorDetail = errorDetails[0];
            Assert.Equal("CS0305", errorDetail.ErrorId);
            Assert.Equal("IEnumerable", errorDetail.UnresolvedMemberName);
            Assert.Equal(null, errorDetail.LeftExpressionDocId);
            Assert.Equal(null, errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(new string[] { "T:System.String", "T:System.String" }, errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS0305WithUnresolvedTypeArgument()
        {
            string lineOfCode = "IEnumerable<string, NotAType> wrongTypeArgCount;";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(2, errorDetails.Count);

            var cs0305errorDetail = errorDetails[0];
            Assert.Equal("CS0305", cs0305errorDetail.ErrorId);
            Assert.Equal("IEnumerable", cs0305errorDetail.UnresolvedMemberName);
            Assert.Equal(null, cs0305errorDetail.LeftExpressionDocId);
            Assert.Equal(null, cs0305errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(new string[] { "T:System.String", "!:NotAType" }, cs0305errorDetail.GenericArguments);

            var cs0246errorDetail = errorDetails[1];
            Assert.Equal("CS0246", cs0246errorDetail.ErrorId);
            Assert.Equal("NotAType", cs0246errorDetail.UnresolvedMemberName);
            Assert.Equal(null, cs0246errorDetail.LeftExpressionDocId);
            Assert.Equal(null, cs0246errorDetail.LeftExpressionBaseTypeDocIds);
        }

        [Fact]
        public async Task TestErrorCS0308()
        {
            string lineOfCode = " F<int>(); ";
            string nonGenericFunction = @"public void F() {}";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode, nonGenericFunction).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);
            var cs0308errorDetail = errorDetails[0];
            Assert.Equal("CS0308", cs0308errorDetail.ErrorId);
            Assert.Equal("F", cs0308errorDetail.UnresolvedMemberName);
            Assert.Equal(null, cs0308errorDetail.LeftExpressionDocId);
            Assert.Equal(null, cs0308errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(new string[] { "T:System.Int32" }, cs0308errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS0308WithGenericReturnType()
        {
            string lineOfCode = "";
            string usings = "using System.Collections;";
            string genericTypeWithGenericReturnValue = @"public class MyGenericType<T>
                                                    {
                                                        private T[] items = new T[100];
                                                        
                                                        public IEnumerator<T> GetEnumerator()   // CS0308
                                                        {
                                                            for (int i = 0; i < items.Length; i++)
                                                                yield return items[i];
                                                        }
                                                    }";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode, genericTypeWithGenericReturnValue, usings).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);
            var cs0308errorDetail = errorDetails[0];
            Assert.Equal("CS0308", cs0308errorDetail.ErrorId);
            Assert.Equal("IEnumerator", cs0308errorDetail.UnresolvedMemberName);
            Assert.Equal(null, cs0308errorDetail.LeftExpressionDocId);
            Assert.Equal(null, cs0308errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(new string[] { "!:T" }, cs0308errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS0616()
        {
            string lineOfCode = "[Program]\n public void SomeMethod() { }";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);
            var cs0616errorDetail = errorDetails[0];
            Assert.Equal("CS0616", cs0616errorDetail.ErrorId);
            Assert.Equal("T:CompilationErrorTest.Program", cs0616errorDetail.UnresolvedMemberName);
            Assert.Equal(null, cs0616errorDetail.LeftExpressionDocId);
            Assert.Equal(null, cs0616errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(null, cs0616errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS1503()
        {
            string lineOfCode = "System.IO.FileStream CS1503 = System.IO.File.Open(123.4f, System.IO.FileMode.Open);";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);

            var cs1503errorDetail = errorDetails[0];
            Assert.Equal("CS1503", cs1503errorDetail.ErrorId);
            Assert.Equal("Open", cs1503errorDetail.MethodName);
            Assert.Equal(new string[] { "T:System.Single", "T:System.IO.FileMode" }, cs1503errorDetail.ArgumentTypes);
            Assert.Equal(null, cs1503errorDetail.UnresolvedMemberName);
            Assert.Equal("T:System.IO.File", cs1503errorDetail.LeftExpressionDocId);
            Assert.Equal(null, cs1503errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(null, cs1503errorDetail.GenericArguments);
        }

        [Fact]
        public async Task TestErrorCS1935()
        {
            string lineOfCode = "IEnumerable<int> e = from n in new int[] { 0, 1, 2, 3, 4, 5 } where n > 3 select n;";
            var errorDetails = await GetErrorDetailsFromLineOfCodeAsync(lineOfCode).ConfigureAwait(false);
            Assert.Equal(1, errorDetails.Count);

            var errorDetail = errorDetails[0];
            Assert.Equal("CS1935", errorDetail.ErrorId);
            Assert.Equal(null, errorDetail.UnresolvedMemberName);
            Assert.Equal(null, errorDetail.LeftExpressionDocId);
            Assert.Equal(null, errorDetail.LeftExpressionBaseTypeDocIds);
            Assert.Equal(null, errorDetail.GenericArguments);
        }
    }
}
