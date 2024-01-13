// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class MethodImplementationFlagsTests : CSharpTestBase
    {
        [Fact]
        public void TestInliningFlags()
        {
            var src = @"
using System.Runtime.CompilerServices;

public class C
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void M_Aggressive()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void M_NoInlining()
    {
    }
}
";

            Action<ModuleSymbol> validator = module =>
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var aggressiveInliningMethod = c.GetMember<MethodSymbol>("M_Aggressive").GetPublicSymbol();
                Assert.Equal(MethodImplAttributes.AggressiveInlining, aggressiveInliningMethod.MethodImplementationFlags);

                var noInliningMethod = c.GetMember<MethodSymbol>("M_NoInlining").GetPublicSymbol();
                Assert.Equal(MethodImplAttributes.NoInlining, noInliningMethod.MethodImplementationFlags);
            };

            CompileAndVerify(src, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void TestOptimizationFlags()
        {
            var src = @"
using System.Runtime.CompilerServices;

public class C
{
    [MethodImpl((MethodImplOptions)512)] // Aggressive optimization
    public void M_Aggressive()
    {
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void M_NoOptimization()
    {
    }
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var aggressiveOptimizationMethod = c.GetMember<MethodSymbol>("M_Aggressive").GetPublicSymbol();
#if !NET472 // MethodImplAttributes.AggressiveOptimization was introduced in .NET Core 3
                Assert.Equal(MethodImplAttributes.AggressiveOptimization, aggressiveOptimizationMethod.MethodImplementationFlags);
#else
                Assert.Equal((MethodImplAttributes)512, aggressiveOptimizationMethod.MethodImplementationFlags);
#endif

                var noOptimizationMethod = c.GetMember<MethodSymbol>("M_NoOptimization").GetPublicSymbol();
                Assert.Equal(MethodImplAttributes.NoOptimization, noOptimizationMethod.MethodImplementationFlags);
            };

            CompileAndVerify(src, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void TestMixingOptimizationWithInliningFlags()
        {
            var src = @"
using System.Runtime.CompilerServices;

public class C
{
    [MethodImpl((MethodImplOptions)512 | MethodImplOptions.NoInlining)] // aggressive optimization and no inlining
    public void M_AggressiveOpt_NoInlining()
    {
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public void M_NoOpt_NoInlining()
    {
    }

    [MethodImpl((MethodImplOptions)512 | MethodImplOptions.AggressiveInlining)] // aggressive optimization and aggressive inlining
    public void M_AggressiveOpt_AggressiveInlining()
    {
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.AggressiveInlining)]
    public void M_NoOpt_AggressiveInlining()
    {
    }
}
";

            Action<ModuleSymbol> validator = module =>
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var aggressiveOptNoInliningMethod = c.GetMember<MethodSymbol>("M_AggressiveOpt_NoInlining").GetPublicSymbol();
#if !NET472 // MethodImplAttributes.AggressiveOptimization was introduced in .NET Core 3
                Assert.Equal(MethodImplAttributes.AggressiveOptimization | MethodImplAttributes.NoInlining, aggressiveOptNoInliningMethod.MethodImplementationFlags);
#else
                Assert.Equal((MethodImplAttributes)512 | MethodImplAttributes.NoInlining, aggressiveOptNoInliningMethod.MethodImplementationFlags);
#endif

                var noOptNoInliningMethod = c.GetMember<MethodSymbol>("M_NoOpt_NoInlining").GetPublicSymbol();
                Assert.Equal(MethodImplAttributes.NoOptimization | MethodImplAttributes.NoInlining, noOptNoInliningMethod.MethodImplementationFlags);

                var aggressiveOptAggressiveInliningMethod = c.GetMember<MethodSymbol>("M_AggressiveOpt_AggressiveInlining").GetPublicSymbol();
#if !NET472
                Assert.Equal(MethodImplAttributes.AggressiveOptimization | MethodImplAttributes.AggressiveInlining, aggressiveOptAggressiveInliningMethod.MethodImplementationFlags);
#else
                Assert.Equal((MethodImplAttributes)512 | MethodImplAttributes.AggressiveInlining, aggressiveOptAggressiveInliningMethod.MethodImplementationFlags);
#endif

                var noOptAggressiveInliningMethod = c.GetMember<MethodSymbol>("M_NoOpt_AggressiveInlining").GetPublicSymbol();
                Assert.Equal(MethodImplAttributes.NoOptimization | MethodImplAttributes.AggressiveInlining, noOptAggressiveInliningMethod.MethodImplementationFlags);
            };

            CompileAndVerify(src, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void TestPreserveSigAndRuntimeFlags()
        {
            var src = @"
using System.Runtime.CompilerServices;

public class C
{
    [MethodImpl(MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
    public void M()
    {
    }
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var method = c.GetMember<MethodSymbol>("M").GetPublicSymbol();
                Assert.Equal(MethodImplAttributes.PreserveSig | MethodImplAttributes.Runtime, method.MethodImplementationFlags);
            };

            CompileAndVerify(src, sourceSymbolValidator: validator, symbolValidator: validator, verify: Verification.Skipped);
        }

        [Fact]
        public void TestNativeFlag()
        {
            var src = @"
using System.Runtime.CompilerServices;

public class C
{
    [MethodImpl(MethodCodeType = MethodCodeType.Native)]
    public extern void M();
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var method = c.GetMember<MethodSymbol>("M").GetPublicSymbol();
                Assert.Equal(MethodImplAttributes.Native, method.MethodImplementationFlags);
            };

            CompileAndVerify(src, sourceSymbolValidator: validator, symbolValidator: validator, verify: Verification.Skipped);
        }
    }
}
