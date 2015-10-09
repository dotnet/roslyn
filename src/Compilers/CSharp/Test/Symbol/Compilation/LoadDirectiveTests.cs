// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LoadDirectiveTests : CSharpTestBase
    {
        [Fact]
        public void EmptyFile()
        {
            var code = "#load \"\"";
            var options = TestOptions.DebugDll.WithSourceReferenceResolver(TestSourceReferenceResolver.Default);
            var compilation = CreateCompilationWithMscorlib45(code, options: options, parseOptions: TestOptions.Interactive);

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
            var compilation = CreateCompilationWithMscorlib45(code, options: options, parseOptions: TestOptions.Interactive);

            Assert.Single(compilation.SyntaxTrees);
            compilation.VerifyDiagnostics(
                // error CS1504: Source file 'missing' could not be opened -- Could not find file.
                Diagnostic(ErrorCode.ERR_NoSourceFile, "\"missing\"").WithArguments("missing", CSharpResources.CouldNotFindFile).WithLocation(1, 7));
        }

        [Fact]
        public void FileWithErrors()
        {
            var code = "#load \"a.csx\"";
            var resolver = CreateResolver(
                Script("a.csx", @"
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

        private static SourceReferenceResolver CreateResolver(params KeyValuePair<string, string>[] scripts)
        {
            var sources = new Dictionary<string, string>();
            foreach (var script in scripts)
            {
                sources.Add(script.Key, script.Value);
            }
            return TestSourceReferenceResolver.Create(sources);
        }

        private static KeyValuePair<string, string> Script(string path, string source)
        {
            return new KeyValuePair<string, string>(path, source);
        }
    }
}
