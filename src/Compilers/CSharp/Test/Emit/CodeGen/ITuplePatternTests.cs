// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class ITuplePatternTests : EmitMetadataTestBase
    {
        protected CSharpCompilation CreatePatternCompilation(string source, CSharpCompilationOptions options = null)
        {
            return CreateCompilation(new[] { source, _iTupleSource }, options: options ?? TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
        }

        private const string _iTupleSource = @"
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}
";

#if NETCOREAPP2_0
        [Fact]
        public void TestPresenceOfITupleInNetcore2()
        {
            var source =
@"using System.Runtime.CompilerServices;
public class C : ITuple
{
    public int Length => 0;
    public object this[int i] => null;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
        }
#endif

        [Fact]
        public void XYZZY()
        {
        }
    }
}
