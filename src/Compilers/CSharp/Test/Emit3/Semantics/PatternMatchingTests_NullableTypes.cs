// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.Patterns)]
public class PatternMatchingTests_NullableTypes : PatternMatchingTestBase
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void PatternCombinations()
    {
        var source = """
            #nullable enable

            class C
            {
                void M(object obj)
                {
                    if (obj is int? or string?) { }
                    if (obj is int? i1 or string? s) { }
                    if (obj is int? and { }) { }
                    if (obj is int? i2 and { }) { }
                    if (obj is int? i3 and (1, 2)) { }
                    if (obj is int? i4 and []) { }
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,20): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (obj is int? or string?) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(7, 20),
            // (7,28): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (obj is int? or string?) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(7, 28),
            // (8,20): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (obj is int? i1 or string? s) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(8, 20),
            // (8,25): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (obj is int? i1 or string? s) { }
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "i1").WithLocation(8, 25),
            // (8,31): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (obj is int? i1 or string? s) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(8, 31),
            // (8,39): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (obj is int? i1 or string? s) { }
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "s").WithLocation(8, 39),
            // (9,20): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (obj is int? and { }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(9, 20),
            // (10,20): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (obj is int? i2 and { }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(10, 20),
            // (11,20): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (obj is int? i3 and (1, 2)) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(11, 20),
            // (11,32): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
            //         if (obj is int? i3 and (1, 2)) { }
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(1, 2)").WithArguments("int", "Deconstruct").WithLocation(11, 32),
            // (11,32): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'int', with 2 out parameters and a void return type.
            //         if (obj is int? i3 and (1, 2)) { }
            Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(1, 2)").WithArguments("int", "2").WithLocation(11, 32),
            // (12,20): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (obj is int? i4 and []) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(12, 20),
            // (12,32): error CS8985: List patterns may not be used for a value of type 'int'. No suitable 'Length' or 'Count' property was found.
            //         if (obj is int? i4 and []) { }
            Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("int").WithLocation(12, 32),
            // (12,32): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         if (obj is int? i4 and []) { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(12, 32),
            // (12,32): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
            //         if (obj is int? i4 and []) { }
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("int").WithLocation(12, 32));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void PatternCombinations_Arrays()
    {
        var source = """
            #nullable enable

            class C
            {
                void M(object obj)
                {
                    if (obj is int[]? or string[]?) { }
                    if (obj is int[]? i1 or string[]? s) { }
                    if (obj is int[]? and { }) { }
                    if (obj is int[]? i2 and { }) { }
                    if (obj is int[]? i3 and (1, 2)) { }
                    if (obj is int[]? i4 and []) { }
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,20): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (obj is int[]? or string[]?) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(7, 20),
            // (7,30): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (obj is int[]? or string[]?) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(7, 30),
            // (8,20): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (obj is int[]? i1 or string[]? s) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(8, 20),
            // (8,27): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (obj is int[]? i1 or string[]? s) { }
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "i1").WithLocation(8, 27),
            // (8,33): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (obj is int[]? i1 or string[]? s) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(8, 33),
            // (8,43): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (obj is int[]? i1 or string[]? s) { }
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "s").WithLocation(8, 43),
            // (9,20): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (obj is int[]? and { }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(9, 20),
            // (10,20): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (obj is int[]? i2 and { }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(10, 20),
            // (11,20): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (obj is int[]? i3 and (1, 2)) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(11, 20),
            // (11,34): error CS1061: 'int[]' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)
            //         if (obj is int[]? i3 and (1, 2)) { }
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(1, 2)").WithArguments("int[]", "Deconstruct").WithLocation(11, 34),
            // (11,34): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'int[]', with 2 out parameters and a void return type.
            //         if (obj is int[]? i3 and (1, 2)) { }
            Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(1, 2)").WithArguments("int[]", "2").WithLocation(11, 34),
            // (12,20): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (obj is int[]? i4 and []) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(12, 20),
            // (12,34): error CS0518: Predefined type 'System.Index' is not defined or imported
            //         if (obj is int[]? i4 and []) { }
            Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(12, 34));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void PatternsInSwitchStatement()
    {
        var source = """
            using System.Threading.Tasks;

            #nullable enable

            class C
            {
                void M(object obj)
                {
                    var x = 0;
                    switch (obj)
                    {
                        case int?:
                            break;
                        case Task?:
                            break;
                        case int? i1:
                            break;
                        case Task? t1:
                            break;
                        case int? when x > 0:
                            break;
                        case Task? when x > 0:
                            break;
                        case int? i2 when x > 0:
                            break;
                        case Task? t2 when x > 0:
                            break;
                        case (int? when) when x > 0:
                            break;
                        case (Task? when) when x > 0:
                            break;
                    }
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (12,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //             case int?:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(12, 18),
            // (14,18): error CS8116: It is not legal to use nullable type 'Task?' in a pattern; use the underlying type 'Task' instead.
            //             case Task?:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task?").WithArguments("System.Threading.Tasks.Task").WithLocation(14, 18),
            // (16,21): error CS1003: Syntax error, ':' expected
            //             case int? i1:
            Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments(":").WithLocation(16, 21),
            // (16,21): error CS1513: } expected
            //             case int? i1:
            Diagnostic(ErrorCode.ERR_RbraceExpected, "?").WithLocation(16, 21),
            // (16,23): warning CS0164: This label has not been referenced
            //             case int? i1:
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "i1").WithLocation(16, 23),
            // (18,18): error CS0119: 'Task' is a type, which is not valid in the given context
            //             case Task? t1:
            Diagnostic(ErrorCode.ERR_BadSKunknown, "Task").WithArguments("System.Threading.Tasks.Task", "type").WithLocation(18, 18),
            // (18,24): error CS0103: The name 't1' does not exist in the current context
            //             case Task? t1:
            Diagnostic(ErrorCode.ERR_NameNotInContext, "t1").WithArguments("t1").WithLocation(18, 24),
            // (18,27): error CS1525: Invalid expression term 'break'
            //             case Task? t1:
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("break").WithLocation(18, 27),
            // (18,27): error CS1003: Syntax error, ':' expected
            //             case Task? t1:
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(18, 27),
            // (20,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //             case int? when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(20, 18),
            // (22,18): error CS8116: It is not legal to use nullable type 'Task?' in a pattern; use the underlying type 'Task' instead.
            //             case Task? when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task?").WithArguments("System.Threading.Tasks.Task").WithLocation(22, 18),
            // (24,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //             case int? i2 when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(24, 18),
            // (26,18): error CS8116: It is not legal to use nullable type 'Task?' in a pattern; use the underlying type 'Task' instead.
            //             case Task? t2 when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task?").WithArguments("System.Threading.Tasks.Task").WithLocation(26, 18),
            // (28,19): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //             case (int? when) when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(28, 19),
            // (30,19): error CS8116: It is not legal to use nullable type 'Task?' in a pattern; use the underlying type 'Task' instead.
            //             case (Task? when) when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task?").WithArguments("System.Threading.Tasks.Task").WithLocation(30, 19));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void PatternsInSwitchStatement_Arrays()
    {
        var source = """
            using System.Threading.Tasks;

            #nullable enable

            class C
            {
                void M(object obj)
                {
                    var x = 0;
                    switch (obj)
                    {
                        case int[]?:
                            break;
                        case Task[]?:
                            break;
                        case int[]? i1:
                            break;
                        case Task[]? t1:
                            break;
                        case int[]? when x > 0:
                            break;
                        case Task[]? when x > 0:
                            break;
                        case int[]? i2 when x > 0:
                            break;
                        case Task[]? t2 when x > 0:
                            break;
                        case (int[]? when) when x > 0:
                            break;
                        case (Task[]? when) when x > 0:
                            break;
                    }
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (12,18): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //             case int[]?:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(12, 18),
            // (14,18): error CS8116: It is not legal to use nullable type 'Task[]?' in a pattern; use the underlying type 'Task[]' instead.
            //             case Task[]?:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task[]?").WithArguments("System.Threading.Tasks.Task[]").WithLocation(14, 18),
            // (16,23): error CS1003: Syntax error, ':' expected
            //             case int[]? i1:
            Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments(":").WithLocation(16, 23),
            // (16,23): error CS1513: } expected
            //             case int[]? i1:
            Diagnostic(ErrorCode.ERR_RbraceExpected, "?").WithLocation(16, 23),
            // (16,25): warning CS0164: This label has not been referenced
            //             case int[]? i1:
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "i1").WithLocation(16, 25),
            // (18,24): error CS1003: Syntax error, ':' expected
            //             case Task[]? t1:
            Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments(":").WithLocation(18, 24),
            // (18,24): error CS1513: } expected
            //             case Task[]? t1:
            Diagnostic(ErrorCode.ERR_RbraceExpected, "?").WithLocation(18, 24),
            // (18,26): warning CS0164: This label has not been referenced
            //             case Task[]? t1:
            Diagnostic(ErrorCode.WRN_UnreferencedLabel, "t1").WithLocation(18, 26),
            // (20,18): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //             case int[]? when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(20, 18),
            // (22,18): error CS8116: It is not legal to use nullable type 'Task[]?' in a pattern; use the underlying type 'Task[]' instead.
            //             case Task[]? when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task[]?").WithArguments("System.Threading.Tasks.Task[]").WithLocation(22, 18),
            // (24,18): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //             case int[]? i2 when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(24, 18),
            // (26,18): error CS8116: It is not legal to use nullable type 'Task[]?' in a pattern; use the underlying type 'Task[]' instead.
            //             case Task[]? t2 when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task[]?").WithArguments("System.Threading.Tasks.Task[]").WithLocation(26, 18),
            // (28,19): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //             case (int[]? when) when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(28, 19),
            // (30,19): error CS8116: It is not legal to use nullable type 'Task[]?' in a pattern; use the underlying type 'Task[]' instead.
            //             case (Task[]? when) when x > 0:
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task[]?").WithArguments("System.Threading.Tasks.Task[]").WithLocation(30, 19));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void PatternsInSwitchExpression()
    {
        var source = """
            using System.Threading.Tasks;

            #nullable enable

            class C
            {
                void M(object obj)
                {
                    var x = 0;
                    _ = obj switch
                    {
                        int? => 1,
                        Task? => 2,
                        int? i1 => 3,
                        Task? t1 => 4,
                        int? when x > 0 => 5,
                        Task? when x > 0 => 6,
                        int? i2 when x > 0 => 7,
                        Task? t2 when x > 0 => 8,
                        (int? when) when x > 0 => 9,
                        (Task? when) when x > 0 => 10
                    };
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (12,13): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //             int? => 1,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(12, 13),
            // (13,13): error CS8116: It is not legal to use nullable type 'Task?' in a pattern; use the underlying type 'Task' instead.
            //             Task? => 2,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task?").WithArguments("System.Threading.Tasks.Task").WithLocation(13, 13),
            // (14,16): error CS1003: Syntax error, '=>' expected
            //             int? i1 => 3,
            Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments("=>").WithLocation(14, 16),
            // (14,16): error CS1525: Invalid expression term '?'
            //             int? i1 => 3,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(14, 16),
            // (14,25): error CS1003: Syntax error, ':' expected
            //             int? i1 => 3,
            Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments(":").WithLocation(14, 25),
            // (14,25): error CS1525: Invalid expression term ','
            //             int? i1 => 3,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(14, 25),
            // (15,17): error CS1003: Syntax error, '=>' expected
            //             Task? t1 => 4,
            Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments("=>").WithLocation(15, 17),
            // (15,17): error CS1525: Invalid expression term '?'
            //             Task? t1 => 4,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(15, 17),
            // (15,26): error CS1003: Syntax error, ':' expected
            //             Task? t1 => 4,
            Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments(":").WithLocation(15, 26),
            // (15,26): error CS1525: Invalid expression term ','
            //             Task? t1 => 4,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(15, 26),
            // (16,13): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //             int? when x > 0 => 5,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(16, 13),
            // (17,13): error CS8116: It is not legal to use nullable type 'Task?' in a pattern; use the underlying type 'Task' instead.
            //             Task? when x > 0 => 6,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task?").WithArguments("System.Threading.Tasks.Task").WithLocation(17, 13),
            // (18,13): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //             int? i2 when x > 0 => 7,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(18, 13),
            // (19,13): error CS8116: It is not legal to use nullable type 'Task?' in a pattern; use the underlying type 'Task' instead.
            //             Task? t2 when x > 0 => 8,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task?").WithArguments("System.Threading.Tasks.Task").WithLocation(19, 13),
            // (20,14): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //             (int? when) when x > 0 => 9,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(20, 14),
            // (21,14): error CS8116: It is not legal to use nullable type 'Task?' in a pattern; use the underlying type 'Task' instead.
            //             (Task? when) when x > 0 => 10
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task?").WithArguments("System.Threading.Tasks.Task").WithLocation(21, 14));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void PatternsInSwitchExpression_Arrays()
    {
        var source = """
            using System.Threading.Tasks;

            #nullable enable

            class C
            {
                void M(object obj)
                {
                    var x = 0;
                    _ = obj switch
                    {
                        int[]? => 1,
                        Task[]? => 2,
                        int[]? i1 => 3,
                        Task[]? t1 => 4,
                        int[]? when x > 0 => 5,
                        Task[]? when x > 0 => 6,
                        int[]? i2 when x > 0 => 7,
                        Task[]? t2 when x > 0 => 8,
                        (int[]? when) when x > 0 => 9,
                        (Task[]? when) when x > 0 => 10
                    };
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (12,13): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //             int[]? => 1,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(12, 13),
            // (13,13): error CS8116: It is not legal to use nullable type 'Task[]?' in a pattern; use the underlying type 'Task[]' instead.
            //             Task[]? => 2,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task[]?").WithArguments("System.Threading.Tasks.Task[]").WithLocation(13, 13),
            // (14,18): error CS1003: Syntax error, '=>' expected
            //             int[]? i1 => 3,
            Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments("=>").WithLocation(14, 18),
            // (14,18): error CS1525: Invalid expression term '?'
            //             int[]? i1 => 3,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(14, 18),
            // (14,27): error CS1003: Syntax error, ':' expected
            //             int[]? i1 => 3,
            Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments(":").WithLocation(14, 27),
            // (14,27): error CS1525: Invalid expression term ','
            //             int[]? i1 => 3,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(14, 27),
            // (15,19): error CS1003: Syntax error, '=>' expected
            //             Task[]? t1 => 4,
            Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments("=>").WithLocation(15, 19),
            // (15,19): error CS1525: Invalid expression term '?'
            //             Task[]? t1 => 4,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(15, 19),
            // (15,28): error CS1003: Syntax error, ':' expected
            //             Task[]? t1 => 4,
            Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments(":").WithLocation(15, 28),
            // (15,28): error CS1525: Invalid expression term ','
            //             Task[]? t1 => 4,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(15, 28),
            // (16,13): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //             int[]? when x > 0 => 5,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(16, 13),
            // (17,13): error CS8116: It is not legal to use nullable type 'Task[]?' in a pattern; use the underlying type 'Task[]' instead.
            //             Task[]? when x > 0 => 6,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task[]?").WithArguments("System.Threading.Tasks.Task[]").WithLocation(17, 13),
            // (18,13): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //             int[]? i2 when x > 0 => 7,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(18, 13),
            // (19,13): error CS8116: It is not legal to use nullable type 'Task[]?' in a pattern; use the underlying type 'Task[]' instead.
            //             Task[]? t2 when x > 0 => 8,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task[]?").WithArguments("System.Threading.Tasks.Task[]").WithLocation(19, 13),
            // (20,14): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //             (int[]? when) when x > 0 => 9,
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(20, 14),
            // (21,14): error CS8116: It is not legal to use nullable type 'Task[]?' in a pattern; use the underlying type 'Task[]' instead.
            //             (Task[]? when) when x > 0 => 10
            Diagnostic(ErrorCode.ERR_PatternNullableType, "Task[]?").WithArguments("System.Threading.Tasks.Task[]").WithLocation(21, 14));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void PropertySubpatterns()
    {
        var source = """
            #nullable enable

            class E
            {
                public object? Prop { get; set; }
                public object? Prop2 { get; set; }
            }

            class C
            {
                void M(E e)
                {
                    if (e is { Prop: int? }) { }
                    if (e is { Prop: int? i1 }) { }
                    if (e is { Prop: int? or string? }) { }
                    if (e is { Prop: int? i2 or string? s1 }) { }
                    if (e is { Prop: int?, Prop2: string? }) { }
                    if (e is { Prop: int? i3, Prop2: string? s2 }) { }
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (13,26): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (e is { Prop: int? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(13, 26),
            // (14,26): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (e is { Prop: int? i1 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(14, 26),
            // (15,26): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (e is { Prop: int? or string? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(15, 26),
            // (15,34): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (e is { Prop: int? or string? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(15, 34),
            // (16,26): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (e is { Prop: int? i2 or string? s1 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(16, 26),
            // (16,31): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (e is { Prop: int? i2 or string? s1 }) { }
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "i2").WithLocation(16, 31),
            // (16,37): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (e is { Prop: int? i2 or string? s1 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(16, 37),
            // (16,45): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (e is { Prop: int? i2 or string? s1 }) { }
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "s1").WithLocation(16, 45),
            // (17,26): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (e is { Prop: int?, Prop2: string? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(17, 26),
            // (17,39): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (e is { Prop: int?, Prop2: string? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(17, 39),
            // (18,26): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
            //         if (e is { Prop: int? i3, Prop2: string? s2 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int?").WithArguments("int").WithLocation(18, 26),
            // (18,42): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (e is { Prop: int? i3, Prop2: string? s2 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(18, 42));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void PropertySubpatterns_Arrays()
    {
        var source = """
            #nullable enable

            class E
            {
                public object? Prop { get; set; }
                public object? Prop2 { get; set; }
            }

            class C
            {
                void M(E e)
                {
                    if (e is { Prop: int[]? }) { }
                    if (e is { Prop: int[]? i1 }) { }
                    if (e is { Prop: int[]? or string[]? }) { }
                    if (e is { Prop: int[]? i2 or string[]? s1 }) { }
                    if (e is { Prop: int[]?, Prop2: string[]? }) { }
                    if (e is { Prop: int[]? i3, Prop2: string[]? s2 }) { }
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (13,26): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (e is { Prop: int[]? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(13, 26),
            // (14,26): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (e is { Prop: int[]? i1 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(14, 26),
            // (15,26): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (e is { Prop: int[]? or string[]? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(15, 26),
            // (15,36): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (e is { Prop: int[]? or string[]? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(15, 36),
            // (16,26): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (e is { Prop: int[]? i2 or string[]? s1 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(16, 26),
            // (16,33): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (e is { Prop: int[]? i2 or string[]? s1 }) { }
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "i2").WithLocation(16, 33),
            // (16,39): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (e is { Prop: int[]? i2 or string[]? s1 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(16, 39),
            // (16,49): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
            //         if (e is { Prop: int[]? i2 or string[]? s1 }) { }
            Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "s1").WithLocation(16, 49),
            // (17,26): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (e is { Prop: int[]?, Prop2: string[]? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(17, 26),
            // (17,41): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (e is { Prop: int[]?, Prop2: string[]? }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(17, 41),
            // (18,26): error CS8116: It is not legal to use nullable type 'int[]?' in a pattern; use the underlying type 'int[]' instead.
            //         if (e is { Prop: int[]? i3, Prop2: string[]? s2 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "int[]?").WithArguments("int[]").WithLocation(18, 26),
            // (18,44): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (e is { Prop: int[]? i3, Prop2: string[]? s2 }) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(18, 44));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void ListPatterns()
    {
        var source = """
            #nullable enable

            class C
            {
                void M2(string s)
                {
                    if (s is [.. string?]) { }
                    if (s is [.. string? slice1]) { }
                    if (s is [.. string? slice2, ')']) { }
                }
            }
            """;

        var comp = CreateCompilationWithIndexAndRange(source);
        comp.VerifyDiagnostics(
            // (7,22): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (s is [.. string?]) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(7, 22),
            // (8,22): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (s is [.. string? slice1]) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(8, 22),
            // (9,22): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
            //         if (s is [.. string? slice2, ')']) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(9, 22));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
    public void ListPatterns_Arrays()
    {
        var source = """
            #nullable enable

            class C
            {
                void M(string[] s)
                {
                    if (s is [.. string[]?]) { }
                    if (s is [.. string[]? slice1]) { }
                    if (s is [.. string[]? slice2, ")"]) { }
                }
            }
            """;

        var comp = CreateCompilationWithIndexAndRange([source, TestSources.GetSubArray]);
        comp.VerifyDiagnostics(
            // (7,22): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (s is [.. string[]?]) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(7, 22),
            // (8,22): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (s is [.. string[]? slice1]) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(8, 22),
            // (9,22): error CS8116: It is not legal to use nullable type 'string[]?' in a pattern; use the underlying type 'string[]' instead.
            //         if (s is [.. string[]? slice2, ')']) { }
            Diagnostic(ErrorCode.ERR_PatternNullableType, "string[]?").WithArguments("string[]").WithLocation(9, 22));
    }
}
