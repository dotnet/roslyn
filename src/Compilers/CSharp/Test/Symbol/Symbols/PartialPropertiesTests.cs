﻿// Licensed to the .NET Foundation under one or more agreements.
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
                // (3,17): error CS9301: Partial property 'C.P' must have a definition part.
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
                // (3,17): error CS9301: Partial property 'C.P' must have a definition part.
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
                // (4,24): error CS9301: Partial property 'C.Item' must have a definition part.
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
                // (4,24): error CS9301: Partial property 'C.Item' must have a definition part.
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
                // (3,24): error CS9301: Partial property 'C.P' must have a definition part.
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
        public void Semantics_Readonly_01()
        {
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        M(new S());
                    }

                    static void M(in S s)
                    {
                        Console.Write(s.P1);
                        Console.Write(s.P2);
                        // We can't exercise S.P2.set here because non-readonly setters will error instead of implicitly copying the receiver.
                        s.P3 = 3;
                        Console.Write(s.P3);
                        Console.Write(s.P4);
                    }
                }

                partial struct S
                {
                    public readonly partial int P1 { get; }
                    public readonly partial int P1 { get => 1; }

                    public partial int P2 { readonly get; set; }
                    public partial int P2 { readonly get => 2; set { } }

                    public partial int P3 { get; readonly set; }
                    public partial int P3 { get => 3; readonly set { } }

                    public partial int P4 { get; }
                    public partial int P4 { get => 4; }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "1234");
            verifier.VerifyDiagnostics();

            // non-readonly accessors need to copy the value to temp before invoking.
            verifier.VerifyIL("Program.M", """
                {
                  // Code size       68 (0x44)
                  .maxstack  2
                  .locals init (S V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  call       "readonly int S.P1.get"
                  IL_0006:  call       "void System.Console.Write(int)"
                  IL_000b:  ldarg.0
                  IL_000c:  call       "readonly int S.P2.get"
                  IL_0011:  call       "void System.Console.Write(int)"
                  IL_0016:  ldarg.0
                  IL_0017:  ldc.i4.3
                  IL_0018:  call       "readonly void S.P3.set"
                  IL_001d:  ldarg.0
                  IL_001e:  ldobj      "S"
                  IL_0023:  stloc.0
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  call       "int S.P3.get"
                  IL_002b:  call       "void System.Console.Write(int)"
                  IL_0030:  ldarg.0
                  IL_0031:  ldobj      "S"
                  IL_0036:  stloc.0
                  IL_0037:  ldloca.s   V_0
                  IL_0039:  call       "int S.P4.get"
                  IL_003e:  call       "void System.Console.Write(int)"
                  IL_0043:  ret
                }
                """);

            var comp = (CSharpCompilation)verifier.Compilation;
        }

        [Fact]
        public void ModifierDifference_Readonly_Property()
        {
            // readonly modifier mismatch on property
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
        public void Accessibility_ExplicitDefault()
        {
            // properties can explicitly specify the default accessibility
            var source = """
                using System;

                partial class C
                {
                    private partial int P1 { get; }

                    static void Main()
                    {
                        Console.Write(new C().P1);
                    }
                }

                partial class C
                {
                    private partial int P1 { get => 1; }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Accessibility_ExplicitDefault_IsRespected()
        {
            var source = """
                using System;

                class Program
                {
                    static void Main()
                    {
                        Console.Write(new C().P1); // 1
                    }

                }

                partial class C
                {
                    private partial int P1 { get; }
                }

                partial class C
                {
                    private partial int P1 { get => 1; }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,31): error CS0122: 'C.P1' is inaccessible due to its protection level
                //         Console.Write(new C().P1); // 1
                Diagnostic(ErrorCode.ERR_BadAccess, "P1").WithArguments("C.P1").WithLocation(7, 31));

            var p1Def = comp.GetMember<SourcePropertySymbol>("C.P1");
            Assert.True(p1Def.IsPartialDefinition);
            Assert.Equal(Accessibility.Private, p1Def.DeclaredAccessibility);

            var p1DefPublic = p1Def.GetPublicSymbol();
            Assert.Equal(Accessibility.Private, p1DefPublic.DeclaredAccessibility);
        }

        [Fact]
        public void ModifierDifference_Accessibility_ExplicitDefault_01()
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
        public void ModifierDifference_Accessibility_ExplicitDefault_02()
        {
            var source = """
                using System;

                partial class C
                {
                    private partial int P1 { get; set; }
                    partial int P2 { private get; private set; }
                }

                partial class C
                {
                    partial int P1 { private get => 1; private set; }
                    partial int P2 { private get => 1; private set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'C.P2'
                //     partial int P2 { private get; private set; }
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "P2").WithArguments("C.P2").WithLocation(6, 17),
                // (6,30): error CS0273: The accessibility modifier of the 'C.P2.get' accessor must be more restrictive than the property or indexer 'C.P2'       
                //     partial int P2 { private get; private set; }
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "get").WithArguments("C.P2.get", "C.P2").WithLocation(6, 30),
                // (6,43): error CS0273: The accessibility modifier of the 'C.P2.set' accessor must be more restrictive than the property or indexer 'C.P2'       
                //     partial int P2 { private get; private set; }
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.P2.set", "C.P2").WithLocation(6, 43),
                // (11,17): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     partial int P1 { private get => 1; private set; }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "P1").WithLocation(11, 17),
                // (11,30): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     partial int P1 { private get => 1; private set; }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "get").WithLocation(11, 30),
                // (11,48): error CS8799: Both partial member declarations must have identical accessibility modifiers.
                //     partial int P1 { private get => 1; private set; }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "set").WithLocation(11, 48));
        }

        [Fact]
        public void TypeDifference_01()
        {
            var source = """
                using System.Collections.Generic;

                partial class C
                {
                    partial int P1 { get; set; }
                    partial string P1 { get => ""; set { } }

                    partial List<int> P2 { get; set; }
                    partial List<string> P2 { get => []; set { } }

                    partial IEnumerable<object> P3 { get; set; }
                    partial IEnumerable<string> P3 { get => []; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,20): error CS9307: Both partial property declarations must have the same type.
                //     partial string P1 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyTypeDifference, "P1").WithLocation(6, 20),
                // (9,26): error CS9307: Both partial property declarations must have the same type.
                //     partial List<string> P2 { get => []; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyTypeDifference, "P2").WithLocation(9, 26),
                // (12,33): error CS9307: Both partial property declarations must have the same type.
                //     partial IEnumerable<string> P3 { get => []; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyTypeDifference, "P3").WithLocation(12, 33));
        }

        [Fact]
        public void TypeDifference_02()
        {
            var source = """
                #nullable enable
                partial class C
                {
                    partial string? P1 { get; set; }
                    partial string P1 { get => ""; set { } }

                    partial string P2 { get; set; }
                    partial string? P2 { get => ""; set { } }

                    partial string?[] P3 { get; set; }
                    partial string[] P3 { get => []; set { } }

                    partial string[] P4 { get; set; }
                    partial string?[] P4 { get => []; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9308: Partial property declarations 'string? C.P1' and 'string C.P1' have signature differences.
                //     partial string P1 { get => ""; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "P1").WithArguments("string? C.P1", "string C.P1").WithLocation(5, 20),
                // (8,21): error CS9308: Partial property declarations 'string C.P2' and 'string? C.P2' have signature differences.
                //     partial string? P2 { get => ""; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "P2").WithArguments("string C.P2", "string? C.P2").WithLocation(8, 21),
                // (11,22): error CS9308: Partial property declarations 'string?[] C.P3' and 'string[] C.P3' have signature differences.
                //     partial string[] P3 { get => []; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "P3").WithArguments("string?[] C.P3", "string[] C.P3").WithLocation(11, 22),
                // (14,23): error CS9308: Partial property declarations 'string[] C.P4' and 'string?[] C.P4' have signature differences.
                //     partial string?[] P4 { get => []; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "P4").WithArguments("string[] C.P4", "string?[] C.P4").WithLocation(14, 23));
        }

        [Fact]
        public void NullableDifference_NullableWarningsDisabled()
        {
            // 'safe' nullable differences for partial methods are still reported even when nullable warnings are disabled.
            // For simplicity we replicate this behavior for partial properties.
            var source = """
                partial class C
                {
                    public partial string? P1 { get; set; }
                    public partial string P1 { get => ""; set { } }

                    public partial string P2 { get; set; }
                    public partial string? P2 { get => ""; set { } }

                    public partial string? M1();
                    public partial string M1() => "";

                    public partial string M2();
                    public partial string? M2() => "";
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithNullableContextOptions(NullableContextOptions.Enable));
            comp.VerifyEmitDiagnostics(
                // (4,27): warning CS9308: Partial property declarations 'string? C.P1' and 'string C.P1' have signature differences.
                //     public partial string P1 { get => ""; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "P1").WithArguments("string? C.P1", "string C.P1").WithLocation(4, 27),
                // (7,28): warning CS9308: Partial property declarations 'string C.P2' and 'string? C.P2' have signature differences.
                //     public partial string? P2 { get => ""; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "P2").WithArguments("string C.P2", "string? C.P2").WithLocation(7, 28),
                // (10,27): warning CS8826: Partial method declarations 'string? C.M1()' and 'string C.M1()' have signature differences.
                //     public partial string M1() => "";
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("string? C.M1()", "string C.M1()").WithLocation(10, 27),
                // (13,28): warning CS8819: Nullability of reference types in return type doesn't match partial definition.
                //     public partial string? M2() => "";
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "M2").WithLocation(13, 28));

            comp = CreateCompilation(source, options: TestOptions.DebugDll.WithNullableContextOptions(NullableContextOptions.Annotations));
            comp.VerifyEmitDiagnostics(
                // (4,27): warning CS9308: Partial property declarations 'string? C.P1' and 'string C.P1' have signature differences.
                //     public partial string P1 { get => ""; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "P1").WithArguments("string? C.P1", "string C.P1").WithLocation(4, 27),
                // (7,28): warning CS9308: Partial property declarations 'string C.P2' and 'string? C.P2' have signature differences.
                //     public partial string? P2 { get => ""; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "P2").WithArguments("string C.P2", "string? C.P2").WithLocation(7, 28),
                // (10,27): warning CS8826: Partial method declarations 'string? C.M1()' and 'string C.M1()' have signature differences.
                //     public partial string M1() => "";
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("string? C.M1()", "string C.M1()").WithLocation(10, 27));
        }

        [Fact]
        public void NullableDifference_Oblivious()
        {
            var source = """
                #nullable enable
                partial class C<T>
                {
                    public partial T P1 { get; set; }
                    public partial T? P2 { get; set; }
                #nullable disable
                    public partial T P3 { get; set; }
                }

                #nullable disable
                partial class C<T>
                {
                    public partial T P1 { get => default!; set { } }
                    public partial T P2 { get => default!; set { } }
                #nullable enable
                    public partial T P3 { get => default!; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            var p1 = comp.GetMember<SourcePropertySymbol>("C.P1");
            Assert.True(p1.IsPartialDefinition);
            Assert.Equal(NullableAnnotation.NotAnnotated, p1.TypeWithAnnotations.NullableAnnotation);

            var p2 = comp.GetMember<SourcePropertySymbol>("C.P2");
            Assert.True(p2.IsPartialDefinition);
            Assert.Equal(NullableAnnotation.Annotated, p2.TypeWithAnnotations.NullableAnnotation);

            var p3 = comp.GetMember<SourcePropertySymbol>("C.P3");
            Assert.True(p3.IsPartialDefinition);
            Assert.Equal(NullableAnnotation.Oblivious, p3.TypeWithAnnotations.NullableAnnotation);
        }

        [Fact]
        public void TypeDifference_03()
        {
            // tuple element name difference
            // this is an error for consistency with methods
            var source = """
                using System.Collections.Generic;

                partial class C
                {
                    partial (int a, int b) P1 { get; set; }
                    partial (int a, int x) P1 { get => default; set { } }

                    partial (int a, int b) P2 { get; set; }
                    partial (int x, int y) P2 { get => default; set { } }

                    partial List<(int a, int b)> P3 { get; set; }
                    partial List<(int x, int y)> P3 { get => null!; set { } }

                    partial List<(int, int)> P4 { get; set; }
                    partial List<(int x, int y)> P4 { get => null!; set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,28): error CS8142: Both partial member declarations, 'C.P1' and 'C.P1', must use the same tuple element names.
                //     partial (int a, int x) P1 { get => default; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "P1").WithArguments("C.P1", "C.P1").WithLocation(6, 28),
                // (9,28): error CS8142: Both partial member declarations, 'C.P2' and 'C.P2', must use the same tuple element names.
                //     partial (int x, int y) P2 { get => default; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "P2").WithArguments("C.P2", "C.P2").WithLocation(9, 28),
                // (12,34): error CS8142: Both partial member declarations, 'C.P3' and 'C.P3', must use the same tuple element names.
                //     partial List<(int x, int y)> P3 { get => null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "P3").WithArguments("C.P3", "C.P3").WithLocation(12, 34),
                // (15,34): error CS8142: Both partial member declarations, 'C.P4' and 'C.P4', must use the same tuple element names.
                //     partial List<(int x, int y)> P4 { get => null!; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "P4").WithArguments("C.P4", "C.P4").WithLocation(15, 34));
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
        public void AllTypeDifferences()
        {
            // Verify which diagnostics are reported when multiple kinds of type differences are present
            var source = """
                #nullable enable

                partial class C
                {
                    public partial ref (int x, string? y) Prop { get; }
                    public partial ref readonly (long x, string y) Prop => throw null!;
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,52): error CS9307: Both partial property declarations must have the same type.
                //     public partial ref readonly (long x, string y) Prop => throw null!;
                Diagnostic(ErrorCode.ERR_PartialPropertyTypeDifference, "Prop").WithLocation(6, 52),
                // (6,52): error CS8818: Partial member declarations must have matching ref return values.
                //     public partial ref readonly (long x, string y) Prop => throw null!;
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "Prop").WithLocation(6, 52));
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
        public void UnsafeDifference_01()
        {
            // 'unsafe' modifiers are required to match across property declarations.
            // Therefore an error is reported on implementation of 'P2' even though both parts are "effectively unsafe".
            var source = """
                partial class C
                {
                    public partial int P1 { get; set; }
                    public unsafe partial int P2 { get; set; }
                }

                unsafe partial class C
                {
                    public unsafe partial int P1 { get => 1; set { } }
                    public partial int P2 { get => 1; set { } }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (9,31): error CS0764: Both partial member declarations must be unsafe or neither may be unsafe
                //     public unsafe partial int P1 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberUnsafeDifference, "P1").WithLocation(9, 31),
                // (10,24): error CS0764: Both partial member declarations must be unsafe or neither may be unsafe
                //     public partial int P2 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberUnsafeDifference, "P2").WithLocation(10, 24));

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,15): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // partial class C
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "C").WithLocation(1, 15),
                // (4,31): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public unsafe partial int P2 { get; set; }
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "P2").WithLocation(4, 31),
                // (9,31): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public unsafe partial int P1 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "P1").WithLocation(9, 31));
        }

        [Fact]
        public void UnsafeDifference_02()
        {
            // A difference in unsafe context only matters when unsafe types are used in the signature
            var source = """
                unsafe partial class C
                {
                    public partial int* P1 { get; set; }
                    public partial int P2 { get; set; }
                }

                partial class C
                {
                    public partial int* P1 { get => null; set { } }
                    public partial int P2 { get => 1; set { } }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (9,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     public partial int* P1 { get => null; set { } }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(9, 20));
        }

        [Fact]
        public void Semantics_ExtendedModifier()
        {
            var source = """
                using System;

                C c = new C();
                Console.Write(c.P);

                c = new D1();
                Console.Write(c.P);

                c = new D2();
                Console.Write(c.P);

                c = new D3();
                Console.Write(c.P);
                Console.Write(new D3().P);

                partial class C
                {
                    public virtual partial int P { get; }
                    public virtual partial int P => 0;
                }

                partial class D1 : C
                {
                    public override partial int P { get; }
                    public override partial int P => 1;
                }

                partial class D2 : C
                {
                    public sealed override partial int P { get; }
                    public sealed override partial int P => 2;
                }

                partial class D3 : C
                {
                    public new partial int P { get; }
                    public new partial int P => 3;
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "01203");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ExtendedDifference_01()
        {
            var source = """
                partial class C
                {
                    public virtual partial int P1 { get; }
                    public partial int P1 => 1; // 1

                    public partial int P2 { get; }
                    public virtual partial int P2 => 1; // 2
                }

                partial class D1 : C
                {
                    public partial int P1 { get; } // 3
                    public override partial int P1 => 1; // 4

                    public override partial int P2 { get; } // 5
                    public partial int P2 => 1; // 6
                }

                partial class D2 : C
                {
                    public partial int P1 { get; } // 7 
                    public sealed override partial int P1 => 1; // 8

                    public sealed override partial int P2 { get; } // 9
                    public partial int P2 => 1; // 10
                }

                partial class D3 : C
                {
                    public sealed partial int P1 { get; } // 11, 12
                    public override partial int P1 => 1; // 13

                    public override partial int P2 { get; } // 14
                    public sealed partial int P2 => 1; // 15
                }

                partial class D4 : C
                {
                    public partial int P1 { get; } // 16
                    public new partial int P1 => 1; // 17

                    public new partial int P2 { get; }
                    public partial int P2 => 1; // 18
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public partial int P1 => 1; // 1
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P1").WithLocation(4, 24),
                // (7,32): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public virtual partial int P2 => 1; // 2
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P2").WithLocation(7, 32),
                // (12,24): warning CS0114: 'D1.P1' hides inherited member 'C.P1'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public partial int P1 { get; } // 3
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "P1").WithArguments("D1.P1", "C.P1").WithLocation(12, 24),
                // (13,33): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public override partial int P1 => 1; // 4
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P1").WithLocation(13, 33),
                // (15,33): error CS0506: 'D1.P2': cannot override inherited member 'C.P2' because it is not marked virtual, abstract, or override
                //     public override partial int P2 { get; } // 5
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "P2").WithArguments("D1.P2", "C.P2").WithLocation(15, 33),
                // (16,24): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public partial int P2 => 1; // 6
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P2").WithLocation(16, 24),
                // (21,24): warning CS0114: 'D2.P1' hides inherited member 'C.P1'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public partial int P1 { get; } // 7
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "P1").WithArguments("D2.P1", "C.P1").WithLocation(21, 24),
                // (22,40): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public sealed override partial int P1 => 1; // 8
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P1").WithLocation(22, 40),
                // (24,40): error CS0506: 'D2.P2': cannot override inherited member 'C.P2' because it is not marked virtual, abstract, or override
                //     public sealed override partial int P2 { get; } // 9
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "P2").WithArguments("D2.P2", "C.P2").WithLocation(24, 40),
                // (25,24): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public partial int P2 => 1; // 10
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P2").WithLocation(25, 24),
                // (30,31): warning CS0114: 'D3.P1' hides inherited member 'C.P1'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public sealed partial int P1 { get; } // 11, 12
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "P1").WithArguments("D3.P1", "C.P1").WithLocation(30, 31),
                // (30,31): error CS0238: 'D3.P1' cannot be sealed because it is not an override
                //     public sealed partial int P1 { get; } // 11, 12
                Diagnostic(ErrorCode.ERR_SealedNonOverride, "P1").WithArguments("D3.P1").WithLocation(30, 31),
                // (31,33): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public override partial int P1 => 1; // 13
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P1").WithLocation(31, 33),
                // (33,33): error CS0506: 'D3.P2': cannot override inherited member 'C.P2' because it is not marked virtual, abstract, or override
                //     public override partial int P2 { get; } // 14
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "P2").WithArguments("D3.P2", "C.P2").WithLocation(33, 33),
                // (34,31): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public sealed partial int P2 => 1; // 15
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P2").WithLocation(34, 31),
                // (39,24): warning CS0114: 'D4.P1' hides inherited member 'C.P1'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public partial int P1 { get; } // 16
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "P1").WithArguments("D4.P1", "C.P1").WithLocation(39, 24),
                // (40,28): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public new partial int P1 => 1; // 17
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P1").WithLocation(40, 28),
                // (43,24): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
                //     public partial int P2 => 1; // 18
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "P2").WithLocation(43, 24));
        }

        [Fact]
        public void Abstract()
        {
            // 'abstract' is not permitted on partial declarations
            var source = """
                abstract partial class C
                {
                    public abstract partial int P1 { get; set; }

                    public abstract partial int P2 { get => ""; set { } }

                    public abstract partial int P3 { get; set; }
                    public abstract partial int P3 { get => ""; set { } }

                    public abstract partial int P4 { get; set; }
                    public partial int P4 { get => ""; set { } }

                    public partial int P5 { get; set; }
                    public abstract partial int P5 { get => ""; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,33): error CS9300: Partial property 'C.P1' must have an implementation part.
                //     public abstract partial int P1 { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P1").WithArguments("C.P1").WithLocation(3, 33),
                // (3,33): error CS0750: A partial member cannot have the 'abstract' modifier
                //     public abstract partial int P1 { get; set; }
                Diagnostic(ErrorCode.ERR_PartialMemberCannotBeAbstract, "P1").WithLocation(3, 33),
                // (5,33): error CS9301: Partial property 'C.P2' must have a definition part.
                //     public abstract partial int P2 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "P2").WithArguments("C.P2").WithLocation(5, 33),
                // (5,33): error CS0750: A partial member cannot have the 'abstract' modifier
                //     public abstract partial int P2 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberCannotBeAbstract, "P2").WithLocation(5, 33),
                // (5,38): error CS0500: 'C.P2.get' cannot declare a body because it is marked abstract
                //     public abstract partial int P2 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("C.P2.get").WithLocation(5, 38),
                // (5,49): error CS0500: 'C.P2.set' cannot declare a body because it is marked abstract
                //     public abstract partial int P2 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("C.P2.set").WithLocation(5, 49),
                // (7,33): error CS0750: A partial member cannot have the 'abstract' modifier
                //     public abstract partial int P3 { get; set; }
                Diagnostic(ErrorCode.ERR_PartialMemberCannotBeAbstract, "P3").WithLocation(7, 33),
                // (8,38): error CS0500: 'C.P3.get' cannot declare a body because it is marked abstract
                //     public abstract partial int P3 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("C.P3.get").WithLocation(8, 38),
                // (8,49): error CS0500: 'C.P3.set' cannot declare a body because it is marked abstract
                //     public abstract partial int P3 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("C.P3.set").WithLocation(8, 49),
                // (10,33): error CS0750: A partial member cannot have the 'abstract' modifier
                //     public abstract partial int P4 { get; set; }
                Diagnostic(ErrorCode.ERR_PartialMemberCannotBeAbstract, "P4").WithLocation(10, 33),
                // (11,36): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //     public partial int P4 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(11, 36),
                // (14,38): error CS0500: 'C.P5.get' cannot declare a body because it is marked abstract
                //     public abstract partial int P5 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "get").WithArguments("C.P5.get").WithLocation(14, 38),
                // (14,49): error CS0500: 'C.P5.set' cannot declare a body because it is marked abstract
                //     public abstract partial int P5 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_AbstractHasBody, "set").WithArguments("C.P5.set").WithLocation(14, 49));
        }

        [Fact]
        public void Semantics_Required()
        {
            var source = """
                using System;

                partial class C
                {
                    public required partial string P1 { get; set; }
                    public required partial string P1 { get => ""; set { Console.Write(value); } }

                    static void Main()
                    {
                        _ = new C() { P1 = "A" };
                    }
                }
                """;

            var verifier = CompileAndVerify([source, RequiredMemberAttribute, SetsRequiredMembersAttribute, CompilerFeatureRequiredAttribute], expectedOutput: "A");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.Main", """
                {
                  // Code size       16 (0x10)
                  .maxstack  2
                  IL_0000:  newobj     "C..ctor()"
                  IL_0005:  ldstr      "A"
                  IL_000a:  callvirt   "void C.P1.set"
                  IL_000f:  ret
                }
                """);
        }

        [Fact]
        public void Required_CreationSiteError()
        {
            var source = """
                partial class C
                {
                    public required partial string P1 { get; set; }
                    public required partial string P1 { get => ""; set { } }

                    static void Main()
                    {
                        _ = new C();
                    }
                }
                """;

            var comp = CreateCompilation([source, RequiredMemberAttribute, SetsRequiredMembersAttribute, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics(
                // (8,17): error CS9035: Required member 'C.P1' must be set in the object initializer or attribute constructor.
                //         _ = new C();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.P1").WithLocation(8, 17));
        }

        [Fact]
        public void Required_Difference()
        {
            var source = """
                partial class C
                {
                    public required partial string P1 { get; set; }
                    public partial string P1 { get => ""; set { } }

                    public partial string P2 { get; set; }
                    public required partial string P2 { get => ""; set { } }

                    static void Main()
                    {
                        _ = new C();
                    }
                }
                """;

            var comp = CreateCompilation([source, RequiredMemberAttribute, SetsRequiredMembersAttribute, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics(
                // (4,27): error CS9309: Both partial property declarations must be required or neither may be required
                //     public partial string P1 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyRequiredDifference, "P1").WithLocation(4, 27),
                // (7,36): error CS9309: Both partial property declarations must be required or neither may be required
                //     public required partial string P2 { get => ""; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyRequiredDifference, "P2").WithLocation(7, 36),
                // (11,17): error CS9035: Required member 'C.P1' must be set in the object initializer or attribute constructor.
                //         _ = new C();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.P1").WithLocation(11, 17));
        }

        [Fact]
        public void AliasDifference()
        {
            var source = """
                namespace NS;

                using MyInt = System.Int32;
                using MyInt2 = System.Int32;

                partial class C
                {
                    public partial int P1 { get; set; }
                    public partial MyInt P1 { get => 1; set { } }

                    public partial MyInt P2 { get; set; }
                    public partial MyInt2 P2 { get => 2; set { } }

                    public partial string P3 { get; set; }
                    public partial MyInt P3 { get => 3; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (15,26): error CS9307: Both partial property declarations must have the same type.
                //     public partial MyInt P3 { get => 3; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyTypeDifference, "P3").WithLocation(15, 26));
        }

        [Fact]
        public void ExplicitImplementation()
        {
            var source = """
                interface I
                {
                    public int P { get; set; }
                }

                partial class C1 : I
                {
                    partial int I.P { get; set; }
                    partial int I.P { get => 1; set { } }
                }

                partial class C2 : I
                {
                    partial int I.P { get; set; }
                }

                partial class C3 : I
                {
                    partial int I.P { get => 1; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): error CS0754: A partial member may not explicitly implement an interface member
                //     partial int I.P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "P").WithLocation(8, 19),
                // (14,19): error CS9300: Partial property 'C2.I.P' must have an implementation part.
                //     partial int I.P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P").WithArguments("C2.I.P").WithLocation(14, 19),
                // (14,19): error CS0754: A partial member may not explicitly implement an interface member
                //     partial int I.P { get; set; }
                Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "P").WithLocation(14, 19),
                // (19,19): error CS9301: Partial property 'C3.I.P' must have a definition part.
                //     partial int I.P { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "P").WithArguments("C3.I.P").WithLocation(19, 19),
                // (19,19): error CS0754: A partial member may not explicitly implement an interface member
                //     partial int I.P { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "P").WithLocation(19, 19));
        }

        [Fact]
        public void NotInPartialType()
        {
            var source = """
                class C
                {
                    partial int P1 { get; set; }
                    partial int P1 { get => 1; set { } }

                    partial int P2 { get; set; }

                    partial int P3 { get => 1; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS0751: A partial member must be declared within a partial type
                //     partial int P1 { get; set; }
                Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "P1").WithLocation(3, 17),
                // (6,17): error CS9300: Partial property 'C.P2' must have an implementation part.
                //     partial int P2 { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P2").WithArguments("C.P2").WithLocation(6, 17),
                // (6,17): error CS0751: A partial member must be declared within a partial type
                //     partial int P2 { get; set; }
                Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "P2").WithLocation(6, 17),
                // (8,17): error CS9301: Partial property 'C.P3' must have a definition part.
                //     partial int P3 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "P3").WithArguments("C.P3").WithLocation(8, 17),
                // (8,17): error CS0751: A partial member must be declared within a partial type
                //     partial int P3 { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "P3").WithLocation(8, 17));
        }

        [Fact]
        public void Semantics_Indexers_01()
        {
            var source = """
                using System;

                partial class C
                {
                    public partial int this[int i] { get; set; }
                    public partial int this[int i]
                    {
                        get => i;
                        set
                        {
                            Console.Write(i);
                            Console.Write(value);
                        }
                    }

                    static void Main()
                    {
                        var c = new C();
                        Console.Write(c[1]);
                        c[2] = 3;
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "123");
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("ref")]
        [InlineData("out")]
        public void RefKindDifference_IndexerParameter_01(string refKind)
        {
            // byvalue is distinct from byreference for signature matching
            var source = $$"""
                partial class C1
                {
                    public partial int this[int i] { get; set; }
                    public partial int this[{{refKind}} int i] { get => i = 0; set => i = 0; }
                }

                partial class C2
                {
                    public partial int this[{{refKind}} int i] { get; set; }
                    public partial int this[int i] { get => i; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C1.this[int]' must have an implementation part.
                //     public partial int this[int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C1.this[int]").WithLocation(3, 24),
                // (4,24): error CS9301: Partial property 'C1.this[ref int]' must have a definition part.
                //     public partial int this[ref int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments($"C1.this[{refKind} int]").WithLocation(4, 24),
                // (4,29): error CS0631: ref and out are not valid in this context
                //     public partial int this[ref int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, refKind).WithLocation(4, 29),
                // (9,24): error CS9300: Partial property 'C2.this[ref int]' must have an implementation part.
                //     public partial int this[ref int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments($"C2.this[{refKind} int]").WithLocation(9, 24),
                // (9,29): error CS0631: ref and out are not valid in this context
                //     public partial int this[ref int i] { get; set; }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, refKind).WithLocation(9, 29),
                // (10,24): error CS9301: Partial property 'C2.this[int]' must have a definition part.
                //     public partial int this[int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C2.this[int]").WithLocation(10, 24));
        }

        [Theory]
        [InlineData("in")]
        [InlineData("ref readonly")]
        public void RefKindDifference_IndexerParameter_02(string refKind)
        {
            var source = $$"""
                partial class C1
                {
                    public partial int this[int i] { get; set; }
                    public partial int this[{{refKind}} int i] { get => i; set { } }
                }

                partial class C2
                {
                    public partial int this[{{refKind}} int i] { get; set; }
                    public partial int this[int i] { get => i; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C1.this[int]' must have an implementation part.
                //     public partial int this[int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C1.this[int]").WithLocation(3, 24),
                // (4,24): error CS9301: Partial property 'C1.this[in int]' must have a definition part.
                //     public partial int this[in int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments($"C1.this[{refKind} int]").WithLocation(4, 24),
                // (9,24): error CS9300: Partial property 'C2.this[in int]' must have an implementation part.
                //     public partial int this[in int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments($"C2.this[{refKind} int]").WithLocation(9, 24),
                // (10,24): error CS9301: Partial property 'C2.this[int]' must have a definition part.
                //     public partial int this[int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C2.this[int]").WithLocation(10, 24));
        }

        [Fact]
        public void RefKindDifference_IndexerParameter_03()
        {
            var source = """
                partial class C1
                {
                    public partial int this[ref int i] { get; set; }
                    public partial int this[out int i] { get => i = 0; set => i = 0; }
                }

                partial class C2
                {
                    public partial int this[out int i] { get; set; }
                    public partial int this[ref int i] { get => i; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C1.this[ref int]' must have an implementation part.
                //     public partial int this[ref int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C1.this[ref int]").WithLocation(3, 24),
                // (3,29): error CS0631: ref and out are not valid in this context
                //     public partial int this[ref int i] { get; set; }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(3, 29),
                // (4,24): error CS9301: Partial property 'C1.this[out int]' must have a definition part.
                //     public partial int this[out int i] { get => i = 0; set => i = 0; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C1.this[out int]").WithLocation(4, 24),
                // (4,24): error CS0111: Type 'C1' already defines a member called 'this' with the same parameter types
                //     public partial int this[out int i] { get => i = 0; set => i = 0; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C1").WithLocation(4, 24),
                // (4,29): error CS0631: ref and out are not valid in this context
                //     public partial int this[out int i] { get => i = 0; set => i = 0; }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(4, 29),
                // (9,24): error CS9300: Partial property 'C2.this[out int]' must have an implementation part.
                //     public partial int this[out int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C2.this[out int]").WithLocation(9, 24),
                // (9,29): error CS0631: ref and out are not valid in this context
                //     public partial int this[out int i] { get; set; }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(9, 29),
                // (10,24): error CS9301: Partial property 'C2.this[ref int]' must have a definition part.
                //     public partial int this[ref int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C2.this[ref int]").WithLocation(10, 24),
                // (10,24): error CS0111: Type 'C2' already defines a member called 'this' with the same parameter types
                //     public partial int this[ref int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C2").WithLocation(10, 24),
                // (10,29): error CS0631: ref and out are not valid in this context
                //     public partial int this[ref int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(10, 29));
        }

        [Fact]
        public void RefKindDifference_IndexerParameter_04()
        {
            // note: this non-merging behavior in presence of ref kind differences is consistent with partial methods
            var source = """
                partial class C1
                {
                    public partial int this[in int i] { get; set; }
                    public partial int this[ref readonly int i] { get => i; set { } }
                }

                partial class C2
                {
                    public partial int this[ref readonly int i] { get; set; }
                    public partial int this[in int i] { get => i; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C1.this[in int]' must have an implementation part.
                //     public partial int this[in int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C1.this[in int]").WithLocation(3, 24),
                // (4,24): error CS9301: Partial property 'C1.this[ref readonly int]' must have a definition part.
                //     public partial int this[ref readonly int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C1.this[ref readonly int]").WithLocation(4, 24),
                // (4,24): error CS0111: Type 'C1' already defines a member called 'this' with the same parameter types
                //     public partial int this[ref readonly int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C1").WithLocation(4, 24),
                // (9,24): error CS9300: Partial property 'C2.this[ref readonly int]' must have an implementation part.
                //     public partial int this[ref readonly int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C2.this[ref readonly int]").WithLocation(9, 24),
                // (10,24): error CS9301: Partial property 'C2.this[in int]' must have a definition part.
                //     public partial int this[in int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C2.this[in int]").WithLocation(10, 24),
                // (10,24): error CS0111: Type 'C2' already defines a member called 'this' with the same parameter types
                //     public partial int this[in int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C2").WithLocation(10, 24));
        }

        [Theory]
        [InlineData("in", "ref")]
        [InlineData("in", "out")]
        [InlineData("ref readonly", "ref")]
        [InlineData("ref readonly", "out")]
        public void RefKindDifference_IndexerParameter_05(string goodRefKind, string badRefKind)
        {
            // Show that errors occur when declarations differ between allowed vs. disallowed parameter ref kinds.
            var source = $$"""
                partial class C1
                {
                    public partial int this[{{goodRefKind}} int i] { get; set; }
                    public partial int this[{{badRefKind}} int i] { get => i = 0; set => i = 0; }
                }

                partial class C2
                {
                    public partial int this[{{badRefKind}} int i] { get; set; }
                    public partial int this[{{goodRefKind}} int i] { get => i; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C1.this[in int]' must have an implementation part.
                //     public partial int this[in int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments($"C1.this[{goodRefKind} int]").WithLocation(3, 24),
                // (4,24): error CS9301: Partial property 'C1.this[ref int]' must have a definition part.
                //     public partial int this[ref int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments($"C1.this[{badRefKind} int]").WithLocation(4, 24),
                // (4,24): error CS0111: Type 'C1' already defines a member called 'this' with the same parameter types
                //     public partial int this[ref int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C1").WithLocation(4, 24),
                // (4,29): error CS0631: ref and out are not valid in this context
                //     public partial int this[ref int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, badRefKind).WithLocation(4, 29),
                // (9,24): error CS9300: Partial property 'C2.this[ref int]' must have an implementation part.
                //     public partial int this[ref int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments($"C2.this[{badRefKind} int]").WithLocation(9, 24),
                // (9,29): error CS0631: ref and out are not valid in this context
                //     public partial int this[ref int i] { get; set; }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, badRefKind).WithLocation(9, 29),
                // (10,24): error CS9301: Partial property 'C2.this[in int]' must have a definition part.
                //     public partial int this[in int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments($"C2.this[{goodRefKind} int]").WithLocation(10, 24),
                // (10,24): error CS0111: Type 'C2' already defines a member called 'this' with the same parameter types
                //     public partial int this[in int i] { get => i; set { } }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C2").WithLocation(10, 24));
        }

        [Fact]
        public void TypeDifference_IndexerParameter()
        {
            var source = """
                partial class C
                {
                    public partial int this[int i] { get; set; }
                    public partial int this[string s] { get => 1; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C.this[int]' must have an implementation part.
                //     public partial int this[int i] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C.this[int]").WithLocation(3, 24),
                // (4,24): error CS9301: Partial property 'C.this[string]' must have a definition part.
                //     public partial int this[string s] { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C.this[string]").WithLocation(4, 24));
        }

        [Fact]
        public void NullabilityDifference_IndexerParameter()
        {
            var source = $$"""
                #nullable enable
                partial class C
                {
                    public partial int this[string s] { get; set; }
                    public partial int this[string? s] { get => 1; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,24): warning CS9308: Partial property declarations 'int C.this[string s]' and 'int C.this[string? s]' have signature differences.
                //     public partial int this[string? s] { get => 1; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "this").WithArguments("int C.this[string s]", "int C.this[string? s]").WithLocation(5, 24));
        }

        [Fact]
        public void DynamicDifference_IndexerParameter()
        {
            var source = """
                #nullable enable
                partial class C
                {
                    public partial int this[dynamic[] s] { get; set; }
                    public partial int this[object[] s] { get => 1; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,24): warning CS9308: Partial property declarations 'int C.this[dynamic[] s]' and 'int C.this[object[] s]' have signature differences. 
                //     public partial int this[object[] s] { get => 1; set { } }
                Diagnostic(ErrorCode.WRN_PartialPropertySignatureDifference, "this").WithArguments("int C.this[dynamic[] s]", "int C.this[object[] s]").WithLocation(5, 24));
        }

        [Fact]
        public void Semantics_Params()
        {
            var source = """
                using System;
                using System.Collections.Generic;

                partial class C
                {
                    public static void Main()
                    {
                        var c = new C();
                        _ = c[1, 2, 3];
                        _ = c["a", "b", "c"];
                    }

                    public partial int this[params int[] arr] { get; }
                    public partial int this[params int[] arr]
                    {
                        get
                        {
                            foreach (var i in arr)
                                Console.Write(i);

                            return 0;
                        }
                    }

                    public partial int this[params IEnumerable<string> enumerable] { get; }
                    public partial int this[params IEnumerable<string> enumerable]
                    {
                        get
                        {
                            foreach (var i in enumerable)
                                Console.Write(i);

                            return 0;
                        }
                    }

                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "123abc");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ParamsDifference_IndexerParameter()
        {
            var source = """
                #nullable enable
                using System.Collections.Generic;

                partial class C
                {
                    public partial int this[params object[] arr] { get; set; }
                    public partial int this[object[] arr] { get => 1; set { } }

                    public partial int this[IEnumerable<object> enumerable] { get; set; }
                    public partial int this[params IEnumerable<object> enumerable] { get => 1; set { } }

                    public partial int this[object[] enumerable, int _] { get; set; }
                    public partial int this[params object[] enumerable, int _] { get => 1; set { } }
                }

                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,24): error CS0758: Both partial member declarations must use a params parameter or neither may use a params parameter
                //     public partial int this[object[] arr] { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberParamsDifference, "this").WithLocation(7, 24),
                // (10,24): error CS0758: Both partial member declarations must use a params parameter or neither may use a params parameter
                //     public partial int this[params IEnumerable<object> enumerable] { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberParamsDifference, "this").WithLocation(10, 24),
                // (13,29): error CS0231: A params parameter must be the last parameter in a parameter list
                //     public partial int this[params object[] enumerable, int _] { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_ParamsLast, "params object[] enumerable").WithLocation(13, 29));
        }

        [Fact]
        public void Semantics_Scoped()
        {
            var source = """
                using System;

                Console.Write(new C().M()._i);

                ref struct RS(ref int i)
                {
                    public ref int _i = ref i;
                }

                partial class C
                {
                    static int s_i = 1;

                    public partial RS this[scoped RS rs] { get; }
                    public partial RS this[scoped RS rs] { get => new RS(ref s_i); }

                    public RS M()
                    {
                        int i = 0;
                        RS rs = new RS(ref i);
                        return this[rs]; // ok
                    }
                }
                """;

            var verifier = CompileAndVerify(
                source,
                targetFramework: TargetFramework.Net70,
                verify: Verification.Fails,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "1" : null);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Scoped_Errors()
        {
            var source = """
                ref struct RS(ref int i) { }

                partial class C1
                {
                    public partial RS this[scoped RS rs] { get; }
                    public partial RS this[scoped RS rs] { get => rs; } // 1
                }

                partial class C2
                {
                    public partial RS this[RS rs] { get; }
                    public partial RS this[RS rs] { get => rs; } // ok

                    public RS M()
                    {
                        int i = 0;
                        RS rs = new RS(ref i);
                        return this[rs]; // error
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,23): warning CS9113: Parameter 'i' is unread.
                // ref struct RS(ref int i) { }
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "i").WithArguments("i").WithLocation(1, 23),
                // (6,51): error CS8352: Cannot use variable 'scoped RS rs' in this context because it may expose referenced variables outside of their declaration scope
                //     public partial RS this[scoped RS rs] { get => rs; } // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs").WithArguments("scoped RS rs").WithLocation(6, 51),
                // (18,16): error CS8347: Cannot use a result of 'C2.this[RS]' in this context because it may expose variables referenced by parameter 'rs' outside of their declaration scope
                //         return this[rs]; // error
                Diagnostic(ErrorCode.ERR_EscapeCall, "this[rs]").WithArguments("C2.this[RS]", "rs").WithLocation(18, 16),
                // (18,21): error CS8352: Cannot use variable 'rs' in this context because it may expose referenced variables outside of their declaration scope  
                //         return this[rs]; // error
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rs").WithArguments("rs").WithLocation(18, 21));
        }

        [Fact]
        public void ScopedDifference_IndexerParameter()
        {
            var source = """
                #nullable enable

                ref struct RS { }

                partial class C
                {
                    public partial RS this[scoped RS rs] { get; set; }
                    public partial RS this[RS rs] { get => default; set { } }

                    public partial RS this[RS rs, int _] { get; set; }
                    public partial RS this[scoped RS rs, int _] { get => default; set { } }
                }

                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,23): error CS8988: The 'scoped' modifier of parameter 'rs' doesn't match partial definition.
                //     public partial RS this[RS rs] { get => default; set { } }
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "this").WithArguments("rs").WithLocation(8, 23),
                // (11,23): error CS8988: The 'scoped' modifier of parameter 'rs' doesn't match partial definition.
                //     public partial RS this[scoped RS rs, int _] { get => default; set { } }
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "this").WithArguments("rs").WithLocation(11, 23));
        }

        [Theory]
        [InlineData("in")]
        [InlineData("ref readonly")]
        public void ScopedDifference_IndexerParameter_SupportedRefKind(string refKind)
        {
            var source = $$"""
                #nullable enable

                ref struct RS { }

                partial class C
                {
                    public partial RS this[scoped {{refKind}} int i] { get; set; }
                    public partial RS this[{{refKind}} int i] { get => default; set { } }

                    public partial RS this[{{refKind}} int i, int _] { get; set; }
                    public partial RS this[scoped {{refKind}} int i, int _] { get => default; set { } }
                }

                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,23): error CS8988: The 'scoped' modifier of parameter 'i' doesn't match partial definition.
                //     public partial RS this[in int i] { get => default; set { } }
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "this").WithArguments("i").WithLocation(8, 23),
                // (11,23): error CS8988: The 'scoped' modifier of parameter 'i' doesn't match partial definition.
                //     public partial RS this[scoped in int i, int _] { get => default; set { } }
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "this").WithArguments("i").WithLocation(11, 23));
        }

        [Fact]
        public void ScopedDifference_IndexerParameter_UnsupportedRefKind()
        {
            var source = $$"""
                #nullable enable

                ref struct RS { }

                partial class C
                {
                    public partial RS this[scoped ref int i] { get; set; }
                    public partial RS this[ref int i] { get => default; set { } }

                    public partial RS this[ref int i, int _] { get; set; }
                    public partial RS this[scoped ref int i, int _] { get => default; set { } }
                }

                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,35): error CS0631: ref and out are not valid in this context
                //     public partial RS this[scoped ref int i] { get; set; }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(7, 35),
                // (8,23): error CS8988: The 'scoped' modifier of parameter 'i' doesn't match partial definition.
                //     public partial RS this[ref int i] { get => default; set { } }
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "this").WithArguments("i").WithLocation(8, 23),
                // (8,28): error CS0631: ref and out are not valid in this context
                //     public partial RS this[ref int i] { get => default; set { } }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(8, 28),
                // (10,28): error CS0631: ref and out are not valid in this context
                //     public partial RS this[ref int i, int _] { get; set; }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(10, 28),
                // (11,23): error CS8988: The 'scoped' modifier of parameter 'i' doesn't match partial definition.
                //     public partial RS this[scoped ref int i, int _] { get => default; set { } }
                Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "this").WithArguments("i").WithLocation(11, 23),
                // (11,35): error CS0631: ref and out are not valid in this context
                //     public partial RS this[scoped ref int i, int _] { get => default; set { } }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(11, 35));
        }

        [Fact]
        public void Semantics_OptionalParameters()
        {
            var source = """
                using System;

                var c = new C();
                Console.Write(c[1]);
                Console.Write(c[1, 2]);

                partial class C
                {
                    public partial int this[int x, int y = 1] { get; }
                    public partial int this[int x, int y] { get => y; }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void OptionalParameters_OnImplementationPart_ResultsInAWarning()
        {
            // A warning is reported for optional parameters on implementation part, even if it matches the definition part.
            var source = """
                partial class C
                {
                    public partial int this[int x, int y = 1] { get; set; }
                    public partial int this[int x, int y = 1] { get => y; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,40): warning CS1066: The default value specified for parameter 'y' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     public partial int this[int x, int y = 1] { get => y; set { } }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "y").WithArguments("y").WithLocation(4, 40));
        }

        [Fact]
        public void OptionalParameters_OnImplementationPart_NotRespectedAtCallSite_Semantics()
        {
            var source = """
                using System;

                partial class C
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.Write(c[0, 0]);
                        Console.Write(c["a", 0]);
                        Console.Write(c["a"]);
                    }

                    public partial int this[int x, int y] { get; set; }
                    public partial int this[int x, int y = 1] { get => y; set { } }

                    public partial int this[string x, int y = 1] { get; set; }
                    public partial int this[string x, int y = 2] { get => y; set { } }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "001");
            verifier.VerifyDiagnostics(
                // (14,40): warning CS1066: The default value specified for parameter 'y' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     public partial int this[int x, int y = 1] { get => y; set { } }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "y").WithArguments("y").WithLocation(14, 40),
                // (17,43): warning CS1066: The default value specified for parameter 'y' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     public partial int this[string x, int y = 2] { get => y; set { } }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "y").WithArguments("y").WithLocation(17, 43));
        }

        [Fact]
        public void OptionalParameters_OnImplementationPart_NotRespectedAtCallSite()
        {
            var source = """
                using System;

                partial class C
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.Write(c[0]);
                    }

                    public partial int this[int x, int y] { get; set; }
                    public partial int this[int x, int y = 1] { get => y; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,23): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'C.this[int, int]'
                //         Console.Write(c[0]);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c[0]").WithArguments("y", "C.this[int, int]").WithLocation(8, 23),
                // (12,40): warning CS1066: The default value specified for parameter 'y' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     public partial int this[int x, int y = 1] { get => y; set { } }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "y").WithArguments("y").WithLocation(12, 40));
        }

        [Fact]
        public void OptionalParameters_AllParametersAreOptional()
        {
            // An indexer access needs at least one argument in order to be valid
            var source = """
                partial class C
                {
                    void M()
                    {
                        _ = this[];
                        _ = this[1];
                    }

                    public partial int this[int x = 1, int y = 2] { get; set; }
                    public partial int this[int x = 1, int y = 2] { get => y; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,18): error CS0443: Syntax error; value expected
                //         _ = this[];
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(5, 18),
                // (10,33): warning CS1066: The default value specified for parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     public partial int this[int x = 1, int y = 2] { get => y; set { } }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "x").WithArguments("x").WithLocation(10, 33),
                // (10,44): warning CS1066: The default value specified for parameter 'y' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     public partial int this[int x = 1, int y = 2] { get => y; set { } }
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "y").WithArguments("y").WithLocation(10, 44));
        }

        [Fact]
        public void Indexers_MissingOrUnexpectedDeclarations()
        {
            var source = """
                partial class C
                {
                    public partial int this[int x] { get; set; } // missing impl

                    public partial int this[int x, int y] { get => 1; set { } } // missing decl

                    public partial int this[int x, int y, int z] { get; set; } // duplicate decl
                    public partial int this[int x, int y, int z] { get; set; }
                    public partial int this[int x, int y, int z] { get => 1; set { } }

                    public partial int this[int x, int y, int z, int a] { get; set; } // duplicate impl
                    public partial int this[int x, int y, int z, int a] { get => 1; set { } }
                    public partial int this[int x, int y, int z, int a] { get => 1; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS9300: Partial property 'C.this[int]' must have an implementation part.
                //     public partial int this[int x] { get; set; } // missing impl
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "this").WithArguments("C.this[int]").WithLocation(3, 24),
                // (5,24): error CS9301: Partial property 'C.this[int, int]' must have a definition part.
                //     public partial int this[int x, int y] { get => 1; set { } } // missing decl
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingDefinition, "this").WithArguments("C.this[int, int]").WithLocation(5, 24),
                // (8,24): error CS9302: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //     public partial int this[int x, int y, int z] { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "this").WithLocation(8, 24),
                // (8,24): error CS0111: Type 'C' already defines a member called 'this' with the same parameter types
                //     public partial int this[int x, int y, int z] { get; set; }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C").WithLocation(8, 24),
                // (13,24): error CS9303: A partial property may not have multiple implementing declarations
                //     public partial int this[int x, int y, int z, int a] { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "this").WithLocation(13, 24),
                // (13,24): error CS0111: Type 'C' already defines a member called 'this' with the same parameter types
                //     public partial int this[int x, int y, int z, int a] { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "this").WithArguments("this", "C").WithLocation(13, 24));
        }

        [Fact]
        public void Indexers_ReturnTypeDifference()
        {
            var source = """
                partial class C
                {
                    public partial int[] this[int x] { get; set; }
                    public partial string[] this[int x] { get => []; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,29): error CS9307: Both partial property declarations must have the same type.
                //     public partial string[] this[int x] { get => []; set { } }
                Diagnostic(ErrorCode.ERR_PartialPropertyTypeDifference, "this").WithLocation(4, 29));
        }

        [Fact]
        public void Indexers_TupleElementNameDifference()
        {
            var source = """
                partial class C
                {
                    // in return type
                    public partial (int x, int y)[] this[int x] { get; set; }
                    public partial (int a, int b)[] this[int x] { get => []; set { } }
                    
                    // in parameter type
                    public partial int this[(int x, int y) pair] { get; set; }
                    public partial int this[(int a, int b) pair] { get => 1; set { } }

                    // in both return and parameter type
                    public partial (int x, int y)[] this[(int x, int y, int z) pair] { get; set; }
                    public partial (int a, int b)[] this[(int a, int b, int c) pair] { get => []; set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,37): error CS8142: Both partial member declarations, 'C.this[int]' and 'C.this[int]', must use the same tuple element names.
                //     public partial (int a, int b)[] this[int x] { get => []; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "this").WithArguments("C.this[int]", "C.this[int]").WithLocation(5, 37),
                // (9,24): error CS8142: Both partial member declarations, 'C.this[(int x, int y)]' and 'C.this[(int a, int b)]', must use the same tuple element names.
                //     public partial int this[(int a, int b) pair] { get => 1; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "this").WithArguments("C.this[(int x, int y)]", "C.this[(int a, int b)]").WithLocation(9, 24),
                // (13,37): error CS8142: Both partial member declarations, 'C.this[(int x, int y, int z)]' and 'C.this[(int a, int b, int c)]', must use the same tuple element names.
                //     public partial (int a, int b)[] this[(int a, int b, int c) pair] { get => []; set { } }
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "this").WithArguments("C.this[(int x, int y, int z)]", "C.this[(int a, int b, int c)]").WithLocation(13, 37));
        }

        // PROTOTYPE(partial-properties): override partial property where base has modopt
        // PROTOTYPE(partial-properties): test indexers incl parameters with attributes
        // PROTOTYPE(partial-properties): test merging property attributes
        // PROTOTYPE(partial-properties): [UnscopedRef]+scoped difference across partials
        // PROTOTYPE(partial-properties): test that doc comments work consistently with partial methods (and probably spec it as well)
        // PROTOTYPE(partial-properties): test CallerInfo attributes applied to either definition or implementation part
    }
}
