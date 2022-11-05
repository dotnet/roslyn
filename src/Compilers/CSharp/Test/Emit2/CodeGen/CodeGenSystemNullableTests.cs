// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenSystemNullableTests : CSharpTestBase
    {
        [Fact]
        [WorkItem(65091, "https://github.com/dotnet/roslyn/issues/65091")]
        public void Nullable_Parameter_EqualsNull()
        {
            var source = @"
void M(in int? v)
{
    if (v == null) 
        return;
}";
            var verifier = CompileAndVerify(source);
            verifier.VerifyIL("Program.<<Main>$>g__M|0_0(in int?)", @"
{
    // Code size        8 (0x8)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       ""bool int?.HasValue.get""
    IL_0006:  pop
    IL_0007:  ret
}
");
        }

        [Fact]
        [WorkItem(65091, "https://github.com/dotnet/roslyn/issues/65091")]
        public void Nullable_Parameter_NotHasValue()
        {
            var source = @"
void M(in int? v)
{
    if (!v.HasValue) 
        return;
}";
            var verifier = CompileAndVerify(source);
            verifier.VerifyIL("Program.<<Main>$>g__M|0_0(in int?)", @"
{
    // Code size        8 (0x8)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       ""bool int?.HasValue.get""
    IL_0006:  pop
    IL_0007:  ret
}
");
        }

        [Fact]
        [WorkItem(65091, "https://github.com/dotnet/roslyn/issues/65091")]
        public void Nullable_Parameter_IsNull()
        {
            var source = @"
void M(in int? v)
{
    if (v is null) 
        return;
}";
            var verifier = CompileAndVerify(source);
            verifier.VerifyIL("Program.<<Main>$>g__M|0_0(in int?)", @"
{
    // Code size        8 (0x8)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       ""bool int?.HasValue.get""
    IL_0006:  pop
    IL_0007:  ret
}
");
        }
    }
}
