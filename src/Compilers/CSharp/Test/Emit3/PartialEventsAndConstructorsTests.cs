// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class PartialEventsAndConstructorsTests : CSharpTestBase
{
    [Fact]
    public void ReturningPartialType_LocalFunction_InMethod()
    {
        var source = """
            class @partial
            {
                static void Main()
                {
                    System.Console.Write(F().GetType().Name);
                    partial F() => new();
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular13, expectedOutput: "partial").VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (5,30): error CS0103: The name 'F' does not exist in the current context
            //         System.Console.Write(F().GetType().Name);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "F").WithArguments("F").WithLocation(5, 30),
            // (5,50): error CS1513: } expected
            //         System.Console.Write(F().GetType().Name);
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 50),
            // (6,17): error CS1520: Method must have a return type
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "F").WithLocation(6, 17),
            // (6,17): error CS0751: A partial member must be declared within a partial type
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "F").WithLocation(6, 17),
            // (6,17): error CS9276: Partial member 'partial.partial()' must have a definition part.
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "F").WithArguments("partial.partial()").WithLocation(6, 17),
            // (6,24): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_IllegalStatement, "new()").WithLocation(6, 24),
            // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReturningPartialType_LocalFunction_TopLevel()
    {
        var source = """
            System.Console.Write(F().GetType().Name);
            partial F() => new();
            class @partial;
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular13, expectedOutput: "partial").VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (1,22): error CS0103: The name 'F' does not exist in the current context
            // System.Console.Write(F().GetType().Name);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "F").WithArguments("F").WithLocation(1, 22),
            // (2,9): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
            // partial F() => new();
            Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "F").WithLocation(2, 9),
            // (2,10): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            // partial F() => new();
            Diagnostic(ErrorCode.ERR_IllegalStatement, "() => new()").WithLocation(2, 10)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReturningPartialType_Method()
    {
        var source = """
            class C
            {
                partial F() => new();
                static void Main()
                {
                    System.Console.Write(new C().F().GetType().Name);
                }
            }
            class @partial;
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular13, expectedOutput: "partial").VerifyDiagnostics();

        var expectedDiagnostics = new[]
        {
            // (3,13): error CS1520: Method must have a return type
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "F").WithLocation(3, 13),
            // (3,13): error CS0751: A partial member must be declared within a partial type
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "F").WithLocation(3, 13),
            // (3,13): error CS9276: Partial member 'C.C()' must have a definition part.
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "F").WithArguments("C.C()").WithLocation(3, 13),
            // (3,20): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_IllegalStatement, "new()").WithLocation(3, 20),
            // (6,38): error CS1061: 'C' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            //         System.Console.Write(new C().F().GetType().Name);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("C", "F").WithLocation(6, 38)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReturningPartialType_Method_CouldBePartialConstructor()
    {
        var source = """
            class C
            {
                partial F() { }
                partial C() { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     partial F() { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(3, 5),
            // (3,5): error CS9260: Feature 'partial events and constructors' is not available in C# 13.0. Please use language version 14.0 or greater.
            //     partial F() { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "partial").WithArguments("partial events and constructors", "14.0").WithLocation(3, 5),
            // (3,13): error CS0161: 'C.F()': not all code paths return a value
            //     partial F() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "F").WithArguments("C.F()").WithLocation(3, 13),
            // (4,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(4, 5),
            // (4,5): error CS9260: Feature 'partial events and constructors' is not available in C# 13.0. Please use language version 14.0 or greater.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "partial").WithArguments("partial events and constructors", "14.0").WithLocation(4, 5),
            // (4,13): error CS0542: 'C': member names cannot be the same as their enclosing type
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(4, 13),
            // (4,13): error CS0161: 'C.C()': not all code paths return a value
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "C").WithArguments("C.C()").WithLocation(4, 13));
    }

    [Fact]
    public void ReturningPartialType_Method_Escaped()
    {
        var source = """
            class C
            {
                @partial F() { }
                @partial C() { }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (3,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     @partial F() { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "@partial").WithArguments("partial").WithLocation(3, 5),
            // (3,14): error CS0161: 'C.F()': not all code paths return a value
            //     @partial F() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "F").WithArguments("C.F()").WithLocation(3, 14),
            // (4,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     @partial C() { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "@partial").WithArguments("partial").WithLocation(4, 5),
            // (4,14): error CS0542: 'C': member names cannot be the same as their enclosing type
            //     @partial C() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(4, 14),
            // (4,14): error CS0161: 'C.C()': not all code paths return a value
            //     @partial C() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "C").WithArguments("C.C()").WithLocation(4, 14)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void LangVersion()
    {
        var source = """
            partial class C
            {
                partial event System.Action E;
                partial event System.Action E { add { } remove { } }
                partial C();
                partial C() { }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,33): error CS8703: The modifier 'partial' is not valid for this item in C# 13.0. Please use language version '14.0' or greater.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "E").WithArguments("partial", "13.0", "14.0").WithLocation(3, 33),
            // (5,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     partial C();
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(5, 5),
            // (5,5): error CS9260: Feature 'partial events and constructors' is not available in C# 13.0. Please use language version 14.0 or greater.
            //     partial C();
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "partial").WithArguments("partial events and constructors", "14.0").WithLocation(5, 5),
            // (5,13): error CS0501: 'C.C()' must declare a body because it is not marked abstract, extern, or partial
            //     partial C();
            Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.C()").WithLocation(5, 13),
            // (5,13): error CS0542: 'C': member names cannot be the same as their enclosing type
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(5, 13),
            // (6,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(6, 5),
            // (6,5): error CS9260: Feature 'partial events and constructors' is not available in C# 13.0. Please use language version 14.0 or greater.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "partial").WithArguments("partial events and constructors", "14.0").WithLocation(6, 5),
            // (6,13): error CS0542: 'C': member names cannot be the same as their enclosing type
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(6, 13),
            // (6,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(6, 13),
            // (6,13): error CS0161: 'C.C()': not all code paths return a value
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "C").WithArguments("C.C()").WithLocation(6, 13));

        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void PartialLast([CombinatorialValues("", "public")] string modifier)
    {
        var source = $$"""
            partial class C
            {
                {{modifier}}
                partial event System.Action E;
                {{modifier}}
                partial event System.Action E { add { } remove { } }
                {{modifier}}
                partial C();
                {{modifier}}
                partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void PartialNotLast()
    {
        var source = """
            partial class C
            {
                partial public event System.Action E;
                partial public event System.Action E { add { } remove { } }
                partial public C();
                partial public C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
            // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
            // (5,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public C();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 5),
            // (6,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public C() { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(6, 5));
    }

    [Fact]
    public void PartialAsType()
    {
        var source = """
            partial class C
            {
                partial C() => new partial();
            }

            class @partial;
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS9276: Partial member 'C.C()' must have a definition part.
            //     partial C() => new partial();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C").WithArguments("C.C()").WithLocation(3, 13));
    }

    [Fact]
    public void MissingImplementation()
    {
        var source = """
            partial class C
            {
                partial event System.Action E;
                partial C();
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,33): error CS9275: Partial member 'C.E' must have an implementation part.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "E").WithArguments("C.E").WithLocation(3, 33),
            // (4,13): error CS9275: Partial member 'C.C()' must have an implementation part.
            //     partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C").WithArguments("C.C()").WithLocation(4, 13));
    }

    [Fact]
    public void MissingDefinition()
    {
        var source = """
            partial class C
            {
                partial event System.Action E { add { } remove { } }
                partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,33): error CS9276: Partial member 'C.E' must have a definition part.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "E").WithArguments("C.E").WithLocation(3, 33),
            // (4,13): error CS9276: Partial member 'C.C()' must have a definition part.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C").WithArguments("C.C()").WithLocation(4, 13));
    }

    [Fact]
    public void DuplicateDefinition()
    {
        var source = """
            partial class C
            {
                partial event System.Action E, F;
                partial event System.Action E;
                partial event System.Action F;
                partial C();
                partial C();

                partial event System.Action E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
                partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,33): error CS9277: Partial member 'C.E' may not have multiple defining declarations.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "E").WithArguments("C.E").WithLocation(4, 33),
            // (4,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(4, 33),
            // (5,33): error CS9277: Partial member 'C.F' may not have multiple defining declarations.
            //     partial event System.Action F;
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "F").WithArguments("C.F").WithLocation(5, 33),
            // (5,33): error CS0102: The type 'C' already contains a definition for 'F'
            //     partial event System.Action F;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "F").WithArguments("C", "F").WithLocation(5, 33),
            // (7,13): error CS9277: Partial member 'C.C()' may not have multiple defining declarations.
            //     partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "C").WithArguments("C.C()").WithLocation(7, 13),
            // (7,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(7, 13));
    }

    [Fact]
    public void DuplicateImplementation()
    {
        var source = """
            partial class C
            {
                partial event System.Action E { add { } remove { } }
                partial event System.Action E { add { } remove { } }
                partial C() { }
                partial C() { }

                partial event System.Action E;
                partial C();
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,33): error CS9278: Partial member 'C.E' may not have multiple implementing declarations.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.E").WithLocation(4, 33),
            // (6,13): error CS9278: Partial member 'C.C()' may not have multiple implementing declarations.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "C").WithArguments("C.C()").WithLocation(6, 13),
            // (8,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(8, 33),
            // (9,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(9, 13));
    }

    [Fact]
    public void DuplicateDeclarations_01()
    {
        var source = """
            partial class C
            {
                partial event System.Action E { add { } remove { } }
                partial event System.Action E { add { } remove { } }
                partial C() { }
                partial C() { }

                partial event System.Action E;
                partial event System.Action E;
                partial C();
                partial C();
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,33): error CS9278: Partial member 'C.E' may not have multiple implementing declarations.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.E").WithLocation(4, 33),
            // (6,13): error CS9278: Partial member 'C.C()' may not have multiple implementing declarations.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "C").WithArguments("C.C()").WithLocation(6, 13),
            // (8,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(8, 33),
            // (9,33): error CS9277: Partial member 'C.E' may not have multiple defining declarations.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "E").WithArguments("C.E").WithLocation(9, 33),
            // (9,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(9, 33),
            // (10,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(10, 13),
            // (11,13): error CS9277: Partial member 'C.C()' may not have multiple defining declarations.
            //     partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "C").WithArguments("C.C()").WithLocation(11, 13),
            // (11,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(11, 13));
    }

    [Fact]
    public void DuplicateDeclarations_02()
    {
        var source = """
            partial class C
            {
                partial event System.Action E;
                partial void add_E(System.Action value);
                partial void remove_E(System.Action value);
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,33): error CS9275: Partial member 'C.E' must have an implementation part.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "E").WithArguments("C.E").WithLocation(3, 33),
            // (3,33): error CS0082: Type 'C' already reserves a member called 'add_E' with the same parameter types
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_MemberReserved, "E").WithArguments("add_E", "C").WithLocation(3, 33),
            // (3,33): error CS0082: Type 'C' already reserves a member called 'remove_E' with the same parameter types
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_MemberReserved, "E").WithArguments("remove_E", "C").WithLocation(3, 33));
    }

    [Fact]
    public void DuplicateDeclarations_03()
    {
        var source = """
            partial class C
            {
                partial event System.Action E;
                partial event System.Action E;
                partial C();
                partial C();

                partial event System.Action E { add { } remove { } }
                partial event System.Action E { add { } remove { } }
                partial C() { }
                partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,33): error CS9277: Partial member 'C.E' may not have multiple defining declarations.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "E").WithArguments("C.E").WithLocation(4, 33),
            // (4,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(4, 33),
            // (6,13): error CS9277: Partial member 'C.C()' may not have multiple defining declarations.
            //     partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "C").WithArguments("C.C()").WithLocation(6, 13),
            // (6,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(6, 13),
            // (9,33): error CS9278: Partial member 'C.E' may not have multiple implementing declarations.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.E").WithLocation(9, 33),
            // (9,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(9, 33),
            // (11,13): error CS9278: Partial member 'C.C()' may not have multiple implementing declarations.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "C").WithArguments("C.C()").WithLocation(11, 13),
            // (11,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(11, 13));
    }

    [Fact]
    public void DuplicateDeclarations_04()
    {
        var source = """
            using System;
            partial class C
            {
                partial event Action E;
                partial event Action E { add { } }
                partial event Action E { remove { } }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (5,26): error CS0065: 'C.E': event property must have both add and remove accessors
            //     partial event Action E { add { } }
            Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E").WithArguments("C.E").WithLocation(5, 26),
            // (6,26): error CS0065: 'C.E': event property must have both add and remove accessors
            //     partial event Action E { remove { } }
            Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E").WithArguments("C.E").WithLocation(6, 26),
            // (6,26): error CS9278: Partial member 'C.E' may not have multiple implementing declarations.
            //     partial event Action E { remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.E").WithLocation(6, 26),
            // (6,26): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event Action E { remove { } }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(6, 26));

        var events = comp.GetMembers("C.E");
        Assert.Equal(2, events.Length);

        var e1 = (SourceEventSymbol)events[0];
        Assert.True(e1.IsPartialDefinition);
        AssertEx.Equal("event System.Action C.E", e1.ToTestDisplayString());
        AssertEx.Equal("event System.Action C.E", e1.PartialImplementationPart.ToTestDisplayString());

        var e2 = (SourceEventSymbol)events[1];
        Assert.True(e2.IsPartialImplementation);
        AssertEx.Equal("event System.Action C.E", e2.ToTestDisplayString());
        Assert.Null(e2.PartialDefinitionPart);
    }

    [Fact]
    public void EventInitializer_Single()
    {
        var source = """
            partial class C
            {
                partial event System.Action E = null;
                partial event System.Action E { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,33): error CS9279: 'C.E': partial event cannot have initializer
            //     partial event System.Action E = null;
            Diagnostic(ErrorCode.ERR_PartialEventInitializer, "E").WithArguments("C.E").WithLocation(3, 33),
            // (3,33): warning CS0414: The field 'C.E' is assigned but its value is never used
            //     partial event System.Action E = null;
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "E").WithArguments("C.E").WithLocation(3, 33));
    }

    [Fact]
    public void EventInitializer_Multiple_01()
    {
        var source = """
            partial class C
            {
                partial event System.Action E, F = null;
                partial event System.Action E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,36): error CS9279: 'C.F': partial event cannot have initializer
            //     partial event System.Action E, F = null;
            Diagnostic(ErrorCode.ERR_PartialEventInitializer, "F").WithArguments("C.F").WithLocation(3, 36),
            // (3,36): warning CS0414: The field 'C.F' is assigned but its value is never used
            //     partial event System.Action E, F = null;
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "F").WithArguments("C.F").WithLocation(3, 36));
    }

    [Fact]
    public void EventInitializer_Multiple_02()
    {
        var source = """
            partial class C
            {
                partial event System.Action E = null, F = null;
                partial event System.Action E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,33): error CS9279: 'C.E': partial event cannot have initializer
            //     partial event System.Action E = null, F = null;
            Diagnostic(ErrorCode.ERR_PartialEventInitializer, "E").WithArguments("C.E").WithLocation(3, 33),
            // (3,33): warning CS0414: The field 'C.E' is assigned but its value is never used
            //     partial event System.Action E = null, F = null;
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "E").WithArguments("C.E").WithLocation(3, 33),
            // (3,43): error CS9279: 'C.F': partial event cannot have initializer
            //     partial event System.Action E = null, F = null;
            Diagnostic(ErrorCode.ERR_PartialEventInitializer, "F").WithArguments("C.F").WithLocation(3, 43),
            // (3,43): warning CS0414: The field 'C.F' is assigned but its value is never used
            //     partial event System.Action E = null, F = null;
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "F").WithArguments("C.F").WithLocation(3, 43));
    }

    [Fact]
    public void EventAccessorMissing()
    {
        var source = """
            partial class C
            {
                partial event System.Action E, F;
                partial event System.Action E { add { } }
                partial event System.Action F { remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,33): error CS0065: 'C.E': event property must have both add and remove accessors
            //     partial event System.Action E { add { } }
            Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E").WithArguments("C.E").WithLocation(4, 33),
            // (5,33): error CS0065: 'C.F': event property must have both add and remove accessors
            //     partial event System.Action F { remove { } }
            Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "F").WithArguments("C.F").WithLocation(5, 33));
    }

    [Fact]
    public void StaticPartialConstructor_01()
    {
        var source = """
            partial class C
            {
                static partial C();
                static partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     static partial C();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 12),
            // (4,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     static partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 12),
            // (4,20): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     static partial C() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(4, 20));
    }

    [Fact]
    public void StaticPartialConstructor_02()
    {
        var source = """
            partial class C
            {
                partial static C();
                partial static C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial static C();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
            // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial static C();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
            // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial static C() { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
            // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial static C() { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
            // (4,20): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial static C() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(4, 20));
    }

    [Fact]
    public void Finalizer()
    {
        var source = """
            partial class C
            {
                partial ~C();
                partial ~C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS1519: Invalid token '~' in class, record, struct, or interface member declaration
            //     partial ~C();
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "~").WithArguments("~").WithLocation(3, 13),
            // (4,13): error CS1519: Invalid token '~' in class, record, struct, or interface member declaration
            //     partial ~C() { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "~").WithArguments("~").WithLocation(4, 13),
            // (3,14): error CS0501: 'C.~C()' must declare a body because it is not marked abstract, extern, or partial
            //     partial ~C();
            Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.~C()").WithLocation(3, 14),
            // (4,14): error CS0111: Type 'C' already defines a member called '~C' with the same parameter types
            //     partial ~C() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("~C", "C").WithLocation(4, 14));
    }

    [Fact]
    public void PrimaryConstructor_Duplicate_WithoutInitializer()
    {
        var source = """
            partial class C()
            {
                partial C();
                partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(3, 13),
            // (4,13): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(4, 13));
    }

    [Fact]
    public void PrimaryConstructor_Duplicate_WithInitializer()
    {
        var source = """
            partial class C()
            {
                partial C();
                partial C() : this() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(3, 13),
            // (4,19): error CS0121: The call is ambiguous between the following methods or properties: 'C.C()' and 'C.C()'
            //     partial C() : this() { }
            Diagnostic(ErrorCode.ERR_AmbigCall, "this").WithArguments("C.C()", "C.C()").WithLocation(4, 19));
    }

    [Fact]
    public void PrimaryConstructor_DefinitionOnly()
    {
        var source = """
            partial class C()
            {
                partial C();
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS9275: Partial member 'C.C()' must have an implementation part.
            //     partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C").WithArguments("C.C()").WithLocation(3, 13),
            // (3,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(3, 13));
    }

    [Fact]
    public void PrimaryConstructor_ImplementationOnly_WithoutInitializer()
    {
        var source = """
            partial class C()
            {
                partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS9276: Partial member 'C.C()' must have a definition part.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C").WithArguments("C.C()").WithLocation(3, 13),
            // (3,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(3, 13),
            // (3,13): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(3, 13));
    }

    [Fact]
    public void PrimaryConstructor_ImplementationOnly_WithInitializer()
    {
        var source = """
            partial class C()
            {
                partial C() : this() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS9276: Partial member 'C.C()' must have a definition part.
            //     partial C() : this() { }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C").WithArguments("C.C()").WithLocation(3, 13),
            // (3,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C() : this() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(3, 13),
            // (3,19): error CS0121: The call is ambiguous between the following methods or properties: 'C.C()' and 'C.C()'
            //     partial C() : this() { }
            Diagnostic(ErrorCode.ERR_AmbigCall, "this").WithArguments("C.C()", "C.C()").WithLocation(3, 19));
    }

    [Fact]
    public void PrimaryConstructor_Different_WithoutInitializer()
    {
        var source = """
            partial class C()
            {
                partial C(int x);
                partial C(int x) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,13): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
            //     partial C(int x) { }
            Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(4, 13));
    }

    [Fact]
    public void PrimaryConstructor_Different_WithInitializer()
    {
        var source = """
            partial class C()
            {
                partial C(int x);
                partial C(int x) : this() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void PrimaryConstructor_Twice()
    {
        var source = """
            partial class C();
            partial class C() { }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,16): error CS8863: Only a single partial type declaration may have a parameter list
            // partial class C() { }
            Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "()").WithLocation(2, 16));
    }

    [Fact]
    public void NotInPartialType()
    {
        var source = """
            class C
            {
                partial event System.Action E;
                partial event System.Action E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
                partial C();
                partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,33): error CS0751: A partial member must be declared within a partial type
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "E").WithLocation(3, 33),
            // (5,33): error CS9276: Partial event 'C.F' must have a definition part.
            //     partial event System.Action F { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "F").WithArguments("C.F").WithLocation(5, 33),
            // (5,33): error CS0751: A partial member must be declared within a partial type
            //     partial event System.Action F { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "F").WithLocation(5, 33),
            // (6,13): error CS0751: A partial member must be declared within a partial type
            //     partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "C").WithLocation(6, 13),
            // (7,13): error CS0751: A partial member must be declared within a partial type
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "C").WithLocation(7, 13));
    }

    [Fact]
    public void InInterface()
    {
        var source = """
            partial interface I
            {
                partial event System.Action E;
                partial event System.Action E { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,37): error CS8701: Target runtime doesn't support default interface implementation.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "add").WithLocation(4, 37),
            // (4,45): error CS8701: Target runtime doesn't support default interface implementation.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "remove").WithLocation(4, 45));

        CreateCompilation(source, targetFramework: TargetFramework.Net60).VerifyDiagnostics();

        CreateCompilation(source, targetFramework: TargetFramework.Net60, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
            // (3,33): error CS8703: The modifier 'partial' is not valid for this item in C# 7.0. Please use language version '14.0' or greater.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "E").WithArguments("partial", "7.0", "14.0").WithLocation(3, 33),
            // (4,37): error CS8107: Feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "add").WithArguments("default interface implementation", "8.0").WithLocation(4, 37),
            // (4,45): error CS8107: Feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "remove").WithArguments("default interface implementation", "8.0").WithLocation(4, 45));
    }

    [Fact]
    public void InInterface_DefinitionOnly()
    {
        var source = """
            partial interface I
            {
                partial event System.Action E;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,33): error CS9275: Partial member 'I.E' must have an implementation part.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "E").WithArguments("I.E").WithLocation(3, 33));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77346")]
    public void InInterface_Virtual(
        [CombinatorialValues("", "public", "private", "protected", "internal", "protected internal", "private protected")] string access,
        [CombinatorialValues("", "virtual", "sealed")] string virt)
    {
        var source1 = $$"""
            using System;

            partial interface I
            {
                {{access}} {{virt}} partial event Action E;
                {{access}} {{virt}} partial event Action E
                {
                    add { Console.Write(1); }
                    remove { Console.Write(2); }
                }
            }
            """;

        var source2 = """
            using System;

            partial interface I
            {
                static void Main()
                {
                    M(new C1());
                    M(new C2());
                }

                static void M(I x)
                {
                    x.E += null;
                    x.E -= null;
                }
            }

            class C1 : I;

            class C2 : I
            {
                event Action I.E
                {
                    add { Console.Write(3); }
                    remove { Console.Write(4); }
                }
            }
            """;

        var expectedAccessibility = access switch
        {
            "" or "public" => Accessibility.Public,
            "private" => Accessibility.Private,
            "protected" => Accessibility.Protected,
            "internal" => Accessibility.Internal,
            "protected internal" => Accessibility.ProtectedOrInternal,
            "private protected" => Accessibility.ProtectedAndFriend,
            _ => throw ExceptionUtilities.UnexpectedValue(access),
        };

        bool expectedVirtual = access == "private"
            ? virt == "virtual"
            : virt != "sealed";

        bool expectedSealed = access == "private" && virt == "sealed";

        bool executable = virt != "sealed" && access is not "private";

        DiagnosticDescription[] expectedDiagnostics = [];

        if (access == "private")
        {
            if (virt == "sealed")
            {
                expectedDiagnostics =
                [
                    // (5,41): error CS0238: 'I.E' cannot be sealed because it is not an override
                    //     private sealed partial event Action E;
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, "E").WithArguments("I.E").WithLocation(5, 41)
                ];
            }
            else if (virt == "virtual")
            {
                expectedDiagnostics =
                [
                    // (5,42): error CS0621: 'I.E': virtual or abstract members cannot be private
                    //     private virtual partial event Action E;
                    Diagnostic(ErrorCode.ERR_VirtualPrivate, "E").WithArguments("I.E").WithLocation(5, 42)
                ];
            }
        }

        var comp = CreateCompilation(executable ? [source1, source2] : source1,
            options: TestOptions.DebugDll
                .WithOutputKind(executable ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary)
                .WithMetadataImportOptions(MetadataImportOptions.All),
            targetFramework: TargetFramework.Net60).VerifyDiagnostics(expectedDiagnostics);

        if (expectedDiagnostics.Length == 0)
        {
            CompileAndVerify(comp,
                sourceSymbolValidator: validate,
                symbolValidator: validate,
                expectedOutput: executable && ExecutionConditionUtil.IsMonoOrCoreClr ? "1234" : null,
                verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }
        else
        {
            validate(comp.SourceModule);
        }

        void validate(ModuleSymbol module)
        {
            var e = module.GlobalNamespace.GetMember<EventSymbol>("I.E");
            validateEvent(e);

            if (module is SourceModuleSymbol)
            {
                validateEvent((EventSymbol)e.GetPartialImplementationPart()!);
            }
        }

        void validateEvent(EventSymbol e)
        {
            Assert.False(e.IsAbstract);
            Assert.Equal(expectedVirtual, e.IsVirtual);
            Assert.Equal(expectedSealed, e.IsSealed);
            Assert.False(e.IsStatic);
            Assert.False(e.IsExtern);
            Assert.False(e.IsOverride);
            Assert.Equal(expectedAccessibility, e.DeclaredAccessibility);
            Assert.True(e.ContainingModule is not SourceModuleSymbol || e.IsPartialMember());
            validateAccessor(e.AddMethod!);
            validateAccessor(e.RemoveMethod!);
        }

        void validateAccessor(MethodSymbol m)
        {
            Assert.False(m.IsAbstract);
            Assert.Equal(expectedVirtual, m.IsVirtual);
            Assert.Equal(expectedVirtual, m.IsMetadataVirtual());
            Assert.Equal(expectedVirtual, m.IsMetadataNewSlot());
            Assert.Equal(expectedSealed, m.IsSealed);
            Assert.False(m.IsStatic);
            Assert.False(m.IsExtern);
            Assert.False(m.IsOverride);
            Assert.Equal(expectedAccessibility, m.DeclaredAccessibility);
            Assert.True(m.ContainingModule is not SourceModuleSymbol || m.IsPartialMember());
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/77346")]
    public void InInterface_StaticVirtual(
        [CombinatorialValues("", "public", "private", "protected", "internal", "protected internal", "private protected")] string access,
        [CombinatorialValues("", "virtual", "sealed")] string virt)
    {
        var source1 = $$"""
            using System;

            partial interface I
            {
                {{access}} static {{virt}} partial event Action E;
                {{access}} static {{virt}} partial event Action E
                {
                    add { Console.Write(1); }
                    remove { Console.Write(2); }
                }
            }
            """;

        var source2 = """
            using System;

            partial interface I
            {
                static void Main()
                {
                    M<C1>();
                    M<C2>();
                    M<C3>();
                }

                static void M<T>() where T : I
                {
                    T.E += null;
                    T.E -= null;
                }
            }

            class C1 : I
            {
                static event Action E
                {
                    add { Console.Write(3); }
                    remove { Console.Write(4); }
                }
            }

            class C2 : I
            {
                public static event Action E
                {
                    add { Console.Write(5); }
                    remove { Console.Write(6); }
                }
            }

            class C3 : I
            {
                static event Action I.E
                {
                    add { Console.Write(7); }
                    remove { Console.Write(8); }
                }
            }
            """;

        var expectedAccessibility = access switch
        {
            "" or "public" => Accessibility.Public,
            "private" => Accessibility.Private,
            "protected" => Accessibility.Protected,
            "internal" => Accessibility.Internal,
            "protected internal" => Accessibility.ProtectedOrInternal,
            "private protected" => Accessibility.ProtectedAndFriend,
            _ => throw ExceptionUtilities.UnexpectedValue(access),
        };

        bool expectedVirtual = virt == "virtual";

        bool executable = virt == "virtual" && access is not "private";

        DiagnosticDescription[] expectedDiagnostics = [];

        if (access == "private" && virt == "virtual")
        {
            expectedDiagnostics =
            [
                // (5,49): error CS0621: 'I.E': virtual or abstract members cannot be private
                //     private static virtual partial event Action E;
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "E").WithArguments("I.E").WithLocation(5, 49)
            ];
        }

        var comp = CreateCompilation(executable ? [source1, source2] : source1,
            options: TestOptions.DebugDll
                .WithOutputKind(executable ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary)
                .WithMetadataImportOptions(MetadataImportOptions.All),
            targetFramework: TargetFramework.Net60).VerifyDiagnostics(expectedDiagnostics);

        if (expectedDiagnostics.Length == 0)
        {
            CompileAndVerify(comp,
                sourceSymbolValidator: validate,
                symbolValidator: validate,
                expectedOutput: executable && ExecutionConditionUtil.IsMonoOrCoreClr ? "125678" : null,
                verify: virt != "virtual" ? Verification.FailsPEVerify : Verification.Fails with
                {
                    ILVerifyMessage = """
                        [M]: Missing callvirt following constrained prefix. { Offset = 0x8 }
                        [M]: Missing callvirt following constrained prefix. { Offset = 0x15 }
                        """,
                }).VerifyDiagnostics();
        }
        else
        {
            validate(comp.SourceModule);
        }

        void validate(ModuleSymbol module)
        {
            var e = module.GlobalNamespace.GetMember<EventSymbol>("I.E");
            validateEvent(e);

            if (module is SourceModuleSymbol)
            {
                validateEvent((EventSymbol)e.GetPartialImplementationPart()!);
            }
        }

        void validateEvent(EventSymbol e)
        {
            Assert.False(e.IsAbstract);
            Assert.Equal(expectedVirtual, e.IsVirtual);
            Assert.False(e.IsSealed);
            Assert.True(e.IsStatic);
            Assert.False(e.IsExtern);
            Assert.False(e.IsOverride);
            Assert.Equal(expectedAccessibility, e.DeclaredAccessibility);
            Assert.True(e.ContainingModule is not SourceModuleSymbol || e.IsPartialMember());
            validateAccessor(e.AddMethod!);
            validateAccessor(e.RemoveMethod!);
        }

        void validateAccessor(MethodSymbol m)
        {
            Assert.False(m.IsAbstract);
            Assert.Equal(expectedVirtual, m.IsVirtual);
            Assert.Equal(expectedVirtual, m.IsMetadataVirtual());
            Assert.False(m.IsMetadataNewSlot());
            Assert.False(m.IsSealed);
            Assert.True(m.IsStatic);
            Assert.False(m.IsExtern);
            Assert.False(m.IsOverride);
            Assert.Equal(expectedAccessibility, m.DeclaredAccessibility);
            Assert.True(m.ContainingModule is not SourceModuleSymbol || m.IsPartialMember());
        }
    }

    [Fact]
    public void Abstract()
    {
        var source = """
            abstract partial class C
            {
                protected abstract partial event System.Action E;
                protected abstract partial event System.Action E { add { } remove { } }
                protected abstract partial C();
                protected abstract partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,52): error CS0750: A partial member cannot have the 'abstract' modifier
            //     protected abstract partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberCannotBeAbstract, "E").WithLocation(3, 52),
            // (4,54): error CS8712: 'C.E': abstract event cannot use event accessor syntax
            //     protected abstract partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("C.E").WithLocation(4, 54),
            // (5,32): error CS0106: The modifier 'abstract' is not valid for this item
            //     protected abstract partial C();
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("abstract").WithLocation(5, 32),
            // (6,32): error CS0106: The modifier 'abstract' is not valid for this item
            //     protected abstract partial C() { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("abstract").WithLocation(6, 32));
    }

    [Fact]
    public void Required()
    {
        var source = """
            partial class C
            {
                public required partial event System.Action E;
                public required partial event System.Action E { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,49): error CS0106: The modifier 'required' is not valid for this item
            //     public required partial event System.Action E;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("required").WithLocation(3, 49),
            // (4,49): error CS0106: The modifier 'required' is not valid for this item
            //     public required partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("required").WithLocation(4, 49));
    }

    [Fact]
    public void ExplicitInterfaceImplementation()
    {
        var source = """
            interface I
            {
                event System.Action E;
            }
            partial class C : I
            {
                partial event System.Action I.E;
                partial event System.Action I.E { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,15): error CS8646: 'I.E' is explicitly implemented more than once.
            // partial class C : I
            Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("I.E").WithLocation(5, 15),
            // (7,35): error CS0071: An explicit interface implementation of an event must use event accessor syntax
            //     partial event System.Action I.E;
            Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, "E").WithLocation(7, 35),
            // (7,35): error CS9276: Partial member 'C.I.E' must have a definition part.
            //     partial event System.Action I.E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "E").WithArguments("C.I.E").WithLocation(7, 35),
            // (7,35): error CS0754: A partial member may not explicitly implement an interface member
            //     partial event System.Action I.E;
            Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "E").WithLocation(7, 35),
            // (8,35): error CS9278: Partial member 'C.I.E' may not have multiple implementing declarations.
            //     partial event System.Action I.E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.I.E").WithLocation(8, 35),
            // (8,35): error CS0102: The type 'C' already contains a definition for 'I.E'
            //     partial event System.Action I.E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "I.E").WithLocation(8, 35),
            // (8,35): error CS0754: A partial member may not explicitly implement an interface member
            //     partial event System.Action I.E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "E").WithLocation(8, 35));
    }

    [Fact]
    public void ConstructorInitializers_This_Duplicate()
    {
        var source = """
            partial class C
            {
                partial C() : this(1) { }
                partial C() : this(2);

                C(int x) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,17): error CS9280: 'C.C()': only the implementing declaration of a partial constructor can have an initializer
            //     partial C() : this(2);
            Diagnostic(ErrorCode.ERR_PartialConstructorInitializer, ": this(2)").WithArguments("C.C()").WithLocation(4, 17));
    }

    [Fact]
    public void ConstructorInitializers_This_OnDefinition()
    {
        var source = """
            partial class C
            {
                partial C() { }
                partial C() : this(1);

                C(int x) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,17): error CS9280: 'C.C()': only the implementing declaration of a partial constructor can have an initializer
            //     partial C() : this(1);
            Diagnostic(ErrorCode.ERR_PartialConstructorInitializer, ": this(1)").WithArguments("C.C()").WithLocation(4, 17));
    }

    [Fact]
    public void ConstructorInitializers_This_OnImplementation()
    {
        var source = """
            var c = new C();

            partial class C
            {
                public partial C() : this(1) { }
                public partial C();

                C(int x) { System.Console.Write(x); }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void ConstructorInitializers_Base_Duplicate()
    {
        var source = """
            abstract class B
            {
                protected B(int x) { }
            }

            partial class C : B
            {
                partial C() : base(1) { }
                partial C() : base(2);
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (9,17): error CS9280: 'C.C()': only the implementing declaration of a partial constructor can have an initializer
            //     partial C() : base(2);
            Diagnostic(ErrorCode.ERR_PartialConstructorInitializer, ": base(2)").WithArguments("C.C()").WithLocation(9, 17));
    }

    [Fact]
    public void ConstructorInitializers_Base_OnDefinition_01()
    {
        var source = """
            abstract class B
            {
                protected B(int x) { }
            }

            partial class C : B
            {
                partial C() { }
                partial C() : base(1);
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (8,13): error CS7036: There is no argument given that corresponds to the required parameter 'x' of 'B.B(int)'
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("x", "B.B(int)").WithLocation(8, 13),
            // (9,17): error CS9280: 'C.C()': only the implementing declaration of a partial constructor can have an initializer
            //     partial C() : base(1);
            Diagnostic(ErrorCode.ERR_PartialConstructorInitializer, ": base(1)").WithArguments("C.C()").WithLocation(9, 17));
    }

    [Fact]
    public void ConstructorInitializers_Base_OnDefinition_02()
    {
        var source = """
            abstract class B
            {
                protected B(int x) { }
                protected B() { }
            }

            partial class C : B
            {
                partial C() { }
                partial C() : base(1);
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,17): error CS9280: 'C.C()': only the implementing declaration of a partial constructor can have an initializer
            //     partial C() : base(1);
            Diagnostic(ErrorCode.ERR_PartialConstructorInitializer, ": base(1)").WithArguments("C.C()").WithLocation(10, 17));
    }

    [Fact]
    public void ConstructorInitializers_Base_OnImplementation()
    {
        var source = """
            var c = new C();

            abstract class B
            {
                protected B(int x) { System.Console.Write(x); }
            }

            partial class C : B
            {
                public partial C() : base(1) { }
                public partial C();
            }
            """;
        CompileAndVerify(source, expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void VariableInitializer()
    {
        var source = """
            var c = new C();

            partial class C
            {
                int x = 5;

                public partial C() { System.Console.Write(x); }
                public partial C();
            }
            """;
        CompileAndVerify(source, expectedOutput: "5").VerifyDiagnostics();
    }

    [Fact]
    public void Extern_01()
    {
        var source = """
            partial class C
            {
                partial event System.Action E;
                extern partial event System.Action E;

                partial C();
                extern partial C();
            }
            """;
        CompileAndVerifyWithMscorlib46(source,
            options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
            sourceSymbolValidator: verifySource,
            symbolValidator: verifyMetadata,
            // PEVerify fails when extern methods lack an implementation
            verify: Verification.FailsPEVerify with
            {
                PEVerifyMessage = """
                    Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                    Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                    Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                    Type load failed.
                    """,
            })
            .VerifyDiagnostics()
            .VerifyTypeIL("C", """
                .class private auto ansi beforefieldinit C
                    extends [mscorlib]System.Object
                {
                    // Methods
                    .method private hidebysig specialname 
                        instance void add_E (
                            class [mscorlib]System.Action 'value'
                        ) cil managed 
                    {
                        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                            01 00 00 00
                        )
                    } // end of method C::add_E
                    .method private hidebysig specialname 
                        instance void remove_E (
                            class [mscorlib]System.Action 'value'
                        ) cil managed 
                    {
                        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                            01 00 00 00
                        )
                    } // end of method C::remove_E
                    .method private hidebysig specialname rtspecialname 
                        instance void .ctor () cil managed 
                    {
                    } // end of method C::.ctor
                    // Events
                    .event [mscorlib]System.Action E
                    {
                        .addon instance void C::add_E(class [mscorlib]System.Action)
                        .removeon instance void C::remove_E(class [mscorlib]System.Action)
                    }
                } // end of class C
                """);

        static void verifySource(ModuleSymbol module)
        {
            var ev = module.GlobalNamespace.GetMember<SourceEventSymbol>("C.E");
            Assert.True(ev.IsPartialDefinition);
            Assert.True(ev.GetPublicSymbol().IsExtern);
            Assert.True(ev.AddMethod!.GetPublicSymbol().IsExtern);
            Assert.True(ev.RemoveMethod!.GetPublicSymbol().IsExtern);
            Assert.True(ev.PartialImplementationPart!.GetPublicSymbol().IsExtern);
            Assert.True(ev.PartialImplementationPart!.AddMethod!.GetPublicSymbol().IsExtern);
            Assert.True(ev.PartialImplementationPart!.RemoveMethod!.GetPublicSymbol().IsExtern);

            var c = module.GlobalNamespace.GetMember<SourceConstructorSymbol>("C..ctor");
            Assert.True(c.IsPartialDefinition);
            Assert.True(c.GetPublicSymbol().IsExtern);
            Assert.True(c.PartialImplementationPart!.GetPublicSymbol().IsExtern);

            var members = module.GlobalNamespace.GetTypeMember("C").GetMembers().Select(s => s.ToTestDisplayString()).Join("\n");
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                void C.E.add
                void C.E.remove
                event System.Action C.E
                C..ctor()
                """, members);
        }

        static void verifyMetadata(ModuleSymbol module)
        {
            // IsExtern doesn't round trip from metadata when DllImportAttribute is missing.
            // This is consistent with the behavior of partial methods and properties.

            var ev = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            Assert.False(ev.GetPublicSymbol().IsExtern);
            Assert.False(ev.AddMethod!.GetPublicSymbol().IsExtern);
            Assert.False(ev.RemoveMethod!.GetPublicSymbol().IsExtern);

            var c = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
            Assert.False(c.GetPublicSymbol().IsExtern);

            var members = module.GlobalNamespace.GetTypeMember("C").GetMembers().Select(s => s.ToTestDisplayString()).Join("\n");
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                void C.E.add
                void C.E.remove
                C..ctor()
                event System.Action C.E
                """, members);
        }
    }

    [Fact]
    public void Extern_02()
    {
        var source = """
            partial class C
            {
                partial event System.Action E;
                extern event System.Action E;

                partial C();
                extern C();
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,33): error CS9275: Partial member 'C.E' must have an implementation part.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "E").WithArguments("C.E").WithLocation(3, 33),
            // (4,32): error CS0102: The type 'C' already contains a definition for 'E'
            //     extern event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(4, 32),
            // (4,32): warning CS0626: Method, operator, or accessor 'C.E.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     extern event System.Action E;
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "E").WithArguments("C.E.remove").WithLocation(4, 32),
            // (6,13): error CS9275: Partial member 'C.C()' must have an implementation part.
            //     partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C").WithArguments("C.C()").WithLocation(6, 13),
            // (7,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     extern C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(7, 12),
            // (7,12): warning CS0824: Constructor 'C.C()' is marked external
            //     extern C();
            Diagnostic(ErrorCode.WRN_ExternCtorNoImplementation, "C").WithArguments("C.C()").WithLocation(7, 12));
    }

    [Fact]
    public void Extern_03()
    {
        var source = """
            partial class C
            {
                extern partial event System.Action E;
                partial event System.Action E { add { } remove { } }

                extern partial C();
                partial C() { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,40): error CS9276: Partial member 'C.E' must have a definition part.
            //     extern partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "E").WithArguments("C.E").WithLocation(3, 40),
            // (4,33): error CS9278: Partial member 'C.E' may not have multiple implementing declarations.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.E").WithLocation(4, 33),
            // (4,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(4, 33),
            // (6,20): error CS9276: Partial member 'C.C()' must have a definition part.
            //     extern partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C").WithArguments("C.C()").WithLocation(6, 20),
            // (7,13): error CS9278: Partial member 'C.C()' may not have multiple implementing declarations.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "C").WithArguments("C.C()").WithLocation(7, 13),
            // (7,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(7, 13));
    }

    [Fact]
    public void Extern_DllImport()
    {
        var source = """
            using System;
            using System.Runtime.InteropServices;
            public partial class C
            {
                public static partial event Action E;
                [method: DllImport("something.dll")]
                public static extern partial event Action E;
            }
            """;
        CompileAndVerify(source,
            sourceSymbolValidator: verify,
            symbolValidator: verify)
            .VerifyDiagnostics();

        static void verify(ModuleSymbol module)
        {
            var e = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            Assert.True(e.GetPublicSymbol().IsExtern);
            // unexpected mismatch between metadata and entrypoint name: https://github.com/dotnet/roslyn/issues/76882
            verifyAccessor(e.AddMethod!, "add_E", "remove_E");
            verifyAccessor(e.RemoveMethod!, "remove_E", "remove_E");

            if (module is SourceModuleSymbol)
            {
                var eImpl = ((SourceEventSymbol)e).PartialImplementationPart!;
                Assert.True(eImpl.GetPublicSymbol().IsExtern);
                // unexpected mismatch between metadata and entrypoint name: https://github.com/dotnet/roslyn/issues/76882
                verifyAccessor(eImpl.AddMethod!, "add_E", "remove_E");
                verifyAccessor(eImpl.RemoveMethod!, "remove_E", "remove_E");
            }
        }

        static void verifyAccessor(MethodSymbol accessor, string expectedMetadataName, string expectedEntryPointName)
        {
            Assert.True(accessor.GetPublicSymbol().IsExtern);
            Assert.Equal(expectedMetadataName, accessor.MetadataName);
            Assert.False(accessor.ImplementationAttributes.HasFlag(MethodImplAttributes.Synchronized));

            var importData = accessor.GetDllImportData()!;
            Assert.Equal("something.dll", importData.ModuleName);
            Assert.Equal(expectedEntryPointName, importData.EntryPointName);
            Assert.Equal(CharSet.None, importData.CharacterSet);
            Assert.False(importData.SetLastError);
            Assert.False(importData.ExactSpelling);
            Assert.Equal(MethodImplAttributes.PreserveSig, accessor.ImplementationAttributes);
            Assert.Equal(CallingConvention.Winapi, importData.CallingConvention);
            Assert.Null(importData.BestFitMapping);
            Assert.Null(importData.ThrowOnUnmappableCharacter);
        }
    }

    [Fact]
    public void Extern_InternalCall()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;
            public partial class C
            {
                public partial C();
                [MethodImpl(MethodImplOptions.InternalCall)]
                public extern partial C();
            
                public partial event Action E;
                [method: MethodImpl(MethodImplOptions.InternalCall)]
                public extern partial event Action E;
            }
            """;
        CompileAndVerify(source,
            sourceSymbolValidator: verifySource,
            symbolValidator: verifyMetadata)
            .VerifyDiagnostics();

        static void verifySource(ModuleSymbol module)
        {
            var ev = module.GlobalNamespace.GetMember<SourceEventSymbol>("C.E");
            Assert.True(ev.GetPublicSymbol().IsExtern);
            Assert.True(ev.AddMethod!.GetPublicSymbol().IsExtern);
            Assert.Null(ev.AddMethod!.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, ev.AddMethod.ImplementationAttributes);
            Assert.False(ev.AddMethod.ImplementationAttributes.HasFlag(MethodImplAttributes.Synchronized));
            Assert.True(ev.RemoveMethod!.GetPublicSymbol().IsExtern);
            Assert.Null(ev.RemoveMethod!.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, ev.RemoveMethod.ImplementationAttributes);
            Assert.False(ev.RemoveMethod.ImplementationAttributes.HasFlag(MethodImplAttributes.Synchronized));

            var c = module.GlobalNamespace.GetMember<SourceConstructorSymbol>("C..ctor");
            Assert.True(c.GetPublicSymbol().IsExtern);
            Assert.Null(c.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, c.ImplementationAttributes);
        }

        static void verifyMetadata(ModuleSymbol module)
        {
            var ev = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            Assert.False(ev.GetPublicSymbol().IsExtern);
            Assert.False(ev.AddMethod!.GetPublicSymbol().IsExtern);
            Assert.Null(ev.AddMethod!.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, ev.AddMethod.ImplementationAttributes);
            Assert.False(ev.AddMethod.ImplementationAttributes.HasFlag(MethodImplAttributes.Synchronized));
            Assert.False(ev.RemoveMethod!.GetPublicSymbol().IsExtern);
            Assert.Null(ev.RemoveMethod!.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, ev.RemoveMethod.ImplementationAttributes);
            Assert.False(ev.RemoveMethod.ImplementationAttributes.HasFlag(MethodImplAttributes.Synchronized));

            var c = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
            Assert.False(c.GetPublicSymbol().IsExtern);
            Assert.Null(c.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, c.ImplementationAttributes);
        }
    }

    [Fact]
    public void WinRtEvent()
    {
        var source = """
            partial class C
            {
                public partial event System.Action E;
                public partial event System.Action E { add { return default; } remove { } }
            }
            """;
        CompileAndVerifyWithWinRt(source,
            sourceSymbolValidator: validate,
            symbolValidator: validate,
            options: TestOptions.ReleaseWinMD)
            .VerifyDiagnostics()
            .VerifyTypeIL("C", """
                .class private auto ansi beforefieldinit C
                    extends [mscorlib]System.Object
                {
                    // Methods
                    .method public hidebysig specialname 
                        instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken add_E (
                            class [mscorlib]System.Action 'value'
                        ) cil managed 
                    {
                        // Method begins at RVA 0x2068
                        // Code size 10 (0xa)
                        .maxstack 1
                        .locals init (
                            [0] valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
                        )
                        IL_0000: ldloca.s 0
                        IL_0002: initobj [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
                        IL_0008: ldloc.0
                        IL_0009: ret
                    } // end of method C::add_E
                    .method public hidebysig specialname 
                        instance void remove_E (
                            valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value'
                        ) cil managed 
                    {
                        // Method begins at RVA 0x207e
                        // Code size 1 (0x1)
                        .maxstack 8
                        IL_0000: ret
                    } // end of method C::remove_E
                    .method public hidebysig specialname rtspecialname 
                        instance void .ctor () cil managed 
                    {
                        // Method begins at RVA 0x2080
                        // Code size 7 (0x7)
                        .maxstack 8
                        IL_0000: ldarg.0
                        IL_0001: call instance void [mscorlib]System.Object::.ctor()
                        IL_0006: ret
                    } // end of method C::.ctor
                    // Events
                    .event [mscorlib]System.Action E
                    {
                        .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C::add_E(class [mscorlib]System.Action)
                        .removeon instance void C::remove_E(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
                    }
                } // end of class C
                """);

        static void validate(ModuleSymbol module)
        {
            var e = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            Assert.True(e.IsWindowsRuntimeEvent);

            if (module is SourceModuleSymbol)
            {
                Assert.True(((SourceEventSymbol)e).PartialImplementationPart!.IsWindowsRuntimeEvent);
            }
        }
    }

    [Fact]
    public void Extern_MissingCompareExchange()
    {
        var source = """
            using System;
            partial class C
            {
                partial event Action E;
                extern partial event Action E;
            }
            """;
        var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
        comp.MakeMemberMissing(WellKnownMember.System_Threading_Interlocked__CompareExchange_T);
        CompileAndVerify(comp,
            sourceSymbolValidator: validate,
            symbolValidator: validate,
            // PEVerify fails when extern methods lack an implementation
            verify: Verification.FailsPEVerify with
            {
                PEVerifyMessage = """
                    Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                    Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                    Type load failed.
                    """,
            })
            .VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            var e = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            Assert.True(e.AddMethod!.ImplementationAttributes.HasFlag(MethodImplAttributes.Synchronized));
            Assert.True(e.RemoveMethod!.ImplementationAttributes.HasFlag(MethodImplAttributes.Synchronized));
        }
    }

    [Fact]
    public void Metadata()
    {
        var source = """
            public partial class C
            {
                public partial event System.Action E;
                public partial event System.Action E { add { } remove { } }
                public partial C();
                public partial C() { }
            }
            """;
        CompileAndVerify(source,
            sourceSymbolValidator: verifySource,
            symbolValidator: verifyMetadata)
            .VerifyDiagnostics();

        static void verifySource(ModuleSymbol module)
        {
            var e = module.GlobalNamespace.GetMember<SourceEventSymbol>("C.E");
            Assert.True(e.IsPartialDefinition);
            Assert.False(e.IsPartialImplementation);
            Assert.False(e.HasAssociatedField);
            Assert.False(e.IsWindowsRuntimeEvent);
            Assert.Null(e.PartialDefinitionPart);
            Assert.True(e.SourcePartialImplementationPart!.IsPartialImplementation);
            Assert.False(e.SourcePartialImplementationPart.IsPartialDefinition);
            Assert.False(e.SourcePartialImplementationPart.HasAssociatedField);
            Assert.False(e.SourcePartialImplementationPart.IsWindowsRuntimeEvent);

            var addMethod = e.AddMethod!;
            Assert.Equal("add_E", addMethod.Name);
            Assert.NotSame(addMethod, e.SourcePartialImplementationPart.AddMethod);
            Assert.Same(e, addMethod.AssociatedSymbol);
            Assert.Same(e.PartialImplementationPart, addMethod.PartialImplementationPart.AssociatedSymbol);
            var removeMethod = e.RemoveMethod!;
            Assert.Equal("remove_E", removeMethod.Name);
            Assert.NotSame(removeMethod, e.SourcePartialImplementationPart.RemoveMethod);
            Assert.Same(e, removeMethod.AssociatedSymbol);
            Assert.Same(e.PartialImplementationPart, removeMethod.PartialImplementationPart.AssociatedSymbol);

            var c = module.GlobalNamespace.GetMember<SourceConstructorSymbol>("C..ctor");
            Assert.True(c.IsPartialDefinition);
            Assert.False(c.IsPartialImplementation);
            Assert.Null(c.PartialDefinitionPart);
            var cImpl = (SourceConstructorSymbol)c.PartialImplementationPart!;
            Assert.True(cImpl.IsPartialImplementation);
            Assert.False(cImpl.IsPartialDefinition);
        }

        static void verifyMetadata(ModuleSymbol module)
        {
            var e = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            Assert.False(e.HasAssociatedField);

            var addMethod = e.AddMethod!;
            Assert.Equal("add_E", addMethod.Name);
            var removeMethod = e.RemoveMethod!;
            Assert.Equal("remove_E", removeMethod.Name);
        }
    }

    [Fact]
    public void GetDeclaredSymbol()
    {
        var source = ("""
            partial class C
            {
                public partial event System.Action E, F;
                public partial event System.Action E { add { } remove { } }
                public partial event System.Action F { add { } remove { } }

                public partial C();
                public partial C() { }
            }
            """, "Program.cs");

        var comp = CreateCompilation(source).VerifyDiagnostics();
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var eventDefs = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToImmutableArray();
        Assert.Equal(2, eventDefs.Length);

        var eventImpls = tree.GetRoot().DescendantNodes().OfType<EventDeclarationSyntax>().ToImmutableArray();
        Assert.Equal(2, eventImpls.Length);

        {
            var defSymbol = (IEventSymbol)model.GetDeclaredSymbol(eventDefs[0])!;
            Assert.Equal("event System.Action C.E", defSymbol.ToTestDisplayString());

            IEventSymbol implSymbol = model.GetDeclaredSymbol(eventImpls[0])!;
            Assert.Equal("event System.Action C.E", implSymbol.ToTestDisplayString());

            Assert.NotEqual(defSymbol, implSymbol);
            Assert.Same(implSymbol, defSymbol.PartialImplementationPart);
            Assert.Same(defSymbol, implSymbol.PartialDefinitionPart);
            Assert.Null(implSymbol.PartialImplementationPart);
            Assert.Null(defSymbol.PartialDefinitionPart);
            Assert.True(defSymbol.IsPartialDefinition);
            Assert.False(implSymbol.IsPartialDefinition);

            Assert.NotEqual(defSymbol.Locations.Single(), implSymbol.Locations.Single());
        }

        {
            var defSymbol = (IEventSymbol)model.GetDeclaredSymbol(eventDefs[1])!;
            Assert.Equal("event System.Action C.F", defSymbol.ToTestDisplayString());

            IEventSymbol implSymbol = model.GetDeclaredSymbol(eventImpls[1])!;
            Assert.Equal("event System.Action C.F", implSymbol.ToTestDisplayString());

            Assert.NotEqual(defSymbol, implSymbol);
            Assert.Same(implSymbol, defSymbol.PartialImplementationPart);
            Assert.Same(defSymbol, implSymbol.PartialDefinitionPart);
            Assert.Null(implSymbol.PartialImplementationPart);
            Assert.Null(defSymbol.PartialDefinitionPart);
            Assert.True(defSymbol.IsPartialDefinition);
            Assert.False(implSymbol.IsPartialDefinition);

            Assert.NotEqual(defSymbol.Locations.Single(), implSymbol.Locations.Single());
        }

        {
            var ctors = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToImmutableArray();
            Assert.Equal(2, ctors.Length);

            IMethodSymbol defSymbol = model.GetDeclaredSymbol(ctors[0])!;
            Assert.Equal("C..ctor()", defSymbol.ToTestDisplayString());

            IMethodSymbol implSymbol = model.GetDeclaredSymbol(ctors[1])!;
            Assert.Equal("C..ctor()", implSymbol.ToTestDisplayString());

            Assert.NotEqual(defSymbol, implSymbol);
            Assert.Same(implSymbol, defSymbol.PartialImplementationPart);
            Assert.Same(defSymbol, implSymbol.PartialDefinitionPart);
            Assert.Null(implSymbol.PartialImplementationPart);
            Assert.Null(defSymbol.PartialDefinitionPart);
            Assert.True(defSymbol.IsPartialDefinition);
            Assert.False(implSymbol.IsPartialDefinition);

            Assert.NotEqual(defSymbol.Locations.Single(), implSymbol.Locations.Single());
        }
    }

    [Fact]
    public void GetDeclaredSymbol_GenericContainer()
    {
        var source = ("""
            partial class C<T>
            {
                public partial event System.Action E;
                public partial event System.Action E { add { } remove { } }

                public partial C();
                public partial C() { }
            }
            """, "Program.cs");

        var comp = CreateCompilation(source).VerifyDiagnostics();
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        {
            var defSymbol = (IEventSymbol)model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single())!;
            Assert.Equal("event System.Action C<T>.E", defSymbol.ToTestDisplayString());

            IEventSymbol implSymbol = model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<EventDeclarationSyntax>().Single())!;
            Assert.Equal("event System.Action C<T>.E", implSymbol.ToTestDisplayString());

            Assert.NotEqual(defSymbol, implSymbol);
            Assert.Same(implSymbol, defSymbol.PartialImplementationPart);
            Assert.Same(defSymbol, implSymbol.PartialDefinitionPart);
            Assert.Null(implSymbol.PartialImplementationPart);
            Assert.Null(defSymbol.PartialDefinitionPart);
            Assert.True(defSymbol.IsPartialDefinition);
            Assert.False(implSymbol.IsPartialDefinition);

            Assert.NotEqual(defSymbol.Locations.Single(), implSymbol.Locations.Single());

            var intSymbol = comp.GetSpecialType(SpecialType.System_Int32);
            var cOfTSymbol = defSymbol.ContainingType!;
            var cOfIntSymbol = cOfTSymbol.Construct([intSymbol]);

            var defOfIntSymbol = (IEventSymbol)cOfIntSymbol.GetMember("E");
            Assert.Equal("event System.Action C<System.Int32>.E", defOfIntSymbol.ToTestDisplayString());
            Assert.Null(defOfIntSymbol.PartialImplementationPart);
            Assert.Null(defOfIntSymbol.PartialDefinitionPart);
            Assert.False(defOfIntSymbol.IsPartialDefinition);
        }

        {
            var ctors = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToImmutableArray();
            Assert.Equal(2, ctors.Length);

            IMethodSymbol defSymbol = model.GetDeclaredSymbol(ctors[0])!;
            Assert.Equal("C<T>..ctor()", defSymbol.ToTestDisplayString());

            IMethodSymbol implSymbol = model.GetDeclaredSymbol(ctors[1])!;
            Assert.Equal("C<T>..ctor()", implSymbol.ToTestDisplayString());

            Assert.NotEqual(defSymbol, implSymbol);
            Assert.Same(implSymbol, defSymbol.PartialImplementationPart);
            Assert.Same(defSymbol, implSymbol.PartialDefinitionPart);
            Assert.Null(implSymbol.PartialImplementationPart);
            Assert.Null(defSymbol.PartialDefinitionPart);
            Assert.True(defSymbol.IsPartialDefinition);
            Assert.False(implSymbol.IsPartialDefinition);

            Assert.NotEqual(defSymbol.Locations.Single(), implSymbol.Locations.Single());

            var intSymbol = comp.GetSpecialType(SpecialType.System_Int32);
            var cOfTSymbol = defSymbol.ContainingType!;
            var cOfIntSymbol = cOfTSymbol.Construct([intSymbol]);

            var defOfIntSymbol = (IMethodSymbol)cOfIntSymbol.GetMember(".ctor");
            Assert.Equal("C<System.Int32>..ctor()", defOfIntSymbol.ToTestDisplayString());
            Assert.Null(defOfIntSymbol.PartialImplementationPart);
            Assert.Null(defOfIntSymbol.PartialDefinitionPart);
            Assert.False(defOfIntSymbol.IsPartialDefinition);
        }
    }

    [Fact]
    public void GetDeclaredSymbol_ConstructorParameter()
    {
        var source = ("""
            partial class C
            {
                public partial C(int i);
                public partial C(int i) { }
            }
            """, "Program.cs");

        var comp = CreateCompilation(source).VerifyDiagnostics();
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var parameters = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().ToArray();
        Assert.Equal(2, parameters.Length);

        IParameterSymbol defSymbol = model.GetDeclaredSymbol(parameters[0])!;
        Assert.Equal("System.Int32 i", defSymbol.ToTestDisplayString());

        IParameterSymbol implSymbol = model.GetDeclaredSymbol(parameters[1])!;
        Assert.Equal("System.Int32 i", implSymbol.ToTestDisplayString());

        Assert.NotEqual(defSymbol, implSymbol);
        Assert.Same(implSymbol, ((IMethodSymbol)defSymbol.ContainingSymbol).PartialImplementationPart!.Parameters.Single());
        Assert.Same(defSymbol, ((IMethodSymbol)implSymbol.ContainingSymbol).PartialDefinitionPart!.Parameters.Single());

        Assert.NotEqual(defSymbol.Locations.Single(), implSymbol.Locations.Single());
    }

    [Fact]
    public void SequencePoints()
    {
        var source = """
            partial class C
            {
                partial C(int i);
                partial C(int i)
                {
                    System.Console.Write(i);
                }
                partial event System.Action E;
                partial event System.Action E
                {
                    add
                    {
                        System.Console.Write(value);
                    }
                    remove
                    {
                        value();
                    }
                }
            }
            """;
        CompileAndVerify(source)
            .VerifyDiagnostics()
            .VerifyMethodBody("C..ctor", """
                {
                  // Code size       13 (0xd)
                  .maxstack  1
                  // sequence point: partial C(int i)
                  IL_0000:  ldarg.0
                  IL_0001:  call       "object..ctor()"
                  // sequence point: System.Console.Write(i);
                  IL_0006:  ldarg.1
                  IL_0007:  call       "void System.Console.Write(int)"
                  // sequence point: }
                  IL_000c:  ret
                }
                """)
            .VerifyMethodBody("C.E.add", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  // sequence point: System.Console.Write(value);
                  IL_0000:  ldarg.1
                  IL_0001:  call       "void System.Console.Write(object)"
                  // sequence point: }
                  IL_0006:  ret
                }
                """)
            .VerifyMethodBody("C.E.remove", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  // sequence point: value();
                  IL_0000:  ldarg.1
                  IL_0001:  callvirt   "void System.Action.Invoke()"
                  // sequence point: }
                  IL_0006:  ret
                }
                """);
    }

    [Fact]
    public void EmitOrder_01()
    {
        verify("""
            partial class C
            {
                partial event System.Action E;
                partial event System.Action E { add { } remove { } }
                partial C();
                partial C() { }
            }
            """);

        verify("""
            partial class C
            {
                partial event System.Action E { add { } remove { } }
                partial event System.Action E;
                partial C() { }
                partial C();
            }
            """);

        verify("""
            partial class C
            {
                partial event System.Action E { add { } remove { } }
                partial C() { }
            }
            """, """
            partial class C
            {
                partial event System.Action E;
                partial C();
            }
            """);

        verify("""
            partial class C
            {
                partial event System.Action E;
                partial C();
            }
            """, """
            partial class C
            {
                partial event System.Action E { add { } remove { } }
                partial C() { }
            }
            """);

        void verify(params CSharpTestSource sources)
        {
            CompileAndVerify(sources,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: validate)
                .VerifyDiagnostics();
        }

        static void validate(ModuleSymbol module)
        {
            var members = module.GlobalNamespace.GetTypeMember("C").GetMembers().Select(s => s.ToTestDisplayString()).Join("\n");
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                void C.E.add
                void C.E.remove
                C..ctor()
                event System.Action C.E
                """, members);
        }
    }

    [Fact]
    public void EmitOrder_02()
    {
        verify("""
            partial class C
            {
                partial C();
                partial C() { }
                partial event System.Action E;
                partial event System.Action E { add { } remove { } }
            }
            """);

        verify("""
            partial class C
            {
                partial C() { }
                partial C();
                partial event System.Action E { add { } remove { } }
                partial event System.Action E;
            }
            """);

        verify("""
            partial class C
            {
                partial C();
                partial event System.Action E;
            }
            """, """
            partial class C
            {
                partial C() { }
                partial event System.Action E { add { } remove { } }
            }
            """);

        verify("""
            partial class C
            {
                partial C() { }
                partial event System.Action E { add { } remove { } }
            }
            """, """
            partial class C
            {
                partial C();
                partial event System.Action E;
            }
            """);

        void verify(params CSharpTestSource sources)
        {
            CompileAndVerify(sources,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: validate)
                .VerifyDiagnostics();
        }

        static void validate(ModuleSymbol module)
        {
            var members = module.GlobalNamespace.GetTypeMember("C").GetMembers().Select(s => s.ToTestDisplayString()).Join("\n");
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                C..ctor()
                void C.E.add
                void C.E.remove
                event System.Action C.E
                """, members);
        }
    }

    [Fact]
    public void Use_Valid()
    {
        var source = """
            using System;

            var c = new C();
            c.E += () => Console.Write(1);
            c.E -= () => Console.Write(2);

            partial class C
            {
                public partial event Action E;
                public partial event Action E
                {
                    add { Console.Write(3); value(); }
                    remove { Console.Write(4); value(); }
                }
                public partial C();
                public partial C() { Console.Write(5); }
            }
            """;
        CompileAndVerify(source, expectedOutput: "53142").VerifyDiagnostics();
    }

    [Fact]
    public void Use_EventAsValue()
    {
        var source = """
            using System;

            var c = new C();
            Action a = c.E;
            c.E();

            partial class C
            {
                public partial event Action E;
                public partial event Action E { add { } remove { } }

                void M()
                {
                    Action a = this.E;
                    this.E();
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,14): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
            // Action a = c.E;
            Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(4, 14),
            // (5,3): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
            // c.E();
            Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(5, 3),
            // (14,25): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
            //         Action a = this.E;
            Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(14, 25),
            // (15,14): error CS0079: The event 'C.E' can only appear on the left hand side of += or -=
            //         this.E();
            Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("C.E").WithLocation(15, 14));
    }

    [Fact]
    public void Use_EventAccessorsInaccessible()
    {
        var source = """
            using System;

            var c = new C();
            c.E += () => { };
            c.E -= () => { };

            partial class C
            {
                partial event Action E;
                partial event Action E { add { } remove { } }

                void M()
                {
                    this.E += () => { };
                    this.E -= () => { };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,3): error CS0122: 'C.E' is inaccessible due to its protection level
            // c.E += () => { };
            Diagnostic(ErrorCode.ERR_BadAccess, "E").WithArguments("C.E").WithLocation(4, 3),
            // (5,3): error CS0122: 'C.E' is inaccessible due to its protection level
            // c.E -= () => { };
            Diagnostic(ErrorCode.ERR_BadAccess, "E").WithArguments("C.E").WithLocation(5, 3));
    }

    [Fact]
    public void Use_Static()
    {
        var source = """
            var c = new C();
            c.E += () => { };
            C.E += () => { }; // 1
            c.F += () => { }; // 2
            C.F += () => { };

            partial class C
            {
                public partial event System.Action E;
                public static partial event System.Action F;
            }
            partial class C
            {
                public partial event System.Action E
                {
                    add
                    {
                        this.E += null;
                        E += null;
                        C.E += null; // 3
                    }
                    remove
                    {
                        this.F += null; // 4
                        F += null;
                        C.F += null;
                    }
                }
                public static partial event System.Action F
                {
                    add
                    {
                        this.E += null; // 5
                        E += null; // 6
                        C.E += null; // 7
                    }
                    remove
                    {
                        this.F += null; // 8
                        F += null;
                        C.F += null;
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,1): error CS0120: An object reference is required for the non-static field, method, or property 'C.E'
            // C.E += () => { }; // 1
            Diagnostic(ErrorCode.ERR_ObjectRequired, "C.E").WithArguments("C.E").WithLocation(3, 1),
            // (4,1): error CS0176: Member 'C.F' cannot be accessed with an instance reference; qualify it with a type name instead
            // c.F += () => { }; // 2
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.F").WithArguments("C.F").WithLocation(4, 1),
            // (20,13): error CS0120: An object reference is required for the non-static field, method, or property 'C.E'
            //             C.E += null; // 3
            Diagnostic(ErrorCode.ERR_ObjectRequired, "C.E").WithArguments("C.E").WithLocation(20, 13),
            // (24,13): error CS0176: Member 'C.F' cannot be accessed with an instance reference; qualify it with a type name instead
            //             this.F += null; // 4
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.F").WithArguments("C.F").WithLocation(24, 13),
            // (33,13): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
            //             this.E += null; // 5
            Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this").WithLocation(33, 13),
            // (34,13): error CS0120: An object reference is required for the non-static field, method, or property 'C.E'
            //             E += null; // 6
            Diagnostic(ErrorCode.ERR_ObjectRequired, "E").WithArguments("C.E").WithLocation(34, 13),
            // (35,13): error CS0120: An object reference is required for the non-static field, method, or property 'C.E'
            //             C.E += null; // 7
            Diagnostic(ErrorCode.ERR_ObjectRequired, "C.E").WithArguments("C.E").WithLocation(35, 13),
            // (39,13): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
            //             this.F += null; // 8
            Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this").WithLocation(39, 13),
            // (39,13): error CS0176: Member 'C.F' cannot be accessed with an instance reference; qualify it with a type name instead
            //             this.F += null; // 8
            Diagnostic(ErrorCode.ERR_ObjectProhibited, "this.F").WithArguments("C.F").WithLocation(39, 13));
    }

    [Fact]
    public void Use_Inheritance()
    {
        var source = """
            using System;

            var c = new C1();
            c.E += () => { };
            c = new C2();
            c.E += () => { };

            partial class C1
            {
                public virtual partial event Action E;
                public virtual partial event Action E { add { Console.Write(1); } remove { } }
            }
            partial class C2 : C1
            {
                public override partial event Action E;
                public override partial event Action E { add { Console.Write(2); } remove { } }
            }
            """;
        CompileAndVerify(source, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void Difference_Accessibility()
    {
        var source = """
            partial class C
            {
                partial C();
                internal partial C(int x);
                partial event System.Action E, F;
                public partial event System.Action G;
            }
            partial class C
            {
                public partial C() { }
                private partial C(int x) { }
                private partial event System.Action E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
                internal partial event System.Action G { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,20): error CS8799: Both partial member declarations must have identical accessibility modifiers.
            //     public partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "C").WithLocation(10, 20),
            // (11,21): error CS8799: Both partial member declarations must have identical accessibility modifiers.
            //     private partial C(int x) { }
            Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "C").WithLocation(11, 21),
            // (12,41): error CS8799: Both partial member declarations must have identical accessibility modifiers.
            //     private partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "E").WithLocation(12, 41),
            // (14,42): error CS8799: Both partial member declarations must have identical accessibility modifiers.
            //     internal partial event System.Action G { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "G").WithLocation(14, 42));
    }

    [Fact]
    public void Difference_Type()
    {
        var source = """
            using A = System.Action;
            partial class C
            {
                partial event System.Action E, F;
                partial event System.Action<(int X, int Y)> G;
                partial event System.Action<(string X, string Y)> H;
                partial event System.Action<dynamic> I;
                partial event A J;
            }
            partial class C
            {
                partial event System.Func<int> E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
                partial event System.Action<(int A, int B)> G { add { } remove { } }
                partial event System.Action<(int A, int B)> H { add { } remove { } }
                partial event System.Action<object> I { add { } remove { } }
                partial event System.Action J { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (12,36): error CS9255: Both partial member declarations must have the same type.
            //     partial event System.Func<int> E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberTypeDifference, "E").WithLocation(12, 36),
            // (14,49): error CS8142: Both partial member declarations, 'C.G' and 'C.G', must use the same tuple element names.
            //     partial event System.Action<(int A, int B)> G { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "G").WithArguments("C.G", "C.G").WithLocation(14, 49),
            // (15,49): error CS9255: Both partial member declarations must have the same type.
            //     partial event System.Action<(int A, int B)> H { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberTypeDifference, "H").WithLocation(15, 49),
            // (16,41): warning CS9256: Partial member declarations 'event Action<dynamic> C.I' and 'event Action<object> C.I' have signature differences.
            //     partial event System.Action<object> I { add { } remove { } }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "I").WithArguments("event Action<dynamic> C.I", "event Action<object> C.I").WithLocation(16, 41));
    }

    [Fact]
    public void Difference_ParameterType()
    {
        var source = """
            partial class C1
            {
                partial C1(string x);
                partial C1(int x) { }
            }
            partial class C2
            {
                partial C2(dynamic x);
                partial C2(object x) { }
            }
            partial class C3
            {
                partial C3((int X, int Y) x);
                partial C3((int A, int B) x) { }
            }
            partial class C4
            {
                partial C4((int X, int Y) x);
                partial C4((string A, string B) x) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS9275: Partial member 'C1.C1(string)' must have an implementation part.
            //     partial C1(string x);
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C1").WithArguments("C1.C1(string)").WithLocation(3, 13),
            // (4,13): error CS9276: Partial member 'C1.C1(int)' must have a definition part.
            //     partial C1(int x) { }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C1").WithArguments("C1.C1(int)").WithLocation(4, 13),
            // (9,13): warning CS9256: Partial member declarations 'C2.C2(dynamic x)' and 'C2.C2(object x)' have signature differences.
            //     partial C2(object x) { }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C2").WithArguments("C2.C2(dynamic x)", "C2.C2(object x)").WithLocation(9, 13),
            // (14,13): error CS8142: Both partial member declarations, 'C3.C3((int X, int Y))' and 'C3.C3((int A, int B))', must use the same tuple element names.
            //     partial C3((int A, int B) x) { }
            Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "C3").WithArguments("C3.C3((int X, int Y))", "C3.C3((int A, int B))").WithLocation(14, 13),
            // (18,13): error CS9275: Partial member 'C4.C4((int X, int Y))' must have an implementation part.
            //     partial C4((int X, int Y) x);
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C4").WithArguments("C4.C4((int X, int Y))").WithLocation(18, 13),
            // (19,13): error CS9276: Partial member 'C4.C4((string A, string B))' must have a definition part.
            //     partial C4((string A, string B) x) { }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C4").WithArguments("C4.C4((string A, string B))").WithLocation(19, 13));
    }

    [Fact]
    public void Difference_Nullability()
    {
        var source = """
            partial class C
            {
                partial event System.Action? E;
                partial event System.Action<string?> F;
                partial event System.Action G;
            }
            partial class C
            {
                partial event System.Action E { add { } remove { } }
                partial event System.Action<string> F { add { } remove { } }
                partial event System.Action? G { add { } remove { } }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (9,33): warning CS9256: Partial member declarations 'event Action? C.E' and 'event Action C.E' have signature differences.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "E").WithArguments("event Action? C.E", "event Action C.E").WithLocation(9, 33),
            // (10,41): warning CS9256: Partial member declarations 'event Action<string?> C.F' and 'event Action<string> C.F' have signature differences.
            //     partial event System.Action<string> F { add { } remove { } }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "F").WithArguments("event Action<string?> C.F", "event Action<string> C.F").WithLocation(10, 41),
            // (11,34): warning CS9256: Partial member declarations 'event Action C.G' and 'event Action? C.G' have signature differences.
            //     partial event System.Action? G { add { } remove { } }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "G").WithArguments("event Action C.G", "event Action? C.G").WithLocation(11, 34)
        };

        CreateCompilation(source, options: TestOptions.DebugDll.WithNullableContextOptions(NullableContextOptions.Enable)).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.DebugDll.WithNullableContextOptions(NullableContextOptions.Annotations)).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Difference_ParameterNullability()
    {
        var source = """
            partial class C1
            {
                partial C1(string x);
                partial C1(string? x) { }
            }
            partial class C2
            {
                partial C2(string? x);
                partial C2(string x) { }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (4,13): warning CS9256: Partial member declarations 'C1.C1(string x)' and 'C1.C1(string? x)' have signature differences.
            //     partial C1(string? x) { }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C1").WithArguments("C1.C1(string x)", "C1.C1(string? x)").WithLocation(4, 13),
            // (9,13): warning CS9256: Partial member declarations 'C2.C2(string? x)' and 'C2.C2(string x)' have signature differences.
            //     partial C2(string x) { }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C2").WithArguments("C2.C2(string? x)", "C2.C2(string x)").WithLocation(9, 13)
        };

        CreateCompilation(source, options: TestOptions.DebugDll.WithNullableContextOptions(NullableContextOptions.Enable)).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.DebugDll.WithNullableContextOptions(NullableContextOptions.Annotations)).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Difference_NullabilityContext()
    {
        var source = """
            #nullable enable
            partial class C
            {
                partial event System.Action E;
                partial event System.Action? F;
            #nullable disable
                partial event System.Action G;
            }

            #nullable disable
            partial class C
            {
                partial event System.Action E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
            #nullable enable
                partial event System.Action G { add { } remove { } }
            }
            """;

        var comp = CreateCompilation(source).VerifyDiagnostics();

        var e = comp.GetMember<SourceEventSymbol>("C.E");
        Assert.True(e.IsPartialDefinition);
        Assert.Equal(NullableAnnotation.NotAnnotated, e.TypeWithAnnotations.NullableAnnotation);

        var f = comp.GetMember<SourceEventSymbol>("C.F");
        Assert.True(f.IsPartialDefinition);
        Assert.Equal(NullableAnnotation.Annotated, f.TypeWithAnnotations.NullableAnnotation);

        var g = comp.GetMember<SourceEventSymbol>("C.G");
        Assert.True(g.IsPartialDefinition);
        Assert.Equal(NullableAnnotation.Oblivious, g.TypeWithAnnotations.NullableAnnotation);
    }

    [Fact]
    public void Difference_NullabilityAnalysis()
    {
        // The implementation part signature is used to analyze the implementation part bodies.
        // The definition part signature is used to analyze use sites.
        // Note that event assignments are not checked for nullability: https://github.com/dotnet/roslyn/issues/31018
        var source = """
            #nullable enable

            var c = new C(0, null);
            c = new C(null, 0);
            c.E += null;
            c.E -= null;
            c.F += null;
            c.F -= null;

            partial class C
            {
                public partial C(int x, string? y);
                public partial C(string x, int y);
                public partial event System.Action E;
                public partial event System.Action? F;
            }

            partial class C
            {
                public partial C(int x, string y)
                {
                    y.ToString();
                }
                public partial C(string? x, int y)
                {
                    x.ToString();
                }
                public partial event System.Action? E { add { value(); } remove { value(); } }
                public partial event System.Action F { add { value(); } remove { value(); } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,11): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // c = new C(null, 0);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(4, 11),
            // (20,20): warning CS9256: Partial member declarations 'C.C(int x, string? y)' and 'C.C(int x, string y)' have signature differences.
            //     public partial C(int x, string y)
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(int x, string? y)", "C.C(int x, string y)").WithLocation(20, 20),
            // (24,20): warning CS9256: Partial member declarations 'C.C(string x, int y)' and 'C.C(string? x, int y)' have signature differences.
            //     public partial C(string? x, int y)
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(string x, int y)", "C.C(string? x, int y)").WithLocation(24, 20),
            // (26,9): warning CS8602: Dereference of a possibly null reference.
            //         x.ToString();
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(26, 9),
            // (28,41): warning CS9256: Partial member declarations 'event Action C.E' and 'event Action? C.E' have signature differences.
            //     public partial event System.Action? E { add { value(); } remove { value(); } }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "E").WithArguments("event Action C.E", "event Action? C.E").WithLocation(28, 41),
            // (28,51): warning CS8602: Dereference of a possibly null reference.
            //     public partial event System.Action? E { add { value(); } remove { value(); } }
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "value").WithLocation(28, 51),
            // (28,71): warning CS8602: Dereference of a possibly null reference.
            //     public partial event System.Action? E { add { value(); } remove { value(); } }
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "value").WithLocation(28, 71),
            // (29,40): warning CS9256: Partial member declarations 'event Action? C.F' and 'event Action C.F' have signature differences.
            //     public partial event System.Action F { add { value(); } remove { value(); } }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "F").WithArguments("event Action? C.F", "event Action C.F").WithLocation(29, 40));
    }

    [Fact]
    public void Difference_Static()
    {
        var source = """
            partial class C
            {
                partial event System.Action E, F;
            }
            partial class C
            {
                static partial event System.Action E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,40): error CS0763: Both partial member declarations must be static or neither may be static
            //     static partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberStaticDifference, "E").WithLocation(7, 40));
    }

    [Fact]
    public void Difference_Unsafe_01()
    {
        var source = """
            partial class C
            {
                unsafe partial C();
                unsafe partial event System.Action E, F;
            }
            partial class C
            {
                partial C() { }
                unsafe partial event System.Action E { add { } remove { } }
                partial event System.Action F { add { } remove { } }
            }
            """;
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (8,13): error CS0764: Both partial member declarations must be unsafe or neither may be unsafe
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberUnsafeDifference, "C").WithLocation(8, 13),
            // (10,33): error CS0764: Both partial member declarations must be unsafe or neither may be unsafe
            //     partial event System.Action F { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberUnsafeDifference, "F").WithLocation(10, 33));
    }

    [Fact]
    public void Difference_Unsafe_02()
    {
        var source = """
            unsafe partial class C
            {
                partial C(int* p);
                partial event System.Action E, F;
            }
            partial class C
            {
                partial C(int* p) { }
                partial event System.Action E { add { int* p = null; } remove { } }
                partial event System.Action F { add { int* p = null; } remove { } }
            }
            """;
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (8,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     partial C(int* p) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(8, 15),
            // (9,43): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     partial event System.Action E { add { int* p = null; } remove { } }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(9, 43),
            // (10,43): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     partial event System.Action F { add { int* p = null; } remove { } }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(10, 43));
    }

    [Fact]
    public void Difference_ExtendedModifiers()
    {
        var source = """
            partial class C1
            {
                protected virtual partial event System.Action E, F;
                protected partial event System.Action E { add { } remove { } }
                protected virtual partial event System.Action F { add { } remove { } }
            }
            partial class C2 : C1
            {
                protected override partial event System.Action E;
                protected sealed partial event System.Action E { add { } remove { } }
                protected new partial event System.Action F;
                protected partial event System.Action F { add { } remove { } }
            }
            partial class C3 : C1
            {
                protected sealed partial event System.Action E;
                protected partial event System.Action E { add { } remove { } }
                protected override partial event System.Action F;
                protected override sealed partial event System.Action F { add { } remove { } }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (4,43): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
            //     protected partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "E").WithLocation(4, 43),
            // (10,50): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
            //     protected sealed partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "E").WithLocation(10, 50),
            // (12,43): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
            //     protected partial event System.Action F { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "F").WithLocation(12, 43),
            // (16,50): warning CS0114: 'C3.E' hides inherited member 'C1.E'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
            //     protected sealed partial event System.Action E;
            Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "E").WithArguments("C3.E", "C1.E").WithLocation(16, 50),
            // (16,50): error CS0238: 'C3.E' cannot be sealed because it is not an override
            //     protected sealed partial event System.Action E;
            Diagnostic(ErrorCode.ERR_SealedNonOverride, "E").WithArguments("C3.E").WithLocation(16, 50),
            // (17,43): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
            //     protected partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "E").WithLocation(17, 43),
            // (19,59): error CS8800: Both partial member declarations must have identical combinations of 'virtual', 'override', 'sealed', and 'new' modifiers.
            //     protected override sealed partial event System.Action F { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "F").WithLocation(19, 59));
    }

    [Fact]
    public void Difference_RefKind()
    {
        var source = """
            partial class C1
            {
                partial C1(int x);
                partial C1(ref int x) { }
            }
            partial class C2
            {
                partial C2(in int x);
                partial C2(ref readonly int x) { }
            }
            partial class C3
            {
                partial C3(ref int x);
                partial C3(out int x) => throw null;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,13): error CS9275: Partial member 'C1.C1(int)' must have an implementation part.
            //     partial C1(int x);
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C1").WithArguments("C1.C1(int)").WithLocation(3, 13),
            // (4,13): error CS9276: Partial member 'C1.C1(ref int)' must have a definition part.
            //     partial C1(ref int x) { }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C1").WithArguments("C1.C1(ref int)").WithLocation(4, 13),
            // (8,13): error CS9275: Partial member 'C2.C2(in int)' must have an implementation part.
            //     partial C2(in int x);
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C2").WithArguments("C2.C2(in int)").WithLocation(8, 13),
            // (9,13): error CS9276: Partial member 'C2.C2(ref readonly int)' must have a definition part.
            //     partial C2(ref readonly int x) { }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C2").WithArguments("C2.C2(ref readonly int)").WithLocation(9, 13),
            // (9,13): error CS0663: 'C2' cannot define an overloaded constructor that differs only on parameter modifiers 'ref readonly' and 'in'
            //     partial C2(ref readonly int x) { }
            Diagnostic(ErrorCode.ERR_OverloadRefKind, "C2").WithArguments("C2", "constructor", "ref readonly", "in").WithLocation(9, 13),
            // (13,13): error CS9275: Partial member 'C3.C3(ref int)' must have an implementation part.
            //     partial C3(ref int x);
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C3").WithArguments("C3.C3(ref int)").WithLocation(13, 13),
            // (14,13): error CS9276: Partial member 'C3.C3(out int)' must have a definition part.
            //     partial C3(out int x) => throw null;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C3").WithArguments("C3.C3(out int)").WithLocation(14, 13),
            // (14,13): error CS0663: 'C3' cannot define an overloaded constructor that differs only on parameter modifiers 'out' and 'ref'
            //     partial C3(out int x) => throw null;
            Diagnostic(ErrorCode.ERR_OverloadRefKind, "C3").WithArguments("C3", "constructor", "out", "ref").WithLocation(14, 13));
    }

    [Fact]
    public void Difference_Params()
    {
        var source = """
            using System.Collections.Generic;
            partial class C1
            {
                partial C1(object[] x);
                partial C1(params object[] x) { }
            }
            partial class C2
            {
                partial C2(int x, params IEnumerable<object> y);
                partial C2(int x, IEnumerable<object> y) { }
            }
            partial class C3
            {
                partial C3(params IEnumerable<object> x, int y);
                partial C3(IEnumerable<object> x, int y) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (5,13): error CS0758: Both partial member declarations must use a params parameter or neither may use a params parameter
            //     partial C1(params object[] x) { }
            Diagnostic(ErrorCode.ERR_PartialMemberParamsDifference, "C1").WithLocation(5, 13),
            // (10,13): error CS0758: Both partial member declarations must use a params parameter or neither may use a params parameter
            //     partial C2(int x, IEnumerable<object> y) { }
            Diagnostic(ErrorCode.ERR_PartialMemberParamsDifference, "C2").WithLocation(10, 13),
            // (14,16): error CS0231: A params parameter must be the last parameter in a parameter list
            //     partial C3(params IEnumerable<object> x, int y);
            Diagnostic(ErrorCode.ERR_ParamsLast, "params IEnumerable<object> x").WithLocation(14, 16));
    }

    [Fact]
    public void Difference_ParameterNames()
    {
        var source = """
            var c = new C(x: 123);

            partial class C
            {
                public partial C(int x);
                public partial C(int y) { System.Console.Write(y); }
            }
            """;
        CompileAndVerify(source,
            sourceSymbolValidator: validate,
            symbolValidator: validate,
            expectedOutput: "123")
            .VerifyDiagnostics(
                // (6,20): warning CS9256: Partial member declarations 'C.C(int x)' and 'C.C(int y)' have signature differences.
                //     public partial C(int y) { System.Console.Write(y); }
                Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(int x)", "C.C(int y)").WithLocation(6, 20));

        static void validate(ModuleSymbol module)
        {
            var indexer = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
            AssertEx.Equal("x", indexer.Parameters.Single().Name);
        }
    }

    [Fact]
    public void Difference_ParameterNames_NameOf()
    {
        var source = """
            using System;

            [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
            class A : Attribute { public A(string s) { } }

            partial class C
            {
                [A(nameof(p1))]
                [A(nameof(p2))] // 1
                partial C(
                    [A(nameof(p1))] // 2
                    [A(nameof(p2))]
                    int p1);

                [A(nameof(p1))]
                [A(nameof(p2))] // 3
                partial C(
                    [A(nameof(p1))] // 4
                    [A(nameof(p2))]
                    int p2)
                {
                    Console.WriteLine(nameof(p1)); // 5
                    Console.WriteLine(nameof(p2));
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (17,13): warning CS9256: Partial member declarations 'C.C(int p1)' and 'C.C(int p2)' have signature differences.
            //     partial C(
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(int p1)", "C.C(int p2)").WithLocation(17, 13),
            // (18,19): error CS0103: The name 'p1' does not exist in the current context
            //         [A(nameof(p1))] // 4
            Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(18, 19),
            // (11,19): error CS0103: The name 'p1' does not exist in the current context
            //         [A(nameof(p1))] // 2
            Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(11, 19),
            // (9,15): error CS0103: The name 'p2' does not exist in the current context
            //     [A(nameof(p2))] // 1
            Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(9, 15),
            // (16,15): error CS0103: The name 'p2' does not exist in the current context
            //     [A(nameof(p2))] // 3
            Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(16, 15),
            // (22,34): error CS0103: The name 'p1' does not exist in the current context
            //         Console.WriteLine(nameof(p1)); // 5
            Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(22, 34));
    }

    [Fact]
    public void Difference_OptionalParameters()
    {
        var source = """
            var c1 = new C1();
            var c2 = new C2();

            partial class C1
            {
                public partial C1(int x = 1);
                public partial C1(int x) { }
            }
            partial class C2
            {
                public partial C2(int x);
                public partial C2(int x = 1) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,14): error CS7036: There is no argument given that corresponds to the required parameter 'x' of 'C2.C2(int)'
            // var c2 = new C2();
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C2").WithArguments("x", "C2.C2(int)").WithLocation(2, 14),
            // (12,27): warning CS1066: The default value specified for parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
            //     public partial C2(int x = 1) { }
            Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "x").WithArguments("x").WithLocation(12, 27));
    }

    [Fact]
    public void Difference_DefaultParameterValues()
    {
        var source = """
            var c = new C();

            partial class C
            {
                public partial C(int x = 1);
                public partial C(int x = 2) { System.Console.Write(x); }
            }
            """;
        CompileAndVerify(source, expectedOutput: "1").VerifyDiagnostics(
            // (6,26): warning CS1066: The default value specified for parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
            //     public partial C(int x = 2) { System.Console.Write(x); }
            Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "x").WithArguments("x").WithLocation(6, 26));
    }

    [Fact]
    public void Difference_Scoped()
    {
        var source = """
            using System.Diagnostics.CodeAnalysis;
            partial class C1
            {
                partial C1(scoped ref int x);
                partial C1(ref int x) { }
            }
            partial class C2
            {
                partial C2(ref int x);
                partial C2(scoped ref int x) { }
            }
            partial class C3
            {
                partial C3([UnscopedRef] ref int x);
                partial C3(ref int x) { }
            }
            partial class C4
            {
                partial C4(ref int x);
                partial C4([UnscopedRef] ref int x) { }
            }
            """;
        CreateCompilation([source, UnscopedRefAttributeDefinition]).VerifyDiagnostics(
            // (5,13): error CS8988: The 'scoped' modifier of parameter 'x' doesn't match partial definition.
            //     partial C1(ref int x) { }
            Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "C1").WithArguments("x").WithLocation(5, 13),
            // (10,13): error CS8988: The 'scoped' modifier of parameter 'x' doesn't match partial definition.
            //     partial C2(scoped ref int x) { }
            Diagnostic(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, "C2").WithArguments("x").WithLocation(10, 13));
    }

    [Theory]
    [InlineData("A(1)", "B(2)")]
    [InlineData("B(2)", "A(1)")]
    [InlineData("A(1), B(1)", "A(2), B(2)")]
    public void Attributes(string declAttributes, string implAttributes)
    {
        var source1 = """
            using System;

            [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
            class A : Attribute { public A(int i) { } }

            [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
            class B : Attribute { public B(int i) { } }
            """;

        var source2 = $$"""
            public partial class C
            {
                [{{declAttributes}}] public partial C([{{declAttributes}}] int x);
                [{{declAttributes}}] public partial event System.Action E;
            }
            """;

        var source3 = $$"""
            public partial class C
            {
                [{{implAttributes}}] public partial C([{{implAttributes}}] int x) { }
                [{{implAttributes}}] public partial event System.Action E { add { } remove { } }
            }
            """;

        CompileAndVerify([source1, source2, source3],
            symbolValidator: validate,
            sourceSymbolValidator: validate)
            .VerifyDiagnostics();

        CompileAndVerify([source1, source3, source2],
            symbolValidator: validate,
            sourceSymbolValidator: validate)
            .VerifyDiagnostics();

        void validate(ModuleSymbol module)
        {
            var ctor = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
            assertEqual([declAttributes, implAttributes], ctor.GetAttributes());

            var ctorParam = ctor.GetParameters().Single();
            assertEqual([implAttributes, declAttributes], ctorParam.GetAttributes());

            var ev = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            assertEqual([declAttributes, implAttributes], ev.GetAttributes());

            if (module is SourceModuleSymbol)
            {
                assertEqual([declAttributes, implAttributes], ((SourceConstructorSymbol)ctor).PartialImplementationPart!.GetAttributes());
                assertEqual([declAttributes, implAttributes], ((SourceEventSymbol)ev).PartialImplementationPart!.GetAttributes());
            }
        }

        static void assertEqual(IEnumerable<string> expected, ImmutableArray<CSharpAttributeData> actual)
        {
            AssertEx.Equal(string.Join(", ", expected), actual.ToStrings().Join(", "));
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77254")]
    public void Attributes_Locations()
    {
        // Note that [method:] is not allowed on partial events.
        // Therefore users have no way to specify accessor attributes on the definition part.
        // Ideally, this would be possible and we would join attributes from accessors with [method:] attributes from the event.
        // However, current implementation of attribute matching is not strong enough to do that (there can be only one attribute owner symbol kind).

        var source = """
            using System;

            [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
            public class A : Attribute { public A(int i) { } }

            partial class C
            {
                [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action E;
                [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public partial event Action E
                {
                    [A(3)] [method: A(13)] [param: A(23)] [return: A(33)] [event: A(43)] [field: A(53)] add { }
                    [A(4)] [method: A(14)] [param: A(24)] [return: A(34)] [event: A(44)] [field: A(54)] remove { }
                }

                [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action F;
                [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public extern partial event Action F;
            }
            """;
        CompileAndVerify(source,
            symbolValidator: validate,
            sourceSymbolValidator: validate,
            // PEVerify fails when extern methods lack an implementation
            verify: Verification.FailsPEVerify with
            {
                PEVerifyMessage = """
                    Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                    Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                    Type load failed.
                    """,
            })
            .VerifyDiagnostics(
                // (8,13): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                //     [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action E;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "event").WithLocation(8, 13),
                // (8,29): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                //     [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action E;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "event").WithLocation(8, 29),
                // (8,44): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                //     [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action E;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "event").WithLocation(8, 44),
                // (8,75): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                //     [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action E;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "event").WithLocation(8, 75),
                // (9,13): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                //     [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public partial event Action E
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "event").WithLocation(9, 13),
                // (9,29): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                //     [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public partial event Action E
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "event").WithLocation(9, 29),
                // (9,44): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                //     [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public partial event Action E
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "event").WithLocation(9, 44),
                // (9,75): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
                //     [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public partial event Action E
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "event").WithLocation(9, 75),
                // (11,64): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                //         [A(3)] [method: A(13)] [param: A(23)] [return: A(33)] [event: A(43)] [field: A(53)] add { }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "method, param, return").WithLocation(11, 64),
                // (11,79): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                //         [A(3)] [method: A(13)] [param: A(23)] [return: A(33)] [event: A(43)] [field: A(53)] add { }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return").WithLocation(11, 79),
                // (12,64): warning CS0657: 'event' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                //         [A(4)] [method: A(14)] [param: A(24)] [return: A(34)] [event: A(44)] [field: A(54)] remove { }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "event").WithArguments("event", "method, param, return").WithLocation(12, 64),
                // (12,79): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                //         [A(4)] [method: A(14)] [param: A(24)] [return: A(34)] [event: A(44)] [field: A(54)] remove { }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return").WithLocation(12, 79),
                // (15,29): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                //     [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action F;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "method, event").WithLocation(15, 29),
                // (15,44): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                //     [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action F;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "method, event").WithLocation(15, 44),
                // (15,75): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                //     [A(1)] [method: A(11)] [param: A(21)] [return: A(31)] [event: A(41)] [field: A(51)] public partial event Action F;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, event").WithLocation(15, 75),
                // (16,29): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                //     [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public extern partial event Action F;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "method, event").WithLocation(16, 29),
                // (16,44): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                //     [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public extern partial event Action F;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "method, event").WithLocation(16, 44),
                // (16,75): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
                //     [A(2)] [method: A(12)] [param: A(22)] [return: A(32)] [event: A(42)] [field: A(52)] public extern partial event Action F;
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, event").WithLocation(16, 75));

        static void validate(ModuleSymbol module)
        {
            var isSource = module is SourceModuleSymbol;
            ReadOnlySpan<string> compiledGeneratedAttr = isSource ? [] : ["System.Runtime.CompilerServices.CompilerGeneratedAttribute"];

            var e = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            AssertEx.Equal(["A(1)", "A(41)", "A(2)", "A(42)"], e.GetAttributes().ToStrings());
            AssertEx.Equal(["A(3)", "A(13)"], e.AddMethod!.GetAttributes().ToStrings());
            AssertEx.Equal(["A(23)"], e.AddMethod.Parameters.Single().GetAttributes().ToStrings());
            AssertEx.Equal(["A(33)"], e.AddMethod.GetReturnTypeAttributes().ToStrings());
            AssertEx.Equal(["A(4)", "A(14)"], e.RemoveMethod!.GetAttributes().ToStrings());
            AssertEx.Equal(["A(24)"], e.RemoveMethod.Parameters.Single().GetAttributes().ToStrings());
            AssertEx.Equal(["A(34)"], e.RemoveMethod.GetReturnTypeAttributes().ToStrings());

            if (isSource)
            {
                var eImpl = ((SourceEventSymbol)e).PartialImplementationPart!;
                AssertEx.Equal(["A(1)", "A(41)", "A(2)", "A(42)"], eImpl.GetAttributes().ToStrings());
                AssertEx.Equal(["A(3)", "A(13)"], eImpl.AddMethod!.GetAttributes().ToStrings());
                AssertEx.Equal(["A(23)"], eImpl.AddMethod.Parameters.Single().GetAttributes().ToStrings());
                AssertEx.Equal(["A(33)"], eImpl.AddMethod.GetReturnTypeAttributes().ToStrings());
                AssertEx.Equal(["A(4)", "A(14)"], eImpl.RemoveMethod!.GetAttributes().ToStrings());
                AssertEx.Equal(["A(24)"], eImpl.RemoveMethod.Parameters.Single().GetAttributes().ToStrings());
                AssertEx.Equal(["A(34)"], eImpl.RemoveMethod.GetReturnTypeAttributes().ToStrings());
            }

            var f = module.GlobalNamespace.GetMember<EventSymbol>("C.F");
            AssertEx.Equal(["A(1)", "A(41)", "A(2)", "A(42)"], f.GetAttributes().ToStrings());
            AssertEx.Equal([.. compiledGeneratedAttr, "A(11)", "A(12)"], f.AddMethod!.GetAttributes().ToStrings());
            AssertEx.Equal(["A(22)", "A(21)"], f.AddMethod.Parameters.Single().GetAttributes().ToStrings());
            AssertEx.Equal([], f.AddMethod.GetReturnTypeAttributes().ToStrings());
            AssertEx.Equal([.. compiledGeneratedAttr, "A(11)", "A(12)"], f.RemoveMethod!.GetAttributes().ToStrings());
            AssertEx.Equal(["A(22)", "A(21)"], f.RemoveMethod.Parameters.Single().GetAttributes().ToStrings());
            AssertEx.Equal([], f.RemoveMethod.GetReturnTypeAttributes().ToStrings());

            if (isSource)
            {
                var fImpl = ((SourceEventSymbol)f).PartialImplementationPart!;
                AssertEx.Equal(["A(1)", "A(41)", "A(2)", "A(42)"], fImpl.GetAttributes().ToStrings());
                AssertEx.Equal([.. compiledGeneratedAttr, "A(11)", "A(12)"], fImpl.AddMethod!.GetAttributes().ToStrings());
                AssertEx.Equal(["A(22)", "A(21)"], fImpl.AddMethod.Parameters.Single().GetAttributes().ToStrings());
                AssertEx.Equal([], fImpl.AddMethod.GetReturnTypeAttributes().ToStrings());
                AssertEx.Equal([.. compiledGeneratedAttr, "A(11)", "A(12)"], fImpl.RemoveMethod!.GetAttributes().ToStrings());
                AssertEx.Equal(["A(22)", "A(21)"], fImpl.RemoveMethod.Parameters.Single().GetAttributes().ToStrings());
                AssertEx.Equal([], fImpl.RemoveMethod.GetReturnTypeAttributes().ToStrings());
            }
        }
    }

    [Fact]
    public void Attributes_Duplicates()
    {
        var source = """
            using System;

            class A1 : Attribute;
            class A2 : Attribute;
            class A3 : Attribute;

            partial class C
            {
                [A1] [method: A2] partial C( // def
                    [A1] [param: A2] int x);
                [A1] [method: A2] partial C( // impl
                    [A1] [param: A2] int x) { }

                [A1] [method: A2] partial event Action E;
                [A1] [method: A2] partial event Action E
                {
                    [A1] [method: A1] [param: A2] add { }
                    [A1] [method: A1] [param: A2] remove { }
                }
            
                [A1] [method: A2] [param: A3] partial event Action F;
                [A1] [method: A2] [param: A3] extern partial event Action F;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,10): error CS0579: Duplicate 'A1' attribute
            //         [A1] [param: A2] int x);
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A1").WithArguments("A1").WithLocation(10, 10),
            // (10,22): error CS0579: Duplicate 'A2' attribute
            //         [A1] [param: A2] int x);
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A2").WithArguments("A2").WithLocation(10, 22),
            // (11,6): error CS0579: Duplicate 'A1' attribute
            //     [A1] [method: A2] partial C( // impl
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A1").WithArguments("A1").WithLocation(11, 6),
            // (11,19): error CS0579: Duplicate 'A2' attribute
            //     [A1] [method: A2] partial C( // impl
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A2").WithArguments("A2").WithLocation(11, 19),
            // (14,11): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
            //     [A1] [method: A2] partial event Action E;
            Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "event").WithLocation(14, 11),
            // (15,6): error CS0579: Duplicate 'A1' attribute
            //     [A1] [method: A2] partial event Action E
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A1").WithArguments("A1").WithLocation(15, 6),
            // (15,11): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'event'. All attributes in this block will be ignored.
            //     [A1] [method: A2] partial event Action E
            Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "event").WithLocation(15, 11),
            // (17,23): error CS0579: Duplicate 'A1' attribute
            //         [A1] [method: A1] [param: A2] add { }
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A1").WithArguments("A1").WithLocation(17, 23),
            // (18,23): error CS0579: Duplicate 'A1' attribute
            //         [A1] [method: A1] [param: A2] remove { }
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A1").WithArguments("A1").WithLocation(18, 23),
            // (21,24): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
            //     [A1] [method: A2] [param: A3] partial event Action F;
            Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "method, event").WithLocation(21, 24),
            // (21,31): error CS0579: Duplicate 'A3' attribute
            //     [A1] [method: A2] [param: A3] partial event Action F;
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A3").WithArguments("A3").WithLocation(21, 31),
            // (21,31): error CS0579: Duplicate 'A3' attribute
            //     [A1] [method: A2] [param: A3] partial event Action F;
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A3").WithArguments("A3").WithLocation(21, 31),
            // (22,6): error CS0579: Duplicate 'A1' attribute
            //     [A1] [method: A2] [param: A3] extern partial event Action F;
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A1").WithArguments("A1").WithLocation(22, 6),
            // (22,19): error CS0579: Duplicate 'A2' attribute
            //     [A1] [method: A2] [param: A3] extern partial event Action F;
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "A2").WithArguments("A2").WithLocation(22, 19),
            // (22,24): warning CS0657: 'param' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, event'. All attributes in this block will be ignored.
            //     [A1] [method: A2] [param: A3] extern partial event Action F;
            Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "param").WithArguments("param", "method, event").WithLocation(22, 24));
    }

    [Fact]
    public void Attributes_CallerInfo()
    {
        var source1 = """
            using System;
            using System.Runtime.CompilerServices;

            public partial class C
            {
                public partial C(int x,
                    [CallerLineNumber] int a = -1,
                    [CallerFilePath] string b = "f",
                    [CallerMemberName] string c = "m",
                    [CallerArgumentExpression(nameof(x))] string d = "e");
                public partial C(int x, int a, string b, string c, string d)
                {
                    Console.WriteLine($"x='{x}' a='{a}' b='{b}' c='{c}' d='{d}'");
                }

                public partial C(string x,
                    int a = -1,
                    string b = "f",
                    string c = "m",
                    string d = "e");
                public partial C(string x,
                    [CallerLineNumber] int a,
                    [CallerFilePath] string b,
                    [CallerMemberName] string c,
                    [CallerArgumentExpression(nameof(x))] string d)
                {
                    Console.WriteLine($"x='{x}' a='{a}' b='{b}' c='{c}' d='{d}'");
                }
            }
            """;

        var source2 = ("""
            var c1 = new C(40 + 2);
            var c2 = new C("s");
            """, "file.cs");

        var expectedDiagnostics = new[]
        {
            // (22,10): warning CS4024: The CallerLineNumberAttribute applied to parameter 'a' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
            //         [CallerLineNumber] int a,
            Diagnostic(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, "CallerLineNumber").WithArguments("a").WithLocation(22, 10),
            // (23,10): warning CS4025: The CallerFilePathAttribute applied to parameter 'b' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
            //         [CallerFilePath] string b,
            Diagnostic(ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation, "CallerFilePath").WithArguments("b").WithLocation(23, 10),
            // (24,10): warning CS4026: The CallerMemberNameAttribute applied to parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
            //         [CallerMemberName] string c,
            Diagnostic(ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation, "CallerMemberName").WithArguments("c").WithLocation(24, 10),
            // (25,10): warning CS8966: The CallerArgumentExpressionAttribute applied to parameter 'd' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
            //         [CallerArgumentExpression(nameof(x))] string d)
            Diagnostic(ErrorCode.WRN_CallerArgumentExpressionParamForUnconsumedLocation, "CallerArgumentExpression").WithArguments("d").WithLocation(25, 10)
        };

        CompileAndVerify([source1, source2, CallerArgumentExpressionAttributeDefinition],
            expectedOutput: """
                x='42' a='1' b='file.cs' c='<Main>$' d='40 + 2'
                x='s' a='-1' b='f' c='m' d='e'
                """)
            .VerifyDiagnostics(expectedDiagnostics);

        // Although caller info attributes on the implementation part have no effect in source,
        // they are written to metadata as is demonstrated below.
        // See https://github.com/dotnet/roslyn/issues/73482.

        var lib = CreateCompilation([source1, CallerArgumentExpressionAttributeDefinition])
            .VerifyDiagnostics(expectedDiagnostics)
            .EmitToPortableExecutableReference();

        CompileAndVerify(CreateCompilation(source2, references: [lib]),
            expectedOutput: """
                x='42' a='1' b='file.cs' c='<Main>$' d='40 + 2'
                x='s' a='2' b='file.cs' c='<Main>$' d='"s"'
                """)
            .VerifyDiagnostics();
    }

    [Fact]
    public void Attributes_Nullable()
    {
        var source = """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            new C(default(I1)!, null);
            new C(default(I2)!, null);
            new C(default(I3)!, null);
            new C(default(I4)!, null);
            new C(default(I5)!, null);
            new C(default(I6)!, null);
            new C(default(I7)!, null);
            new C(default(I8)!, null);

            interface I1;
            interface I2;
            interface I3;
            interface I4;
            interface I5;
            interface I6;
            interface I7;
            interface I8;

            partial class C
            {
                public partial C(I1 i, [AllowNull] object x);
                public partial C(I1 i, object x) { x.ToString(); }

                public partial C(I2 i, object x);
                public partial C(I2 i, [AllowNull] object x) { x.ToString(); }

                public partial C(I3 i, [AllowNull] object x);
                public partial C(I3 i, object? x) { x.ToString(); }

                public partial C(I4 i, object? x);
                public partial C(I4 i, [AllowNull] object x) { x.ToString(); }

                public partial C(I5 i, [DisallowNull] object? x);
                public partial C(I5 i, object? x) { x.ToString(); }

                public partial C(I6 i, object? x);
                public partial C(I6 i, [DisallowNull] object? x) { x.ToString(); }

                public partial C(I7 i, [DisallowNull] object? x);
                public partial C(I7 i, object x) { x.ToString(); }

                public partial C(I8 i, object x);
                public partial C(I8 i, [DisallowNull] object? x) { x.ToString(); }
            }
            """;
        CreateCompilation([source, AllowNullAttributeDefinition, DisallowNullAttributeDefinition]).VerifyDiagnostics(
            // (8,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new C(default(I5)!, null);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(8, 21),
            // (9,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new C(default(I6)!, null);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(9, 21),
            // (10,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new C(default(I7)!, null);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 21),
            // (11,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // new C(default(I8)!, null);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 21),
            // (25,40): warning CS8602: Dereference of a possibly null reference.
            //     public partial C(I1 i, object x) { x.ToString(); }
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(25, 40),
            // (28,52): warning CS8602: Dereference of a possibly null reference.
            //     public partial C(I2 i, [AllowNull] object x) { x.ToString(); }
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(28, 52),
            // (31,20): warning CS9256: Partial member declarations 'C.C(I3 i, object x)' and 'C.C(I3 i, object? x)' have signature differences.
            //     public partial C(I3 i, object? x) { x.ToString(); }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(I3 i, object x)", "C.C(I3 i, object? x)").WithLocation(31, 20),
            // (31,41): warning CS8602: Dereference of a possibly null reference.
            //     public partial C(I3 i, object? x) { x.ToString(); }
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(31, 41),
            // (34,20): warning CS9256: Partial member declarations 'C.C(I4 i, object? x)' and 'C.C(I4 i, object x)' have signature differences.
            //     public partial C(I4 i, [AllowNull] object x) { x.ToString(); }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(I4 i, object? x)", "C.C(I4 i, object x)").WithLocation(34, 20),
            // (34,52): warning CS8602: Dereference of a possibly null reference.
            //     public partial C(I4 i, [AllowNull] object x) { x.ToString(); }
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(34, 52),
            // (43,20): warning CS9256: Partial member declarations 'C.C(I7 i, object? x)' and 'C.C(I7 i, object x)' have signature differences.
            //     public partial C(I7 i, object x) { x.ToString(); }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(I7 i, object? x)", "C.C(I7 i, object x)").WithLocation(43, 20),
            // (46,20): warning CS9256: Partial member declarations 'C.C(I8 i, object x)' and 'C.C(I8 i, object? x)' have signature differences.
            //     public partial C(I8 i, [DisallowNull] object? x) { x.ToString(); }
            Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(I8 i, object x)", "C.C(I8 i, object? x)").WithLocation(46, 20));
    }

    [Fact]
    public void Attributes_Obsolete()
    {
        var source = """
            using System;

            var c = new C();
            c = new C(2);
            c.E += null;
            c.E -= null;
            c.F += null;
            c.F -= null;
            c.G += null;
            c.G -= null;
            c.H += null;
            c.H -= null;

            partial class C
            {
                [Obsolete] public partial C();
                public partial C() { M(); }

                public partial C(int x = X);
                [Obsolete] public partial C(int x) { M(); }

                [Obsolete] public partial event Action E;
                public partial event Action E { add { M(); } remove { M(); } }

                public partial event Action F;
                public partial event Action F
                {
                    [Obsolete] add { M(); }
                    remove { M(); }
                }

                [Obsolete] public partial event Action G;
                public extern partial event Action G;

                [method: Obsolete] public partial event Action H;
                public extern partial event Action H;

                [Obsolete] void M() { }
                [Obsolete] const int X = 1;
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (3,9): warning CS0612: 'C.C()' is obsolete
            // var c = new C();
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()").WithArguments("C.C()").WithLocation(3, 9),
            // (4,5): warning CS0612: 'C.C(int)' is obsolete
            // c = new C(2);
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C(2)").WithArguments("C.C(int)").WithLocation(4, 5),
            // (5,1): warning CS0612: 'C.E' is obsolete
            // c.E += null;
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c.E").WithArguments("C.E").WithLocation(5, 1),
            // (6,1): warning CS0612: 'C.E' is obsolete
            // c.E -= null;
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c.E").WithArguments("C.E").WithLocation(6, 1),
            // (9,1): warning CS0612: 'C.G' is obsolete
            // c.G += null;
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c.G").WithArguments("C.G").WithLocation(9, 1),
            // (10,1): warning CS0612: 'C.G' is obsolete
            // c.G -= null;
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c.G").WithArguments("C.G").WithLocation(10, 1),
            // (28,10): error CS8423: Attribute 'System.ObsoleteAttribute' is not valid on event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
            //         [Obsolete] add { M(); }
            Diagnostic(ErrorCode.ERR_AttributeNotOnEventAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(28, 10),
            // (29,18): warning CS0612: 'C.M()' is obsolete
            //         remove { M(); }
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "M()").WithArguments("C.M()").WithLocation(29, 18),
            // (35,14): error CS8423: Attribute 'System.ObsoleteAttribute' is not valid on event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
            //     [method: Obsolete] public partial event Action H;
            Diagnostic(ErrorCode.ERR_AttributeNotOnEventAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(35, 14));
    }
}
