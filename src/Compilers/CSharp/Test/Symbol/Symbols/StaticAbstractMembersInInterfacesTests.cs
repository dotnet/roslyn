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
        public void SealedStaticInstruct_01()
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
    }
}
