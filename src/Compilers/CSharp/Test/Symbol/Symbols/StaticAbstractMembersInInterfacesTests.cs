// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class StaticAbstractMembersInInterfacesTests : CSharpTestBase
    {
        [Fact]
        public void MethodModifiers_01()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01()
    ; 

    virtual static void M02()
    ; 

    sealed static void M03() 
    ; 

    override static void M04() 
    ; 

    abstract virtual static void M05()
    ; 

    abstract sealed static void M06()
    ; 

    abstract override static void M07()
    ; 

    virtual sealed static void M08() 
    ; 

    virtual override static void M09() 
    ; 

    sealed override static void M10() 
    ; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular9,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract static void M01()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "9.0", "preview").WithLocation(4, 26),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (7,25): error CS0501: 'I1.M02()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M02").WithArguments("I1.M02()").WithLocation(7, 25),
                // (10,24): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed static void M03() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "9.0", "preview").WithLocation(10, 24),
                // (10,24): error CS0501: 'I1.M03()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed static void M03() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M03").WithArguments("I1.M03()").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (13,26): error CS0501: 'I1.M04()' must declare a body because it is not marked abstract, extern, or partial
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M04").WithArguments("I1.M04()").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "9.0", "preview").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "9.0", "preview").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "9.0", "preview").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "9.0", "preview").WithLocation(25, 32),
                // (25,32): error CS0501: 'I1.M08()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M08").WithArguments("I1.M08()").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (28,34): error CS0501: 'I1.M09()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M09").WithArguments("I1.M09()").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "9.0", "preview").WithLocation(31, 33),
                // (31,33): error CS0501: 'I1.M10()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M10").WithArguments("I1.M10()").WithLocation(31, 33)
                );

            ValidateMethodModifiers_01(compilation1);
        }

        private static void ValidateMethodModifiers_01(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("M01");

            Assert.True(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.True(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            var m02 = i1.GetMember<MethodSymbol>("M02");

            Assert.False(m02.IsAbstract);
            Assert.False(m02.IsVirtual);
            Assert.False(m02.IsMetadataVirtual());
            Assert.False(m02.IsSealed);
            Assert.True(m02.IsStatic);
            Assert.False(m02.IsExtern);
            Assert.False(m02.IsAsync);
            Assert.False(m02.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m02));

            var m03 = i1.GetMember<MethodSymbol>("M03");

            Assert.False(m03.IsAbstract);
            Assert.False(m03.IsVirtual);
            Assert.False(m03.IsMetadataVirtual());
            Assert.False(m03.IsSealed);
            Assert.True(m03.IsStatic);
            Assert.False(m03.IsExtern);
            Assert.False(m03.IsAsync);
            Assert.False(m03.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m03));

            var m04 = i1.GetMember<MethodSymbol>("M04");

            Assert.False(m04.IsAbstract);
            Assert.False(m04.IsVirtual);
            Assert.False(m04.IsMetadataVirtual());
            Assert.False(m04.IsSealed);
            Assert.True(m04.IsStatic);
            Assert.False(m04.IsExtern);
            Assert.False(m04.IsAsync);
            Assert.False(m04.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m04));

            var m05 = i1.GetMember<MethodSymbol>("M05");

            Assert.True(m05.IsAbstract);
            Assert.False(m05.IsVirtual);
            Assert.True(m05.IsMetadataVirtual());
            Assert.False(m05.IsSealed);
            Assert.True(m05.IsStatic);
            Assert.False(m05.IsExtern);
            Assert.False(m05.IsAsync);
            Assert.False(m05.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m05));

            var m06 = i1.GetMember<MethodSymbol>("M06");

            Assert.True(m06.IsAbstract);
            Assert.False(m06.IsVirtual);
            Assert.True(m06.IsMetadataVirtual());
            Assert.False(m06.IsSealed);
            Assert.True(m06.IsStatic);
            Assert.False(m06.IsExtern);
            Assert.False(m06.IsAsync);
            Assert.False(m06.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m06));

            var m07 = i1.GetMember<MethodSymbol>("M07");

            Assert.True(m07.IsAbstract);
            Assert.False(m07.IsVirtual);
            Assert.True(m07.IsMetadataVirtual());
            Assert.False(m07.IsSealed);
            Assert.True(m07.IsStatic);
            Assert.False(m07.IsExtern);
            Assert.False(m07.IsAsync);
            Assert.False(m07.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m07));

            var m08 = i1.GetMember<MethodSymbol>("M08");

            Assert.False(m08.IsAbstract);
            Assert.False(m08.IsVirtual);
            Assert.False(m08.IsMetadataVirtual());
            Assert.False(m08.IsSealed);
            Assert.True(m08.IsStatic);
            Assert.False(m08.IsExtern);
            Assert.False(m08.IsAsync);
            Assert.False(m08.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m08));

            var m09 = i1.GetMember<MethodSymbol>("M09");

            Assert.False(m09.IsAbstract);
            Assert.False(m09.IsVirtual);
            Assert.False(m09.IsMetadataVirtual());
            Assert.False(m09.IsSealed);
            Assert.True(m09.IsStatic);
            Assert.False(m09.IsExtern);
            Assert.False(m09.IsAsync);
            Assert.False(m09.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m09));

            var m10 = i1.GetMember<MethodSymbol>("M10");

            Assert.False(m10.IsAbstract);
            Assert.False(m10.IsVirtual);
            Assert.False(m10.IsMetadataVirtual());
            Assert.False(m10.IsSealed);
            Assert.True(m10.IsStatic);
            Assert.False(m10.IsExtern);
            Assert.False(m10.IsAsync);
            Assert.False(m10.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m10));
        }

        [Fact]
        public void MethodModifiers_02()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01()
    {}

    virtual static void M02()
    {}

    sealed static void M03() 
    {}

    override static void M04() 
    {}

    abstract virtual static void M05()
    {}

    abstract sealed static void M06()
    {}

    abstract override static void M07()
    {}

    virtual sealed static void M08() 
    {}

    virtual override static void M09() 
    {}

    sealed override static void M10() 
    {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular9,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract static void M01()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "9.0", "preview").WithLocation(4, 26),
                // (4,26): error CS0500: 'I1.M01()' cannot declare a body because it is marked abstract
                //     abstract static void M01()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M01").WithArguments("I1.M01()").WithLocation(4, 26),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (10,24): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed static void M03() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "9.0", "preview").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "9.0", "preview").WithLocation(16, 34),
                // (16,34): error CS0500: 'I1.M05()' cannot declare a body because it is marked abstract
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M05").WithArguments("I1.M05()").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "9.0", "preview").WithLocation(19, 33),
                // (19,33): error CS0500: 'I1.M06()' cannot declare a body because it is marked abstract
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M06").WithArguments("I1.M06()").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "9.0", "preview").WithLocation(22, 35),
                // (22,35): error CS0500: 'I1.M07()' cannot declare a body because it is marked abstract
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M07").WithArguments("I1.M07()").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "9.0", "preview").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "9.0", "preview").WithLocation(31, 33)
                );

            ValidateMethodModifiers_01(compilation1);
        }

        [Fact]
        public void MethodModifiers_03()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01()
    ; 

    virtual static void M02()
    ; 

    sealed static void M03() 
    ; 

    override static void M04() 
    ; 

    abstract virtual static void M05()
    ; 

    abstract sealed static void M06()
    ; 

    abstract override static void M07()
    ; 

    virtual sealed static void M08() 
    ; 

    virtual override static void M09() 
    ; 

    sealed override static void M10() 
    ; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (7,25): error CS0501: 'I1.M02()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M02").WithArguments("I1.M02()").WithLocation(7, 25),
                // (10,24): error CS0501: 'I1.M03()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed static void M03() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M03").WithArguments("I1.M03()").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (13,26): error CS0501: 'I1.M04()' must declare a body because it is not marked abstract, extern, or partial
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M04").WithArguments("I1.M04()").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS0501: 'I1.M08()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M08").WithArguments("I1.M08()").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (28,34): error CS0501: 'I1.M09()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M09").WithArguments("I1.M09()").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS0501: 'I1.M10()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M10").WithArguments("I1.M10()").WithLocation(31, 33)
                );

            ValidateMethodModifiers_01(compilation1);
        }

        [Fact]
        public void MethodModifiers_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01()
    {}

    virtual static void M02()
    {}

    sealed static void M03() 
    {}

    override static void M04() 
    {}

    abstract virtual static void M05()
    {}

    abstract sealed static void M06()
    {}

    abstract override static void M07()
    {}

    virtual sealed static void M08() 
    {}

    virtual override static void M09() 
    {}

    sealed override static void M10() 
    {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS0500: 'I1.M01()' cannot declare a body because it is marked abstract
                //     abstract static void M01()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M01").WithArguments("I1.M01()").WithLocation(4, 26),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS0500: 'I1.M05()' cannot declare a body because it is marked abstract
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M05").WithArguments("I1.M05()").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS0500: 'I1.M06()' cannot declare a body because it is marked abstract
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M06").WithArguments("I1.M06()").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS0500: 'I1.M07()' cannot declare a body because it is marked abstract
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M07").WithArguments("I1.M07()").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33)
                );

            ValidateMethodModifiers_01(compilation1);
        }

        [Fact]
        public void MethodModifiers_05()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01()
    ; 

    virtual static void M02()
    ; 

    sealed static void M03() 
    ; 

    override static void M04() 
    ; 

    abstract virtual static void M05()
    ; 

    abstract sealed static void M06()
    ; 

    abstract override static void M07()
    ; 

    virtual sealed static void M08() 
    ; 

    virtual override static void M09() 
    ; 

    sealed override static void M10() 
    ; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular7_3,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract static void M01()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "7.3", "preview").WithLocation(4, 26),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (7,25): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M02").WithArguments("static", "7.3", "8.0").WithLocation(7, 25),
                // (7,25): error CS0501: 'I1.M02()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M02").WithArguments("I1.M02()").WithLocation(7, 25),
                // (10,24): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed static void M03() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "7.3", "preview").WithLocation(10, 24),
                // (10,24): error CS0501: 'I1.M03()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed static void M03() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M03").WithArguments("I1.M03()").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (13,26): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M04").WithArguments("static", "7.3", "8.0").WithLocation(13, 26),
                // (13,26): error CS0501: 'I1.M04()' must declare a body because it is not marked abstract, extern, or partial
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M04").WithArguments("I1.M04()").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "7.3", "preview").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "7.3", "preview").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "7.3", "preview").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "7.3", "preview").WithLocation(25, 32),
                // (25,32): error CS0501: 'I1.M08()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M08").WithArguments("I1.M08()").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (28,34): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M09").WithArguments("static", "7.3", "8.0").WithLocation(28, 34),
                // (28,34): error CS0501: 'I1.M09()' must declare a body because it is not marked abstract, extern, or partial
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M09").WithArguments("I1.M09()").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "7.3", "preview").WithLocation(31, 33),
                // (31,33): error CS0501: 'I1.M10()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M10").WithArguments("I1.M10()").WithLocation(31, 33)
                );

            ValidateMethodModifiers_01(compilation1);
        }

        [Fact]
        public void MethodModifiers_06()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01()
    {}

    virtual static void M02()
    {}

    sealed static void M03() 
    {}

    override static void M04() 
    {}

    abstract virtual static void M05()
    {}

    abstract sealed static void M06()
    {}

    abstract override static void M07()
    {}

    virtual sealed static void M08() 
    {}

    virtual override static void M09() 
    {}

    sealed override static void M10() 
    {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular7_3,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract static void M01()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "7.3", "preview").WithLocation(4, 26),
                // (4,26): error CS0500: 'I1.M01()' cannot declare a body because it is marked abstract
                //     abstract static void M01()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M01").WithArguments("I1.M01()").WithLocation(4, 26),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (7,25): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual static void M02()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M02").WithArguments("default interface implementation", "8.0").WithLocation(7, 25),
                // (10,24): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed static void M03() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "7.3", "preview").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (13,26): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     override static void M04() 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M04").WithArguments("default interface implementation", "8.0").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "7.3", "preview").WithLocation(16, 34),
                // (16,34): error CS0500: 'I1.M05()' cannot declare a body because it is marked abstract
                //     abstract virtual static void M05()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M05").WithArguments("I1.M05()").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "7.3", "preview").WithLocation(19, 33),
                // (19,33): error CS0500: 'I1.M06()' cannot declare a body because it is marked abstract
                //     abstract sealed static void M06()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M06").WithArguments("I1.M06()").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "7.3", "preview").WithLocation(22, 35),
                // (22,35): error CS0500: 'I1.M07()' cannot declare a body because it is marked abstract
                //     abstract override static void M07()
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M07").WithArguments("I1.M07()").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     virtual sealed static void M08() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "7.3", "preview").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (28,34): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual override static void M09() 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M09").WithArguments("default interface implementation", "8.0").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed override static void M10() 
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "7.3", "preview").WithLocation(31, 33)
                );

            ValidateMethodModifiers_01(compilation1);
        }

        [Fact]
        public void SealedStaticConstructor_01()
        {
            var source1 =
@"
interface I1
{
    sealed static I1() {}
}

partial interface I2
{
    partial sealed static I2();
}

partial interface I2
{
    partial static I2() {}
}

partial interface I3
{
    partial static I3();
}

partial interface I3
{
    partial sealed static I3() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,19): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed static I1() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I1").WithArguments("sealed").WithLocation(4, 19),
                // (9,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial sealed static I2();
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(9, 5),
                // (9,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial sealed static I2();
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(9, 5),
                // (9,27): error CS0106: The modifier 'sealed' is not valid for this item
                //     partial sealed static I2();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I2").WithArguments("sealed").WithLocation(9, 27),
                // (14,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial static I2() {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(14, 5),
                // (14,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial static I2() {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(14, 5),
                // (14,20): error CS0111: Type 'I2' already defines a member called 'I2' with the same parameter types
                //     partial static I2() {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "I2").WithArguments("I2", "I2").WithLocation(14, 20),
                // (19,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial static I3();
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(19, 5),
                // (19,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial static I3();
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(19, 5),
                // (24,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial sealed static I3() {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(24, 5),
                // (24,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                //     partial sealed static I3() {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(24, 5),
                // (24,27): error CS0106: The modifier 'sealed' is not valid for this item
                //     partial sealed static I3() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I3").WithArguments("sealed").WithLocation(24, 27),
                // (24,27): error CS0111: Type 'I3' already defines a member called 'I3' with the same parameter types
                //     partial sealed static I3() {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "I3").WithArguments("I3", "I3").WithLocation(24, 27)
                );

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>(".cctor");

            Assert.False(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.False(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));
        }

        [Fact]
        public void SealedStaticConstructor_02()
        {
            var source1 =
@"
partial interface I2
{
    sealed static partial I2();
}

partial interface I2
{
    static partial I2() {}
}

partial interface I3
{
    static partial I3();
}

partial interface I3
{
    sealed static partial I3() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,19): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     sealed static partial I2();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(4, 19),
                // (4,27): error CS0501: 'I2.I2()' must declare a body because it is not marked abstract, extern, or partial
                //     sealed static partial I2();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "I2").WithArguments("I2.I2()").WithLocation(4, 27),
                // (4,27): error CS0542: 'I2': member names cannot be the same as their enclosing type
                //     sealed static partial I2();
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "I2").WithArguments("I2").WithLocation(4, 27),
                // (9,12): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     static partial I2() {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(9, 12),
                // (9,20): error CS0542: 'I2': member names cannot be the same as their enclosing type
                //     static partial I2() {}
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "I2").WithArguments("I2").WithLocation(9, 20),
                // (9,20): error CS0111: Type 'I2' already defines a member called 'I2' with the same parameter types
                //     static partial I2() {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "I2").WithArguments("I2", "I2").WithLocation(9, 20),
                // (9,20): error CS0161: 'I2.I2()': not all code paths return a value
                //     static partial I2() {}
                Diagnostic(ErrorCode.ERR_ReturnExpected, "I2").WithArguments("I2.I2()").WithLocation(9, 20),
                // (14,12): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     static partial I3();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(14, 12),
                // (14,20): error CS0501: 'I3.I3()' must declare a body because it is not marked abstract, extern, or partial
                //     static partial I3();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "I3").WithArguments("I3.I3()").WithLocation(14, 20),
                // (14,20): error CS0542: 'I3': member names cannot be the same as their enclosing type
                //     static partial I3();
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "I3").WithArguments("I3").WithLocation(14, 20),
                // (19,19): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //     sealed static partial I3() {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(19, 19),
                // (19,27): error CS0542: 'I3': member names cannot be the same as their enclosing type
                //     sealed static partial I3() {}
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "I3").WithArguments("I3").WithLocation(19, 27),
                // (19,27): error CS0111: Type 'I3' already defines a member called 'I3' with the same parameter types
                //     sealed static partial I3() {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "I3").WithArguments("I3", "I3").WithLocation(19, 27),
                // (19,27): error CS0161: 'I3.I3()': not all code paths return a value
                //     sealed static partial I3() {}
                Diagnostic(ErrorCode.ERR_ReturnExpected, "I3").WithArguments("I3.I3()").WithLocation(19, 27)
                );
        }

        [Fact]
        public void AbstractStaticConstructor_01()
        {
            var source1 =
@"
interface I1
{
    abstract static I1();
}

interface I2
{
    abstract static I2() {}
}

interface I3
{
    static I3();
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,21): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract static I1();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I1").WithArguments("abstract").WithLocation(4, 21),
                // (9,21): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract static I2() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I2").WithArguments("abstract").WithLocation(9, 21),
                // (14,12): error CS0501: 'I3.I3()' must declare a body because it is not marked abstract, extern, or partial
                //     static I3();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "I3").WithArguments("I3.I3()").WithLocation(14, 12)
                );

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>(".cctor");

            Assert.False(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.False(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));
        }

        [Fact]
        public void PartialSealedStatic_01()
        {
            var source1 =
@"
partial interface I1
{
    sealed static partial void M01();
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics();

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("M01");

            Assert.False(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.False(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialDefinition());
            Assert.Null(m01.PartialImplementationPart);
        }

        [Fact]
        public void PartialSealedStatic_02()
        {
            var source1 =
@"
partial interface I1
{
    sealed static partial void M01();
}
partial interface I1
{
    sealed static partial void M01() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            ValidatePartialSealedStatic_02(compilation1);
        }

        private static void ValidatePartialSealedStatic_02(CSharpCompilation compilation1)
        {
            compilation1.VerifyDiagnostics();

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("M01");

            Assert.False(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.False(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialDefinition());
            Assert.Same(m01, m01.PartialImplementationPart.PartialDefinitionPart);

            m01 = m01.PartialImplementationPart;

            Assert.False(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.False(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialImplementation());
        }

        [Fact]
        public void PartialSealedStatic_03()
        {
            var source1 =
@"
partial interface I1
{
    static partial void M01();
}
partial interface I1
{
    sealed static partial void M01() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            ValidatePartialSealedStatic_02(compilation1);
        }

        [Fact]
        public void PartialSealedStatic_04()
        {
            var source1 =
@"
partial interface I1
{
    sealed static partial void M01();
}
partial interface I1
{
    static partial void M01() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            ValidatePartialSealedStatic_02(compilation1);
        }

        [Fact]
        public void PartialAbstractStatic_01()
        {
            var source1 =
@"
partial interface I1
{
    abstract static partial void M01();
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,34): error CS0750: A partial method cannot have the 'abstract' modifier
                //     abstract static partial void M01();
                Diagnostic(ErrorCode.ERR_PartialMethodInvalidModifier, "M01").WithLocation(4, 34)
                );

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("M01");

            Assert.True(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.True(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialDefinition());
            Assert.Null(m01.PartialImplementationPart);
        }

        [Fact]
        public void PartialAbstractStatic_02()
        {
            var source1 =
@"
partial interface I1
{
    abstract static partial void M01();
}
partial interface I1
{
    abstract static partial void M01() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,34): error CS0750: A partial method cannot have the 'abstract' modifier
                //     abstract static partial void M01();
                Diagnostic(ErrorCode.ERR_PartialMethodInvalidModifier, "M01").WithLocation(4, 34),
                // (8,34): error CS0500: 'I1.M01()' cannot declare a body because it is marked abstract
                //     abstract static partial void M01() {}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M01").WithArguments("I1.M01()").WithLocation(8, 34),
                // (8,34): error CS0750: A partial method cannot have the 'abstract' modifier
                //     abstract static partial void M01() {}
                Diagnostic(ErrorCode.ERR_PartialMethodInvalidModifier, "M01").WithLocation(8, 34)
                );

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("M01");

            Assert.True(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.True(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialDefinition());
            Assert.Same(m01, m01.PartialImplementationPart.PartialDefinitionPart);

            m01 = m01.PartialImplementationPart;

            Assert.True(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.True(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialImplementation());
        }

        [Fact]
        public void PartialAbstractStatic_03()
        {
            var source1 =
@"
partial interface I1
{
    abstract static partial void M01();
}
partial interface I1
{
    static partial void M01() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,34): error CS0750: A partial method cannot have the 'abstract' modifier
                //     abstract static partial void M01();
                Diagnostic(ErrorCode.ERR_PartialMethodInvalidModifier, "M01").WithLocation(4, 34)
                );

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("M01");

            Assert.True(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.True(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialDefinition());
            Assert.Same(m01, m01.PartialImplementationPart.PartialDefinitionPart);

            m01 = m01.PartialImplementationPart;

            Assert.False(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.False(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialImplementation());
        }

        [Fact]
        public void PartialAbstractStatic_04()
        {
            var source1 =
@"
partial interface I1
{
    static partial void M01();
}
partial interface I1
{
    abstract static partial void M01() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,34): error CS0500: 'I1.M01()' cannot declare a body because it is marked abstract
                //     abstract static partial void M01() {}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "M01").WithArguments("I1.M01()").WithLocation(8, 34),
                // (8,34): error CS0750: A partial method cannot have the 'abstract' modifier
                //     abstract static partial void M01() {}
                Diagnostic(ErrorCode.ERR_PartialMethodInvalidModifier, "M01").WithLocation(8, 34)
                );

            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("M01");

            Assert.False(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.False(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialDefinition());
            Assert.Same(m01, m01.PartialImplementationPart.PartialDefinitionPart);

            m01 = m01.PartialImplementationPart;

            Assert.True(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.True(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            Assert.True(m01.IsPartialImplementation());
        }

        [Fact]
        public void PrivateAbstractStatic_01()
        {
            var source1 =
@"
interface I1
{
    private abstract static void M01();
    private abstract static bool P01 { get; }
    private abstract static event System.Action E01;
    private abstract static I1 operator+ (I1 x);
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,34): error CS0621: 'I1.M01()': virtual or abstract members cannot be private
                //     private abstract static void M01();
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M01").WithArguments("I1.M01()").WithLocation(4, 34),
                // (5,34): error CS0621: 'I1.P01': virtual or abstract members cannot be private
                //     private abstract static bool P01 { get; }
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "P01").WithArguments("I1.P01").WithLocation(5, 34),
                // (6,49): error CS0621: 'I1.E01': virtual or abstract members cannot be private
                //     private abstract static event System.Action E01;
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "E01").WithArguments("I1.E01").WithLocation(6, 49),
                // (7,40): error CS0558: User-defined operator 'I1.operator +(I1)' must be declared static and public
                //     private abstract static I1 operator+ (I1 x);
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "+").WithArguments("I1.operator +(I1)").WithLocation(7, 40)
                );
        }

        [Fact]
        public void PropertyModifiers_01()
        {
            var source1 =
@"
public interface I1
{
    abstract static bool M01 { get
    ; } 

    virtual static bool M02 { get
    ; } 

    sealed static bool M03 { get
    ; } 

    override static bool M04 { get
    ; } 

    abstract virtual static bool M05 { get
    ; } 

    abstract sealed static bool M06 { get
    ; } 

    abstract override static bool M07 { get
    ; } 

    virtual sealed static bool M08 { get
    ; } 

    virtual override static bool M09 { get
    ; } 

    sealed override static bool M10 { get
    ; } 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular9,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract static bool M01 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "9.0", "preview").WithLocation(4, 26),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static bool M02 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (10,24): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed static bool M03 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "9.0", "preview").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static bool M04 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "9.0", "preview").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "9.0", "preview").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "9.0", "preview").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "9.0", "preview").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "9.0", "preview").WithLocation(31, 33)
                );

            ValidatePropertyModifiers_01(compilation1);
        }

        private static void ValidatePropertyModifiers_01(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");

            {
                var m01 = i1.GetMember<PropertySymbol>("M01");

                Assert.True(m01.IsAbstract);
                Assert.False(m01.IsVirtual);
                Assert.False(m01.IsSealed);
                Assert.True(m01.IsStatic);
                Assert.False(m01.IsExtern);
                Assert.False(m01.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m01));

                var m02 = i1.GetMember<PropertySymbol>("M02");

                Assert.False(m02.IsAbstract);
                Assert.False(m02.IsVirtual);
                Assert.False(m02.IsSealed);
                Assert.True(m02.IsStatic);
                Assert.False(m02.IsExtern);
                Assert.False(m02.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m02));

                var m03 = i1.GetMember<PropertySymbol>("M03");

                Assert.False(m03.IsAbstract);
                Assert.False(m03.IsVirtual);
                Assert.False(m03.IsSealed);
                Assert.True(m03.IsStatic);
                Assert.False(m03.IsExtern);
                Assert.False(m03.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m03));

                var m04 = i1.GetMember<PropertySymbol>("M04");

                Assert.False(m04.IsAbstract);
                Assert.False(m04.IsVirtual);
                Assert.False(m04.IsSealed);
                Assert.True(m04.IsStatic);
                Assert.False(m04.IsExtern);
                Assert.False(m04.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m04));

                var m05 = i1.GetMember<PropertySymbol>("M05");

                Assert.True(m05.IsAbstract);
                Assert.False(m05.IsVirtual);
                Assert.False(m05.IsSealed);
                Assert.True(m05.IsStatic);
                Assert.False(m05.IsExtern);
                Assert.False(m05.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m05));

                var m06 = i1.GetMember<PropertySymbol>("M06");

                Assert.True(m06.IsAbstract);
                Assert.False(m06.IsVirtual);
                Assert.False(m06.IsSealed);
                Assert.True(m06.IsStatic);
                Assert.False(m06.IsExtern);
                Assert.False(m06.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m06));

                var m07 = i1.GetMember<PropertySymbol>("M07");

                Assert.True(m07.IsAbstract);
                Assert.False(m07.IsVirtual);
                Assert.False(m07.IsSealed);
                Assert.True(m07.IsStatic);
                Assert.False(m07.IsExtern);
                Assert.False(m07.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m07));

                var m08 = i1.GetMember<PropertySymbol>("M08");

                Assert.False(m08.IsAbstract);
                Assert.False(m08.IsVirtual);
                Assert.False(m08.IsSealed);
                Assert.True(m08.IsStatic);
                Assert.False(m08.IsExtern);
                Assert.False(m08.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m08));

                var m09 = i1.GetMember<PropertySymbol>("M09");

                Assert.False(m09.IsAbstract);
                Assert.False(m09.IsVirtual);
                Assert.False(m09.IsSealed);
                Assert.True(m09.IsStatic);
                Assert.False(m09.IsExtern);
                Assert.False(m09.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m09));

                var m10 = i1.GetMember<PropertySymbol>("M10");

                Assert.False(m10.IsAbstract);
                Assert.False(m10.IsVirtual);
                Assert.False(m10.IsSealed);
                Assert.True(m10.IsStatic);
                Assert.False(m10.IsExtern);
                Assert.False(m10.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m10));
            }
            {
                var m01 = i1.GetMember<PropertySymbol>("M01").GetMethod;

                Assert.True(m01.IsAbstract);
                Assert.False(m01.IsVirtual);
                Assert.True(m01.IsMetadataVirtual());
                Assert.False(m01.IsSealed);
                Assert.True(m01.IsStatic);
                Assert.False(m01.IsExtern);
                Assert.False(m01.IsAsync);
                Assert.False(m01.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m01));

                var m02 = i1.GetMember<PropertySymbol>("M02").GetMethod;

                Assert.False(m02.IsAbstract);
                Assert.False(m02.IsVirtual);
                Assert.False(m02.IsMetadataVirtual());
                Assert.False(m02.IsSealed);
                Assert.True(m02.IsStatic);
                Assert.False(m02.IsExtern);
                Assert.False(m02.IsAsync);
                Assert.False(m02.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m02));

                var m03 = i1.GetMember<PropertySymbol>("M03").GetMethod;

                Assert.False(m03.IsAbstract);
                Assert.False(m03.IsVirtual);
                Assert.False(m03.IsMetadataVirtual());
                Assert.False(m03.IsSealed);
                Assert.True(m03.IsStatic);
                Assert.False(m03.IsExtern);
                Assert.False(m03.IsAsync);
                Assert.False(m03.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m03));

                var m04 = i1.GetMember<PropertySymbol>("M04").GetMethod;

                Assert.False(m04.IsAbstract);
                Assert.False(m04.IsVirtual);
                Assert.False(m04.IsMetadataVirtual());
                Assert.False(m04.IsSealed);
                Assert.True(m04.IsStatic);
                Assert.False(m04.IsExtern);
                Assert.False(m04.IsAsync);
                Assert.False(m04.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m04));

                var m05 = i1.GetMember<PropertySymbol>("M05").GetMethod;

                Assert.True(m05.IsAbstract);
                Assert.False(m05.IsVirtual);
                Assert.True(m05.IsMetadataVirtual());
                Assert.False(m05.IsSealed);
                Assert.True(m05.IsStatic);
                Assert.False(m05.IsExtern);
                Assert.False(m05.IsAsync);
                Assert.False(m05.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m05));

                var m06 = i1.GetMember<PropertySymbol>("M06").GetMethod;

                Assert.True(m06.IsAbstract);
                Assert.False(m06.IsVirtual);
                Assert.True(m06.IsMetadataVirtual());
                Assert.False(m06.IsSealed);
                Assert.True(m06.IsStatic);
                Assert.False(m06.IsExtern);
                Assert.False(m06.IsAsync);
                Assert.False(m06.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m06));

                var m07 = i1.GetMember<PropertySymbol>("M07").GetMethod;

                Assert.True(m07.IsAbstract);
                Assert.False(m07.IsVirtual);
                Assert.True(m07.IsMetadataVirtual());
                Assert.False(m07.IsSealed);
                Assert.True(m07.IsStatic);
                Assert.False(m07.IsExtern);
                Assert.False(m07.IsAsync);
                Assert.False(m07.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m07));

                var m08 = i1.GetMember<PropertySymbol>("M08").GetMethod;

                Assert.False(m08.IsAbstract);
                Assert.False(m08.IsVirtual);
                Assert.False(m08.IsMetadataVirtual());
                Assert.False(m08.IsSealed);
                Assert.True(m08.IsStatic);
                Assert.False(m08.IsExtern);
                Assert.False(m08.IsAsync);
                Assert.False(m08.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m08));

                var m09 = i1.GetMember<PropertySymbol>("M09").GetMethod;

                Assert.False(m09.IsAbstract);
                Assert.False(m09.IsVirtual);
                Assert.False(m09.IsMetadataVirtual());
                Assert.False(m09.IsSealed);
                Assert.True(m09.IsStatic);
                Assert.False(m09.IsExtern);
                Assert.False(m09.IsAsync);
                Assert.False(m09.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m09));

                var m10 = i1.GetMember<PropertySymbol>("M10").GetMethod;

                Assert.False(m10.IsAbstract);
                Assert.False(m10.IsVirtual);
                Assert.False(m10.IsMetadataVirtual());
                Assert.False(m10.IsSealed);
                Assert.True(m10.IsStatic);
                Assert.False(m10.IsExtern);
                Assert.False(m10.IsAsync);
                Assert.False(m10.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m10));
            }
        }

        [Fact]
        public void PropertyModifiers_02()
        {
            var source1 =
@"
public interface I1
{
    abstract static bool M01 { get
    => throw null; } 

    virtual static bool M02 { get
    => throw null; } 

    sealed static bool M03 { get
    => throw null; } 

    override static bool M04 { get
    => throw null; } 

    abstract virtual static bool M05 { get
    { throw null; } } 

    abstract sealed static bool M06 { get
    => throw null; } 

    abstract override static bool M07 { get
    => throw null; } 

    virtual sealed static bool M08 { get
    => throw null; } 

    virtual override static bool M09 { get
    => throw null; } 

    sealed override static bool M10 { get
    => throw null; } 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular9,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract static bool M01 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "9.0", "preview").WithLocation(4, 26),
                // (4,32): error CS0500: 'I1.M01.get' cannot declare a body because it is marked abstract
                //     abstract static bool M01 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M01.get").WithLocation(4, 32),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static bool M02 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (10,24): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed static bool M03 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "9.0", "preview").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static bool M04 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "9.0", "preview").WithLocation(16, 34),
                // (16,40): error CS0500: 'I1.M05.get' cannot declare a body because it is marked abstract
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M05.get").WithLocation(16, 40),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "9.0", "preview").WithLocation(19, 33),
                // (19,39): error CS0500: 'I1.M06.get' cannot declare a body because it is marked abstract
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M06.get").WithLocation(19, 39),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "9.0", "preview").WithLocation(22, 35),
                // (22,41): error CS0500: 'I1.M07.get' cannot declare a body because it is marked abstract
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M07.get").WithLocation(22, 41),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "9.0", "preview").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "9.0", "preview").WithLocation(31, 33)
                );

            ValidatePropertyModifiers_01(compilation1);
        }

        [Fact]
        public void PropertyModifiers_03()
        {
            var source1 =
@"
public interface I1
{
    abstract static bool M01 { get
    ; } 

    virtual static bool M02 { get
    ; } 

    sealed static bool M03 { get
    ; } 

    override static bool M04 { get
    ; } 

    abstract virtual static bool M05 { get
    ; } 

    abstract sealed static bool M06 { get
    ; } 

    abstract override static bool M07 { get
    ; } 

    virtual sealed static bool M08 { get
    ; } 

    virtual override static bool M09 { get
    ; } 

    sealed override static bool M10 { get
    ; } 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static bool M02 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static bool M04 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33)
                );

            ValidatePropertyModifiers_01(compilation1);
        }

        [Fact]
        public void PropertyModifiers_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static bool M01 { get
    => throw null; } 

    virtual static bool M02 { get
    => throw null; } 

    sealed static bool M03 { get
    => throw null; } 

    override static bool M04 { get
    => throw null; } 

    abstract virtual static bool M05 { get
    { throw null; } } 

    abstract sealed static bool M06 { get
    => throw null; } 

    abstract override static bool M07 { get
    => throw null; } 

    virtual sealed static bool M08 { get
    => throw null; } 

    virtual override static bool M09 { get
    => throw null; } 

    sealed override static bool M10 { get
    => throw null; } 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,32): error CS0500: 'I1.M01.get' cannot declare a body because it is marked abstract
                //     abstract static bool M01 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M01.get").WithLocation(4, 32),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static bool M02 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static bool M04 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,40): error CS0500: 'I1.M05.get' cannot declare a body because it is marked abstract
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M05.get").WithLocation(16, 40),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,39): error CS0500: 'I1.M06.get' cannot declare a body because it is marked abstract
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M06.get").WithLocation(19, 39),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,41): error CS0500: 'I1.M07.get' cannot declare a body because it is marked abstract
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M07.get").WithLocation(22, 41),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33)
                );

            ValidatePropertyModifiers_01(compilation1);
        }

        [Fact]
        public void PropertyModifiers_05()
        {
            var source1 =
@"
public interface I1
{
    abstract static bool M01 { get
    ; } 

    virtual static bool M02 { get
    ; } 

    sealed static bool M03 { get
    ; } 

    override static bool M04 { get
    ; } 

    abstract virtual static bool M05 { get
    ; } 

    abstract sealed static bool M06 { get
    ; } 

    abstract override static bool M07 { get
    ; } 

    virtual sealed static bool M08 { get
    ; } 

    virtual override static bool M09 { get
    ; } 

    sealed override static bool M10 { get
    ; } 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular7_3,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract static bool M01 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "7.3", "preview").WithLocation(4, 26),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static bool M02 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (7,25): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     virtual static bool M02 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M02").WithArguments("static", "7.3", "8.0").WithLocation(7, 25),
                // (10,24): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed static bool M03 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "7.3", "preview").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static bool M04 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (13,26): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     override static bool M04 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M04").WithArguments("static", "7.3", "8.0").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "7.3", "preview").WithLocation(16, 34),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "7.3", "preview").WithLocation(19, 33),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "7.3", "preview").WithLocation(22, 35),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "7.3", "preview").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (28,34): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M09").WithArguments("static", "7.3", "8.0").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "7.3", "preview").WithLocation(31, 33)
                );

            ValidatePropertyModifiers_01(compilation1);
        }

        [Fact]
        public void PropertyModifiers_06()
        {
            var source1 =
@"
public interface I1
{
    abstract static bool M01 { get
    => throw null; } 

    virtual static bool M02 { get
    => throw null; } 

    sealed static bool M03 { get
    => throw null; } 

    override static bool M04 { get
    => throw null; } 

    abstract virtual static bool M05 { get
    { throw null; } } 

    abstract sealed static bool M06 { get
    => throw null; } 

    abstract override static bool M07 { get
    => throw null; } 

    virtual sealed static bool M08 { get
    => throw null; } 

    virtual override static bool M09 { get
    => throw null; } 

    sealed override static bool M10 { get
    => throw null; } 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular7_3,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract static bool M01 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "7.3", "preview").WithLocation(4, 26),
                // (4,32): error CS0500: 'I1.M01.get' cannot declare a body because it is marked abstract
                //     abstract static bool M01 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M01.get").WithLocation(4, 32),
                // (7,25): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static bool M02 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 25),
                // (7,25): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual static bool M02 { get
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M02").WithArguments("default interface implementation", "8.0").WithLocation(7, 25),
                // (10,24): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed static bool M03 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "7.3", "preview").WithLocation(10, 24),
                // (13,26): error CS0106: The modifier 'override' is not valid for this item
                //     override static bool M04 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 26),
                // (13,26): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     override static bool M04 { get
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M04").WithArguments("default interface implementation", "8.0").WithLocation(13, 26),
                // (16,34): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 34),
                // (16,34): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "7.3", "preview").WithLocation(16, 34),
                // (16,40): error CS0500: 'I1.M05.get' cannot declare a body because it is marked abstract
                //     abstract virtual static bool M05 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M05.get").WithLocation(16, 40),
                // (19,33): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 33),
                // (19,33): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "7.3", "preview").WithLocation(19, 33),
                // (19,39): error CS0500: 'I1.M06.get' cannot declare a body because it is marked abstract
                //     abstract sealed static bool M06 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M06.get").WithLocation(19, 39),
                // (22,35): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 35),
                // (22,35): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "7.3", "preview").WithLocation(22, 35),
                // (22,41): error CS0500: 'I1.M07.get' cannot declare a body because it is marked abstract
                //     abstract override static bool M07 { get
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("I1.M07.get").WithLocation(22, 41),
                // (25,32): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 32),
                // (25,32): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     virtual sealed static bool M08 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "7.3", "preview").WithLocation(25, 32),
                // (28,34): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 34),
                // (28,34): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 34),
                // (28,34): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual override static bool M09 { get
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M09").WithArguments("default interface implementation", "8.0").WithLocation(28, 34),
                // (31,33): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 33),
                // (31,33): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed override static bool M10 { get
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "7.3", "preview").WithLocation(31, 33)
                );

            ValidatePropertyModifiers_01(compilation1);
        }

        [Fact]
        public void EventModifiers_01()
        {
            var source1 =
@"#pragma warning disable CS0067 // The event is never used
public interface I1
{
    abstract static event D M01
    ;

    virtual static event D M02
    ;

    sealed static event D M03
    ;

    override static event D M04
    ;

    abstract virtual static event D M05
    ;

    abstract sealed static event D M06
    ;

    abstract override static event D M07
    ;

    virtual sealed static event D M08
    ;

    virtual override static event D M09
    ;

    sealed override static event D M10
    ;
}

public delegate void D();
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular9,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,29): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract static event D M01
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "9.0", "preview").WithLocation(4, 29),
                // (7,28): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static event D M02
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 28),
                // (10,27): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed static event D M03
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "9.0", "preview").WithLocation(10, 27),
                // (13,29): error CS0106: The modifier 'override' is not valid for this item
                //     override static event D M04
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 29),
                // (16,37): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static event D M05
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 37),
                // (16,37): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract virtual static event D M05
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "9.0", "preview").WithLocation(16, 37),
                // (19,36): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static event D M06
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 36),
                // (19,36): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract sealed static event D M06
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "9.0", "preview").WithLocation(19, 36),
                // (22,38): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static event D M07
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 38),
                // (22,38): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract override static event D M07
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "9.0", "preview").WithLocation(22, 38),
                // (25,35): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static event D M08
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 35),
                // (25,35): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     virtual sealed static event D M08
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "9.0", "preview").WithLocation(25, 35),
                // (28,37): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static event D M09
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 37),
                // (28,37): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static event D M09
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 37),
                // (31,36): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static event D M10
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 36),
                // (31,36): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed override static event D M10
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "9.0", "preview").WithLocation(31, 36)
                );

            ValidateEventModifiers_01(compilation1);
        }

        private static void ValidateEventModifiers_01(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");

            {
                var m01 = i1.GetMember<EventSymbol>("M01");

                Assert.True(m01.IsAbstract);
                Assert.False(m01.IsVirtual);
                Assert.False(m01.IsSealed);
                Assert.True(m01.IsStatic);
                Assert.False(m01.IsExtern);
                Assert.False(m01.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m01));

                var m02 = i1.GetMember<EventSymbol>("M02");

                Assert.False(m02.IsAbstract);
                Assert.False(m02.IsVirtual);
                Assert.False(m02.IsSealed);
                Assert.True(m02.IsStatic);
                Assert.False(m02.IsExtern);
                Assert.False(m02.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m02));

                var m03 = i1.GetMember<EventSymbol>("M03");

                Assert.False(m03.IsAbstract);
                Assert.False(m03.IsVirtual);
                Assert.False(m03.IsSealed);
                Assert.True(m03.IsStatic);
                Assert.False(m03.IsExtern);
                Assert.False(m03.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m03));

                var m04 = i1.GetMember<EventSymbol>("M04");

                Assert.False(m04.IsAbstract);
                Assert.False(m04.IsVirtual);
                Assert.False(m04.IsSealed);
                Assert.True(m04.IsStatic);
                Assert.False(m04.IsExtern);
                Assert.False(m04.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m04));

                var m05 = i1.GetMember<EventSymbol>("M05");

                Assert.True(m05.IsAbstract);
                Assert.False(m05.IsVirtual);
                Assert.False(m05.IsSealed);
                Assert.True(m05.IsStatic);
                Assert.False(m05.IsExtern);
                Assert.False(m05.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m05));

                var m06 = i1.GetMember<EventSymbol>("M06");

                Assert.True(m06.IsAbstract);
                Assert.False(m06.IsVirtual);
                Assert.False(m06.IsSealed);
                Assert.True(m06.IsStatic);
                Assert.False(m06.IsExtern);
                Assert.False(m06.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m06));

                var m07 = i1.GetMember<EventSymbol>("M07");

                Assert.True(m07.IsAbstract);
                Assert.False(m07.IsVirtual);
                Assert.False(m07.IsSealed);
                Assert.True(m07.IsStatic);
                Assert.False(m07.IsExtern);
                Assert.False(m07.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m07));

                var m08 = i1.GetMember<EventSymbol>("M08");

                Assert.False(m08.IsAbstract);
                Assert.False(m08.IsVirtual);
                Assert.False(m08.IsSealed);
                Assert.True(m08.IsStatic);
                Assert.False(m08.IsExtern);
                Assert.False(m08.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m08));

                var m09 = i1.GetMember<EventSymbol>("M09");

                Assert.False(m09.IsAbstract);
                Assert.False(m09.IsVirtual);
                Assert.False(m09.IsSealed);
                Assert.True(m09.IsStatic);
                Assert.False(m09.IsExtern);
                Assert.False(m09.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m09));

                var m10 = i1.GetMember<EventSymbol>("M10");

                Assert.False(m10.IsAbstract);
                Assert.False(m10.IsVirtual);
                Assert.False(m10.IsSealed);
                Assert.True(m10.IsStatic);
                Assert.False(m10.IsExtern);
                Assert.False(m10.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m10));
            }

            foreach (var addAccessor in new[] { true, false })
            {
                var m01 = getAccessor(i1.GetMember<EventSymbol>("M01"), addAccessor);

                Assert.True(m01.IsAbstract);
                Assert.False(m01.IsVirtual);
                Assert.True(m01.IsMetadataVirtual());
                Assert.False(m01.IsSealed);
                Assert.True(m01.IsStatic);
                Assert.False(m01.IsExtern);
                Assert.False(m01.IsAsync);
                Assert.False(m01.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m01));

                var m02 = getAccessor(i1.GetMember<EventSymbol>("M02"), addAccessor);

                Assert.False(m02.IsAbstract);
                Assert.False(m02.IsVirtual);
                Assert.False(m02.IsMetadataVirtual());
                Assert.False(m02.IsSealed);
                Assert.True(m02.IsStatic);
                Assert.False(m02.IsExtern);
                Assert.False(m02.IsAsync);
                Assert.False(m02.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m02));

                var m03 = getAccessor(i1.GetMember<EventSymbol>("M03"), addAccessor);

                Assert.False(m03.IsAbstract);
                Assert.False(m03.IsVirtual);
                Assert.False(m03.IsMetadataVirtual());
                Assert.False(m03.IsSealed);
                Assert.True(m03.IsStatic);
                Assert.False(m03.IsExtern);
                Assert.False(m03.IsAsync);
                Assert.False(m03.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m03));

                var m04 = getAccessor(i1.GetMember<EventSymbol>("M04"), addAccessor);

                Assert.False(m04.IsAbstract);
                Assert.False(m04.IsVirtual);
                Assert.False(m04.IsMetadataVirtual());
                Assert.False(m04.IsSealed);
                Assert.True(m04.IsStatic);
                Assert.False(m04.IsExtern);
                Assert.False(m04.IsAsync);
                Assert.False(m04.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m04));

                var m05 = getAccessor(i1.GetMember<EventSymbol>("M05"), addAccessor);

                Assert.True(m05.IsAbstract);
                Assert.False(m05.IsVirtual);
                Assert.True(m05.IsMetadataVirtual());
                Assert.False(m05.IsSealed);
                Assert.True(m05.IsStatic);
                Assert.False(m05.IsExtern);
                Assert.False(m05.IsAsync);
                Assert.False(m05.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m05));

                var m06 = getAccessor(i1.GetMember<EventSymbol>("M06"), addAccessor);

                Assert.True(m06.IsAbstract);
                Assert.False(m06.IsVirtual);
                Assert.True(m06.IsMetadataVirtual());
                Assert.False(m06.IsSealed);
                Assert.True(m06.IsStatic);
                Assert.False(m06.IsExtern);
                Assert.False(m06.IsAsync);
                Assert.False(m06.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m06));

                var m07 = getAccessor(i1.GetMember<EventSymbol>("M07"), addAccessor);

                Assert.True(m07.IsAbstract);
                Assert.False(m07.IsVirtual);
                Assert.True(m07.IsMetadataVirtual());
                Assert.False(m07.IsSealed);
                Assert.True(m07.IsStatic);
                Assert.False(m07.IsExtern);
                Assert.False(m07.IsAsync);
                Assert.False(m07.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m07));

                var m08 = getAccessor(i1.GetMember<EventSymbol>("M08"), addAccessor);

                Assert.False(m08.IsAbstract);
                Assert.False(m08.IsVirtual);
                Assert.False(m08.IsMetadataVirtual());
                Assert.False(m08.IsSealed);
                Assert.True(m08.IsStatic);
                Assert.False(m08.IsExtern);
                Assert.False(m08.IsAsync);
                Assert.False(m08.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m08));

                var m09 = getAccessor(i1.GetMember<EventSymbol>("M09"), addAccessor);

                Assert.False(m09.IsAbstract);
                Assert.False(m09.IsVirtual);
                Assert.False(m09.IsMetadataVirtual());
                Assert.False(m09.IsSealed);
                Assert.True(m09.IsStatic);
                Assert.False(m09.IsExtern);
                Assert.False(m09.IsAsync);
                Assert.False(m09.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m09));

                var m10 = getAccessor(i1.GetMember<EventSymbol>("M10"), addAccessor);

                Assert.False(m10.IsAbstract);
                Assert.False(m10.IsVirtual);
                Assert.False(m10.IsMetadataVirtual());
                Assert.False(m10.IsSealed);
                Assert.True(m10.IsStatic);
                Assert.False(m10.IsExtern);
                Assert.False(m10.IsAsync);
                Assert.False(m10.IsOverride);
                Assert.Null(i1.FindImplementationForInterfaceMember(m10));
            }

            static MethodSymbol getAccessor(EventSymbol e, bool addAccessor)
            {
                return addAccessor ? e.AddMethod : e.RemoveMethod;
            }
        }

        [Fact]
        public void EventModifiers_02()
        {
            var source1 =
@"#pragma warning disable CS0067 // The event is never used
public interface I1
{
    abstract static event D M01 { add {} remove {} }
    

    virtual static event D M02 { add {} remove {} }
    

    sealed static event D M03 { add {} remove {} }
    

    override static event D M04 { add {} remove {} }
    

    abstract virtual static event D M05 { add {} remove {} }
    

    abstract sealed static event D M06 { add {} remove {} }
    

    abstract override static event D M07 { add {} remove {} }
    

    virtual sealed static event D M08 { add {} remove {} }
    

    virtual override static event D M09 { add {} remove {} }
    

    sealed override static event D M10 { add {} remove {} }
}

public delegate void D();
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular9,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,29): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract static event D M01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "9.0", "preview").WithLocation(4, 29),
                // (4,33): error CS8712: 'I1.M01': abstract event cannot use event accessor syntax
                //     abstract static event D M01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M01").WithLocation(4, 33),
                // (7,28): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static event D M02 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 28),
                // (10,27): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed static event D M03 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "9.0", "preview").WithLocation(10, 27),
                // (13,29): error CS0106: The modifier 'override' is not valid for this item
                //     override static event D M04 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 29),
                // (16,37): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static event D M05 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 37),
                // (16,37): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract virtual static event D M05 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "9.0", "preview").WithLocation(16, 37),
                // (16,41): error CS8712: 'I1.M05': abstract event cannot use event accessor syntax
                //     abstract virtual static event D M05 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M05").WithLocation(16, 41),
                // (19,36): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static event D M06 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 36),
                // (19,36): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract sealed static event D M06 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "9.0", "preview").WithLocation(19, 36),
                // (19,40): error CS8712: 'I1.M06': abstract event cannot use event accessor syntax
                //     abstract sealed static event D M06 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M06").WithLocation(19, 40),
                // (22,38): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static event D M07 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 38),
                // (22,38): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract override static event D M07 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "9.0", "preview").WithLocation(22, 38),
                // (22,42): error CS8712: 'I1.M07': abstract event cannot use event accessor syntax
                //     abstract override static event D M07 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M07").WithLocation(22, 42),
                // (25,35): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static event D M08 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 35),
                // (25,35): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     virtual sealed static event D M08 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "9.0", "preview").WithLocation(25, 35),
                // (28,37): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static event D M09 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 37),
                // (28,37): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static event D M09 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 37),
                // (31,36): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static event D M10 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 36),
                // (31,36): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed override static event D M10 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "9.0", "preview").WithLocation(31, 36)
                );

            ValidateEventModifiers_01(compilation1);
        }

        [Fact]
        public void EventModifiers_03()
        {
            var source1 =
@"#pragma warning disable CS0067 // The event is never used
public interface I1
{
    abstract static event D M01
    ;

    virtual static event D M02
    ;

    sealed static event D M03
    ;

    override static event D M04
    ;

    abstract virtual static event D M05
    ;

    abstract sealed static event D M06
    ;

    abstract override static event D M07
    ;

    virtual sealed static event D M08
    ;

    virtual override static event D M09
    ;

    sealed override static event D M10
    ;
}

public delegate void D();
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (7,28): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static event D M02
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 28),
                // (13,29): error CS0106: The modifier 'override' is not valid for this item
                //     override static event D M04
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 29),
                // (16,37): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static event D M05
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 37),
                // (19,36): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static event D M06
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 36),
                // (22,38): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static event D M07
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 38),
                // (25,35): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static event D M08
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 35),
                // (28,37): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static event D M09
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 37),
                // (28,37): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static event D M09
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 37),
                // (31,36): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static event D M10
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 36)
                );

            ValidateEventModifiers_01(compilation1);
        }

        [Fact]
        public void EventModifiers_04()
        {
            var source1 =
@"#pragma warning disable CS0067 // The event is never used
public interface I1
{
    abstract static event D M01 { add {} remove {} }
    

    virtual static event D M02 { add {} remove {} }
    

    sealed static event D M03 { add {} remove {} }
    

    override static event D M04 { add {} remove {} }
    

    abstract virtual static event D M05 { add {} remove {} }
    

    abstract sealed static event D M06 { add {} remove {} }
    

    abstract override static event D M07 { add {} remove {} }
    

    virtual sealed static event D M08 { add {} remove {} }
    

    virtual override static event D M09 { add {} remove {} }
    

    sealed override static event D M10 { add {} remove {} }
}

public delegate void D();
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,33): error CS8712: 'I1.M01': abstract event cannot use event accessor syntax
                //     abstract static event D M01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M01").WithLocation(4, 33),
                // (7,28): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static event D M02 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 28),
                // (13,29): error CS0106: The modifier 'override' is not valid for this item
                //     override static event D M04 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 29),
                // (16,37): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static event D M05 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 37),
                // (16,41): error CS8712: 'I1.M05': abstract event cannot use event accessor syntax
                //     abstract virtual static event D M05 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M05").WithLocation(16, 41),
                // (19,36): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static event D M06 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 36),
                // (19,40): error CS8712: 'I1.M06': abstract event cannot use event accessor syntax
                //     abstract sealed static event D M06 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M06").WithLocation(19, 40),
                // (22,38): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static event D M07 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 38),
                // (22,42): error CS8712: 'I1.M07': abstract event cannot use event accessor syntax
                //     abstract override static event D M07 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M07").WithLocation(22, 42),
                // (25,35): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static event D M08 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 35),
                // (28,37): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static event D M09 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 37),
                // (28,37): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static event D M09 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 37),
                // (31,36): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static event D M10 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 36)
                );

            ValidateEventModifiers_01(compilation1);
        }

        [Fact]
        public void EventModifiers_05()
        {
            var source1 =
@"#pragma warning disable CS0067 // The event is never used
public interface I1
{
    abstract static event D M01
    ;

    virtual static event D M02
    ;

    sealed static event D M03
    ;

    override static event D M04
    ;

    abstract virtual static event D M05
    ;

    abstract sealed static event D M06
    ;

    abstract override static event D M07
    ;

    virtual sealed static event D M08
    ;

    virtual override static event D M09
    ;

    sealed override static event D M10
    ;
}

public delegate void D();
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular7_3,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,29): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract static event D M01
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "7.3", "preview").WithLocation(4, 29),
                // (7,28): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static event D M02
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 28),
                // (7,28): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     virtual static event D M02
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M02").WithArguments("static", "7.3", "8.0").WithLocation(7, 28),
                // (10,27): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed static event D M03
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "7.3", "preview").WithLocation(10, 27),
                // (13,29): error CS0106: The modifier 'override' is not valid for this item
                //     override static event D M04
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 29),
                // (13,29): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     override static event D M04
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M04").WithArguments("static", "7.3", "8.0").WithLocation(13, 29),
                // (16,37): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static event D M05
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 37),
                // (16,37): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract virtual static event D M05
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "7.3", "preview").WithLocation(16, 37),
                // (19,36): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static event D M06
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 36),
                // (19,36): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract sealed static event D M06
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "7.3", "preview").WithLocation(19, 36),
                // (22,38): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static event D M07
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 38),
                // (22,38): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract override static event D M07
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "7.3", "preview").WithLocation(22, 38),
                // (25,35): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static event D M08
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 35),
                // (25,35): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     virtual sealed static event D M08
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "7.3", "preview").WithLocation(25, 35),
                // (28,37): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static event D M09
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 37),
                // (28,37): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static event D M09
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 37),
                // (28,37): error CS8703: The modifier 'static' is not valid for this item in C# 7.3. Please use language version '8.0' or greater.
                //     virtual override static event D M09
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M09").WithArguments("static", "7.3", "8.0").WithLocation(28, 37),
                // (31,36): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static event D M10
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 36),
                // (31,36): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed override static event D M10
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "7.3", "preview").WithLocation(31, 36)
                );

            ValidateEventModifiers_01(compilation1);
        }

        [Fact]
        public void EventModifiers_06()
        {
            var source1 =
@"#pragma warning disable CS0067 // The event is never used
public interface I1
{
    abstract static event D M01 { add {} remove {} }
    

    virtual static event D M02 { add {} remove {} }
    

    sealed static event D M03 { add {} remove {} }
    

    override static event D M04 { add {} remove {} }
    

    abstract virtual static event D M05 { add {} remove {} }
    

    abstract sealed static event D M06 { add {} remove {} }
    

    abstract override static event D M07 { add {} remove {} }
    

    virtual sealed static event D M08 { add {} remove {} }
    

    virtual override static event D M09 { add {} remove {} }
    

    sealed override static event D M10 { add {} remove {} }
}

public delegate void D();
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular7_3,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,29): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract static event D M01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M01").WithArguments("abstract", "7.3", "preview").WithLocation(4, 29),
                // (4,33): error CS8712: 'I1.M01': abstract event cannot use event accessor syntax
                //     abstract static event D M01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M01").WithLocation(4, 33),
                // (7,28): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual static event D M02 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M02").WithArguments("virtual").WithLocation(7, 28),
                // (7,28): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual static event D M02 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M02").WithArguments("default interface implementation", "8.0").WithLocation(7, 28),
                // (10,27): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed static event D M03 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M03").WithArguments("sealed", "7.3", "preview").WithLocation(10, 27),
                // (13,29): error CS0106: The modifier 'override' is not valid for this item
                //     override static event D M04 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M04").WithArguments("override").WithLocation(13, 29),
                // (13,29): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     override static event D M04 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M04").WithArguments("default interface implementation", "8.0").WithLocation(13, 29),
                // (16,37): error CS0112: A static member cannot be marked as 'virtual'
                //     abstract virtual static event D M05 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M05").WithArguments("virtual").WithLocation(16, 37),
                // (16,37): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract virtual static event D M05 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M05").WithArguments("abstract", "7.3", "preview").WithLocation(16, 37),
                // (16,41): error CS8712: 'I1.M05': abstract event cannot use event accessor syntax
                //     abstract virtual static event D M05 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M05").WithLocation(16, 41),
                // (19,36): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static event D M06 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M06").WithArguments("sealed").WithLocation(19, 36),
                // (19,36): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract sealed static event D M06 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M06").WithArguments("abstract", "7.3", "preview").WithLocation(19, 36),
                // (19,40): error CS8712: 'I1.M06': abstract event cannot use event accessor syntax
                //     abstract sealed static event D M06 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M06").WithLocation(19, 40),
                // (22,38): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static event D M07 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M07").WithArguments("override").WithLocation(22, 38),
                // (22,38): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract override static event D M07 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M07").WithArguments("abstract", "7.3", "preview").WithLocation(22, 38),
                // (22,42): error CS8712: 'I1.M07': abstract event cannot use event accessor syntax
                //     abstract override static event D M07 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.M07").WithLocation(22, 42),
                // (25,35): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual sealed static event D M08 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M08").WithArguments("virtual").WithLocation(25, 35),
                // (25,35): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     virtual sealed static event D M08 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M08").WithArguments("sealed", "7.3", "preview").WithLocation(25, 35),
                // (28,37): error CS0112: A static member cannot be marked as 'virtual'
                //     virtual override static event D M09 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M09").WithArguments("virtual").WithLocation(28, 37),
                // (28,37): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static event D M09 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M09").WithArguments("override").WithLocation(28, 37),
                // (28,37): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual override static event D M09 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "M09").WithArguments("default interface implementation", "8.0").WithLocation(28, 37),
                // (31,36): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static event D M10 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M10").WithArguments("override").WithLocation(31, 36),
                // (31,36): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed override static event D M10 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M10").WithArguments("sealed", "7.3", "preview").WithLocation(31, 36)
                );

            ValidateEventModifiers_01(compilation1);
        }

        [Fact]
        public void OperatorModifiers_01()
        {
            var source1 =
@"
public interface I1
{
    abstract static I1 operator+ (I1 x)
    ; 

    virtual static I1 operator- (I1 x)
    ; 

    sealed static I1 operator++ (I1 x)
    ; 

    override static I1 operator-- (I1 x)
    ; 

    abstract virtual static I1 operator! (I1 x)
    ; 

    abstract sealed static I1 operator~ (I1 x)
    ; 

    abstract override static I1 operator+ (I1 x, I1 y)
    ; 

    virtual sealed static I1 operator- (I1 x, I1 y)
    ; 

    virtual override static I1 operator* (I1 x, I1 y) 
    ; 

    sealed override static I1 operator/ (I1 x, I1 y)
    ; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular9,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,32): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract static I1 operator+ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "+").WithArguments("abstract", "9.0", "preview").WithLocation(4, 32),
                // (7,31): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(7, 31),
                // (7,31): error CS0501: 'I1.operator -(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "-").WithArguments("I1.operator -(I1)").WithLocation(7, 31),
                // (10,30): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed static I1 operator++ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "++").WithArguments("sealed", "9.0", "preview").WithLocation(10, 30),
                // (10,30): error CS0501: 'I1.operator ++(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     sealed static I1 operator++ (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "++").WithArguments("I1.operator ++(I1)").WithLocation(10, 30),
                // (13,32): error CS0106: The modifier 'override' is not valid for this item
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "--").WithArguments("override").WithLocation(13, 32),
                // (13,32): error CS0501: 'I1.operator --(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "--").WithArguments("I1.operator --(I1)").WithLocation(13, 32),
                // (16,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "!").WithArguments("virtual").WithLocation(16, 40),
                // (16,40): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "!").WithArguments("abstract", "9.0", "preview").WithLocation(16, 40),
                // (19,39): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "~").WithArguments("sealed").WithLocation(19, 39),
                // (19,39): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "~").WithArguments("abstract", "9.0", "preview").WithLocation(19, 39),
                // (22,41): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("override").WithLocation(22, 41),
                // (22,41): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "+").WithArguments("abstract", "9.0", "preview").WithLocation(22, 41),
                // (25,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(25, 38),
                // (25,38): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "-").WithArguments("sealed", "9.0", "preview").WithLocation(25, 38),
                // (25,38): error CS0501: 'I1.operator -(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "-").WithArguments("I1.operator -(I1, I1)").WithLocation(25, 38),
                // (28,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("virtual").WithLocation(28, 40),
                // (28,40): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("override").WithLocation(28, 40),
                // (28,40): error CS0501: 'I1.operator *(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "*").WithArguments("I1.operator *(I1, I1)").WithLocation(28, 40),
                // (31,39): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "/").WithArguments("override").WithLocation(31, 39),
                // (31,39): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "/").WithArguments("sealed", "9.0", "preview").WithLocation(31, 39),
                // (31,39): error CS0501: 'I1.operator /(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "/").WithArguments("I1.operator /(I1, I1)").WithLocation(31, 39)
                );

            ValidateOperatorModifiers_01(compilation1);
        }

        private static void ValidateOperatorModifiers_01(CSharpCompilation compilation1)
        {
            var i1 = compilation1.GetTypeByMetadataName("I1");
            var m01 = i1.GetMember<MethodSymbol>("op_UnaryPlus");

            Assert.True(m01.IsAbstract);
            Assert.False(m01.IsVirtual);
            Assert.True(m01.IsMetadataVirtual());
            Assert.False(m01.IsSealed);
            Assert.True(m01.IsStatic);
            Assert.False(m01.IsExtern);
            Assert.False(m01.IsAsync);
            Assert.False(m01.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m01));

            var m02 = i1.GetMember<MethodSymbol>("op_UnaryNegation");

            Assert.False(m02.IsAbstract);
            Assert.False(m02.IsVirtual);
            Assert.False(m02.IsMetadataVirtual());
            Assert.False(m02.IsSealed);
            Assert.True(m02.IsStatic);
            Assert.False(m02.IsExtern);
            Assert.False(m02.IsAsync);
            Assert.False(m02.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m02));

            var m03 = i1.GetMember<MethodSymbol>("op_Increment");

            Assert.False(m03.IsAbstract);
            Assert.False(m03.IsVirtual);
            Assert.False(m03.IsMetadataVirtual());
            Assert.False(m03.IsSealed);
            Assert.True(m03.IsStatic);
            Assert.False(m03.IsExtern);
            Assert.False(m03.IsAsync);
            Assert.False(m03.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m03));

            var m04 = i1.GetMember<MethodSymbol>("op_Decrement");

            Assert.False(m04.IsAbstract);
            Assert.False(m04.IsVirtual);
            Assert.False(m04.IsMetadataVirtual());
            Assert.False(m04.IsSealed);
            Assert.True(m04.IsStatic);
            Assert.False(m04.IsExtern);
            Assert.False(m04.IsAsync);
            Assert.False(m04.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m04));

            var m05 = i1.GetMember<MethodSymbol>("op_LogicalNot");

            Assert.True(m05.IsAbstract);
            Assert.False(m05.IsVirtual);
            Assert.True(m05.IsMetadataVirtual());
            Assert.False(m05.IsSealed);
            Assert.True(m05.IsStatic);
            Assert.False(m05.IsExtern);
            Assert.False(m05.IsAsync);
            Assert.False(m05.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m05));

            var m06 = i1.GetMember<MethodSymbol>("op_OnesComplement");

            Assert.True(m06.IsAbstract);
            Assert.False(m06.IsVirtual);
            Assert.True(m06.IsMetadataVirtual());
            Assert.False(m06.IsSealed);
            Assert.True(m06.IsStatic);
            Assert.False(m06.IsExtern);
            Assert.False(m06.IsAsync);
            Assert.False(m06.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m06));

            var m07 = i1.GetMember<MethodSymbol>("op_Addition");

            Assert.True(m07.IsAbstract);
            Assert.False(m07.IsVirtual);
            Assert.True(m07.IsMetadataVirtual());
            Assert.False(m07.IsSealed);
            Assert.True(m07.IsStatic);
            Assert.False(m07.IsExtern);
            Assert.False(m07.IsAsync);
            Assert.False(m07.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m07));

            var m08 = i1.GetMember<MethodSymbol>("op_Subtraction");

            Assert.False(m08.IsAbstract);
            Assert.False(m08.IsVirtual);
            Assert.False(m08.IsMetadataVirtual());
            Assert.False(m08.IsSealed);
            Assert.True(m08.IsStatic);
            Assert.False(m08.IsExtern);
            Assert.False(m08.IsAsync);
            Assert.False(m08.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m08));

            var m09 = i1.GetMember<MethodSymbol>("op_Multiply");

            Assert.False(m09.IsAbstract);
            Assert.False(m09.IsVirtual);
            Assert.False(m09.IsMetadataVirtual());
            Assert.False(m09.IsSealed);
            Assert.True(m09.IsStatic);
            Assert.False(m09.IsExtern);
            Assert.False(m09.IsAsync);
            Assert.False(m09.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m09));

            var m10 = i1.GetMember<MethodSymbol>("op_Division");

            Assert.False(m10.IsAbstract);
            Assert.False(m10.IsVirtual);
            Assert.False(m10.IsMetadataVirtual());
            Assert.False(m10.IsSealed);
            Assert.True(m10.IsStatic);
            Assert.False(m10.IsExtern);
            Assert.False(m10.IsAsync);
            Assert.False(m10.IsOverride);
            Assert.Null(i1.FindImplementationForInterfaceMember(m10));
        }

        [Fact]
        public void OperatorModifiers_02()
        {
            var source1 =
@"
public interface I1
{
    abstract static I1 operator+ (I1 x)
    {throw null;} 

    virtual static I1 operator- (I1 x)
    {throw null;} 

    sealed static I1 operator++ (I1 x)
    {throw null;} 

    override static I1 operator-- (I1 x)
    {throw null;} 

    abstract virtual static I1 operator! (I1 x)
    {throw null;} 

    abstract sealed static I1 operator~ (I1 x)
    {throw null;} 

    abstract override static I1 operator+ (I1 x, I1 y)
    {throw null;} 

    virtual sealed static I1 operator- (I1 x, I1 y)
    {throw null;} 

    virtual override static I1 operator* (I1 x, I1 y) 
    {throw null;} 

    sealed override static I1 operator/ (I1 x, I1 y)
    {throw null;} 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular9,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,32): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract static I1 operator+ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "+").WithArguments("abstract", "9.0", "preview").WithLocation(4, 32),
                // (4,32): error CS0500: 'I1.operator +(I1)' cannot declare a body because it is marked abstract
                //     abstract static I1 operator+ (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "+").WithArguments("I1.operator +(I1)").WithLocation(4, 32),
                // (7,31): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(7, 31),
                // (10,30): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed static I1 operator++ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "++").WithArguments("sealed", "9.0", "preview").WithLocation(10, 30),
                // (13,32): error CS0106: The modifier 'override' is not valid for this item
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "--").WithArguments("override").WithLocation(13, 32),
                // (16,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "!").WithArguments("virtual").WithLocation(16, 40),
                // (16,40): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "!").WithArguments("abstract", "9.0", "preview").WithLocation(16, 40),
                // (16,40): error CS0500: 'I1.operator !(I1)' cannot declare a body because it is marked abstract
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "!").WithArguments("I1.operator !(I1)").WithLocation(16, 40),
                // (19,39): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "~").WithArguments("sealed").WithLocation(19, 39),
                // (19,39): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "~").WithArguments("abstract", "9.0", "preview").WithLocation(19, 39),
                // (19,39): error CS0500: 'I1.operator ~(I1)' cannot declare a body because it is marked abstract
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "~").WithArguments("I1.operator ~(I1)").WithLocation(19, 39),
                // (22,41): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("override").WithLocation(22, 41),
                // (22,41): error CS8703: The modifier 'abstract' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "+").WithArguments("abstract", "9.0", "preview").WithLocation(22, 41),
                // (22,41): error CS0500: 'I1.operator +(I1, I1)' cannot declare a body because it is marked abstract
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "+").WithArguments("I1.operator +(I1, I1)").WithLocation(22, 41),
                // (25,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(25, 38),
                // (25,38): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "-").WithArguments("sealed", "9.0", "preview").WithLocation(25, 38),
                // (28,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("virtual").WithLocation(28, 40),
                // (28,40): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("override").WithLocation(28, 40),
                // (31,39): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "/").WithArguments("override").WithLocation(31, 39),
                // (31,39): error CS8703: The modifier 'sealed' is not valid for this item in C# 9.0. Please use language version 'preview' or greater.
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "/").WithArguments("sealed", "9.0", "preview").WithLocation(31, 39)
                );

            ValidateOperatorModifiers_01(compilation1);
        }

        [Fact]
        public void OperatorModifiers_03()
        {
            var source1 =
@"
public interface I1
{
    abstract static I1 operator+ (I1 x)
    ; 

    virtual static I1 operator- (I1 x)
    ; 

    sealed static I1 operator++ (I1 x)
    ; 

    override static I1 operator-- (I1 x)
    ; 

    abstract virtual static I1 operator! (I1 x)
    ; 

    abstract sealed static I1 operator~ (I1 x)
    ; 

    abstract override static I1 operator+ (I1 x, I1 y)
    ; 

    virtual sealed static I1 operator- (I1 x, I1 y)
    ; 

    virtual override static I1 operator* (I1 x, I1 y) 
    ; 

    sealed override static I1 operator/ (I1 x, I1 y)
    ; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (7,31): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(7, 31),
                // (7,31): error CS0501: 'I1.operator -(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "-").WithArguments("I1.operator -(I1)").WithLocation(7, 31),
                // (10,30): error CS0501: 'I1.operator ++(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     sealed static I1 operator++ (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "++").WithArguments("I1.operator ++(I1)").WithLocation(10, 30),
                // (13,32): error CS0106: The modifier 'override' is not valid for this item
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "--").WithArguments("override").WithLocation(13, 32),
                // (13,32): error CS0501: 'I1.operator --(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "--").WithArguments("I1.operator --(I1)").WithLocation(13, 32),
                // (16,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "!").WithArguments("virtual").WithLocation(16, 40),
                // (19,39): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "~").WithArguments("sealed").WithLocation(19, 39),
                // (22,41): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("override").WithLocation(22, 41),
                // (25,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(25, 38),
                // (25,38): error CS0501: 'I1.operator -(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "-").WithArguments("I1.operator -(I1, I1)").WithLocation(25, 38),
                // (28,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("virtual").WithLocation(28, 40),
                // (28,40): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("override").WithLocation(28, 40),
                // (28,40): error CS0501: 'I1.operator *(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "*").WithArguments("I1.operator *(I1, I1)").WithLocation(28, 40),
                // (31,39): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "/").WithArguments("override").WithLocation(31, 39),
                // (31,39): error CS0501: 'I1.operator /(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "/").WithArguments("I1.operator /(I1, I1)").WithLocation(31, 39)
                );

            ValidateOperatorModifiers_01(compilation1);
        }

        [Fact]
        public void OperatorModifiers_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static I1 operator+ (I1 x)
    {throw null;} 

    virtual static I1 operator- (I1 x)
    {throw null;} 

    sealed static I1 operator++ (I1 x)
    {throw null;} 

    override static I1 operator-- (I1 x)
    {throw null;} 

    abstract virtual static I1 operator! (I1 x)
    {throw null;} 

    abstract sealed static I1 operator~ (I1 x)
    {throw null;} 

    abstract override static I1 operator+ (I1 x, I1 y)
    {throw null;} 

    virtual sealed static I1 operator- (I1 x, I1 y)
    {throw null;} 

    virtual override static I1 operator* (I1 x, I1 y) 
    {throw null;} 

    sealed override static I1 operator/ (I1 x, I1 y)
    {throw null;} 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,32): error CS0500: 'I1.operator +(I1)' cannot declare a body because it is marked abstract
                //     abstract static I1 operator+ (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "+").WithArguments("I1.operator +(I1)").WithLocation(4, 32),
                // (7,31): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(7, 31),
                // (13,32): error CS0106: The modifier 'override' is not valid for this item
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "--").WithArguments("override").WithLocation(13, 32),
                // (16,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "!").WithArguments("virtual").WithLocation(16, 40),
                // (16,40): error CS0500: 'I1.operator !(I1)' cannot declare a body because it is marked abstract
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "!").WithArguments("I1.operator !(I1)").WithLocation(16, 40),
                // (19,39): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "~").WithArguments("sealed").WithLocation(19, 39),
                // (19,39): error CS0500: 'I1.operator ~(I1)' cannot declare a body because it is marked abstract
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "~").WithArguments("I1.operator ~(I1)").WithLocation(19, 39),
                // (22,41): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("override").WithLocation(22, 41),
                // (22,41): error CS0500: 'I1.operator +(I1, I1)' cannot declare a body because it is marked abstract
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "+").WithArguments("I1.operator +(I1, I1)").WithLocation(22, 41),
                // (25,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(25, 38),
                // (28,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("virtual").WithLocation(28, 40),
                // (28,40): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("override").WithLocation(28, 40),
                // (31,39): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "/").WithArguments("override").WithLocation(31, 39)
                );

            ValidateOperatorModifiers_01(compilation1);
        }

        [Fact]
        public void OperatorModifiers_05()
        {
            var source1 =
@"
public interface I1
{
    abstract static I1 operator+ (I1 x)
    ; 

    virtual static I1 operator- (I1 x)
    ; 

    sealed static I1 operator++ (I1 x)
    ; 

    override static I1 operator-- (I1 x)
    ; 

    abstract virtual static I1 operator! (I1 x)
    ; 

    abstract sealed static I1 operator~ (I1 x)
    ; 

    abstract override static I1 operator+ (I1 x, I1 y)
    ; 

    virtual sealed static I1 operator- (I1 x, I1 y)
    ; 

    virtual override static I1 operator* (I1 x, I1 y) 
    ; 

    sealed override static I1 operator/ (I1 x, I1 y)
    ; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular7_3,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,32): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract static I1 operator+ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "+").WithArguments("abstract", "7.3", "preview").WithLocation(4, 32),
                // (7,31): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(7, 31),
                // (7,31): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "-").WithArguments("default interface implementation", "8.0").WithLocation(7, 31),
                // (7,31): error CS0501: 'I1.operator -(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "-").WithArguments("I1.operator -(I1)").WithLocation(7, 31),
                // (10,30): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed static I1 operator++ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "++").WithArguments("sealed", "7.3", "preview").WithLocation(10, 30),
                // (10,30): error CS0501: 'I1.operator ++(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     sealed static I1 operator++ (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "++").WithArguments("I1.operator ++(I1)").WithLocation(10, 30),
                // (13,32): error CS0106: The modifier 'override' is not valid for this item
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "--").WithArguments("override").WithLocation(13, 32),
                // (13,32): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "--").WithArguments("default interface implementation", "8.0").WithLocation(13, 32),
                // (13,32): error CS0501: 'I1.operator --(I1)' must declare a body because it is not marked abstract, extern, or partial
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "--").WithArguments("I1.operator --(I1)").WithLocation(13, 32),
                // (16,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "!").WithArguments("virtual").WithLocation(16, 40),
                // (16,40): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "!").WithArguments("abstract", "7.3", "preview").WithLocation(16, 40),
                // (19,39): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "~").WithArguments("sealed").WithLocation(19, 39),
                // (19,39): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "~").WithArguments("abstract", "7.3", "preview").WithLocation(19, 39),
                // (22,41): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("override").WithLocation(22, 41),
                // (22,41): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "+").WithArguments("abstract", "7.3", "preview").WithLocation(22, 41),
                // (25,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(25, 38),
                // (25,38): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "-").WithArguments("sealed", "7.3", "preview").WithLocation(25, 38),
                // (25,38): error CS0501: 'I1.operator -(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "-").WithArguments("I1.operator -(I1, I1)").WithLocation(25, 38),
                // (28,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("virtual").WithLocation(28, 40),
                // (28,40): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("override").WithLocation(28, 40),
                // (28,40): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "*").WithArguments("default interface implementation", "8.0").WithLocation(28, 40),
                // (28,40): error CS0501: 'I1.operator *(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "*").WithArguments("I1.operator *(I1, I1)").WithLocation(28, 40),
                // (31,39): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "/").WithArguments("override").WithLocation(31, 39),
                // (31,39): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "/").WithArguments("sealed", "7.3", "preview").WithLocation(31, 39),
                // (31,39): error CS0501: 'I1.operator /(I1, I1)' must declare a body because it is not marked abstract, extern, or partial
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "/").WithArguments("I1.operator /(I1, I1)").WithLocation(31, 39)
                );

            ValidateOperatorModifiers_01(compilation1);
        }

        [Fact]
        public void OperatorModifiers_06()
        {
            var source1 =
@"
public interface I1
{
    abstract static I1 operator+ (I1 x)
    {throw null;} 

    virtual static I1 operator- (I1 x)
    {throw null;} 

    sealed static I1 operator++ (I1 x)
    {throw null;} 

    override static I1 operator-- (I1 x)
    {throw null;} 

    abstract virtual static I1 operator! (I1 x)
    {throw null;} 

    abstract sealed static I1 operator~ (I1 x)
    {throw null;} 

    abstract override static I1 operator+ (I1 x, I1 y)
    {throw null;} 

    virtual sealed static I1 operator- (I1 x, I1 y)
    {throw null;} 

    virtual override static I1 operator* (I1 x, I1 y) 
    {throw null;} 

    sealed override static I1 operator/ (I1 x, I1 y)
    {throw null;} 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.Regular7_3,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,32): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract static I1 operator+ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "+").WithArguments("abstract", "7.3", "preview").WithLocation(4, 32),
                // (4,32): error CS0500: 'I1.operator +(I1)' cannot declare a body because it is marked abstract
                //     abstract static I1 operator+ (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "+").WithArguments("I1.operator +(I1)").WithLocation(4, 32),
                // (7,31): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(7, 31),
                // (7,31): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual static I1 operator- (I1 x)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "-").WithArguments("default interface implementation", "8.0").WithLocation(7, 31),
                // (10,30): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed static I1 operator++ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "++").WithArguments("sealed", "7.3", "preview").WithLocation(10, 30),
                // (13,32): error CS0106: The modifier 'override' is not valid for this item
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "--").WithArguments("override").WithLocation(13, 32),
                // (13,32): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     override static I1 operator-- (I1 x)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "--").WithArguments("default interface implementation", "8.0").WithLocation(13, 32),
                // (16,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "!").WithArguments("virtual").WithLocation(16, 40),
                // (16,40): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "!").WithArguments("abstract", "7.3", "preview").WithLocation(16, 40),
                // (16,40): error CS0500: 'I1.operator !(I1)' cannot declare a body because it is marked abstract
                //     abstract virtual static I1 operator! (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "!").WithArguments("I1.operator !(I1)").WithLocation(16, 40),
                // (19,39): error CS0106: The modifier 'sealed' is not valid for this item
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "~").WithArguments("sealed").WithLocation(19, 39),
                // (19,39): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "~").WithArguments("abstract", "7.3", "preview").WithLocation(19, 39),
                // (19,39): error CS0500: 'I1.operator ~(I1)' cannot declare a body because it is marked abstract
                //     abstract sealed static I1 operator~ (I1 x)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "~").WithArguments("I1.operator ~(I1)").WithLocation(19, 39),
                // (22,41): error CS0106: The modifier 'override' is not valid for this item
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("override").WithLocation(22, 41),
                // (22,41): error CS8703: The modifier 'abstract' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "+").WithArguments("abstract", "7.3", "preview").WithLocation(22, 41),
                // (22,41): error CS0500: 'I1.operator +(I1, I1)' cannot declare a body because it is marked abstract
                //     abstract override static I1 operator+ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "+").WithArguments("I1.operator +(I1, I1)").WithLocation(22, 41),
                // (25,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "-").WithArguments("virtual").WithLocation(25, 38),
                // (25,38): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     virtual sealed static I1 operator- (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "-").WithArguments("sealed", "7.3", "preview").WithLocation(25, 38),
                // (28,40): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("virtual").WithLocation(28, 40),
                // (28,40): error CS0106: The modifier 'override' is not valid for this item
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "*").WithArguments("override").WithLocation(28, 40),
                // (28,40): error CS8370: Feature 'default interface implementation' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     virtual override static I1 operator* (I1 x, I1 y) 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "*").WithArguments("default interface implementation", "8.0").WithLocation(28, 40),
                // (31,39): error CS0106: The modifier 'override' is not valid for this item
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "/").WithArguments("override").WithLocation(31, 39),
                // (31,39): error CS8703: The modifier 'sealed' is not valid for this item in C# 7.3. Please use language version 'preview' or greater.
                //     sealed override static I1 operator/ (I1 x, I1 y)
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "/").WithArguments("sealed", "7.3", "preview").WithLocation(31, 39)
                );

            ValidateOperatorModifiers_01(compilation1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OperatorModifiers_07(bool use7_3)
        {
            var source1 =
@"
public interface I1
{
    abstract static bool operator== (I1 x, I1 y); 

    abstract static bool operator!= (I1 x, I1 y) {return false;} 
}

public interface I2
{
    sealed static bool operator== (I2 x, I2 y) {return false;} 

    sealed static bool operator!= (I2 x, I2 y) {return false;} 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: use7_3 ? TestOptions.Regular7_3 : TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,34): error CS0567: Interfaces cannot contain conversion, equality, or inequality operators
                //     abstract static bool operator== (I1 x, I1 y); 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, "==").WithLocation(4, 34),
                // (6,34): error CS0567: Interfaces cannot contain conversion, equality, or inequality operators
                //     abstract static bool operator!= (I1 x, I1 y) {return false;} 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, "!=").WithLocation(6, 34),
                // (11,32): error CS0567: Interfaces cannot contain conversion, equality, or inequality operators
                //     sealed static bool operator== (I2 x, I2 y) {return false;} 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, "==").WithLocation(11, 32),
                // (13,32): error CS0567: Interfaces cannot contain conversion, equality, or inequality operators
                //     sealed static bool operator!= (I2 x, I2 y) {return false;} 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, "!=").WithLocation(13, 32)
                );
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OperatorModifiers_08(bool use7_3)
        {
            var source1 =
@"
public interface I1
{
    abstract static implicit operator int(I1 x); 

    abstract static explicit operator bool(I1 x) {return false;} 
}

public interface I2
{
    sealed static implicit operator int(I2 x) {return 0;} 

    sealed static explicit operator bool(I2 x) {return false;} 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: use7_3 ? TestOptions.Regular7_3 : TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,39): error CS0567: Interfaces cannot contain conversion, equality, or inequality operators
                //     abstract static implicit operator int(I1 x); 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, "int").WithLocation(4, 39),
                // (6,39): error CS0567: Interfaces cannot contain conversion, equality, or inequality operators
                //     abstract static explicit operator bool(I1 x) {return false;} 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, "bool").WithLocation(6, 39),
                // (11,37): error CS0567: Interfaces cannot contain conversion, equality, or inequality operators
                //     sealed static implicit operator int(I2 x) {return 0;} 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, "int").WithLocation(11, 37),
                // (13,37): error CS0567: Interfaces cannot contain conversion, equality, or inequality operators
                //     sealed static explicit operator bool(I2 x) {return false;} 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, "bool").WithLocation(13, 37)
                );
        }

        [Fact]
        public void FieldModifiers_01()
        {
            var source1 =
@"
public interface I1
{
    abstract static int F1; 
    sealed static int F2; 
    abstract int F3; 
    sealed int F4; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,25): error CS0681: The modifier 'abstract' is not valid on fields. Try using a property instead.
                //     abstract static int F1; 
                Diagnostic(ErrorCode.ERR_AbstractField, "F1").WithLocation(4, 25),
                // (5,23): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed static int F2; 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F2").WithArguments("sealed").WithLocation(5, 23),
                // (6,18): error CS0681: The modifier 'abstract' is not valid on fields. Try using a property instead.
                //     abstract int F3; 
                Diagnostic(ErrorCode.ERR_AbstractField, "F3").WithLocation(6, 18),
                // (6,18): error CS0525: Interfaces cannot contain instance fields
                //     abstract int F3; 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F3").WithLocation(6, 18),
                // (7,16): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed int F4; 
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "F4").WithArguments("sealed").WithLocation(7, 16),
                // (7,16): error CS0525: Interfaces cannot contain instance fields
                //     sealed int F4; 
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "F4").WithLocation(7, 16)
                );
        }

        [Fact]
        public void ExternAbstractStatic_01()
        {
            var source1 =
@"
interface I1
{
    extern abstract static void M01();
    extern abstract static bool P01 { get; }
    extern abstract static event System.Action E01;
    extern abstract static I1 operator+ (I1 x);
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,33): error CS0180: 'I1.M01()' cannot be both extern and abstract
                //     extern abstract static void M01();
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "M01").WithArguments("I1.M01()").WithLocation(4, 33),
                // (5,33): error CS0180: 'I1.P01' cannot be both extern and abstract
                //     extern abstract static bool P01 { get; }
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "P01").WithArguments("I1.P01").WithLocation(5, 33),
                // (6,48): error CS0180: 'I1.E01' cannot be both extern and abstract
                //     extern abstract static event System.Action E01;
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "E01").WithArguments("I1.E01").WithLocation(6, 48),
                // (7,39): error CS0180: 'I1.operator +(I1)' cannot be both extern and abstract
                //     extern abstract static I1 operator+ (I1 x);
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "+").WithArguments("I1.operator +(I1)").WithLocation(7, 39)
                );
        }

        [Fact]
        public void ExternAbstractStatic_02()
        {
            var source1 =
@"
interface I1
{
    extern abstract static void M01() {}
    extern abstract static bool P01 { get => false; }
    extern abstract static event System.Action E01 { add {} remove {} }
    extern abstract static I1 operator+ (I1 x) => null;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,33): error CS0180: 'I1.M01()' cannot be both extern and abstract
                //     extern abstract static void M01() {}
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "M01").WithArguments("I1.M01()").WithLocation(4, 33),
                // (5,33): error CS0180: 'I1.P01' cannot be both extern and abstract
                //     extern abstract static bool P01 { get => false; }
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "P01").WithArguments("I1.P01").WithLocation(5, 33),
                // (6,48): error CS0180: 'I1.E01' cannot be both extern and abstract
                //     extern abstract static event System.Action E01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "E01").WithArguments("I1.E01").WithLocation(6, 48),
                // (6,52): error CS8712: 'I1.E01': abstract event cannot use event accessor syntax
                //     extern abstract static event System.Action E01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("I1.E01").WithLocation(6, 52),
                // (7,39): error CS0180: 'I1.operator +(I1)' cannot be both extern and abstract
                //     extern abstract static I1 operator+ (I1 x) => null;
                Diagnostic(ErrorCode.ERR_AbstractAndExtern, "+").WithArguments("I1.operator +(I1)").WithLocation(7, 39)
                );
        }

        [Fact]
        public void ExternSealedStatic_01()
        {
            var source1 =
@"
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.

interface I1
{
    extern sealed static void M01();
    extern sealed static bool P01 { get; }
    extern sealed static event System.Action E01;
    extern sealed static I1 operator+ (I1 x);
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics();
        }

        [Fact]
        public void AbstractStaticInClass_01()
        {
            var source1 =
@"
abstract class C1
{
    public abstract static void M01();
    public abstract static bool P01 { get; }
    public abstract static event System.Action E01;
    public abstract static C1 operator+ (C1 x);
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,33): error CS0112: A static member cannot be marked as 'abstract'
                //     public abstract static void M01();
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M01").WithArguments("abstract").WithLocation(4, 33),
                // (5,33): error CS0112: A static member cannot be marked as 'abstract'
                //     public abstract static bool P01 { get; }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P01").WithArguments("abstract").WithLocation(5, 33),
                // (6,48): error CS0112: A static member cannot be marked as 'abstract'
                //     public abstract static event System.Action E01;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "E01").WithArguments("abstract").WithLocation(6, 48),
                // (7,39): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract static C1 operator+ (C1 x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("abstract").WithLocation(7, 39),
                // (7,39): error CS0501: 'C1.operator +(C1)' must declare a body because it is not marked abstract, extern, or partial
                //     public abstract static C1 operator+ (C1 x);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "+").WithArguments("C1.operator +(C1)").WithLocation(7, 39)
                );
        }

        [Fact]
        public void SealedStaticInClass_01()
        {
            var source1 =
@"
class C1
{
    sealed static void M01() {}
    sealed static bool P01 { get => false; }
    sealed static event System.Action E01 { add {} remove {} }
    public sealed static C1 operator+ (C1 x) => null;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,24): error CS0238: 'C1.M01()' cannot be sealed because it is not an override
                //     sealed static void M01() {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "M01").WithArguments("C1.M01()").WithLocation(4, 24),
                // (5,24): error CS0238: 'C1.P01' cannot be sealed because it is not an override
                //     sealed static bool P01 { get => false; }
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P01").WithArguments("C1.P01").WithLocation(5, 24),
                // (6,39): error CS0238: 'C1.E01' cannot be sealed because it is not an override
                //     sealed static event System.Action E01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "E01").WithArguments("C1.E01").WithLocation(6, 39),
                // (7,37): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed static C1 operator+ (C1 x) => null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("sealed").WithLocation(7, 37)
                );
        }

        [Fact]
        public void AbstractStaticInStruct_01()
        {
            var source1 =
@"
struct C1
{
    public abstract static void M01();
    public abstract static bool P01 { get; }
    public abstract static event System.Action E01;
    public abstract static C1 operator+ (C1 x);
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,33): error CS0112: A static member cannot be marked as 'abstract'
                //     public abstract static void M01();
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "M01").WithArguments("abstract").WithLocation(4, 33),
                // (5,33): error CS0112: A static member cannot be marked as 'abstract'
                //     public abstract static bool P01 { get; }
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "P01").WithArguments("abstract").WithLocation(5, 33),
                // (6,48): error CS0112: A static member cannot be marked as 'abstract'
                //     public abstract static event System.Action E01;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, "E01").WithArguments("abstract").WithLocation(6, 48),
                // (7,39): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract static C1 operator+ (C1 x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("abstract").WithLocation(7, 39),
                // (7,39): error CS0501: 'C1.operator +(C1)' must declare a body because it is not marked abstract, extern, or partial
                //     public abstract static C1 operator+ (C1 x);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "+").WithArguments("C1.operator +(C1)").WithLocation(7, 39)
                );
        }

        [Fact]
        public void SealedStaticInStruct_01()
        {
            var source1 =
@"
struct C1
{
    sealed static void M01() {}
    sealed static bool P01 { get => false; }
    sealed static event System.Action E01 { add {} remove {} }
    public sealed static C1 operator+ (C1 x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (4,24): error CS0238: 'C1.M01()' cannot be sealed because it is not an override
                //     sealed static void M01() {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "M01").WithArguments("C1.M01()").WithLocation(4, 24),
                // (5,24): error CS0238: 'C1.P01' cannot be sealed because it is not an override
                //     sealed static bool P01 { get => false; }
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P01").WithArguments("C1.P01").WithLocation(5, 24),
                // (6,39): error CS0238: 'C1.E01' cannot be sealed because it is not an override
                //     sealed static event System.Action E01 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "E01").WithArguments("C1.E01").WithLocation(6, 39),
                // (7,37): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed static C1 operator+ (C1 x) => default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("sealed").WithLocation(7, 37)
                );
        }

        [Fact]
        public void DefineAbstractStaticMethod_01()
        {
            var source1 =
@"
interface I1
{
    abstract static void M01();
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            CompileAndVerify(compilation1, sourceSymbolValidator: validate, symbolValidator: validate, verify: Verification.Skipped).VerifyDiagnostics();

            void validate(ModuleSymbol module)
            {
                var m01 = module.GlobalNamespace.GetTypeMember("I1").GetMembers().OfType<MethodSymbol>().Single();

                Assert.True(m01.IsMetadataNewSlot());
                Assert.True(m01.IsAbstract);
                Assert.True(m01.IsMetadataVirtual());
                Assert.False(m01.IsMetadataFinal);
                Assert.False(m01.IsVirtual);
                Assert.False(m01.IsSealed);
                Assert.True(m01.IsStatic);
                Assert.False(m01.IsOverride);
            }
        }

        [Fact]
        public void DefineAbstractStaticMethod_02()
        {
            var source1 =
@"
interface I1
{
    abstract static void M01();
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation1.VerifyDiagnostics(
                // (4,26): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static void M01();
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "M01").WithLocation(4, 26)
                );
        }

        [Theory]
        [InlineData("I1", "+", "(I1 x)")]
        [InlineData("I1", "-", "(I1 x)")]
        [InlineData("I1", "!", "(I1 x)")]
        [InlineData("I1", "~", "(I1 x)")]
        [InlineData("I1", "++", "(I1 x)")]
        [InlineData("I1", "--", "(I1 x)")]
        [InlineData("I1", "+", "(I1 x, I1 y)")]
        [InlineData("I1", "-", "(I1 x, I1 y)")]
        [InlineData("I1", "*", "(I1 x, I1 y)")]
        [InlineData("I1", "/", "(I1 x, I1 y)")]
        [InlineData("I1", "%", "(I1 x, I1 y)")]
        [InlineData("I1", "&", "(I1 x, I1 y)")]
        [InlineData("I1", "|", "(I1 x, I1 y)")]
        [InlineData("I1", "^", "(I1 x, I1 y)")]
        [InlineData("I1", "<<", "(I1 x, int y)")]
        [InlineData("I1", ">>", "(I1 x, int y)")]
        public void DefineAbstractStaticOperator_01(string type, string op, string paramList)
        {
            var source1 =
@"
interface I1
{
    abstract static " + type + " operator " + op + " " + paramList + @";
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            CompileAndVerify(compilation1, sourceSymbolValidator: validate, symbolValidator: validate, verify: Verification.Skipped).VerifyDiagnostics();

            void validate(ModuleSymbol module)
            {
                var m01 = module.GlobalNamespace.GetTypeMember("I1").GetMembers().OfType<MethodSymbol>().Single();

                Assert.True(m01.IsMetadataNewSlot());
                Assert.True(m01.IsAbstract);
                Assert.True(m01.IsMetadataVirtual());
                Assert.False(m01.IsMetadataFinal);
                Assert.False(m01.IsVirtual);
                Assert.False(m01.IsSealed);
                Assert.True(m01.IsStatic);
                Assert.False(m01.IsOverride);
            }
        }

        [Fact]
        public void DefineAbstractStaticOperator_02()
        {
            var source1 =
@"
interface I1
{
    abstract static bool operator true (I1 x);
    abstract static bool operator false (I1 x);
    abstract static I1 operator > (I1 x, I1 y);
    abstract static I1 operator < (I1 x, I1 y);
    abstract static I1 operator >= (I1 x, I1 y);
    abstract static I1 operator <= (I1 x, I1 y);
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            CompileAndVerify(compilation1, sourceSymbolValidator: validate, symbolValidator: validate, verify: Verification.Skipped).VerifyDiagnostics();

            void validate(ModuleSymbol module)
            {
                int count = 0;
                foreach (var m01 in module.GlobalNamespace.GetTypeMember("I1").GetMembers().OfType<MethodSymbol>())
                {
                    Assert.True(m01.IsMetadataNewSlot());
                    Assert.True(m01.IsAbstract);
                    Assert.True(m01.IsMetadataVirtual());
                    Assert.False(m01.IsMetadataFinal);
                    Assert.False(m01.IsVirtual);
                    Assert.False(m01.IsSealed);
                    Assert.True(m01.IsStatic);
                    Assert.False(m01.IsOverride);

                    count++;
                }

                Assert.Equal(6, count);
            }
        }

        [Theory]
        [InlineData("I1", "+", "(I1 x)")]
        [InlineData("I1", "-", "(I1 x)")]
        [InlineData("I1", "!", "(I1 x)")]
        [InlineData("I1", "~", "(I1 x)")]
        [InlineData("I1", "++", "(I1 x)")]
        [InlineData("I1", "--", "(I1 x)")]
        [InlineData("I1", "+", "(I1 x, I1 y)")]
        [InlineData("I1", "-", "(I1 x, I1 y)")]
        [InlineData("I1", "*", "(I1 x, I1 y)")]
        [InlineData("I1", "/", "(I1 x, I1 y)")]
        [InlineData("I1", "%", "(I1 x, I1 y)")]
        [InlineData("I1", "&", "(I1 x, I1 y)")]
        [InlineData("I1", "|", "(I1 x, I1 y)")]
        [InlineData("I1", "^", "(I1 x, I1 y)")]
        [InlineData("I1", "<<", "(I1 x, int y)")]
        [InlineData("I1", ">>", "(I1 x, int y)")]
        public void DefineAbstractStaticOperator_03(string type, string op, string paramList)
        {
            var source1 =
@"
interface I1
{
    abstract static " + type + " operator " + op + " " + paramList + @";
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation1.VerifyDiagnostics(
                // (4,33): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static I1 operator + (I1 x);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, op).WithLocation(4, 31 + type.Length)
                );
        }

        [Fact]
        public void DefineAbstractStaticOperator_04()
        {
            var source1 =
@"
interface I1
{
    abstract static bool operator true (I1 x);
    abstract static bool operator false (I1 x);
    abstract static I1 operator > (I1 x, I1 y);
    abstract static I1 operator < (I1 x, I1 y);
    abstract static I1 operator >= (I1 x, I1 y);
    abstract static I1 operator <= (I1 x, I1 y);
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation1.VerifyDiagnostics(
                // (4,35): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static bool operator true (I1 x);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "true").WithLocation(4, 35),
                // (5,35): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static bool operator false (I1 x);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "false").WithLocation(5, 35),
                // (6,33): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static I1 operator > (I1 x, I1 y);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, ">").WithLocation(6, 33),
                // (7,33): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static I1 operator < (I1 x, I1 y);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "<").WithLocation(7, 33),
                // (8,33): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static I1 operator >= (I1 x, I1 y);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, ">=").WithLocation(8, 33),
                // (9,33): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static I1 operator <= (I1 x, I1 y);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "<=").WithLocation(9, 33)
                );
        }

        [Fact]
        public void DefineAbstractStaticProperty_01()
        {
            var source1 =
@"
interface I1
{
    abstract static int P01 { get; set; }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            CompileAndVerify(compilation1, sourceSymbolValidator: validate, symbolValidator: validate, verify: Verification.Skipped).VerifyDiagnostics();

            void validate(ModuleSymbol module)
            {
                var p01 = module.GlobalNamespace.GetTypeMember("I1").GetMembers().OfType<PropertySymbol>().Single();

                Assert.True(p01.IsAbstract);
                Assert.False(p01.IsVirtual);
                Assert.False(p01.IsSealed);
                Assert.True(p01.IsStatic);
                Assert.False(p01.IsOverride);

                int count = 0;
                foreach (var m01 in module.GlobalNamespace.GetTypeMember("I1").GetMembers().OfType<MethodSymbol>())
                {
                    Assert.True(m01.IsMetadataNewSlot());
                    Assert.True(m01.IsAbstract);
                    Assert.True(m01.IsMetadataVirtual());
                    Assert.False(m01.IsMetadataFinal);
                    Assert.False(m01.IsVirtual);
                    Assert.False(m01.IsSealed);
                    Assert.True(m01.IsStatic);
                    Assert.False(m01.IsOverride);

                    count++;
                }

                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void DefineAbstractStaticProperty_02()
        {
            var source1 =
@"
interface I1
{
    abstract static int P01 { get; set; }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation1.VerifyDiagnostics(
                // (4,31): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static int P01 { get; set; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "get").WithLocation(4, 31),
                // (4,36): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static int P01 { get; set; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "set").WithLocation(4, 36)
                );
        }

        [Fact]
        public void DefineAbstractStaticEvent_01()
        {
            var source1 =
@"
interface I1
{
    abstract static event System.Action E01;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            CompileAndVerify(compilation1, sourceSymbolValidator: validate, symbolValidator: validate, verify: Verification.Skipped).VerifyDiagnostics();

            void validate(ModuleSymbol module)
            {
                var e01 = module.GlobalNamespace.GetTypeMember("I1").GetMembers().OfType<EventSymbol>().Single();

                Assert.True(e01.IsAbstract);
                Assert.False(e01.IsVirtual);
                Assert.False(e01.IsSealed);
                Assert.True(e01.IsStatic);
                Assert.False(e01.IsOverride);

                int count = 0;
                foreach (var m01 in module.GlobalNamespace.GetTypeMember("I1").GetMembers().OfType<MethodSymbol>())
                {
                    Assert.True(m01.IsMetadataNewSlot());
                    Assert.True(m01.IsAbstract);
                    Assert.True(m01.IsMetadataVirtual());
                    Assert.False(m01.IsMetadataFinal);
                    Assert.False(m01.IsVirtual);
                    Assert.False(m01.IsSealed);
                    Assert.True(m01.IsStatic);
                    Assert.False(m01.IsOverride);

                    count++;
                }

                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void DefineAbstractStaticEvent_02()
        {
            var source1 =
@"
interface I1
{
    abstract static event System.Action E01;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation1.VerifyDiagnostics(
                // (4,41): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static event System.Action E01;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "E01").WithLocation(4, 41)
                );
        }

        [Fact]
        public void ConstraintChecks_01()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01();
}

public interface I2 : I1
{
}

public interface I3 : I2
{
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var source2 =
@"
class C1<T1> where T1 : I1
{
    void Test(C1<I2> x)
    {
    }
}

class C2
{
    void M<T2>() where T2 : I1 {}

    void Test(C2 x)
    {
        x.M<I2>();
    }
}

class C3<T3> where T3 : I2
{
    void Test(C3<I2> x, C3<I3> y)
    {
    }
}

class C4
{
    void M<T4>() where T4 : I2 {}

    void Test(C4 x)
    {
        x.M<I2>();
        x.M<I3>();
    }
}

class C5<T5> where T5 : I3
{
    void Test(C5<I3> y)
    {
    }
}

class C6
{
    void M<T6>() where T6 : I3 {}

    void Test(C6 x)
    {
        x.M<I3>();
    }
}

class C7<T7> where T7 : I1
{
    void Test(C7<I1> y)
    {
    }
}

class C8
{
    void M<T8>() where T8 : I1 {}

    void Test(C8 x)
    {
        x.M<I1>();
    }
}
";
            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp,
                                                 references: new[] { compilation1.ToMetadataReference() });

            var expected = new[] {
                // (4,22): error CS9101: The interface 'I2' cannot be used as type parameter 'T1' in the generic type or method 'C1<T1>'. The constraint interface 'I1' or its base interface has static abstract members.
                //     void Test(C1<I2> x)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "x").WithArguments("C1<T1>", "I1", "T1", "I2").WithLocation(4, 22),
                // (15,11): error CS9101: The interface 'I2' cannot be used as type parameter 'T2' in the generic type or method 'C2.M<T2>()'. The constraint interface 'I1' or its base interface has static abstract members.
                //         x.M<I2>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "M<I2>").WithArguments("C2.M<T2>()", "I1", "T2", "I2").WithLocation(15, 11),
                // (21,22): error CS9101: The interface 'I2' cannot be used as type parameter 'T3' in the generic type or method 'C3<T3>'. The constraint interface 'I2' or its base interface has static abstract members.
                //     void Test(C3<I2> x, C3<I3> y)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "x").WithArguments("C3<T3>", "I2", "T3", "I2").WithLocation(21, 22),
                // (21,32): error CS9101: The interface 'I3' cannot be used as type parameter 'T3' in the generic type or method 'C3<T3>'. The constraint interface 'I2' or its base interface has static abstract members.
                //     void Test(C3<I2> x, C3<I3> y)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "y").WithArguments("C3<T3>", "I2", "T3", "I3").WithLocation(21, 32),
                // (32,11): error CS9101: The interface 'I2' cannot be used as type parameter 'T4' in the generic type or method 'C4.M<T4>()'. The constraint interface 'I2' or its base interface has static abstract members.
                //         x.M<I2>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "M<I2>").WithArguments("C4.M<T4>()", "I2", "T4", "I2").WithLocation(32, 11),
                // (33,11): error CS9101: The interface 'I3' cannot be used as type parameter 'T4' in the generic type or method 'C4.M<T4>()'. The constraint interface 'I2' or its base interface has static abstract members.
                //         x.M<I3>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "M<I3>").WithArguments("C4.M<T4>()", "I2", "T4", "I3").WithLocation(33, 11),
                // (39,22): error CS9101: The interface 'I3' cannot be used as type parameter 'T5' in the generic type or method 'C5<T5>'. The constraint interface 'I3' or its base interface has static abstract members.
                //     void Test(C5<I3> y)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "y").WithArguments("C5<T5>", "I3", "T5", "I3").WithLocation(39, 22),
                // (50,11): error CS9101: The interface 'I3' cannot be used as type parameter 'T6' in the generic type or method 'C6.M<T6>()'. The constraint interface 'I3' or its base interface has static abstract members.
                //         x.M<I3>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "M<I3>").WithArguments("C6.M<T6>()", "I3", "T6", "I3").WithLocation(50, 11),
                // (56,22): error CS9101: The interface 'I1' cannot be used as type parameter 'T7' in the generic type or method 'C7<T7>'. The constraint interface 'I1' or its base interface has static abstract members.
                //     void Test(C7<I1> y)
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "y").WithArguments("C7<T7>", "I1", "T7", "I1").WithLocation(56, 22),
                // (67,11): error CS9101: The interface 'I1' cannot be used as type parameter 'T8' in the generic type or method 'C8.M<T8>()'. The constraint interface 'I1' or its base interface has static abstract members.
                //         x.M<I1>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, "M<I1>").WithArguments("C8.M<T8>()", "I1", "T8", "I1").WithLocation(67, 11)
            };

            compilation2.VerifyDiagnostics(expected);

            compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp,
                                                 references: new[] { compilation1.EmitToImageReference() });

            compilation2.VerifyDiagnostics(expected);
        }

        [Fact]
        public void ConstraintChecks_02()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01();
}

public class C : I1
{
    public static void M01() {}
}

public struct S : I1
{
    public static void M01() {}
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var source2 =
@"
class C1<T1> where T1 : I1
{
    void Test(C1<C> x, C1<S> y, C1<T1> z)
    {
    }
}

class C2
{
    public void M<T2>(C2 x) where T2 : I1
    {
        x.M<T2>(x);
    }

    void Test(C2 x)
    {
        x.M<C>(x);
        x.M<S>(x);
    }
}

class C3<T3> where T3 : I1
{
    void Test(C1<T3> z)
    {
    }
}

class C4
{
    void M<T4>(C2 x) where T4 : I1
    {
        x.M<T4>(x);
    }
}

class C5<T5>
{
    internal virtual void M<U5>() where U5 : T5 { }
}

class C6 : C5<I1>
{
    internal override void M<U6>() { base.M<U6>(); }
}
";
            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyEmitDiagnostics();

            compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp,
                                                 references: new[] { compilation1.EmitToImageReference() });

            compilation2.VerifyEmitDiagnostics();
        }

        [Fact]
        public void VarianceSafety_01()
        {
            var source1 =
@"
interface I2<out T1, in T2>
{
    abstract static T1 P1 { get; }
    abstract static T2 P2 { get; }
    abstract static T1 P3 { set; }
    abstract static T2 P4 { set; }
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.VerifyDiagnostics(
                // (5,21): error CS1961: Invalid variance: The type parameter 'T2' must be covariantly valid on 'I2<T1, T2>.P2'. 'T2' is contravariant.
                //     abstract static T2 P2 { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T2").WithArguments("I2<T1, T2>.P2", "T2", "contravariant", "covariantly").WithLocation(5, 21),
                // (6,21): error CS1961: Invalid variance: The type parameter 'T1' must be contravariantly valid on 'I2<T1, T2>.P3'. 'T1' is covariant.
                //     abstract static T1 P3 { set; }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T1").WithArguments("I2<T1, T2>.P3", "T1", "covariant", "contravariantly").WithLocation(6, 21)
                );
        }

        [Fact]
        public void VarianceSafety_02()
        {
            var source1 =
@"
interface I2<out T1, in T2>
{
    abstract static T1 M1();
    abstract static T2 M2();
    abstract static void M3(T1 x);
    abstract static void M4(T2 x);
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.VerifyDiagnostics(
                // (5,21): error CS1961: Invalid variance: The type parameter 'T2' must be covariantly valid on 'I2<T1, T2>.M2()'. 'T2' is contravariant.
                //     abstract static T2 M2();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T2").WithArguments("I2<T1, T2>.M2()", "T2", "contravariant", "covariantly").WithLocation(5, 21),
                // (6,29): error CS1961: Invalid variance: The type parameter 'T1' must be contravariantly valid on 'I2<T1, T2>.M3(T1)'. 'T1' is covariant.
                //     abstract static void M3(T1 x);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "T1").WithArguments("I2<T1, T2>.M3(T1)", "T1", "covariant", "contravariantly").WithLocation(6, 29)
                );
        }

        [Fact]
        public void VarianceSafety_03()
        {
            var source1 =
@"
interface I2<out T1, in T2>
{
    abstract static event System.Action<System.Func<T1>> E1;
    abstract static event System.Action<System.Func<T2>> E2;
    abstract static event System.Action<System.Action<T1>> E3;
    abstract static event System.Action<System.Action<T2>> E4;
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.VerifyDiagnostics(
                // (5,58): error CS1961: Invalid variance: The type parameter 'T2' must be covariantly valid on 'I2<T1, T2>.E2'. 'T2' is contravariant.
                //     abstract static event System.Action<System.Func<T2>> E2;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "E2").WithArguments("I2<T1, T2>.E2", "T2", "contravariant", "covariantly").WithLocation(5, 58),
                // (6,60): error CS1961: Invalid variance: The type parameter 'T1' must be contravariantly valid on 'I2<T1, T2>.E3'. 'T1' is covariant.
                //     abstract static event System.Action<System.Action<T1>> E3;
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "E3").WithArguments("I2<T1, T2>.E3", "T1", "covariant", "contravariantly").WithLocation(6, 60)
                );
        }

        [Fact]
        public void VarianceSafety_04()
        {
            var source1 =
@"
interface I2<out T2>
{
    abstract static int operator +(I2<T2> x);
}

interface I3<out T3>
{
    abstract static int operator +(I3<T3> x);
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.VerifyDiagnostics(
                // (4,36): error CS1961: Invalid variance: The type parameter 'T2' must be contravariantly valid on 'I2<T2>.operator +(I2<T2>)'. 'T2' is covariant.
                //     abstract static int operator +(I2<T2> x);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "I2<T2>").WithArguments("I2<T2>.operator +(I2<T2>)", "T2", "covariant", "contravariantly").WithLocation(4, 36),
                // (9,36): error CS1961: Invalid variance: The type parameter 'T3' must be contravariantly valid on 'I3<T3>.operator +(I3<T3>)'. 'T3' is covariant.
                //     abstract static int operator +(I3<T3> x);
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "I3<T3>").WithArguments("I3<T3>.operator +(I3<T3>)", "T3", "covariant", "contravariantly").WithLocation(9, 36)
                );
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("!")]
        [InlineData("~")]
        [InlineData("true")]
        [InlineData("false")]
        public void OperatorSignature_01(string op)
        {
            var source1 =
@"
interface I1<T1> where T1 : I1<T1>
{
    static bool operator " + op + @"(T1 x) => throw null;
}

interface I2<T2> where T2 : struct, I2<T2>
{
    static bool operator " + op + @"(T2? x) => throw null;
}

interface I3<T3> where T3 : I3<T3>
{
    static abstract bool operator " + op + @"(T3 x);
}

interface I4<T4> where T4 : struct, I4<T4>
{
    static abstract bool operator " + op + @"(T4? x);
}

class C5<T5> where T5 : C5<T5>.I6
{
    public interface I6
    {
        static abstract bool operator " + op + @"(T5 x);
    }
}

interface I7<T71, T72> where T72 : I7<T71, T72> where T71 : T72
{
    static abstract bool operator " + op + @"(T71 x);
}

interface I8<T8> where T8 : I9<T8>
{
    static abstract bool operator " + op + @"(T8 x);
}

interface I9<T9> : I8<T9> where T9 : I9<T9> {}

interface I10<T10> where T10 : C11<T10>
{
    static abstract bool operator " + op + @"(T10 x);
}

class C11<T11> : I10<T11> where T11 : C11<T11> {}

interface I12
{
    static abstract bool operator " + op + @"(int x);
}

interface I13
{
    static abstract bool operator " + op + @"(I13 x);
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_OperatorNeedsMatch).Verify(
                // (4,26): error CS0562: The parameter of a unary operator must be the containing type
                //     static bool operator +(T1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadUnaryOperatorSignature, op).WithLocation(4, 26),
                // (9,26): error CS0562: The parameter of a unary operator must be the containing type
                //     static bool operator +(T2? x) => throw null;
                Diagnostic(ErrorCode.ERR_BadUnaryOperatorSignature, op).WithLocation(9, 26),
                // (26,39): error CS9102: The parameter of a unary operator must be the containing type, or its type parameter constrained to it.
                //         static abstract bool operator +(T5 x);
                Diagnostic(ErrorCode.ERR_BadAbstractUnaryOperatorSignature, op).WithLocation(26, 39),
                // (32,35): error CS9102: The parameter of a unary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(T71 x);
                Diagnostic(ErrorCode.ERR_BadAbstractUnaryOperatorSignature, op).WithLocation(32, 35),
                // (37,35): error CS9102: The parameter of a unary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(T8 x);
                Diagnostic(ErrorCode.ERR_BadAbstractUnaryOperatorSignature, op).WithLocation(37, 35),
                // (44,35): error CS9102: The parameter of a unary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(T10 x);
                Diagnostic(ErrorCode.ERR_BadAbstractUnaryOperatorSignature, op).WithLocation(44, 35),
                // (51,35): error CS9102: The parameter of a unary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator false(int x);
                Diagnostic(ErrorCode.ERR_BadAbstractUnaryOperatorSignature, op).WithLocation(51, 35)
                );
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void OperatorSignature_02(string op)
        {
            var source1 =
@"
interface I1<T1> where T1 : I1<T1>
{
    static T1 operator " + op + @"(T1 x) => throw null;
}

interface I2<T2> where T2 : struct, I2<T2>
{
    static T2? operator " + op + @"(T2? x) => throw null;
}

interface I3<T3> where T3 : I3<T3>
{
    static abstract T3 operator " + op + @"(T3 x);
}

interface I4<T4> where T4 : struct, I4<T4>
{
    static abstract T4? operator " + op + @"(T4? x);
}

class C5<T5> where T5 : C5<T5>.I6
{
    public interface I6
    {
        static abstract T5 operator " + op + @"(T5 x);
    }
}

interface I7<T71, T72> where T72 : I7<T71, T72> where T71 : T72
{
    static abstract T71 operator " + op + @"(T71 x);
}

interface I8<T8> where T8 : I9<T8>
{
    static abstract T8 operator " + op + @"(T8 x);
}

interface I9<T9> : I8<T9> where T9 : I9<T9> {}

interface I10<T10> where T10 : C11<T10>
{
    static abstract T10 operator " + op + @"(T10 x);
}

class C11<T11> : I10<T11> where T11 : C11<T11> {}

interface I12
{
    static abstract int operator " + op + @"(int x);
}

interface I13
{
    static abstract I13 operator " + op + @"(I13 x);
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.VerifyDiagnostics(
                // (4,24): error CS0559: The parameter type for ++ or -- operator must be the containing type
                //     static T1 operator ++(T1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadIncDecSignature, op).WithLocation(4, 24),
                // (9,25): error CS0559: The parameter type for ++ or -- operator must be the containing type
                //     static T2? operator ++(T2? x) => throw null;
                Diagnostic(ErrorCode.ERR_BadIncDecSignature, op).WithLocation(9, 25),
                // (26,37): error CS9103: The parameter type for ++ or -- operator must be the containing type, or its type parameter constrained to it.
                //         static abstract T5 operator ++(T5 x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecSignature, op).WithLocation(26, 37),
                // (32,34): error CS9103: The parameter type for ++ or -- operator must be the containing type, or its type parameter constrained to it.
                //     static abstract T71 operator ++(T71 x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecSignature, op).WithLocation(32, 34),
                // (37,33): error CS9103: The parameter type for ++ or -- operator must be the containing type, or its type parameter constrained to it.
                //     static abstract T8 operator ++(T8 x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecSignature, op).WithLocation(37, 33),
                // (44,34): error CS9103: The parameter type for ++ or -- operator must be the containing type, or its type parameter constrained to it.
                //     static abstract T10 operator ++(T10 x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecSignature, op).WithLocation(44, 34),
                // (51,34): error CS9103: The parameter type for ++ or -- operator must be the containing type, or its type parameter constrained to it.
                //     static abstract int operator ++(int x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecSignature, op).WithLocation(51, 34)
                );
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void OperatorSignature_03(string op)
        {
            var source1 =
@"
interface I1<T1> where T1 : I1<T1>
{
    static T1 operator " + op + @"(I1<T1> x) => throw null;
}

interface I2<T2> where T2 : struct, I2<T2>
{
    static T2? operator " + op + @"(I2<T2> x) => throw null;
}

interface I3<T3> where T3 : I3<T3>
{
    static abstract T3 operator " + op + @"(I3<T3> x);
}

interface I4<T4> where T4 : struct, I4<T4>
{
    static abstract T4? operator " + op + @"(I4<T4> x);
}

class C5<T5> where T5 : C5<T5>.I6
{
    public interface I6
    {
        static abstract T5 operator " + op + @"(I6 x);
    }
}

interface I7<T71, T72> where T72 : I7<T71, T72> where T71 : T72
{
    static abstract T71 operator " + op + @"(I7<T71, T72> x);
}

interface I8<T8> where T8 : I9<T8>
{
    static abstract T8 operator " + op + @"(I8<T8> x);
}

interface I9<T9> : I8<T9> where T9 : I9<T9> {}

interface I10<T10> where T10 : C11<T10>
{
    static abstract T10 operator " + op + @"(I10<T10> x);
}

class C11<T11> : I10<T11> where T11 : C11<T11> {}

interface I12
{
    static abstract int operator " + op + @"(I12 x);
}

interface I13<T13> where T13 : struct, I13<T13>
{
    static abstract T13? operator " + op + @"(T13 x);
}

interface I14<T14> where T14 : struct, I14<T14>
{
    static abstract T14 operator " + op + @"(T14? x);
}

interface I15<T151, T152> where T151 : I15<T151, T152> where T152 : I15<T151, T152>
{
    static abstract T151 operator " + op + @"(T152 x);
}

";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.VerifyDiagnostics(
                // (4,24): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //     static T1 operator ++(I1<T1> x) => throw null;
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(4, 24),
                // (9,25): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //     static T2? operator ++(I2<T2> x) => throw null;
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(9, 25),
                // (19,34): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //     static abstract T4? operator ++(I4<T4> x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(19, 34),
                // (26,37): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //         static abstract T5 operator ++(I6 x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(26, 37),
                // (32,34): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //     static abstract T71 operator ++(I7<T71, T72> x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(32, 34),
                // (37,33): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //     static abstract T8 operator ++(I8<T8> x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(37, 33),
                // (44,34): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //     static abstract T10 operator ++(I10<T10> x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(44, 34),
                // (51,34): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //     static abstract int operator ++(I12 x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(51, 34),
                // (56,35): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //     static abstract T13? operator ++(T13 x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(56, 35),
                // (61,34): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //     static abstract T14 operator ++(T14? x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(61, 34),
                // (66,35): error CS9104: The return type for ++ or -- operator must either match the parameter type, or be derived from the parameter type, or be the containing type's type parameter constrained to it unless the parameter type is a different type parameter.
                //     static abstract T151 operator ++(T152 x);
                Diagnostic(ErrorCode.ERR_BadAbstractIncDecRetType, op).WithLocation(66, 35)
                );
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<")]
        [InlineData(">")]
        [InlineData("<=")]
        [InlineData(">=")]
        public void OperatorSignature_04(string op)
        {
            var source1 =
@"
interface I1<T1> where T1 : I1<T1>
{
    static bool operator " + op + @"(T1 x, bool y) => throw null;
}

interface I2<T2> where T2 : struct, I2<T2>
{
    static bool operator " + op + @"(T2? x, bool y) => throw null;
}

interface I3<T3> where T3 : I3<T3>
{
    static abstract bool operator " + op + @"(T3 x, bool y);
}

interface I4<T4> where T4 : struct, I4<T4>
{
    static abstract bool operator " + op + @"(T4? x, bool y);
}

class C5<T5> where T5 : C5<T5>.I6
{
    public interface I6
    {
        static abstract bool operator " + op + @"(T5 x, bool y);
    }
}

interface I7<T71, T72> where T72 : I7<T71, T72> where T71 : T72
{
    static abstract bool operator " + op + @"(T71 x, bool y);
}

interface I8<T8> where T8 : I9<T8>
{
    static abstract bool operator " + op + @"(T8 x, bool y);
}

interface I9<T9> : I8<T9> where T9 : I9<T9> {}

interface I10<T10> where T10 : C11<T10>
{
    static abstract bool operator " + op + @"(T10 x, bool y);
}

class C11<T11> : I10<T11> where T11 : C11<T11> {}

interface I12
{
    static abstract bool operator " + op + @"(int x, bool y);
}

interface I13
{
    static abstract bool operator " + op + @"(I13 x, bool y);
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_OperatorNeedsMatch).Verify(
                // (4,26): error CS0563: One of the parameters of a binary operator must be the containing type
                //     static bool operator +(T1 x, bool y) => throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, op).WithLocation(4, 26),
                // (9,26): error CS0563: One of the parameters of a binary operator must be the containing type
                //     static bool operator +(T2? x, bool y) => throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, op).WithLocation(9, 26),
                // (26,39): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //         static abstract bool operator +(T5 x, bool y);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(26, 39),
                // (32,35): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(T71 x, bool y);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(32, 35),
                // (37,35): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(T8 x, bool y);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(37, 35),
                // (44,35): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(T10 x, bool y);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(44, 35),
                // (51,35): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(int x, bool y);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(51, 35)
                );
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<")]
        [InlineData(">")]
        [InlineData("<=")]
        [InlineData(">=")]
        public void OperatorSignature_05(string op)
        {
            var source1 =
@"
interface I1<T1> where T1 : I1<T1>
{
    static bool operator " + op + @"(bool y, T1 x) => throw null;
}

interface I2<T2> where T2 : struct, I2<T2>
{
    static bool operator " + op + @"(bool y, T2? x) => throw null;
}

interface I3<T3> where T3 : I3<T3>
{
    static abstract bool operator " + op + @"(bool y, T3 x);
}

interface I4<T4> where T4 : struct, I4<T4>
{
    static abstract bool operator " + op + @"(bool y, T4? x);
}

class C5<T5> where T5 : C5<T5>.I6
{
    public interface I6
    {
        static abstract bool operator " + op + @"(bool y, T5 x);
    }
}

interface I7<T71, T72> where T72 : I7<T71, T72> where T71 : T72
{
    static abstract bool operator " + op + @"(bool y, T71 x);
}

interface I8<T8> where T8 : I9<T8>
{
    static abstract bool operator " + op + @"(bool y, T8 x);
}

interface I9<T9> : I8<T9> where T9 : I9<T9> {}

interface I10<T10> where T10 : C11<T10>
{
    static abstract bool operator " + op + @"(bool y, T10 x);
}

class C11<T11> : I10<T11> where T11 : C11<T11> {}

interface I12
{
    static abstract bool operator " + op + @"(bool y, int x);
}

interface I13
{
    static abstract bool operator " + op + @"(bool y, I13 x);
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_OperatorNeedsMatch).Verify(
                // (4,26): error CS0563: One of the parameters of a binary operator must be the containing type
                //     static bool operator +(bool y, T1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, op).WithLocation(4, 26),
                // (9,26): error CS0563: One of the parameters of a binary operator must be the containing type
                //     static bool operator +(bool y, T2? x) => throw null;
                Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, op).WithLocation(9, 26),
                // (26,39): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //         static abstract bool operator +(bool y, T5 x);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(26, 39),
                // (32,35): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(bool y, T71 x);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(32, 35),
                // (37,35): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(bool y, T8 x);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(37, 35),
                // (44,35): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(bool y, T10 x);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(44, 35),
                // (51,35): error CS9105: One of the parameters of a binary operator must be the containing type, or its type parameter constrained to it.
                //     static abstract bool operator +(bool y, int x);
                Diagnostic(ErrorCode.ERR_BadAbstractBinaryOperatorSignature, op).WithLocation(51, 35)
                );
        }

        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        public void OperatorSignature_06(string op)
        {
            var source1 =
@"
interface I1<T1> where T1 : I1<T1>
{
    static bool operator " + op + @"(T1 x, int y) => throw null;
}

interface I2<T2> where T2 : struct, I2<T2>
{
    static bool operator " + op + @"(T2? x, int y) => throw null;
}

interface I3<T3> where T3 : I3<T3>
{
    static abstract bool operator " + op + @"(T3 x, int y);
}

interface I4<T4> where T4 : struct, I4<T4>
{
    static abstract bool operator " + op + @"(T4? x, int y);
}

class C5<T5> where T5 : C5<T5>.I6
{
    public interface I6
    {
        static abstract bool operator " + op + @"(T5 x, int y);
    }
}

interface I7<T71, T72> where T72 : I7<T71, T72> where T71 : T72
{
    static abstract bool operator " + op + @"(T71 x, int y);
}

interface I8<T8> where T8 : I9<T8>
{
    static abstract bool operator " + op + @"(T8 x, int y);
}

interface I9<T9> : I8<T9> where T9 : I9<T9> {}

interface I10<T10> where T10 : C11<T10>
{
    static abstract bool operator " + op + @"(T10 x, int y);
}

class C11<T11> : I10<T11> where T11 : C11<T11> {}

interface I12
{
    static abstract bool operator " + op + @"(int x, int y);
}

interface I13
{
    static abstract bool operator " + op + @"(I13 x, int y);
}

interface I14
{
    static abstract bool operator " + op + @"(I14 x, bool y);
}
";

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);
            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_OperatorNeedsMatch).Verify(
                // (4,26): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type, and the type of the second operand must be int
                //     static bool operator <<(T1 x, int y) => throw null;
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, op).WithLocation(4, 26),
                // (9,26): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type, and the type of the second operand must be int
                //     static bool operator <<(T2? x, int y) => throw null;
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, op).WithLocation(9, 26),
                // (26,39): error CS9106: The first operand of an overloaded shift operator must have the same type as the containing type or its type parameter constrained to it, and the type of the second operand must be int
                //         static abstract bool operator <<(T5 x, int y);
                Diagnostic(ErrorCode.ERR_BadAbstractShiftOperatorSignature, op).WithLocation(26, 39),
                // (32,35): error CS9106: The first operand of an overloaded shift operator must have the same type as the containing type or its type parameter constrained to it, and the type of the second operand must be int
                //     static abstract bool operator <<(T71 x, int y);
                Diagnostic(ErrorCode.ERR_BadAbstractShiftOperatorSignature, op).WithLocation(32, 35),
                // (37,35): error CS9106: The first operand of an overloaded shift operator must have the same type as the containing type or its type parameter constrained to it, and the type of the second operand must be int
                //     static abstract bool operator <<(T8 x, int y);
                Diagnostic(ErrorCode.ERR_BadAbstractShiftOperatorSignature, op).WithLocation(37, 35),
                // (44,35): error CS9106: The first operand of an overloaded shift operator must have the same type as the containing type or its type parameter constrained to it, and the type of the second operand must be int
                //     static abstract bool operator <<(T10 x, int y);
                Diagnostic(ErrorCode.ERR_BadAbstractShiftOperatorSignature, op).WithLocation(44, 35),
                // (51,35): error CS9106: The first operand of an overloaded shift operator must have the same type as the containing type or its type parameter constrained to it, and the type of the second operand must be int
                //     static abstract bool operator <<(int x, int y);
                Diagnostic(ErrorCode.ERR_BadAbstractShiftOperatorSignature, op).WithLocation(51, 35),
                // (61,35): error CS9106: The first operand of an overloaded shift operator must have the same type as the containing type or its type parameter constrained to it, and the type of the second operand must be int
                //     static abstract bool operator <<(I14 x, bool y);
                Diagnostic(ErrorCode.ERR_BadAbstractShiftOperatorSignature, op).WithLocation(61, 35)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticMethod_01()
        {
            var source1 =
@"
interface I1
{
    abstract static void M01();

    static void M02()
    {
        M01();
        M04();
    }

    void M03()
    {
        this.M01();
        this.M04();
    }

    static void M04() {}

    protected abstract static void M05();
}

class Test
{
    static void MT1(I1 x)
    {
        I1.M01();
        x.M01();
        I1.M04();
        x.M04();
    }

    static void MT2<T>() where T : I1
    {
        T.M03();
        T.M04();
        T.M00();
        T.M05();

        _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.M01());
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         M01();
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "M01").WithLocation(8, 9),
                // (14,9): error CS0176: Member 'I1.M01()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.M01();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.M01").WithArguments("I1.M01()").WithLocation(14, 9),
                // (15,9): error CS0176: Member 'I1.M04()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.M04();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.M04").WithArguments("I1.M04()").WithLocation(15, 9),
                // (27,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         I1.M01();
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "I1.M01").WithLocation(27, 9),
                // (28,9): error CS0176: Member 'I1.M01()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.M01();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.M01").WithArguments("I1.M01()").WithLocation(28, 9),
                // (30,9): error CS0176: Member 'I1.M04()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.M04();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.M04").WithArguments("I1.M04()").WithLocation(30, 9),
                // (35,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.M03();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 9),
                // (36,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.M04();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 9),
                // (37,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.M00();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 9),
                // (38,11): error CS0122: 'I1.M05()' is inaccessible due to its protection level
                //         T.M05();
                Diagnostic(ErrorCode.ERR_BadAccess, "M05").WithArguments("I1.M05()").WithLocation(38, 11),
                // (40,71): error CS9108: An expression tree may not contain an access of static abstract interface member
                //         _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.M01());
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, "T.M01()").WithLocation(40, 71)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticMethod_02()
        {
            var source1 =
@"
interface I1
{
    abstract static void M01();

    static void M02()
    {
        _ = nameof(M01);
        _ = nameof(M04);
    }

    void M03()
    {
        _ = nameof(this.M01);
        _ = nameof(this.M04);
    }

    static void M04() {}

    protected abstract static void M05();
}

class Test
{
    static void MT1(I1 x)
    {
        _ = nameof(I1.M01);
        _ = nameof(x.M01);
        _ = nameof(I1.M04);
        _ = nameof(x.M04);
    }

    static void MT2<T>() where T : I1
    {
        _ = nameof(T.M03);
        _ = nameof(T.M04);
        _ = nameof(T.M00);
        _ = nameof(T.M05);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (35,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.M03);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 20),
                // (36,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.M04);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 20),
                // (37,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.M00);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 20),
                // (38,22): error CS0122: 'I1.M05()' is inaccessible due to its protection level
                //         _ = nameof(T.M05);
                Diagnostic(ErrorCode.ERR_BadAccess, "M05").WithArguments("I1.M05()").WithLocation(38, 22)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticMethod_03()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01();
    abstract static void M04(int x);
}

class Test
{
    static void M02<T>() where T : I1
    {
        T.M01();
    }

    static string M03<T>() where T : I1
    {
        return nameof(T.M01);
    }

    static async void M05<T>() where T : I1
    {
        T.M04(await System.Threading.Tasks.Task.FromResult(1));
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("Test.M02<T>()",
@"
{
  // Code size       14 (0xe)
  .maxstack  0
  IL_0000:  nop
  IL_0001:  constrained. ""T""
  IL_0007:  call       ""void I1.M01()""
  IL_000c:  nop
  IL_000d:  ret
}
");

            verifier.VerifyIL("Test.M03<T>()",
@"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (string V_0)
  IL_0000:  nop
  IL_0001:  ldstr      ""M01""
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0009
  IL_0009:  ldloc.0
  IL_000a:  ret
}
");

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

            Assert.Equal("T.M01()", node.ToString());
            VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" qualifier is important for this invocation, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IInvocationOperation (virtual void I1.M01()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'T.M01()')
  Instance Receiver: 
    null
  Arguments(0)
");

            var m02 = compilation1.GetMember<MethodSymbol>("Test.M02");

            Assert.Equal("void I1.M01()", ((CSharpSemanticModel)model).LookupSymbols(node.SpanStart, m02.TypeParameters[0], "M01").Single().ToTestDisplayString());
        }

        [Fact]
        public void ConsumeAbstractStaticMethod_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static void M01();
}
";
            var source2 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T.M01();
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,9): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         T.M01();
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "T.M01").WithLocation(6, 9)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,26): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static void M01();
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "M01").WithLocation(12, 26)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticMethod_05()
        {
            var source1 =
@"
public interface I1
{
    abstract static I1 Select(System.Func<int, int> p);
}

class Test
{
    static void M02<T>() where T : I1
    {
        _ = from t in T select t + 1;
        _ = from t in I1 select t + 1;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (11,23): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = from t in T select t + 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(11, 23),
                // (12,26): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = from t in I1 select t + 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "select t + 1").WithLocation(12, 26)
                );
        }

        [Theory]
        [InlineData("+", "")]
        [InlineData("-", "")]
        [InlineData("!", "")]
        [InlineData("~", "")]
        [InlineData("++", "")]
        [InlineData("--", "")]
        [InlineData("", "++")]
        [InlineData("", "--")]
        public void ConsumeAbstractUnaryOperator_01(string prefixOp, string postfixOp)
        {
            var source1 =
@"
interface I1<T> where T : I1<T>
{
    abstract static T operator" + prefixOp + postfixOp + @" (T x);
    abstract static I1<T> operator" + prefixOp + postfixOp + @" (I1<T> x);
    static void M02(I1<T> x)
    {
        _ = " + prefixOp + "x" + postfixOp + @";
    }

    void M03(I1<T> y)
    {
        _ = " + prefixOp + "y" + postfixOp + @";
    }
}

class Test<T> where T : I1<T>
{
    static void MT1(I1<T> a)
    {
        _ = " + prefixOp + "a" + postfixOp + @";
    }

    static void MT2()
    {
        _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (" + prefixOp + "b" + postfixOp + @").ToString());
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = -x;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, prefixOp + "x" + postfixOp).WithLocation(8, 13),
                // (13,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = -y;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, prefixOp + "y" + postfixOp).WithLocation(13, 13),
                // (21,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = -a;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, prefixOp + "a" + postfixOp).WithLocation(21, 13),
                (prefixOp + postfixOp).Length == 1 ?
                    // (26,78): error CS9108: An expression tree may not contain an access of static abstract interface member
                    //         _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (-b).ToString());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, prefixOp + "b" + postfixOp).WithLocation(26, 78)
                    :
                    // (26,78): error CS0832: An expression tree may not contain an assignment operator
                    //         _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b--).ToString());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, prefixOp + "b" + postfixOp).WithLocation(26, 78)
                );
        }

        [Theory]
        [InlineData("+", "", "op_UnaryPlus", "Plus")]
        [InlineData("-", "", "op_UnaryNegation", "Minus")]
        [InlineData("!", "", "op_LogicalNot", "Not")]
        [InlineData("~", "", "op_OnesComplement", "BitwiseNegation")]
        [InlineData("++", "", "op_Increment", "Increment")]
        [InlineData("--", "", "op_Decrement", "Decrement")]
        [InlineData("", "++", "op_Increment", "Increment")]
        [InlineData("", "--", "op_Decrement", "Decrement")]
        public void ConsumeAbstractUnaryOperator_03(string prefixOp, string postfixOp, string metadataName, string opKind)
        {
            var source1 =
@"
public interface I1<T> where T : I1<T>
{
    abstract static T operator" + prefixOp + postfixOp + @" (T x);
}

class Test
{
    static T M02<T>(T x) where T : I1<T>
    {
        return " + prefixOp + "x" + postfixOp + @";
    }

    static T? M03<T>(T? y) where T : struct, I1<T>
    {
        return " + prefixOp + "y" + postfixOp + @";
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            switch ((prefixOp, postfixOp))
            {
                case ("++", ""):
                case ("--", ""):
                    verifier.VerifyIL("Test.M02<T>(T)",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  constrained. ""T""
  IL_0008:  call       ""T I1<T>." + metadataName + @"(T)""
  IL_000d:  dup
  IL_000e:  starg.s    V_0
  IL_0010:  dup
  IL_0011:  stloc.0
  IL_0012:  br.s       IL_0014
  IL_0014:  ldloc.0
  IL_0015:  ret
}
");
                    verifier.VerifyIL("Test.M03<T>(T?)",
@"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (T? V_0,
                T? V_1,
                T? V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""readonly bool T?.HasValue.get""
  IL_000a:  brtrue.s   IL_0017
  IL_000c:  ldloca.s   V_1
  IL_000e:  initobj    ""T?""
  IL_0014:  ldloc.1
  IL_0015:  br.s       IL_002e
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""readonly T T?.GetValueOrDefault()""
  IL_001e:  constrained. ""T""
  IL_0024:  call       ""T I1<T>." + metadataName + @"(T)""
  IL_0029:  newobj     ""T?..ctor(T)""
  IL_002e:  dup
  IL_002f:  starg.s    V_0
  IL_0031:  dup
  IL_0032:  stloc.2
  IL_0033:  br.s       IL_0035
  IL_0035:  ldloc.2
  IL_0036:  ret
}
");
                    break;

                case ("", "++"):
                case ("", "--"):
                    verifier.VerifyIL("Test.M02<T>(T)",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""T I1<T>." + metadataName + @"(T)""
  IL_000e:  starg.s    V_0
  IL_0010:  dup
  IL_0011:  stloc.0
  IL_0012:  br.s       IL_0014
  IL_0014:  ldloc.0
  IL_0015:  ret
}
");
                    verifier.VerifyIL("Test.M03<T>(T?)",
@"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (T? V_0,
                T? V_1,
                T? V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  ldloca.s   V_0
  IL_0006:  call       ""readonly bool T?.HasValue.get""
  IL_000b:  brtrue.s   IL_0018
  IL_000d:  ldloca.s   V_1
  IL_000f:  initobj    ""T?""
  IL_0015:  ldloc.1
  IL_0016:  br.s       IL_002f
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       ""readonly T T?.GetValueOrDefault()""
  IL_001f:  constrained. ""T""
  IL_0025:  call       ""T I1<T>." + metadataName + @"(T)""
  IL_002a:  newobj     ""T?..ctor(T)""
  IL_002f:  starg.s    V_0
  IL_0031:  dup
  IL_0032:  stloc.2
  IL_0033:  br.s       IL_0035
  IL_0035:  ldloc.2
  IL_0036:  ret
}
");
                    break;

                default:
                    verifier.VerifyIL("Test.M02<T>(T)",
@"
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  constrained. ""T""
  IL_0008:  call       ""T I1<T>." + metadataName + @"(T)""
  IL_000d:  stloc.0
  IL_000e:  br.s       IL_0010
  IL_0010:  ldloc.0
  IL_0011:  ret
}
");
                    verifier.VerifyIL("Test.M03<T>(T?)",
@"
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (T? V_0,
                T? V_1,
                T? V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""readonly bool T?.HasValue.get""
  IL_000a:  brtrue.s   IL_0017
  IL_000c:  ldloca.s   V_1
  IL_000e:  initobj    ""T?""
  IL_0014:  ldloc.1
  IL_0015:  br.s       IL_002e
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""readonly T T?.GetValueOrDefault()""
  IL_001e:  constrained. ""T""
  IL_0024:  call       ""T I1<T>." + metadataName + @"(T)""
  IL_0029:  newobj     ""T?..ctor(T)""
  IL_002e:  stloc.2
  IL_002f:  br.s       IL_0031
  IL_0031:  ldloc.2
  IL_0032:  ret
}
");
                    break;
            }

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = postfixOp != "" ? (ExpressionSyntax)tree.GetRoot().DescendantNodes().OfType<PostfixUnaryExpressionSyntax>().First() : tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().First();

            Assert.Equal(prefixOp + "x" + postfixOp, node.ToString());

            switch ((prefixOp, postfixOp))
            {
                case ("++", ""):
                case ("--", ""):
                case ("", "++"):
                case ("", "--"):
                    VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" constraint is important for this operator, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IIncrementOrDecrementOperation (" + (prefixOp != "" ? "Prefix" : "Postfix") + @") (OperatorMethod: T I1<T>." + metadataName + @"(T x)) (OperationKind." + opKind + @", Type: T) (Syntax: '" + prefixOp + "x" + postfixOp + @"')
  Target: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
");
                    break;

                default:
                    VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" constraint is important for this operator, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IUnaryOperation (UnaryOperatorKind." + opKind + @") (OperatorMethod: T I1<T>." + metadataName + @"(T x)) (OperationKind.Unary, Type: T) (Syntax: '" + prefixOp + "x" + postfixOp + @"')
  Operand: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
");
                    break;
            }
        }

        [Theory]
        [InlineData("+", "")]
        [InlineData("-", "")]
        [InlineData("!", "")]
        [InlineData("~", "")]
        [InlineData("++", "")]
        [InlineData("--", "")]
        [InlineData("", "++")]
        [InlineData("", "--")]
        public void ConsumeAbstractUnaryOperator_04(string prefixOp, string postfixOp)
        {
            var source1 =
@"
public interface I1<T> where T : I1<T>
{
    abstract static T operator" + prefixOp + postfixOp + @" (T x);
}
";
            var source2 =
@"
class Test
{
    static void M02<T>(T x) where T : I1<T>
    {
        _ = " + prefixOp + "x" + postfixOp + @";
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,13): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         _ = -x;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, prefixOp + "x" + postfixOp).WithLocation(6, 13)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,32): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static T operator- (T x);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, prefixOp + postfixOp).WithLocation(12, 31)
                );
        }

        [Fact]
        public void ConsumeAbstractTrueOperator_01()
        {
            var source1 =
@"
interface I1
{
    abstract static bool operator true (I1 x);
    abstract static bool operator false (I1 x);

    static void M02(I1 x)
    {
        _ = x ? true : false;
    }

    void M03(I1 y)
    {
        _ = y ? true : false;
    }
}

class Test
{
    static void MT1(I1 a)
    {
        _ = a ? true : false;
    }

    static void MT2<T>() where T : I1
    {
        _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b ? true : false).ToString());
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (9,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = x ? true : false;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "x").WithLocation(9, 13),
                // (14,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = y ? true : false;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "y").WithLocation(14, 13),
                // (22,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = a ? true : false;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "a").WithLocation(22, 13),
                // (27,78): error CS9108: An expression tree may not contain an access of static abstract interface member
                //         _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b ? true : false).ToString());
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, "b").WithLocation(27, 78)
                );
        }

        [Fact]
        public void ConsumeAbstractTrueOperator_03()
        {
            var source1 =
@"
public interface I1<T> where T : I1<T>
{
    abstract static bool operator true (T x);
    abstract static bool operator false (T x);
}

class Test
{
    static void M02<T>(T x) where T : I1<T>
    {
        _ = x ? true : false;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("Test.M02<T>(T)",
@"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  constrained. ""T""
  IL_0008:  call       ""bool I1<T>.op_True(T)""
  IL_000d:  brtrue.s   IL_0011
  IL_000f:  br.s       IL_0011
  IL_0011:  ret
}
");

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>().First();

            Assert.Equal("x ? true : false", node.ToString());
            VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" constraint is important for this operator, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IConditionalOperation (OperationKind.Conditional, Type: System.Boolean) (Syntax: 'x ? true : false')
  Condition: 
    IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: System.Boolean I1<T>.op_True(T x)) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'x')
      Operand: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
  WhenTrue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenFalse: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
");
        }

        [Fact]
        public void ConsumeAbstractTrueOperator_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static bool operator true (I1 x);
    abstract static bool operator false (I1 x);
}
";
            var source2 =
@"
class Test
{
    static void M02<T>(T x) where T : I1
    {
        _ = x ? true : false;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,13): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         _ = x ? true : false;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "x").WithLocation(6, 13)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,35): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static bool operator true (I1 x);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "true").WithLocation(12, 35),
                // (13,35): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static bool operator false (I1 x);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "false").WithLocation(13, 35)
                );
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<<")]
        [InlineData(">>")]
        public void ConsumeAbstractBinaryOperator_01(string op)
        {
            var source1 =
@"
interface I1
{
    abstract static I1 operator" + op + @" (I1 x, int y);

    static void M02(I1 x)
    {
        _ = x " + op + @" 1;
    }

    void M03(I1 y)
    {
        _ = y " + op + @" 2;
    }
}

class Test
{
    static void MT1(I1 a)
    {
        _ = a " + op + @" 3;
    }

    static void MT2<T>() where T : I1
    {
        _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b " + op + @" 4).ToString());
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = x - 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "x " + op + " 1").WithLocation(8, 13),
                // (13,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = y - 2;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "y " + op + " 2").WithLocation(13, 13),
                // (21,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = a - 3;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "a " + op + " 3").WithLocation(21, 13),
                // (26,78): error CS9108: An expression tree may not contain an access of static abstract interface member
                //         _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b - 4).ToString());
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, "b " + op + " 4").WithLocation(26, 78)
                );
        }

        [Theory]
        [InlineData("&", true, false, false, false)]
        [InlineData("|", true, false, false, false)]
        [InlineData("&", false, false, true, false)]
        [InlineData("|", false, true, false, false)]
        [InlineData("&", true, false, true, false)]
        [InlineData("|", true, true, false, false)]
        [InlineData("&", false, true, false, true)]
        [InlineData("|", false, false, true, true)]
        public void ConsumeAbstractLogicalBinaryOperator_01(string op, bool binaryIsAbstract, bool trueIsAbstract, bool falseIsAbstract, bool success)
        {
            var source1 =
@"
interface I1
{
    " + (binaryIsAbstract ? "abstract " : "") + @"static I1 operator" + op + @" (I1 x, I1 y)" + (binaryIsAbstract ? ";" : " => throw null;") + @"
    " + (trueIsAbstract ? "abstract " : "") + @"static bool operator true (I1 x)" + (trueIsAbstract ? ";" : " => throw null;") + @"
    " + (falseIsAbstract ? "abstract " : "") + @"static bool operator false (I1 x)" + (falseIsAbstract ? ";" : " => throw null;") + @"

    static void M02(I1 x)
    {
        _ = x " + op + op + @" x;
    }

    void M03(I1 y)
    {
        _ = y " + op + op + @" y;
    }
}

class Test
{
    static void MT1(I1 a)
    {
        _ = a " + op + op + @" a;
    }

    static void MT2<T>() where T : I1
    {
        _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b " + op + op + @" b).ToString());
    }

    static void MT3(I1 b, dynamic c)
    {
        _ = b " + op + op + @" c;
    }
