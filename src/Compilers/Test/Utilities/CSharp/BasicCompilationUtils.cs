// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CodeGen.CompilationTestData;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public static class BasicCompilationUtils
    {
        public static MetadataReference CompileToMetadata(string source, string assemblyName = null, IEnumerable<MetadataReference> references = null, Verification verify = default)
        {
            if (references == null)
            {
                references = new[] { TestBase.MscorlibRef };
            }
            var compilation = CreateCompilationWithMscorlib(source, assemblyName, references);
            var verifier = Instance.CompileAndVerifyCommon(compilation, verify: verify);
            return MetadataReference.CreateFromImage(verifier.EmittedAssemblyData);
        }

        private static VisualBasicCompilation CreateCompilationWithMscorlib(string source, string assemblyName, IEnumerable<MetadataReference> references)
        {
            if (assemblyName == null)
            {
                assemblyName = TestBase.GetUniqueName();
            }
            var tree = VisualBasicSyntaxTree.ParseText(SourceText.From(source, encoding: null, SourceHashAlgorithms.Default));
            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);
            return VisualBasicCompilation.Create(assemblyName, new[] { tree }, references, options);
        }

        private static BasicTestBase s_instance;

        private static BasicTestBase Instance => s_instance ?? (s_instance = new BasicTestBase());

        private sealed class BasicTestBase : CommonTestBase
        {
            internal override string VisualizeRealIL(IModuleSymbol peModule, MethodData methodData, IReadOnlyDictionary<int, string> markers, bool areLocalsZeroed)
            {
                throw new NotImplementedException();
            }
        }
    }
}
