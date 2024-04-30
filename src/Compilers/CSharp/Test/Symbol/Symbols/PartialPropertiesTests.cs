// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class PartialPropertiesTests : CSharpTestBase
    {
        [Fact]
        public void MissingDeclaration_01()
        {
            // definition without implementation
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS9300: Partial property 'C.P' must have an implementation part.
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P").WithArguments("C.P").WithLocation(3, 17)
                );

            var cClass = comp.GetMember<NamedTypeSymbol>("C");
            AssertEx.Equal([
                "System.Int32 C.P { get; set; }",
                "System.Int32 C.P.get",
                "void C.P.set",
                "C..ctor()"
                ],
                cClass.GetMembers().SelectAsArray(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void MissingDeclaration_02()
        {
            // implementation without definition
            var source = """
                partial class C
                {
                    partial int P { get => throw null!; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS9301: Partial property 'C.P' must have an definition part.
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "P").WithArguments("C.P").WithLocation(3, 17)
                );
        }

        [Fact]
        public void DuplicateDeclaration_01()
        {
            // duplicate definition
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    partial int P { get; set; }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS9300: Partial property 'C.P' must have an implementation part.
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P").WithArguments("C.P").WithLocation(3, 17),
                // (4,17): error CS9302: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P").WithLocation(4, 17),
                // (4,17): error CS0102: The type 'C' already contains a definition for 'P'
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 17)
                );
        }

        [Fact]
        public void DuplicateDeclaration_02()
        {
            // duplicate definition with single implementation
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    partial int P { get; set; }
                    partial int P { get => throw null!; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9302: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P").WithLocation(4, 17),
                // (4,17): error CS0102: The type 'C' already contains a definition for 'P'
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 17)
                );
        }

        [Fact]
        public void DuplicateDeclaration_03()
        {
            // duplicate implementation
            var source = """
                partial class C
                {
                    partial int P { get => throw null!; set { } }
                    partial int P { get => throw null!; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS9301: Partial property 'C.P' must have an definition part.
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "P").WithArguments("C.P").WithLocation(3, 17),
                // (4,17): error CS9303: A partial property may not have multiple implementing declarations
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "P").WithLocation(4, 17),
                // (4,17): error CS0102: The type 'C' already contains a definition for 'P'
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 17)
                );
        }

        [Fact]
        public void DuplicateDeclaration_04()
        {
            // duplicate implementation with single definition
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    partial int P { get => throw null!; set { } }
                    partial int P { get => throw null!; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,17): error CS9303: A partial property may not have multiple implementing declarations
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "P").WithLocation(5, 17),
                // (5,17): error CS0102: The type 'C' already contains a definition for 'P'
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(5, 17)
                );
        }

        [Fact]
        public void DuplicateDeclaration_05()
        {
            // duplicate implementation and duplicate definition
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    partial int P { get; set; }
                    partial int P { get => throw null!; set { } }
                    partial int P { get => throw null!; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9302: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P").WithLocation(4, 17),
                // (4,17): error CS0102: The type 'C' already contains a definition for 'P'
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 17),
                // (6,17): error CS9303: A partial property may not have multiple implementing declarations
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "P").WithLocation(6, 17),
                // (6,17): error CS0102: The type 'C' already contains a definition for 'P'
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(6, 17)
                );
        }

        [Fact]
        public void MissingAccessor_01()
        {
            // implementation missing setter
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    partial int P { get => throw null!; }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9304: Property accessor 'C.P.set' must be implemented because it is declared on the definition part
                //     partial int P { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingAccessor, "P").WithArguments("C.P.set").WithLocation(4, 17)
                );
        }

        [Fact]
        public void MissingAccessor_02()
        {
            // implementation missing getter
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    partial int P { get => throw null!; }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9304: Property accessor 'C.P.set' must be implemented because it is declared on the definition part
                //     partial int P { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingAccessor, "P").WithArguments("C.P.set").WithLocation(4, 17)
                );
        }

        [Fact]
        public void MissingAccessor_03()
        {
            // implementation missing init
            var source = """
                partial class C
                {
                    partial int P { get; init; }
                    partial int P { get => throw null!; }
                }
                """;
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9304: Property accessor 'C.P.init' must be implemented because it is declared on the definition part
                //     partial int P { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingAccessor, "P").WithArguments("C.P.init").WithLocation(4, 17)
                );
        }

        [Fact]
        public void UnexpectedAccessor_01()
        {
            // implementation unexpected setter
            var source = """
                partial class C
                {
                    partial int P { get; }
                    partial int P { get => throw null!; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9305: Property accessor 'C.P.set' does not implement any accessor declared on the definition part
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyUnexpectedAccessor, "P").WithArguments("C.P.set").WithLocation(4, 17)
                );
        }

        [Fact]
        public void UnexpectedAccessor_02()
        {
            // implementation unexpected getter
            var source = """
                partial class C
                {
                    partial int P { set; }
                    partial int P { get => throw null!; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9305: Property accessor 'C.P.get' does not implement any accessor declared on the definition part
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyUnexpectedAccessor, "P").WithArguments("C.P.get").WithLocation(4, 17)
                );
        }

        [Fact]
        public void UnexpectedAccessor_03()
        {
            // implementation unexpected init
            var source = """
                partial class C
                {
                    partial int P { get; }
                    partial int P { get => throw null!; init { } }
                }
                """;
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9305: Property accessor 'C.P.init' does not implement any accessor declared on the definition part
                //     partial int P { get => throw null!; init { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyUnexpectedAccessor, "P").WithArguments("C.P.init").WithLocation(4, 17)
                );
        }

        [Fact]
        public void AccessorKind_01()
        {
            // definition has set but implementation has init
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    partial int P { get => throw null!; init { } }
                }
                """;
            // PROTOTYPE(partial-properties): give an error diagnostic for an accessor kind difference
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                );
        }

        [Fact]
        public void AccessorKind_02()
        {
            // definition has init but implementation has set
            var source = """
                partial class C
                {
                    partial int P { get; init; }
                    partial int P { get => throw null!; set { } }
                }
                """;
            // PROTOTYPE(partial-properties): give an error diagnostic for an accessor kind difference
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                );
        }

        [Fact]
        public void Extern_01()
        {
            // PROTOTYPE(partial-properties): test that appropriate flags are set in metadata for the property accessors.
            // See ExtendedPartialMethodsTests.Extern_Symbols as a starting point.
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    extern partial int P { get; set; }
                }
                """;
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                );

            var prop = comp.GetMember<SourcePropertySymbol>("C.P");
            // PROTOTYPE(partial-properties): a partial method definition should delegate to its implementation part to implement this API, i.e. return 'true' here
            Assert.False(prop.GetPublicSymbol().IsExtern);
            Assert.True(prop.PartialImplementationPart!.GetPublicSymbol().IsExtern);
        }

        [Fact]
        public void Extern_02()
        {
            var source = """
                partial class C
                {
                    extern partial int P { get; set; }
                    extern partial int P { get; set; }
                }
                """;
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9301: Partial property 'C.P' must have an definition part.
                //     extern partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "P").WithArguments("C.P").WithLocation(3, 24),
                // (4,24): error CS9303: A partial property may not have multiple implementing declarations
                //     extern partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "P").WithLocation(4, 24),
                // (4,24): error CS0102: The type 'C' already contains a definition for 'P'
                //     extern partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 24)
                );
        }

        [Fact]
        public void Semantics_01()
        {
            // happy definition + implementation case
            var source = """
                using System;

                var c = new C { P = 1 };
                Console.Write(c.P);

                partial class C
                {
                    public partial int P { get; set; }
                }

                partial class C
                {
                    private int _p;
                    public partial int P { get => _p; set => _p = value; }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.P.get", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "int C._p"
                  IL_0006:  ret
                }
                """);
            verifier.VerifyIL("C.P.set", """
                {
                  // Code size        8 (0x8)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  stfld      "int C._p"
                  IL_0007:  ret
                }
                """);

            var comp = (CSharpCompilation)verifier.Compilation;
            var cClass = comp.GetMember<NamedTypeSymbol>("C");
            var members = cClass.GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            AssertEx.Equal([
                "System.Int32 C.P { get; set; }",
                "System.Int32 C.P.get",
                "void C.P.set",
                "System.Int32 C._p",
                "C..ctor()"
                ], members);
        }

        [Fact]
        public void ModifierDifference_01()
        {
            // access modifier on declaration but not implementation
            var source = """
                partial class C
                {
                    public partial int P { get; set; }
                }

                partial class C
                {
                    partial int P { get => throw null!; set { } }
                }
                """;

            // PROTOTYPE(partial-properties): diagnostic message should be generalized for properties as well.
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,17): error CS8799: Both partial method declarations must have identical accessibility modifiers.
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMethodAccessibilityDifference, "P").WithLocation(8, 17));
        }

        [Fact]
        public void ModifierDifference_02()
        {
            // access modifier on declaration but not implementation
            var source = """
                partial class C
                {
                    public partial int P { get; private set; }
                }

                partial class C
                {
                    public partial int P { get => throw null!; set { } }
                }
                """;

            var comp = CreateCompilation(source);

            // PROTOTYPE(partial-properties): missing diagnostic
            comp.VerifyEmitDiagnostics();
        }

        // PROTOTYPE(partial-properties): test more mismatching scenarios
        // PROTOTYPE(partial-properties): test indexers incl parameters with attributes
        // PROTOTYPE(partial-properties): test merging property attributes
    }
}
