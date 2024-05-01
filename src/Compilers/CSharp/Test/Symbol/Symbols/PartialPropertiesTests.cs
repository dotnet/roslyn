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
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class PartialPropertiesTests : CSharpTestBase
    {
        [Theory]
        [InlineData("partial int P { get; set; }")]
        [InlineData("partial int P { get; }")]
        [InlineData("partial int P { set; }")]
        [InlineData("partial int P { get; init; }")]
        [InlineData("partial int P { init; }")]
        public void MissingDeclaration_01(string definitionPart)
        {
            // definition without implementation
            var source = $$"""
                partial class C
                {
                    {{definitionPart}}
                }
                """;
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS9300: Partial property 'C.P' must have an implementation part.
                //     partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P").WithArguments("C.P").WithLocation(3, 17)
                );

            var cClass = comp.GetMember<NamedTypeSymbol>("C");
            var prop = cClass.GetMember<SourcePropertySymbol>("P");
            Assert.True(prop.IsPartialDefinition);
            Assert.Null(prop.PartialImplementationPart);

            var members = cClass.GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            switch (definitionPart)
            {
                case "partial int P { get; set; }":
                    AssertEx.Equal([
                        "System.Int32 C.P { get; set; }",
                        "System.Int32 C.P.get",
                        "void C.P.set",
                        "C..ctor()"
                        ], members);
                    break;
                case "partial int P { get; }":
                    AssertEx.Equal([
                        "System.Int32 C.P { get; }",
                        "System.Int32 C.P.get",
                        "C..ctor()"
                        ], members);
                    break;
                case "partial int P { set; }":
                    AssertEx.Equal([
                        "System.Int32 C.P { set; }",
                        "void C.P.set",
                        "C..ctor()"
                        ], members);
                    break;
                case "partial int P { get; init; }":
                    AssertEx.Equal([
                        "System.Int32 C.P { get; init; }",
                        "System.Int32 C.P.get",
                        "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.P.init",
                        "C..ctor()"
                        ], members);
                    break;
                case "partial int P { init; }":
                    AssertEx.Equal([
                        "System.Int32 C.P { init; }",
                        "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.P.init",
                        "C..ctor()"
                        ], members);
                    break;
                default:
                    throw ExceptionUtilities.Unreachable();
            }
        }

        [Theory]
        [InlineData("partial int P { get => throw null!; set { } }")]
        [InlineData("partial int P { get => throw null!; }")]
        [InlineData("partial int P { set { } }")]
        [InlineData("partial int P { get => throw null!; init { } }")]
        [InlineData("partial int P { init { } }")]
        public void MissingDeclaration_02(string implementationPart)
        {
            // implementation without definition
            var source = $$"""
                partial class C
                {
                    {{implementationPart}}
                }
                """;
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS9301: Partial property 'C.P' must have an definition part.
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "P").WithArguments("C.P").WithLocation(3, 17)
                );

            var cClass = comp.GetMember<NamedTypeSymbol>("C");
            var prop = cClass.GetMember<SourcePropertySymbol>("P");
            Assert.True(prop.IsPartialImplementation);
            Assert.Null(prop.PartialDefinitionPart);

            var members = cClass.GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            switch (implementationPart)
            {
                case "partial int P { get => throw null!; set { } }":
                    AssertEx.Equal([
                        "System.Int32 C.P { get; set; }",
                        "System.Int32 C.P.get",
                        "void C.P.set",
                        "C..ctor()"
                        ], members);
                    break;
                case "partial int P { get => throw null!; }":
                    AssertEx.Equal([
                        "System.Int32 C.P { get; }",
                        "System.Int32 C.P.get",
                        "C..ctor()"
                        ], members);
                    break;
                case "partial int P { set { } }":
                    AssertEx.Equal([
                        "System.Int32 C.P { set; }",
                        "void C.P.set",
                        "C..ctor()"
                        ], members);
                    break;
                case "partial int P { get => throw null!; init { } }":
                    AssertEx.Equal([
                        "System.Int32 C.P { get; init; }",
                        "System.Int32 C.P.get",
                        "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.P.init",
                        "C..ctor()"
                        ], members);
                    break;
                case "partial int P { init { } }":
                    AssertEx.Equal([
                        "System.Int32 C.P { init; }",
                        "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.P.init",
                        "C..ctor()"
                        ], members);
                    break;
                default:
                    throw ExceptionUtilities.Unreachable();
            }
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
        public void DuplicateDeclaration_06()
        {
            // partial method and partial property have the same name
            var source = """
                partial class C
                {
                    public partial int P { get; set; }
                    public partial int P() => 1;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C.P' must have an implementation part.
                //     public partial int P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P").WithArguments("C.P").WithLocation(3, 24),
                // (4,24): error CS0759: No defining declaration found for implementing declaration of partial method 'C.P()'
                //     public partial int P() => 1;
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "P").WithArguments("C.P()").WithLocation(4, 24),
                // (4,24): error CS0102: The type 'C' already contains a definition for 'P'
                //     public partial int P() => 1;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 24)
                );
        }

        [Fact]
        public void DuplicateDeclaration_07()
        {
            // partial method and partial property accessor have the same metadata name
            var source = """
                partial class C
                {
                    public partial int P { get; }
                    public partial int get_P() => 1;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C.P' must have an implementation part.
                //     public partial int P { get; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P").WithArguments("C.P").WithLocation(3, 24),
                // (3,28): error CS0082: Type 'C' already reserves a member called 'get_P' with the same parameter types
                //     public partial int P { get; }
                Diagnostic(ErrorCode.ERR_MemberReserved, "get").WithArguments("get_P", "C").WithLocation(3, 28)
                );
        }

        [Fact]
        public void DuplicateDeclaration_08()
        {
            // multiple implementing declarations where accessors are "split" across declarations
            var source = """
                partial class C
                {
                    public partial int P { get; set; }
                    public partial int P { get => 1; }
                    public partial int P { set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS9304: Property accessor 'C.P.set' must be implemented because it is declared on the definition part
                //     public partial int P { get => 1; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingAccessor, "P").WithArguments("C.P.set").WithLocation(4, 24),
                // (5,24): error CS9303: A partial property may not have multiple implementing declarations
                //     public partial int P { set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "P").WithLocation(5, 24),
                // (5,24): error CS0102: The type 'C' already contains a definition for 'P'
                //     public partial int P { set { } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(5, 24)
                );

            if (comp.GetMembers("C.P") is not [SourcePropertySymbol prop, SourcePropertySymbol duplicateProp])
                throw ExceptionUtilities.UnexpectedValue(comp.GetMembers("C.P"));

            Assert.True(prop.IsPartialDefinition);
            Assert.Equal("System.Int32 C.P { get; set; }", prop.ToTestDisplayString());
            Assert.Equal("System.Int32 C.P { get; }", prop.PartialImplementationPart.ToTestDisplayString());

            Assert.True(duplicateProp.IsPartialImplementation);
            Assert.Null(duplicateProp.PartialDefinitionPart);
            Assert.Equal("System.Int32 C.P { set; }", duplicateProp.ToTestDisplayString());
        }

        [Fact]
        public void DuplicateDeclaration_09()
        {
            // partial indexer and partial property Item
            var source = """
                partial class C
                {
                    public partial int this[int i] { get; }
                    public partial int Item => 1;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C.this[int]' must have an implementation part.
                //     public partial int this[int i] { get; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C.this[int]").WithLocation(3, 24),
                // (3,24): error CS0102: The type 'C' already contains a definition for 'Item'
                //     public partial int this[int i] { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("C", "Item").WithLocation(3, 24),
                // (4,24): error CS9301: Partial property 'C.Item' must have an definition part.
                //     public partial int Item => 1;
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "Item").WithArguments("C.Item").WithLocation(4, 24)
                );
        }

        [Fact]
        public void DuplicateDeclaration_10()
        {
            // partial parameterless (error) indexer and partial property Item
            var source = """
                partial class C
                {
                    public partial int this[] { get; }
                    public partial int Item => 1;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C.this' must have an implementation part.
                //     public partial int this[] { get; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C.this").WithLocation(3, 24),
                // (3,24): error CS0102: The type 'C' already contains a definition for 'Item'
                //     public partial int this[] { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("C", "Item").WithLocation(3, 24),
                // (3,29): error CS1551: Indexers must have at least one parameter
                //     public partial int this[] { get; }
                Diagnostic(ErrorCode.ERR_IndexerNeedsParam, "]").WithLocation(3, 29),
                // (4,24): error CS9301: Partial property 'C.Item' must have an definition part.
                //     public partial int Item => 1;
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "Item").WithArguments("C.Item").WithLocation(4, 24)
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
                    partial int P { set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS9304: Property accessor 'C.P.get' must be implemented because it is declared on the definition part
                //     partial int P { set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingAccessor, "P").WithArguments("C.P.get").WithLocation(4, 17)
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

        [Theory]
        [InlineData("get")]
        [InlineData("set")]
        [InlineData("init")]
        public void MissingAccessor_04(string accessorKind)
        {
            // duplicate property definitions, one with a single accessor, one empty
            var source = $$"""
                partial class C
                {
                    partial int P { {{accessorKind}}; }
                    partial int P { }
                }
                """;
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS9300: Partial property 'C.P' must have an implementation part.
                //     partial int P { {{accessorKind}}; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P").WithArguments("C.P").WithLocation(3, 17),
                // (4,17): error CS9302: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //     partial int P { }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P").WithLocation(4, 17),
                // (4,17): error CS0102: The type 'C' already contains a definition for 'P'
                //     partial int P { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 17),
                // (4,17): error CS0548: 'C.P': property or indexer must have at least one accessor
                //     partial int P { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P").WithArguments("C.P").WithLocation(4, 17)
                );
        }

        [Theory]
        [InlineData("get")]
        [InlineData("set")]
        [InlineData("init")]
        public void MissingAccessor_05(string accessorKind)
        {
            // implementation single accessor, definition empty
            var source = $$"""
                partial class C
                {
                    partial int P { {{accessorKind}} => throw null!; }
                    partial int P { }
                }
                """;
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS9305: Property accessor 'C.P.{accessorKind}' does not implement any accessor declared on the definition part
                //     partial int P { {{accessorKind}} => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialPropertyUnexpectedAccessor, "P").WithArguments($"C.P.{accessorKind}").WithLocation(3, 17),
                // (4,17): error CS0548: 'C.P': property or indexer must have at least one accessor
                //     partial int P { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P").WithArguments("C.P").WithLocation(4, 17)
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

            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (4,41): error CS9306: Property accessor 'C.P.init' must be 'set' to match the definition part       
                //     partial int P { get => throw null!; init { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyInitMismatch, "init").WithArguments("C.P.init", "set").WithLocation(4, 41)
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
            var comp = CreateCompilation([source, IsExternalInitTypeDefinition]);
            comp.VerifyEmitDiagnostics(
                // (4,41): error CS9306: Property accessor 'C.P.set' must be 'init' to match the definition part
                //     partial int P { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyInitMismatch, "set").WithArguments("C.P.set", "init").WithLocation(4, 41)
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

            var propDefinition = comp.GetMember<SourcePropertySymbol>("C.P");
            Assert.True(propDefinition.IsPartialDefinition);

            var propImplementation = propDefinition.PartialImplementationPart!;
            Assert.True(propImplementation.IsPartialImplementation);

            Assert.Same(propDefinition, propImplementation.PartialDefinitionPart);
            Assert.Null(propImplementation.PartialImplementationPart);
            Assert.Same(propImplementation, propDefinition.PartialImplementationPart);
            Assert.Null(propDefinition.PartialDefinitionPart);

            Assert.Same(propDefinition.GetMethod, comp.GetMember<MethodSymbol>("C.get_P"));
            Assert.Same(propDefinition.SetMethod, comp.GetMember<MethodSymbol>("C.set_P"));

            verifyAccessor(propDefinition.GetMethod!, propImplementation.GetMethod!);
            verifyAccessor(propDefinition.SetMethod!, propImplementation.SetMethod!);

            void verifyAccessor(MethodSymbol definitionAccessor, MethodSymbol implementationAccessor)
            {
                Assert.True(definitionAccessor.IsPartialDefinition());
                Assert.True(implementationAccessor.IsPartialImplementation());

                Assert.Same(implementationAccessor, definitionAccessor.PartialImplementationPart);
                Assert.Null(definitionAccessor.PartialDefinitionPart);
                Assert.Same(definitionAccessor, implementationAccessor.PartialDefinitionPart);
                Assert.Null(implementationAccessor.PartialImplementationPart);
            }
        }

        [Theory]
        [InlineData("public partial int P { get => _p; }")]
        [InlineData("public partial int P => _p;")]
        public void Semantics_02(string implementationPart)
        {
            // get-only
            var source = $$"""
                using System;

                var c = new C();
                Console.Write(c.P);

                partial class C
                {
                    public partial int P { get; }
                }

                partial class C
                {
                    private int _p = 1;
                    {{implementationPart}}
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

            var comp = (CSharpCompilation)verifier.Compilation;
            var cClass = comp.GetMember<NamedTypeSymbol>("C");
            var members = cClass.GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            AssertEx.Equal([
                "System.Int32 C.P { get; }",
                "System.Int32 C.P.get",
                "System.Int32 C._p",
                "C..ctor()"
                ], members);

            var propDefinition = comp.GetMember<SourcePropertySymbol>("C.P");
            Assert.True(propDefinition.IsPartialDefinition);

            var propImplementation = propDefinition.PartialImplementationPart!;
            Assert.True(propImplementation.IsPartialImplementation);

            Assert.Same(propDefinition, propImplementation.PartialDefinitionPart);
            Assert.Null(propImplementation.PartialImplementationPart);
            Assert.Same(propImplementation, propDefinition.PartialImplementationPart);
            Assert.Null(propDefinition.PartialDefinitionPart);

            Assert.Null(propDefinition.SetMethod);
            Assert.Null(propImplementation.SetMethod);

            var definitionAccessor = propDefinition.GetMethod!;
            var implementationAccessor = propImplementation.GetMethod!;
            Assert.True(definitionAccessor.IsPartialDefinition());
            Assert.True(implementationAccessor.IsPartialImplementation());

            Assert.Same(implementationAccessor, definitionAccessor.PartialImplementationPart);
            Assert.Null(definitionAccessor.PartialDefinitionPart);
            Assert.Same(definitionAccessor, implementationAccessor.PartialDefinitionPart);
            Assert.Null(implementationAccessor.PartialImplementationPart);
        }

        [Theory]
        [InlineData("set")]
        [InlineData("init")]
        public void Semantics_03(string accessorKind)
        {
            // set/init-only
            var source = $$"""
                using System;

                var c = new C() { P = 1 };

                partial class C
                {
                    public partial int P { {{accessorKind}}; }
                }

                partial class C
                {
                    public partial int P
                    {
                        {{accessorKind}}
                        {
                            Console.Write(value);
                        }
                    }
                }
                """;
            var verifier = CompileAndVerify([source, IsExternalInitTypeDefinition], expectedOutput: "1");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL($"C.P.{accessorKind}", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.1
                  IL_0001:  call       "void System.Console.Write(int)"
                  IL_0006:  ret
                }
                """);

            var comp = (CSharpCompilation)verifier.Compilation;
            var cClass = comp.GetMember<NamedTypeSymbol>("C");
            var members = cClass.GetMembers().SelectAsArray(m => m.ToTestDisplayString());

            if (accessorKind == "set")
            {
                AssertEx.Equal([
                    "System.Int32 C.P { set; }",
                    "void C.P.set",
                    "C..ctor()"
                    ], members);
            }
            else
            {
                AssertEx.Equal([
                    "System.Int32 C.P { init; }",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.P.init",
                    "C..ctor()"
                    ],
                    members);
            }

            var propDefinition = comp.GetMember<SourcePropertySymbol>("C.P");
            Assert.True(propDefinition.IsPartialDefinition);

            var propImplementation = propDefinition.PartialImplementationPart!;
            Assert.True(propImplementation.IsPartialImplementation);

            Assert.Same(propDefinition, propImplementation.PartialDefinitionPart);
            Assert.Null(propImplementation.PartialImplementationPart);
            Assert.Same(propImplementation, propDefinition.PartialImplementationPart);
            Assert.Null(propDefinition.PartialDefinitionPart);

            Assert.Null(propDefinition.GetMethod);
            Assert.Null(propImplementation.GetMethod);

            var definitionAccessor = propDefinition.SetMethod!;
            var implementationAccessor = propImplementation.SetMethod!;
            Assert.True(definitionAccessor.IsPartialDefinition());
            Assert.True(implementationAccessor.IsPartialImplementation());

            Assert.Same(implementationAccessor, definitionAccessor.PartialImplementationPart);
            Assert.Null(definitionAccessor.PartialDefinitionPart);
            Assert.Same(definitionAccessor, implementationAccessor.PartialDefinitionPart);
            Assert.Null(implementationAccessor.PartialImplementationPart);
        }

        [Theory]
        [InlineData("public partial int P { get => _p; set => _p = value; }")]
        [InlineData("public partial int P { set => _p = value; get => _p; }")]
        public void Semantics_04(string implementationPart)
        {
            // ordering difference between def and impl
            var source = $$"""
                using System;

                var c = new C() { P = 1 };
                Console.Write(c.P);

                partial class C
                {
                    public partial int P { get; set; }

                    private int _p;
                    {{implementationPart}}
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();
            var comp = (CSharpCompilation)verifier.Compilation;

            var members = comp.GetMember<NamedTypeSymbol>("C").GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            AssertEx.Equal([
                "System.Int32 C.P { get; set; }",
                "System.Int32 C.P.get",
                "void C.P.set",
                "System.Int32 C._p",
                "C..ctor()"
                ], members);

            var reference = comp.EmitToImageReference();
            var comp1 = CreateCompilation([], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), references: [reference]);
            var members1 = comp1.GetMember<NamedTypeSymbol>("C").GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            AssertEx.Equal([
                "System.Int32 C._p",
                "System.Int32 C.P.get",
                "void C.P.set",
                "C..ctor()",
                "System.Int32 C.P { get; set; }"
                ], members1);
        }

        [Theory]
        [InlineData("public partial int P { get => _p; set => _p = value; }")]
        [InlineData("public partial int P { set => _p = value; get => _p; }")]
        public void Semantics_05(string implementationPart)
        {
            // ordering difference between def and impl (def set before get)
            var source = $$"""
                using System;

                var c = new C() { P = 1 };
                Console.Write(c.P);

                partial class C
                {
                    public partial int P { set; get; }

                    private int _p;
                    {{implementationPart}}
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();
            var comp = (CSharpCompilation)verifier.Compilation;

            var members = comp.GetMember<NamedTypeSymbol>("C").GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            // set accessor appears before get accessor, otherwise member order and symbol display is the same as Semantics_04
            AssertEx.Equal([
                "System.Int32 C.P { get; set; }",
                "void C.P.set",
                "System.Int32 C.P.get",
                "System.Int32 C._p",
                "C..ctor()"
                ], members);

            var reference = comp.EmitToImageReference();
            var comp1 = CreateCompilation([], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), references: [reference]);
            var members1 = comp1.GetMember<NamedTypeSymbol>("C").GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            AssertEx.Equal([
                "System.Int32 C._p",
                "void C.P.set",
                "System.Int32 C.P.get",
                "C..ctor()",
                "System.Int32 C.P { get; set; }"
                ], members1);
        }

        [Fact]
        public void Semantics_RefKind()
        {
            var source = """
                using System;

                var c = new C();
                ref int i = ref c.P1;
                c.P1++;
                Console.Write(i);
                Console.Write(c.P2);
                
                partial class C
                {
                    public partial ref int P1 { get; }
                    public partial ref readonly int P2 { get; }

                    private int _p;
                    public partial ref int P1 => ref _p;
                    public partial ref readonly int P2 { get => ref _p; }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "11");
            verifier.VerifyDiagnostics();
            var comp = (CSharpCompilation)verifier.Compilation;

            var members = comp.GetMember<NamedTypeSymbol>("C").GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            AssertEx.Equal([
                "ref System.Int32 C.P1 { get; }",
                "ref System.Int32 C.P1.get",
                "ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Int32 C.P2 { get; }",
                "ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Int32 C.P2.get",
                "System.Int32 C._p",
                "C..ctor()"
                ], members);
        }

        [Fact]
        public void Semantics_Static()
        {
            var source = """
                using System;

                C.P++;
                Console.Write(C.P);
                
                partial class C
                {
                    public static partial int P { get; set; }

                    public static partial int P { get => _p; set => _p = value; }
                    private static int _p;
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();
            var comp = (CSharpCompilation)verifier.Compilation;

            var members = comp.GetMember<NamedTypeSymbol>("C").GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            AssertEx.Equal([
                "System.Int32 C.P { get; set; }",
                "System.Int32 C.P.get",
                "void C.P.set",
                "System.Int32 C._p",
                "C..ctor()"
                ], members);
        }

        [Fact]
        public void Semantics_Unsafe()
        {
            var source = """
                using System;

                class Program
                {
                    static unsafe void Main()
                    {
                        int i = 1;
                        S s = new S() { P = &i };
                        Console.Write(*s.P);
                    }
                }

                partial struct S
                {
                    public unsafe partial int* P { get; set; }

                    public unsafe partial int* P { get => _p; set => _p = value; }
                    private unsafe int* _p;
                }
                """;

            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails, expectedOutput: "1");
            verifier.VerifyDiagnostics();
            var comp = (CSharpCompilation)verifier.Compilation;

            var members = comp.GetMember<NamedTypeSymbol>("S").GetMembers().SelectAsArray(m => m.ToTestDisplayString());
            AssertEx.Equal([
                "System.Int32* S.P { get; set; }",
                "System.Int32* S.P.get",
                "void S.P.set",
                "System.Int32* S._p",
                "S..ctor()"
                ], members);
        }


        [Fact]
        public void ModifierDifference_Accessibility_Property()
        {
            var source = """
                partial class C
                {
                    public partial int P1 { get; set; }
                    partial int P2 { get; set; }
                }

                partial class C
                {
                    partial int P1 { get => throw null!; set { } }
                    protected partial int P2 { get => throw null!; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     partial int P1 { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "P1").WithLocation(9, 17),
                // (10,27): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     protected partial int P2 { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "P2").WithLocation(10, 27));
        }

        // Property accessor modifiers can only include:
        // - accessibility modifiers
        // - readonly modifier
        // The test burden for accessors is further reduced by the fact that explicit accessibility
        // is only permitted when it is more restrictive than the containing property.

        [Fact]
        public void ModifierDifference_Accessibility_Accessors()
        {
            // access modifier mismatch on accessors
            var source = """
                partial class C
                {
                    public partial int P { get; private set; }
                }

                partial class C
                {
                    public partial int P { internal get => throw null!; set { } }
                }
                """;

            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (8,37): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     public partial int P { internal get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "get").WithLocation(8, 37),
                // (8,57): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     public partial int P { internal get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "set").WithLocation(8, 57));
        }

        [Fact]
        public void ModifierDifference_Readonly_Property()
        {
            // readonly modifier mismatch on accessors
            var source = """
                partial struct S
                {
                    readonly partial int P1 { get; set; }
                    partial int P2 { get; set; }
                    readonly partial int P3 { get; set; }
                }

                partial struct S
                {
                    partial int P1 { get => throw null!; set { } }
                    readonly partial int P2 { get => throw null!; set { } }
                    readonly partial int P3 { get => throw null!; set { } }
                }
                """;

            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (10,17): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                //     partial int P1 { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "P1").WithLocation(10, 17),
                // (11,26): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                //     readonly partial int P2 { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "P2").WithLocation(11, 26));
        }

        [Fact]
        public void ModifierDifference_Readonly_Accessors()
        {
            // readonly modifier mismatch on accessors
            var source = """
                partial struct S
                {
                    public partial int P { readonly get; set; }
                }

                partial struct S
                {
                    public partial int P { get => throw null!; readonly set { } }
                }
                """;

            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (8,28): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                //     public partial int P { get => throw null!; readonly set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "get").WithLocation(8, 28),
                // (8,57): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                //     public partial int P { get => throw null!; readonly set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "set").WithLocation(8, 57));
        }

        [Fact]
        public void ModifierDifference_Accessibility_ExplicitDefault()
        {
            // only one part explicitly specifies the default accessibility
            var source = """
                partial class C
                {
                    private partial int P1 { get; set; }
                    partial int P2 { get; set; }
                }

                partial class C
                {
                    partial int P1 { get => throw null!; set { } }
                    private partial int P2 { get => throw null!; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     partial int P1 { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "P1").WithLocation(9, 17),
                // (10,25): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     private partial int P2 { get => throw null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "P2").WithLocation(10, 25));
        }

        [Fact]
        public void TypeDifference_01()
        {
            var source = """
                partial class C
                {
                    partial int P { get; set; }
                    partial string P { get => ""; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,20): error CS9307: Both partial property declarations must have the same type.
                //     partial string P { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyTypeDifference, "P").WithLocation(4, 20));
        }

        [Fact]
        public void TypeDifference_02()
        {
            // TODO2: nullable difference warning
            var source = """
                #nullable enable
                partial class C
                {
                    partial string? P1 { get; set; }
                    partial string P1 { get => ""; set { } }

                    partial string P2 { get; set; }
                    partial string? P2 { get => ""; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void TypeDifference_03()
        {
            // tuple element name difference
            // this is an error for consistency with methods
            var source = """
                partial class C
                {
                    partial (int a, int b) P1 { get; set; }
                    partial (int a, int x) P1 { get => default; set { } }

                    partial (int a, int b) P2 { get; set; }
                    partial (int x, int y) P2 { get => default; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,28): error CS8142: Both partial member declarations, 'C.P1' and 'C.P1', must use the same tuple element names.
                //     partial (int a, int x) P1 { get => default; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "P1").WithArguments("C.P1", "C.P1").WithLocation(4, 28),
                // (7,28): error CS8142: Both partial member declarations, 'C.P2' and 'C.P2', must use the same tuple element names.
                //     partial (int x, int y) P2 { get => default; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "P2").WithArguments("C.P2", "C.P2").WithLocation(7, 28));
        }

        [Fact]
        public void RefKindDifference_01()
        {
            var source = """
                partial class C
                {
                    partial int P1 { get; }
                    partial ref int P1 { get => throw null!; }

                    partial int P2 { get; }
                    partial ref readonly int P2 { get => throw null!; }

                    partial ref int P3 { get; }
                    partial int P3 { get => throw null!; }

                    partial ref readonly int P4 { get; }
                    partial int P4 { get => throw null!; }

                    partial ref readonly int P5 { get; }
                    partial ref int P5 { get => throw null!; }

                    partial ref int P6 { get; }
                    partial ref readonly int P6 { get => throw null!; }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,21): error CS8818: Partial member declarations must have matching ref return values.
                //     partial ref int P1 { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "P1").WithLocation(4, 21),
                // (7,30): error CS8818: Partial member declarations must have matching ref return values.
                //     partial ref readonly int P2 { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "P2").WithLocation(7, 30),
                // (10,17): error CS8818: Partial member declarations must have matching ref return values.
                //     partial int P3 { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "P3").WithLocation(10, 17),
                // (13,17): error CS8818: Partial member declarations must have matching ref return values.
                //     partial int P4 { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "P4").WithLocation(13, 17),
                // (16,21): error CS8818: Partial member declarations must have matching ref return values.
                //     partial ref int P5 { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "P5").WithLocation(16, 21),
                // (19,30): error CS8818: Partial member declarations must have matching ref return values.
                //     partial ref readonly int P6 { get => throw null!; }
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "P6").WithLocation(19, 30));
        }

        [Fact]
        public void StaticDifference()
        {
            var source = """
                partial class C
                {
                    public static partial int P1 { get; set; }
                    public partial int P1 { get => 1; set { } }

                    public partial int P2 { get; set; }
                    public static partial int P2 { get => 1; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS0763: Both partial member declarations must be static or neither may be static
                //     public partial int P1 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberStaticDifference, "P1").WithLocation(4, 24),
                // (7,31): error CS0763: Both partial member declarations must be static or neither may be static
                //     public static partial int P2 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberStaticDifference, "P2").WithLocation(7, 31));
        }

        [Fact]
        public void UnsafeDifference_01()
        {
            // Note that the diagnostic is on difference in unsafe modifiers on the property declarations, not on a difference in unsafe context.
            var source = """
                partial class C
                {
                    public partial int P1 { get; set; }
                    public unsafe partial int P1 { get => 1; set { } }

                    public unsafe partial int P2 { get; set; }
                    public partial int P2 { get => 1; set { } }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,31): error CS0764: Both partial method declarations must be unsafe or neither may be unsafe
                //     public unsafe partial int P1 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMethodUnsafeDifference, "P1").WithLocation(4, 31),
                // (7,24): error CS0764: Both partial method declarations must be unsafe or neither may be unsafe
                //     public partial int P2 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMethodUnsafeDifference, "P2").WithLocation(7, 24));
        }

        [Fact]
        public void UnsafeDifference_02()
        {
            var source = """
                unsafe partial class C
                {
                    public partial int* P1 { get; set; }
                }

                partial class C
                {
                    public partial int* P1 { get => null; set { } }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     public partial int* P1 { get => null; set { } }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(8, 20));
        }

        // PROTOTYPE(partial-properties): virtual, sealed, override, new modifier differences

        // PROTOTYPE(partial-properties): override partial property where base has modopt
        // PROTOTYPE(partial-properties): unsafe context differences between partial property declarations
        // PROTOTYPE(partial-properties): test indexers incl parameters with attributes
        // PROTOTYPE(partial-properties): test merging property attributes
    }
}
