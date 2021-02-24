// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class PropertyTests : EmitMetadataTestBase
    {
        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        [CompilerTrait(CompilerFeature.ExpressionBody)]
        public void ExpressionBodedProperty()
        {
            var source = @"
class C
{
    public int x;
    public int X
    {
        set => x = value;
        get => x;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("C.X.get", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.x""
  IL_0006:  ret
}
");
            verifier.VerifyIL("C.X.set", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.x""
  IL_0007:  ret
}
");
        }
    }
}
