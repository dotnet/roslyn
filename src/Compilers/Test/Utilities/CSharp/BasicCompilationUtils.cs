// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Symbols;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public static class BasicCompilationUtils
    {
        public static MetadataReference CompileToMetadata(string source, string assemblyName = null, IEnumerable<MetadataReference> references = null, bool verify = true)
        {
            if (references == null)
            {
                references = new[] { TestBase.MscorlibRef };
            }
            var compilation = CreateCompilationWithMscorlib(source, assemblyName, references);
            var verifier = Instance.CompileAndVerify(compilation, verify: verify);
            return MetadataReference.CreateFromImage(verifier.EmittedAssemblyData);
        }

        private static VisualBasicCompilation CreateCompilationWithMscorlib(string source, string assemblyName, IEnumerable<MetadataReference> references)
        {
            if (assemblyName == null)
            {
                assemblyName = TestBase.GetUniqueName();
            }
            var tree = VisualBasicSyntaxTree.ParseText(source);
            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);
            return VisualBasicCompilation.Create(assemblyName, new[] { tree }, references, options);
        }

        private static BasicTestBase s_instance;

        private static BasicTestBase Instance => s_instance ?? (s_instance = new BasicTestBase());

        private sealed class BasicTestBase : CommonTestBase
        {
            protected override CompilationOptions CompilationOptionsReleaseDll
            {
                get { throw new NotImplementedException(); }
            }

            protected override Compilation GetCompilationForEmit(IEnumerable<string> source, IEnumerable<MetadataReference> additionalRefs, CompilationOptions options, ParseOptions parseOptions)
            {
                throw new NotImplementedException();
            }

            internal override IEnumerable<IModuleSymbol> ReferencesToModuleSymbols(IEnumerable<MetadataReference> references, MetadataImportOptions importOptions = MetadataImportOptions.Public)
            {
                throw new NotImplementedException();
            }

            internal override string VisualizeRealIL(IModuleSymbol peModule, CodeAnalysis.CodeGen.CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers)
            {
                throw new NotImplementedException();
            }
        }
    }
}
