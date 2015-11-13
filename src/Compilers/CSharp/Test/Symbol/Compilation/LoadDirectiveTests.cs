// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LoadDirectiveTests : CSharpTestBase
    {
        [Fact]
        public void EmptyFile()
        {
            var code = "#load \"\"";
            var options = TestOptions.DebugDll.WithSourceReferenceResolver(TestSourceReferenceResolver.Default);
            var compilation = CreateCompilationWithMscorlib45(code, options: options, parseOptions: TestOptions.Script);

            Assert.Single(compilation.SyntaxTrees);
            compilation.VerifyDiagnostics(
                // error CS1504: Source file '' could not be opened -- Could not find file.
                Diagnostic(ErrorCode.ERR_NoSourceFile, "\"\"").WithArguments("", CSharpResources.CouldNotFindFile).WithLocation(1, 7));
        }

        [Fact]
        public void MissingFile()
        {
            var code = "#load \"missing\"";
            var options = TestOptions.DebugDll.WithSourceReferenceResolver(TestSourceReferenceResolver.Default);
            var compilation = CreateCompilationWithMscorlib45(code, options: options, parseOptions: TestOptions.Script);

            Assert.Single(compilation.SyntaxTrees);
            compilation.VerifyDiagnostics(
                // error CS1504: Source file 'missing' could not be opened -- Could not find file.
                Diagnostic(ErrorCode.ERR_NoSourceFile, "\"missing\"").WithArguments("missing", CSharpResources.CouldNotFindFile).WithLocation(1, 7));
        }

        [Fact]
        public void FileWithErrors()
        {
            var code = "#load \"a.csx\"";
            var resolver = TestSourceReferenceResolver.Create(
                KeyValuePair.Create("a.csx", @"
                    #load ""b.csx""
                    asdf();"));
            var options = TestOptions.DebugDll.WithSourceReferenceResolver(resolver);
            var compilation = CreateCompilationWithMscorlib45(code, options: options, parseOptions: TestOptions.Script);

            Assert.Equal(2, compilation.SyntaxTrees.Length);
            compilation.GetParseDiagnostics().Verify(
                // a.csx(2,27): error CS1504: Source file 'b.csx' could not be opened -- Could not find file.
                //                     #load "b.csx";
                Diagnostic(ErrorCode.ERR_NoSourceFile, @"""b.csx""").WithArguments("b.csx", "Could not find file.").WithLocation(2, 27));
            compilation.GetDiagnostics().Verify(
                // a.csx(2,27): error CS1504: Source file 'b.csx' could not be opened -- Could not find file.
                //                     #load "b.csx";
                Diagnostic(ErrorCode.ERR_NoSourceFile, @"""b.csx""").WithArguments("b.csx", "Could not find file.").WithLocation(2, 27),
                // a.csx(3,21): error CS0103: The name 'asdf' does not exist in the current context
                //                     asdf();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "asdf").WithArguments("asdf").WithLocation(3, 21));
        }

        [Fact]
        public void FileThatCannotBeDecoded()
        {
            var code = "#load \"b.csx\"";
            var resolver = TestSourceReferenceResolver.Create(
                KeyValuePair.Create<string, object>("a.csx", new byte[] { 0xd8, 0x00, 0x00 }),
                KeyValuePair.Create<string, object>("b.csx", "#load \"a.csx\""));
            var options = TestOptions.DebugDll.WithSourceReferenceResolver(resolver);
            var compilation = CreateCompilationWithMscorlib45(code, sourceFileName: "external1.csx", options: options, parseOptions: TestOptions.Script);
            var external1 = compilation.SyntaxTrees.Last();
            var external2 = Parse(code, "external2.csx", TestOptions.Script);
            compilation = compilation.AddSyntaxTrees(external2);

            Assert.Equal(3, compilation.SyntaxTrees.Length);
            compilation.GetParseDiagnostics().Verify(
                // (1,7): error CS2015: 'a.csx' is a binary file instead of a text file
                // #load "a.csx"
                Diagnostic(ErrorCode.ERR_BinaryFile, @"""a.csx""").WithArguments("a.csx").WithLocation(1, 7));

            var external3 = Parse(@"
                #load ""b.csx""
                #load ""a.csx""", filename: "external3.csx", options: TestOptions.Script);
            compilation = compilation.ReplaceSyntaxTree(external1, external3);

            Assert.Equal(3, compilation.SyntaxTrees.Length);
            compilation.GetParseDiagnostics().Verify(
                // external3.csx(3,23): error CS2015: 'a.csx' is a binary file instead of a text file
                //                 #load "a.csx"
                Diagnostic(ErrorCode.ERR_BinaryFile, @"""a.csx""").WithArguments("a.csx").WithLocation(3, 23),
                // b.csx(1,7): error CS2015: 'a.csx' is a binary file instead of a text file
                // #load "a.csx"
                Diagnostic(ErrorCode.ERR_BinaryFile, @"""a.csx""").WithArguments("a.csx").WithLocation(1, 7));

            var external4 = Parse("#load \"a.csx\"", "external4.csx", TestOptions.Script);
            compilation = compilation.ReplaceSyntaxTree(external3, external4);

            Assert.Equal(3, compilation.SyntaxTrees.Length);
            compilation.GetParseDiagnostics().Verify(
                // external4.csx(1,7): error CS2015: 'a.csx' is a binary file instead of a text file
                // #load "a.csx"
                Diagnostic(ErrorCode.ERR_BinaryFile, @"""a.csx""").WithArguments("a.csx").WithLocation(1, 7),
                // b.csx(1,7): error CS2015: 'a.csx' is a binary file instead of a text file
                // #load "a.csx"
                Diagnostic(ErrorCode.ERR_BinaryFile, @"""a.csx""").WithArguments("a.csx").WithLocation(1, 7));

            compilation = compilation.RemoveSyntaxTrees(external2);

            Assert.Equal(external4, compilation.SyntaxTrees.Single());
            compilation.GetParseDiagnostics().Verify(
                // external4.csx(1,7): error CS2015: 'a.csx' is a binary file instead of a text file
                // #load "a.csx"
                Diagnostic(ErrorCode.ERR_BinaryFile, @"""a.csx""").WithArguments("a.csx").WithLocation(1, 7));
        }

        [Fact]
        public void NoSourceReferenceResolver()
        {
            var code = "#load \"test\"";
            var compilation = CreateCompilationWithMscorlib45(code, parseOptions: TestOptions.Script);

            Assert.Single(compilation.SyntaxTrees);
            compilation.VerifyDiagnostics(
                // (1,1): error CS8099: Source file references are not supported.
                // #load "test"
                Diagnostic(ErrorCode.ERR_SourceFileReferencesNotSupported, @"#load ""test""").WithLocation(1, 1));
        }

        [Fact, WorkItem(6439, "https://github.com/dotnet/roslyn/issues/6439")]
        public void ErrorInInactiveRegion()
        {
            var code = @"
#if undefined
#load nothing
#endif";
            var compilation = CreateCompilationWithMscorlib45(code, parseOptions: TestOptions.Script);

            Assert.Single(compilation.SyntaxTrees);
            compilation.VerifyDiagnostics();
        }

        [Fact, WorkItem(6698, "https://github.com/dotnet/roslyn/issues/6698")]
        public void Cycles()
        {
            var code = "#load \"a.csx\"";
            var resolver = TestSourceReferenceResolver.Create(KeyValuePair.Create("a.csx", code));
            var options = TestOptions.DebugDll.WithSourceReferenceResolver(resolver);
            var compilation = CreateCompilationWithMscorlib45(code, options: options, parseOptions: TestOptions.Script);

            Assert.Equal(2, compilation.SyntaxTrees.Length);
            compilation.VerifyDiagnostics();

            var newTree = Parse(code, "a.csx", TestOptions.Script);
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Last(), newTree);

            Assert.Equal(2, compilation.SyntaxTrees.Length);
            compilation.VerifyDiagnostics();

            compilation = compilation.RemoveSyntaxTrees(newTree);

            Assert.Empty(compilation.SyntaxTrees);
            compilation.VerifyDiagnostics();

            code = "#load \"a.csx\"";
            resolver = TestSourceReferenceResolver.Create(
                KeyValuePair.Create("a.csx", "#load \"b.csx\""),
                KeyValuePair.Create("b.csx", code));
            options = TestOptions.DebugDll.WithSourceReferenceResolver(resolver);
            compilation = CreateCompilationWithMscorlib45(code, options: options, parseOptions: TestOptions.Script);

            Assert.Equal(3, compilation.SyntaxTrees.Length);
            compilation.VerifyDiagnostics();

            newTree = Parse(code, "a.csx", TestOptions.Script);
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Last(), newTree);

            Assert.Equal(3, compilation.SyntaxTrees.Length);
            compilation.VerifyDiagnostics();

            compilation = compilation.RemoveSyntaxTrees(newTree);

            Assert.Empty(compilation.SyntaxTrees);
            compilation.VerifyDiagnostics();
        }
    }
}
