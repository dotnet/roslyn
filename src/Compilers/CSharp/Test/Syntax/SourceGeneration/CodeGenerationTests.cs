// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;
using static Microsoft.CodeAnalysis.CSharp.SourceGeneration.CSharpCodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        private static ITypeSymbol Int32 = System_Int32;
        private static ITypeSymbol Boolean = System_Boolean;
        private static ITypeSymbol Void = System_Void;

        //[Fact]
        //public void TestAllGeneration()
        //{
        //    var compilation = (Compilation)CSharpTestBase.CreateCompilationWithMscorlib45(new[] { "" });
        //    var syntax = compilation.GlobalNamespace.GenerateSyntax();
        //}
    }
}