";
            if (!success)
            {
                source1 +=
@"
    static void MT4<T>() where T : I1
    {
        _ = (System.Linq.Expressions.Expression<System.Action<T, dynamic>>)((T d, dynamic e) => (d " + op + op + @" e).ToString());
    }
";
            }

            source1 +=
@"
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreAppAndCSharp);

            if (success)
            {
                Assert.False(binaryIsAbstract);
                Assert.False(op == "&" ? falseIsAbstract : trueIsAbstract);
                var binaryMetadataName = op == "&" ? "op_BitwiseAnd" : "op_BitwiseOr";
                var unaryMetadataName = op == "&" ? "op_False" : "op_True";

                var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

                verifier.VerifyIL("Test.MT1(I1)",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (I1 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""bool I1." + unaryMetadataName + @"(I1)""
  IL_0009:  brtrue.s   IL_0015
  IL_000b:  ldloc.0
  IL_000c:  ldarg.0
  IL_000d:  call       ""I1 I1." + binaryMetadataName + @"(I1, I1)""
  IL_0012:  pop
  IL_0013:  br.s       IL_0015
  IL_0015:  ret
}
");

                if (op == "&")
                {
                    verifier.VerifyIL("Test.MT3(I1, dynamic)",
@"
{
  // Code size       97 (0x61)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""bool I1.op_False(I1)""
  IL_0007:  brtrue.s   IL_0060
  IL_0009:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> Test.<>o__2.<>p__0""
  IL_000e:  brfalse.s  IL_0012
  IL_0010:  br.s       IL_0047
  IL_0012:  ldc.i4.8
  IL_0013:  ldc.i4.2
  IL_0014:  ldtoken    ""Test""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  ldc.i4.2
  IL_001f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.i4.1
  IL_0027:  ldnull
  IL_0028:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002d:  stelem.ref
  IL_002e:  dup
  IL_002f:  ldc.i4.1
  IL_0030:  ldc.i4.0
  IL_0031:  ldnull
  IL_0032:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0037:  stelem.ref
  IL_0038:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0042:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> Test.<>o__2.<>p__0""
  IL_0047:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> Test.<>o__2.<>p__0""
  IL_004c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>>.Target""
  IL_0051:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> Test.<>o__2.<>p__0""
  IL_0056:  ldarg.0
  IL_0057:  ldarg.1
  IL_0058:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, I1, dynamic)""
  IL_005d:  pop
  IL_005e:  br.s       IL_0060
  IL_0060:  ret
}
");
                }
                else
                {
                    verifier.VerifyIL("Test.MT3(I1, dynamic)",
@"
{
  // Code size       98 (0x62)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""bool I1.op_True(I1)""
  IL_0007:  brtrue.s   IL_0061
  IL_0009:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> Test.<>o__2.<>p__0""
  IL_000e:  brfalse.s  IL_0012
  IL_0010:  br.s       IL_0048
  IL_0012:  ldc.i4.8
  IL_0013:  ldc.i4.s   36
  IL_0015:  ldtoken    ""Test""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  ldc.i4.2
  IL_0020:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0025:  dup
  IL_0026:  ldc.i4.0
  IL_0027:  ldc.i4.1
  IL_0028:  ldnull
  IL_0029:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002e:  stelem.ref
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.0
  IL_0032:  ldnull
  IL_0033:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0038:  stelem.ref
  IL_0039:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_003e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0043:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> Test.<>o__2.<>p__0""
  IL_0048:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> Test.<>o__2.<>p__0""
  IL_004d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>>.Target""
  IL_0052:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>> Test.<>o__2.<>p__0""
  IL_0057:  ldarg.0
  IL_0058:  ldarg.1
  IL_0059:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, I1, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, I1, dynamic)""
  IL_005e:  pop
  IL_005f:  br.s       IL_0061
  IL_0061:  ret
}
");
                }
            }
            else
            {
                var builder = ArrayBuilder<DiagnosticDescription>.GetInstance();

                builder.AddRange(
                    // (10,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                    //         _ = x && x;
                    Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "x " + op + op + " x").WithLocation(10, 13),
                    // (15,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                    //         _ = y && y;
                    Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "y " + op + op + " y").WithLocation(15, 13),
                    // (23,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                    //         _ = a && a;
                    Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "a " + op + op + " a").WithLocation(23, 13),
                    // (28,78): error CS9108: An expression tree may not contain an access of static abstract interface member
                    //         _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b && b).ToString());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, "b " + op + op + " b").WithLocation(28, 78)
                    );

                if (op == "&" ? falseIsAbstract : trueIsAbstract)
                {
                    builder.Add(
                        // (33,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                        //         _ = b || c;
                        Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "b " + op + op + " c").WithLocation(33, 13)
                        );
                }

                builder.Add(
                    // (38,98): error CS7083: Expression must be implicitly convertible to Boolean or its type 'T' must define operator 'true'.
                    //         _ = (System.Linq.Expressions.Expression<System.Action<T, dynamic>>)((T d, dynamic e) => (d || e).ToString());
                    Diagnostic(ErrorCode.ERR_InvalidDynamicCondition, "d").WithArguments("T", op == "&" ? "false" : "true").WithLocation(38, 98)
                    );

                compilation1.VerifyDiagnostics(builder.ToArrayAndFree());
            }
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<<")]
        [InlineData(">>")]
        public void ConsumeAbstractCompoundBinaryOperator_01(string op)
        {
            var source1 =
@"
interface I1
{
    abstract static I1 operator" + op + @" (I1 x, int y);

    static void M02(I1 x)
    {
        x " + op + @"= 1;
    }

    void M03(I1 y)
    {
        y " + op + @"= 2;
    }
}

interface I2<T> where T : I2<T>
{
    abstract static T operator" + op + @" (T x, int y);
}

class Test
{
    static void MT1(I1 a)
    {
        a " + op + @"= 3;
    }

    static void MT2<T>() where T : I2<T>
    {
        _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b " + op + @"= 4).ToString());
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         x /= 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "x " + op + "= 1").WithLocation(8, 9),
                // (13,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         y /= 2;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "y " + op + "= 2").WithLocation(13, 9),
                // (26,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         a /= 3;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "a " + op + "= 3").WithLocation(26, 9),
                // (31,78): error CS0832: An expression tree may not contain an assignment operator
                //         _ = (System.Linq.Expressions.Expression<System.Action<T>>)((T b) => (b /= 4).ToString());
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "b " + op + "= 4").WithLocation(31, 78)
                );
        }

        [Theory]
        [InlineData("+", "op_Addition", "Add")]
        [InlineData("-", "op_Subtraction", "Subtract")]
        [InlineData("*", "op_Multiply", "Multiply")]
        [InlineData("/", "op_Division", "Divide")]
        [InlineData("%", "op_Modulus", "Remainder")]
        [InlineData("&", "op_BitwiseAnd", "And")]
        [InlineData("|", "op_BitwiseOr", "Or")]
        [InlineData("^", "op_ExclusiveOr", "ExclusiveOr")]
        [InlineData("<<", "op_LeftShift", "LeftShift")]
        [InlineData(">>", "op_RightShift", "RightShift")]
        public void ConsumeAbstractBinaryOperator_03(string op, string metadataName, string operatorKind)
        {
            var source1 =
@"
public interface I1<T0> where T0 : I1<T0>
{
";
            if (op.Length == 1)
            {
                source1 += @"
    abstract static T0 operator" + op + @" (int a, T0 x);
";
            }

            source1 += @"
    abstract static T0 operator" + op + @" (T0 x, int a);
";
            if (op.Length == 1)
            {
                source1 += @"
    abstract static T0 operator" + op + @" (I1<T0> x, T0 a);
    abstract static T0 operator" + op + @" (T0 x, I1<T0> a);
";
            }

            source1 += @"
}

class Test
{
";
            if (op.Length == 1)
            {
                source1 += @"
    static void M02<T>(T x) where T : I1<T>
    {
        _ = 1 " + op + @" x;
    }
";
            }

            source1 += @"
    static void M03<T>(T x) where T : I1<T>
    {
        _ = x " + op + @" 1;
    }
";
            if (op.Length == 1)
            {
                source1 += @"
    static void M04<T>(T? y) where T : struct, I1<T>
    {
        _ = 1 " + op + @" y;
    }
";
            }

            source1 += @"
    static void M05<T>(T? y) where T : struct, I1<T>
    {
        _ = y " + op + @" 1;
    }
";
            if (op.Length == 1)
            {
                source1 += @"
    static void M06<T>(I1<T> x, T y) where T : I1<T>
    {
        _ = x " + op + @" y;
    }

    static void M07<T>(T x, I1<T> y) where T : I1<T>
    {
        _ = x " + op + @" y;
    }
";
            }

            source1 += @"
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            if (op.Length == 1)
            {
                verifier.VerifyIL("Test.M02<T>(T)",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""T I1<T>." + metadataName + @"(int, T)""
  IL_000e:  pop
  IL_000f:  ret
}
");
            }

            verifier.VerifyIL("Test.M03<T>(T)",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.1
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""T I1<T>." + metadataName + @"(T, int)""
  IL_000e:  pop
  IL_000f:  ret
}
");

            if (op.Length == 1)
            {
                verifier.VerifyIL("Test.M04<T>(T?)",
@"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (T? V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""readonly bool T?.HasValue.get""
  IL_000a:  brtrue.s   IL_000e
  IL_000c:  br.s       IL_0022
  IL_000e:  ldc.i4.1
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       ""readonly T T?.GetValueOrDefault()""
  IL_0016:  constrained. ""T""
  IL_001c:  call       ""T I1<T>." + metadataName + @"(int, T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
            }

            verifier.VerifyIL("Test.M05<T>(T?)",
@"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (T? V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""readonly bool T?.HasValue.get""
  IL_000a:  brtrue.s   IL_000e
  IL_000c:  br.s       IL_0022
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       ""readonly T T?.GetValueOrDefault()""
  IL_0015:  ldc.i4.1
  IL_0016:  constrained. ""T""
  IL_001c:  call       ""T I1<T>." + metadataName + @"(T, int)""
  IL_0021:  pop
  IL_0022:  ret
}
");

            if (op.Length == 1)
            {
                verifier.VerifyIL("Test.M06<T>(I1<T>, T)",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""T I1<T>." + metadataName + @"(I1<T>, T)""
  IL_000e:  pop
  IL_000f:  ret
}
");

                verifier.VerifyIL("Test.M07<T>(T, I1<T>)",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""T I1<T>." + metadataName + @"(T, I1<T>)""
  IL_000e:  pop
  IL_000f:  ret
}
");
            }

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(n => n.ToString() == "x " + op + " 1").Single();

            Assert.Equal("x " + op + " 1", node.ToString());
            VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" constraint is important for this operator, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IBinaryOperation (BinaryOperatorKind." + operatorKind + @") (OperatorMethod: T I1<T>." + metadataName + @"(T x, System.Int32 a)) (OperationKind.Binary, Type: T) (Syntax: 'x " + op + @" 1')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
");
        }

        [Theory]
        [InlineData("&", true, true)]
        [InlineData("|", true, true)]
        [InlineData("&", true, false)]
        [InlineData("|", true, false)]
        [InlineData("&", false, true)]
        [InlineData("|", false, true)]
        public void ConsumeAbstractLogicalBinaryOperator_03(string op, bool binaryIsAbstract, bool unaryIsAbstract)
        {
            var binaryMetadataName = op == "&" ? "op_BitwiseAnd" : "op_BitwiseOr";
            var unaryMetadataName = op == "&" ? "op_False" : "op_True";
            var opKind = op == "&" ? "ConditionalAnd" : "ConditionalOr";

            if (binaryIsAbstract && unaryIsAbstract)
            {
                consumeAbstract(op);
            }
            else
            {
                consumeMixed(op, binaryIsAbstract, unaryIsAbstract);
            }

            void consumeAbstract(string op)
            {
                var source1 =
@"
public interface I1<T0> where T0 : I1<T0>
{
    abstract static T0 operator" + op + @" (T0 a, T0 x);
    abstract static bool operator true (T0 x);
    abstract static bool operator false (T0 x);
}

class Test
{
    static void M03<T>(T x, T y) where T : I1<T>
    {
        _ = x " + op + op + @" y;
    }
}
";
                var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                     parseOptions: TestOptions.RegularPreview,
                                                     targetFramework: TargetFramework.NetCoreAppAndCSharp);

                var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();
                verifier.VerifyIL("Test.M03<T>(T, T)",
@"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  constrained. ""T""
  IL_000a:  call       ""bool I1<T>." + unaryMetadataName + @"(T)""
  IL_000f:  brtrue.s   IL_0021
  IL_0011:  ldloc.0
  IL_0012:  ldarg.1
  IL_0013:  constrained. ""T""
  IL_0019:  call       ""T I1<T>." + binaryMetadataName + @"(T, T)""
  IL_001e:  pop
  IL_001f:  br.s       IL_0021
  IL_0021:  ret
}
");

                var tree = compilation1.SyntaxTrees.Single();
                var model = compilation1.GetSemanticModel(tree);
                var node1 = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(n => n.ToString() == "x " + op + op + " y").Single();

                Assert.Equal("x " + op + op + " y", node1.ToString());

                VerifyOperationTreeForNode(compilation1, model, node1,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" constraint is important for this operator, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IBinaryOperation (BinaryOperatorKind." + opKind + @") (OperatorMethod: T I1<T>." + binaryMetadataName + @"(T a, T x)) (OperationKind.Binary, Type: T) (Syntax: 'x " + op + op + @" y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: T) (Syntax: 'y')
");
            }

            void consumeMixed(string op, bool binaryIsAbstract, bool unaryIsAbstract)
            {
                var source1 =
@"
public interface I1
{
    " + (binaryIsAbstract ? "abstract " : "") + @"static I1 operator" + op + @" (I1 a, I1 x)" + (binaryIsAbstract ? ";" : " => throw null;") + @"
    " + (unaryIsAbstract ? "abstract " : "") + @"static bool operator true (I1 x)" + (unaryIsAbstract ? ";" : " => throw null;") + @"
    " + (unaryIsAbstract ? "abstract " : "") + @"static bool operator false (I1 x)" + (unaryIsAbstract ? ";" : " => throw null;") + @"
}

class Test
{
    static void M03<T>(T x, T y) where T : I1
    {
        _ = x " + op + op + @" y;
    }
}
";
                var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                     parseOptions: TestOptions.RegularPreview,
                                                     targetFramework: TargetFramework.NetCoreAppAndCSharp);

                var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

                switch (binaryIsAbstract, unaryIsAbstract)
                {
                    case (true, false):
                        verifier.VerifyIL("Test.M03<T>(T, T)",
@"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (I1 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  box        ""T""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       ""bool I1." + unaryMetadataName + @"(I1)""
  IL_000e:  brtrue.s   IL_0025
  IL_0010:  ldloc.0
  IL_0011:  ldarg.1
  IL_0012:  box        ""T""
  IL_0017:  constrained. ""T""
  IL_001d:  call       ""I1 I1." + binaryMetadataName + @"(I1, I1)""
  IL_0022:  pop
  IL_0023:  br.s       IL_0025
  IL_0025:  ret
}
");
                        break;

                    case (false, true):
                        verifier.VerifyIL("Test.M03<T>(T, T)",
@"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (I1 V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  box        ""T""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  constrained. ""T""
  IL_000f:  call       ""bool I1." + unaryMetadataName + @"(I1)""
  IL_0014:  brtrue.s   IL_0025
  IL_0016:  ldloc.0
  IL_0017:  ldarg.1
  IL_0018:  box        ""T""
  IL_001d:  call       ""I1 I1." + binaryMetadataName + @"(I1, I1)""
  IL_0022:  pop
  IL_0023:  br.s       IL_0025
  IL_0025:  ret
}
");
                        break;

                    default:
                        Assert.True(false);
                        break;
                }

                var tree = compilation1.SyntaxTrees.Single();
                var model = compilation1.GetSemanticModel(tree);
                var node1 = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(n => n.ToString() == "x " + op + op + " y").Single();

                Assert.Equal("x " + op + op + " y", node1.ToString());

                VerifyOperationTreeForNode(compilation1, model, node1,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" constraint is important for this operator, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IBinaryOperation (BinaryOperatorKind." + opKind + @") (OperatorMethod: I1 I1." + binaryMetadataName + @"(I1 a, I1 x)) (OperationKind.Binary, Type: I1) (Syntax: 'x " + op + op + @" y')
  Left: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsImplicit) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: T) (Syntax: 'y')
");
            }
        }

        [Theory]
        [InlineData("+", "op_Addition", "Add")]
        [InlineData("-", "op_Subtraction", "Subtract")]
        [InlineData("*", "op_Multiply", "Multiply")]
        [InlineData("/", "op_Division", "Divide")]
        [InlineData("%", "op_Modulus", "Remainder")]
        [InlineData("&", "op_BitwiseAnd", "And")]
        [InlineData("|", "op_BitwiseOr", "Or")]
        [InlineData("^", "op_ExclusiveOr", "ExclusiveOr")]
        [InlineData("<<", "op_LeftShift", "LeftShift")]
        [InlineData(">>", "op_RightShift", "RightShift")]
        public void ConsumeAbstractCompoundBinaryOperator_03(string op, string metadataName, string operatorKind)
        {
            var source1 =
@"
public interface I1<T0> where T0 : I1<T0>
{
";
            if (op.Length == 1)
            {
                source1 += @"
    abstract static int operator" + op + @" (int a, T0 x);
";
            }

            source1 += @"
    abstract static T0 operator" + op + @" (T0 x, int a);
";
            if (op.Length == 1)
            {
                source1 += @"
    abstract static I1<T0> operator" + op + @" (I1<T0> x, T0 a);
    abstract static T0 operator" + op + @" (T0 x, I1<T0> a);
";
            }

            source1 += @"
}

class Test
{
";
            if (op.Length == 1)
            {
                source1 += @"
    static void M02<T>(int a, T x) where T : I1<T>
    {
        a " + op + @"= x;
    }
";
            }

            source1 += @"
    static void M03<T>(T x) where T : I1<T>
    {
        x " + op + @"= 1;
    }
";
            if (op.Length == 1)
            {
                source1 += @"
    static void M04<T>(int? a, T? y) where T : struct, I1<T>
    {
        a " + op + @"= y;
    }
";
            }

            source1 += @"
    static void M05<T>(T? y) where T : struct, I1<T>
    {
        y " + op + @"= 1;
    }
";
            if (op.Length == 1)
            {
                source1 += @"
    static void M06<T>(I1<T> x, T y) where T : I1<T>
    {
        x " + op + @"= y;
    }

    static void M07<T>(T x, I1<T> y) where T : I1<T>
    {
        x " + op + @"= y;
    }
";
            }

            source1 += @"
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            if (op.Length == 1)
            {
                verifier.VerifyIL("Test.M02<T>(int, T)",
@"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""int I1<T>." + metadataName + @"(int, T)""
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}
");
            }

            verifier.VerifyIL("Test.M03<T>(T)",
@"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.1
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""T I1<T>." + metadataName + @"(T, int)""
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}
");

            if (op.Length == 1)
            {
                verifier.VerifyIL("Test.M04<T>(int?, T?)",
@"
{
  // Code size       66 (0x42)
  .maxstack  2
  .locals init (int? V_0,
                T? V_1,
                int? V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldarg.1
  IL_0004:  stloc.1
  IL_0005:  ldloca.s   V_0
  IL_0007:  call       ""readonly bool int?.HasValue.get""
  IL_000c:  ldloca.s   V_1
  IL_000e:  call       ""readonly bool T?.HasValue.get""
  IL_0013:  and
  IL_0014:  brtrue.s   IL_0021
  IL_0016:  ldloca.s   V_2
  IL_0018:  initobj    ""int?""
  IL_001e:  ldloc.2
  IL_001f:  br.s       IL_003f
  IL_0021:  ldloca.s   V_0
  IL_0023:  call       ""readonly int int?.GetValueOrDefault()""
  IL_0028:  ldloca.s   V_1
  IL_002a:  call       ""readonly T T?.GetValueOrDefault()""
  IL_002f:  constrained. ""T""
  IL_0035:  call       ""int I1<T>." + metadataName + @"(int, T)""
  IL_003a:  newobj     ""int?..ctor(int)""
  IL_003f:  starg.s    V_0
  IL_0041:  ret
}
");
            }

            verifier.VerifyIL("Test.M05<T>(T?)",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (T? V_0,
                T? V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       ""readonly bool T?.HasValue.get""
  IL_000a:  brtrue.s   IL_0017
  IL_000c:  ldloca.s   V_1
  IL_000e:  initobj    ""T?""
  IL_0014:  ldloc.1
  IL_0015:  br.s       IL_002f
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""readonly T T?.GetValueOrDefault()""
  IL_001e:  ldc.i4.1
  IL_001f:  constrained. ""T""
  IL_0025:  call       ""T I1<T>." + metadataName + @"(T, int)""
  IL_002a:  newobj     ""T?..ctor(T)""
  IL_002f:  starg.s    V_0
  IL_0031:  ret
}
");

            if (op.Length == 1)
            {
                verifier.VerifyIL("Test.M06<T>(I1<T>, T)",
@"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""I1<T> I1<T>." + metadataName + @"(I1<T>, T)""
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}
");

                verifier.VerifyIL("Test.M07<T>(T, I1<T>)",
@"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""T I1<T>." + metadataName + @"(T, I1<T>)""
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}
");
            }

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(n => n.ToString() == "x " + op + "= 1").Single();

            Assert.Equal("x " + op + "= 1", node.ToString());
            VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" constraint is important for this operator, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
ICompoundAssignmentOperation (BinaryOperatorKind." + operatorKind + @") (OperatorMethod: T I1<T>." + metadataName + @"(T x, System.Int32 a)) (OperationKind.CompoundAssignment, Type: T) (Syntax: 'x " + op + @"= 1')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
");
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<<")]
        [InlineData(">>")]
        public void ConsumeAbstractBinaryOperator_04(string op)
        {
            var source1 =
@"
public interface I1
{
    abstract static I1 operator" + op + @" (I1 x, int y);
}
";
            var source2 =
@"
class Test
{
    static void M02<T>(T x, int y) where T : I1
    {
        _ = x " + op + @" y;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,13): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         _ = x - y;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "x " + op + " y").WithLocation(6, 13)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,32): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static I1 operator- (I1 x, int y);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, op).WithLocation(12, 32)
                );
        }

        [Theory]
        [InlineData("&", true, false, false, false)]
        [InlineData("|", true, false, false, false)]
        [InlineData("&", false, false, true, false)]
        [InlineData("|", false, true, false, false)]
        [InlineData("&", true, false, true, false)]
        [InlineData("|", true, true, false, false)]
        [InlineData("&", false, true, false, true)]
        [InlineData("|", false, false, true, true)]
        public void ConsumeAbstractLogicalBinaryOperator_04(string op, bool binaryIsAbstract, bool trueIsAbstract, bool falseIsAbstract, bool success)
        {
            var source1 =
@"
public interface I1
{
    " + (binaryIsAbstract ? "abstract " : "") + @"static I1 operator" + op + @" (I1 x, I1 y)" + (binaryIsAbstract ? ";" : " => throw null;") + @"
    " + (trueIsAbstract ? "abstract " : "") + @"static bool operator true (I1 x)" + (trueIsAbstract ? ";" : " => throw null;") + @"
    " + (falseIsAbstract ? "abstract " : "") + @"static bool operator false (I1 x)" + (falseIsAbstract ? ";" : " => throw null;") + @"
}
";
            var source2 =
@"
class Test
{
    static void M02<T>(T x, T y) where T : I1
    {
        _ = x " + op + op + @" y;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreAppAndCSharp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            if (success)
            {
                compilation2.VerifyDiagnostics();
            }
            else
            {
                compilation2.VerifyDiagnostics(
                    // (6,13): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                    //         _ = x && y;
                    Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "x " + op + op + " y").WithLocation(6, 13)
                    );
            }

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            var builder = ArrayBuilder<DiagnosticDescription>.GetInstance();

            if (binaryIsAbstract)
            {
                builder.Add(
                    // (12,32): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                    //     abstract static I1 operator& (I1 x, I1 y);
                    Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, op).WithLocation(12, 32)
                    );
            }

            if (trueIsAbstract)
            {
                builder.Add(
                    // (13,35): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                    //     abstract static bool operator true (I1 x);
                    Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "true").WithLocation(13, 35)
                    );
            }

            if (falseIsAbstract)
            {
                builder.Add(
                    // (14,35): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                    //     abstract static bool operator false (I1 x);
                    Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "false").WithLocation(14, 35)
                    );
            }

            compilation3.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation).Verify(builder.ToArrayAndFree());
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<<")]
        [InlineData(">>")]
        public void ConsumeAbstractCompoundBinaryOperator_04(string op)
        {
            var source1 =
@"
public interface I1<T> where T : I1<T>
{
    abstract static T operator" + op + @" (T x, int y);
}
";
            var source2 =
@"
class Test
{
    static void M02<T>(T x, int y) where T : I1<T>
    {
        x " + op + @"= y;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,9): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         x *= y;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "x " + op + "= y").WithLocation(6, 9)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,31): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static T operator* (T x, int y);
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, op).WithLocation(12, 31)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticPropertyGet_01()
        {
            var source1 =
@"
interface I1
{
    abstract static int P01 { get; set;}

    static void M02()
    {
        _ = P01;
        _ = P04;
    }

    void M03()
    {
        _ = this.P01;
        _ = this.P04;
    }

    static int P04 { get; set; }

    protected abstract static int P05 { get; set; }
}

class Test
{
    static void MT1(I1 x)
    {
        _ = I1.P01;
        _ = x.P01;
        _ = I1.P04;
        _ = x.P04;
    }

    static void MT2<T>() where T : I1
    {
        _ = T.P03;
        _ = T.P04;
        _ = T.P00;
        _ = T.P05;

        _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01.ToString());
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = P01;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "P01").WithLocation(8, 13),
                // (14,13): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = this.P01;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P01").WithArguments("I1.P01").WithLocation(14, 13),
                // (15,13): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = this.P04;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P04").WithArguments("I1.P04").WithLocation(15, 13),
                // (27,13): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         _ = I1.P01;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "I1.P01").WithLocation(27, 13),
                // (28,13): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = x.P01;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P01").WithArguments("I1.P01").WithLocation(28, 13),
                // (30,13): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = x.P04;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P04").WithArguments("I1.P04").WithLocation(30, 13),
                // (35,13): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = T.P03;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 13),
                // (36,13): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = T.P04;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 13),
                // (37,13): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = T.P00;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 13),
                // (38,15): error CS0122: 'I1.P05' is inaccessible due to its protection level
                //         _ = T.P05;
                Diagnostic(ErrorCode.ERR_BadAccess, "P05").WithArguments("I1.P05").WithLocation(38, 15),
                // (40,71): error CS9108: An expression tree may not contain an access of static abstract interface member
                //         _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01.ToString());
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, "T.P01").WithLocation(40, 71)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticPropertySet_01()
        {
            var source1 =
@"
interface I1
{
    abstract static int P01 { get; set;}

    static void M02()
    {
        P01 = 1;
        P04 = 1;
    }

    void M03()
    {
        this.P01 = 1;
        this.P04 = 1;
    }

    static int P04 { get; set; }

    protected abstract static int P05 { get; set; }
}

class Test
{
    static void MT1(I1 x)
    {
        I1.P01 = 1;
        x.P01 = 1;
        I1.P04 = 1;
        x.P04 = 1;
    }

    static void MT2<T>() where T : I1
    {
        T.P03 = 1;
        T.P04 = 1;
        T.P00 = 1;
        T.P05 = 1;

        _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 = 1);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         P01 = 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "P01").WithLocation(8, 9),
                // (14,9): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.P01 = 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P01").WithArguments("I1.P01").WithLocation(14, 9),
                // (15,9): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.P04 = 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P04").WithArguments("I1.P04").WithLocation(15, 9),
                // (27,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         I1.P01 = 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "I1.P01").WithLocation(27, 9),
                // (28,9): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.P01 = 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P01").WithArguments("I1.P01").WithLocation(28, 9),
                // (30,9): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.P04 = 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P04").WithArguments("I1.P04").WithLocation(30, 9),
                // (35,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P03 = 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 9),
                // (36,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P04 = 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 9),
                // (37,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P00 = 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 9),
                // (38,11): error CS0122: 'I1.P05' is inaccessible due to its protection level
                //         T.P05 = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "P05").WithArguments("I1.P05").WithLocation(38, 11),
                // (40,71): error CS0832: An expression tree may not contain an assignment operator
                //         _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 = 1);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "T.P01 = 1").WithLocation(40, 71),
                // (40,71): error CS9108: An expression tree may not contain an access of static abstract interface member
                //         _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 = 1);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, "T.P01").WithLocation(40, 71)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticPropertyCompound_01()
        {
            var source1 =
@"
interface I1
{
    abstract static int P01 { get; set;}

    static void M02()
    {
        P01 += 1;
        P04 += 1;
    }

    void M03()
    {
        this.P01 += 1;
        this.P04 += 1;
    }

    static int P04 { get; set; }

    protected abstract static int P05 { get; set; }
}

class Test
{
    static void MT1(I1 x)
    {
        I1.P01 += 1;
        x.P01 += 1;
        I1.P04 += 1;
        x.P04 += 1;
    }

    static void MT2<T>() where T : I1
    {
        T.P03 += 1;
        T.P04 += 1;
        T.P00 += 1;
        T.P05 += 1;

        _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 += 1);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         P01 += 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "P01").WithLocation(8, 9),
                // (8,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         P01 += 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "P01").WithLocation(8, 9),
                // (14,9): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.P01 += 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P01").WithArguments("I1.P01").WithLocation(14, 9),
                // (15,9): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.P04 += 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P04").WithArguments("I1.P04").WithLocation(15, 9),
                // (27,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         I1.P01 += 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "I1.P01").WithLocation(27, 9),
                // (27,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         I1.P01 += 1;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "I1.P01").WithLocation(27, 9),
                // (28,9): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.P01 += 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P01").WithArguments("I1.P01").WithLocation(28, 9),
                // (30,9): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.P04 += 1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P04").WithArguments("I1.P04").WithLocation(30, 9),
                // (35,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P03 += 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 9),
                // (36,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P04 += 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 9),
                // (37,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P00 += 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 9),
                // (38,11): error CS0122: 'I1.P05' is inaccessible due to its protection level
                //         T.P05 += 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "P05").WithArguments("I1.P05").WithLocation(38, 11),
                // (40,71): error CS0832: An expression tree may not contain an assignment operator
                //         _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 += 1);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "T.P01 += 1").WithLocation(40, 71),
                // (40,71): error CS9108: An expression tree may not contain an access of static abstract interface member
                //         _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 += 1);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess, "T.P01").WithLocation(40, 71)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticProperty_02()
        {
            var source1 =
@"
interface I1
{
    abstract static int P01 { get; set; }

    static void M02()
    {
        _ = nameof(P01);
        _ = nameof(P04);
    }

    void M03()
    {
        _ = nameof(this.P01);
        _ = nameof(this.P04);
    }

    static int P04 { get; set; }

    protected abstract static int P05 { get; set; }
}

class Test
{
    static void MT1(I1 x)
    {
        _ = nameof(I1.P01);
        _ = nameof(x.P01);
        _ = nameof(I1.P04);
        _ = nameof(x.P04);
    }

    static void MT2<T>() where T : I1
    {
        _ = nameof(T.P03);
        _ = nameof(T.P04);
        _ = nameof(T.P00);
        _ = nameof(T.P05);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (14,20): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = nameof(this.P01);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P01").WithArguments("I1.P01").WithLocation(14, 20),
                // (15,20): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = nameof(this.P04);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P04").WithArguments("I1.P04").WithLocation(15, 20),
                // (28,20): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = nameof(x.P01);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P01").WithArguments("I1.P01").WithLocation(28, 20),
                // (30,20): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = nameof(x.P04);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P04").WithArguments("I1.P04").WithLocation(30, 20),
                // (35,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.P03);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 20),
                // (36,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.P04);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 20),
                // (37,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.P00);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 20),
                // (38,22): error CS0122: 'I1.P05' is inaccessible due to its protection level
                //         _ = nameof(T.P05);
                Diagnostic(ErrorCode.ERR_BadAccess, "P05").WithArguments("I1.P05").WithLocation(38, 22)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticPropertyGet_03()
        {
            var source1 =
@"
public interface I1
{
    abstract static int P01 { get; set; }
}

class Test
{
    static void M02<T>() where T : I1
    {
        _ = T.P01;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("Test.M02<T>()",
@"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  constrained. ""T""
  IL_0007:  call       ""int I1.P01.get""
  IL_000c:  pop
  IL_000d:  ret
}
");

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First().Right;

            Assert.Equal("T.P01", node.ToString());
            VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" qualifier is important for this invocation, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IPropertyReferenceOperation: System.Int32 I1.P01 { get; set; } (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'T.P01')
  Instance Receiver: 
    null
");

            var m02 = compilation1.GetMember<MethodSymbol>("Test.M02");

            Assert.Equal("System.Int32 I1.P01 { get; set; }", ((CSharpSemanticModel)model).LookupSymbols(node.SpanStart, m02.TypeParameters[0], "P01").Single().ToTestDisplayString());
        }

        [Fact]
        public void ConsumeAbstractStaticPropertySet_03()
        {
            var source1 =
@"
public interface I1
{
    abstract static int P01 { get; set; }
}

class Test
{
    static void M02<T>() where T : I1
    {
        T.P01 = 1;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("Test.M02<T>()",
@"
{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  constrained. ""T""
  IL_0008:  call       ""void I1.P01.set""
  IL_000d:  nop
  IL_000e:  ret
}
");

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First().Left;

            Assert.Equal("T.P01", node.ToString());
            VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" qualifier is important for this invocation, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IPropertyReferenceOperation: System.Int32 I1.P01 { get; set; } (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'T.P01')
  Instance Receiver: 
    null
");

            var m02 = compilation1.GetMember<MethodSymbol>("Test.M02");

            Assert.Equal("System.Int32 I1.P01 { get; set; }", ((CSharpSemanticModel)model).LookupSymbols(node.SpanStart, m02.TypeParameters[0], "P01").Single().ToTestDisplayString());
        }

        [Fact]
        public void ConsumeAbstractStaticPropertyCompound_03()
        {
            var source1 =
@"
public interface I1
{
    abstract static int P01 { get; set; }
}

class Test
{
    static void M02<T>() where T : I1
    {
        T.P01 += 1;
    }

    static string M03<T>() where T : I1
    {
        return nameof(T.P01);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("Test.M02<T>()",
@"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  constrained. ""T""
  IL_0007:  call       ""int I1.P01.get""
  IL_000c:  ldc.i4.1
  IL_000d:  add
  IL_000e:  constrained. ""T""
  IL_0014:  call       ""void I1.P01.set""
  IL_0019:  nop
  IL_001a:  ret
}
");

            verifier.VerifyIL("Test.M03<T>()",
@"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (string V_0)
  IL_0000:  nop
  IL_0001:  ldstr      ""P01""
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0009
  IL_0009:  ldloc.0
  IL_000a:  ret
}
");

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First().Left;

            Assert.Equal("T.P01", node.ToString());
            VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" qualifier is important for this invocation, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IPropertyReferenceOperation: System.Int32 I1.P01 { get; set; } (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'T.P01')
  Instance Receiver: 
    null
");

            var m02 = compilation1.GetMember<MethodSymbol>("Test.M02");

            Assert.Equal("System.Int32 I1.P01 { get; set; }", ((CSharpSemanticModel)model).LookupSymbols(node.SpanStart, m02.TypeParameters[0], "P01").Single().ToTestDisplayString());
        }

        [Fact]
        public void ConsumeAbstractStaticPropertyGet_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static int P01 { get; set; }
}
";
            var source2 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        _ = T.P01;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,13): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         _ = T.P01;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "T.P01").WithLocation(6, 13)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,31): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static int P01 { get; set; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "get").WithLocation(12, 31),
                // (12,36): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static int P01 { get; set; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "set").WithLocation(12, 36)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticPropertySet_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static int P01 { get; set; }
}
";
            var source2 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T.P01 = 1;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,9): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         T.P01 = 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "T.P01").WithLocation(6, 9)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,31): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static int P01 { get; set; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "get").WithLocation(12, 31),
                // (12,36): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static int P01 { get; set; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "set").WithLocation(12, 36)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticPropertyCompound_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static int P01 { get; set; }
}
";
            var source2 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T.P01 += 1;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,9): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         T.P01 += 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "T.P01").WithLocation(6, 9),
                // (6,9): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         T.P01 += 1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "T.P01").WithLocation(6, 9)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,31): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static int P01 { get; set; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "get").WithLocation(12, 31),
                // (12,36): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static int P01 { get; set; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "set").WithLocation(12, 36)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticEventAdd_01()
        {
            var source1 =
@"#pragma warning disable CS0067 // The event is never used
interface I1
{
    abstract static event System.Action P01;

    static void M02()
    {
        P01 += null;
        P04 += null;
    }

    void M03()
    {
        this.P01 += null;
        this.P04 += null;
    }

    static event System.Action P04;

    protected abstract static event System.Action P05;
}

class Test
{
    static void MT1(I1 x)
    {
        I1.P01 += null;
        x.P01 += null;
        I1.P04 += null;
        x.P04 += null;
    }

    static void MT2<T>() where T : I1
    {
        T.P03 += null;
        T.P04 += null;
        T.P00 += null;
        T.P05 += null;

        _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 += null);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         P01 += null;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "P01 += null").WithLocation(8, 9),
                // (14,9): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.P01 += null;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P01").WithArguments("I1.P01").WithLocation(14, 9),
                // (14,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         this.P01 += null;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "this.P01 += null").WithLocation(14, 9),
                // (15,9): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.P04 += null;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P04").WithArguments("I1.P04").WithLocation(15, 9),
                // (27,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         I1.P01 += null;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "I1.P01 += null").WithLocation(27, 9),
                // (28,9): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.P01 += null;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P01").WithArguments("I1.P01").WithLocation(28, 9),
                // (28,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         x.P01 += null;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "x.P01 += null").WithLocation(28, 9),
                // (30,9): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.P04 += null;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P04").WithArguments("I1.P04").WithLocation(30, 9),
                // (35,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P03 += null;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 9),
                // (36,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P04 += null;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 9),
                // (37,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P00 += null;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 9),
                // (38,11): error CS0122: 'I1.P05' is inaccessible due to its protection level
                //         T.P05 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P05").WithArguments("I1.P05").WithLocation(38, 11),
                // (40,71): error CS0832: An expression tree may not contain an assignment operator
                //         _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 += null);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "T.P01 += null").WithLocation(40, 71)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticEventRemove_01()
        {
            var source1 =
@"#pragma warning disable CS0067 // The event is never used
interface I1
{
    abstract static event System.Action P01;

    static void M02()
    {
        P01 -= null;
        P04 -= null;
    }

    void M03()
    {
        this.P01 -= null;
        this.P04 -= null;
    }

    static event System.Action P04;

    protected abstract static event System.Action P05;
}

class Test
{
    static void MT1(I1 x)
    {
        I1.P01 -= null;
        x.P01 -= null;
        I1.P04 -= null;
        x.P04 -= null;
    }

    static void MT2<T>() where T : I1
    {
        T.P03 -= null;
        T.P04 -= null;
        T.P00 -= null;
        T.P05 -= null;

        _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 -= null);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (8,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         P01 -= null;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "P01 -= null").WithLocation(8, 9),
                // (14,9): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.P01 -= null;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P01").WithArguments("I1.P01").WithLocation(14, 9),
                // (14,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         this.P01 -= null;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "this.P01 -= null").WithLocation(14, 9),
                // (15,9): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         this.P04 -= null;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P04").WithArguments("I1.P04").WithLocation(15, 9),
                // (27,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         I1.P01 -= null;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "I1.P01 -= null").WithLocation(27, 9),
                // (28,9): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.P01 -= null;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P01").WithArguments("I1.P01").WithLocation(28, 9),
                // (28,9): error CS9107: A static abstract interface member can be accessed only on a type parameter.
                //         x.P01 -= null;
                Diagnostic(ErrorCode.ERR_BadAbstractStaticMemberAccess, "x.P01 -= null").WithLocation(28, 9),
                // (30,9): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         x.P04 -= null;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P04").WithArguments("I1.P04").WithLocation(30, 9),
                // (35,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P03 -= null;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 9),
                // (36,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P04 -= null;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 9),
                // (37,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.P00 -= null;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 9),
                // (38,11): error CS0122: 'I1.P05' is inaccessible due to its protection level
                //         T.P05 -= null;
                Diagnostic(ErrorCode.ERR_BadAccess, "P05").WithArguments("I1.P05").WithLocation(38, 11),
                // (40,71): error CS0832: An expression tree may not contain an assignment operator
                //         _ = (System.Linq.Expressions.Expression<System.Action>)(() => T.P01 -= null);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "T.P01 -= null").WithLocation(40, 71)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticEvent_02()
        {
            var source1 =
@"
interface I1
{
    abstract static event System.Action P01;

    static void M02()
    {
        _ = nameof(P01);
        _ = nameof(P04);
    }

    void M03()
    {
        _ = nameof(this.P01);
        _ = nameof(this.P04);
    }

    static event System.Action P04;

    protected abstract static event System.Action P05;
}

class Test
{
    static void MT1(I1 x)
    {
        _ = nameof(I1.P01);
        _ = nameof(x.P01);
        _ = nameof(I1.P04);
        _ = nameof(x.P04);
    }

    static void MT2<T>() where T : I1
    {
        _ = nameof(T.P03);
        _ = nameof(T.P04);
        _ = nameof(T.P00);
        _ = nameof(T.P05);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (14,20): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = nameof(this.P01);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P01").WithArguments("I1.P01").WithLocation(14, 20),
                // (15,20): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = nameof(this.P04);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.P04").WithArguments("I1.P04").WithLocation(15, 20),
                // (28,20): error CS0176: Member 'I1.P01' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = nameof(x.P01);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P01").WithArguments("I1.P01").WithLocation(28, 20),
                // (30,20): error CS0176: Member 'I1.P04' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = nameof(x.P04);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "x.P04").WithArguments("I1.P04").WithLocation(30, 20),
                // (35,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.P03);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(35, 20),
                // (36,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.P04);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(36, 20),
                // (37,20): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         _ = nameof(T.P00);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(37, 20),
                // (38,22): error CS0122: 'I1.P05' is inaccessible due to its protection level
                //         _ = nameof(T.P05);
                Diagnostic(ErrorCode.ERR_BadAccess, "P05").WithArguments("I1.P05").WithLocation(38, 22)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticEvent_03()
        {
            var source1 =
@"
public interface I1
{
    abstract static event System.Action E01;
}

class Test
{
    static void M02<T>() where T : I1
    {
        T.E01 += null;
        T.E01 -= null;
    }

    static string M03<T>() where T : I1
    {
        return nameof(T.E01);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation1, verify: Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("Test.M02<T>()",
@"
{
  // Code size       28 (0x1c)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  constrained. ""T""
  IL_0008:  call       ""void I1.E01.add""
  IL_000d:  nop
  IL_000e:  ldnull
  IL_000f:  constrained. ""T""
  IL_0015:  call       ""void I1.E01.remove""
  IL_001a:  nop
  IL_001b:  ret
}
");

            verifier.VerifyIL("Test.M03<T>()",
@"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (string V_0)
  IL_0000:  nop
  IL_0001:  ldstr      ""E01""
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0009
  IL_0009:  ldloc.0
  IL_000a:  ret
}
");

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().First().Left;

            Assert.Equal("T.E01", node.ToString());
            VerifyOperationTreeForNode(compilation1, model, node,
// PROTOTYPE(StaticAbstractMembersInInterfaces): It feels like the "T" qualifier is important for this invocation, but it is not 
//                                               reflected in the IOperation tree. Should we change the shape of the tree in order
//                                               to expose this information? 
@"
IEventReferenceOperation: event System.Action I1.E01 (Static) (OperationKind.EventReference, Type: System.Action) (Syntax: 'T.E01')
  Instance Receiver: 
    null
");

            var m02 = compilation1.GetMember<MethodSymbol>("Test.M02");

            Assert.Equal("event System.Action I1.E01", ((CSharpSemanticModel)model).LookupSymbols(node.SpanStart, m02.TypeParameters[0], "E01").Single().ToTestDisplayString());
        }

        [Fact]
        public void ConsumeAbstractStaticPropertyAdd_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static event System.Action P01;
}
";
            var source2 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T.P01 += null;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,9): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         T.P01 += null;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "T.P01 += null").WithLocation(6, 9)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,41): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static event System.Action P01;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "P01").WithLocation(12, 41)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticPropertyRemove_04()
        {
            var source1 =
@"
public interface I1
{
    abstract static event System.Action P01;
}
";
            var source2 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T.P01 -= null;
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var compilation2 = CreateCompilation(source2, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended,
                                                 references: new[] { compilation1.ToMetadataReference() });

            compilation2.VerifyDiagnostics(
                // (6,9): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //         T.P01 -= null;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "T.P01 -= null").WithLocation(6, 9)
                );

            var compilation3 = CreateCompilation(source2 + source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.DesktopLatestExtended);

            compilation3.VerifyDiagnostics(
                // (12,41): error CS9100: Target runtime doesn't support static abstract members in interfaces.
                //     abstract static event System.Action P01;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, "P01").WithLocation(12, 41)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticIndexedProperty_03()
        {
            var ilSource = @"
.class interface public auto ansi abstract I1
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    // Methods
    .method public hidebysig specialname newslot abstract virtual 
        static int32 get_Item (
            int32 x
        ) cil managed 
    {
    } // end of method I1::get_Item

    .method public hidebysig specialname newslot abstract virtual 
        static void set_Item (
            int32 x,
            int32 'value'
        ) cil managed 
    {
    } // end of method I1::set_Item

    // Properties
    .property int32 Item(
        int32 x
    )
    {
        .get int32 I1::get_Item(int32)
        .set void I1::set_Item(int32, int32)
    }

} // end of class I1
";

            var source1 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T.Item[0] += 1;
    }

    static string M03<T>() where T : I1
    {
        return nameof(T.Item);
    }
}
";
            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (6,9): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         T.Item[0] += 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(6, 9),
                // (11,23): error CS0119: 'T' is a type parameter, which is not valid in the given context
                //         return nameof(T.Item);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type parameter").WithLocation(11, 23)
                );

            var source2 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T[0] += 1;
    }

    static void M03<T>() where T : I1
    {
        T.set_Item(0, T.get_Item(0) + 1);
    }
}
";
            var compilation2 = CreateCompilationWithIL(source2, ilSource, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation2.VerifyDiagnostics(
                // (6,9): error CS0119: 'T' is a type, which is not valid in the given context
                //         T[0] += 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type").WithLocation(6, 9),
                // (11,11): error CS0571: 'I1.this[int].set': cannot explicitly call operator or accessor
                //         T.set_Item(0, T.get_Item(0) + 1);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_Item").WithArguments("I1.this[int].set").WithLocation(11, 11),
                // (11,25): error CS0571: 'I1.this[int].get': cannot explicitly call operator or accessor
                //         T.set_Item(0, T.get_Item(0) + 1);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Item").WithArguments("I1.this[int].get").WithLocation(11, 25)
                );
        }

        [Fact]
        public void ConsumeAbstractStaticIndexedProperty_04()
        {
            var ilSource = @"
.class interface public auto ansi abstract I1
{
    // Methods
    .method public hidebysig specialname newslot abstract virtual 
        static int32 get_Item (
            int32 x
        ) cil managed 
    {
    } // end of method I1::get_Item

    .method public hidebysig specialname newslot abstract virtual 
        static void set_Item (
            int32 x,
            int32 'value'
        ) cil managed 
    {
    } // end of method I1::set_Item

    // Properties
    .property int32 Item(
        int32 x
    )
    {
        .get int32 I1::get_Item(int32)
        .set void I1::set_Item(int32, int32)
    }

} // end of class I1
";

            var source1 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T.Item[0] += 1;
    }

    static string M03<T>() where T : I1
    {
        return nameof(T.Item);
    }
}
";
            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            compilation1.VerifyDiagnostics(
                // (6,11): error CS1545: Property, indexer, or event 'I1.Item[int]' is not supported by the language; try directly calling accessor methods 'I1.get_Item(int)' or 'I1.set_Item(int, int)'
                //         T.Item[0] += 1;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Item").WithArguments("I1.Item[int]", "I1.get_Item(int)", "I1.set_Item(int, int)").WithLocation(6, 11),
                // (11,25): error CS1545: Property, indexer, or event 'I1.Item[int]' is not supported by the language; try directly calling accessor methods 'I1.get_Item(int)' or 'I1.set_Item(int, int)'
                //         return nameof(T.Item);
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Item").WithArguments("I1.Item[int]", "I1.get_Item(int)", "I1.set_Item(int, int)").WithLocation(11, 25)
                );

            var source2 =
@"
class Test
{
    static void M02<T>() where T : I1
    {
        T.set_Item(0, T.get_Item(0) + 1);
    }
}
";
            var compilation2 = CreateCompilationWithIL(source2, ilSource, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview,
                                                 targetFramework: TargetFramework.NetCoreApp);

            var verifier = CompileAndVerify(compilation2, verify: Verification.Skipped).VerifyDiagnostics();

            verifier.VerifyIL("Test.M02<T>()",
@"
{
  // Code size       29 (0x1d)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  ldc.i4.0
  IL_0003:  constrained. ""T""
  IL_0009:  call       ""int I1.get_Item(int)""
  IL_000e:  ldc.i4.1
  IL_000f:  add
  IL_0010:  constrained. ""T""
  IL_0016:  call       ""void I1.set_Item(int, int)""
  IL_001b:  nop
  IL_001c:  ret
}
");
        }
    }
}
