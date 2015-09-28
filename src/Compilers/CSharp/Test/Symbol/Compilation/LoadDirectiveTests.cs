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
        void EmptyFile()
        {
            var code = "#load \"\"";
            var compilation = CreateCompilation(code);
                
            Assert.Single(compilation.SyntaxTrees);
            compilation.GetDiagnostics().Verify(
                // error CS1504: Source file '' could not be opened -- Could not find file.
                Diagnostic(ErrorCode.ERR_NoSourceFile, "\"\"").WithArguments("", CSharpResources.CouldNotFindFile).WithLocation(1, 7));
        }

        [Fact]
        void MissingFile()
        {
            var code = "#load \"missing\"";
            var compilation = CreateCompilation(code);
                
            Assert.Single(compilation.SyntaxTrees);
            compilation.GetDiagnostics().Verify(
                // error CS1504: Source file 'missing' could not be opened -- Could not find file.
                Diagnostic(ErrorCode.ERR_NoSourceFile, "\"missing\"").WithArguments("missing", CSharpResources.CouldNotFindFile).WithLocation(1, 7));
        }

        [Fact]
        void FileWithErrors()
        {
            var code = "#load \"a.csx\"";
            var resolver = CreateResolver(
                Script("a.csx", @"
                    #load ""b.csx""
                    asdf();"));
            var compilation = CreateCompilation(code, resolver);

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
        void NoSourceReferenceResolver()
        {
            var code = "#load \"test\"";
            var compilation = CreateCompilationWithMscorlib(code, parseOptions: TestOptions.Script);

            Assert.Single(compilation.SyntaxTrees);
            compilation.GetDiagnostics().Verify(
                // (1,1): error CS8099: Source file references are not supported.
                // #load "test"
                Diagnostic(ErrorCode.ERR_SourceFileReferencesNotSupported, @"#load ""test""").WithLocation(1, 1));
        }

        private static CSharpCompilation CreateCompilation(string code, SourceReferenceResolver sourceReferenceResolver = null)
        {
            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                sourceReferenceResolver: sourceReferenceResolver ?? TestSourceReferenceResolver.Default);
            var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Interactive);
            return CreateCompilationWithMscorlib(code, options: options, parseOptions: parseOptions);
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
