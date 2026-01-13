// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class ClosedClassesTests : CSharpTestBase
{
    [Fact]
    public void LangVersion()
    {
        var source = """
            closed class C { }
            """;

        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular14);
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS8652: The feature 'closed classes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // closed class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("closed classes").WithLocation(1, 14));

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularNext);
        comp.VerifyEmitDiagnostics();

        comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void InheritFromMetadata_01()
    {
        var source1 = """
            public closed class C { }
            """;
        var source2 = """
            public class D : C { }
            """;

        var comp = CreateCompilation([source1, source2]);
        comp.VerifyEmitDiagnostics();

        var comp1 = CreateCompilation(source1);

        // PROTOTYPE(cc): other module should be blocked from inheriting
        var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()]);
        comp2.VerifyEmitDiagnostics();

        comp2 = CreateCompilation(source2, references: [comp1.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics();

        comp1 = CreateCompilation(source1, options: TestOptions.ReleaseModule);
        comp2 = CreateCompilation(source2, references: [comp1.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics();
    }

    // PROTOTYPE(cc): NamedTypeSymbol.IsClosed API

    [Fact]
    public void Sealed_01()
    {
        var source = """
            sealed closed class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS9366: 'C': a closed type cannot be sealed or static
            // sealed closed class C { }
            Diagnostic(ErrorCode.ERR_ClosedSealedStatic, "C").WithArguments("C").WithLocation(1, 21));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsSealed);
    }

    [Fact]
    public void Static_01()
    {
        var source = """
            static closed class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS9366: 'C': a closed type cannot be sealed or static
            // static closed class C { }
            Diagnostic(ErrorCode.ERR_ClosedSealedStatic, "C").WithArguments("C").WithLocation(1, 21));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsStatic);
    }

    [Fact]
    public void Abstract_01()
    {
        var source = """
            abstract closed class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
    }

    [Fact]
    public void ImplicitlyAbstract_01()
    {
        var source1 = """
            public closed class C { }
            """;

        var source2 = """
            new C(); // 1
            """;

        var comp = CreateCompilation([source1, source2]);
        comp.VerifyEmitDiagnostics(
            // (1,1): error CS0144: Cannot create an instance of the abstract type or interface 'C'
            // new C(); // 1
            Diagnostic(ErrorCode.ERR_NoNewAbstract, "new C()").WithArguments("C").WithLocation(1, 1));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);

        var referenceComp = CreateCompilation(source1);
        verifyReference(referenceComp.ToMetadataReference());
        verifyReference(referenceComp.EmitToImageReference());

        void verifyReference(MetadataReference reference)
        {
            var comp2 = CreateCompilation("""
                new C(); // 1
                """, references: [reference]);
            comp2.VerifyEmitDiagnostics(
                // (1,1): error CS0144: Cannot create an instance of the abstract type or interface 'C'
                // new C(); // 1
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new C()").WithArguments("C").WithLocation(1, 1));

            var classC2 = comp2.GetMember<NamedTypeSymbol>("C");
            Assert.True(classC2.IsAbstract);
        }
    }

    [Fact]
    public void ImplicitlyAbstract_02()
    {
        var source = """
            abstract class Base
            {
                public abstract void M();
            }

            closed class C : Base { }

            class D : C { }
            class E : C
            {
                public override void M() { }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (8,7): error CS0534: 'D' does not implement inherited abstract member 'Base.M()'
            // class D : C { }
            Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "D").WithArguments("D", "Base.M()").WithLocation(8, 7));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
    }

    [Fact]
    public void ImplicitlyAbstract_03()
    {
        var source = """
            closed class C
            {
                public abstract void M();
            }
            class D : C { }

            class E : C
            {
                public override void M() { }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (5,7): error CS0534: 'D' does not implement inherited abstract member 'C.M()'
            // class D : C { }
            Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "D").WithArguments("D", "C.M()").WithLocation(5, 7));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
    }

    [Fact]
    public void BadTypeKind_01()
    {
        var source = """
            closed interface I { } // 1
            closed enum E { } // 2
            closed delegate void D(); // 3
            closed struct S { } // 4

            class C
            {
                closed void M() { } // 5
                closed int P { get; set; } // 6
                closed event System.Action E; // 7
                closed string F; // 8
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,18): error CS0106: The modifier 'closed' is not valid for this item
            // closed interface I { } // 1
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "I").WithArguments("closed").WithLocation(1, 18),
            // (2,13): error CS0106: The modifier 'closed' is not valid for this item
            // closed enum E { } // 2
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("closed").WithLocation(2, 13),
            // (3,22): error CS0106: The modifier 'closed' is not valid for this item
            // closed delegate void D(); // 3
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "D").WithArguments("closed").WithLocation(3, 22),
            // (4,15): error CS0106: The modifier 'closed' is not valid for this item
            // closed struct S { } // 4
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("closed").WithLocation(4, 15),
            // (8,17): error CS0106: The modifier 'closed' is not valid for this item
            //     closed void M() { } // 5
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("closed").WithLocation(8, 17),
            // (9,16): error CS0106: The modifier 'closed' is not valid for this item
            //     closed int P { get; set; } // 6
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "P").WithArguments("closed").WithLocation(9, 16),
            // (10,32): error CS0106: The modifier 'closed' is not valid for this item
            //     closed event System.Action E; // 7
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("closed").WithLocation(10, 32),
            // (10,32): warning CS0067: The event 'C.E' is never used
            //     closed event System.Action E; // 7
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(10, 32),
            // (11,19): error CS0106: The modifier 'closed' is not valid for this item
            //     closed string F; // 8
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "F").WithArguments("closed").WithLocation(11, 19),
            // (11,19): warning CS0169: The field 'C.F' is never used
            //     closed string F; // 8
            Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("C.F").WithLocation(11, 19));
    }
}
