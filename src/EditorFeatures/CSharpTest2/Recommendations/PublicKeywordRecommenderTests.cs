// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class PublicKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInCompilationUnit()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterExtern()
        {
            VerifyKeyword(
@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterUsing()
        {
            VerifyKeyword(
@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNamespace()
        {
            VerifyKeyword(
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeDeclaration()
        {
            VerifyKeyword(
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDelegateDeclaration()
        {
            VerifyKeyword(
@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAssemblyAttribute()
        {
            VerifyKeyword(
@"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRootAttribute()
        {
            VerifyKeyword(
@"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [goo]
  $$");
        }

        // This will be fixed once we have accessibility for members
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideInterface()
        {
            VerifyKeyword(
@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPartial()
            => VerifyAbsence(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAbstract()
        {
            VerifyKeyword(
@"abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterInternal()
            => VerifyAbsence(@"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPublic()
            => VerifyAbsence(@"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterStaticPublic()
            => VerifyAbsence(@"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPublicStatic()
            => VerifyAbsence(@"public static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterInvalidPublic()
            => VerifyAbsence(@"virtual public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPrivate()
            => VerifyAbsence(@"private $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterProtected()
            => VerifyAbsence(@"protected $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterSealed()
        {
            VerifyKeyword(
@"sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterStatic()
        {
            VerifyKeyword(
@"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterStaticInUsingDirective()
        {
            VerifyAbsence(
@"using static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass()
            => VerifyAbsence(@"class $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDelegate()
            => VerifyAbsence(@"delegate $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBetweenUsings()
        {
            VerifyAbsence(AddInsideMethod(
@"using Goo;
$$
using Bar;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedVirtual()
        {
            VerifyKeyword(
@"class C {
    virtual $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedOverride()
        {
            VerifyKeyword(
@"class C {
    override $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterEnum()
        {
            VerifyKeyword(
@"enum E {
}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDocComment1()
        {
            VerifyKeyword(
@"using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Subclass this class and define private fields for OptionSet
/// </summary>
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDocComment2()
        {
            VerifyKeyword(
@"using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Subclass this class and define private fields for OptionSet
/// </summary>

$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDocComment3()
        {
            VerifyKeyword(
@"using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Subclass this class and define private fields for OptionSet
/// </summary>

$$
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDocComment4()
        {
            VerifyKeyword(
@"using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Subclass this class and define private fields for OptionSet
/// </summary>
  
$$
  ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterComment1()
        {
            VerifyKeyword(
@"// </summary>  
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterComment2()
        {
            VerifyKeyword(
@"// </summary>  

$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterComment3()
        {
            VerifyKeyword(
@"// </summary>  
$$
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterComment4()
        {
            VerifyKeyword(
@"// </summary>  

$$
");
        }
    }
}
