// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
            // (6,17): error CS9401: Partial member 'partial.partial()' must have a definition part.
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "F").WithArguments("partial.partial()").WithLocation(6, 17),
            // (6,24): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //         partial F() => new();
            Diagnostic(ErrorCode.ERR_IllegalStatement, "new()").WithLocation(6, 24),
            // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,13): error CS9401: Partial member 'C.C()' must have a definition part.
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "F").WithArguments("C.C()").WithLocation(3, 13),
            // (3,20): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //     partial F() => new();
            Diagnostic(ErrorCode.ERR_IllegalStatement, "new()").WithLocation(3, 20),
            // (6,38): error CS1061: 'C' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            //         System.Console.Write(new C().F().GetType().Name);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("C", "F").WithLocation(6, 38)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
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
            // (3,5): error CS8652: The feature 'partial events and constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial F() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("partial events and constructors").WithLocation(3, 5),
            // (3,13): error CS0161: 'C.F()': not all code paths return a value
            //     partial F() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "F").WithArguments("C.F()").WithLocation(3, 13),
            // (4,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(4, 5),
            // (4,5): error CS8652: The feature 'partial events and constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("partial events and constructors").WithLocation(4, 5),
            // (4,13): error CS0542: 'C': member names cannot be the same as their enclosing type
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(4, 13),
            // (4,13): error CS0161: 'C.C()': not all code paths return a value
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "C").WithArguments("C.C()").WithLocation(4, 13));
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
            // (3,33): error CS8703: The modifier 'partial' is not valid for this item in C# 13.0. Please use language version 'preview' or greater.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "E").WithArguments("partial", "13.0", "preview").WithLocation(3, 33),
            // (5,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     partial C();
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(5, 5),
            // (5,5): error CS8652: The feature 'partial events and constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial C();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("partial events and constructors").WithLocation(5, 5),
            // (5,13): error CS0501: 'C.C()' must declare a body because it is not marked abstract, extern, or partial
            //     partial C();
            Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.C()").WithLocation(5, 13),
            // (5,13): error CS0542: 'C': member names cannot be the same as their enclosing type
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(5, 13),
            // (6,5): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(6, 5),
            // (6,5): error CS8652: The feature 'partial events and constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("partial events and constructors").WithLocation(6, 5),
            // (6,13): error CS0542: 'C': member names cannot be the same as their enclosing type
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(6, 13),
            // (6,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(6, 13),
            // (6,13): error CS0161: 'C.C()': not all code paths return a value
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "C").WithArguments("C.C()").WithLocation(6, 13));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
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
            // (3,13): error CS9401: Partial member 'C.C()' must have a definition part.
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
            // (3,33): error CS9400: Partial member 'C.E' must have an implementation part.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "E").WithArguments("C.E").WithLocation(3, 33),
            // (4,13): error CS9400: Partial member 'C.C()' must have an implementation part.
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
            // (3,33): error CS9401: Partial member 'C.E' must have a definition part.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "E").WithArguments("C.E").WithLocation(3, 33),
            // (4,13): error CS9401: Partial member 'C.C()' must have a definition part.
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
            // (4,33): error CS9402: Partial member 'C.E' may not have multiple defining declarations.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "E").WithArguments("C.E").WithLocation(4, 33),
            // (4,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(4, 33),
            // (5,33): error CS9402: Partial member 'C.F' may not have multiple defining declarations.
            //     partial event System.Action F;
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "F").WithArguments("C.F").WithLocation(5, 33),
            // (5,33): error CS0102: The type 'C' already contains a definition for 'F'
            //     partial event System.Action F;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "F").WithArguments("C", "F").WithLocation(5, 33),
            // (7,13): error CS9402: Partial member 'C.C()' may not have multiple defining declarations.
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
            // (4,33): error CS9403: Partial member 'C.E' may not have multiple implementing declarations.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.E").WithLocation(4, 33),
            // (6,13): error CS9403: Partial member 'C.C()' may not have multiple implementing declarations.
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
            // (4,33): error CS9403: Partial member 'C.E' may not have multiple implementing declarations.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.E").WithLocation(4, 33),
            // (6,13): error CS9403: Partial member 'C.C()' may not have multiple implementing declarations.
            //     partial C() { }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "C").WithArguments("C.C()").WithLocation(6, 13),
            // (8,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(8, 33),
            // (9,33): error CS9402: Partial member 'C.E' may not have multiple defining declarations.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateDefinition, "E").WithArguments("C.E").WithLocation(9, 33),
            // (9,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(9, 33),
            // (10,13): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
            //     partial C();
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(10, 13),
            // (11,13): error CS9402: Partial member 'C.C()' may not have multiple defining declarations.
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
            // (3,33): error CS9400: Partial member 'C.E' must have an implementation part.
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
            // (3,33): error CS9404: 'C.E': partial event cannot have initializer
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
            // (3,36): error CS9404: 'C.F': partial event cannot have initializer
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
            // (3,33): error CS9404: 'C.E': partial event cannot have initializer
            //     partial event System.Action E = null, F = null;
            Diagnostic(ErrorCode.ERR_PartialEventInitializer, "E").WithArguments("C.E").WithLocation(3, 33),
            // (3,33): warning CS0414: The field 'C.E' is assigned but its value is never used
            //     partial event System.Action E = null, F = null;
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "E").WithArguments("C.E").WithLocation(3, 33),
            // (3,43): error CS9404: 'C.F': partial event cannot have initializer
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
    public void StaticPartialConstructor()
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
            // (5,33): error CS9401: Partial event 'C.F' must have a definition part.
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
            // (7,35): error CS9401: Partial member 'C.I.E' must have a definition part.
            //     partial event System.Action I.E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "E").WithArguments("C.I.E").WithLocation(7, 35),
            // (7,35): error CS0754: A partial member may not explicitly implement an interface member
            //     partial event System.Action I.E;
            Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "E").WithLocation(7, 35),
            // (8,35): error CS9403: Partial member 'C.I.E' may not have multiple implementing declarations.
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
        CompileAndVerify(source,
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
            .VerifyDiagnostics(
                // (4,40): warning CS0626: Method, operator, or accessor 'C.E.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //     extern partial event System.Action E;
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "E").WithArguments("C.E.remove").WithLocation(4, 40));

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
                event System.Action C.E
                void C.E.add
                void C.E.remove
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
            // (3,33): error CS9400: Partial member 'C.E' must have an implementation part.
            //     partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "E").WithArguments("C.E").WithLocation(3, 33),
            // (4,32): error CS0102: The type 'C' already contains a definition for 'E'
            //     extern event System.Action E;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(4, 32),
            // (4,32): warning CS0626: Method, operator, or accessor 'C.E.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     extern event System.Action E;
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "E").WithArguments("C.E.remove").WithLocation(4, 32),
            // (6,13): error CS9400: Partial member 'C.C()' must have an implementation part.
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
            // (3,40): error CS9401: Partial member 'C.E' must have a definition part.
            //     extern partial event System.Action E;
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "E").WithArguments("C.E").WithLocation(3, 40),
            // (3,40): warning CS0626: Method, operator, or accessor 'C.E.remove' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
            //     extern partial event System.Action E;
            Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "E").WithArguments("C.E.remove").WithLocation(3, 40),
            // (4,33): error CS9403: Partial member 'C.E' may not have multiple implementing declarations.
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMemberDuplicateImplementation, "E").WithArguments("C.E").WithLocation(4, 33),
            // (4,33): error CS0102: The type 'C' already contains a definition for 'E'
            //     partial event System.Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(4, 33),
            // (6,20): error CS9401: Partial member 'C.C()' must have a definition part.
            //     extern partial C();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "C").WithArguments("C.C()").WithLocation(6, 20),
            // (7,13): error CS9403: Partial member 'C.C()' may not have multiple implementing declarations.
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
            symbolValidator: verifyMetadata,
            // PEVerify fails when extern methods lack an implementation
            verify: Verification.FailsPEVerify with
            {
                PEVerifyMessage = """
                    Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                    Type load failed.
                    """,
            })
            .VerifyDiagnostics();

        static void verifySource(ModuleSymbol module)
        {
            var ev = module.GlobalNamespace.GetMember<SourceEventSymbol>("C.E");
            Assert.True(ev.GetPublicSymbol().IsExtern);
            Assert.True(ev.AddMethod!.GetPublicSymbol().IsExtern);
            Assert.Null(ev.AddMethod!.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, ev.AddMethod.ImplementationAttributes);
            Assert.True(ev.RemoveMethod!.GetPublicSymbol().IsExtern);
            Assert.Null(ev.RemoveMethod!.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, ev.RemoveMethod.ImplementationAttributes);

            var c = module.GlobalNamespace.GetMember<SourceConstructorSymbol>("C..ctor");
            Assert.True(c.GetPublicSymbol().IsExtern);
            Assert.Null(c.GetDllImportData());
            // PROTOTYPE: needs attribute merging
            //Assert.Equal(MethodImplAttributes.InternalCall, c.ImplementationAttributes);
        }

        static void verifyMetadata(ModuleSymbol module)
        {
            // IsExtern doesn't round trip from metadata when DllImportAttribute is missing.
            // This is consistent with the behavior of partial methods and properties.

            var ev = module.GlobalNamespace.GetMember<EventSymbol>("C.E");
            Assert.False(ev.GetPublicSymbol().IsExtern);
            Assert.False(ev.AddMethod!.GetPublicSymbol().IsExtern);
            Assert.Null(ev.AddMethod!.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, ev.AddMethod.ImplementationAttributes);
            Assert.False(ev.RemoveMethod!.GetPublicSymbol().IsExtern);
            Assert.Null(ev.RemoveMethod!.GetDllImportData());
            Assert.Equal(MethodImplAttributes.InternalCall, ev.RemoveMethod.ImplementationAttributes);

            var c = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
            Assert.False(c.GetPublicSymbol().IsExtern);
            Assert.Null(c.GetDllImportData());
            // PROTOTYPE: needs attribute merging
            //Assert.Equal(MethodImplAttributes.InternalCall, c.ImplementationAttributes);
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
            Assert.Null(e.PartialDefinitionPart);
            Assert.True(e.SourcePartialImplementationPart!.IsPartialImplementation);
            Assert.False(e.SourcePartialImplementationPart.IsPartialDefinition);
            Assert.False(e.SourcePartialImplementationPart.HasAssociatedField);

            var addMethod = e.AddMethod!;
            Assert.Equal("add_E", addMethod.Name);
            Assert.Same(addMethod, e.SourcePartialImplementationPart.AddMethod);
            var removeMethod = e.RemoveMethod!;
            Assert.Equal("remove_E", removeMethod.Name);
            Assert.Same(removeMethod, e.SourcePartialImplementationPart.RemoveMethod);

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
                public partial event Action E;
                partial event Action E { add { } remove { } }

                void M()
                {
                    this.E += () => { };
                    this.E -= () => { };
                }
            }
            """;
        // PROTOTYPE: Mismatch between accessibility modifiers of parts should be reported.
        CreateCompilation(source).VerifyDiagnostics(
            // (4,1): error CS0122: 'C.E.add' is inaccessible due to its protection level
            // c.E += () => { };
            Diagnostic(ErrorCode.ERR_BadAccess, "c.E += () => { }").WithArguments("C.E.add").WithLocation(4, 1),
            // (5,1): error CS0122: 'C.E.remove' is inaccessible due to its protection level
            // c.E -= () => { };
            Diagnostic(ErrorCode.ERR_BadAccess, "c.E -= () => { }").WithArguments("C.E.remove").WithLocation(5, 1));
    }
}
