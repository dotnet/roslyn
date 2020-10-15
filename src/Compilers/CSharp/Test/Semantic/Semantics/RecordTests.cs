﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class RecordTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilation(CSharpTestSource source)
            => CSharpTestBase.CreateCompilation(new[] { source, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.Regular9);

        private CompilationVerifier CompileAndVerify(
            CSharpTestSource src,
            string? expectedOutput = null,
            IEnumerable<MetadataReference>? references = null)
            => base.CompileAndVerify(
                new[] { src, IsExternalInitTypeDefinition },
                expectedOutput: expectedOutput,
                parseOptions: TestOptions.Regular9,
                references: references,
                // init-only is unverifiable
                verify: Verification.Skipped);

        [Fact, WorkItem(45900, "https://github.com/dotnet/roslyn/issues/45900")]
        public void RecordLanguageVersion()
        {
            var src1 = @"
class Point(int x, int y);
";
            var src2 = @"
record Point { }
";
            var src3 = @"
record Point(int x, int y);
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // error CS8805: Program using top-level statements must be an executable.
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable).WithLocation(1, 1),
                // (2,12): error CS1514: { expected
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 12),
                // (2,12): error CS1513: } expected
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 12),
                // (2,12): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "(int x, int y);").WithArguments("top-level statements", "9.0").WithLocation(2, 12),
                // (2,12): error CS8803: Top-level statements must precede namespace and type declarations.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(2, 12),
                // (2,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x, int y)").WithLocation(2, 12),
                // (2,13): error CS8185: A declaration is not allowed in this context.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(2, 13),
                // (2,13): error CS0165: Use of unassigned local variable 'x'
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(2, 13),
                // (2,20): error CS8185: A declaration is not allowed in this context.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(2, 20),
                // (2,20): error CS0165: Use of unassigned local variable 'y'
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(2, 20)
            );
            comp = CreateCompilation(src2, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (2,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record Point { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(2, 1),
                // (2,8): error CS0116: A namespace cannot directly contain members such as fields or methods
                // record Point { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "Point").WithLocation(2, 8),
                // (2,8): error CS0548: '<invalid-global-code>.Point': property or indexer must have at least one accessor
                // record Point { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "Point").WithArguments("<invalid-global-code>.Point").WithLocation(2, 8)
            );
            comp = CreateCompilation(src3, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // error CS8805: Program using top-level statements must be an executable.
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable).WithLocation(1, 1),
                // (2,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // record Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "record Point(int x, int y);").WithArguments("top-level statements", "9.0").WithLocation(2, 1),
                // (2,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(2, 1),
                // (2,8): error CS8112: Local function 'Point(int, int)' must declare a body because it is not marked 'static extern'.
                // record Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "Point").WithArguments("Point(int, int)").WithLocation(2, 8),
                // (2,8): warning CS8321: The local function 'Point' is declared but never used
                // record Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Point").WithArguments("Point").WithLocation(2, 8)
            );

            comp = CreateCompilation(src1);
            comp.VerifyDiagnostics(
                // error CS8805: Program using top-level statements must be an executable.
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable).WithLocation(1, 1),
                // (2,12): error CS1514: { expected
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 12),
                // (2,12): error CS1513: } expected
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 12),
                // (2,12): error CS8803: Top-level statements must precede namespace and type declarations.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(2, 12),
                // (2,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x, int y)").WithLocation(2, 12),
                // (2,13): error CS8185: A declaration is not allowed in this context.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(2, 13),
                // (2,13): error CS0165: Use of unassigned local variable 'x'
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(2, 13),
                // (2,20): error CS8185: A declaration is not allowed in this context.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(2, 20),
                // (2,20): error CS0165: Use of unassigned local variable 'y'
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(2, 20)
            );
            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src3);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(45900, "https://github.com/dotnet/roslyn/issues/45900")]
        public void RecordLanguageVersion_Nested()
        {
            var src1 = @"
class C
{
    class Point(int x, int y);
}
";
            var src2 = @"
class D
{
    record Point { }
}
";
            var src3 = @"
class E
{
    record Point(int x, int y);
}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,16): error CS1514: { expected
                //     class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(4, 16),
                // (4,16): error CS1513: } expected
                //     class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(4, 16),
                // (4,30): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 30),
                // (4,30): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 30)
                );

            comp = CreateCompilation(src2, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,5): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                //     record Point { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(4, 5),
                // (4,12): error CS0548: 'D.Point': property or indexer must have at least one accessor
                //     record Point { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "Point").WithArguments("D.Point").WithLocation(4, 12)
                );

            comp = CreateCompilation(src3, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,5): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                //     record Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(4, 5),
                // (4,12): error CS0501: 'E.Point(int, int)' must declare a body because it is not marked abstract, extern, or partial
                //     record Point(int x, int y);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "Point").WithArguments("E.Point(int, int)").WithLocation(4, 12)
                );

            comp = CreateCompilation(src1);
            comp.VerifyDiagnostics(
                // (4,16): error CS1514: { expected
                //     class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(4, 16),
                // (4,16): error CS1513: } expected
                //     class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(4, 16),
                // (4,30): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 30),
                // (4,30): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 30)
                );

            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src3);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(46123, "https://github.com/dotnet/roslyn/issues/46123")]
        public void IncompletePositionalRecord()
        {
            string source = @"
public record A(int i,) { }
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,23): error CS1031: Type expected
                // public record A(int i,) { }
                Diagnostic(ErrorCode.ERR_TypeExpected, ")").WithLocation(2, 23),
                // (2,23): error CS1001: Identifier expected
                // public record A(int i,) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(2, 23)
                );

            var expectedMembers = new[]
            {
                "System.Type A.EqualityContract { get; }",
                "System.Int32 A.i { get; init; }",
                "? A. { get; init; }"
            };
            AssertEx.Equal(expectedMembers,
                comp.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<PropertySymbol>().ToTestDisplayStrings());

            AssertEx.Equal(new[] { "A..ctor(System.Int32 i, ? )", "A..ctor(A original)" },
                comp.GetMember<NamedTypeSymbol>("A").Constructors.ToTestDisplayStrings());

            var primaryCtor = comp.GetMember<NamedTypeSymbol>("A").Constructors.First();
            Assert.Equal("A..ctor(System.Int32 i, ? )", primaryCtor.ToTestDisplayString());
            Assert.IsType<ParameterSyntax>(primaryCtor.Parameters[1].DeclaringSyntaxReferences.Single().GetSyntax());
        }

        [Fact, WorkItem(46123, "https://github.com/dotnet/roslyn/issues/46123")]
        public void IncompletePositionalRecord_WithTrivia()
        {
            string source = @"
public record A(int i, // A
    // B
    , /* C */ ) { }
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,23): error CS1031: Type expected
                // public record A(int i, // A
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(2, 23),
                // (2,23): error CS1001: Identifier expected
                // public record A(int i, // A
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(2, 23),
                // (4,15): error CS1031: Type expected
                //     , /* C */ ) { }
                Diagnostic(ErrorCode.ERR_TypeExpected, ")").WithLocation(4, 15),
                // (4,15): error CS1001: Identifier expected
                //     , /* C */ ) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(4, 15),
                // (4,15): error CS0102: The type 'A' already contains a definition for ''
                //     , /* C */ ) { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "").WithArguments("A", "").WithLocation(4, 15)
                );

            var primaryCtor = comp.GetMember<NamedTypeSymbol>("A").Constructors.First();
            Assert.Equal("A..ctor(System.Int32 i, ? , ? )", primaryCtor.ToTestDisplayString());
            Assert.IsType<ParameterSyntax>(primaryCtor.Parameters[0].DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.IsType<ParameterSyntax>(primaryCtor.Parameters[1].DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.IsType<ParameterSyntax>(primaryCtor.Parameters[2].DeclaringSyntaxReferences.Single().GetSyntax());
        }

        [Fact, WorkItem(46123, "https://github.com/dotnet/roslyn/issues/46123")]
        public void IncompleteConstructor()
        {
            string source = @"
public class C
{
    C(int i, ) { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,14): error CS1031: Type expected
                //     C(int i, ) { }
                Diagnostic(ErrorCode.ERR_TypeExpected, ")").WithLocation(4, 14),
                // (4,14): error CS1001: Identifier expected
                //     C(int i, ) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(4, 14)
                );

            var ctor = comp.GetMember<NamedTypeSymbol>("C").Constructors.Single();
            Assert.Equal("C..ctor(System.Int32 i, ? )", ctor.ToTestDisplayString());
            Assert.IsType<ParameterSyntax>(ctor.Parameters[1].DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(0, ctor.Parameters[1].Locations.Single().SourceSpan.Length);
        }

        [Fact, WorkItem(46123, "https://github.com/dotnet/roslyn/issues/46123")]
        public void IncompletePositionalRecord_WithType()
        {
            string source = @"
public record A(int i, int ) { }
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,28): error CS1001: Identifier expected
                // public record A(int i, int ) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(2, 28)
                );

            var expectedMembers = new[]
            {
                "System.Type A.EqualityContract { get; }",
                "System.Int32 A.i { get; init; }",
                "System.Int32 A. { get; init; }"
            };
            AssertEx.Equal(expectedMembers,
                comp.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<PropertySymbol>().ToTestDisplayStrings());

            var ctor = comp.GetMember<NamedTypeSymbol>("A").Constructors[0];
            Assert.Equal("A..ctor(System.Int32 i, System.Int32 )", ctor.ToTestDisplayString());
            Assert.IsType<ParameterSyntax>(ctor.Parameters[1].DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.Equal(0, ctor.Parameters[1].Locations.Single().SourceSpan.Length);
        }

        [Fact, WorkItem(46123, "https://github.com/dotnet/roslyn/issues/46123")]
        public void IncompletePositionalRecord_WithTwoTypes()
        {
            string source = @"
public record A(int, string ) { }
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): error CS1001: Identifier expected
                // public record A(int, string ) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(2, 20),
                // (2,29): error CS1001: Identifier expected
                // public record A(int, string ) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(2, 29),
                // (2,29): error CS0102: The type 'A' already contains a definition for ''
                // public record A(int, string ) { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "").WithArguments("A", "").WithLocation(2, 29)
                );

            var expectedMembers = new[]
            {
                "System.Type A.EqualityContract { get; }",
                "System.Int32 A. { get; init; }",
                "System.String A. { get; init; }"
            };
            AssertEx.Equal(expectedMembers,
                comp.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<PropertySymbol>().ToTestDisplayStrings());

            AssertEx.Equal(new[] { "A..ctor(System.Int32 , System.String )", "A..ctor(A original)" },
                comp.GetMember<NamedTypeSymbol>("A").Constructors.ToTestDisplayStrings());

            Assert.IsType<ParameterSyntax>(comp.GetMember<NamedTypeSymbol>("A").Constructors[0].Parameters[1].DeclaringSyntaxReferences.Single().GetSyntax());
        }

        [Fact, WorkItem(46123, "https://github.com/dotnet/roslyn/issues/46123")]
        public void IncompletePositionalRecord_WithTwoTypes_SameType()
        {
            string source = @"
public record A(int, int ) { }
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): error CS1001: Identifier expected
                // public record A(int, int ) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(2, 20),
                // (2,26): error CS1001: Identifier expected
                // public record A(int, int ) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(2, 26),
                // (2,26): error CS0102: The type 'A' already contains a definition for ''
                // public record A(int, int ) { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "").WithArguments("A", "").WithLocation(2, 26)
                );

            var expectedMembers = new[]
            {
                "System.Type A.EqualityContract { get; }",
                "System.Int32 A. { get; init; }",
                "System.Int32 A. { get; init; }"
            };
            AssertEx.Equal(expectedMembers,
                comp.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<PropertySymbol>().ToTestDisplayStrings());

            AssertEx.Equal(new[] { "A..ctor(System.Int32 , System.Int32 )", "A..ctor(A original)" },
                comp.GetMember<NamedTypeSymbol>("A").Constructors.ToTestDisplayStrings());

            Assert.IsType<ParameterSyntax>(comp.GetMember<NamedTypeSymbol>("A").Constructors[0].Parameters[1].DeclaringSyntaxReferences.Single().GetSyntax());
        }

        [Fact, WorkItem(46123, "https://github.com/dotnet/roslyn/issues/46123")]
        public void IncompletePositionalRecord_WithTwoTypes_WithTrivia()
        {
            string source = @"
public record A(int // A
    // B
    , int /* C */) { }
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): error CS1001: Identifier expected
                // public record A(int // A
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(2, 20),
                // (4,18): error CS1001: Identifier expected
                //     , int /* C */) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(4, 18),
                // (4,18): error CS0102: The type 'A' already contains a definition for ''
                //     , int /* C */) { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "").WithArguments("A", "").WithLocation(4, 18)
                );

            var ctor = comp.GetMember<NamedTypeSymbol>("A").Constructors[0];
            Assert.IsType<ParameterSyntax>(ctor.Parameters[0].DeclaringSyntaxReferences.Single().GetSyntax());
            Assert.IsType<ParameterSyntax>(ctor.Parameters[1].DeclaringSyntaxReferences.Single().GetSyntax());
        }

        [Fact, WorkItem(46083, "https://github.com/dotnet/roslyn/issues/46083")]
        public void IncompletePositionalRecord_SingleParameter()
        {
            string source = @"
record A(x)
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,10): error CS0246: The type or namespace name 'x' could not be found (are you missing a using directive or an assembly reference?)
                // record A(x)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("x").WithLocation(2, 10),
                // (2,11): error CS1001: Identifier expected
                // record A(x)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(2, 11),
                // (2,12): error CS1514: { expected
                // record A(x)
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(2, 12),
                // (2,12): error CS1513: } expected
                // record A(x)
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(2, 12)
                );
        }

        [Fact]
        public void TestInExpressionTree()
        {
            var source = @"
using System;
using System.Linq.Expressions;
public record C(int i)
{
    public static void M()
    {
        Expression<Func<C, C>> expr = c => c with { i = 5 };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,44): error CS8849: An expression tree may not contain a with-expression.
                //         Expression<Func<C, C>> expr = c => c with { i = 5 };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsWithExpression, "c with { i = 5 }").WithLocation(8, 44)
                );
        }

        [Fact]
        public void PartialRecord_MixedWithClass()
        {
            var src = @"
partial record C(int X, int Y)
{
}
partial class C
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,15): error CS0261: Partial declarations of 'C' must be all classes, all records, all structs, or all interfaces
                // partial class C
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C").WithArguments("C").WithLocation(5, 15)
                );
        }

        [Fact]
        public void PartialRecord_ParametersInScopeOfBothParts()
        {
            var src = @"
var c = new C(2);
System.Console.Write((c.P1, c.P2));

public partial record C(int X)
{
    public int P1 { get; set; } = X;
}
public partial record C
{
    public int P2 { get; set; } = X;
}
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            CompileAndVerify(comp, expectedOutput: "(2, 2)", verify: Verification.Skipped /* init-only */).VerifyDiagnostics();
        }

        [Fact]
        public void RecordInsideGenericType()
        {
            var src = @"
var c = new C<int>.Nested(2);
System.Console.Write(c.T);

public class C<T>
{
    public record Nested(T T);
}
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void RecordProperties_01()
        {
            var src = @"
using System;
record C(int X, int Y)
{
    int Z = 123;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
        Console.WriteLine(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2
123").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor(int, int)", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   123
  IL_0011:  stfld      ""int C.Z""
  IL_0016:  ldarg.0
  IL_0017:  call       ""object..ctor()""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void RecordProperties_02()
        {
            var src = @"
using System;
record C(int X, int Y)
{
    public C(int a, int b)
    {
    }

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }

    private int X1 = X;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                //     public C(int a, int b)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(5, 12),
                // (5,12): error CS8862: A constructor declared in a record with parameter list must have 'this' constructor initializer.
                //     public C(int a, int b)
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(5, 12),
                // (11,21): error CS0121: The call is ambiguous between the following methods or properties: 'C.C(int, int)' and 'C.C(int, int)'
                //         var c = new C(1, 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "C").WithArguments("C.C(int, int)", "C.C(int, int)").WithLocation(11, 21)
                );
        }

        [Fact]
        public void RecordProperties_03()
        {
            var src = @"
using System;
record C(int X, int Y)
{
    public int X { get; }

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: @"
0
2").VerifyDiagnostics();
        }

        [Fact]
        public void RecordProperties_04()
        {
            var src = @"
using System;
record C(int X, int Y)
{
    public int X { get; } = 3;

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: @"
3
2").VerifyDiagnostics();
        }

        [Fact]
        public void RecordProperties_05()
        {
            var src = @"
record C(int X, int X)
{
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,21): error CS0100: The parameter name 'X' is a duplicate
                // record C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "X").WithArguments("X").WithLocation(2, 21),
                // (2,21): error CS0102: The type 'C' already contains a definition for 'X'
                // record C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(2, 21)
            );

            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Int32 C.X { get; init; }",
                "System.Int32 C.X { get; init; }"
            };
            AssertEx.Equal(expectedMembers,
                comp.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<PropertySymbol>().ToTestDisplayStrings());
        }

        [Fact]
        public void RecordProperties_06()
        {
            var src = @"
record C(int X, int Y)
{
    public void get_X() { }
    public void set_X() { }
    int get_Y(int value) => value;
    int set_Y(int value) => value;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,14): error CS0082: Type 'C' already reserves a member called 'get_X' with the same parameter types
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MemberReserved, "X").WithArguments("get_X", "C").WithLocation(2, 14),
                // (2,21): error CS0082: Type 'C' already reserves a member called 'set_Y' with the same parameter types
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MemberReserved, "Y").WithArguments("set_Y", "C").WithLocation(2, 21));

            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "C..ctor(System.Int32 X, System.Int32 Y)",
                "System.Type C.EqualityContract.get",
                "System.Type C.EqualityContract { get; }",
                "System.Int32 C.<X>k__BackingField",
                "System.Int32 C.X.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.X.init",
                "System.Int32 C.X { get; init; }",
                "System.Int32 C.<Y>k__BackingField",
                "System.Int32 C.Y.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.Y.init",
                "System.Int32 C.Y { get; init; }",
                "void C.get_X()",
                "void C.set_X()",
                "System.Int32 C.get_Y(System.Int32 value)",
                "System.Int32 C.set_Y(System.Int32 value)",
                "System.String C.ToString()",
                "System.Boolean C." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean C.op_Inequality(C? r1, C? r2)",
                "System.Boolean C.op_Equality(C? r1, C? r2)",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? obj)",
                "System.Boolean C.Equals(C? other)",
                "C C." + WellKnownMemberNames.CloneMethodName + "()",
                "C..ctor(C original)",
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void RecordProperties_07()
        {
            var comp = CreateCompilation(@"
record C1(object P, object get_P);
record C2(object get_P, object P);");
            comp.VerifyDiagnostics(
                // (2,18): error CS0102: The type 'C1' already contains a definition for 'get_P'
                // record C1(object P, object get_P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C1", "get_P").WithLocation(2, 18),
                // (3,32): error CS0102: The type 'C2' already contains a definition for 'get_P'
                // record C2(object get_P, object P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C2", "get_P").WithLocation(3, 32)
            );
        }

        [Fact]
        public void RecordProperties_08()
        {
            var comp = CreateCompilation(@"
record C1(object O1)
{
    public object O1 { get; } = O1;
    public object O2 { get; } = O1;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecordProperties_09()
        {
            var src =
@"record C(object P1, object P2, object P3, object P4)
{
    class P1 { }
    object P2 = 2;
    int P3(object o) => 3;
    int P4<T>(T t) => 4;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (1,17): error CS0102: The type 'C' already contains a definition for 'P1'
                // record C(object P1, object P2, object P3, object P4)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P1").WithArguments("C", "P1").WithLocation(1, 17),
                // (4,12): error CS0102: The type 'C' already contains a definition for 'P2'
                //     object P2 = 2;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P2").WithArguments("C", "P2").WithLocation(4, 12),
                // (5,9): error CS0102: The type 'C' already contains a definition for 'P3'
                //     int P3(object o) => 3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P3").WithArguments("C", "P3").WithLocation(5, 9),
                // (6,9): error CS0102: The type 'C' already contains a definition for 'P4'
                //     int P4<T>(T t) => 4;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P4").WithArguments("C", "P4").WithLocation(6, 9)
                );
        }

        [Fact]
        public void RecordProperties_10()
        {
            var src =
@"record C(object P)
{
    const int P = 4;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,15): error CS0102: The type 'C' already contains a definition for 'P'
                //     const int P = 4;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(3, 15)
                );
        }

        [Fact]
        public void EmptyRecord_01()
        {
            var src = @"
record C();

class Program
{
    static void Main()
    {
        var x = new C();
        var y = new C();
        System.Console.WriteLine(x == y);
    }
}
";

            var verifier = CompileAndVerify(src, expectedOutput: "True");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("C..ctor()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}
");

            var comp = (CSharpCompilation)verifier.Compilation;

            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "C..ctor()",
                "System.Type C.EqualityContract.get",
                "System.Type C.EqualityContract { get; }",
                "System.String C.ToString()",
                "System.Boolean C." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean C.op_Inequality(C? r1, C? r2)",
                "System.Boolean C.op_Equality(C? r1, C? r2)",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? obj)",
                "System.Boolean C.Equals(C? other)",
                "C C." + WellKnownMemberNames.CloneMethodName + "()",
                "C..ctor(C original)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void EmptyRecord_02()
        {
            var src = @"
record C()
{
    C(int x)
    {}
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,5): error CS8862: A constructor declared in a record with parameter list must have 'this' constructor initializer.
                //     C(int x)
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C", isSuppressed: false).WithLocation(4, 5)
                );
        }

        [Fact]
        public void EmptyRecord_03()
        {
            var src = @"
record B
{
    public B(int x)
    {
        System.Console.WriteLine(x);
    }
}

record C() :  B(12)
{
    C(int x) : this()
    {}
}

class Program
{
    static void Main()
    {
        _ = new C();
    }
}
";

            var verifier = CompileAndVerify(src, expectedOutput: "12");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("C..ctor()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   12
  IL_0003:  call       ""B..ctor(int)""
  IL_0008:  ret
}
");
        }

        [Fact(Skip = "record struct")]
        public void StructRecord1()
        {
            var src = @"
data struct Point(int X, int Y);";

            var verifier = CompileAndVerify(src).VerifyDiagnostics();
            verifier.VerifyIL("Point.Equals(object)", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Point V_0)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""Point""
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  ldc.i4.0
  IL_0009:  ret
  IL_000a:  ldarg.0
  IL_000b:  ldarg.1
  IL_000c:  unbox.any  ""Point""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool Point.Equals(in Point)""
  IL_0019:  ret
}");
            verifier.VerifyIL("Point.Equals(in Point)", @"
{
  // Code size       49 (0x31)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""int Point.<X>k__BackingField""
  IL_000b:  ldarg.1
  IL_000c:  ldfld      ""int Point.<X>k__BackingField""
  IL_0011:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0016:  brfalse.s  IL_002f
  IL_0018:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001d:  ldarg.0
  IL_001e:  ldfld      ""int Point.<Y>k__BackingField""
  IL_0023:  ldarg.1
  IL_0024:  ldfld      ""int Point.<Y>k__BackingField""
  IL_0029:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_002e:  ret
  IL_002f:  ldc.i4.0
  IL_0030:  ret
}");
        }

        [Fact(Skip = "record struct")]
        public void StructRecord2()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public static void Main()
    {
        var s1 = new S(0, 1);
        var s2 = new S(0, 1);
        Console.WriteLine(s1.X);
        Console.WriteLine(s1.Y);
        Console.WriteLine(s1.Equals(s2));
        Console.WriteLine(s1.Equals(new S(1, 0)));
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0
1
True
False").VerifyDiagnostics();
        }

        [Fact(Skip = "record struct")]
        public void StructRecord3()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public bool Equals(S s) => false;
    public static void Main()
    {
        var s1 = new S(0, 1);
        Console.WriteLine(s1.Equals(s1));
        Console.WriteLine(s1.Equals(in s1));
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"False
True").VerifyDiagnostics();

            verifier.VerifyIL("S.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (S V_0) //s1
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""S..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloc.0
  IL_000c:  call       ""bool S.Equals(S)""
  IL_0011:  call       ""void System.Console.WriteLine(bool)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       ""bool S.Equals(in S)""
  IL_001f:  call       ""void System.Console.WriteLine(bool)""
  IL_0024:  ret
}");
        }

        [Fact(Skip = "record struct")]
        public void StructRecord4()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public override bool Equals(object o)
    {
        Console.WriteLine(""obj"");
        return true;
    }
    public bool Equals(in S s)
    {
        Console.WriteLine(""s"");
        return true;
    }
    public static void Main()
    {
        var s1 = new S(0, 1);
        s1.Equals((object)s1);
        s1.Equals(s1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"obj
s").VerifyDiagnostics();
        }

        [Fact(Skip = "record struct")]
        public void StructRecord5()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public bool Equals(in S s)
    {
        Console.WriteLine(""s"");
        return true;
    }
    public static void Main()
    {
        var s1 = new S(0, 1);
        s1.Equals((object)s1);
        s1.Equals(s1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"s
s").VerifyDiagnostics();
        }

        [Fact(Skip = "record struct")]
        public void StructRecordDefaultCtor()
        {
            const string src = @"
public data struct S(int X);";
            const string src2 = @"
class C
{
    public S M() => new S();
}";
            var comp = CreateCompilation(src + src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src);
            var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr1()
        {
            var src = @"
record C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        _ = Main() with { };
        _ = default with { };
        _ = null with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,13): error CS8802: The receiver of a `with` expression must have a valid non-void type.
                //         _ = Main() with { };
                Diagnostic(ErrorCode.ERR_InvalidWithReceiverType, "Main()").WithLocation(7, 13),
                // (8,13): error CS8716: There is no target type for the default literal.
                //         _ = default with { };
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 13),
                // (9,13): error CS8802: The receiver of a `with` expression must have a valid non-void type.
                //         _ = null with { };
                Diagnostic(ErrorCode.ERR_InvalidWithReceiverType, "null").WithLocation(9, 13)
            );
        }

        [Fact]
        public void WithExpr2()
        {
            var src = @"
using System;
record C(int X)
{
    public static void Main()
    {
        var c1 = new C(1);
        c1 = c1 with { };
        var c2 = c1 with { X = 11 };
        Console.WriteLine(c1.X);
        Console.WriteLine(c2.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"1
11").VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr3()
        {
            var src = @"
record C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            var root = comp.SyntaxTrees[0].GetRoot();
            var main = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            Assert.Equal("Main", main.Identifier.ToString());
            VerifyFlowGraph(comp, main, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C(0)')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C(0)')
              Right:
                IObjectCreationOperation (Constructor: C..ctor(System.Int32 X)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0)')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '0')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = c with { };')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'c = c with { }')
                  Left:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                  Right:
                    IInvocationOperation (virtual C C." + WellKnownMemberNames.CloneMethodName + @"()) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'c with { }')
                      Instance Receiver:
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                      Arguments(0)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr4()
        {
            var src = @"
class B
{
    public B Clone() => null;
}
record C(int X) : B
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
    }

    public new C Clone() => null;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr6()
        {
            var src = @"
record B
{
    public int X { get; init; }
}
record C : B
{
    public static void Main()
    {
        var c = new C();
        c = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr7()
        {
            var src = @"
record B
{
    public int X { get; }
}

record C : B
{
    public new int X { get; init; }
    public static void Main()
    {
        var c = new C();
        B b = c;
        b = b with { X = 0 };
        var b2 = c with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (14,22): error CS0200: Property or indexer 'B.X' cannot be assigned to -- it is read only
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "X").WithArguments("B.X").WithLocation(14, 22)
            );
        }

        [Fact]
        public void WithExpr8()
        {
            var src = @"
record B
{
    public int X { get; }
}
record C : B
{
    public string Y { get; }
    public static void Main()
    {
        var c = new C();
        B b = c;
        b = b with { };
        b = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr9()
        {
            var src = @"
record C(int X)
{
    public string Clone() => null;
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS8859: Members named 'Clone' are disallowed in records.
                //     public string Clone() => null;
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(4, 19)
            );
        }

        [Fact]
        public void WithExpr11()
        {
            var src = @"

record C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { X = """"};
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         c = c with { X = ""};
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(8, 26)
            );
        }

        [Fact]
        public void WithExpr12()
        {
            var src = @"
using System;
record C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        Console.WriteLine(c.X);
        c = c with { X = 5 };
        Console.WriteLine(c.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0
5").VerifyDiagnostics();

            verifier.VerifyIL("C.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""C..ctor(int)""
  IL_0006:  dup
  IL_0007:  callvirt   ""int C.X.get""
  IL_000c:  call       ""void System.Console.WriteLine(int)""
  IL_0011:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0016:  dup
  IL_0017:  ldc.i4.5
  IL_0018:  callvirt   ""void C.X.init""
  IL_001d:  callvirt   ""int C.X.get""
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ret
}");
        }

        [Fact]
        public void WithExpr13()
        {
            var src = @"
using System;

record C(int X, int Y)
{
    public override string ToString() => X + "" "" + Y;
    public static void Main()
    {
        var c = new C(0, 1);
        Console.WriteLine(c);
        c = c with { X = 5 };
        Console.WriteLine(c);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0 1
5 1").VerifyDiagnostics();

            verifier.VerifyIL("C.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""C..ctor(int, int)""
  IL_0007:  dup
  IL_0008:  call       ""void System.Console.WriteLine(object)""
  IL_000d:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0012:  dup
  IL_0013:  ldc.i4.5
  IL_0014:  callvirt   ""void C.X.init""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void WithExpr14()
        {
            var src = @"
using System;

record C(int X, int Y)
{
    public override string ToString() => X + "" "" + Y;
    public static void Main()
    {
        var c = new C(0, 1);
        Console.WriteLine(c);
        c = c with { X = 5 };
        Console.WriteLine(c);
        c = c with { Y = 2 };
        Console.WriteLine(c);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0 1
5 1
5 2").VerifyDiagnostics();

            verifier.VerifyIL("C.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""C..ctor(int, int)""
  IL_0007:  dup
  IL_0008:  call       ""void System.Console.WriteLine(object)""
  IL_000d:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0012:  dup
  IL_0013:  ldc.i4.5
  IL_0014:  callvirt   ""void C.X.init""
  IL_0019:  dup
  IL_001a:  call       ""void System.Console.WriteLine(object)""
  IL_001f:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0024:  dup
  IL_0025:  ldc.i4.2
  IL_0026:  callvirt   ""void C.Y.init""
  IL_002b:  call       ""void System.Console.WriteLine(object)""
  IL_0030:  ret
}");
        }

        [Fact]
        public void WithExpr15()
        {
            var src = @"

record C(int X, int Y)
{
    public static void Main()
    {
        var c = new C(0, 0);
        c = c with { = 5 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,22): error CS1525: Invalid expression term '='
                //         c = c with { = 5 };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(8, 22)
            );
        }

        [Fact]
        public void WithExpr16()
        {
            var src = @"

record C(int X, int Y)
{
    public static void Main()
    {
        var c = new C(0, 0);
        c = c with { X = };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): error CS1525: Invalid expression term '}'
                //         c = c with { X = };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(8, 26)
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr17()
        {
            var src = @"
record B
{
    public int X { get; }
    private B Clone() => null;
}
record C(int X) : B
{
    public static void Main()
    {
        var b = new B();
        b = b with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8803: The 'with' expression requires the receiver type 'B' to have a single accessible non-inherited instance method named "Clone".
                //         b = b with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "b").WithArguments("B").WithLocation(12, 13)
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr18()
        {
            var src = @"
class B
{
    public int X { get; }
    protected B Clone() => null;
}
record C(int X) : B
{
    public static void Main()
    {
        var b = new B();
        b = b with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8803: The receiver type 'B' does not have an accessible parameterless instance method named "Clone".
                //         b = b with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "b").WithArguments("B").WithLocation(12, 13)
            );
        }

        [Fact]
        public void WithExpr19()
        {
            var src = @"
class B
{
    public int X { get; }
    protected B Clone() => null;
}
record C(int X)
{
    public static void Main()
    {
        var b = new B();
        b = b with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8803: The 'with' expression requires the receiver type 'B' to have a single accessible non-inherited instance method named "Clone".
                //         b = b with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "b").WithArguments("B").WithLocation(12, 13)
            );
        }

        [Fact]
        public void WithExpr20()
        {
            var src = @"
using System;
record C
{
    public event Action X;
    public static void Main()
    {
        var c = new C();
        c = c with { X = null };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,25): warning CS0067: The event 'C.X' is never used
                //     public event Action X;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "X").WithArguments("C.X").WithLocation(5, 25)
            );
        }

        [Fact]
        public void WithExpr21()
        {
            var src = @"
record B
{
    public class X { }
}
class C
{
    public static void Main()
    {
        var b = new B();
        b = b with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,22): error CS0572: 'X': cannot reference a type through an expression; try 'B.X' instead
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_BadTypeReference, "X").WithArguments("X", "B.X").WithLocation(11, 22),
                // (11,22): error CS1913: Member 'X' cannot be initialized. It is not a field or property.
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "X").WithArguments("X").WithLocation(11, 22)
            );
        }

        [Fact]
        public void WithExpr22()
        {
            var src = @"
record B
{
    public int X = 0;
}
class C
{
    public static void Main()
    {
        var b = new B();
        b = b with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr23()
        {
            var src = @"
class B
{
    public int X = 0;
    public B Clone() => null;
}
record C(int X)
{
    public static void Main()
    {
        var b = new B();
        b = b with { Y = 2 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8858: The receiver type 'B' is not a valid record type.
                //         b = b with { Y = 2 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "b").WithArguments("B").WithLocation(12, 13),
                // (12,22): error CS0117: 'B' does not contain a definition for 'Y'
                //         b = b with { Y = 2 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Y").WithArguments("B", "Y").WithLocation(12, 22)
            );
        }

        [Fact]
        public void WithExpr24_Dynamic()
        {
            var src = @"
record C(int X)
{
    public static void Main()
    {
        dynamic c = new C(1);
        var x = c with { X = 2 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,17): error CS8858: The receiver type 'dynamic' is not a valid record type.
                //         var x = c with { X = 2 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("dynamic").WithLocation(7, 17)
                );
        }

        [Fact, WorkItem(46427, "https://github.com/dotnet/roslyn/issues/46427"), WorkItem(46249, "https://github.com/dotnet/roslyn/issues/46249")]
        public void WithExpr25_TypeParameterWithRecordConstraint()
        {
            var src = @"
record R(int X);

class C
{
    public static T M<T>(T t) where T : R
    {
        return t with { X = 2 };
    }

    static void Main()
    {
        System.Console.Write(M(new R(-1)).X);
    }
}";

            var verifier = CompileAndVerify(src, expectedOutput: "2").VerifyDiagnostics();
            verifier.VerifyIL("C.M<T>(T)", @"
{
  // Code size       29 (0x1d)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  callvirt   ""R R.<Clone>$()""
  IL_000b:  unbox.any  ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  ldc.i4.2
  IL_0017:  callvirt   ""void R.X.init""
  IL_001c:  ret
}
");
        }

        [Fact, WorkItem(46427, "https://github.com/dotnet/roslyn/issues/46427"), WorkItem(46249, "https://github.com/dotnet/roslyn/issues/46249")]
        public void WithExpr26_TypeParameterWithRecordAndInterfaceConstraint()
        {
            var src = @"
record R
{
    public int X { get; set; }
}
interface I { int Property { get; set; } }

record T : R, I
{
    public int Property { get; set; }
}

class C
{
    public static T M<T>(T t) where T : R, I
    {
        return t with { X = 2, Property = 3 };
    }

    static void Main()
    {
        System.Console.WriteLine(M(new T()).X);
        System.Console.WriteLine(M(new T()).Property);
    }
}";

            var verifier = CompileAndVerify(
                new[] { src, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.Regular9,
                verify: Verification.Passes,
                expectedOutput:
@"2
3").VerifyDiagnostics();

            verifier.VerifyIL("C.M<T>(T)", @"
{
  // Code size       41 (0x29)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  callvirt   ""R R.<Clone>$()""
  IL_000b:  unbox.any  ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  ldc.i4.2
  IL_0017:  callvirt   ""void R.X.set""
  IL_001c:  dup
  IL_001d:  box        ""T""
  IL_0022:  ldc.i4.3
  IL_0023:  callvirt   ""void I.Property.set""
  IL_0028:  ret
}
");
        }

        [Fact]
        public void WithExpr27_InExceptionFilter()
        {
            var src = @"
var r = new R(1);

try
{
    throw new System.Exception();
}
catch (System.Exception) when ((r = r with { X = 2 }).X == 2)
{
    System.Console.Write(""RAN "");
    System.Console.Write(r.X);
}

record R(int X);
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN 2", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void WithExpr28_WithAwait()
        {
            var src = @"
var r = new R(1);
r = r with { X = await System.Threading.Tasks.Task.FromResult(42) };
System.Console.Write(r.X);

record R(int X);
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped /* init-only */);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var x = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Last().Left;
            Assert.Equal("X", x.ToString());
            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal("System.Int32 R.X { get; init; }", symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(46465, "https://github.com/dotnet/roslyn/issues/46465")]
        public void WithExpr29_DisallowedAsExpressionStatement()
        {
            var src = @"
record R(int X)
{
    void M()
    {
        var r = new R(1);
        r with { X = 2 };
    }
}
";
            // Note: we didn't parse the `with` as a `with` expression, but as a broken local declaration
            // Tracked by https://github.com/dotnet/roslyn/issues/46465
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS0118: 'r' is a variable but is used like a type
                //         r with { X = 2 };
                Diagnostic(ErrorCode.ERR_BadSKknown, "r").WithArguments("r", "variable", "type").WithLocation(7, 9),
                // (7,11): warning CS0168: The variable 'with' is declared but never used
                //         r with { X = 2 };
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "with").WithArguments("with").WithLocation(7, 11),
                // (7,16): error CS1002: ; expected
                //         r with { X = 2 };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(7, 16),
                // (7,18): error CS8852: Init-only property or indexer 'R.X' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         r with { X = 2 };
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "X").WithArguments("R.X").WithLocation(7, 18),
                // (7,24): error CS1002: ; expected
                //         r with { X = 2 };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(7, 24)
                );
        }

        [Fact]
        public void WithExpr30_TypeParameterNoConstraint()
        {
            var src = @"
class C
{
    public static void M<T>(T t)
    {
        _ = t with { X = 2 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (6,13): error CS8858: The receiver type 'T' is not a valid record type.
                //         _ = t with { X = 2 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "t").WithArguments("T").WithLocation(6, 13),
                // (6,22): error CS0117: 'T' does not contain a definition for 'X'
                //         _ = t with { X = 2 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("T", "X").WithLocation(6, 22)
                );
        }

        [Fact]
        public void WithExpr31_TypeParameterWithInterfaceConstraint()
        {
            var src = @"
interface I { int Property { get; set; } }

class C
{
    public static void M<T>(T t) where T : I
    {
        _ = t with { X = 2, Property = 3 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS8858: The receiver type 'T' is not a valid record type.
                //         _ = t with { X = 2, Property = 3 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "t").WithArguments("T").WithLocation(8, 13),
                // (8,22): error CS0117: 'T' does not contain a definition for 'X'
                //         _ = t with { X = 2, Property = 3 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("T", "X").WithLocation(8, 22)
                );
        }

        [Fact]
        public void WithExpr32_TypeParameterWithInterfaceConstraint()
        {
            var ilSource = @"
.class interface public auto ansi abstract I
{
    // Methods
    .method public hidebysig specialname newslot abstract virtual 
        instance class I '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
    } // end of method I::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig specialname newslot abstract virtual 
        instance int32 get_Property () cil managed 
    {
    } // end of method I::get_Property

    .method public hidebysig specialname newslot abstract virtual 
        instance void set_Property (
            int32 'value'
        ) cil managed 
    {
    } // end of method I::set_Property

    // Properties
    .property instance int32 Property()
    {
        .get instance int32 I::get_Property()
        .set instance void I::set_Property(int32)
    }

} // end of class I
";

            var src = @"
class C
{
    public static void M<T>(T t) where T : I
    {
        _ = t with { X = 2, Property = 3 };
    }
}";
            var comp = CreateCompilationWithIL(new[] { src, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,13): error CS8858: The receiver type 'T' is not a valid record type.
                //         _ = t with { X = 2, Property = 3 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "t").WithArguments("T").WithLocation(6, 13),
                // (6,22): error CS0117: 'T' does not contain a definition for 'X'
                //         _ = t with { X = 2, Property = 3 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("T", "X").WithLocation(6, 22)
                );
        }

        [Fact]
        public void WithExpr33_TypeParameterWithStructureConstraint()
        {
            var ilSource = @"
.class public sequential ansi sealed beforefieldinit S
    extends System.ValueType
{
    .pack 0
    .size 1

    // Methods
    .method public hidebysig 
        instance valuetype S '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        // Method begins at RVA 0x2150
        // Code size 2 (0x2)
        .maxstack 1
        .locals init (
            [0] valuetype S
        )

        IL_0000: ldnull
        IL_0001: throw
    } // end of method S::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig specialname 
        instance int32 get_Property () cil managed 
    {
        // Method begins at RVA 0x215e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method S::get_Property

    .method public hidebysig specialname 
        instance void set_Property (
            int32 'value'
        ) cil managed 
    {
        // Method begins at RVA 0x215e
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method S::set_Property

    // Properties
    .property instance int32 Property()
    {
        .get instance int32 S::get_Property()
        .set instance void S::set_Property(int32)
    }
} // end of class S
";

            var src = @"
abstract class Base<T>
{
    public abstract void M<U>(U t) where U : T;
}

class C : Base<S>
{
    public override void M<U>(U t)
    {
        _ = t with { X = 2, Property = 3 };
    }
}";
            var comp = CreateCompilationWithIL(new[] { src, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (11,13): error CS8858: The receiver type 'U' is not a valid record type.
                //         _ = t with { X = 2, Property = 3 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "t").WithArguments("U").WithLocation(11, 13),
                // (11,22): error CS0117: 'U' does not contain a definition for 'X'
                //         _ = t with { X = 2, Property = 3 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("U", "X").WithLocation(11, 22),
                // (11,29): error CS0117: 'U' does not contain a definition for 'Property'
                //         _ = t with { X = 2, Property = 3 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Property").WithArguments("U", "Property").WithLocation(11, 29)
                );
        }

        [Fact, WorkItem(47513, "https://github.com/dotnet/roslyn/issues/47513")]
        public void BothGetHashCodeAndEqualsAreDefined()
        {
            var src = @"
public sealed record C
{
    public object Data;
    public bool Equals(C c) { return false; }
    public override int GetHashCode() { return 0; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(47513, "https://github.com/dotnet/roslyn/issues/47513")]
        public void BothGetHashCodeAndEqualsAreNotDefined()
        {
            var src = @"
public sealed record C
{
    public object Data;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(47513, "https://github.com/dotnet/roslyn/issues/47513")]
        public void GetHashCodeIsDefinedButEqualsIsNot()
        {
            var src = @"
public sealed record C
{
    public object Data;
    public override int GetHashCode() { return 0; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(47513, "https://github.com/dotnet/roslyn/issues/47513")]
        public void EqualsIsDefinedButGetHashCodeIsNot()
        {
            var src = @"
public sealed record C
{
    public object Data;
    public bool Equals(C c) { return false; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,17): warning CS8851: 'C' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(C c) { return false; }
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("C").WithLocation(5, 17));
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord()
        {
            var src = @"
class record { }

class C
{
    record M(record r) => r;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,7): error CS8860: Types and aliases should not be named 'record'.
                // class record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 7),
                // (6,24): error CS1514: { expected
                //     record M(record r) => r;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "=>").WithLocation(6, 24),
                // (6,24): error CS1513: } expected
                //     record M(record r) => r;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(6, 24),
                // (6,24): error CS1519: Invalid token '=>' in class, record, struct, or interface member declaration
                //     record M(record r) => r;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(6, 24),
                // (6,28): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     record M(record r) => r;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(6, 28),
                // (6,28): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     record M(record r) => r;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(6, 28)
                );

            comp = CreateCompilation(src, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Struct()
        {
            var src = @"
struct record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 8)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Interface()
        {
            var src = @"
interface record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,11): warning CS8860: Types and aliases should not be named 'record'.
                // interface record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 11)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Enum()
        {
            var src = @"
enum record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,6): warning CS8860: Types and aliases should not be named 'record'.
                // enum record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 6)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Delegate()
        {
            var src = @"
delegate void record();
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,15): warning CS8860: Types and aliases should not be named 'record'.
                // delegate void record();
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 15)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Delegate_Escaped()
        {
            var src = @"
delegate void @record();
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Alias()
        {
            var src = @"
using record = System.Console;
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using record = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using record = System.Console;").WithLocation(2, 1),
                // (2,7): warning CS8860: Types and aliases should not be named 'record'.
                // using record = System.Console;
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 7)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Alias_Escaped()
        {
            var src = @"
using @record = System.Console;
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using @record = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using @record = System.Console;").WithLocation(2, 1)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_TypeParameter()
        {
            var src = @"
class C<record> { } // 1
class C2
{
    class Nested<record> { } // 2
}
class C3
{
    void Method<record>() { } // 3

    void Method2()
    {
        void local<record>() // 4
        {
            local<record>();
        }
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,9): warning CS8860: Types and aliases should not be named 'record'.
                // class C<record> { } // 1
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 9),
                // (5,18): warning CS8860: Types and aliases should not be named 'record'.
                //     class Nested<record> { } // 2
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(5, 18),
                // (9,17): warning CS8860: Types and aliases should not be named 'record'.
                //     void Method<record>() { } // 3
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(9, 17),
                // (13,20): warning CS8860: Types and aliases should not be named 'record'.
                //         void local<record>() // 4
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(13, 20)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_TypeParameter_Escaped()
        {
            var src = @"
class C<@record> { }
class C2
{
    class Nested<@record> { }
}
class C3
{
    void Method<@record>() { }

    void Method2()
    {
        void local<@record>()
        {
            local<record>();
        }
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_TypeParameter_Escaped_Partial()
        {
            var src = @"
partial class C<@record> { }
partial class C<record> { } // 1

partial class D<record> { } // 2
partial class D<@record> { }

partial class D<record> { } // 3
partial class D<record> { } // 4

partial class E<@record> { }
partial class E<@record> { }

partial class C2
{
    partial class Nested<record> { } // 5
    partial class Nested<@record> { }
}
partial class C3
{
    partial void Method<@record>();
    partial void Method<record>() { } // 6

    partial void Method2<@record>() { }
    partial void Method2<record>(); // 7

    partial void Method3<record>(); // 8
    partial void Method3<@record>() { }

    partial void Method4<record>() { } // 9
    partial void Method4<@record>();
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,17): warning CS8860: Types and aliases should not be named 'record'.
                // partial class C<record> { } // 1
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(3, 17),
                // (5,17): warning CS8860: Types and aliases should not be named 'record'.
                // partial class D<record> { } // 2
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(5, 17),
                // (8,17): warning CS8860: Types and aliases should not be named 'record'.
                // partial class D<record> { } // 3
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(8, 17),
                // (9,17): warning CS8860: Types and aliases should not be named 'record'.
                // partial class D<record> { } // 4
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(9, 17),
                // (16,26): warning CS8860: Types and aliases should not be named 'record'.
                //     partial class Nested<record> { } // 5
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(16, 26),
                // (22,25): warning CS8860: Types and aliases should not be named 'record'.
                //     partial void Method<record>() { } // 6
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(22, 25),
                // (25,26): warning CS8860: Types and aliases should not be named 'record'.
                //     partial void Method2<record>(); // 7
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(25, 26),
                // (27,26): warning CS8860: Types and aliases should not be named 'record'.
                //     partial void Method3<record>(); // 8
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(27, 26),
                // (30,26): warning CS8860: Types and aliases should not be named 'record'.
                //     partial void Method4<record>() { } // 9
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(30, 26)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Record()
        {
            var src = @"
record record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,8): warning CS8860: Types and aliases should not be named 'record'.
                // record record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 8)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_TwoParts()
        {
            var src = @"
partial class record { }
partial class record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,15): warning CS8860: Types and aliases should not be named 'record'.
                // partial class record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 15),
                // (3,15): warning CS8860: Types and aliases should not be named 'record'.
                // partial class record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(3, 15)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_Escaped()
        {
            var src = @"
class @record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_MixedEscapedPartial()
        {
            var src = @"
partial class @record { }
partial class record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,15): warning CS8860: Types and aliases should not be named 'record'.
                // partial class record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(3, 15)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_MixedEscapedPartial_ReversedOrder()
        {
            var src = @"
partial class record { }
partial class @record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,15): warning CS8860: Types and aliases should not be named 'record'.
                // partial class record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 15)
                );
        }

        [Fact, WorkItem(47090, "https://github.com/dotnet/roslyn/issues/47090")]
        public void TypeNamedRecord_BothEscapedPartial()
        {
            var src = @"
partial class @record { }
partial class @record { }
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeNamedRecord_EscapedReturnType()
        {
            var src = @"
class record { }

class C
{
    @record M(record r)
    {
        System.Console.Write(""RAN"");
        return r;
    }

    public static void Main()
    {
        var c = new C();
        _ = c.M(new record());
    }
}";
            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (2,7): warning CS8860: Types and aliases should not be named 'record'.
                // class record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithArguments("record").WithLocation(2, 7)
                );
            CompileAndVerify(comp, expectedOutput: "RAN");
        }

        [Fact, WorkItem(45591, "https://github.com/dotnet/roslyn/issues/45591")]
        public void Clone_DisallowedInSource()
        {
            var src = @"
record C1(string Clone); // 1
record C2
{
    string Clone; // 2
}
record C3
{
    string Clone { get; set; } // 3
}
record C4
{
    data string Clone; // 4 not yet supported
}
record C5
{
    void Clone() { } // 5
    void Clone(int i) { } // 6
}
record C6
{
    class Clone { } // 7
}
record C7
{
    delegate void Clone(); // 8
}
record C8
{
    event System.Action Clone;  // 9
}
record Clone
{
    Clone(int i) => throw null;
}
record C9 : System.ICloneable
{
    object System.ICloneable.Clone() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,18): error CS8859: Members named 'Clone' are disallowed in records.
                // record C1(string Clone); // 1
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(2, 18),
                // (5,12): error CS8859: Members named 'Clone' are disallowed in records.
                //     string Clone; // 2
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(5, 12),
                // (5,12): warning CS0169: The field 'C2.Clone' is never used
                //     string Clone; // 2
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Clone").WithArguments("C2.Clone").WithLocation(5, 12),
                // (9,12): error CS8859: Members named 'Clone' are disallowed in records.
                //     string Clone { get; set; } // 3
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(9, 12),
                // (13,10): error CS1519: Invalid token 'string' in class, record, struct, or interface member declaration
                //     data string Clone; // 4 not yet supported
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "string").WithArguments("string").WithLocation(13, 10),
                // (13,17): error CS8859: Members named 'Clone' are disallowed in records.
                //     data string Clone; // 4 not yet supported
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(13, 17),
                // (13,17): warning CS0169: The field 'C4.Clone' is never used
                //     data string Clone; // 4 not yet supported
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Clone").WithArguments("C4.Clone").WithLocation(13, 17),
                // (17,10): error CS8859: Members named 'Clone' are disallowed in records.
                //     void Clone() { } // 5
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(17, 10),
                // (18,10): error CS8859: Members named 'Clone' are disallowed in records.
                //     void Clone(int i) { } // 6
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(18, 10),
                // (22,11): error CS8859: Members named 'Clone' are disallowed in records.
                //     class Clone { } // 7
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(22, 11),
                // (26,19): error CS8859: Members named 'Clone' are disallowed in records.
                //     delegate void Clone(); // 8
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(26, 19),
                // (30,25): error CS8859: Members named 'Clone' are disallowed in records.
                //     event System.Action Clone;  // 9
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(30, 25),
                // (30,25): warning CS0067: The event 'C8.Clone' is never used
                //     event System.Action Clone;  // 9
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Clone").WithArguments("C8.Clone").WithLocation(30, 25)
                );
        }

        [Fact]
        public void Clone_LoadedFromMetadata()
        {
            // IL for ' public record Base(int i);' with a 'void Clone()' method added
            var il = @"
.class public auto ansi beforefieldinit Base
    extends [mscorlib]System.Object
    implements class [mscorlib]System.IEquatable`1<class Base>
{
    .field private initonly int32 '<i>k__BackingField'
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )

    .method public hidebysig specialname newslot virtual instance class Base '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: newobj instance void Base::.ctor(class Base)
        IL_0006: ret
    }

    .method family hidebysig specialname newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldtoken Base
        IL_0005: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor ( int32 i ) cil managed
    {
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 Base::'<i>k__BackingField'
        IL_0007: ldarg.0
        IL_0008: call instance void [mscorlib]System.Object::.ctor()
        IL_000d: ret
    }

    .method public hidebysig specialname instance int32 get_i () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::'<i>k__BackingField'
        IL_0006: ret
    }

    .method public hidebysig specialname instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_i ( int32 'value' ) cil managed
    {
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld int32 Base::'<i>k__BackingField'
        IL_0007: ret
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: call class [mscorlib]System.Collections.Generic.EqualityComparer`1<!0> class [mscorlib]System.Collections.Generic.EqualityComparer`1<class [mscorlib]System.Type>::get_Default()
        IL_0005: ldarg.0
        IL_0006: callvirt instance class [mscorlib]System.Type Base::get_EqualityContract()
        IL_000b: callvirt instance int32 class [mscorlib]System.Collections.Generic.EqualityComparer`1<class [mscorlib]System.Type>::GetHashCode(!0)
        IL_0010: ldc.i4 -1521134295
        IL_0015: mul
        IL_0016: call class [mscorlib]System.Collections.Generic.EqualityComparer`1<!0> class [mscorlib]System.Collections.Generic.EqualityComparer`1<int32>::get_Default()
        IL_001b: ldarg.0
        IL_001c: ldfld int32 Base::'<i>k__BackingField'
        IL_0021: callvirt instance int32 class [mscorlib]System.Collections.Generic.EqualityComparer`1<int32>::GetHashCode(!0)
        IL_0026: add
        IL_0027: ret
    }

    .method public hidebysig virtual instance bool Equals ( object obj ) cil managed
    {
        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: isinst Base
        IL_0007: callvirt instance bool Base::Equals(class Base)
        IL_000c: ret
    }

    .method public newslot virtual instance bool Equals ( class Base '' ) cil managed
    {
        IL_0000: ldarg.1
        IL_0001: brfalse.s IL_002d

        IL_0003: ldarg.0
        IL_0004: callvirt instance class [mscorlib]System.Type Base::get_EqualityContract()
        IL_0009: ldarg.1
        IL_000a: callvirt instance class [mscorlib]System.Type Base::get_EqualityContract()
        IL_000f: call bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
        IL_0014: brfalse.s IL_002d

        IL_0016: call class [mscorlib]System.Collections.Generic.EqualityComparer`1<!0> class [mscorlib]System.Collections.Generic.EqualityComparer`1<int32>::get_Default()
        IL_001b: ldarg.0
        IL_001c: ldfld int32 Base::'<i>k__BackingField'
        IL_0021: ldarg.1
        IL_0022: ldfld int32 Base::'<i>k__BackingField'
        IL_0027: callvirt instance bool class [mscorlib]System.Collections.Generic.EqualityComparer`1<int32>::Equals(!0, !0)
        IL_002c: ret

        IL_002d: ldc.i4.0
        IL_002e: ret
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class Base '' ) cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ldarg.0
        IL_0007: ldarg.1
        IL_0008: ldfld int32 Base::'<i>k__BackingField'
        IL_000d: stfld int32 Base::'<i>k__BackingField'
        IL_0012: ret
    }

    .method public hidebysig instance void Deconstruct ( [out] int32& i ) cil managed
    {
        IL_0000: ldarg.1
        IL_0001: ldarg.0
        IL_0002: call instance int32 Base::get_i()
        IL_0007: stind.i4
        IL_0008: ret
    }

    .method public hidebysig instance void Clone () cil managed
    {
        IL_0000: ldstr ""RAN""
        IL_0005: call void [mscorlib]System.Console::Write(string)
        IL_000a: ret
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type Base::get_EqualityContract()
    }

    .property instance int32 i()
    {
        .get instance int32 Base::get_i()
        .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) Base::set_i(int32)
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi abstract sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends [mscorlib]System.Object
{
}
";
            var src = @"
record R(int i) : Base(i);

public class C
{
    public static void Main()
    {
        var r = new R(1);
        r.Clone();
    }
}
";

            var comp = CreateCompilationWithIL(src, il, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN").VerifyDiagnostics();
            // Note: we do load the Clone method from metadata
        }

        [Fact]
        public void Clone_01()
        {
            var src = @"
abstract sealed record C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,24): error CS0418: 'C1': an abstract type cannot be sealed or static
                // abstract sealed record C1;
                Diagnostic(ErrorCode.ERR_AbstractSealedStatic, "C1").WithArguments("C1").WithLocation(2, 24)
                );

            var clone = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.False(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.True(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);

            Assert.True(clone.ContainingType.IsSealed);
            Assert.True(clone.ContainingType.IsAbstract);

            Assert.Equal("record C1", comp.GlobalNamespace.GetTypeMember("C1")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_02()
        {
            var src = @"
sealed abstract record C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,24): error CS0418: 'C1': an abstract type cannot be sealed or static
                // sealed abstract record C1;
                Diagnostic(ErrorCode.ERR_AbstractSealedStatic, "C1").WithArguments("C1").WithLocation(2, 24)
                );

            var clone = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.False(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.True(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);

            Assert.True(clone.ContainingType.IsSealed);
            Assert.True(clone.ContainingType.IsAbstract);

            Assert.Equal("record C1", comp.GlobalNamespace.GetTypeMember("C1")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_03()
        {
            var src = @"
record C1;
abstract sealed record C2 : C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS0418: 'C2': an abstract type cannot be sealed or static
                // abstract sealed record C2 : C1;
                Diagnostic(ErrorCode.ERR_AbstractSealedStatic, "C2").WithArguments("C2").WithLocation(3, 24)
                );

            var clone = comp.GetMember<MethodSymbol>("C2." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.True(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.True(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);

            Assert.True(clone.ContainingType.IsSealed);
            Assert.True(clone.ContainingType.IsAbstract);
        }

        [Fact]
        public void Clone_04()
        {
            var src = @"
record C1;
sealed abstract record C2 : C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS0418: 'C2': an abstract type cannot be sealed or static
                // sealed abstract record C2 : C1;
                Diagnostic(ErrorCode.ERR_AbstractSealedStatic, "C2").WithArguments("C2").WithLocation(3, 24)
                );

            var clone = comp.GetMember<MethodSymbol>("C2." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.True(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.True(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);

            Assert.True(clone.ContainingType.IsSealed);
            Assert.True(clone.ContainingType.IsAbstract);

            Assert.Equal("record C1", comp.GlobalNamespace.GetTypeMember("C1")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_05_IntReturnType_UsedAsBaseType()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance int32 '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );

            Assert.Equal("class A", comp.GlobalNamespace.GetTypeMember("A")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_06_IntReturnType_UsedInWith()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance int32 '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A
";
            var source = @"
public class Program
{
    static void Main()
    {
        A x = new A() with { };
    }
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (6,15): error CS8858: The receiver type 'A' is not a valid record type.
                //         A x = new A() with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "new A()").WithArguments("A").WithLocation(6, 15)
                );

            Assert.Equal("class A", comp.GlobalNamespace.GetTypeMember("A")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_07_Ambiguous_UsedAsBaseType()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance int32 '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );

            Assert.Equal("class A", comp.GlobalNamespace.GetTypeMember("A")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_08_Ambiguous_UsedInWith()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance int32 '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A
";
            var source = @"
public class Program
{
    static void Main()
    {
        A x = new A() with { };
    }
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (6,15): error CS8858: The receiver type 'A' is not a valid record type.
                //         A x = new A() with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "new A()").WithArguments("A").WithLocation(6, 15)
                );

            Assert.Equal("class A", comp.GlobalNamespace.GetTypeMember("A")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_09_AmbiguousReverseOrder_UsedAsBaseType()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig specialname newslot virtual 
        instance int32 '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );

            Assert.Equal("class A", comp.GlobalNamespace.GetTypeMember("A")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_10_AmbiguousReverseOrder_UsedInWith()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig specialname newslot virtual 
        instance int32 '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A
";
            var source = @"
public class Program
{
    static void Main()
    {
        A x = new A() with { };
    }
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (6,15): error CS8858: The receiver type 'A' is not a valid record type.
                //         A x = new A() with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "new A()").WithArguments("A").WithLocation(6, 15)
                );

            Assert.Equal("class A", comp.GlobalNamespace.GetTypeMember("A")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_11()
        {
            string source1 = @"
public record A;
";

            var comp1Ref = CreateCompilation(source1).EmitToImageReference();

            string source2 = @"
public record B(int X) : A;
";

            var comp2Ref = CreateCompilation(new[] { source2, IsExternalInitTypeDefinition }, references: new[] { comp1Ref }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source3 = @"
class Program
{
    public static void Main()
    {
        var c1 = new B(1);
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }
}
";
            CompileAndVerify(source3, references: new[] { comp1Ref, comp2Ref }, expectedOutput: @"1
11").VerifyDiagnostics();
        }

        [Fact]
        public void Clone_12()
        {
            string source1 = @"
public record A;
";

            var comp1Ref = CreateCompilation(new[] { source1, IsExternalInitTypeDefinition }, assemblyName: "Clone_12", parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source2 = @"
public record B(int X) : A;
";

            var comp2Ref = CreateCompilation(new[] { source2, IsExternalInitTypeDefinition }, references: new[] { comp1Ref }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source3 = @"
class Program
{
    public static void Main()
    {
        var c1 = new B(1);
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }
}
";
            var comp3 = CreateCompilation(new[] { source3, IsExternalInitTypeDefinition }, references: new[] { comp2Ref }, parseOptions: TestOptions.Regular9);
            comp3.VerifyEmitDiagnostics(
                // (7,18): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_12, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c1").WithArguments("A", "Clone_12, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18)
                );
        }

        [Fact]
        public void Clone_13()
        {
            string source1 = @"
public record A;
";

            var comp1Ref = CreateCompilation(new[] { source1, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source2 = @"
public record B(int X) : A;
";

            var comp2Ref = CreateCompilation(new[] { source2, IsExternalInitTypeDefinition }, assemblyName: "Clone_13", references: new[] { comp1Ref }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source3 = @"
public record C(int X) : B(X);
";

            var comp3Ref = CreateCompilation(new[] { source3, IsExternalInitTypeDefinition }, references: new[] { comp1Ref, comp2Ref }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source4 = @"
class Program
{
    public static void Main()
    {
        var c1 = new C(1);
        var c2 = c1 with { X = 11 };
    }
}
";
            var comp4 = CreateCompilation(new[] { source4, IsExternalInitTypeDefinition }, references: new[] { comp1Ref, comp3Ref }, parseOptions: TestOptions.Regular9);
            comp4.VerifyEmitDiagnostics(
                // (7,18): error CS8858: The receiver type 'C' is not a valid record type.
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c1").WithArguments("C").WithLocation(7, 18),
                // (7,18): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_13, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c1").WithArguments("B", "Clone_13, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18),
                // (7,28): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_13, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoTypeDef, "X").WithArguments("B", "Clone_13, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 28),
                // (7,28): error CS0117: 'C' does not contain a definition for 'X'
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("C", "X").WithLocation(7, 28)
                );

            var comp5 = CreateCompilation(new[] { source4, IsExternalInitTypeDefinition }, references: new[] { comp3Ref }, parseOptions: TestOptions.Regular9);
            comp5.VerifyEmitDiagnostics(
                // (7,18): error CS8858: The receiver type 'C' is not a valid record type.
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c1").WithArguments("C").WithLocation(7, 18),
                // (7,18): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_13, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c1").WithArguments("B", "Clone_13, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18),
                // (7,28): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_13, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoTypeDef, "X").WithArguments("B", "Clone_13, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 28),
                // (7,28): error CS0117: 'C' does not contain a definition for 'X'
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("C", "X").WithLocation(7, 28)
                );
        }

        [Fact]
        public void Clone_14()
        {
            string source1 = @"
public record A;
";

            var comp1Ref = CreateCompilation(source1).EmitToImageReference();

            string source2 = @"
public record B(int X) : A;
";

            var comp2Ref = CreateCompilation(new[] { source2, IsExternalInitTypeDefinition }, references: new[] { comp1Ref }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source3 = @"
record C(int X) : B(X)
{
    public static void Main()
    {
        var c1 = new C(1);
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }
}
";
            CompileAndVerify(source3, references: new[] { comp1Ref, comp2Ref }, expectedOutput: @"1
11").VerifyDiagnostics();
        }

        [Fact]
        public void Clone_15()
        {
            string source1 = @"
public record A;
";

            var comp1Ref = CreateCompilation(new[] { source1, IsExternalInitTypeDefinition }, assemblyName: "Clone_15", parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source2 = @"
public record B(int X) : A;
";

            var comp2Ref = CreateCompilation(new[] { source2, IsExternalInitTypeDefinition }, references: new[] { comp1Ref }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source3 = @"
record C(int X) : B(X)
{
    public static void Main()
    {
        var c1 = new C(1);
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }
}
";
            var comp3 = CreateCompilation(new[] { source3, IsExternalInitTypeDefinition }, references: new[] { comp2Ref }, parseOptions: TestOptions.Regular9);
            comp3.VerifyEmitDiagnostics(
                // (2,8): error CS8869: 'C.Equals(object?)' does not override expected method from 'object'.
                // record C(int X) : B(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "C").WithArguments("C.Equals(object?)").WithLocation(2, 8),
                // (2,8): error CS8869: 'C.GetHashCode()' does not override expected method from 'object'.
                // record C(int X) : B(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "C").WithArguments("C.GetHashCode()").WithLocation(2, 8),
                // (2,8): error CS8869: 'C.ToString()' does not override expected method from 'object'.
                // record C(int X) : B(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "C").WithArguments("C.ToString()").WithLocation(2, 8),
                // (2,8): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // record C(int X) : B(X)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("A", "Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 8),
                // (2,19): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // record C(int X) : B(X)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B").WithArguments("A", "Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 19),
                // (2,19): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // record C(int X) : B(X)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B").WithArguments("A", "Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 19),
                // (6,22): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c1 = new C(1);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("A", "Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 22),
                // (7,18): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c2 = c1 with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoTypeDef, "c1").WithArguments("A", "Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18),
                // (8,9): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine(c1.X);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "System").WithArguments("A", "Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 9),
                // (9,9): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine(c2.X);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "System").WithArguments("A", "Clone_15, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 9)
                );
        }

        [Fact]
        public void Clone_16()
        {
            string source1 = @"
public record A;
";

            var comp1Ref = CreateCompilation(new[] { source1, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source2 = @"
public record B(int X) : A;
";

            var comp2Ref = CreateCompilation(new[] { source2, IsExternalInitTypeDefinition }, assemblyName: "Clone_16", references: new[] { comp1Ref }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source3 = @"
public record C(int X) : B(X);
";

            var comp3Ref = CreateCompilation(new[] { source3, IsExternalInitTypeDefinition }, references: new[] { comp1Ref, comp2Ref }, parseOptions: TestOptions.Regular9).EmitToImageReference();

            string source4 = @"
record D(int X) : C(X)
{
    public static void Main()
    {
        var c1 = new D(1);
        var c2 = c1 with { X = 11 };
    }
}
";
            var comp4 = CreateCompilation(new[] { source4, IsExternalInitTypeDefinition }, references: new[] { comp1Ref, comp3Ref }, parseOptions: TestOptions.Regular9);
            comp4.VerifyEmitDiagnostics(
                // (2,8): error CS8869: 'D.Equals(object?)' does not override expected method from 'object'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "D").WithArguments("D.Equals(object?)").WithLocation(2, 8),
                // (2,8): error CS8869: 'D.GetHashCode()' does not override expected method from 'object'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "D").WithArguments("D.GetHashCode()").WithLocation(2, 8),
                // (2,8): error CS8869: 'D.ToString()' does not override expected method from 'object'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "D").WithArguments("D.ToString()").WithLocation(2, 8),
                // (2,19): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("B", "Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 19),
                // (2,19): error CS8864: Records may only inherit from object or another record
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_BadRecordBase, "C").WithLocation(2, 19),
                // (2,19): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("B", "Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 19),
                // (6,22): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c1 = new D(1);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("B", "Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 22)
                );

            var comp5 = CreateCompilation(new[] { source4, IsExternalInitTypeDefinition }, references: new[] { comp3Ref }, parseOptions: TestOptions.Regular9);
            comp5.VerifyEmitDiagnostics(
                // (2,8): error CS8869: 'D.Equals(object?)' does not override expected method from 'object'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "D").WithArguments("D.Equals(object?)").WithLocation(2, 8),
                // (2,8): error CS8869: 'D.GetHashCode()' does not override expected method from 'object'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "D").WithArguments("D.GetHashCode()").WithLocation(2, 8),
                // (2,8): error CS8869: 'D.ToString()' does not override expected method from 'object'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "D").WithArguments("D.ToString()").WithLocation(2, 8),
                // (2,19): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("B", "Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 19),
                // (2,19): error CS8864: Records may only inherit from object or another record
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_BadRecordBase, "C").WithLocation(2, 19),
                // (2,19): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // record D(int X) : C(X)
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C").WithArguments("B", "Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 19),
                // (6,22): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         var c1 = new D(1);
                Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("B", "Clone_16, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 22)
                );
        }

        [Fact]
        public void Clone_17_NonOverridable()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );

            Assert.Equal("class A", comp.GlobalNamespace.GetTypeMember("A")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_18_NonOverridable()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual final
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );

            Assert.Equal("class A", comp.GlobalNamespace.GetTypeMember("A")
                .ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void Clone_19()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A

.class public auto ansi beforefieldinit B
    extends A
{
    // Methods
    .method public hidebysig specialname virtual final
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method B::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public final virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public newslot virtual 
        instance bool Equals (
            class B ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class B ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method B::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class B

";
            var source = @"
public record C : B {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0239: 'C.<Clone>$()': cannot override inherited member 'B.<Clone>$()' because it is sealed
                // public record C : B {
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "C").WithArguments("C.<Clone>$()", "B.<Clone>$()").WithLocation(2, 15)
                );
        }

        [Fact, WorkItem(47093, "https://github.com/dotnet/roslyn/issues/47093")]
        public void ToString_TopLevelRecord_Empty()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record C1;
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { }");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Protected, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.True(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
");
            v.VerifyIL("C1." + WellKnownMemberNames.ObjectToString, @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.Text.StringBuilder V_0)
  IL_0000:  newobj     ""System.Text.StringBuilder..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""C1""
  IL_000c:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldstr      "" { ""
  IL_0018:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  ldloc.0
  IL_0020:  callvirt   ""bool C1.PrintMembers(System.Text.StringBuilder)""
  IL_0025:  brfalse.s  IL_0033
  IL_0027:  ldloc.0
  IL_0028:  ldstr      "" ""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldloc.0
  IL_0034:  ldstr      ""}""
  IL_0039:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_003e:  pop
  IL_003f:  ldloc.0
  IL_0040:  callvirt   ""string object.ToString()""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_AbstractRecord()
        {
            var src = @"
var c2 = new C2();
System.Console.Write(c2);

abstract record C1;
record C2 : C1
{
    public override string ToString() => base.ToString();
}
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { }");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Protected, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.True(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
");
            v.VerifyIL("C1." + WellKnownMemberNames.ObjectToString, @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.Text.StringBuilder V_0)
  IL_0000:  newobj     ""System.Text.StringBuilder..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""C1""
  IL_000c:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldstr      "" { ""
  IL_0018:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  ldloc.0
  IL_0020:  callvirt   ""bool C1.PrintMembers(System.Text.StringBuilder)""
  IL_0025:  brfalse.s  IL_0033
  IL_0027:  ldloc.0
  IL_0028:  ldstr      "" ""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldloc.0
  IL_0034:  ldstr      ""}""
  IL_0039:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_003e:  pop
  IL_003f:  ldloc.0
  IL_0040:  callvirt   ""string object.ToString()""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void ToString_DerivedRecord_AbstractRecord()
        {
            var src = @"
var c2 = new C2();
System.Console.Write(c2);

record Base;
abstract record C1 : Base;
record C2 : C1
{
    public override string ToString() => base.ToString();
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { }");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Protected, print.DeclaredAccessibility);
            Assert.True(print.IsOverride);
            Assert.False(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool Base.PrintMembers(System.Text.StringBuilder)""
  IL_0007:  ret
}
");
            v.VerifyIL("C1." + WellKnownMemberNames.ObjectToString, @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.Text.StringBuilder V_0)
  IL_0000:  newobj     ""System.Text.StringBuilder..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""C1""
  IL_000c:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldstr      "" { ""
  IL_0018:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  ldloc.0
  IL_0020:  callvirt   ""bool Base.PrintMembers(System.Text.StringBuilder)""
  IL_0025:  brfalse.s  IL_0033
  IL_0027:  ldloc.0
  IL_0028:  ldstr      "" ""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldloc.0
  IL_0034:  ldstr      ""}""
  IL_0039:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_003e:  pop
  IL_003f:  ldloc.0
  IL_0040:  callvirt   ""string object.ToString()""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilder()
        {
            var src = @"
record C1;
";

            var comp = CreateCompilation(src);
            comp.MakeTypeMissing(WellKnownType.System_Text_StringBuilder);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0518: Predefined type 'System.Text.StringBuilder' is not defined or imported
                // record C1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "record C1;").WithArguments("System.Text.StringBuilder").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder..ctor'
                // record C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C1;").WithArguments("System.Text.StringBuilder", ".ctor").WithLocation(2, 1),
                // (2,8): error CS0518: Predefined type 'System.Text.StringBuilder' is not defined or imported
                // record C1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C1").WithArguments("System.Text.StringBuilder").WithLocation(2, 8)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilderCtor()
        {
            var src = @"
record C1;
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__ctor);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder..ctor'
                // record C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C1;").WithArguments("System.Text.StringBuilder", ".ctor").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilderAppendString()
        {
            var src = @"
record C1;
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__AppendString);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C1;").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_OneProperty_MissingStringBuilderAppendString()
        {
            var src = @"
record C1(int P);
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__AppendString);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record C1(int P);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C1(int P);").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record C1(int P);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C1(int P);").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_Empty_Sealed()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

sealed record C1;
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { }");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Private, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.False(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);
        }

        [Fact]
        public void ToString_AbstractRecord()
        {
            var src = @"
var c2 = new C2(42, 43);
System.Console.Write(c2.ToString());

abstract record C1(int I1);
sealed record C2(int I1, int I2) : C1(I1);
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C2 { I1 = 42, I2 = 43 }", verify: Verification.Skipped /* init-only */);
        }

        [Fact, WorkItem(47672, "https://github.com/dotnet/roslyn/issues/47672")]
        public void ToString_RecordWithIndexer()
        {
            var src = @"
var c1 = new C1(42);
System.Console.Write(c1.ToString());

record C1(int I1)
{
    private int field = 44;
    public int this[int i] => 0;
    public int PropertyWithoutGetter { set { } }
    public int P2 { get => 43; }
    public ref int P3 { get => ref field; }
    public event System.Action a;

    private int field1 = 100;
    internal int field2 = 100;
    protected int field3 = 100;
    private protected int field4 = 100;
    internal protected int field5 = 100;

    private int Property1 { get; set; } = 100;
    internal int Property2 { get; set; } = 100;
    protected int Property3 { get; set; } = 100;
    private protected int Property4 { get; set; } = 100;
    internal protected int Property5 { get; set; } = 100;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1 { I1 = 42, P2 = 43, P3 = 44 }", verify: Verification.Skipped /* init-only */);
            comp.VerifyEmitDiagnostics(
                // (12,32): warning CS0067: The event 'C1.a' is never used
                //     public event System.Action a;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "a", isSuppressed: false).WithArguments("C1.a").WithLocation(12, 32),
                // (14,17): warning CS0414: The field 'C1.field1' is assigned but its value is never used
                //     private int field1 = 100;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field1", isSuppressed: false).WithArguments("C1.field1").WithLocation(14, 17)
                );
        }

        [Fact, WorkItem(47672, "https://github.com/dotnet/roslyn/issues/47672")]
        public void ToString_PrivateGetter()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record C1
{
    public int P1 { private get => 43; set => throw null; }
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1 { P1 = 43 }", verify: Verification.Skipped /* init-only */);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem(47797, "https://github.com/dotnet/roslyn/issues/47797")]
        public void ToString_OverriddenVirtualProperty_NoRepetition()
        {
            var src = @"
System.Console.WriteLine(new B() { P = 2 }.ToString());

abstract record A
{
    public virtual int P { get; set; }
}
record B : A
{
    public override int P { get; set; }
}
";
            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "B { P = 2 }");
        }

        [Fact, WorkItem(47797, "https://github.com/dotnet/roslyn/issues/47797")]
        public void ToString_OverriddenAbstractProperty_NoRepetition()
        {
            var src = @"
System.Console.Write(new B1() { P = 1 });
System.Console.Write("" "");
System.Console.Write(new B2(2));

abstract record A1
{
    public abstract int P { get; set; }
}

record B1 : A1
{
    public override int P { get; set; }
}

abstract record A2(int P);

record B2(int P) : A2(P);
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "B1 { P = 1 } B2 { P = 2 }", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void ToString_ErrorBase()
        {
            var src = @"
record C2: Error;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,8): error CS0115: 'C2.ToString()': no suitable method found to override
                // record C2: Error;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C2").WithArguments("C2.ToString()").WithLocation(2, 8),
                // (2,8): error CS0115: 'C2.EqualityContract': no suitable method found to override
                // record C2: Error;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C2").WithArguments("C2.EqualityContract").WithLocation(2, 8),
                // (2,8): error CS0115: 'C2.Equals(object?)': no suitable method found to override
                // record C2: Error;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C2").WithArguments("C2.Equals(object?)").WithLocation(2, 8),
                // (2,8): error CS0115: 'C2.GetHashCode()': no suitable method found to override
                // record C2: Error;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C2").WithArguments("C2.GetHashCode()").WithLocation(2, 8),
                // (2,12): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                // record C2: Error;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(2, 12)
                );
        }

        [Fact]
        public void ToString_SelfReferentialBase()
        {
            var src = @"
record R : R;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,8): error CS0146: Circular base type dependency involving 'R' and 'R'
                // record R : R;
                Diagnostic(ErrorCode.ERR_CircularBase, "R").WithArguments("R", "R").WithLocation(2, 8),
                // (2,8): error CS0115: 'R.ToString()': no suitable method found to override
                // record R : R;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "R").WithArguments("R.ToString()").WithLocation(2, 8),
                // (2,8): error CS0115: 'R.EqualityContract': no suitable method found to override
                // record R : R;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "R").WithArguments("R.EqualityContract").WithLocation(2, 8),
                // (2,8): error CS0115: 'R.Equals(object?)': no suitable method found to override
                // record R : R;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "R").WithArguments("R.Equals(object?)").WithLocation(2, 8),
                // (2,8): error CS0115: 'R.GetHashCode()': no suitable method found to override
                // record R : R;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "R").WithArguments("R.GetHashCode()").WithLocation(2, 8)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_Empty_AbstractSealed()
        {
            var src = @"
abstract sealed record C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,24): error CS0418: 'C1': an abstract type cannot be sealed or static
                // abstract sealed record C1;
                Diagnostic(ErrorCode.ERR_AbstractSealedStatic, "C1").WithArguments("C1").WithLocation(2, 24)
                );

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Private, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.False(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);
        }

        [Fact, WorkItem(47092, "https://github.com/dotnet/roslyn/issues/47092")]
        public void ToString_TopLevelRecord_OneField_ValueType()
        {
            var src = @"
var c1 = new C1() { field = 42 };
System.Console.Write(c1.ToString());

record C1
{
    public int field;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = 42 }", verify: Verification.Skipped /* init-only */);

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Protected, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.True(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       50 (0x32)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  ldflda     ""int C1.field""
  IL_001f:  constrained. ""int""
  IL_0025:  callvirt   ""string object.ToString()""
  IL_002a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_002f:  pop
  IL_0030:  ldc.i4.1
  IL_0031:  ret
}
");
        }

        [Fact, WorkItem(47092, "https://github.com/dotnet/roslyn/issues/47092")]
        public void ToString_TopLevelRecord_OneField_ConstrainedValueType()
        {
            var src = @"
var c1 = new C1<int>() { field = 42 };
System.Console.Write(c1.ToString());

record C1<T> where T : struct
{
    public T field;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = 42 }", verify: Verification.Skipped /* init-only */);

            v.VerifyIL("C1<T>." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       50 (0x32)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  ldflda     ""T C1<T>.field""
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""string object.ToString()""
  IL_002a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_002f:  pop
  IL_0030:  ldc.i4.1
  IL_0031:  ret
}
");
        }

        [Fact, WorkItem(47092, "https://github.com/dotnet/roslyn/issues/47092")]
        public void ToString_TopLevelRecord_OneField_ReferenceType()
        {
            var src = @"
var c1 = new C1() { field = ""hello"" };
System.Console.Write(c1.ToString());

record C1
{
    public string field;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = hello }", verify: Verification.Skipped /* init-only */);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  ldfld      ""string C1.field""
  IL_001f:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0024:  pop
  IL_0025:  ldc.i4.1
  IL_0026:  ret
}
");
        }

        [Fact, WorkItem(47092, "https://github.com/dotnet/roslyn/issues/47092")]
        public void ToString_TopLevelRecord_OneField_Unconstrained()
        {
            var src = @"
var c1 = new C1<string>() { field = ""hello"" };
System.Console.Write(c1.ToString());
System.Console.Write("" "");

var c2 = new C1<int>() { field = 42 };
System.Console.Write(c2.ToString());

record C1<T>
{
    public T field;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = hello } C1 { field = 42 }", verify: Verification.Skipped /* init-only */);

            v.VerifyIL("C1<T>." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  ldfld      ""T C1<T>.field""
  IL_001f:  box        ""T""
  IL_0024:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0029:  pop
  IL_002a:  ldc.i4.1
  IL_002b:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_OneField_ErrorType()
        {
            var src = @"
record C1
{
    public Error field;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,12): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //     public Error field;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(4, 12),
                // (4,18): warning CS0649: Field 'C1.field' is never assigned to, and will always have its default value null
                //     public Error field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C1.field", "null").WithLocation(4, 18)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_TwoFields_ReferenceType()
        {
            var src = @"
var c1 = new C1() { field1 = ""hi"", field2 = null };
System.Console.Write(c1.ToString());

record C1
{
    public string field1;
    public string field2;

    private string field3;
    internal string field4;
    protected string field5;
    protected internal string field6;
    private protected string field7;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (10,20): warning CS0169: The field 'C1.field3' is never used
                //     private string field3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "field3").WithArguments("C1.field3").WithLocation(10, 20),
                // (11,21): warning CS0649: Field 'C1.field4' is never assigned to, and will always have its default value null
                //     internal string field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field4").WithArguments("C1.field4", "null").WithLocation(11, 21),
                // (12,22): warning CS0649: Field 'C1.field5' is never assigned to, and will always have its default value null
                //     protected string field5;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field5").WithArguments("C1.field5", "null").WithLocation(12, 22),
                // (13,31): warning CS0649: Field 'C1.field6' is never assigned to, and will always have its default value null
                //     protected internal string field6;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field6").WithArguments("C1.field6", "null").WithLocation(13, 31),
                // (14,30): warning CS0649: Field 'C1.field7' is never assigned to, and will always have its default value null
                //     private protected string field7;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field7").WithArguments("C1.field7", "null").WithLocation(14, 30)
                );
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field1 = hi, field2 =  }", verify: Verification.Skipped /* init-only */);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       88 (0x58)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field1""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  ldfld      ""string C1.field1""
  IL_001f:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0024:  pop
  IL_0025:  ldarg.1
  IL_0026:  ldstr      "", ""
  IL_002b:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0030:  pop
  IL_0031:  ldarg.1
  IL_0032:  ldstr      ""field2""
  IL_0037:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_003c:  pop
  IL_003d:  ldarg.1
  IL_003e:  ldstr      "" = ""
  IL_0043:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0048:  pop
  IL_0049:  ldarg.1
  IL_004a:  ldarg.0
  IL_004b:  ldfld      ""string C1.field2""
  IL_0050:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0055:  pop
  IL_0056:  ldc.i4.1
  IL_0057:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_OneProperty()
        {
            var src = @"
var c1 = new C1(Property: 42);
System.Console.Write(c1.ToString());

record C1(int Property)
{
    private int Property2;
    internal int Property3;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (7,17): warning CS0169: The field 'C1.Property2' is never used
                //     private int Property2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Property2").WithArguments("C1.Property2").WithLocation(7, 17),
                // (8,18): warning CS0649: Field 'C1.Property3' is never assigned to, and will always have its default value 0
                //     internal int Property3;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Property3").WithArguments("C1.Property3", "0").WithLocation(8, 18)
                );
            var v = CompileAndVerify(comp, expectedOutput: "C1 { Property = 42 }", verify: Verification.Skipped /* init-only */);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""Property""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  call       ""int C1.Property.get""
  IL_001f:  stloc.0
  IL_0020:  ldloca.s   V_0
  IL_0022:  constrained. ""int""
  IL_0028:  callvirt   ""string object.ToString()""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldc.i4.1
  IL_0034:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_TwoFieldsAndTwoProperties()
        {
            var src = @"
var c1 = new C1<int, string>(42, null) { field1 = 43, field2 = ""hi"" };
System.Console.Write(c1.ToString());

record C1<T1, T2>(T1 Property1, T2 Property2)
{
    public T1 field1;
    public T2 field2;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { Property1 = 42, Property2 = , field1 = 43, field2 = hi }", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void ToString_DerivedRecord_TwoFieldsAndTwoProperties()
        {
            var src = @"
var c1 = new C1(42, 43) { A2 = 100, B2 = 101 };
System.Console.Write(c1.ToString());

record Base(int A1)
{
    public int A2;
}
record C1(int A1, int B1) : Base(A1)
{
    public int B2;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { A1 = 42, A2 = 100, B1 = 43, B2 = 101 }", verify: Verification.Skipped /* init-only */);

            v.VerifyIL("C1.PrintMembers", @"
{
  // Code size      134 (0x86)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool Base.PrintMembers(System.Text.StringBuilder)""
  IL_0007:  brfalse.s  IL_0015
  IL_0009:  ldarg.1
  IL_000a:  ldstr      "", ""
  IL_000f:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0014:  pop
  IL_0015:  ldarg.1
  IL_0016:  ldstr      ""B1""
  IL_001b:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0020:  pop
  IL_0021:  ldarg.1
  IL_0022:  ldstr      "" = ""
  IL_0027:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_002c:  pop
  IL_002d:  ldarg.1
  IL_002e:  ldarg.0
  IL_002f:  call       ""int C1.B1.get""
  IL_0034:  stloc.0
  IL_0035:  ldloca.s   V_0
  IL_0037:  constrained. ""int""
  IL_003d:  callvirt   ""string object.ToString()""
  IL_0042:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0047:  pop
  IL_0048:  ldarg.1
  IL_0049:  ldstr      "", ""
  IL_004e:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0053:  pop
  IL_0054:  ldarg.1
  IL_0055:  ldstr      ""B2""
  IL_005a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_005f:  pop
  IL_0060:  ldarg.1
  IL_0061:  ldstr      "" = ""
  IL_0066:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_006b:  pop
  IL_006c:  ldarg.1
  IL_006d:  ldarg.0
  IL_006e:  ldflda     ""int C1.B2""
  IL_0073:  constrained. ""int""
  IL_0079:  callvirt   ""string object.ToString()""
  IL_007e:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0083:  pop
  IL_0084:  ldc.i4.1
  IL_0085:  ret
}
");
        }

        [Fact]
        public void ToString_DerivedRecord_AbstractSealed()
        {
            var src = @"
record C1;
abstract sealed record C2 : C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS0418: 'C2': an abstract type cannot be sealed or static
                // abstract sealed record C2 : C1;
                Diagnostic(ErrorCode.ERR_AbstractSealedStatic, "C2").WithArguments("C2").WithLocation(3, 24)
                );

            var print = comp.GetMember<MethodSymbol>("C2." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Protected, print.DeclaredAccessibility);
            Assert.True(print.IsOverride);
            Assert.False(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);
        }

        [Fact]
        public void ToString_DerivedRecord_BaseHasSealedToString()
        {
            var src = @"
record C1
{
    public sealed override string ToString() => throw null;
}
record C2 : C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,35): error CS8870: 'C1.ToString()' cannot be sealed because containing record is not sealed.
                //     public sealed override string ToString() => throw null;
                Diagnostic(ErrorCode.ERR_SealedAPIInRecord, "ToString").WithArguments("C1.ToString()").WithLocation(4, 35),
                // (6,8): error CS0239: 'C2.ToString()': cannot override inherited member 'C1.ToString()' because it is sealed
                // record C2 : C1;
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "C2").WithArguments("C2.ToString()", "C1.ToString()").WithLocation(6, 8)
                );
        }

        [Fact]
        public void ToString_DerivedRecord_TwoFieldsAndTwoProperties_ReverseOrder()
        {
            var src = @"
var c1 = new C1(42, 43) { A1 = 100, B1 = 101 };
System.Console.Write(c1.ToString());

record Base(int A2)
{
    public int A1;
}
record C1(int A2, int B2) : Base(A2)
{
    public int B1;
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { A2 = 42, A1 = 100, B2 = 43, B1 = 101 }", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void ToString_DerivedRecord_TwoFieldsAndTwoProperties_Partial()
        {
            var src1 = @"
var c1 = new C1() { A1 = 100, B1 = 101 };
System.Console.Write(c1.ToString());

partial record C1
{
    public int A1;
}
";
            var src2 = @"
partial record C1
{
    public int B1;
}
";

            var comp = CreateCompilation(new[] { src1, src2, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { A1 = 100, B1 = 101 }", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void ToString_DerivedRecord_TwoFieldsAndTwoProperties_Partial_ReverseOrder()
        {
            var src1 = @"
var c1 = new C1() { A1 = 100, B1 = 101 };
System.Console.Write(c1.ToString());

partial record C1
{
    public int B1;
}
";
            var src2 = @"
partial record C1
{
    public int A1;
}
";

            var comp = CreateCompilation(new[] { src1, src2, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { B1 = 101, A1 = 100 }", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void ToString_BadBase_MissingToString()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldstr ""RAN""
        IL_0005: ret
    }
}

.class public auto ansi beforefieldinit B
    extends A
{
    .method family hidebysig specialname virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static bool op_Inequality ( class B r1, class B r2 ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static bool op_Equality ( class B r1, class B r2 ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object obj ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public final hidebysig virtual instance bool Equals ( class A other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig newslot virtual instance bool Equals ( class B other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class B original ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void A::.ctor()
        IL_0006: ret
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    // no override for ToString
}
";
            var source = @"
var c = new C();
System.Console.Write(c);

public record C : B
{
    public override string ToString() => base.ToString();
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN");
        }

        [Fact]
        public void ToString_BadBase_PrintMembersSealed()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method final family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): warning CS0108: 'B.PrintMembers(StringBuilder)' hides inherited member 'A.PrintMembers(StringBuilder)'. Use the new keyword if hiding was intended.
                // public record B : A {
                Diagnostic(ErrorCode.WRN_NewRequired, "B").WithArguments("B.PrintMembers(System.Text.StringBuilder)", "A.PrintMembers(System.Text.StringBuilder)").WithLocation(2, 15),
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );
        }

        [Fact]
        public void ToString_BadBase_PrintMembersInaccessible()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method private hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );
        }

        [Fact]
        public void ToString_BadBase_PrintMembersReturnsInt()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance int32 '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): warning CS0114: 'B.PrintMembers(StringBuilder)' hides inherited member 'A.PrintMembers(StringBuilder)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                // public record B : A {
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "B").WithArguments("B.PrintMembers(System.Text.StringBuilder)", "A.PrintMembers(System.Text.StringBuilder)").WithLocation(2, 15),
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );
        }

        [Fact]
        public void ToString_BadBase_PrintMembersIsAmbiguous()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance int32 '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): warning CS0114: 'B.PrintMembers(StringBuilder)' hides inherited member 'A.PrintMembers(StringBuilder)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                // public record B : A {
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "B").WithArguments("B.PrintMembers(System.Text.StringBuilder)", "A.PrintMembers(System.Text.StringBuilder)").WithLocation(2, 15),
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );
        }

        [Fact]
        public void ToString_BadBase_MissingPrintMembers()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );
        }

        [Fact]
        public void ToString_BadBase_DuplicatePrintMembers()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder modopt(int64) builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): warning CS0114: 'B.PrintMembers(StringBuilder)' hides inherited member 'A.PrintMembers(StringBuilder)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                // public record B : A {
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "B").WithArguments("B.PrintMembers(System.Text.StringBuilder)", "A.PrintMembers(System.Text.StringBuilder)").WithLocation(2, 15),
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record B : A {
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(2, 19)
                );
        }

        [Fact]
        public void ToString_BadBase_PrintMembersNotOverriddenInBase()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi beforefieldinit B
    extends A
{
    .method family hidebysig specialname virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static bool op_Inequality ( class B r1, class B r2 ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname static bool op_Equality ( class B r1, class B r2 ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object obj ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public final hidebysig virtual instance bool Equals ( class A other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig newslot virtual instance bool Equals ( class B other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class B original ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }

    // no override for PrintMembers
}
";

            var source = @"
public record C : B
{
    protected override bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8864: Records may only inherit from object or another record
                // public record C : B
                Diagnostic(ErrorCode.ERR_BadRecordBase, "B").WithLocation(2, 19),
                // (4,29): error CS8871: 'C.PrintMembers(StringBuilder)' does not override expected method from 'B'.
                //     protected override bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseMethod, "PrintMembers").WithArguments("C.PrintMembers(System.Text.StringBuilder)", "B").WithLocation(4, 29)
                );
        }

        [Fact]
        public void ToString_BadBase_PrintMembersWithModOpt()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder modopt(int64) builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics();

            var print = comp.GetMember<MethodSymbol>("B." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal("System.Boolean B.PrintMembers(System.Text.StringBuilder modopt(System.Int64) builder)", print.ToTestDisplayString());
            Assert.Equal("System.Boolean A.PrintMembers(System.Text.StringBuilder modopt(System.Int64) builder)", print.OverriddenMethod.ToTestDisplayString());
        }

        [Fact]
        public void ToString_BadBase_NewToString()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig newslot virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS8869: 'B.ToString()' does not override expected method from 'object'.
                // public record B : A {
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "B").WithArguments("B.ToString()").WithLocation(2, 15)
                );
        }

        [Fact]
        public void ToString_BadBase_SealedToString()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    .method public hidebysig specialname newslot virtual instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object other ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig specialname rtspecialname instance void .ctor ( class A '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public final hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0239: 'B.ToString()': cannot override inherited member 'A.ToString()' because it is sealed
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "B").WithArguments("B.ToString()", "A.ToString()").WithLocation(2, 15)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record C1
{
    public override string ToString() => ""RAN"";
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal("System.Boolean C1." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)", print.ToTestDisplayString());
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString_Sealed()
        {
            var src = @"
record C1
{
    public sealed override string ToString() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,35): error CS8870: 'C1.ToString()' cannot be sealed because containing record is not sealed.
                //     public sealed override string ToString() => throw null;
                Diagnostic(ErrorCode.ERR_SealedAPIInRecord, "ToString").WithArguments("C1.ToString()").WithLocation(4, 35)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString_Sealed_InSealedRecord()
        {
            var src = @"
sealed record C1
{
    public sealed override string ToString() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WithNullableStringBuilder()
        {
            var src = @"
#nullable enable
record C1
{
    protected virtual bool PrintMembers(System.Text.StringBuilder? builder) => throw null!;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_ErrorReturnType()
        {
            var src = @"
record C1
{
    protected virtual Error PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,23): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //     protected virtual Error PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(4, 23)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WrongReturnType()
        {
            var src = @"
record C1
{
    protected virtual int PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,27): error CS8874: Record member 'C1.PrintMembers(StringBuilder)' must return 'bool'.
                //     protected virtual int PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)", "bool").WithLocation(4, 27)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_Sealed()
        {
            var src = @"
record C1(int I1);
record C2(int I1, int I2) : C1(I1)
{
    protected sealed override bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (5,36): error CS8872: 'C2.PrintMembers(StringBuilder)' must allow overriding because the containing record is not sealed.
                //     protected sealed override bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "PrintMembers").WithArguments("C2.PrintMembers(System.Text.StringBuilder)").WithLocation(5, 36)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_NonVirtual()
        {
            var src = @"
record C1
{
    protected bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,20): error CS8872: 'C1.PrintMembers(StringBuilder)' must allow overriding because the containing record is not sealed.
                //     protected bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(4, 20)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_SealedInSealedRecord()
        {
            var src = @"
record C1(int I1);
sealed record C2(int I1, int I2) : C1(I1)
{
    protected sealed override bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_Static()
        {
            var src = @"
sealed record C
{
    private static bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,25): error CS8877: Record member 'C.PrintMembers(StringBuilder)' may not be static.
                //     private static bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "PrintMembers").WithArguments("C.PrintMembers(System.Text.StringBuilder)").WithLocation(4, 25)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record C1
{
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append(""RAN"");
        return true;
    }
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { RAN }");
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WrongAccessibility()
        {
            var src = @"
record C1
{
    public virtual bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,25): error CS8875: Record member 'C1.PrintMembers(StringBuilder)' must be protected.
                //     public virtual bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_NonProtectedAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(4, 25)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WrongAccessibility_SealedRecord()
        {
            var src = @"
sealed record C1
{
    protected bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,20): warning CS0628: 'C1.PrintMembers(StringBuilder)': new protected member declared in sealed type
                //     protected bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(4, 20),
                // (4,20): error CS8879: Record member 'C1.PrintMembers(StringBuilder)' must be private.
                //     protected bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_NonPrivateAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(4, 20)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_DerivedRecord_WrongAccessibility()
        {
            var src = @"
record B;
record C1 : B
{
    public bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (5,17): error CS8875: Record member 'C1.PrintMembers(StringBuilder)' must be protected.
                //     public bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_NonProtectedAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(5, 17),
                // (5,17): error CS8860: 'C1.PrintMembers(StringBuilder)' does not override expected method from 'B'.
                //     public bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseMethod, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)", "B").WithLocation(5, 17),
                // (5,17): error CS8872: 'C1.PrintMembers(StringBuilder)' must allow overriding because the containing record is not sealed.
                //     public bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(5, 17),
                // (5,17): warning CS0114: 'C1.PrintMembers(StringBuilder)' hides inherited member 'B.PrintMembers(StringBuilder)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)", "B.PrintMembers(System.Text.StringBuilder)").WithLocation(5, 17)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_New()
        {
            var src = @"
record B;
record C1 : B
{
    protected new virtual bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (5,32): error CS8860: 'C1.PrintMembers(StringBuilder)' does not override expected method from 'B'.
                //     protected new virtual bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseMethod, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)", "B").WithLocation(5, 32)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_EscapedNamed()
        {
            var src = @"
var c1 = new @base();
System.Console.Write(c1.ToString());

record @base;
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "base { }");
        }

        [Fact]
        public void ToString_DerivedDerivedRecord()
        {
            var src = @"
var r1 = new R1(1);
System.Console.Write(r1.ToString());
System.Console.Write("" "");

var r2 = new R2(10, 11);
System.Console.Write(r2.ToString());
System.Console.Write("" "");

var r3 = new R3(20, 21, 22);
System.Console.Write(r3.ToString());

record R1(int I1);
record R2(int I1, int I2) : R1(I1);
record R3(int I1, int I2, int I3) : R2(I1, I2);
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "R1 { I1 = 1 } R2 { I1 = 10, I2 = 11 } R3 { I1 = 20, I2 = 21, I3 = 22 }", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void WithExpr24()
        {
            string source = @"
record C(int X)
{
    public static void Main()
    {
        var c1 = new C(1);
        c1 = c1 with { };
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }

    protected C(ref C other) : this(-1)
    {
    }

    protected C(C other)
    {
        X = other.X; 
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"1
11").VerifyDiagnostics();

            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}
");

            var clone = verifier.Compilation.GetMember("C." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal("<Clone>$", clone.Name);
        }

        [Fact]
        public void WithExpr25()
        {
            string source = @"
record C(int X)
{
    public static void Main()
    {
        var c1 = new C(1);
        c1 = c1 with { };
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }

    protected C(in C other) : this(-1)
    {
    }

    protected C(C other)
    {
        X = other.X; 
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"1
11").VerifyDiagnostics();

            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void WithExpr26()
        {
            string source = @"
record C(int X)
{
    public static void Main()
    {
        var c1 = new C(1);
        c1 = c1 with { };
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }

    protected C(out C other) : this(-1)
    {
        other = null;
    }

    protected C(C other)
    {
        X = other.X; 
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"1
11").VerifyDiagnostics();

            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void WithExpr27()
        {
            string source = @"
record C(int X)
{
    public static void Main()
    {
        var c1 = new C(1);
        c1 = c1 with { };
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }

    protected C(ref C other) : this(-1)
    {
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"1
11").VerifyDiagnostics();

            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void WithExpr28()
        {
            string source = @"
record C(int X)
{
    public static void Main()
    {
        var c1 = new C(1);
        c1 = c1 with { };
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }

    protected C(in C other) : this(-1)
    {
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"1
11").VerifyDiagnostics();

            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void WithExpr29()
        {
            string source = @"
record C(int X)
{
    public static void Main()
    {
        var c1 = new C(1);
        c1 = c1 with { };
        var c2 = c1 with { X = 11 };
        System.Console.WriteLine(c1.X);
        System.Console.WriteLine(c2.X);
    }

    protected C(out C other) : this(-1)
    {
        other = null;
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"1
11").VerifyDiagnostics();

            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void AccessibilityOfBaseCtor_01()
        {
            var src = @"
using System;

record Base
{
    protected Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}

    public static void Main()
    {
        var c = new C(1, 2);
    }
}

record C(int X, int Y) : Base(X, Y);
";
            CompileAndVerify(src, expectedOutput: @"
1
2
").VerifyDiagnostics();
        }

        [Fact]
        public void AccessibilityOfBaseCtor_02()
        {
            var src = @"
using System;

record Base
{
    protected Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}

    public static void Main()
    {
        var c = new C(1, 2);
    }
}

record C(int X, int Y) : Base(X, Y) {}
";
            CompileAndVerify(src, expectedOutput: @"
1
2
").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(44898, "https://github.com/dotnet/roslyn/issues/44898")]
        public void AccessibilityOfBaseCtor_03()
        {
            var src = @"
abstract record A
{
    protected A() {}
    protected A(A x) {}
};
record B(object P) : A;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(44898, "https://github.com/dotnet/roslyn/issues/44898")]
        public void AccessibilityOfBaseCtor_04()
        {
            var src = @"
abstract record A
{
    protected A() {}
    protected A(A x) {}
};
record B(object P) : A {}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(44898, "https://github.com/dotnet/roslyn/issues/44898")]
        public void AccessibilityOfBaseCtor_05()
        {
            var src = @"
abstract record A
{
    protected A() {}
    protected A(A x) {}
};
record B : A;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(44898, "https://github.com/dotnet/roslyn/issues/44898")]
        public void AccessibilityOfBaseCtor_06()
        {
            var src = @"
abstract record A
{
    protected A() {}
    protected A(A x) {}
};
record B : A {}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExprNestedErrors()
        {
            var src = @"
class C
{
    public int X { get; init; }
    public static void Main()
    {
        var c = new C();
        c = c with { X = """"-3 };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS8808: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { X = ""-3 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(8, 13),
                // (8,26): error CS0019: Operator '-' cannot be applied to operands of type 'string' and 'int'
                //         c = c with { X = ""-3 };
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"""""-3").WithArguments("-", "string", "int").WithLocation(8, 26)
            );
        }

        [Fact]
        public void WithExprNoExpressionToPropertyTypeConversion()
        {
            var src = @"

record C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { X = """" };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         c = c with { X = "" };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(8, 26)
            );
        }

        [Fact]
        public void WithExprPropertyInaccessibleSet()
        {
            var src = @"
record C
{
    public int X { get; private set; }
}
class D
{
    public static void Main()
    {
        var c = new C();
        c = c with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,22): error CS0272: The property or indexer 'C.X' cannot be used in this context because the set accessor is inaccessible
                //         c = c with { X = 0 };
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "X").WithArguments("C.X").WithLocation(11, 22)
            );
        }

        [Fact]
        public void WithExprSideEffects1()
        {
            var src = @"
using System;

record C(int X, int Y, int Z)
{
    public static void Main()
    {
        var c = new C(0, 1, 2);
        c = c with { Y = W(""Y""), X = W(""X"") };
    }

    public static int W(string s)
    {
        Console.WriteLine(s);
        return 0;
    }
}
";
            var verifier = CompileAndVerify(src, expectedOutput: @"
Y
X").VerifyDiagnostics();

            verifier.VerifyIL("C.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.2
  IL_0003:  newobj     ""C..ctor(int, int, int)""
  IL_0008:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_000d:  dup
  IL_000e:  ldstr      ""Y""
  IL_0013:  call       ""int C.W(string)""
  IL_0018:  callvirt   ""void C.Y.init""
  IL_001d:  dup
  IL_001e:  ldstr      ""X""
  IL_0023:  call       ""int C.W(string)""
  IL_0028:  callvirt   ""void C.X.init""
  IL_002d:  pop
  IL_002e:  ret
}");

            var comp = (CSharpCompilation)verifier.Compilation;
            var tree = comp.SyntaxTrees.First();
            var root = tree.GetRoot();
            var model = comp.GetSemanticModel(tree);

            var withExpr1 = root.DescendantNodes().OfType<WithExpressionSyntax>().First();
            comp.VerifyOperationTree(withExpr1, @"
IWithOperation (OperationKind.With, Type: C) (Syntax: 'c with { Y  ...  = W(""X"") }')
  Operand:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  CloneMethod: C C." + WellKnownMemberNames.CloneMethodName + @"()
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ Y = W(""Y"" ...  = W(""X"") }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Y = W(""Y"")')
            Left:
              IPropertyReferenceOperation: System.Int32 C.Y { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Y')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Y')
            Right:
              IInvocationOperation (System.Int32 C.W(System.String s)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'W(""Y"")')
                Instance Receiver:
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""Y""')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Y"") (Syntax: '""Y""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = W(""X"")')
            Left:
              IPropertyReferenceOperation: System.Int32 C.X { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
            Right:
              IInvocationOperation (System.Int32 C.W(System.String s)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'W(""X"")')
                Instance Receiver:
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""X""')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""X"") (Syntax: '""X""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            var main = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            Assert.Equal("Main", main.Identifier.ToString());
            VerifyFlowGraph(comp, main, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C(0, 1, 2)')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C(0, 1, 2)')
              Right:
                IObjectCreationOperation (Constructor: C..ctor(System.Int32 X, System.Int32 Y, System.Int32 Z)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0, 1, 2)')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '0')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Z) (OperationKind.Argument, Type: null) (Syntax: '2')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
                  Value:
                    IInvocationOperation (virtual C C." + WellKnownMemberNames.CloneMethodName + @"()) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
                      Instance Receiver:
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                      Arguments(0)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Y = W(""Y"")')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.Y { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Y')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
                  Right:
                    IInvocationOperation (System.Int32 C.W(System.String s)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'W(""Y"")')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""Y""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Y"") (Syntax: '""Y""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = W(""X"")')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.X { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
                  Right:
                    IInvocationOperation (System.Int32 C.W(System.String s)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'W(""X"")')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""X""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""X"") (Syntax: '""X""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = c with  ... = W(""X"") };')
                  Expression:
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'c = c with  ...  = W(""X"") }')
                      Left:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                      Right:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void WithExprConversions1()
        {
            var src = @"
using System;
record C(long X)
{
    public static void Main()
    {
        var c = new C(0);
        Console.WriteLine((c with { X = 11 }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: "11").VerifyDiagnostics();
            verifier.VerifyIL("C.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i8
  IL_0002:  newobj     ""C..ctor(long)""
  IL_0007:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_000c:  dup
  IL_000d:  ldc.i4.s   11
  IL_000f:  conv.i8
  IL_0010:  callvirt   ""void C.X.init""
  IL_0015:  callvirt   ""long C.X.get""
  IL_001a:  call       ""void System.Console.WriteLine(long)""
  IL_001f:  ret
}");
        }

        [Fact]
        public void WithExprConversions2()
        {
            var src = @"
using System;
struct S
{
    private int _i;
    public S(int i)
    {
        _i = i;
    }
    public static implicit operator long(S s)
    {
        Console.WriteLine(""conversion"");
        return s._i;
    }
}
record C(long X)
{
    public static void Main()
    {
        var c = new C(0);
        var s = new S(11);
        Console.WriteLine((c with { X = s }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
conversion
11").VerifyDiagnostics();
            verifier.VerifyIL("C.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i8
  IL_0002:  newobj     ""C..ctor(long)""
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.s   11
  IL_000b:  call       ""S..ctor(int)""
  IL_0010:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0015:  dup
  IL_0016:  ldloc.0
  IL_0017:  call       ""long S.op_Implicit(S)""
  IL_001c:  callvirt   ""void C.X.init""
  IL_0021:  callvirt   ""long C.X.get""
  IL_0026:  call       ""void System.Console.WriteLine(long)""
  IL_002b:  ret
}");
        }

        [Fact]
        public void WithExprConversions3()
        {
            var src = @"
using System;
struct S
{
    private int _i;
    public S(int i)
    {
        _i = i;
    }
    public static explicit operator int(S s)
    {
        Console.WriteLine(""conversion"");
        return s._i;
    }
}
record C(long X)
{
    public static void Main()
    {
        var c = new C(0);
        var s = new S(11);
        Console.WriteLine((c with { X = (int)s }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
conversion
11").VerifyDiagnostics();
        }

        [Fact]
        public void WithExprConversions4()
        {
            var src = @"
using System;
struct S
{
    private int _i;
    public S(int i)
    {
        _i = i;
    }
    public static explicit operator long(S s) => s._i;
}

record C(long X)
{
    public static void Main()
    {
        var c = new C(0);
        var s = new S(11);
        Console.WriteLine((c with { X = s }).X);
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (19,41): error CS0266: Cannot implicitly convert type 'S' to 'long'. An explicit conversion exists (are you missing a cast?)
                //         Console.WriteLine((c with { X = s }).X);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "s").WithArguments("S", "long").WithLocation(19, 41)
            );
        }

        [Fact]
        public void WithExprConversions5()
        {
            var src = @"
using System;
record C(object X)
{
    public static void Main()
    {
        var c = new C(0);
        Console.WriteLine((c with { X = ""abc"" }).X);
    }
}";
            CompileAndVerify(src, expectedOutput: "abc").VerifyDiagnostics();
        }

        [Fact]
        public void WithExprConversions6()
        {
            var src = @"
using System;
struct S
{
    private int _i;
    public S(int i)
    {
        _i = i;
    }
    public static implicit operator int(S s)
    {
        Console.WriteLine(""conversion"");
        return s._i;
    }
}
record C
{
    private readonly long _x;
    public long X { get => _x; init { Console.WriteLine(""set""); _x = value; } }
    public static void Main()
    {
        var c = new C();
        var s = new S(11);
        Console.WriteLine((c with { X = s }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
conversion
set
11").VerifyDiagnostics();
            verifier.VerifyIL("C.Main", @"
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_0
  IL_0007:  ldc.i4.s   11
  IL_0009:  call       ""S..ctor(int)""
  IL_000e:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0013:  dup
  IL_0014:  ldloc.0
  IL_0015:  call       ""int S.op_Implicit(S)""
  IL_001a:  conv.i8
  IL_001b:  callvirt   ""void C.X.init""
  IL_0020:  callvirt   ""long C.X.get""
  IL_0025:  call       ""void System.Console.WriteLine(long)""
  IL_002a:  ret
}");
        }

        [Fact]
        public void WithExprStaticProperty()
        {
            var src = @"
record C
{
    public static int X { get; set; }
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,22): error CS0176: Member 'C.X' cannot be accessed with an instance reference; qualify it with a type name instead
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "X").WithArguments("C.X").WithLocation(9, 22)
            );
        }

        [Fact]
        public void WithExprMethodAsArgument()
        {
            var src = @"
record C
{
    public int X() => 0;
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,22): error CS1913: Member 'X' cannot be initialized. It is not a field or property.
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "X").WithArguments("X").WithLocation(9, 22)
            );
        }

        [Fact]
        public void WithExprStaticWithMethod()
        {
            var src = @"
class C
{
    public int X = 0;
    public static C Clone() => null;
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,13): error CS8808: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(9, 13),
                // (10,13): error CS8808: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(10, 13)
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExprStaticWithMethod2()
        {
            var src = @"
class B
{
    public B Clone() => null;
}
class C : B
{
    public int X = 0;
    public static new C Clone() => null; // static
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,13): error CS0266: Cannot implicitly convert type 'B' to 'C'. An explicit conversion exists (are you missing a cast?)
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c with { }").WithArguments("B", "C").WithLocation(13, 13),
                // (14,22): error CS0117: 'B' does not contain a definition for 'X'
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("B", "X").WithLocation(14, 22)
            );
        }

        [Fact]
        public void WithExprBadMemberBadType()
        {
            var src = @"
record C
{
    public int X { get; init; }
    public static void Main()
    {
        var c = new C();
        c = c with { X = ""a"" };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         c = c with { X = "a" };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""a""").WithArguments("string", "int").WithLocation(8, 26)
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExprCloneReturnDifferent()
        {
            var src = @"
class B
{
    public int X { get; init; }
}
class C : B
{
    public B Clone() => new B();
    public static void Main()
    {
        var c = new C();
        var b = c with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithSemanticModel1()
        {
            var src = @"
record C(int X, string Y)
{
    public static void Main()
    {
        var c = new C(0, ""a"");
        c = c with { X = 2 };
    }
}";
            var comp = CreateCompilation(src);
            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();
            var model = comp.GetSemanticModel(tree);
            var withExpr = root.DescendantNodes().OfType<WithExpressionSyntax>().Single();
            var typeInfo = model.GetTypeInfo(withExpr);
            var c = comp.GlobalNamespace.GetTypeMember("C");
            Assert.True(c.ISymbol.Equals(typeInfo.Type));

            var x = c.GetMembers("X").Single();
            var xId = withExpr.DescendantNodes().Single(id => id.ToString() == "X");
            var symbolInfo = model.GetSymbolInfo(xId);
            Assert.True(x.ISymbol.Equals(symbolInfo.Symbol));

            comp.VerifyOperationTree(withExpr, @"
IWithOperation (OperationKind.With, Type: C) (Syntax: 'c with { X = 2 }')
  Operand:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  CloneMethod: C C." + WellKnownMemberNames.CloneMethodName + @"()
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ X = 2 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
            Left:
              IPropertyReferenceOperation: System.Int32 C.X { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
            Right:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')");

            var main = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            Assert.Equal("Main", main.Identifier.ToString());
            VerifyFlowGraph(comp, main, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C(0, ""a"")')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C(0, ""a"")')
              Right:
                IObjectCreationOperation (Constructor: C..ctor(System.Int32 X, System.String Y)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0, ""a"")')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '0')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: '""a""')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""a"") (Syntax: '""a""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c with { X = 2 }')
                  Value:
                    IInvocationOperation (virtual C C." + WellKnownMemberNames.CloneMethodName + @"()) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'c with { X = 2 }')
                      Instance Receiver:
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                      Arguments(0)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.X { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { X = 2 }')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = c with { X = 2 };')
                  Expression:
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'c = c with { X = 2 }')
                      Left:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                      Right:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { X = 2 }')
            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void NoCloneMethod_01()
        {
            var src = @"
class C
{
    int X { get; set; }

    public static void Main()
    {
        var c = new C();
        c = c with { X = 2 };
    }
}";
            var comp = CreateCompilation(src);
            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();
            var withExpr = root.DescendantNodes().OfType<WithExpressionSyntax>().Single();

            comp.VerifyOperationTree(withExpr, @"
IWithOperation (OperationKind.With, Type: C, IsInvalid) (Syntax: 'c with { X = 2 }')
  Operand:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
  CloneMethod: null
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ X = 2 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
            Left:
              IPropertyReferenceOperation: System.Int32 C.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
            Right:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')");

            var main = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            Assert.Equal("Main", main.Identifier.ToString());
            VerifyFlowGraph(comp, main, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C()')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C()')
              Right:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                  Value:
                    IInvalidOperation (OperationKind.Invalid, Type: C, IsInvalid, IsImplicit) (Syntax: 'c')
                      Children(1):
                          ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = c with { X = 2 };')
                  Expression:
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: 'c = c with { X = 2 }')
                      Left:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                      Right:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c')
            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void NoCloneMethod_02()
        {
            var source =
@"#nullable enable
class R
{
    public object? P { get; set; }
}
class Program
{
    static void Main()
    {
        R r = new R();
        _ = r with { P = 2 };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (11,13): error CS8858: The receiver type 'R' is not a valid record type.
                //         _ = r with { P = 2 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "r").WithArguments("R").WithLocation(11, 13));
        }

        [Fact]
        public void WithBadExprArg()
        {
            var src = @"

record C(int X, int Y)
{
    public static void Main()
    {
        var c = new C(0, 0);
        c = c with { 5 };
        c = c with { { X = 2 } };
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,22): error CS0747: Invalid initializer member declarator
                //         c = c with { 5 };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "5").WithLocation(8, 22),
                // (9,22): error CS1513: } expected
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{").WithLocation(9, 22),
                // (9,22): error CS1002: ; expected
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(9, 22),
                // (9,24): error CS0120: An object reference is required for the non-static field, method, or property 'C.X'
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "X").WithArguments("C.X").WithLocation(9, 24),
                // (9,30): error CS1002: ; expected
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(9, 30),
                // (9,33): error CS1597: Semicolon after method or accessor block is not valid
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(9, 33),
                // (11,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(11, 1)
            );

            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();
            var model = comp.GetSemanticModel(tree);
            VerifyClone(model);

            var withExpr1 = root.DescendantNodes().OfType<WithExpressionSyntax>().First();
            comp.VerifyOperationTree(withExpr1, @"
IWithOperation (OperationKind.With, Type: C, IsInvalid) (Syntax: 'c with { 5 }')
  Operand:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  CloneMethod: C C." + WellKnownMemberNames.CloneMethodName + @"()
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ 5 }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '5')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5, IsInvalid) (Syntax: '5')");

            var withExpr2 = root.DescendantNodes().OfType<WithExpressionSyntax>().Skip(1).Single();
            comp.VerifyOperationTree(withExpr2, @"
IWithOperation (OperationKind.With, Type: C, IsInvalid) (Syntax: 'c with { ')
  Operand:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  CloneMethod: C C." + WellKnownMemberNames.CloneMethodName + @"()
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ ')
      Initializers(0)");
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_01()
        {
            var src = @"
record B(int X)
{
    static void M(B b)
    {
        int y;
        _ = b with { X = y = 42 };
        y.ToString();
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_02()
        {
            var src = @"
record B(int X, string Y)
{
    static void M(B b)
    {
        int z;
        _ = b with { X = z = 42, Y = z.ToString() };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_03()
        {
            var src = @"
record B(int X, string Y)
{
    static void M(B b)
    {
        int z;
        _ = b with { Y = z.ToString(), X = z = 42 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,26): error CS0165: Use of unassigned local variable 'z'
                //         _ = b with { Y = z.ToString(), X = z = 42 };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(7, 26));
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_04()
        {
            var src = @"
record B(int X)
{
    static void M()
    {
        B b;
        _ = (b = new B(42)) with { X = b.X };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_05()
        {
            var src = @"
record B(int X)
{
    static void M()
    {
        B b;
        _ = new B(b.X) with { X = (b = new B(42)).X };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,19): error CS0165: Use of unassigned local variable 'b'
                //         _ = new B(b.X) with { X = new B(42).X };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "b").WithArguments("b").WithLocation(7, 19));
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_06()
        {
            var src = @"
record B(int X)
{
    static void M(B b)
    {
        int y;
        _ = b with { X = M(out y) };
        y.ToString();
    }

    static int M(out int y) { y = 42; return 43; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_07()
        {
            var src = @"
record B(int X)
{
    static void M(B b)
    {
        _ = b with { X = M(out int y) };
        y.ToString();
    }

    static int M(out int y) { y = 42; return 43; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_NullableAnalysis_01()
        {
            var src = @"
#nullable enable
record B(int X)
{
    static void M(B b)
    {
        string? s = null;
        _ = b with { X = M(out s) };
        s.ToString();
    }

    static int M(out string s) { s = ""a""; return 42; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_NullableAnalysis_02()
        {
            var src = @"
#nullable enable
record B(string X)
{
    static void M(B b, string? s)
    {
        b.X.ToString();
        _ = b with { X = s }; // 1
        b.X.ToString(); // 2
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): warning CS8601: Possible null reference assignment.
                //         _ = b with { X = s }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "s").WithLocation(8, 26));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_03()
        {
            var src = @"
#nullable enable
record B(string? X)
{
    static void M1(B b, string s, bool flag)
    {
        if (flag) { b.X.ToString(); } // 1
        _ = b with { X = s };
        if (flag) { b.X.ToString(); } // 2
    }

    static void M2(B b, string s, bool flag)
    {
        if (flag) { b.X.ToString(); } // 3
        b = b with { X = s };
        if (flag) { b.X.ToString(); }
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(7, 21),
                // (9,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(9, 21),
                // (14,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(14, 21));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_04()
        {
            var src = @"
#nullable enable
record B(int X)
{
    static void M1(B? b)
    {
        var b1 = b with { X = 42 }; // 1
        _ = b.ToString();
        _ = b1.ToString();
    }

    static void M2(B? b)
    {
        (b with { X = 42 }).ToString(); // 2
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,18): warning CS8602: Dereference of a possibly null reference.
                //         var b1 = b with { X = 42 }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b").WithLocation(7, 18),
                // (14,10): warning CS8602: Dereference of a possibly null reference.
                //         (b with { X = 42 }).ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b").WithLocation(14, 10));
        }

        [Fact, WorkItem(44763, "https://github.com/dotnet/roslyn/issues/44763")]
        public void WithExpr_NullableAnalysis_05()
        {
            var src = @"
#nullable enable
record B(string? X, string? Y)
{
    static void M1(bool flag)
    {
        B b = new B(""hello"", null);
        if (flag)
        {
            b.X.ToString(); // shouldn't warn
            b.Y.ToString(); // 1
        }

        b = b with { Y = ""world"" };
        b.X.ToString(); // shouldn't warn
        b.Y.ToString();
    }
}";
            // records should propagate the nullability of the
            // constructor arguments to the corresponding properties.
            // https://github.com/dotnet/roslyn/issues/44763
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (10,13): warning CS8602: Dereference of a possibly null reference.
                //             b.X.ToString(); // shouldn't warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(10, 13),
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //             b.Y.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Y").WithLocation(11, 13),
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         b.X.ToString(); // shouldn't warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(15, 9));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_06()
        {
            var src = @"
#nullable enable
record B
{
    public string? X { get; init; }
    public string? Y { get; init; }

    static void M1(bool flag)
    {
        B b = new B { X = ""hello"", Y = null };
        if (flag)
        {
            b.X.ToString();
            b.Y.ToString(); // 1
        }

        b = b with { Y = ""world"" };

        b.X.ToString();
        b.Y.ToString();
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (14,13): warning CS8602: Dereference of a possibly null reference.
                //             b.Y.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Y").WithLocation(14, 13)
            );
        }

        [Fact, WorkItem(44691, "https://github.com/dotnet/roslyn/issues/44691")]
        public void WithExpr_NullableAnalysis_07()
        {
            var src = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

record B([AllowNull] string X) // 1
{
    static void M1(B b)
    {
        b.X.ToString();
        b = b with { X = null }; // 2
        b.X.ToString(); // 3
        b = new B((string?)null);
        b.X.ToString();
    }
}";
            var comp = CreateCompilation(new[] { src, AllowNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (5,10): warning CS8601: Possible null reference assignment.
                // record B([AllowNull] string X) // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "[AllowNull] string X").WithLocation(5, 10),
                // (10,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         b = b with { X = null }; // 2
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 26),
                // (11,9): warning CS8602: Dereference of a possibly null reference.
                //         b.X.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(11, 9));
        }

        [Fact, WorkItem(44691, "https://github.com/dotnet/roslyn/issues/44691")]
        public void WithExpr_NullableAnalysis_08()
        {
            var src = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

record B([property: AllowNull][AllowNull] string X)
{
    static void M1(B b)
    {
        b.X.ToString();
        b = b with { X = null };
        b.X.ToString(); // 1
        b = new B((string?)null);
        b.X.ToString();
    }
}";
            var comp = CreateCompilation(new[] { src, AllowNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Dereference of a possibly null reference.
                //         b.X.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(11, 9));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_09()
        {
            var src = @"
#nullable enable
record B(string? X, string? Y)
{
    static void M1(B b1)
    {
        B b2 = b1 with { X = ""hello"" };
        B b3 = b1 with { Y = ""world"" };
        B b4 = b2 with { Y = ""world"" };

        b1.X.ToString(); // 1
        b1.Y.ToString(); // 2
        b2.X.ToString();
        b2.Y.ToString(); // 3
        b3.X.ToString(); // 4
        b3.Y.ToString();
        b4.X.ToString();
        b4.Y.ToString();
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Dereference of a possibly null reference.
                //         b1.X.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b1.X").WithLocation(11, 9),
                // (12,9): warning CS8602: Dereference of a possibly null reference.
                //         b1.Y.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b1.Y").WithLocation(12, 9),
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         b2.Y.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b2.Y").WithLocation(14, 9),
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         b3.X.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b3.X").WithLocation(15, 9));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_10()
        {
            var src = @"
#nullable enable
record B(string? X, string? Y)
{
    static void M1(B b1)
    {
        string? local = ""hello"";
        _ = b1 with
        {
            X = local = null,
            Y = local.ToString() // 1
        };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,17): warning CS8602: Dereference of a possibly null reference.
                //             Y = local.ToString() // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "local").WithLocation(11, 17));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_11()
        {
            var src = @"
#nullable enable
record B(string X, string Y)
{
    static string M0(out string? s) { s = null; return ""hello""; }

    static void M1(B b1)
    {
        string? local = ""world"";
        _ = b1 with
        {
            X = M0(out local),
            Y = local // 1
        };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,17): warning CS8601: Possible null reference assignment.
                //             Y = local // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "local").WithLocation(13, 17));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_VariantClone()
        {
            var src = @"
#nullable enable

record A
{
    public string? Y { get; init; }
    public string? Z { get; init; }
}

record B(string? X) : A
{
    public new string Z { get; init; } = ""zed"";

    static void M1(B b1)
    {
        b1.Z.ToString();
        (b1 with { Y = ""hello"" }).Y.ToString();
        (b1 with { Y = ""hello"" }).Z.ToString();
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr_NullableAnalysis_NullableClone()
        {
            var src = @"
#nullable enable

record B(string? X)
{
    public B? Clone() => new B(X);

    static void M1(B b1)
    {
        _ = b1 with { X = null }; // 1
        (b1 with { X = null }).ToString(); // 2
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (10,21): warning CS8602: Dereference of a possibly null reference.
                //         _ = b1 with { X = null }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "{ X = null }").WithLocation(10, 21),
                // (11,18): warning CS8602: Dereference of a possibly null reference.
                //         (b1 with { X = null }).ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "{ X = null }").WithLocation(11, 18));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr_NullableAnalysis_MaybeNullClone()
        {
            var src = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

record B(string? X)
{
    [return: MaybeNull]
    public B Clone() => new B(X);

    static void M1(B b1)
    {
        _ = b1 with { };
        _ = b1 with { X = null }; // 1
        (b1 with { X = null }).ToString(); // 2
    }
}
";
            var comp = CreateCompilation(new[] { src, MaybeNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (13,21): warning CS8602: Dereference of a possibly null reference.
                //         _ = b1 with { X = null }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "{ X = null }").WithLocation(13, 21),
                // (14,18): warning CS8602: Dereference of a possibly null reference.
                //         (b1 with { X = null }).ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "{ X = null }").WithLocation(14, 18));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr_NullableAnalysis_NotNullClone()
        {
            var src = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

record B(string? X)
{
    [return: NotNull]
    public B? Clone() => new B(X);

    static void M1(B b1)
    {
        _ = b1 with { };
        _ = b1 with { X = null };
        (b1 with { X = null }).ToString();
    }
}
";
            var comp = CreateCompilation(new[] { src, NotNullAttributeDefinition });
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr_NullableAnalysis_NullableClone_NoInitializers()
        {
            var src = @"
#nullable enable

record B(string? X)
{
    public B? Clone() => new B(X);

    static void M1(B b1)
    {
        _ = b1 with { };
        (b1 with { }).ToString(); // 1
    }
}
";
            var comp = CreateCompilation(src);
            // Note: we expect to give a warning on `// 1`, but do not currently
            // due to limitations of object initializer analysis.
            // Tracking in https://github.com/dotnet/roslyn/issues/44759
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExprNominalRecord()
        {
            var src = @"
using System;
record C
{
    public int X { get; set; }
    public string Y { get; init; }
    public long Z;
    public event Action E;

    public C() { }
    public C(C other)
    {
        X = other.X;
        Y = other.Y;
        Z = other.Z;
        E = other.E;
    }

    public static void Main()
    {
        var c = new C() { X = 1, Y = ""2"", Z = 3, E = () => { } };
        var c2 = c with {};
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(ReferenceEquals(c, c2));
        Console.WriteLine(c2.X);
        Console.WriteLine(c2.Y);
        Console.WriteLine(c2.Z);
        Console.WriteLine(ReferenceEquals(c.E, c2.E));
        var c3 = c with { Y = ""3"", X = 2 };
        Console.WriteLine(c.Y);
        Console.WriteLine(c3.Y);
        Console.WriteLine(c.X);
        Console.WriteLine(c3.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
True
False
1
2
3
True
2
3
1
2").VerifyDiagnostics();
        }

        [Fact]
        public void WithExprNominalRecord2()
        {
            var comp1 = CreateCompilation(@"
public record C
{
    public int X { get; set; }
    public string Y { get; init; }
    public long Z;

    public C() { }
    public C(C other)
    {
        X = other.X;
        Y = other.Y;
        Z = other.Z;
    }
}");
            var verifier = CompileAndVerify(@"
class D
{
    public C M(C c) => c with
    {
        X = 5,
        Y = ""a"",
        Z = 2,
    };
}", references: new[] { comp1.EmitToImageReference() }).VerifyDiagnostics();

            verifier.VerifyIL("D.M", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0006:  dup
  IL_0007:  ldc.i4.5
  IL_0008:  callvirt   ""void C.X.set""
  IL_000d:  dup
  IL_000e:  ldstr      ""a""
  IL_0013:  callvirt   ""void C.Y.init""
  IL_0018:  dup
  IL_0019:  ldc.i4.2
  IL_001a:  conv.i8
  IL_001b:  stfld      ""long C.Z""
  IL_0020:  ret
}");
        }

        [Fact]
        public void WithExprAssignToRef1()
        {
            var src = @"
using System;
record C(int Y)
{
    private readonly int[] _a = new[] { 0 };
    public ref int X => ref _a[0];

    public static void Main()
    {
        var c = new C(0) { X = 5 };
        Console.WriteLine(c.X);
        c = c with { X = 1 };
        Console.WriteLine(c.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
5
1").VerifyDiagnostics();

            verifier.VerifyIL("C.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""C..ctor(int)""
  IL_0006:  dup
  IL_0007:  callvirt   ""ref int C.X.get""
  IL_000c:  ldc.i4.5
  IL_000d:  stind.i4
  IL_000e:  dup
  IL_000f:  callvirt   ""ref int C.X.get""
  IL_0014:  ldind.i4
  IL_0015:  call       ""void System.Console.WriteLine(int)""
  IL_001a:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_001f:  dup
  IL_0020:  callvirt   ""ref int C.X.get""
  IL_0025:  ldc.i4.1
  IL_0026:  stind.i4
  IL_0027:  callvirt   ""ref int C.X.get""
  IL_002c:  ldind.i4
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  ret
}");
        }

        [Fact]
        public void WithExprAssignToRef2()
        {
            var src = @"
using System;
record C(int Y)
{
    private readonly int[] _a = new[] { 0 };
    public ref int X
    {
        get => ref _a[0];
        set { }
    }

    public static void Main()
    {
        var a = new[] { 0 };
        var c = new C(0) { X = ref a[0] };
        Console.WriteLine(c.X);
        c = c with { X = ref a[0] };
        Console.WriteLine(c.X);
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,9): error CS8147: Properties which return by reference cannot have set accessors
                //         set { }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithArguments("C.X.set").WithLocation(9, 9),
                // (15,32): error CS1525: Invalid expression term 'ref'
                //         var c = new C(0) { X = ref a[0] };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a[0]").WithArguments("ref").WithLocation(15, 32),
                // (15,32): error CS1073: Unexpected token 'ref'
                //         var c = new C(0) { X = ref a[0] };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(15, 32),
                // (17,26): error CS1073: Unexpected token 'ref'
                //         c = c with { X = ref a[0] };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(17, 26)
            );
        }

        [Fact]
        public void WithExpressionSameLHS()
        {
            var comp = CreateCompilation(@"
record C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { X = 1, X = 2};
    }
}");
            comp.VerifyDiagnostics(
                // (7,29): error CS1912: Duplicate initialization of member 'X'
                //         c = c with { X = 1, X = 2};
                Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "X").WithArguments("X").WithLocation(7, 29)
            );
        }

        [WorkItem(44616, "https://github.com/dotnet/roslyn/issues/44616")]
        [Fact]
        public void Inheritance_01()
        {
            var source =
@"record A
{
    internal A() { }
    public object P1 { get; set; }
    internal object P2 { get; set; }
    protected internal object P3 { get; set; }
    protected object P4 { get; set; }
    private protected object P5 { get; set; }
    private object P6 { get; set; }
}
record B(object P1, object P2, object P3, object P4, object P5, object P6) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P6 { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [WorkItem(44616, "https://github.com/dotnet/roslyn/issues/44616")]
        [Fact]
        public void Inheritance_02()
        {
            var source =
@"record A
{
    internal A() { }
    private protected object P1 { get; set; }
    private object P2 { get; set; }
    private record B(object P1, object P2) : A
    {
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "A.B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type A.B.EqualityContract { get; }" }, actualMembers);
        }

        [WorkItem(44616, "https://github.com/dotnet/roslyn/issues/44616")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Inheritance_03(bool useCompilationReference)
        {
            var sourceA =
@"public record A
{
    public A() { }
    internal object P { get; set; }
}
public record B(object Q) : A
{
    public B() : this(null) { }
}
record C1(object P, object Q) : B
{
}";
            var comp = CreateCompilation(sourceA);
            AssertEx.Equal(new[] { "System.Type C1.EqualityContract { get; }" }, GetProperties(comp, "C1").ToTestDisplayStrings());
            var refA = useCompilationReference ? comp.ToMetadataReference() : comp.EmitToImageReference();

            var sourceB =
@"record C2(object P, object Q) : B
{
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            AssertEx.Equal(new[] { "System.Type C2.EqualityContract { get; }", "System.Object C2.P { get; init; }" }, GetProperties(comp, "C2").ToTestDisplayStrings());
        }

        [WorkItem(44616, "https://github.com/dotnet/roslyn/issues/44616")]
        [Fact]
        public void Inheritance_04()
        {
            var source =
@"record A
{
    internal A() { }
    public object P1 { get { return null; } set { } }
    public object P2 { get; init; }
    public object P3 { get; }
    public object P4 { set { } }
    public virtual object P5 { get; set; }
    public static object P6 { get; set; }
    public ref object P7 => throw null;
}
record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,50): error CS8866: Record member 'A.P4' must be a readable instance property of type 'object' to match positional parameter 'P4'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P4").WithArguments("A.P4", "object", "P4").WithLocation(12, 50),
                // (12,72): error CS8866: Record member 'A.P6' must be a readable instance property of type 'object' to match positional parameter 'P6'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P6").WithArguments("A.P6", "object", "P6").WithLocation(12, 72));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_05()
        {
            var source =
@"record A
{
    internal A() { }
    public object P1 { get; set; }
    public int P2 { get; set; }
}
record B(int P1, object P2) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,14): error CS8866: Record member 'A.P1' must be a readable instance property of type 'int' to match positional parameter 'P1'.
                // record B(int P1, object P2) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("A.P1", "int", "P1").WithLocation(7, 14),
                // (7,25): error CS8866: Record member 'A.P2' must be a readable instance property of type 'object' to match positional parameter 'P2'.
                // record B(int P1, object P2) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("A.P2", "object", "P2").WithLocation(7, 25));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_06()
        {
            var source =
@"record A
{
    internal int X { get; set; }
    internal int Y { set { } }
    internal int Z;
}
record B(int X, int Y, int Z) : A
{
}
class Program
{
    static void Main()
    {
        var b = new B(1, 2, 3);
        b.X = 4;
        b.Y = 5;
        b.Z = 6;
        ((A)b).X = 7;
        ((A)b).Y = 8;
        ((A)b).Z = 9;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,21): error CS8866: Record member 'A.Y' must be a readable instance property of type 'int' to match positional parameter 'Y'.
                // record B(int X, int Y, int Z) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("A.Y", "int", "Y").WithLocation(7, 21),
                // (7,28): error CS8866: Record member 'A.Z' must be a readable instance property of type 'int' to match positional parameter 'Z'.
                // record B(int X, int Y, int Z) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Z").WithArguments("A.Z", "int", "Z").WithLocation(7, 28));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_07()
        {
            var source =
@"abstract record A
{
    public abstract int X { get; }
    public virtual int Y { get; }
}
abstract record B1(int X, int Y) : A
{
}
record B2(int X, int Y) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,24): error CS0546: 'B1.X.init': cannot override because 'A.X' does not have an overridable set accessor
                // abstract record B1(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "X").WithArguments("B1.X.init", "A.X").WithLocation(6, 24),
                // (9,15): error CS0546: 'B2.X.init': cannot override because 'A.X' does not have an overridable set accessor
                // record B2(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "X").WithArguments("B2.X.init", "A.X").WithLocation(9, 15));

            AssertEx.Equal(new[] { "System.Type B1.EqualityContract { get; }", "System.Int32 B1.X { get; init; }" }, GetProperties(comp, "B1").ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.Type B2.EqualityContract { get; }", "System.Int32 B2.X { get; init; }" }, GetProperties(comp, "B2").ToTestDisplayStrings());

            var b1Ctor = comp.GetTypeByMetadataName("B1")!.GetMembersUnordered().OfType<SynthesizedRecordConstructor>().Single();
            Assert.Equal("B1..ctor(System.Int32 X, System.Int32 Y)", b1Ctor.ToTestDisplayString());
            Assert.Equal(Accessibility.Protected, b1Ctor.DeclaredAccessibility);
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_08()
        {
            var source =
@"abstract record A
{
    public abstract int X { get; }
    public virtual int Y { get; }
    public virtual int Z { get; }
}
abstract record B : A
{
    public override abstract int Y { get; }
}
record C(int X, int Y, int Z) : B
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,14): error CS0546: 'C.X.init': cannot override because 'A.X' does not have an overridable set accessor
                // record C(int X, int Y, int Z) : B
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "X").WithArguments("C.X.init", "A.X").WithLocation(11, 14),
                // (11,21): error CS0546: 'C.Y.init': cannot override because 'B.Y' does not have an overridable set accessor
                // record C(int X, int Y, int Z) : B
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "Y").WithArguments("C.Y.init", "B.Y").WithLocation(11, 21));

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }", "System.Int32 C.X { get; init; }", "System.Int32 C.Y { get; init; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_09()
        {
            var source =
@"abstract record C(int X, int Y)
{
    public abstract int X { get; }
    public virtual int Y { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "C..ctor(System.Int32 X, System.Int32 Y)",
                "System.Type C.EqualityContract.get",
                "System.Type C.EqualityContract { get; }",
                "System.Int32 C.X { get; }",
                "System.Int32 C.X.get",
                "System.Int32 C.<Y>k__BackingField",
                "System.Int32 C.Y { get; }",
                "System.Int32 C.Y.get",
                "System.String C.ToString()",
                "System.Boolean C." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean C.op_Inequality(C? r1, C? r2)",
                "System.Boolean C.op_Equality(C? r1, C? r2)",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? obj)",
                "System.Boolean C.Equals(C? other)",
                "C C." + WellKnownMemberNames.CloneMethodName + "()",
                "C..ctor(C original)",
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_10()
        {
            var source =
@"using System;
interface IA
{
    int X { get; }
}
interface IB
{
    int Y { get; }
}
record C(int X, int Y) : IA, IB
{
}
class Program
{
    static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(""{0}, {1}"", c.X, c.Y);
        Console.WriteLine(""{0}, {1}"", ((IA)c).X, ((IB)c).Y);
    }
}";
            CompileAndVerify(source, expectedOutput:
@"1, 2
1, 2").VerifyDiagnostics();
        }

        [Fact]
        public void Inheritance_11()
        {
            var source =
@"interface IA
{
    int X { get; }
}
interface IB
{
    object Y { get; set; }
}
record C(object X, object Y) : IA, IB
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,32): error CS0738: 'C' does not implement interface member 'IA.X'. 'C.X' cannot implement 'IA.X' because it does not have the matching return type of 'int'.
                // record C(object X, object Y) : IA, IB
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "IA").WithArguments("C", "IA.X", "C.X", "int").WithLocation(9, 32),
                // (9,36): error CS8854: 'C' does not implement interface member 'IB.Y.set'. 'C.Y.init' cannot implement 'IB.Y.set'.
                // record C(object X, object Y) : IA, IB
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "IB").WithArguments("C", "IB.Y.set", "C.Y.init").WithLocation(9, 36)
            );
        }

        [Fact]
        public void Inheritance_12()
        {
            var source =
@"record A
{
    public object X { get; }
    public object Y { get; }
}
record B(object X, object Y) : A
{
    public object X { get; }
    public object Y { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): warning CS0108: 'B.X' hides inherited member 'A.X'. Use the new keyword if hiding was intended.
                //     public object X { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "X").WithArguments("B.X", "A.X").WithLocation(8, 19),
                // (9,19): warning CS0108: 'B.Y' hides inherited member 'A.Y'. Use the new keyword if hiding was intended.
                //     public object Y { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "Y").WithArguments("B.Y", "A.Y").WithLocation(9, 19));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.X { get; }",
                "System.Object B.Y { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_13()
        {
            var source =
@"record A(object X, object Y)
{
    internal A() : this(null, null) { }
}
record B(object X, object Y) : A
{
    public object X { get; }
    public object Y { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,19): warning CS0108: 'B.X' hides inherited member 'A.X'. Use the new keyword if hiding was intended.
                //     public object X { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "X").WithArguments("B.X", "A.X").WithLocation(7, 19),
                // (8,19): warning CS0108: 'B.Y' hides inherited member 'A.Y'. Use the new keyword if hiding was intended.
                //     public object Y { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "Y").WithArguments("B.Y", "A.Y").WithLocation(8, 19));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.X { get; }",
                "System.Object B.Y { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_14()
        {
            var source =
@"record A
{
    public object P1 { get; }
    public object P2 { get; }
    public object P3 { get; }
    public object P4 { get; }
}
record B : A
{
    public new int P1 { get; }
    public new int P2 { get; }
}
record C(object P1, int P2, object P3, int P4) : B
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,17): error CS8866: Record member 'B.P1' must be a readable instance property of type 'object' to match positional parameter 'P1'.
                // record C(object P1, int P2, object P3, int P4) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("B.P1", "object", "P1").WithLocation(13, 17),
                // (13,44): error CS8866: Record member 'A.P4' must be a readable instance property of type 'int' to match positional parameter 'P4'.
                // record C(object P1, int P2, object P3, int P4) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P4").WithArguments("A.P4", "int", "P4").WithLocation(13, 44));
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_15()
        {
            var source =
@"record C(int P1, object P2)
{
    public object P1 { get; set; }
    public int P2 { get; set; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,14): error CS8866: Record member 'C.P1' must be a readable instance property of type 'int' to match positional parameter 'P1'.
                // record C(int P1, object P2)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("C.P1", "int", "P1").WithLocation(1, 14),
                // (1,25): error CS8866: Record member 'C.P2' must be a readable instance property of type 'object' to match positional parameter 'P2'.
                // record C(int P1, object P2)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("C.P2", "object", "P2").WithLocation(1, 25));
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.P1 { get; set; }",
                "System.Int32 C.P2 { get; set; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_16()
        {
            var source =
@"record A
{
    public int P1 { get; }
    public int P2 { get; }
    public int P3 { get; }
    public int P4 { get; }
}
record B(object P1, int P2, object P3, int P4) : A
{
    public new object P1 { get; }
    public new object P2 { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,25): error CS8866: Record member 'B.P2' must be a readable instance property of type 'int' to match positional parameter 'P2'.
                // record B(object P1, int P2, object P3, int P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("B.P2", "int", "P2").WithLocation(8, 25),
                // (8,36): error CS8866: Record member 'A.P3' must be a readable instance property of type 'object' to match positional parameter 'P3'.
                // record B(object P1, int P2, object P3, int P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P3").WithArguments("A.P3", "object", "P3").WithLocation(8, 36));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P1 { get; }",
                "System.Object B.P2 { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_17()
        {
            var source =
@"record A
{
    public object P1 { get; }
    public object P2 { get; }
    public object P3 { get; }
    public object P4 { get; }
}
record B(object P1, int P2, object P3, int P4) : A
{
    public new int P1 { get; }
    public new int P2 { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,17): error CS8866: Record member 'B.P1' must be a readable instance property of type 'object' to match positional parameter 'P1'.
                // record B(object P1, int P2, object P3, int P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("B.P1", "object", "P1").WithLocation(8, 17),
                // (8,44): error CS8866: Record member 'A.P4' must be a readable instance property of type 'int' to match positional parameter 'P4'.
                // record B(object P1, int P2, object P3, int P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P4").WithArguments("A.P4", "int", "P4").WithLocation(8, 44));

            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Int32 B.P1 { get; }",
                "System.Int32 B.P2 { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_18()
        {
            var source =
@"record C(object P1, object P2, object P3, object P4, object P5)
{
    public object P1 { get { return null; } set { } }
    public object P2 { get; }
    public object P3 { set { } }
    public static object P4 { get; set; }
    public ref object P5 => throw null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,39): error CS8866: Record member 'C.P3' must be a readable instance property of type 'object' to match positional parameter 'P3'.
                // record C(object P1, object P2, object P3, object P4, object P5)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P3").WithArguments("C.P3", "object", "P3").WithLocation(1, 39),
                // (1,50): error CS8866: Record member 'C.P4' must be a readable instance property of type 'object' to match positional parameter 'P4'.
                // record C(object P1, object P2, object P3, object P4, object P5)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P4").WithArguments("C.P4", "object", "P4").WithLocation(1, 50));

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.P1 { get; set; }",
                "System.Object C.P2 { get; }",
                "System.Object C.P3 { set; }",
                "System.Object C.P4 { get; set; }",
                "ref System.Object C.P5 { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_19()
        {
            var source =
@"#pragma warning disable 8618
#nullable enable
record A
{
    internal A() { }
    public object P1 { get; }
    public dynamic[] P2 { get; }
    public object? P3 { get; }
    public object[] P4 { get; }
    public (int X, int Y) P5 { get; }
    public (int, int)[] P6 { get; }
    public nint P7 { get; }
    public System.UIntPtr[] P8 { get; }
}
record B(dynamic P1, object[] P2, object P3, object?[] P4, (int, int) P5, (int X, int Y)[] P6, System.IntPtr P7, nuint[] P8) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_20()
        {
            var source =
@"#pragma warning disable 8618
#nullable enable
record C(dynamic P1, object[] P2, object P3, object?[] P4, (int, int) P5, (int X, int Y)[] P6, System.IntPtr P7, nuint[] P8)
{
    public object P1 { get; }
    public dynamic[] P2 { get; }
    public object? P3 { get; }
    public object[] P4 { get; }
    public (int X, int Y) P5 { get; }
    public (int, int)[] P6 { get; }
    public nint P7 { get; }
    public System.UIntPtr[] P8 { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.P1 { get; }",
                "dynamic[] C.P2 { get; }",
                "System.Object? C.P3 { get; }",
                "System.Object[] C.P4 { get; }",
                "(System.Int32 X, System.Int32 Y) C.P5 { get; }",
                "(System.Int32, System.Int32)[] C.P6 { get; }",
                "nint C.P7 { get; }",
                "System.UIntPtr[] C.P8 { get; }"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Inheritance_21(bool useCompilationReference)
        {
            var sourceA =
@"public record A
{
    public object P1 { get; }
    internal object P2 { get; }
}
public record B : A
{
    internal new object P1 { get; }
    public new object P2 { get; }
}";
            var comp = CreateCompilation(sourceA);
            var refA = useCompilationReference ? comp.ToMetadataReference() : comp.EmitToImageReference();

            var sourceB =
@"record C(object P1, object P2) : B
{
}
class Program
{
    static void Main()
    {
        var c = new C(1, 2);
        System.Console.WriteLine(""({0}, {1})"", c.P1, c.P2);
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);

            var verifier = CompileAndVerify(comp, expectedOutput: "(, )").VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(object, object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""B..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ret
}");

            verifier.VerifyIL("Program.Main",
@"{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (C V_0) //c
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""int""
  IL_0006:  ldc.i4.2
  IL_0007:  box        ""int""
  IL_000c:  newobj     ""C..ctor(object, object)""
  IL_0011:  stloc.0
  IL_0012:  ldstr      ""({0}, {1})""
  IL_0017:  ldloc.0
  IL_0018:  callvirt   ""object A.P1.get""
  IL_001d:  ldloc.0
  IL_001e:  callvirt   ""object B.P2.get""
  IL_0023:  call       ""void System.Console.WriteLine(string, object, object)""
  IL_0028:  ret
}");
        }

        [Fact]
        public void Inheritance_22()
        {
            var source =
@"record A
{
    public ref object P1 => throw null;
    public object P2 => throw null;
}
record B : A
{
    public new object P1 => throw null;
    public new ref object P2 => throw null;
}
record C(object P1, object P2) : B
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_23()
        {
            var source =
@"record A
{
    public static object P1 { get; }
    public object P2 { get; }
}
record B : A
{
    public new object P1 { get; }
    public new static object P2 { get; }
}
record C(object P1, object P2) : B
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,28): error CS8866: Record member 'B.P2' must be a readable instance property of type 'object' to match positional parameter 'P2'.
                // record C(object P1, object P2) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("B.P2", "object", "P2").WithLocation(11, 28));
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_24()
        {
            var source =
@"record A
{
    public object get_P() => null;
    public object set_Q() => null;
}
record B(object P, object Q) : A
{
}
record C(object P)
{
    public object get_P() => null;
    public object set_Q() => null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,17): error CS0082: Type 'C' already reserves a member called 'get_P' with the same parameter types
                // record C(object P)
                Diagnostic(ErrorCode.ERR_MemberReserved, "P").WithArguments("get_P", "C").WithLocation(9, 17));

            var expectedMembers = new[]
            {
                "B..ctor(System.Object P, System.Object Q)",
                "System.Type B.EqualityContract.get",
                "System.Type B.EqualityContract { get; }",
                "System.Object B.<P>k__BackingField",
                "System.Object B.P.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.P.init",
                "System.Object B.P { get; init; }",
                "System.Object B.<Q>k__BackingField",
                "System.Object B.Q.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.Q.init",
                "System.Object B.Q { get; init; }",
                "System.String B.ToString()",
                "System.Boolean B." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean B.op_Inequality(B? r1, B? r2)",
                "System.Boolean B.op_Equality(B? r1, B? r2)",
                "System.Int32 B.GetHashCode()",
                "System.Boolean B.Equals(System.Object? obj)",
                "System.Boolean B.Equals(A? other)",
                "System.Boolean B.Equals(B? other)",
                "A B." + WellKnownMemberNames.CloneMethodName + "()",
                "B..ctor(B original)",
                "void B.Deconstruct(out System.Object P, out System.Object Q)"
            };
            AssertEx.Equal(expectedMembers, comp.GetMember<NamedTypeSymbol>("B").GetMembers().ToTestDisplayStrings());

            expectedMembers = new[]
            {
                "C..ctor(System.Object P)",
                "System.Type C.EqualityContract.get",
                "System.Type C.EqualityContract { get; }",
                "System.Object C.<P>k__BackingField",
                "System.Object C.P.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.P.init",
                "System.Object C.P { get; init; }",
                "System.Object C.get_P()",
                "System.Object C.set_Q()",
                "System.String C.ToString()",
                "System.Boolean C." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean C.op_Inequality(C? r1, C? r2)",
                "System.Boolean C.op_Equality(C? r1, C? r2)",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? obj)",
                "System.Boolean C.Equals(C? other)",
                "C C." + WellKnownMemberNames.CloneMethodName + "()",
                "C..ctor(C original)",
                "void C.Deconstruct(out System.Object P)"
            };
            AssertEx.Equal(expectedMembers, comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings());
        }

        [Fact]
        public void Inheritance_25()
        {
            var sourceA =
@"public record A
{
    public class P1 { }
    internal object P2 = 2;
    public int P3(object o) => 3;
    internal int P4<T>(T t) => 4;
}";
            var sourceB =
@"record B(object P1, object P2, object P3, object P4) : A
{
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // (1,17): error CS8866: Record member 'A.P1' must be a readable instance property of type 'object' to match positional parameter 'P1'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("A.P1", "object", "P1").WithLocation(1, 17),
                // (1,28): error CS8866: Record member 'A.P2' must be a readable instance property of type 'object' to match positional parameter 'P2'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("A.P2", "object", "P2").WithLocation(1, 28),
                // (1,39): error CS8866: Record member 'A.P3' must be a readable instance property of type 'object' to match positional parameter 'P3'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P3").WithArguments("A.P3", "object", "P3").WithLocation(1, 39));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P4 { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);

            comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,17): error CS8866: Record member 'A.P1' must be a readable instance property of type 'object' to match positional parameter 'P1'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("A.P1", "object", "P1").WithLocation(1, 17),
                // (1,39): error CS8866: Record member 'A.P3' must be a readable instance property of type 'object' to match positional parameter 'P3'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P3").WithArguments("A.P3", "object", "P3").WithLocation(1, 39));
            actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P2 { get; init; }",
                "System.Object B.P4 { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_26()
        {
            var sourceA =
@"public record A
{
    internal const int P = 4;
}";
            var sourceB =
@"record B(object P) : A
{
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // (1,17): error CS8866: Record member 'A.P' must be a readable instance property of type 'object' to match positional parameter 'P'.
                // record B(object P) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P").WithArguments("A.P", "object", "P").WithLocation(1, 17));
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, GetProperties(comp, "B").ToTestDisplayStrings());

            comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }", "System.Object B.P { get; init; }" }, GetProperties(comp, "B").ToTestDisplayStrings());
        }

        [Fact]
        public void Inheritance_27()
        {
            var source =
@"record A
{
    public object P { get; }
    public object Q { get; set; }
}
record B(object get_P, object set_Q) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.get_P { get; init; }",
                "System.Object B.set_Q { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_28()
        {
            var source =
@"interface I
{
    object P { get; }
}
record A : I
{
    object I.P => null;
}
record B(object P) : A
{
}
record C(object P) : I
{
    object I.P => null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }", "System.Object B.P { get; init; }" }, GetProperties(comp, "B").ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }", "System.Object C.P { get; init; }", "System.Object C.I.P { get; }" }, GetProperties(comp, "C").ToTestDisplayStrings());
        }

        [Fact]
        public void Inheritance_29()
        {
            var sourceA =
@"Public Class A
    Public Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Property Q(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
";
            var compA = CreateVisualBasicCompilation(sourceA);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();

            var sourceB =
@"record B(object P, object Q) : A
{
    object P { get; }
}";
            var compB = CreateCompilation(new[] { sourceB, IsExternalInitTypeDefinition }, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            compB.VerifyDiagnostics(
                // (1,8): error CS0115: 'B.EqualityContract': no suitable method found to override
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B.EqualityContract").WithLocation(1, 8),
                // (1,8): error CS0115: 'B.Equals(A?)': no suitable method found to override
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B.Equals(A?)").WithLocation(1, 8),
                // (1,8): error CS8867: No accessible copy constructor found in base type 'A'.
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "B").WithArguments("A").WithLocation(1, 8),
                // (1,32): error CS8864: Records may only inherit from object or another record
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(1, 32)
            );

            var actualMembers = GetProperties(compB, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.Q { get; init; }",
                "System.Object B.P { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_30()
        {
            var sourceA =
@"Public Class A
    Public ReadOnly Overloads Property P() As Object
        Get
            Return Nothing
        End Get
    End Property
    Public ReadOnly Overloads Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Overloads Property Q(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Overloads Property Q() As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
";
            var compA = CreateVisualBasicCompilation(sourceA);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();

            var sourceB =
@"record B(object P, object Q) : A
{
}";
            var compB = CreateCompilation(new[] { sourceB, IsExternalInitTypeDefinition }, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            compB.VerifyDiagnostics(
                // (1,8): error CS0115: 'B.EqualityContract': no suitable method found to override
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B.EqualityContract").WithLocation(1, 8),
                // (1,8): error CS0115: 'B.Equals(A?)': no suitable method found to override
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B.Equals(A?)").WithLocation(1, 8),
                // (1,8): error CS8867: No accessible copy constructor found in base type 'A'.
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "B").WithArguments("A").WithLocation(1, 8),
                // (1,32): error CS8864: Records may only inherit from object or another record
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(1, 32)
            );

            var actualMembers = GetProperties(compB, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_31()
        {
            var sourceA =
@"Public Class A
    Public ReadOnly Property P() As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Property Q(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Property R(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Sub New(a as A)
    End Sub
End Class
Public Class B
    Inherits A
    Public ReadOnly Overloads Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Overloads Property Q() As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Overloads Property R(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Sub New(b as B)
        MyBase.New(b)
    End Sub
End Class
";
            var compA = CreateVisualBasicCompilation(sourceA);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();

            var sourceB =
@"record C(object P, object Q, object R) : B
{
}";
            var compB = CreateCompilation(new[] { sourceB, IsExternalInitTypeDefinition }, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            compB.VerifyDiagnostics(
                // (1,8): error CS0115: 'C.EqualityContract': no suitable method found to override
                // record C(object P, object Q, object R) : B
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C").WithArguments("C.EqualityContract").WithLocation(1, 8),
                // (1,8): error CS0115: 'C.Equals(B?)': no suitable method found to override
                // record C(object P, object Q, object R) : B
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C").WithArguments("C.Equals(B?)").WithLocation(1, 8),
                // (1,8): error CS7036: There is no argument given that corresponds to the required formal parameter 'b' of 'B.B(B)'
                // record C(object P, object Q, object R) : B
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("b", "B.B(B)").WithLocation(1, 8),
                // (1,42): error CS8864: Records may only inherit from object or another record
                // record C(object P, object Q, object R) : B
                Diagnostic(ErrorCode.ERR_BadRecordBase, "B").WithLocation(1, 42)
            );

            var actualMembers = GetProperties(compB, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.R { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_32()
        {
            var source =
@"record A
{
    public virtual object P1 { get; }
    public virtual object P2 { get; set; }
    public virtual object P3 { get; protected init; }
    public virtual object P4 { protected get; init; }
    public virtual object P5 { init { } }
    public virtual object P6 { set { } }
    public static object P7 { get; set; }
}
record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,61): error CS8866: Record member 'A.P5' must be a readable instance property of type 'object' to match positional parameter 'P5'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P5").WithArguments("A.P5", "object", "P5").WithLocation(11, 61),
                // (11,72): error CS8866: Record member 'A.P6' must be a readable instance property of type 'object' to match positional parameter 'P6'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P6").WithArguments("A.P6", "object", "P6").WithLocation(11, 72),
                // (11,83): error CS8866: Record member 'A.P7' must be a readable instance property of type 'object' to match positional parameter 'P7'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P7").WithArguments("A.P7", "object", "P7").WithLocation(11, 83));

            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, GetProperties(comp, "B").ToTestDisplayStrings());
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_33()
        {
            var source =
@"abstract record A
{
    public abstract object P1 { get; }
    public abstract object P2 { get; set; }
    public abstract object P3 { get; protected init; }
    public abstract object P4 { protected get; init; }
    public abstract object P5 { init; }
    public abstract object P6 { set; }
}
record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,8): error CS0534: 'B' does not implement inherited abstract member 'A.P6.set'
                // record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.P6.set").WithLocation(10, 8),
                // (10,8): error CS0534: 'B' does not implement inherited abstract member 'A.P5.init'
                // record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.P5.init").WithLocation(10, 8),
                // (10,17): error CS0546: 'B.P1.init': cannot override because 'A.P1' does not have an overridable set accessor
                // record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "P1").WithArguments("B.P1.init", "A.P1").WithLocation(10, 17),
                // (10,28): error CS8853: 'B.P2' must match by init-only of overridden member 'A.P2'
                // record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
                Diagnostic(ErrorCode.ERR_CantChangeInitOnlyOnOverride, "P2").WithArguments("B.P2", "A.P2").WithLocation(10, 28),
                // (10,39): error CS0507: 'B.P3.init': cannot change access modifiers when overriding 'protected' inherited member 'A.P3.init'
                // record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "P3").WithArguments("B.P3.init", "protected", "A.P3.init").WithLocation(10, 39),
                // (10,50): error CS0507: 'B.P4.get': cannot change access modifiers when overriding 'protected' inherited member 'A.P4.get'
                // record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "P4").WithArguments("B.P4.get", "protected", "A.P4.get").WithLocation(10, 50),
                // (10,61): error CS8866: Record member 'A.P5' must be a readable instance property of type 'object' to match positional parameter 'P5'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P5").WithArguments("A.P5", "object", "P5").WithLocation(10, 61),
                // (10,72): error CS8866: Record member 'A.P6' must be a readable instance property of type 'object' to match positional parameter 'P6'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6) : A;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P6").WithArguments("A.P6", "object", "P6").WithLocation(10, 72));

            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P1 { get; init; }",
                "System.Object B.P2 { get; init; }",
                "System.Object B.P3 { get; init; }",
                "System.Object B.P4 { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_34()
        {
            var source =
@"abstract record A
{
    public abstract object P1 { get; init; }
    public virtual object P2 { get; init; }
}
record B(string P1, string P2) : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,8): error CS0534: 'B' does not implement inherited abstract member 'A.P1.get'
                // record B(string P1, string P2) : A;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.P1.get").WithLocation(6, 8),
                // (6,8): error CS0534: 'B' does not implement inherited abstract member 'A.P1.init'
                // record B(string P1, string P2) : A;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.P1.init").WithLocation(6, 8),
                // (6,17): error CS8866: Record member 'A.P1' must be a readable instance property of type 'string' to match positional parameter 'P1'.
                // record B(string P1, string P2) : A;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("A.P1", "string", "P1").WithLocation(6, 17),
                // (6,28): error CS8866: Record member 'A.P2' must be a readable instance property of type 'string' to match positional parameter 'P2'.
                // record B(string P1, string P2) : A;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("A.P2", "string", "P2").WithLocation(6, 28));

            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, GetProperties(comp, "B").ToTestDisplayStrings());
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_35()
        {
            var source =
@"using static System.Console;
abstract record A(object X, object Y)
{
    public abstract object X { get; }
    public abstract object Y { get; init; }
}
record B(object X, object Y) : A(X, Y)
{
    public override object X { get; } = X;
}
class Program
{
    static void Main()
    {
        B b = new B(1, 2);
        A a = b;
        WriteLine((b.X, b.Y));
        WriteLine((a.X, a.Y));
        var (x, y) = b;
        WriteLine((x, y));
        (x, y) = a;
        WriteLine((x, y));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.Y { get; init; }",
                "System.Object B.X { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"(1, 2)
(1, 2)
(1, 2)
(1, 2)").VerifyDiagnostics();

            verifier.VerifyIL("A..ctor(object, object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("A..ctor(A)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("A.Deconstruct",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object A.X.get""
  IL_0007:  stind.ref
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  callvirt   ""object A.Y.get""
  IL_000f:  stind.ref
  IL_0010:  ret
}");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0015
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
            verifier.VerifyIL("A.GetHashCode()",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<System.Type> System.Collections.Generic.EqualityComparer<System.Type>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<System.Type>.GetHashCode(System.Type)""
  IL_0010:  ret
}");
            verifier.VerifyIL("B..ctor(object, object)",
@"{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.2
  IL_0002:  stfld      ""object B.<Y>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""object B.<X>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldarg.1
  IL_0010:  ldarg.2
  IL_0011:  call       ""A..ctor(object, object)""
  IL_0016:  ret
}");
            verifier.VerifyIL("B..ctor(B)",
@"{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""A..ctor(A)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object B.<Y>k__BackingField""
  IL_000e:  stfld      ""object B.<Y>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object B.<X>k__BackingField""
  IL_001a:  stfld      ""object B.<X>k__BackingField""
  IL_001f:  ret
}");
            verifier.VerifyIL("B.Deconstruct",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object A.X.get""
  IL_0007:  stind.ref
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  callvirt   ""object A.Y.get""
  IL_000f:  stind.ref
  IL_0010:  ret
}");
            verifier.VerifyIL("B.Equals(B)",
@"{
  // Code size       58 (0x3a)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  brfalse.s  IL_0038
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""object B.<Y>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object B.<Y>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<object>.Equals(object, object)""
  IL_001f:  brfalse.s  IL_0038
  IL_0021:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0026:  ldarg.0
  IL_0027:  ldfld      ""object B.<X>k__BackingField""
  IL_002c:  ldarg.1
  IL_002d:  ldfld      ""object B.<X>k__BackingField""
  IL_0032:  callvirt   ""bool System.Collections.Generic.EqualityComparer<object>.Equals(object, object)""
  IL_0037:  ret
  IL_0038:  ldc.i4.0
  IL_0039:  ret
}");
            verifier.VerifyIL("B.GetHashCode()",
@"{
  // Code size       53 (0x35)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""int A.GetHashCode()""
  IL_0006:  ldc.i4     0xa5555529
  IL_000b:  mul
  IL_000c:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""object B.<Y>k__BackingField""
  IL_0017:  callvirt   ""int System.Collections.Generic.EqualityComparer<object>.GetHashCode(object)""
  IL_001c:  add
  IL_001d:  ldc.i4     0xa5555529
  IL_0022:  mul
  IL_0023:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""object B.<X>k__BackingField""
  IL_002e:  callvirt   ""int System.Collections.Generic.EqualityComparer<object>.GetHashCode(object)""
  IL_0033:  add
  IL_0034:  ret
}");
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_36()
        {
            var source =
@"using static System.Console;
abstract record A
{
    public abstract object X { get; init; }
}
abstract record B : A
{
    public abstract object Y { get; init; }
}
record C(object X, object Y) : B;
class Program
{
    static void Main()
    {
        C c = new C(1, 2);
        B b = c;
        A a = c;
        WriteLine((c.X, c.Y));
        WriteLine((b.X, b.Y));
        WriteLine(a.X);
        var (x, y) = c;
        WriteLine((x, y));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.X { get; init; }",
                "System.Object C.Y { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"(1, 2)
(1, 2)
1
(1, 2)").VerifyDiagnostics();

            verifier.VerifyIL("A..ctor()",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("A..ctor(A)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0015
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
            verifier.VerifyIL("A.GetHashCode()",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<System.Type> System.Collections.Generic.EqualityComparer<System.Type>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<System.Type>.GetHashCode(System.Type)""
  IL_0010:  ret
}");
            verifier.VerifyIL("B..ctor()",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""A..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("B..ctor(B)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""A..ctor(A)""
  IL_0007:  ret
}");
            verifier.VerifyIL("B.Equals(B)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  ret
}");
            verifier.VerifyIL("B.GetHashCode()",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""int A.GetHashCode()""
  IL_0006:  ret
}");
            verifier.VerifyIL("C..ctor(object, object)",
@"{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""object C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""object C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  call       ""B..ctor()""
  IL_0014:  ret
}");
            verifier.VerifyIL("C..ctor(C)",
@"{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<X>k__BackingField""
  IL_000e:  stfld      ""object C.<X>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<Y>k__BackingField""
  IL_001a:  stfld      ""object C.<Y>k__BackingField""
  IL_001f:  ret
}");
            verifier.VerifyIL("C.Deconstruct",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object A.X.get""
  IL_0007:  stind.ref
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  callvirt   ""object B.Y.get""
  IL_000f:  stind.ref
  IL_0010:  ret
}");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       58 (0x3a)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  brfalse.s  IL_0038
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""object C.<X>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<X>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<object>.Equals(object, object)""
  IL_001f:  brfalse.s  IL_0038
  IL_0021:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0026:  ldarg.0
  IL_0027:  ldfld      ""object C.<Y>k__BackingField""
  IL_002c:  ldarg.1
  IL_002d:  ldfld      ""object C.<Y>k__BackingField""
  IL_0032:  callvirt   ""bool System.Collections.Generic.EqualityComparer<object>.Equals(object, object)""
  IL_0037:  ret
  IL_0038:  ldc.i4.0
  IL_0039:  ret
}");
            verifier.VerifyIL("C.GetHashCode()",
@"{
  // Code size       53 (0x35)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""int B.GetHashCode()""
  IL_0006:  ldc.i4     0xa5555529
  IL_000b:  mul
  IL_000c:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""object C.<X>k__BackingField""
  IL_0017:  callvirt   ""int System.Collections.Generic.EqualityComparer<object>.GetHashCode(object)""
  IL_001c:  add
  IL_001d:  ldc.i4     0xa5555529
  IL_0022:  mul
  IL_0023:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""object C.<Y>k__BackingField""
  IL_002e:  callvirt   ""int System.Collections.Generic.EqualityComparer<object>.GetHashCode(object)""
  IL_0033:  add
  IL_0034:  ret
}");
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_37()
        {
            var source =
@"using static System.Console;
abstract record A(object X, object Y)
{
    public abstract object X { get; init; }
    public virtual object Y { get; init; }
}
abstract record B(object X, object Y) : A(X, Y)
{
    public override abstract object X { get; init; }
    public override abstract object Y { get; init; }
}
record C(object X, object Y) : B(X, Y);
class Program
{
    static void Main()
    {
        C c = new C(1, 2);
        B b = c;
        A a = c;
        WriteLine((c.X, c.Y));
        WriteLine((b.X, b.Y));
        WriteLine((a.X, a.Y));
        var (x, y) = c;
        WriteLine((x, y));
        (x, y) = b;
        WriteLine((x, y));
        (x, y) = a;
        WriteLine((x, y));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.X { get; init; }",
                "System.Object C.Y { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"(1, 2)
(1, 2)
(1, 2)
(1, 2)
(1, 2)
(1, 2)").VerifyDiagnostics();

            verifier.VerifyIL("A..ctor(object, object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("A..ctor(A)",
@"{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  ldfld      ""object A.<Y>k__BackingField""
  IL_000d:  stfld      ""object A.<Y>k__BackingField""
  IL_0012:  ret
}");
            verifier.VerifyIL("A.Deconstruct",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object A.X.get""
  IL_0007:  stind.ref
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  callvirt   ""object A.Y.get""
  IL_000f:  stind.ref
  IL_0010:  ret
}");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_002d
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  brfalse.s  IL_002d
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""object A.<Y>k__BackingField""
  IL_0021:  ldarg.1
  IL_0022:  ldfld      ""object A.<Y>k__BackingField""
  IL_0027:  callvirt   ""bool System.Collections.Generic.EqualityComparer<object>.Equals(object, object)""
  IL_002c:  ret
  IL_002d:  ldc.i4.0
  IL_002e:  ret
}");
            verifier.VerifyIL("A.GetHashCode()",
@"{
  // Code size       40 (0x28)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<System.Type> System.Collections.Generic.EqualityComparer<System.Type>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<System.Type>.GetHashCode(System.Type)""
  IL_0010:  ldc.i4     0xa5555529
  IL_0015:  mul
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""object A.<Y>k__BackingField""
  IL_0021:  callvirt   ""int System.Collections.Generic.EqualityComparer<object>.GetHashCode(object)""
  IL_0026:  add
  IL_0027:  ret
}");
            verifier.VerifyIL("B..ctor(object, object)",
@"{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  call       ""A..ctor(object, object)""
  IL_0008:  ret
}");
            verifier.VerifyIL("B..ctor(B)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""A..ctor(A)""
  IL_0007:  ret
}");
            verifier.VerifyIL("B.Deconstruct",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object A.X.get""
  IL_0007:  stind.ref
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  callvirt   ""object A.Y.get""
  IL_000f:  stind.ref
  IL_0010:  ret
}");
            verifier.VerifyIL("B.Equals(B)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  ret
}");
            verifier.VerifyIL("B.GetHashCode()",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""int A.GetHashCode()""
  IL_0006:  ret
}");
            verifier.VerifyIL("C..ctor(object, object)",
@"{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""object C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""object C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldarg.1
  IL_0010:  ldarg.2
  IL_0011:  call       ""B..ctor(object, object)""
  IL_0016:  ret
}");
            verifier.VerifyIL("C..ctor(C)",
@"{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<X>k__BackingField""
  IL_000e:  stfld      ""object C.<X>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<Y>k__BackingField""
  IL_001a:  stfld      ""object C.<Y>k__BackingField""
  IL_001f:  ret
}");
            verifier.VerifyIL("C.Deconstruct",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""object A.X.get""
  IL_0007:  stind.ref
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  callvirt   ""object A.Y.get""
  IL_000f:  stind.ref
  IL_0010:  ret
}");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       58 (0x3a)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  brfalse.s  IL_0038
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""object C.<X>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<X>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<object>.Equals(object, object)""
  IL_001f:  brfalse.s  IL_0038
  IL_0021:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0026:  ldarg.0
  IL_0027:  ldfld      ""object C.<Y>k__BackingField""
  IL_002c:  ldarg.1
  IL_002d:  ldfld      ""object C.<Y>k__BackingField""
  IL_0032:  callvirt   ""bool System.Collections.Generic.EqualityComparer<object>.Equals(object, object)""
  IL_0037:  ret
  IL_0038:  ldc.i4.0
  IL_0039:  ret
}");
            verifier.VerifyIL("C.GetHashCode()",
@"{
  // Code size       53 (0x35)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""int B.GetHashCode()""
  IL_0006:  ldc.i4     0xa5555529
  IL_000b:  mul
  IL_000c:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""object C.<X>k__BackingField""
  IL_0017:  callvirt   ""int System.Collections.Generic.EqualityComparer<object>.GetHashCode(object)""
  IL_001c:  add
  IL_001d:  ldc.i4     0xa5555529
  IL_0022:  mul
  IL_0023:  call       ""System.Collections.Generic.EqualityComparer<object> System.Collections.Generic.EqualityComparer<object>.Default.get""
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""object C.<Y>k__BackingField""
  IL_002e:  callvirt   ""int System.Collections.Generic.EqualityComparer<object>.GetHashCode(object)""
  IL_0033:  add
  IL_0034:  ret
}");
        }

        // Member in intermediate base that hides abstract property. Not supported.
        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_38()
        {
            var source =
@"using static System.Console;
abstract record A
{
    public abstract object X { get; init; }
    public abstract object Y { get; init; }
}
abstract record B : A
{
    public new void X() { }
    public new struct Y { }
}
record C(object X, object Y) : B;
class Program
{
    static void Main()
    {
        C c = new C(1, 2);
        A a = c;
        WriteLine((a.X, a.Y));
        var (x, y) = c;
        WriteLine((x, y));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,21): error CS0533: 'B.X()' hides inherited abstract member 'A.X'
                //     public new void X() { }
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "X").WithArguments("B.X()", "A.X").WithLocation(9, 21),
                // (10,23): error CS0533: 'B.Y' hides inherited abstract member 'A.Y'
                //     public new struct Y { }
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "Y").WithArguments("B.Y", "A.Y").WithLocation(10, 23),
                // (12,8): error CS0534: 'C' does not implement inherited abstract member 'A.X.init'
                // record C(object X, object Y) : B;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C").WithArguments("C", "A.X.init").WithLocation(12, 8),
                // (12,8): error CS0534: 'C' does not implement inherited abstract member 'A.Y.init'
                // record C(object X, object Y) : B;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C").WithArguments("C", "A.Y.init").WithLocation(12, 8),
                // (12,8): error CS0534: 'C' does not implement inherited abstract member 'A.X.get'
                // record C(object X, object Y) : B;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C").WithArguments("C", "A.X.get").WithLocation(12, 8),
                // (12,8): error CS0534: 'C' does not implement inherited abstract member 'A.Y.get'
                // record C(object X, object Y) : B;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C").WithArguments("C", "A.Y.get").WithLocation(12, 8),
                // (12,17): error CS8866: Record member 'B.X' must be a readable instance property of type 'object' to match positional parameter 'X'.
                // record C(object X, object Y) : B;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("B.X", "object", "X").WithLocation(12, 17),
                // (12,27): error CS8866: Record member 'B.Y' must be a readable instance property of type 'object' to match positional parameter 'Y'.
                // record C(object X, object Y) : B;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("B.Y", "object", "Y").WithLocation(12, 27));

            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }", }, GetProperties(comp, "C").ToTestDisplayStrings());
        }

        // Member in intermediate base that hides abstract property. Not supported.
        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_39()
        {
            var sourceA =
@".class public System.Runtime.CompilerServices.IsExternalInit
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ldnull throw }
}
.class public abstract A
{
  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
  .method family hidebysig specialname rtspecialname instance void .ctor(class A A_1) { ldnull throw }
  .method public hidebysig newslot specialname abstract virtual instance class A  '" + WellKnownMemberNames.CloneMethodName + @"'() { }
  .property instance class [mscorlib]System.Type EqualityContract()
  {
    .get instance class [mscorlib]System.Type A::get_EqualityContract()
  }
  .property instance object P()
  {
    .get instance object A::get_P()
    .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) A::set_P(object)
  }
  .method family virtual instance class [mscorlib]System.Type get_EqualityContract() { ldnull ret }
  .method public abstract virtual instance object get_P() { }
  .method public abstract virtual instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(object 'value') { }

  .method public newslot virtual  
      instance bool Equals (
          class A ''
      ) cil managed 
  {
      .maxstack 8

      IL_0000: ldnull
      IL_0001: throw
  } // end of method A::Equals

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
.class public abstract B extends A
{
  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }
  .method family hidebysig specialname rtspecialname instance void .ctor(class B A_1) { ldnull throw }
  .method public hidebysig specialname abstract virtual instance class A  '" + WellKnownMemberNames.CloneMethodName + @"'() { }
  .property instance class [mscorlib]System.Type EqualityContract()
  {
    .get instance class [mscorlib]System.Type B::get_EqualityContract()
  }
  .method family virtual instance class [mscorlib]System.Type get_EqualityContract() { ldnull ret }
  .method public hidebysig instance object P() { ldnull ret }

  .method public newslot virtual  
      instance bool Equals (
          class B ''
      ) cil managed 
  {
      .maxstack 8

      IL_0000: ldnull
      IL_0001: throw
  } // end of method B::Equals

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"record CA(object P) : A;
record CB(object P) : B;
";
            var comp = CreateCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (2,8): error CS0534: 'CB' does not implement inherited abstract member 'A.P.get'
                // record CB(object P) : B;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "CB").WithArguments("CB", "A.P.get").WithLocation(2, 8),
                // (2,8): error CS0534: 'CB' does not implement inherited abstract member 'A.P.init'
                // record CB(object P) : B;
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "CB").WithArguments("CB", "A.P.init").WithLocation(2, 8),
                // (2,18): error CS8866: Record member 'B.P' must be a readable instance property of type 'object' to match positional parameter 'P'.
                // record CB(object P) : B;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P").WithArguments("B.P", "object", "P").WithLocation(2, 18));

            AssertEx.Equal(new[] { "System.Type CA.EqualityContract { get; }", "System.Object CA.P { get; init; }" }, GetProperties(comp, "CA").ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.Type CB.EqualityContract { get; }" }, GetProperties(comp, "CB").ToTestDisplayStrings());
        }

        // Accessor names that do not match the property name.
        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_40()
        {
            var sourceA =
@".class public System.Runtime.CompilerServices.IsExternalInit
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ldnull throw }
}
.class public abstract A
{
  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
  .method family hidebysig specialname rtspecialname instance void .ctor(class A A_1) { ldnull throw }
  .method public hidebysig newslot specialname abstract virtual instance class A  '" + WellKnownMemberNames.CloneMethodName + @"'() { }
  .property instance class [mscorlib]System.Type EqualityContract()
  {
    .get instance class [mscorlib]System.Type A::GetProperty1()
  }
  .property instance object P()
  {
    .get instance object A::GetProperty2()
    .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) A::SetProperty2(object)
  }
  .method family virtual instance class [mscorlib]System.Type GetProperty1() { ldnull ret }
  .method public abstract virtual instance object GetProperty2() { }
  .method public abstract virtual instance void modreq(System.Runtime.CompilerServices.IsExternalInit) SetProperty2(object 'value') { }

  .method public newslot virtual  
      instance bool Equals (
          class A ''
      ) cil managed 
  {
      .maxstack 8

      IL_0000: ldnull
      IL_0001: throw
  } // end of method A::Equals

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using static System.Console;
record B(object P) : A
{
    static void Main()
    {
        B b = new B(3);
        WriteLine(b.P);
        WriteLine(((A)b).P);
        b = new B(1) { P = 2 };
        WriteLine(b.P);
        WriteLine(b.EqualityContract.Name);
    }
}";
            var comp = CreateCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"3
3
2
B").VerifyDiagnostics();

            var actualMembers = GetProperties(comp, "B");
            Assert.Equal(2, actualMembers.Length);
            VerifyProperty(actualMembers[0], "System.Type B.EqualityContract { get; }", "GetProperty1", null);
            VerifyProperty(actualMembers[1], "System.Object B.P { get; init; }", "GetProperty2", "SetProperty2");
        }

        // Accessor names that do not match the property name and are not valid C# names.
        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_41()
        {
            var sourceA =
@".class public System.Runtime.CompilerServices.IsExternalInit
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ldnull throw }
}
.class public abstract A
{
  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
  .method family hidebysig specialname rtspecialname instance void .ctor(class A A_1) { ldnull throw }
  .method public hidebysig newslot specialname abstract virtual instance class A  '" + WellKnownMemberNames.CloneMethodName + @"'() { }
  .property instance class [mscorlib]System.Type EqualityContract()
  {
    .get instance class [mscorlib]System.Type A::'EqualityContract<>get'()
  }
  .property instance object P()
  {
    .get instance object A::'P<>get'()
    .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) A::'P<>set'(object)
  }
  .method family virtual instance class [mscorlib]System.Type 'EqualityContract<>get'() { ldnull ret }
  .method public abstract virtual instance object 'P<>get'() { }
  .method public abstract virtual instance void modreq(System.Runtime.CompilerServices.IsExternalInit) 'P<>set'(object 'value') { }

  .method public newslot virtual  
      instance bool Equals (
          class A ''
      ) cil managed 
  {
      .maxstack 8

      IL_0000: ldnull
      IL_0001: throw
  } // end of method A::Equals

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using static System.Console;
record B(object P) : A
{
    static void Main()
    {
        B b = new B(3);
        WriteLine(b.P);
        WriteLine(((A)b).P);
        b = new B(1) { P = 2 };
        WriteLine(b.P);
        WriteLine(b.EqualityContract.Name);
    }
}";
            var comp = CreateCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"3
3
2
B").VerifyDiagnostics();

            var actualMembers = GetProperties(comp, "B");
            Assert.Equal(2, actualMembers.Length);
            VerifyProperty(actualMembers[0], "System.Type B.EqualityContract { get; }", "EqualityContract<>get", null);
            VerifyProperty(actualMembers[1], "System.Object B.P { get; init; }", "P<>get", "P<>set");
        }

        private static void VerifyProperty(Symbol symbol, string propertyDescription, string? getterName, string? setterName)
        {
            var property = (PropertySymbol)symbol;
            Assert.Equal(propertyDescription, symbol.ToTestDisplayString());
            VerifyAccessor(property.GetMethod, getterName);
            VerifyAccessor(property.SetMethod, setterName);
        }

        private static void VerifyAccessor(MethodSymbol? accessor, string? name)
        {
            Assert.Equal(name, accessor?.Name);
            if (accessor is object)
            {
                Assert.True(accessor.HasSpecialName);
                foreach (var parameter in accessor.Parameters)
                {
                    Assert.Same(accessor, parameter.ContainingSymbol);
                }
            }
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_42()
        {
            var sourceA =
@".class public System.Runtime.CompilerServices.IsExternalInit
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ldnull throw }
}
.class public abstract A
{
  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
  .method family hidebysig specialname rtspecialname instance void .ctor(class A A_1) { ldnull throw }
  .method public hidebysig newslot specialname abstract virtual instance class A  '" + WellKnownMemberNames.CloneMethodName + @"'() { }
  .property instance class [mscorlib]System.Type modopt(int32) EqualityContract()
  {
    .get instance class [mscorlib]System.Type modopt(int32) A::get_EqualityContract()
  }
  .property instance object modopt(uint16) P()
  {
    .get instance object modopt(uint16) A::get_P()
    .set instance void modopt(uint8) modreq(System.Runtime.CompilerServices.IsExternalInit) A::set_P(object modopt(uint16))
  }
  .method family virtual instance class [mscorlib]System.Type modopt(int32) get_EqualityContract() { ldnull ret }
  .method public abstract virtual instance object modopt(uint16) get_P() { }
  .method public abstract virtual instance void modopt(uint8) modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(object modopt(uint16) 'value') { }

  .method public newslot virtual  
      instance bool Equals (
          class A ''
      ) cil managed 
  {
      .maxstack 8

      IL_0000: ldnull
      IL_0001: throw
  } // end of method A::Equals

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using static System.Console;
record B(object P) : A
{
    static void Main()
    {
        B b = new B(3);
        WriteLine(b.P);
        WriteLine(((A)b).P);
        b = new B(1) { P = 2 };
        WriteLine(b.P);
        WriteLine(b.EqualityContract.Name);
    }
}";
            var comp = CreateCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"3
3
2
B").VerifyDiagnostics();

            var actualMembers = GetProperties(comp, "B");
            Assert.Equal(2, actualMembers.Length);

            var property = (PropertySymbol)actualMembers[0];
            Assert.Equal("System.Type modopt(System.Int32) B.EqualityContract { get; }", property.ToTestDisplayString());
            verifyReturnType(property.GetMethod, CSharpCustomModifier.CreateOptional(comp.GetSpecialType(SpecialType.System_Int32)));

            property = (PropertySymbol)actualMembers[1];
            Assert.Equal("System.Object modopt(System.UInt16) B.P { get; init; }", property.ToTestDisplayString());
            verifyReturnType(property.GetMethod,
                CSharpCustomModifier.CreateOptional(comp.GetSpecialType(SpecialType.System_UInt16)));
            verifyReturnType(property.SetMethod,
                CSharpCustomModifier.CreateRequired(comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IsExternalInit)),
                CSharpCustomModifier.CreateOptional(comp.GetSpecialType(SpecialType.System_Byte)));
            verifyParameterType(property.SetMethod,
                CSharpCustomModifier.CreateOptional(comp.GetSpecialType(SpecialType.System_UInt16)));

            static void verifyReturnType(MethodSymbol method, params CustomModifier[] expectedModifiers)
            {
                var returnType = method.ReturnTypeWithAnnotations;
                Assert.True(method.OverriddenMethod.ReturnTypeWithAnnotations.Equals(returnType, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
                AssertEx.Equal(expectedModifiers, returnType.CustomModifiers);
            }

            static void verifyParameterType(MethodSymbol method, params CustomModifier[] expectedModifiers)
            {
                var parameterType = method.Parameters[0].TypeWithAnnotations;
                Assert.True(method.OverriddenMethod.Parameters[0].TypeWithAnnotations.Equals(parameterType, TypeCompareKind.ConsiderEverything));
                AssertEx.Equal(expectedModifiers, parameterType.CustomModifiers);
            }
        }

        [WorkItem(44618, "https://github.com/dotnet/roslyn/issues/44618")]
        [Fact]
        public void Inheritance_43()
        {
            var source =
@"#nullable enable
record A
{
    protected virtual System.Type? EqualityContract => null;
}
record B : A
{
    static void Main()
    {
        var b = new B();
        _ = b.EqualityContract.ToString();
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();

            Assert.Equal("System.Type! B.EqualityContract { get; }", GetProperties(comp, "B").Single().ToTestDisplayString(includeNonNullable: true));
        }

        // No EqualityContract property on base.
        [Fact]
        public void Inheritance_44()
        {
            var sourceA =
@".class public System.Runtime.CompilerServices.IsExternalInit
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ldnull throw }
}
.class public A
{
  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
  .method family hidebysig specialname rtspecialname instance void .ctor(class A A_1) { ldnull throw }
  .method public hidebysig newslot specialname virtual instance class A  '" + WellKnownMemberNames.CloneMethodName + @"'() { ldnull throw }
  .property instance object P()
  {
    .get instance object A::get_P()
    .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) A::set_P(object)
  }
  .method public instance object get_P() { ldnull ret }
  .method public instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(object 'value') { ret }

  .method public newslot virtual  
      instance bool Equals (
          class A ''
      ) cil managed 
  {
      .maxstack 8

      IL_0000: ldnull
      IL_0001: throw
  } // end of method A::Equals

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"record B : A;
";
            var comp = CreateCompilation(sourceB, new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): error CS0115: 'B.EqualityContract': no suitable method found to override
                // record B : A;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B.EqualityContract").WithLocation(1, 8));

            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, GetProperties(comp, "B").ToTestDisplayStrings());
        }

        [Theory, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        [InlineData(false)]
        [InlineData(true)]
        public void CopyCtor(bool useCompilationReference)
        {
            var sourceA =
@"public record B(object N1, object N2)
{
}";
            var compA = CreateCompilation(sourceA);
            var verifierA = CompileAndVerify(compA, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();

            verifierA.VerifyIL("B..ctor(B)", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  ldfld      ""object B.<N1>k__BackingField""
  IL_000d:  stfld      ""object B.<N1>k__BackingField""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  ldfld      ""object B.<N2>k__BackingField""
  IL_0019:  stfld      ""object B.<N2>k__BackingField""
  IL_001e:  ret
}");

            var refA = useCompilationReference ? compA.ToMetadataReference() : compA.EmitToImageReference();

            var sourceB =
@"record C(object P1, object P2) : B(3, 4)
{
    static void Main()
    {
        var c1 = new C(1, 2);
        System.Console.Write((c1.P1, c1.P2, c1.N1, c1.N2));
        System.Console.Write("" "");

        var c2 = new C(c1);
        System.Console.Write((c2.P1, c2.P2, c2.N1, c2.N2));
        System.Console.Write("" "");

        var c3 = c1 with { P1 = 10, N1 = 30 };
        System.Console.Write((c3.P1, c3.P2, c3.N1, c3.N2));
    }
}";
            var compB = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var verifierB = CompileAndVerify(compB, expectedOutput: "(1, 2, 3, 4) (1, 2, 3, 4) (10, 2, 30, 4)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();
            // call base copy constructor B..ctor(B)
            verifierB.VerifyIL("C..ctor(C)", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<P1>k__BackingField""
  IL_000e:  stfld      ""object C.<P1>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<P2>k__BackingField""
  IL_001a:  stfld      ""object C.<P2>k__BackingField""
  IL_001f:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithOtherOverload()
        {
            var source =
@"public record B(object N1, object N2)
{
    public B(C c) : this(30, 40) => throw null;
}
public record C(object P1, object P2) : B(3, 4)
{
    static void Main()
    {
        var c1 = new C(1, 2);
        System.Console.Write((c1.P1, c1.P2, c1.N1, c1.N2));
        System.Console.Write("" "");

        var c2 = c1 with { P1 = 10, P2 = 20, N1 = 30, N2 = 40 };
        System.Console.Write((c2.P1, c2.P2, c2.N1, c2.N2));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2, 3, 4) (10, 20, 30, 40)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();
            // call base copy constructor B..ctor(B)
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<P1>k__BackingField""
  IL_000e:  stfld      ""object C.<P1>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<P2>k__BackingField""
  IL_001a:  stfld      ""object C.<P2>k__BackingField""
  IL_001f:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithObsoleteCopyConstructor()
        {
            var source =
@"public record B(object N1, object N2)
{
    [System.Obsolete(""Obsolete"", true)]
    public B(B b) { }
}
public record C(object P1, object P2) : B(3, 4) { }
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithParamsCopyConstructor()
        {
            var source =
@"public record B(object N1, object N2)
{
    public B(B b, params int[] i) : this(30, 40) { }
}
public record C(object P1, object P2) : B(3, 4) { }
";
            var comp = CreateCompilation(source);

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().Where(m => m.Name == ".ctor").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "B..ctor(System.Object N1, System.Object N2)",
                "B..ctor(B b, params System.Int32[] i)",
                "B..ctor(B original)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);

            var verifier = CompileAndVerify(comp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<P1>k__BackingField""
  IL_000e:  stfld      ""object C.<P1>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<P2>k__BackingField""
  IL_001a:  stfld      ""object C.<P2>k__BackingField""
  IL_001f:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithInitializers()
        {
            var source =
@"public record C(object N1, object N2)
{
    private int field = 42;
    public int Property = 43;
}";
            var comp = CreateCompilation(source);

            var verifier = CompileAndVerify(comp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics(
                // (3,17): warning CS0414: The field 'C.field' is assigned but its value is never used
                //     private int field = 42;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field").WithArguments("C.field").WithLocation(3, 17)
                );

            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  ldfld      ""object C.<N1>k__BackingField""
  IL_000d:  stfld      ""object C.<N1>k__BackingField""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  ldfld      ""object C.<N2>k__BackingField""
  IL_0019:  stfld      ""object C.<N2>k__BackingField""
  IL_001e:  ldarg.0
  IL_001f:  ldarg.1
  IL_0020:  ldfld      ""int C.field""
  IL_0025:  stfld      ""int C.field""
  IL_002a:  ldarg.0
  IL_002b:  ldarg.1
  IL_002c:  ldfld      ""int C.Property""
  IL_0031:  stfld      ""int C.Property""
  IL_0036:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_NotInRecordType()
        {
            var source =
@"public class C
{
    public object Property { get; set; }
    public int field = 42;

    public C(C c)
    {
    }
}
public class D : C
{
    public int field2 = 43;
    public D(D d) : base(d)
    {
    }
}
";
            var comp = CreateCompilation(source);

            var verifier = CompileAndVerify(comp).VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int C.field""
  IL_0008:  ldarg.0
  IL_0009:  call       ""object..ctor()""
  IL_000e:  ret
}");

            verifier.VerifyIL("D..ctor(D)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   43
  IL_0003:  stfld      ""int D.field2""
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  call       ""C..ctor(C)""
  IL_000f:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor()
        {
            var source =
@"public record B(object N1, object N2)
{
}
public record C(object P1, object P2) : B(0, 1)
{
    public C(C c) // 1, 2
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,12): error CS1729: 'B' does not contain a constructor that takes 0 arguments
                //     public C(C c) // 1, 2
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("B", "0").WithLocation(6, 12),
                // (6,12): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) // 1, 2
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "C").WithLocation(6, 12)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_DerivesFromObject()
        {
            var source =
@"public record C(int I)
{
    public int I { get; set; } = 42;
    public C(C c)
    {
    }
    public static void Main()
    {
        var c = new C(1);
        c.I = 2;
        var c2 = new C(c);
        System.Console.Write((c.I, c2.I));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "(2, 0)").VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_DerivesFromObject_WithFieldInitializer()
        {
            var source =
@"public record C(int I)
{
    public int I { get; set; } = 42;
    public int field = 43;
    public C(C c)
    {
        System.Console.Write("" RAN "");
    }
    public static void Main()
    {
        var c = new C(1);
        c.I = 2;
        c.field = 100;
        System.Console.Write((c.I, c.field));

        var c2 = new C(c);
        System.Console.Write((c2.I, c2.field));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "(2, 100) RAN (0, 0)").VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldstr      "" RAN ""
  IL_000d:  call       ""void System.Console.Write(string)""
  IL_0012:  nop
  IL_0013:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_DerivesFromObject_GivesParameterToBase()
        {
            var source = @"
public record C(object I)
{
    public C(C c) : base(1) { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,21): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                //     public C(C c) : base(1) { }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "base").WithArguments("object", "1").WithLocation(4, 21),
                // (4,21): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) : base(1) { }
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(4, 21)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_DerivesFromObject_WithSomeOtherConstructor()
        {
            var source = @"
public record C(object I)
{
    public C(int i) : this((object)null) { }
    public static void Main()
    {
        var c = new C((object)null);
        var c2 = new C(1);
        var c3 = new C(c);
        System.Console.Write(""RAN"");
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "RAN", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(int)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""C..ctor(object)""
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_DerivesFromObject_UsesThis()
        {
            var source =
@"public record C(int I)
{
    public C(C c) : this(c.I)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,21): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) : this(c.I)
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "this").WithLocation(3, 21)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefined_DerivesFromObject_UsesBase()
        {
            var source =
@"public record C(int I)
{
    public C(C c) : base()
    {
        System.Console.Write(""RAN "");
    }
    public static void Main()
    {
        var c = new C(1);
        System.Console.Write(c.I);
        System.Console.Write("" "");
        var c2 = c with { I = 2 };
        System.Console.Write(c2.I);
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1 RAN 2", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldstr      ""RAN ""
  IL_000d:  call       ""void System.Console.Write(string)""
  IL_0012:  nop
  IL_0013:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_NoPositionalMembers()
        {
            var source =
@"public record B(object N1, object N2)
{
}
public record C(object P1) : B(0, 1)
{
    public C(C c) // 1, 2
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,12): error CS1729: 'B' does not contain a constructor that takes 0 arguments
                //     public C(C c) // 1, 2
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("B", "0").WithLocation(6, 12),
                // (6,12): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) // 1, 2
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "C").WithLocation(6, 12)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_UsesThis()
        {
            var source =
@"public record B(object N1, object N2)
{
}
public record C(object P1, object P2) : B(0, 1)
{
    public C(C c) : this(1, 2) // 1
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,21): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) : this(1, 2) // 1
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "this").WithLocation(6, 21)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_UsesBase()
        {
            var source =
@"public record B(int i)
{
}
public record C(int j) : B(0)
{
    public C(C c) : base(1) // 1
    {
    }
}
#nullable enable
public record D(int j) : B(0)
{
    public D(D? d) : base(1) // 2
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,21): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) : base(1) // 1
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(6, 21),
                // (13,22): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public D(D? d) : base(1) // 2
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(13, 22)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefined_WithFieldInitializers()
        {
            var source =
@"public record C(int I)
{
}
public record D(int J) : C(1)
{
    public int field = 42;
    public D(D d) : base(d)
    {
        System.Console.Write(""RAN "");
    }
    public static void Main()
    {
        var d = new D(2);
        System.Console.Write((d.I, d.J, d.field));
        System.Console.Write("" "");

        var d2 = d with { I = 10, J = 20 };
        System.Console.Write((d2.I, d2.J, d.field));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2, 42) RAN (10, 20, 42)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();
            verifier.VerifyIL("D..ctor(D)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""C..ctor(C)""
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  ldstr      ""RAN ""
  IL_000e:  call       ""void System.Console.Write(string)""
  IL_0013:  nop
  IL_0014:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_Synthesized_WithFieldInitializers()
        {
            var source =
@"public record C(int I)
{
}
public record D(int J) : C(1)
{
    public int field = 42;
    public static void Main()
    {
        var d = new D(2);
        System.Console.Write((d.I, d.J, d.field));
        System.Console.Write("" "");

        var d2 = d with { I = 10, J = 20 };
        System.Console.Write((d2.I, d2.J, d.field));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2, 42) (10, 20, 42)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();
            verifier.VerifyIL("D..ctor(D)", @"
 {
      // Code size       33 (0x21)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  call       ""C..ctor(C)""
      IL_0007:  nop
      IL_0008:  ldarg.0
      IL_0009:  ldarg.1
      IL_000a:  ldfld      ""int D.<J>k__BackingField""
      IL_000f:  stfld      ""int D.<J>k__BackingField""
      IL_0014:  ldarg.0
      IL_0015:  ldarg.1
      IL_0016:  ldfld      ""int D.field""
      IL_001b:  stfld      ""int D.field""
      IL_0020:  ret
    }
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButPrivate()
        {
            var source =
@"public record B(object N1, object N2)
{
    private B(B b) { }
}
public record C(object P1, object P2) : B(0, 1)
{
    private C(C c) : base(2, 3) { } // 1
}
public record D(object P1, object P2) : B(0, 1)
{
    private D(D d) : base(d) { } // 2
}
public record E(object P1, object P2) : B(0, 1); // 3
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,13): error CS8878: A copy constructor 'B.B(B)' must be public or protected because the record is not sealed.
                //     private B(B b) { }
                Diagnostic(ErrorCode.ERR_CopyConstructorWrongAccessibility, "B").WithArguments("B.B(B)").WithLocation(3, 13),
                // (7,13): error CS8878: A copy constructor 'C.C(C)' must be public or protected because the record is not sealed.
                //     private C(C c) : base(2, 3) { } // 1
                Diagnostic(ErrorCode.ERR_CopyConstructorWrongAccessibility, "C").WithArguments("C.C(C)").WithLocation(7, 13),
                // (7,22): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     private C(C c) : base(2, 3) { } // 1
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(7, 22),
                // (11,13): error CS8878: A copy constructor 'D.D(D)' must be public or protected because the record is not sealed.
                //     private D(D d) : base(d) { } // 2
                Diagnostic(ErrorCode.ERR_CopyConstructorWrongAccessibility, "D").WithArguments("D.D(D)").WithLocation(11, 13),
                // (11,22): error CS0122: 'B.B(B)' is inaccessible due to its protection level
                //     private D(D d) : base(d) { } // 2
                Diagnostic(ErrorCode.ERR_BadAccess, "base").WithArguments("B.B(B)").WithLocation(11, 22),
                // (13,15): error CS8867: No accessible copy constructor found in base type 'B'.
                // public record E(object P1, object P2) : B(0, 1); // 3
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "E").WithArguments("B").WithLocation(13, 15)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_InaccessibleToCaller()
        {
            var sourceA =
@"public record B(object N1, object N2)
{
    internal B(B b) { }
}";
            var compA = CreateCompilation(sourceA);
            compA.VerifyDiagnostics(
                // (3,14): error CS8878: A copy constructor 'B.B(B)' must be public or protected because the record is not sealed.
                //     internal B(B b) { }
                Diagnostic(ErrorCode.ERR_CopyConstructorWrongAccessibility, "B").WithArguments("B.B(B)").WithLocation(3, 14)
                );

            var refA = compA.ToMetadataReference();

            var sourceB = @"
record C(object P1, object P2) : B(3, 4); // 1
";
            var compB = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            compB.VerifyDiagnostics(
                // (2,8): error CS8867: No accessible copy constructor found in base type 'B'.
                // record C(object P1, object P2) : B(3, 4); // 1
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "C").WithArguments("B").WithLocation(2, 8)
                );

            var sourceC = @"
record C(object P1, object P2) : B(3, 4)
{
    protected C(C c) : base(c) { } // 1, 2
}
";
            var compC = CreateCompilation(sourceC, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            compC.VerifyDiagnostics(
                // (4,24): error CS0122: 'B.B(B)' is inaccessible due to its protection level
                //     protected C(C c) : base(c) { } // 1, 2
                Diagnostic(ErrorCode.ERR_BadAccess, "base").WithArguments("B.B(B)").WithLocation(4, 24)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_InaccessibleToCallerFromPE_WithIVT()
        {
            var sourceA = @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""AssemblyB"")]

internal record B(object N1, object N2)
{
    public B(B b) { }
}";
            var compA = CreateCompilation(new[] { sourceA, IsExternalInitTypeDefinition }, assemblyName: "AssemblyA", parseOptions: TestOptions.Regular9);
            var refA = compA.EmitToImageReference();

            var sourceB = @"
record C(int j) : B(3, 4);
";
            var compB = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9, assemblyName: "AssemblyB");
            compB.VerifyDiagnostics();

            var sourceC = @"
record C(int j) : B(3, 4)
{
    protected C(C c) : base(c) { }
}
";
            var compC = CreateCompilation(sourceC, references: new[] { refA }, parseOptions: TestOptions.Regular9, assemblyName: "AssemblyB");
            compC.VerifyDiagnostics();

            var compB2 = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9, assemblyName: "AssemblyB2");
            compB2.VerifyEmitDiagnostics(
                // (2,8): error CS0115: 'C.ToString()': no suitable method found to override
                // record C(int j) : B(3, 4);
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C").WithArguments("C.ToString()").WithLocation(2, 8),
                // (2,8): error CS0115: 'C.GetHashCode()': no suitable method found to override
                // record C(int j) : B(3, 4);
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C").WithArguments("C.GetHashCode()").WithLocation(2, 8),
                // (2,8): error CS0115: 'C.EqualityContract': no suitable method found to override
                // record C(int j) : B(3, 4);
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C").WithArguments("C.EqualityContract").WithLocation(2, 8),
                // (2,8): error CS0115: 'C.Equals(object?)': no suitable method found to override
                // record C(int j) : B(3, 4);
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C").WithArguments("C.Equals(object?)").WithLocation(2, 8),
                // (2,19): error CS0122: 'B' is inaccessible due to its protection level
                // record C(int j) : B(3, 4);
                Diagnostic(ErrorCode.ERR_BadAccess, "B").WithArguments("B").WithLocation(2, 19),
                // (2,20): error CS0122: 'B.B(object, object)' is inaccessible due to its protection level
                // record C(int j) : B(3, 4);
                Diagnostic(ErrorCode.ERR_BadAccess, "(3, 4)").WithArguments("B.B(object, object)").WithLocation(2, 20)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        [WorkItem(45012, "https://github.com/dotnet/roslyn/issues/45012")]
        public void CopyCtor_UserDefinedButPrivate_InSealedType()
        {
            var source =
@"public record B(int i)
{
}
public sealed record C(int j) : B(0)
{
    private C(C c) : base(c)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var copyCtor = comp.GetMembers("C..ctor")[1];
            Assert.Equal("C..ctor(C c)", copyCtor.ToTestDisplayString());
            Assert.True(copyCtor.DeclaredAccessibility == Accessibility.Private);
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        [WorkItem(45012, "https://github.com/dotnet/roslyn/issues/45012")]
        public void CopyCtor_UserDefinedButInternal()
        {
            var source =
@"public record B(object N1, object N2)
{
}
public sealed record Sealed(object P1, object P2) : B(0, 1)
{
    internal Sealed(Sealed s) : base(s)
    {
    }
}
public record Unsealed(object P1, object P2) : B(0, 1)
{
    internal Unsealed(Unsealed s) : base(s)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,14): error CS8878: A copy constructor 'Unsealed.Unsealed(Unsealed)' must be public or protected because the record is not sealed.
                //     internal Unsealed(Unsealed s) : base(s)
                Diagnostic(ErrorCode.ERR_CopyConstructorWrongAccessibility, "Unsealed").WithArguments("Unsealed.Unsealed(Unsealed)").WithLocation(12, 14)
                );

            var sealedCopyCtor = comp.GetMembers("Sealed..ctor")[1];
            Assert.Equal("Sealed..ctor(Sealed s)", sealedCopyCtor.ToTestDisplayString());
            Assert.True(sealedCopyCtor.DeclaredAccessibility == Accessibility.Internal);

            var unsealedCopyCtor = comp.GetMembers("Unsealed..ctor")[1];
            Assert.Equal("Unsealed..ctor(Unsealed s)", unsealedCopyCtor.ToTestDisplayString());
            Assert.True(unsealedCopyCtor.DeclaredAccessibility == Accessibility.Internal);
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_BaseHasRefKind()
        {
            var source =
@"public record B(int i)
{
    public B(ref B b) => throw null; // 1, not recognized as copy constructor
}
public record C(int j) : B(1)
{
    protected C(C c) : base(c)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,12): error CS8862: A constructor declared in a record with parameter list must have 'this' constructor initializer.
                //     public B(ref B b) => throw null; // 1, not recognized as copy constructor
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "B").WithLocation(3, 12)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_BaseHasRefKind_WithThisInitializer()
        {
            var source =
@"public record B(int i)
{
    public B(ref B b) : this(0) => throw null; // 1, not recognized as copy constructor
}
public record C(int j) : B(1)
{
    protected C(C c) : base(c)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().Where(m => m.Name == ".ctor").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "B..ctor(System.Int32 i)",
                "B..ctor(ref B b)",
                "B..ctor(B original)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithPrivateField()
        {
            var source =
@"public record B(object N1, object N2)
{
    private int field1 = 100;
    public int GetField1() => field1;
}
public record C(object P1, object P2) : B(3, 4)
{
    private int field2 = 200;
    public int GetField2() => field2;

    static void Main()
    {
        var c1 = new C(1, 2);
        var c2 = new C(c1);
        System.Console.Write((c2.P1, c2.P2, c2.N1, c2.N2, c2.GetField1(), c2.GetField2()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2, 3, 4, 100, 200)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails).VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<P1>k__BackingField""
  IL_000e:  stfld      ""object C.<P1>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<P2>k__BackingField""
  IL_001a:  stfld      ""object C.<P2>k__BackingField""
  IL_001f:  ldarg.0
  IL_0020:  ldarg.1
  IL_0021:  ldfld      ""int C.field2""
  IL_0026:  stfld      ""int C.field2""
  IL_002b:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_MissingInMetadata()
        {
            // IL for `public record B { }`
            var ilSource = @"
.class public auto ansi beforefieldinit B extends [mscorlib]System.Object
{
    .method public hidebysig specialname newslot virtual instance class B '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class B '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    // Removed copy constructor
    //.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record C : B {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (2,15): error CS8867: No accessible copy constructor found in base type 'B'.
                // public record C : B {
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "C").WithArguments("B").WithLocation(2, 15)
                );

            var source2 = @"
public record C : B
{
    public C(C c) { }
}";
            var comp2 = CreateCompilationWithIL(new[] { source2, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp2.VerifyDiagnostics(
                // (4,12): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) { }
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "C").WithLocation(4, 12)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_InaccessibleInMetadata()
        {
            // IL for `public record B { }`
            var ilSource = @"
.class public auto ansi beforefieldinit B extends [mscorlib]System.Object
{
    .method public hidebysig specialname newslot virtual instance class B '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class B '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    // Inaccessible copy constructor
    .method private hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";
            var source = @"
public record C : B {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (2,15): error CS8867: No accessible copy constructor found in base type 'B'.
                // public record C : B {
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "C").WithArguments("B").WithLocation(2, 15)
                );
        }

        [Fact, WorkItem(45077, "https://github.com/dotnet/roslyn/issues/45077")]
        public void CopyCtor_AmbiguitiesInMetadata()
        {
            // IL for a minimal `public record B { }` with injected copy constructors
            var ilSource_template = @"
.class public auto ansi beforefieldinit B extends [mscorlib]System.Object
{
    INJECT

    .method public hidebysig specialname newslot virtual instance class B '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: newobj instance void B::.ctor(class B)
        IL_0006: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .method public newslot virtual 
        instance bool Equals (
            class B ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    }

    .method family virtual instance class [mscorlib]System.Type get_EqualityContract() { ldnull ret }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";

            var source = @"
public record C : B
{
    public static void Main()
    {
        var c = new C();
        _ = c with { };
    }
}";

            // We're going to inject various copy constructors into record B (at INJECT marker), and check which one is used
            // by derived record C
            // The RAN and THROW markers are shorthands for method bodies that print "RAN" and throw, respectively.

            // .ctor(B) vs. .ctor(modopt B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW
");

            // .ctor(modopt B) alone
            verify(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
RAN
");

            // .ctor(B) vs. .ctor(modreq B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modreq(int64) '' ) cil managed
THROW
");

            // .ctor(modopt B) vs. .ctor(modreq B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modreq(int64) '' ) cil managed
THROW
");

            // .ctor(B) vs. .ctor(modopt1 B) and .ctor(modopt2 B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW

.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int32) '' ) cil managed
THROW
");
            // .ctor(B) vs. .ctor(modopt1 B) and .ctor(modreq B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW

.method public hidebysig specialname rtspecialname instance void .ctor ( class B modreq(int32) '' ) cil managed
THROW
");

            // .ctor(modeopt1 B) vs. .ctor(modopt2 B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int32) '' ) cil managed
THROW
", isError: true);

            // private .ctor(B) vs. .ctor(modopt1 B) and .ctor(modopt B)
            verifyBoth(@"
.method private hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW

.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int32) '' ) cil managed
THROW
", isError: true);

            void verifyBoth(string inject1, string inject2, bool isError = false)
            {
                verify(inject1 + inject2, isError);
                verify(inject2 + inject1, isError);
            }

            void verify(string inject, bool isError = false)
            {
                var ranBody = @"
{
    IL_0000:  ldstr      ""RAN""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
}
";

                var throwBody = @"
{
    IL_0000: ldnull
    IL_0001: throw
}
";

                var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition },
                    ilSource: ilSource_template.Replace("INJECT", inject).Replace("RAN", ranBody).Replace("THROW", throwBody),
                    parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);

                var expectedDiagnostics = isError ? new[] {
                    // (2,15): error CS8867: No accessible copy constructor found in base type 'B'.
                    // public record C : B
                    Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "C").WithArguments("B").WithLocation(2, 15)
                } : new DiagnosticDescription[] { };

                comp.VerifyDiagnostics(expectedDiagnostics);
                if (expectedDiagnostics is null)
                {
                    CompileAndVerify(comp, expectedOutput: "RAN").VerifyDiagnostics();
                }
            }
        }

        [Fact, WorkItem(45077, "https://github.com/dotnet/roslyn/issues/45077")]
        public void CopyCtor_AmbiguitiesInMetadata_GenericType()
        {
            // IL for a minimal `public record B<T> { }` with modopt in nested position of parameter type
            var ilSource = @"
.class public auto ansi beforefieldinit B`1<T> extends [mscorlib]System.Object implements class [mscorlib]System.IEquatable`1<class B`1<!T>>
{
    .method family hidebysig specialname rtspecialname instance void .ctor ( class B`1<!T modopt(int64)> '' ) cil managed
    {
        IL_0000:  ldstr      ""RAN""
        IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_000a:  ret
    }

    .method public hidebysig specialname newslot virtual instance class B`1<!T> '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: newobj instance void class B`1<!T>::.ctor(class B`1<!0>)
        IL_0006: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .method public newslot virtual instance bool Equals ( class B`1<!T> '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family virtual instance class [mscorlib]System.Type get_EqualityContract() { ldnull ret }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B`1::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";

            var source = @"
public record C<T> : B<T> { }

public class Program
{
    public static void Main()
    {
        var c = new C<string>();
        _ = c with { };
    }
}";


            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition },
                ilSource: ilSource,
                parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: "RAN").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        [InlineData("internal")]
        [InlineData("private protected")]
        [InlineData("internal protected")]
        public void CopyCtor_Accessibility_01(string accessibility)
        {
            var source =
$@"
record A(int X)
{{
    { accessibility } A(A a)
        => throw null;
}}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,6): error CS8878: A copy constructor 'A.A(A)' must be public or protected because the record is not sealed.
                //      A(A a)
                Diagnostic(ErrorCode.ERR_CopyConstructorWrongAccessibility, "A").WithArguments("A.A(A)").WithLocation(4, 6 + accessibility.Length)
                );
        }

        [Theory]
        [InlineData("public")]
        [InlineData("protected")]
        public void CopyCtor_Accessibility_02(string accessibility)
        {
            var source =
$@"
record A(int X)
{{
    { accessibility } A(A a)
        => System.Console.Write(""RAN"");

    public static void Main()
    {{
        var a = new A(123);
        _ = a with {{}};
    }}

}}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: "RAN").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        [InlineData("internal")]
        [InlineData("public")]
        public void CopyCtor_Accessibility_03(string accessibility)
        {
            var source =
$@"
sealed record A(int X)
{{
    { accessibility } A(A a)
        => System.Console.Write(""RAN"");

    public static void Main()
    {{
        var a = new A(123);
        _ = a with {{}};
    }}

}}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: "RAN").VerifyDiagnostics();

            var clone = comp.GetMember<MethodSymbol>("A." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.False(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.False(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);
        }

        [Theory]
        [InlineData("private protected")]
        [InlineData("internal protected")]
        [InlineData("protected")]
        public void CopyCtor_Accessibility_04(string accessibility)
        {
            var source =
$@"
sealed record A(int X)
{{
    { accessibility } A(A a)
        => System.Console.Write(""RAN"");

    public static void Main()
    {{
        var a = new A(123);
        _ = a with {{}};
    }}

}}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: "RAN").VerifyDiagnostics(
                // (4,15): warning CS0628: 'A.A(A)': new protected member declared in sealed type
                //     protected A(A a)
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "A").WithArguments("A.A(A)").WithLocation(4, 6 + accessibility.Length)
                );

            var clone = comp.GetMember<MethodSymbol>("A." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.False(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.False(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);
        }

        [Fact]
        public void CopyCtor_Signature_01()
        {
            var source =
@"
record A(int X)
{
    public A(in A a) : this(-15)
        => System.Console.Write(""RAN"");

    public static void Main()
    {
        var a = new A(123);
        System.Console.Write((a with { }).X);
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: "123").VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_Simple()
        {
            var source =
@"using System;

record B(int X, int Y)
{
    public static void Main()
    {
        M(new B(1, 2));
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("B.Deconstruct", @"
{
    // Code size       17 (0x11)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  ldarg.0
    IL_0002:  call       ""int B.X.get""
    IL_0007:  stind.i4
    IL_0008:  ldarg.2
    IL_0009:  ldarg.0
    IL_000a:  call       ""int B.Y.get""
    IL_000f:  stind.i4
    IL_0010:  ret
}");

            var deconstruct = ((CSharpCompilation)verifier.Compilation).GetMember<MethodSymbol>("B.Deconstruct");
            Assert.Equal(2, deconstruct.ParameterCount);

            Assert.Equal(RefKind.Out, deconstruct.Parameters[0].RefKind);
            Assert.Equal("X", deconstruct.Parameters[0].Name);

            Assert.Equal(RefKind.Out, deconstruct.Parameters[1].RefKind);
            Assert.Equal("Y", deconstruct.Parameters[1].Name);

            Assert.True(deconstruct.ReturnsVoid);
            Assert.False(deconstruct.IsVirtual);
            Assert.False(deconstruct.IsStatic);
            Assert.Equal(Accessibility.Public, deconstruct.DeclaredAccessibility);
        }

        [Fact]
        public void Deconstruct_PositionalAndNominalProperty()
        {
            var source =
@"using System;

record B(int X)
{
    public int Y { get; init; }

    public static void Main()
    {
        M(new B(1));
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X)",
                verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Nested()
        {
            var source =
@"using System;

record B(int X, int Y);

record C(B B, int Z)
{
    public static void Main()
    {
        M(new C(new B(1, 2), 3));
    }

    static void M(C c)
    {
        switch (c)
        {
            case C(B(int x, int y), int z):
                Console.Write(x);
                Console.Write(y);
                Console.Write(z);
                break;
        }
    }
}
";

            var verifier = CompileAndVerify(source, expectedOutput: "123");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("B.Deconstruct", @"
{
    // Code size       17 (0x11)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  ldarg.0
    IL_0002:  call       ""int B.X.get""
    IL_0007:  stind.i4
    IL_0008:  ldarg.2
    IL_0009:  ldarg.0
    IL_000a:  call       ""int B.Y.get""
    IL_000f:  stind.i4
    IL_0010:  ret
}");

            verifier.VerifyIL("C.Deconstruct", @"
{
    // Code size       17 (0x11)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  ldarg.0
    IL_0002:  call       ""B C.B.get""
    IL_0007:  stind.ref
    IL_0008:  ldarg.2
    IL_0009:  ldarg.0
    IL_000a:  call       ""int C.Z.get""
    IL_000f:  stind.i4
    IL_0010:  ret
}");
        }

        [Fact]
        public void Deconstruct_PropertyCollision()
        {
            var source =
@"using System;

record B(int X, int Y)
{
    public int X => 3;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "32");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_01()
        {
            var source = @"
record B(int X, int Y)
{
    public int X() => 3;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                break;
        }
    }

    static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,16): error CS0102: The type 'B' already contains a definition for 'X'
                //     public int X() => 3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("B", "X").WithLocation(4, 16)
                );

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_02()
        {
            var source = @"
record B
{
    public int X(int y) => y;
}

record C(int X, int Y) : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                break;
        }
    }

    static void Main()
    {
        M(new C(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,14): error CS8866: Record member 'B.X' must be a readable instance property of type 'int' to match positional parameter 'X'.
                // record C(int X, int Y) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("B.X", "int", "X").WithLocation(7, 14)
            );

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_03()
        {
            var source = @"
using System;

record B
{
    public int X() => 3;
}

record C(int X, int Y) : B
{
    public new int X { get; }

    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "02");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                verifier.Compilation.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_04()
        {
            var source = @"
record C(int X, int Y)
{
    public int X(int arg) => 3;

    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                break;
        }
    }

    static void Main()
    {
        M(new C(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,16): error CS0102: The type 'C' already contains a definition for 'X'
                //     public int X(int arg) => 3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(4, 16)
                );

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_FieldCollision()
        {
            var source = @"
using System;

record C(int X)
{
    int X;

    static void M(C c)
    {
        switch (c)
        {
            case C(int x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(0));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0102: The type 'C' already contains a definition for 'X'
                //     int X;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(6, 9),
                // (6,9): warning CS0169: The field 'C.X' is never used
                //     int X;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "X").WithArguments("C.X").WithLocation(6, 9));

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_EventCollision()
        {
            var source = @"
using System;

record C(Action X)
{
    event Action X;

    static void M(C c)
    {
        switch (c)
        {
            case C(Action x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(() => { }));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,18): error CS0102: The type 'C' already contains a definition for 'X'
                //     event Action X;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(6, 18),
                // (6,18): warning CS0067: The event 'C.X' is never used
                //     event Action X;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "X").WithArguments("C.X").WithLocation(6, 18)
                );

            Assert.Equal(
                "void C.Deconstruct(out System.Action X)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_WriteOnlyPropertyInBase()
        {
            var source = @"
using System;

record B
{
    public int X { set { } }
}

record C(int X) : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,14): error CS8866: Record member 'B.X' must be a readable instance property of type 'int' to match positional parameter 'X'.
                // record C(int X) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("B.X", "int", "X").WithLocation(9, 14));

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_PrivateWriteOnlyPropertyInBase()
        {
            var source = @"
using System;

record B
{
    private int X { set { } }
}

record C(int X) : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X)",
                verifier.Compilation.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Empty()
        {
            var source = @"
record C
{
    static void M(C c)
    {
        switch (c)
        {
            case C():
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //             case C():
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "()").WithArguments("C", "Deconstruct").WithLocation(8, 19),
                // (8,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 0 out parameters and a void return type.
                //             case C():
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("C", "0").WithLocation(8, 19));

            Assert.Null(comp.GetMember("C.Deconstruct"));
        }

        [Fact]
        public void Deconstruct_Inheritance_01()
        {
            var source = @"
using System;

record B(int X, int Y)
{
    internal B() : this(0, 1) { }
}

record C : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
        switch (c)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "0101");
            verifier.VerifyDiagnostics();

            var comp = verifier.Compilation;
            Assert.Null(comp.GetMember("C.Deconstruct"));
            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Inheritance_02()
        {
            var source = @"
using System;

record B(int X, int Y)
{
    // https://github.com/dotnet/roslyn/issues/44902
    internal B() : this(0, 1) { }
}

record C(int X, int Y, int Z) : B(X, Y)
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y, int z):
                Console.Write(x);
                Console.Write(y);
                Console.Write(z);
                break;
        }
        switch (c)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(0, 1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "01201");
            verifier.VerifyDiagnostics();

            var comp = verifier.Compilation;
            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y, out System.Int32 Z)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Inheritance_03()
        {
            var source = @"
using System;

record B(int X, int Y)
{
    internal B() : this(0, 1) { }
}

record C(int X, int Y) : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
        switch (c)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(0, 1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "0101");
            verifier.VerifyDiagnostics();

            var comp = verifier.Compilation;
            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Inheritance_04()
        {
            var source = @"
using System;

record A<T>(T P) { internal A() : this(default(T)) { } }
record B1(int P, object Q) : A<int>(P) { internal B1() : this(0, null) { } }
record B2(object P, object Q) : A<object>(P) { internal B2() : this(null, null) { } }
record B3<T>(T P, object Q) : A<T>(P) { internal B3() : this(default, 0) { } }

class C
{
    static void M0(A<int> arg)
    {
        switch (arg)
        {
            case A<int>(int x):
                Console.Write(x);
                break;
        }
    }

    static void M1(B1 arg)
    {
        switch (arg)
        {
            case B1(int p, object q):
                Console.Write(p);
                Console.Write(q);
                break;
        }
    }

    static void M2(B2 arg)
    {
        switch (arg)
        {
            case B2(object p, object q):
                Console.Write(p);
                Console.Write(q);
                break;
        }
    }

    static void M3(B3<int> arg)
    {
        switch (arg)
        {
            case B3<int>(int p, object q):
                Console.Write(p);
                Console.Write(q);
                break;
        }
    }

    static void Main()
    {
        M0(new A<int>(0));
        M1(new B1(1, 2));
        M2(new B2(3, 4));
        M3(new B3<int>(5, 6));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "0123456");
            verifier.VerifyDiagnostics();

            var comp = verifier.Compilation;
            Assert.Equal(
                "void A<T>.Deconstruct(out T P)",
                comp.GetMember("A.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B1.Deconstruct(out System.Int32 P, out System.Object Q)",
                comp.GetMember("B1.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B2.Deconstruct(out System.Object P, out System.Object Q)",
                comp.GetMember("B2.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B3<T>.Deconstruct(out T P, out System.Object Q)",
                comp.GetMember("B3.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Conversion_01()
        {
            var source = @"
using System;

record C(int X, int Y)
{
    public long X { get; init; }
    public long Y { get; init; }

    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(0, 1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,14): error CS8866: Record member 'C.X' must be a readable instance property of type 'int' to match positional parameter 'X'.
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("C.X", "int", "X").WithLocation(4, 14),
                // (4,21): error CS8866: Record member 'C.Y' must be a readable instance property of type 'int' to match positional parameter 'Y'.
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("C.Y", "int", "Y").WithLocation(4, 21));

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Conversion_02()
        {
            var source = @"
#nullable enable
using System;

record C(string? X, string Y)
{
    public string X { get; init; } = null!;
    public string? Y { get; init; }

    static void M(C c)
    {
        switch (c)
        {
            case C(var x, string y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(""a"", ""b""));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            Assert.Equal(
                "void C.Deconstruct(out System.String? X, out System.String Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Conversion_03()
        {
            var source = @"
using System;

class Base { }
class Derived : Base { }

record C(Derived X, Base Y)
{
    public Base X { get; init; }
    public Derived Y { get; init; }

    static void M(C c)
    {
        switch (c)
        {
            case C(Derived x, Base y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(new Derived(), new Base()));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,18): error CS8866: Record member 'C.X' must be a readable instance property of type 'Derived' to match positional parameter 'X'.
                // record C(Derived X, Base Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("C.X", "Derived", "X").WithLocation(7, 18),
                // (7,26): error CS8866: Record member 'C.Y' must be a readable instance property of type 'Base' to match positional parameter 'Y'.
                // record C(Derived X, Base Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("C.Y", "Base", "Y").WithLocation(7, 26));

            Assert.Equal(
                "void C.Deconstruct(out Derived X, out Base Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList()
        {
            var source = @"
record C()
{
    static void M(C c)
    {
        switch (c)
        {
            case C():
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //             case C():
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "()").WithArguments("C", "Deconstruct").WithLocation(8, 19),
                // (8,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 0 out parameters and a void return type.
                //             case C():
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("C", "0").WithLocation(8, 19));

            Assert.Null(comp.GetMember("C.Deconstruct"));
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList_UserDefined_01()
        {
            var source =
@"using System;

record C()
{
    public void Deconstruct()
    {
    }

    static void M(C c)
    {
        switch (c)
        {
            case C():
                Console.Write(12);
                break;
        }
    }

    public static void Main()
    {
        M(new C());
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList_UserDefined_02()
        {
            var source =
@"using System;

record C()
{
    public void Deconstruct(out int X, out int Y)
    {
        X = 1;
        Y = 2;
    }

    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new C());
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList_UserDefined_03()
        {
            var source =
@"using System;

record C()
{
    private void Deconstruct()
    {
    }

    static void M(C c)
    {
        switch (c)
        {
            case C():
                Console.Write(12);
                break;
        }
    }

    public static void Main()
    {
        M(new C());
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList_UserDefined_04()
        {
            var source = @"
record C()
{
    static void M(C c)
    {
        switch (c)
        {
            case C():
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }

    public static void Deconstruct()
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): error CS0176: Member 'C.Deconstruct()' cannot be accessed with an instance reference; qualify it with a type name instead
                //             case C():
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "()", isSuppressed: false).WithArguments("C.Deconstruct()").WithLocation(8, 19),
                // (8,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 0 out parameters and a void return type.
                //             case C():
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("C", "0").WithLocation(8, 19));
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList_UserDefined_05()
        {
            var source = @"
record C()
{
    static void M(C c)
    {
        switch (c)
        {
            case C():
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }

    public int Deconstruct()
    {
        return 1;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 0 out parameters and a void return type.
                //             case C():
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("C", "0").WithLocation(8, 19));
        }

        [Fact]
        public void Deconstruct_UserDefined()
        {
            var source =
@"using System;

record B(int X, int Y)
{
    public void Deconstruct(out int X, out int Y)
    {
        X = this.X + 1;
        Y = this.Y + 2;
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new B(0, 0));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_01()
        {
            var source =
@"using System;

record B(int X, int Y)
{
    public void Deconstruct(out int Z)
    {
        Z = X + Y;
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
        switch (b)
        {
            case B(int z):
                Console.Write(z);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "123");
            verifier.VerifyDiagnostics();

            var expectedSymbols = new[]
            {
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                "void B.Deconstruct(out System.Int32 Z)",
            };
            Assert.Equal(expectedSymbols, verifier.Compilation.GetMembers("B.Deconstruct").Select(s => s.ToTestDisplayString(includeNonNullable: false)));
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_02()
        {
            var source =
@"using System;

record B(int X)
{
    public int Deconstruct(out int a) => throw null;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,16): error CS8874: Record member 'B.Deconstruct(out int)' must return 'void'.
                //     public int Deconstruct(out int a) => throw null;
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "Deconstruct").WithArguments("B.Deconstruct(out int)", "void").WithLocation(5, 16),
                // (11,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'B', with 1 out parameters and a void return type.
                //             case B(int x):
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(int x)").WithArguments("B", "1").WithLocation(11, 19));

            Assert.Equal("System.Int32 B.Deconstruct(out System.Int32 a)", comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_03()
        {
            var source =
@"using System;

record B(int X)
{
    public void Deconstruct(int X)
    {
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            var expectedSymbols = new[]
            {
                "void B.Deconstruct(out System.Int32 X)",
                "void B.Deconstruct(System.Int32 X)",
            };
            Assert.Equal(expectedSymbols, verifier.Compilation.GetMembers("B.Deconstruct").Select(s => s.ToTestDisplayString(includeNonNullable: false)));
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_04()
        {
            var source =
@"using System;

record B(int X)
{
    public void Deconstruct(ref int X)
    {
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,17): error CS0663: 'B' cannot define an overloaded method that differs only on parameter modifiers 'ref' and 'out'
                //     public void Deconstruct(ref int X)
                Diagnostic(ErrorCode.ERR_OverloadRefKind, "Deconstruct").WithArguments("B", "method", "ref", "out").WithLocation(5, 17)
                );

            Assert.Equal(2, comp.GetMembers("B.Deconstruct").Length);
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_05()
        {
            var source =
@"using System;

record A(int X)
{
    public A() : this(0) { }
    public int Deconstruct(out int a, out int b) => throw null;
}
record B(int X, int Y) : A(X)
{
    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();

            Assert.Equal("void B.Deconstruct(out System.Int32 X, out System.Int32 Y)", verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_06()
        {
            var source =
@"using System;

record A(int X)
{
    public A() : this(0) { }
    public virtual int Deconstruct(out int a, out int b) => throw null;
}
record B(int X, int Y) : A(X)
{
    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();

            Assert.Equal("void B.Deconstruct(out System.Int32 X, out System.Int32 Y)", verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("private protected")]
        [InlineData("internal protected")]
        public void Deconstruct_UserDefined_Accessibility_07(string accessibility)
        {
            var source =
$@"
record A(int X)
{{
    { accessibility } void Deconstruct(out int a)
        => throw null;
}}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,11): error CS8873: Record member 'A.Deconstruct(out int)' must be public.
                //      void Deconstruct(out int a)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Deconstruct").WithArguments("A.Deconstruct(out int)").WithLocation(4, 11 + accessibility.Length)
                );
        }

        [Fact]
        public void Deconstruct_UserDefined_Static_08()
        {
            var source =
@"
record A(int X)
{
    public static void Deconstruct(out int a)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS8877: Record member 'A.Deconstruct(out int)' may not be static.
                //     public static void Deconstruct(out int a)
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "Deconstruct").WithArguments("A.Deconstruct(out int)").WithLocation(4, 24)
                );
        }

        [Fact]
        public void Deconstruct_Shadowing_01()
        {
            var source =
@"
abstract record A(int X)
{
    public abstract int Deconstruct(out int a, out int b);
}
abstract record B(int X, int Y) : A(X)
{
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0533: 'B.Deconstruct(out int, out int)' hides inherited abstract member 'A.Deconstruct(out int, out int)'
                // abstract record B(int X, int Y) : A(X)
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "B").WithArguments("B.Deconstruct(out int, out int)", "A.Deconstruct(out int, out int)").WithLocation(6, 17)
                );
        }

        [Fact]
        public void Deconstruct_TypeMismatch_01()
        {
            var source =
@"
record A(int X)
{
    public System.Type X => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,14): error CS8866: Record member 'A.X' must be a readable instance property of type 'int' to match positional parameter 'X'.
                // record A(int X)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("A.X", "int", "X").WithLocation(2, 14)
                );
        }

        [Fact]
        public void Deconstruct_TypeMismatch_02()
        {
            var source =
@"
record A
{
    public System.Type X => throw null;
}

record B(int X) : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,14): error CS8866: Record member 'A.X' must be a readable instance property of type 'int' to match positional parameter 'X'.
                // record B(int X) : A;
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("A.X", "int", "X").WithLocation(7, 14)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/45010")]
        [WorkItem(45010, "https://github.com/dotnet/roslyn/issues/45010")]
        public void Deconstruct_ObsoleteProperty()
        {
            var source =
@"using System;

record B(int X)
{
    [Obsolete] int X { get; } = X;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            Assert.Equal("void B.Deconstruct(out System.Int32 X)", verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/45009")]
        [WorkItem(45009, "https://github.com/dotnet/roslyn/issues/45009")]
        public void Deconstruct_RefProperty()
        {
            var source =
@"using System;

record B(int X)
{
    static int _x = 2;
    ref int X => ref _x;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "2");
            verifier.VerifyDiagnostics();

            var deconstruct = verifier.Compilation.GetMember("B.Deconstruct");
            Assert.Equal("void B.Deconstruct(out System.Int32 X)", deconstruct.ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(Accessibility.Public, deconstruct.DeclaredAccessibility);
            Assert.False(deconstruct.IsAbstract);
            Assert.False(deconstruct.IsVirtual);
            Assert.False(deconstruct.IsOverride);
            Assert.False(deconstruct.IsSealed);
            Assert.True(deconstruct.IsImplicitlyDeclared);
        }

        [Fact]
        public void Deconstruct_Static()
        {
            var source = @"
using System;

record B(int X, int Y)
{
    static int Y { get; }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,21): error CS8866: Record member 'B.Y' must be a readable instance property of type 'int' to match positional parameter 'Y'.
                // record B(int X, int Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("B.Y", "int", "Y").WithLocation(4, 21));

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Overrides_01()
        {
            var source =
@"record A
{
    public sealed override bool Equals(object other) => false;
    public sealed override int GetHashCode() => 0;
    public sealed override string ToString() => null;
}
record B(int X, int Y) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,33): error CS0111: Type 'A' already defines a member called 'Equals' with the same parameter types
                //     public sealed override bool Equals(object other) => false;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "A").WithLocation(3, 33),
                // (5,35): error CS8870: 'A.ToString()' cannot be sealed because containing record is not sealed.
                //     public sealed override string ToString() => null;
                Diagnostic(ErrorCode.ERR_SealedAPIInRecord, "ToString").WithArguments("A.ToString()").WithLocation(5, 35),
                // (7,8): error CS0239: 'B.ToString()': cannot override inherited member 'A.ToString()' because it is sealed
                // record B(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "B").WithArguments("B.ToString()", "A.ToString()").WithLocation(7, 8),
                // (4,32): error CS8870: 'A.GetHashCode()' cannot be sealed because containing record is not sealed.
                //     public sealed override int GetHashCode() => 0;
                Diagnostic(ErrorCode.ERR_SealedAPIInRecord, "GetHashCode").WithArguments("A.GetHashCode()").WithLocation(4, 32),
                // (7,8): error CS0239: 'B.GetHashCode()': cannot override inherited member 'A.GetHashCode()' because it is sealed
                // record B(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "B").WithArguments("B.GetHashCode()", "A.GetHashCode()").WithLocation(7, 8)
                );

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "B..ctor(System.Int32 X, System.Int32 Y)",
                "System.Type B.EqualityContract.get",
                "System.Type B.EqualityContract { get; }",
                "System.Int32 B.<X>k__BackingField",
                "System.Int32 B.X.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.X.init",
                "System.Int32 B.X { get; init; }",
                "System.Int32 B.<Y>k__BackingField",
                "System.Int32 B.Y.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.Y.init",
                "System.Int32 B.Y { get; init; }",
                "System.String B.ToString()",
                "System.Boolean B." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean B.op_Inequality(B? r1, B? r2)",
                "System.Boolean B.op_Equality(B? r1, B? r2)",
                "System.Int32 B.GetHashCode()",
                "System.Boolean B.Equals(System.Object? obj)",
                "System.Boolean B.Equals(A? other)",
                "System.Boolean B.Equals(B? other)",
                "A B." + WellKnownMemberNames.CloneMethodName + "()",
                "B..ctor(B original)",
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Overrides_02()
        {
            var source =
@"abstract record A
{
    public abstract override bool Equals(object other);
    public abstract override int GetHashCode();
    public abstract override string ToString();
}
record B(int X, int Y) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,35): error CS0111: Type 'A' already defines a member called 'Equals' with the same parameter types
                //     public abstract override bool Equals(object other);
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "A").WithLocation(3, 35),
                // (7,8): error CS0534: 'B' does not implement inherited abstract member 'A.Equals(object)'
                // record B(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.Equals(object)").WithLocation(7, 8)
                );

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "B..ctor(System.Int32 X, System.Int32 Y)",
                "System.Type B.EqualityContract.get",
                "System.Type B.EqualityContract { get; }",
                "System.Int32 B.<X>k__BackingField",
                "System.Int32 B.X.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.X.init",
                "System.Int32 B.X { get; init; }",
                "System.Int32 B.<Y>k__BackingField",
                "System.Int32 B.Y.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.Y.init",
                "System.Int32 B.Y { get; init; }",
                "System.String B.ToString()",
                "System.Boolean B." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean B.op_Inequality(B? r1, B? r2)",
                "System.Boolean B.op_Equality(B? r1, B? r2)",
                "System.Int32 B.GetHashCode()",
                "System.Boolean B.Equals(System.Object? obj)",
                "System.Boolean B.Equals(A? other)",
                "System.Boolean B.Equals(B? other)",
                "A B." + WellKnownMemberNames.CloneMethodName + "()",
                "B..ctor(B original)",
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void ObjectEquals_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public final hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0239: 'B.Equals(object?)': cannot override inherited member 'A.Equals(object)' because it is sealed
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "B").WithArguments("B.Equals(object?)", "A.Equals(object)").WithLocation(2, 15)
                );
        }

        [Fact]
        public void ObjectEquals_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public newslot hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS8869: 'B.Equals(object?)' does not override expected method from 'object'.
                // public record B : A {
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "B").WithArguments("B.Equals(object?)").WithLocation(2, 15)
                );
        }

        [Fact]
        public void ObjectEquals_03()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public newslot hidebysig 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0506: 'B.Equals(object?)': cannot override inherited member 'A.Equals(object)' because it is not marked virtual, abstract, or override
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.Equals(object?)", "A.Equals(object)").WithLocation(2, 15)
                );
        }

        [Fact]
        public void ObjectEquals_04()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public newslot hidebysig virtual 
        instance int32 Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0508: 'B.Equals(object?)': return type must be 'int' to match overridden member 'A.Equals(object)'
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "B").WithArguments("B.Equals(object?)", "A.Equals(object)", "int").WithLocation(2, 15)
                );
        }

        [Fact]
        public void ObjectEquals_05()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual int Equals(object other) => default;
        public virtual int GetHashCode() => default;
        public virtual string ToString() => """";
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}
";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"
public record A {
}
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0508: 'A.Equals(object?)': return type must be 'int' to match overridden member 'object.Equals(object)'
                // public record A {
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "A").WithArguments("A.Equals(object?)", "object.Equals(object)", "int").WithLocation(2, 15),

                // (2,15): error CS0518: Predefined type 'System.Type' is not defined or imported
                // public record A {
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(2, 15),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
}").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.GetHashCode'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
}").WithArguments("System.Collections.Generic.EqualityComparer`1", "GetHashCode").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.op_Equality'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
}").WithArguments("System.Type", "op_Equality").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ObjectEquals_06()
        {
            var source =
@"record A
{
    public static new bool Equals(object obj) => throw null;
}

record B : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,28): error CS0111: Type 'A' already defines a member called 'Equals' with the same parameter types
                //     public static new bool Equals(object obj) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "A").WithLocation(3, 28)
                );
        }

        [Fact]
        public void ObjectGetHashCode_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public newslot hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source1 = @"
public record B : A {
}";
            var source2 = @"
public record B : A {
    public override int GetHashCode() => 0;
}";
            var source3 = @"
public record C : B {
}
";
            var source4 = @"
public record C : B {
    public override int GetHashCode() => 0;
}
";
            var comp = CreateCompilationWithIL(new[] { source1, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS8869: 'B.GetHashCode()' does not override expected method from 'object'.
                // public record B : A {
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "B").WithArguments("B.GetHashCode()").WithLocation(2, 15)
                );

            comp = CreateCompilationWithIL(new[] { source2, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,25): error CS8869: 'B.GetHashCode()' does not override expected method from 'object'.
                //     public override int GetHashCode() => 0;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("B.GetHashCode()").WithLocation(3, 25)
                );

            comp = CreateCompilationWithIL(new[] { source1 + source3, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS8869: 'B.GetHashCode()' does not override expected method from 'object'.
                // public record B : A {
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "B").WithArguments("B.GetHashCode()").WithLocation(2, 15)
                );

            comp = CreateCompilationWithIL(new[] { source1 + source4, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS8869: 'B.GetHashCode()' does not override expected method from 'object'.
                // public record B : A {
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "B").WithArguments("B.GetHashCode()").WithLocation(2, 15)
                );

            comp = CreateCompilationWithIL(new[] { source2 + source3, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,25): error CS8869: 'B.GetHashCode()' does not override expected method from 'object'.
                //     public override int GetHashCode() => 0;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("B.GetHashCode()").WithLocation(3, 25)
                );

            comp = CreateCompilationWithIL(new[] { source2 + source4, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,25): error CS8869: 'B.GetHashCode()' does not override expected method from 'object'.
                //     public override int GetHashCode() => 0;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("B.GetHashCode()").WithLocation(3, 25)
                );
        }

        [Fact]
        public void ObjectGetHashCode_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public newslot hidebysig  
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source1 = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source1, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0506: 'B.GetHashCode()': cannot override inherited member 'A.GetHashCode()' because it is not marked virtual, abstract, or override
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.GetHashCode()", "A.GetHashCode()").WithLocation(2, 15)
                );

            var source2 = @"
public record B : A {
    public override int GetHashCode() => throw null;
}";
            comp = CreateCompilationWithIL(new[] { source2, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,25): error CS0506: 'B.GetHashCode()': cannot override inherited member 'A.GetHashCode()' because it is not marked virtual, abstract, or override
                //     public override int GetHashCode() => throw null;
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "GetHashCode").WithArguments("B.GetHashCode()", "A.GetHashCode()").WithLocation(3, 25)
                );
        }

        [Fact]
        public void ObjectGetHashCode_03()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public newslot hidebysig virtual 
        instance bool GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0508: 'B.GetHashCode()': return type must be 'bool' to match overridden member 'A.GetHashCode()'
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "B").WithArguments("B.GetHashCode()", "A.GetHashCode()", "bool").WithLocation(2, 15)
                );

            var source2 = @"
public record B : A {
    public override int GetHashCode() => throw null;
}";
            comp = CreateCompilationWithIL(new[] { source2, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,25): error CS0508: 'B.GetHashCode()': return type must be 'bool' to match overridden member 'A.GetHashCode()'
                //     public override int GetHashCode() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "GetHashCode").WithArguments("B.GetHashCode()", "A.GetHashCode()", "bool").WithLocation(3, 25)
                );
        }

        [Fact]
        public void ObjectGetHashCode_04()
        {
            var source =
@"record A
{
    public override bool GetHashCode() => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,26): error CS0508: 'A.GetHashCode()': return type must be 'int' to match overridden member 'object.GetHashCode()'
                //     public override bool GetHashCode() => throw null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "GetHashCode").WithArguments("A.GetHashCode()", "object.GetHashCode()", "int").WithLocation(3, 26)
                );
        }

        [Fact]
        public void ObjectGetHashCode_05()
        {
            var source =
@"record A
{
    public new int GetHashCode() => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,20): error CS8869: 'A.GetHashCode()' does not override expected method from 'object'.
                //     public new int GetHashCode() => throw null;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("A.GetHashCode()").WithLocation(3, 20)
                );
        }

        [Fact]
        public void ObjectGetHashCode_06()
        {
            var source =
@"record A
{
    public static new int GetHashCode() => throw null;
}

record B : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,27): error CS8869: 'A.GetHashCode()' does not override expected method from 'object'.
                //     public static new int GetHashCode() => throw null;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("A.GetHashCode()").WithLocation(3, 27),
                // (6,8): error CS0506: 'B.GetHashCode()': cannot override inherited member 'A.GetHashCode()' because it is not marked virtual, abstract, or override
                // record B : A;
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.GetHashCode()", "A.GetHashCode()").WithLocation(6, 8)
                );
        }

        [Fact]
        public void ObjectGetHashCode_07()
        {
            var source =
@"record A
{
    public new int GetHashCode => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,20): error CS0102: The type 'A' already contains a definition for 'GetHashCode'
                //     public new int GetHashCode => throw null;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "GetHashCode").WithArguments("A", "GetHashCode").WithLocation(3, 20)
                );
        }

        [Fact]
        public void ObjectGetHashCode_08()
        {
            var source =
@"record A
{
    public new void GetHashCode() => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,21): error CS8869: 'A.GetHashCode()' does not override expected method from 'object'.
                //     public new void GetHashCode() => throw null;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("A.GetHashCode()").WithLocation(3, 21)
                );
        }

        [Fact]
        public void ObjectGetHashCode_09()
        {
            var source =
@"record A
{
    public void GetHashCode(int x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            Assert.Equal("System.Int32 A.GetHashCode()", comp.GetMembers("A.GetHashCode").First().ToTestDisplayString());
        }

        [Fact]
        public void ObjectGetHashCode_10()
        {
            var source =
@"
record A
{
    public sealed override int GetHashCode() => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,32): error CS8870: 'A.GetHashCode()' cannot be sealed because containing record is not sealed.
                //     public sealed override int GetHashCode() => throw null;
                Diagnostic(ErrorCode.ERR_SealedAPIInRecord, "GetHashCode").WithArguments("A.GetHashCode()").WithLocation(4, 32)
                );
        }

        [Fact]
        public void ObjectGetHashCode_11()
        {
            var source =
@"
sealed record A
{
    public sealed override int GetHashCode() => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ObjectGetHashCode_12()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public final hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0239: 'B.GetHashCode()': cannot override inherited member 'A.GetHashCode()' because it is sealed
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "B").WithArguments("B.GetHashCode()", "A.GetHashCode()").WithLocation(2, 15)
                );

            var source2 = @"
public record B : A {
    public override int GetHashCode() => throw null;
}";
            comp = CreateCompilationWithIL(new[] { source2, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,25): error CS0239: 'B.GetHashCode()': cannot override inherited member 'A.GetHashCode()' because it is sealed
                //     public override int GetHashCode() => throw null;
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "GetHashCode").WithArguments("B.GetHashCode()", "A.GetHashCode()").WithLocation(3, 25)
                );
        }

        [Fact]
        public void ObjectGetHashCode_13()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public newslot hidebysig virtual 
        instance class A GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source2 = @"
public record B : A {
    public override A GetHashCode() => default;
}";
            var comp = CreateCompilationWithIL(new[] { source2, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,23): error CS8869: 'B.GetHashCode()' does not override expected method from 'object'.
                //     public override A GetHashCode() => default;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("B.GetHashCode()").WithLocation(3, 23)
                );
        }

        [Fact]
        public void ObjectGetHashCode_14()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public newslot hidebysig virtual 
        instance class A GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source2 = @"
public record B : A {
    public override B GetHashCode() => default;
}";
            var comp = CreateCompilationWithIL(new[] { source2, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,23): error CS8830: 'B.GetHashCode()': Target runtime doesn't support covariant return types in overrides. Return type must be 'A' to match overridden member 'A.GetHashCode()'
                //     public override B GetHashCode() => default;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, "GetHashCode").WithArguments("B.GetHashCode()", "A.GetHashCode()", "A").WithLocation(3, 23)
                );
        }

        [Fact]
        public void ObjectGetHashCode_15()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual Something GetHashCode() => default;
        public virtual string ToString() => """";
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}

public class Something
{
}
";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"
public record A {
    public override Something GetHashCode() => default;
}
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,31): error CS8869: 'A.GetHashCode()' does not override expected method from 'object'.
                //     public override Something GetHashCode() => default;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("A.GetHashCode()").WithLocation(3, 31),
                // (2,15): error CS0518: Predefined type 'System.Type' is not defined or imported
                // public record A {
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(2, 15),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
    public override Something GetHashCode() => default;
}").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.op_Equality'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
    public override Something GetHashCode() => default;
}").WithArguments("System.Type", "op_Equality").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ObjectGetHashCode_16()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual bool GetHashCode() => default;
        public virtual string ToString() => """";
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}
";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"
public record A {
    public override bool GetHashCode() => default;
}
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,26): error CS8869: 'A.GetHashCode()' does not override expected method from 'object'.
                //     public override bool GetHashCode() => default;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "GetHashCode").WithArguments("A.GetHashCode()").WithLocation(3, 26),
                // (2,15): error CS0518: Predefined type 'System.Type' is not defined or imported
                // public record A {
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(2, 15),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
    public override bool GetHashCode() => default;
}").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.op_Equality'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
    public override bool GetHashCode() => default;
}").WithArguments("System.Type", "op_Equality").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ObjectGetHashCode_17()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual bool GetHashCode() => default;
        public virtual string ToString() => """";
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}
";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"
public record A {
}
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0508: 'A.GetHashCode()': return type must be 'bool' to match overridden member 'object.GetHashCode()'
                // public record A {
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "A").WithArguments("A.GetHashCode()", "object.GetHashCode()", "bool").WithLocation(2, 15),

                // (2,15): error CS0518: Predefined type 'System.Type' is not defined or imported
                // public record A {
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(2, 15),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Attribute' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Attribute").WithLocation(1, 1),
                // error CS0518: Predefined type 'System.Byte' is not defined or imported
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound).WithArguments("System.Byte").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
}").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.GetHashCode'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
}").WithArguments("System.Collections.Generic.EqualityComparer`1", "GetHashCode").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.op_Equality'
                // public record A {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"public record A {
}").WithArguments("System.Type", "op_Equality").WithLocation(2, 1)
                );
        }

        [Fact]
        public void BaseEquals_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot  
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0506: 'B.Equals(A?)': cannot override inherited member 'A.Equals(A)' because it is not marked virtual, abstract, or override
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.Equals(A?)", "A.Equals(A)").WithLocation(2, 15)
                );
        }

        [Fact]
        public void BaseEquals_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot final virtual  
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0506: 'B.Equals(A?)': cannot override inherited member 'A.Equals(A)' because it is not marked virtual, abstract, or override
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.Equals(A?)", "A.Equals(A)").WithLocation(2, 15)
                );
        }

        [Fact]
        public void BaseEquals_03()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance int32 Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance string ToString () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0508: 'B.Equals(A?)': return type must be 'int' to match overridden member 'A.Equals(A)'
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "B").WithArguments("B.Equals(A?)", "A.Equals(A)", "int").WithLocation(2, 15)
                );
        }

        [Fact]
        public void BaseEquals_04()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public newslot virtual 
        instance bool Equals (
            class B ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A

.class public auto ansi beforefieldinit B
    extends A
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public final virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class B ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method B::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class B

";
            var source = @"
public record C : B {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS8871: 'C.Equals(B?)' does not override expected method from 'B'.
                // public record C : B {
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseMethod, "C").WithArguments("C.Equals(B?)", "B").WithLocation(2, 15)
                );
        }

        [Fact]
        public void BaseEquals_05()
        {
            var source =
@"
record A
{
}

record B : A
{
    public override bool Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,26): error CS0111: Type 'B' already defines a member called 'Equals' with the same parameter types
                //     public override bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "B").WithLocation(8, 26)
                );
        }

        [Fact]
        public void RecordEquals_01()
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public abstract bool Equals(A x);
}
record B : A
{
    public virtual bool Equals(B other) => Report(""B.Equals(B)"");
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        A a2 = new B();

        System.Console.WriteLine(a1.Equals(a2));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
B.Equals(B)
False
").VerifyDiagnostics(
    // (5,26): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
    //     public abstract bool Equals(A x);
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(5, 26),
    // (9,25): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(B other) => Report("B.Equals(B)");
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(9, 25)
);
        }

        [Fact]
        public void RecordEquals_02()
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public abstract bool Equals(B x);
}
record B : A
{
    public override bool Equals(B other) => Report(""B.Equals(B)"");
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
B.Equals(B)
False
").VerifyDiagnostics(
    // (9,26): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
    //     public override bool Equals(B other) => Report("B.Equals(B)");
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(9, 26)
);
            var recordEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("System.Boolean A.Equals(A? other)", recordEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, recordEquals.DeclaredAccessibility);
            Assert.False(recordEquals.IsAbstract);
            Assert.True(recordEquals.IsVirtual);
            Assert.False(recordEquals.IsOverride);
            Assert.False(recordEquals.IsSealed);
            Assert.True(recordEquals.IsImplicitlyDeclared);
        }

        [Fact]
        public void RecordEquals_03()
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public abstract bool Equals(B x);
}
record B : A
{
    public sealed override bool Equals(B other) => Report(""B.Equals(B)"");
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (9,33): error CS8872: 'B.Equals(B)' must allow overriding because the containing record is not sealed.
                //     public sealed override bool Equals(B other) => Report("B.Equals(B)");
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "Equals").WithArguments("B.Equals(B)").WithLocation(9, 33),
                // (9,33): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
                //     public sealed override bool Equals(B other) => Report("B.Equals(B)");
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(9, 33)
                );
        }

        [Fact]
        public void RecordEquals_04()
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public abstract bool Equals(B x);
}
sealed record B : A
{
    public sealed override bool Equals(B other) => Report(""B.Equals(B)"");
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
B.Equals(B)
False
").VerifyDiagnostics(
    // (9,33): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
    //     public sealed override bool Equals(B other) => Report("B.Equals(B)");
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(9, 33)
);

            var copyCtor = comp.GetMember<NamedTypeSymbol>("A").InstanceConstructors.Where(c => c.ParameterCount == 1).Single();
            Assert.Equal(Accessibility.Protected, copyCtor.DeclaredAccessibility);
            Assert.False(copyCtor.IsOverride);
            Assert.False(copyCtor.IsVirtual);
            Assert.False(copyCtor.IsAbstract);
            Assert.False(copyCtor.IsSealed);
            Assert.True(copyCtor.IsImplicitlyDeclared);

            copyCtor = comp.GetMember<NamedTypeSymbol>("B").InstanceConstructors.Where(c => c.ParameterCount == 1).Single();
            Assert.Equal(Accessibility.Private, copyCtor.DeclaredAccessibility);
            Assert.False(copyCtor.IsOverride);
            Assert.False(copyCtor.IsVirtual);
            Assert.False(copyCtor.IsAbstract);
            Assert.False(copyCtor.IsSealed);
            Assert.True(copyCtor.IsImplicitlyDeclared);
        }

        [Fact]
        public void RecordEquals_05()
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public abstract bool Equals(B x);
}
abstract record B : A
{
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (7,17): error CS0533: 'B.Equals(B?)' hides inherited abstract member 'A.Equals(B)'
                // abstract record B : A
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "B").WithArguments("B.Equals(B?)", "A.Equals(B)").WithLocation(7, 17)
                );

            var recordEquals = comp.GetMembers("B.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("System.Boolean B.Equals(B? other)", recordEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, recordEquals.DeclaredAccessibility);
            Assert.False(recordEquals.IsAbstract);
            Assert.True(recordEquals.IsVirtual);
            Assert.False(recordEquals.IsOverride);
            Assert.False(recordEquals.IsSealed);
            Assert.True(recordEquals.IsImplicitlyDeclared);
        }

        [Theory]
        [InlineData("")]
        [InlineData("sealed ")]
        public void RecordEquals_06(string modifiers)
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public abstract bool Equals(B x);
}
" + modifiers + @"
record B : A
{
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (8,8): error CS0534: 'B' does not implement inherited abstract member 'A.Equals(B)'
                // record B : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.Equals(B)").WithLocation(8, 8)
                );
        }

        [Theory]
        [InlineData("")]
        [InlineData("sealed ")]
        public void RecordEquals_07(string modifiers)
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public virtual bool Equals(B x) => Report(""A.Equals(B)"");
}
" + modifiers + @"
record B : A
{
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
        System.Console.WriteLine(b2.Equals(a1));
        System.Console.WriteLine(b2.Equals((B)a1));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
A.Equals(B)
False
True
True
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("")]
        [InlineData("sealed ")]
        public void RecordEquals_08(string modifiers)
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public abstract bool Equals(C x);
}
abstract record B : A
{
    public override bool Equals(C x) => Report(""B.Equals(C)"");
}
" + modifiers + @"
record C : B
{
}
class Program
{
    static void Main()
    {
        A a1 = new C();
        C c2 = new C();

        System.Console.WriteLine(a1.Equals(c2));
        System.Console.WriteLine(c2.Equals(a1));
        System.Console.WriteLine(c2.Equals((C)a1));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
B.Equals(C)
False
True
True
").VerifyDiagnostics();

            var clone = comp.GetMember<MethodSymbol>("A." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.False(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.True(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);

            clone = comp.GetMember<MethodSymbol>("B." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.True(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.True(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);

            clone = comp.GetMember<MethodSymbol>("C." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.True(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.False(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);
        }

        [Theory]
        [InlineData("")]
        [InlineData("sealed ")]
        public void RecordEquals_09(string modifiers)
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public bool Equals(B x) => Report(""A.Equals(B)"");
}
" + modifiers + @"
record B : A
{
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
        System.Console.WriteLine(b2.Equals(a1));
        System.Console.WriteLine(b2.Equals((B)a1));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
A.Equals(B)
False
True
True
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("private protected")]
        [InlineData("internal protected")]
        public void RecordEquals_10(string accessibility)
        {
            var source =
$@"
record A
{{
    { accessibility } virtual bool Equals(A x)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,...): error CS8873: Record member 'A.Equals(A)' must be public.
                //     { accessibility } virtual bool Equals(A x)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 19 + accessibility.Length),
                // (4,...): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     { accessibility } virtual bool Equals(A x)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 19 + accessibility.Length)
                );
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        public void RecordEquals_11(string accessibility)
        {
            var source =
$@"
record A
{{
    { accessibility } virtual bool Equals(A x)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,...): error CS0621: 'A.Equals(A)': virtual or abstract members cannot be private
                //      virtual bool Equals(A x)
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 19 + accessibility.Length),
                // (4,...): error CS8873: Record member 'A.Equals(A)' must be public.
                //     { accessibility } virtual bool Equals(A x)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 19 + accessibility.Length),
                // (4,...): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //      virtual bool Equals(A x)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 19 + accessibility.Length)
                );
        }

        [Fact]
        public void RecordEquals_12()
        {
            var source =
@"
record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    public virtual bool Equals(B other) => Report(""A.Equals(B)"");
}
class B
{
}
class Program
{
    static void Main()
    {
        A a1 = new A();
        A a2 = new A();

        System.Console.WriteLine(a1.Equals(a2));
        System.Console.WriteLine(a1.Equals((object)a2));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
True
True
").VerifyDiagnostics();
            var recordEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("System.Boolean A.Equals(A? other)", recordEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, recordEquals.DeclaredAccessibility);
            Assert.False(recordEquals.IsAbstract);
            Assert.True(recordEquals.IsVirtual);
            Assert.False(recordEquals.IsOverride);
            Assert.False(recordEquals.IsSealed);
            Assert.True(recordEquals.IsImplicitlyDeclared);
        }

        [Fact]
        public void RecordEquals_13()
        {
            var source =
@"
record A
{
    public virtual int Equals(A other)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS8874: Record member 'A.Equals(A)' must return 'bool'.
                //     public virtual int Equals(A other)
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "Equals").WithArguments("A.Equals(A)", "bool").WithLocation(4, 24),
                // (4,24): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public virtual int Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void RecordEquals_14()
        {
            var source =
@"
record A
{
    public virtual bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.MakeTypeMissing(SpecialType.System_Boolean);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"record A
{
    public virtual bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}").WithArguments("System.Boolean").WithLocation(2, 1),
                // (2,1): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"record A
{
    public virtual bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}").WithArguments("System.Boolean").WithLocation(2, 1),
                // (2,1): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"record A
{
    public virtual bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}").WithArguments("System.Boolean").WithLocation(2, 1),
                // (2,1): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"record A
{
    public virtual bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}").WithArguments("System.Boolean").WithLocation(2, 1),
                // (2,8): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 8),
                // (4,20): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                //     public virtual bool Equals(A other)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "bool").WithArguments("System.Boolean").WithLocation(4, 20),
                // (4,25): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public virtual bool Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 25)
                );
        }

        [Fact]
        public void RecordEquals_15()
        {
            var source =
@"
record A
{
    public virtual Boolean Equals(A other)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,20): error CS0246: The type or namespace name 'Boolean' could not be found (are you missing a using directive or an assembly reference?)
                //     public virtual Boolean Equals(A other)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Boolean").WithArguments("Boolean").WithLocation(4, 20),
                // (4,28): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public virtual Boolean Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 28)
                );
        }

        [Fact]
        public void RecordEquals_16()
        {
            var source =
@"
abstract record A
{
}
record B : A
{
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
        System.Console.WriteLine(b2.Equals(a1));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
True
True
").VerifyDiagnostics();

            var recordEquals = comp.GetMembers("B.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("System.Boolean B.Equals(B? other)", recordEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, recordEquals.DeclaredAccessibility);
            Assert.False(recordEquals.IsAbstract);
            Assert.True(recordEquals.IsVirtual);
            Assert.False(recordEquals.IsOverride);
            Assert.False(recordEquals.IsSealed);
            Assert.True(recordEquals.IsImplicitlyDeclared);
        }

        [Fact]
        public void RecordEquals_17()
        {
            var source =
@"
abstract record A
{
}
sealed record B : A
{
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
        System.Console.WriteLine(b2.Equals(a1));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
True
True
").VerifyDiagnostics();

            var recordEquals = comp.GetMembers("B.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("System.Boolean B.Equals(B? other)", recordEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, recordEquals.DeclaredAccessibility);
            Assert.False(recordEquals.IsAbstract);
            Assert.False(recordEquals.IsVirtual);
            Assert.False(recordEquals.IsOverride);
            Assert.False(recordEquals.IsSealed);
            Assert.True(recordEquals.IsImplicitlyDeclared);
        }

        [Fact]
        public void RecordEquals_18()
        {
            var source =
@"
sealed record A
{
}
class Program
{
    static void Main()
    {
        A a1 = new A();
        A a2 = new A();

        System.Console.WriteLine(a1.Equals(a2));
        System.Console.WriteLine(a2.Equals(a1));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
True
True
").VerifyDiagnostics();

            var recordEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("System.Boolean A.Equals(A? other)", recordEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, recordEquals.DeclaredAccessibility);
            Assert.False(recordEquals.IsAbstract);
            Assert.False(recordEquals.IsVirtual);
            Assert.False(recordEquals.IsOverride);
            Assert.False(recordEquals.IsSealed);
            Assert.True(recordEquals.IsImplicitlyDeclared);
        }

        [Fact]
        public void RecordEquals_19()
        {
            var source =
@"
record A
{
    public static bool Equals(A x) => throw null;
}

record B : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,8): error CS0736: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is static.
                // record A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(2, 8),
                // (4,24): error CS8872: 'A.Equals(A)' must allow overriding because the containing record is not sealed.
                //     public static bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 24),
                // (4,24): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public static bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 24),
                // (7,8): error CS0506: 'B.Equals(A?)': cannot override inherited member 'A.Equals(A)' because it is not marked virtual, abstract, or override
                // record B : A;
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.Equals(A?)", "A.Equals(A)").WithLocation(7, 8)
                );
        }

        [Fact]
        public void RecordEquals_20()
        {
            var source =
@"
sealed record A
{
    public static bool Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0736: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is static.
                // sealed record A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(2, 15),
                // (4,24): error CS8877: Record member 'A.Equals(A)' may not be static.
                //     public static bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 24),
                // (4,24): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public static bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void EqualityContract_01()
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }

    protected abstract System.Type EqualityContract { get; }
}
record B : A
{
    protected override System.Type EqualityContract
    {
        get
        {
            Report(""B.EqualityContract"");
            return typeof(B);
        }
    }
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        A a2 = new B();

        System.Console.WriteLine(a1.Equals(a2));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
B.EqualityContract
B.EqualityContract
True
").VerifyDiagnostics();
        }

        [Fact]
        public void EqualityContract_02()
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    protected abstract System.Type EqualityContract { get; }
}
record B : A
{
    protected sealed override System.Type EqualityContract
    {
        get
        {
            Report(""B.EqualityContract"");
            return typeof(B);
        }
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (9,43): error CS8872: 'B.EqualityContract' must allow overriding because the containing record is not sealed.
                //     protected sealed override System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "EqualityContract").WithArguments("B.EqualityContract").WithLocation(9, 43)
                );
        }

        [Fact]
        public void EqualityContract_03()
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }

    protected abstract System.Type EqualityContract { get; }
}
sealed record B : A
{
    protected sealed override System.Type EqualityContract
    {
        get
        {
            Report(""B.EqualityContract"");
            return typeof(B);
        }
    }
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        A a2 = new B();

        System.Console.WriteLine(a1.Equals(a2));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
B.EqualityContract
B.EqualityContract
True
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("")]
        [InlineData("sealed ")]
        public void EqualityContract_04(string modifiers)
        {
            var source =
@"
abstract record A
{
    internal static bool Report(string s) { System.Console.WriteLine(s); return false; }
    protected virtual System.Type EqualityContract
    {
        get
        {
            Report(""A.EqualityContract"");
            return typeof(B);
        }
    }
}
" + modifiers + @"
record B : A
{
}
class Program
{
    static void Main()
    {
        A a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
        System.Console.WriteLine(b2.Equals(a1));
        System.Console.WriteLine(b2.Equals((B)a1));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"
True
True
True
").VerifyDiagnostics();

            var equalityContract = comp.GetMembers("B.EqualityContract").OfType<SynthesizedRecordEqualityContractProperty>().Single();
            Assert.Equal("System.Type B.EqualityContract { get; }", equalityContract.ToTestDisplayString());
            Assert.Equal(Accessibility.Protected, equalityContract.DeclaredAccessibility);
            Assert.False(equalityContract.IsAbstract);
            Assert.False(equalityContract.IsVirtual);
            Assert.True(equalityContract.IsOverride);
            Assert.False(equalityContract.IsSealed);
            Assert.True(equalityContract.IsImplicitlyDeclared);
            Assert.Empty(equalityContract.DeclaringSyntaxReferences);

            var equalityContractGet = equalityContract.GetMethod;
            Assert.Equal("System.Type B.EqualityContract { get; }", equalityContract.ToTestDisplayString());
            Assert.Equal(Accessibility.Protected, equalityContractGet.DeclaredAccessibility);
            Assert.False(equalityContractGet.IsAbstract);
            Assert.False(equalityContractGet.IsVirtual);
            Assert.True(equalityContractGet.IsOverride);
            Assert.False(equalityContractGet.IsSealed);
            Assert.True(equalityContractGet.IsImplicitlyDeclared);
            Assert.Empty(equalityContractGet.DeclaringSyntaxReferences);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("private protected")]
        [InlineData("internal protected")]
        public void EqualityContract_05(string accessibility)
        {
            var source =
$@"
record A
{{
    { accessibility } virtual System.Type EqualityContract
        => throw null;
}}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,...): error CS8875: Record member 'A.EqualityContract' must be protected.
                //     { accessibility } virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_NonProtectedAPIInRecord, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(4, 26 + accessibility.Length)
                );
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        public void EqualityContract_06(string accessibility)
        {
            var source =
$@"
record A
{{
    { accessibility } virtual System.Type EqualityContract
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,...): error CS0621: 'A.EqualityContract': virtual or abstract members cannot be private
                //      { accessibility } virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(4, 26 + accessibility.Length),
                // (4,...): error CS8875: Record member 'A.EqualityContract' must be protected.
                //      { accessibility } virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_NonProtectedAPIInRecord, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(4, 26 + accessibility.Length)
                );
        }

        [Theory]
        [InlineData("")]
        [InlineData("abstract ")]
        [InlineData("sealed ")]
        public void EqualityContract_07(string modifiers)
        {
            var source =
@"
record A
{
}
" + modifiers + @"
record B : A
{
    public void PrintEqualityContract() => System.Console.WriteLine(EqualityContract);
}
";

            if (modifiers != "abstract ")
            {
                source +=
@"
class Program
{
    static void Main()
    {
        A a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
        System.Console.WriteLine(b2.Equals(a1));
        System.Console.WriteLine(b2.Equals((B)a1));
        b2.PrintEqualityContract();
    }
}";
            }

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: modifiers == "abstract " ? TestOptions.ReleaseDll : TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: modifiers == "abstract " ? null :
@"
True
True
True
B
").VerifyDiagnostics();

            var equalityContract = comp.GetMembers("B.EqualityContract").OfType<SynthesizedRecordEqualityContractProperty>().Single();
            Assert.Equal("System.Type B.EqualityContract { get; }", equalityContract.ToTestDisplayString());
            Assert.Equal(Accessibility.Protected, equalityContract.DeclaredAccessibility);
            Assert.False(equalityContract.IsAbstract);
            Assert.False(equalityContract.IsVirtual);
            Assert.True(equalityContract.IsOverride);
            Assert.False(equalityContract.IsSealed);
            Assert.True(equalityContract.IsImplicitlyDeclared);
            Assert.Empty(equalityContract.DeclaringSyntaxReferences);

            var equalityContractGet = equalityContract.GetMethod;
            Assert.Equal("System.Type B.EqualityContract { get; }", equalityContract.ToTestDisplayString());
            Assert.Equal(Accessibility.Protected, equalityContractGet.DeclaredAccessibility);
            Assert.False(equalityContractGet.IsAbstract);
            Assert.False(equalityContractGet.IsVirtual);
            Assert.True(equalityContractGet.IsOverride);
            Assert.False(equalityContractGet.IsSealed);
            Assert.True(equalityContractGet.IsImplicitlyDeclared);
            Assert.Empty(equalityContractGet.DeclaringSyntaxReferences);

            verifier.VerifyIL("B.EqualityContract.get", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldtoken    ""B""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ret
}
");
        }

        [Theory]
        [InlineData("")]
        [InlineData("abstract ")]
        [InlineData("sealed ")]
        public void EqualityContract_08(string modifiers)
        {
            var source =
modifiers + @"
record B
{
    public void PrintEqualityContract() => System.Console.WriteLine(EqualityContract);
}
";

            if (modifiers != "abstract ")
            {
                source +=
@"
class Program
{
    static void Main()
    {
        B a1 = new B();
        B b2 = new B();

        System.Console.WriteLine(a1.Equals(b2));
        System.Console.WriteLine(b2.Equals(a1));
        System.Console.WriteLine(b2.Equals((B)a1));
        b2.PrintEqualityContract();
    }
}";
            }

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: modifiers == "abstract " ? TestOptions.ReleaseDll : TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: modifiers == "abstract " ? null :
@"
True
True
True
B
").VerifyDiagnostics();

            var equalityContract = comp.GetMembers("B.EqualityContract").OfType<SynthesizedRecordEqualityContractProperty>().Single();
            Assert.Equal("System.Type B.EqualityContract { get; }", equalityContract.ToTestDisplayString());
            Assert.Equal(modifiers == "sealed " ? Accessibility.Private : Accessibility.Protected, equalityContract.DeclaredAccessibility);
            Assert.False(equalityContract.IsAbstract);
            Assert.Equal(modifiers != "sealed ", equalityContract.IsVirtual);
            Assert.False(equalityContract.IsOverride);
            Assert.False(equalityContract.IsSealed);
            Assert.True(equalityContract.IsImplicitlyDeclared);
            Assert.Empty(equalityContract.DeclaringSyntaxReferences);

            var equalityContractGet = equalityContract.GetMethod;
            Assert.Equal("System.Type B.EqualityContract { get; }", equalityContract.ToTestDisplayString());
            Assert.Equal(modifiers == "sealed " ? Accessibility.Private : Accessibility.Protected, equalityContractGet.DeclaredAccessibility);
            Assert.False(equalityContractGet.IsAbstract);
            Assert.Equal(modifiers != "sealed ", equalityContractGet.IsVirtual);
            Assert.False(equalityContractGet.IsOverride);
            Assert.False(equalityContractGet.IsSealed);
            Assert.True(equalityContractGet.IsImplicitlyDeclared);
            Assert.Empty(equalityContractGet.DeclaringSyntaxReferences);

            verifier.VerifyIL("B.EqualityContract.get", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldtoken    ""B""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ret
}
");
        }

        [Fact]
        public void EqualityContract_09()
        {
            var source =
@"
record A
{
    protected virtual int EqualityContract
        => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,27): error CS8874: Record member 'A.EqualityContract' must return 'Type'.
                //     protected virtual int EqualityContract
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "EqualityContract").WithArguments("A.EqualityContract", "System.Type").WithLocation(4, 27)
                );
        }

        [Fact]
        public void EqualityContract_10()
        {
            var source =
@"
record A
{
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.MakeTypeMissing(WellKnownType.System_Type);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Type.GetTypeFromHandle'
                // record A
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"record A
{
}").WithArguments("System.Type", "GetTypeFromHandle").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Type.op_Equality'
                // record A
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"record A
{
}").WithArguments("System.Type", "op_Equality").WithLocation(2, 1),
                // (2,8): error CS0518: Predefined type 'System.Type' is not defined or imported
                // record A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(2, 8)
                );
        }

        [Fact]
        public void EqualityContract_11()
        {
            var source =
@"
record A
{
    protected virtual Type EqualityContract
        => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,23): error CS0246: The type or namespace name 'Type' could not be found (are you missing a using directive or an assembly reference?)
                //     protected virtual Type EqualityContract
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Type").WithArguments("Type").WithLocation(4, 23)
                );
        }

        [Fact]
        public void EqualityContract_12()
        {
            var source =
@"
record A
{
    protected System.Type EqualityContract
        => throw null;
}

sealed record B
{
    protected System.Type EqualityContract
        => throw null;
}

sealed record C
{
    protected virtual System.Type EqualityContract
        => throw null;
}

record D
{
    protected virtual System.Type EqualityContract
        => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,27): error CS8872: 'A.EqualityContract' must allow overriding because the containing record is not sealed.
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(4, 27),
                // (10,27): warning CS0628: 'B.EqualityContract': new protected member declared in sealed type
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "EqualityContract").WithArguments("B.EqualityContract").WithLocation(10, 27),
                // (10,27): error CS8879: Record member 'B.EqualityContract' must be private.
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_NonPrivateAPIInRecord, "EqualityContract").WithArguments("B.EqualityContract").WithLocation(10, 27),
                // (11,12): warning CS0628: 'B.EqualityContract.get': new protected member declared in sealed type
                //         => throw null;
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "throw null").WithArguments("B.EqualityContract.get").WithLocation(11, 12),
                // (16,35): warning CS0628: 'C.EqualityContract': new protected member declared in sealed type
                //     protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "EqualityContract").WithArguments("C.EqualityContract").WithLocation(16, 35),
                // (16,35): error CS8879: Record member 'C.EqualityContract' must be private.
                //     protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_NonPrivateAPIInRecord, "EqualityContract").WithArguments("C.EqualityContract").WithLocation(16, 35),
                // (17,12): error CS0549: 'C.EqualityContract.get' is a new virtual member in sealed type 'C'
                //         => throw null;
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, "throw null").WithArguments("C.EqualityContract.get", "C").WithLocation(17, 12)
                );
        }

        [Fact]
        public void EqualityContract_13()
        {
            var source =
@"
record A
{}

record B : A
{
    protected System.Type EqualityContract
        => throw null;
}

sealed record C : A
{
    protected System.Type EqualityContract
        => throw null;
}

sealed record D : A
{
    protected virtual System.Type EqualityContract
        => throw null;
}

record E : A
{
    protected virtual System.Type EqualityContract
        => throw null;
}

record F : A
{
    protected override System.Type EqualityContract
        => throw null;
}

record G : A
{
    protected sealed override System.Type EqualityContract
        => throw null;
}

sealed record H : A
{
    protected sealed override System.Type EqualityContract
        => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (7,27): error CS8876: 'B.EqualityContract' does not override expected property from 'A'.
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("B.EqualityContract", "A").WithLocation(7, 27),
                // (7,27): error CS8872: 'B.EqualityContract' must allow overriding because the containing record is not sealed.
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "EqualityContract").WithArguments("B.EqualityContract").WithLocation(7, 27),
                // (7,27): warning CS0114: 'B.EqualityContract' hides inherited member 'A.EqualityContract'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "EqualityContract").WithArguments("B.EqualityContract", "A.EqualityContract").WithLocation(7, 27),
                // (13,27): warning CS0628: 'C.EqualityContract': new protected member declared in sealed type
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "EqualityContract").WithArguments("C.EqualityContract").WithLocation(13, 27),
                // (13,27): error CS8876: 'C.EqualityContract' does not override expected property from 'A'.
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("C.EqualityContract", "A").WithLocation(13, 27),
                // (13,27): warning CS0114: 'C.EqualityContract' hides inherited member 'A.EqualityContract'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     protected System.Type EqualityContract
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "EqualityContract").WithArguments("C.EqualityContract", "A.EqualityContract").WithLocation(13, 27),
                // (14,12): warning CS0628: 'C.EqualityContract.get': new protected member declared in sealed type
                //         => throw null;
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "throw null").WithArguments("C.EqualityContract.get").WithLocation(14, 12),
                // (19,35): warning CS0628: 'D.EqualityContract': new protected member declared in sealed type
                //     protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "EqualityContract").WithArguments("D.EqualityContract").WithLocation(19, 35),
                // (19,35): error CS8876: 'D.EqualityContract' does not override expected property from 'A'.
                //     protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("D.EqualityContract", "A").WithLocation(19, 35),
                // (19,35): warning CS0114: 'D.EqualityContract' hides inherited member 'A.EqualityContract'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "EqualityContract").WithArguments("D.EqualityContract", "A.EqualityContract").WithLocation(19, 35),
                // (20,12): error CS0549: 'D.EqualityContract.get' is a new virtual member in sealed type 'D'
                //         => throw null;
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, "throw null").WithArguments("D.EqualityContract.get", "D").WithLocation(20, 12),
                // (25,35): error CS8876: 'E.EqualityContract' does not override expected property from 'A'.
                //     protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("E.EqualityContract", "A").WithLocation(25, 35),
                // (25,35): warning CS0114: 'E.EqualityContract' hides inherited member 'A.EqualityContract'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "EqualityContract").WithArguments("E.EqualityContract", "A.EqualityContract").WithLocation(25, 35),
                // (37,43): error CS8872: 'G.EqualityContract' must allow overriding because the containing record is not sealed.
                //     protected sealed override System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "EqualityContract").WithArguments("G.EqualityContract").WithLocation(37, 43)
                );
        }

        [Fact]
        public void EqualityContract_14()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual  
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}

public record C : A {
    new protected virtual System.Type EqualityContract
        => throw null;
}

public record D : A {
    new protected virtual int EqualityContract
        => throw null;
}

public record E : A {
    new protected virtual Type EqualityContract
        => throw null;
}

public record F : A {
    protected override System.Type EqualityContract
        => throw null;
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0506: 'B.EqualityContract': cannot override inherited member 'A.EqualityContract' because it is not marked virtual, abstract, or override
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.EqualityContract", "A.EqualityContract").WithLocation(2, 15),
                // (6,39): error CS8876: 'C.EqualityContract' does not override expected property from 'A'.
                //     new protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("C.EqualityContract", "A").WithLocation(6, 39),
                // (11,31): error CS8874: Record member 'D.EqualityContract' must return 'Type'.
                //     new protected virtual int EqualityContract
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "EqualityContract").WithArguments("D.EqualityContract", "System.Type").WithLocation(11, 31),
                // (16,27): error CS0246: The type or namespace name 'Type' could not be found (are you missing a using directive or an assembly reference?)
                //     new protected virtual Type EqualityContract
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Type").WithArguments("Type").WithLocation(16, 27),
                // (21,36): error CS0506: 'F.EqualityContract': cannot override inherited member 'A.EqualityContract' because it is not marked virtual, abstract, or override
                //     protected override System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "EqualityContract").WithArguments("F.EqualityContract", "A.EqualityContract").WithLocation(21, 36)
                );
        }

        [Fact]
        public void EqualityContract_15()
        {
            var source =
@"
record A
{
    protected virtual int EqualityContract
        => throw null;
}

record B : A
{
}

record C : A
{
    protected override System.Type EqualityContract
           => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,27): error CS8874: Record member 'A.EqualityContract' must return 'Type'.
                //     protected virtual int EqualityContract
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "EqualityContract").WithArguments("A.EqualityContract", "System.Type").WithLocation(4, 27),
                // (8,8): error CS1715: 'B.EqualityContract': type must be 'int' to match overridden member 'A.EqualityContract'
                // record B : A
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "B").WithArguments("B.EqualityContract", "A.EqualityContract", "int").WithLocation(8, 8),
                // (14,36): error CS1715: 'C.EqualityContract': type must be 'int' to match overridden member 'A.EqualityContract'
                //     protected override System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "EqualityContract").WithArguments("C.EqualityContract", "A.EqualityContract", "int").WithLocation(14, 36)
                );
        }

        [Fact]
        public void EqualityContract_16()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual  
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot final virtual
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}

public record C : A {
    new protected virtual System.Type EqualityContract
        => throw null;
}

public record D : A {
    new protected virtual int EqualityContract
        => throw null;
}

public record E : A {
    new protected virtual Type EqualityContract
        => throw null;
}

public record F : A {
    protected override System.Type EqualityContract
        => throw null;
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0506: 'B.EqualityContract': cannot override inherited member 'A.EqualityContract' because it is not marked virtual, abstract, or override
                // public record B : A {
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.EqualityContract", "A.EqualityContract").WithLocation(2, 15),
                // (6,39): error CS8876: 'C.EqualityContract' does not override expected property from 'A'.
                //     new protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("C.EqualityContract", "A").WithLocation(6, 39),
                // (11,31): error CS8874: Record member 'D.EqualityContract' must return 'Type'.
                //     new protected virtual int EqualityContract
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "EqualityContract").WithArguments("D.EqualityContract", "System.Type").WithLocation(11, 31),
                // (16,27): error CS0246: The type or namespace name 'Type' could not be found (are you missing a using directive or an assembly reference?)
                //     new protected virtual Type EqualityContract
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Type").WithArguments("Type").WithLocation(16, 27),
                // (21,36): error CS0506: 'F.EqualityContract': cannot override inherited member 'A.EqualityContract' because it is not marked virtual, abstract, or override
                //     protected override System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "EqualityContract").WithArguments("F.EqualityContract", "A.EqualityContract").WithLocation(21, 36)
                );
        }

        [Fact]
        public void EqualityContract_17()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual  
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class A
";
            var source = @"
public record B : A {
}

public record C : A {
    protected virtual System.Type EqualityContract
        => throw null;
}

public record D : A {
    protected virtual int EqualityContract
        => throw null;
}

public record E : A {
    protected virtual Type EqualityContract
        => throw null;
}

public record F : A {
    protected override System.Type EqualityContract
        => throw null;
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0115: 'B.EqualityContract': no suitable method found to override
                // public record B : A {
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B.EqualityContract").WithLocation(2, 15),
                // (6,35): error CS8876: 'C.EqualityContract' does not override expected property from 'A'.
                //     protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("C.EqualityContract", "A").WithLocation(6, 35),
                // (11,27): error CS8874: Record member 'D.EqualityContract' must return 'Type'.
                //     protected virtual int EqualityContract
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "EqualityContract").WithArguments("D.EqualityContract", "System.Type").WithLocation(11, 27),
                // (16,23): error CS0246: The type or namespace name 'Type' could not be found (are you missing a using directive or an assembly reference?)
                //     protected virtual Type EqualityContract
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Type").WithArguments("Type").WithLocation(16, 23),
                // (21,36): error CS0115: 'F.EqualityContract': no suitable method found to override
                //     protected override System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "EqualityContract").WithArguments("F.EqualityContract").WithLocation(21, 36)
                );
        }

        [Fact]
        public void EqualityContract_18()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit A
    extends System.Object
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public newslot virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual 
        instance class [mscorlib]System.Type get_EqualityContract () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::get_EqualityContract

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type A::get_EqualityContract()
    }
} // end of class A

.class public auto ansi beforefieldinit B
    extends A
{
    // Methods
    .method public hidebysig specialname newslot virtual 
        instance class A '" + WellKnownMemberNames.CloneMethodName + @"' () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::'" + WellKnownMemberNames.CloneMethodName + @"'

    .method public hidebysig virtual 
        instance bool Equals (
            object other
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public hidebysig virtual 
        instance int32 GetHashCode () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::GetHashCode

    .method public final virtual 
        instance bool Equals (
            class A ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::Equals

    .method public newslot virtual 
        instance bool Equals (
            class B ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method B::Equals

    .method family hidebysig specialname rtspecialname 
        instance void .ctor (
            class B ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method B::.ctor

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: throw
    } // end of method A::.ctor

    .method family hidebysig newslot virtual instance bool '" + WellKnownMemberNames.PrintMembersMethodName + @"' (class [mscorlib]System.Text.StringBuilder builder) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
} // end of class B

";
            var source = @"
public record C : B {
}

public record D : B {
    new protected virtual System.Type EqualityContract
        => throw null;
}

public record E : B {
    new protected virtual int EqualityContract
        => throw null;
}

public record F : B {
    new protected virtual Type EqualityContract
        => throw null;
}

public record G : B {
    protected override System.Type EqualityContract
        => throw null;
}
";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS8876: 'C.EqualityContract' does not override expected property from 'B'.
                // public record C : B {
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "C").WithArguments("C.EqualityContract", "B").WithLocation(2, 15),
                // (6,39): error CS8876: 'D.EqualityContract' does not override expected property from 'B'.
                //     new protected virtual System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("D.EqualityContract", "B").WithLocation(6, 39),
                // (11,31): error CS8874: Record member 'E.EqualityContract' must return 'Type'.
                //     new protected virtual int EqualityContract
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "EqualityContract").WithArguments("E.EqualityContract", "System.Type").WithLocation(11, 31),
                // (16,27): error CS0246: The type or namespace name 'Type' could not be found (are you missing a using directive or an assembly reference?)
                //     new protected virtual Type EqualityContract
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Type").WithArguments("Type").WithLocation(16, 27),
                // (21,36): error CS8876: 'G.EqualityContract' does not override expected property from 'B'.
                //     protected override System.Type EqualityContract
                Diagnostic(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, "EqualityContract").WithArguments("G.EqualityContract", "B").WithLocation(21, 36)
                );
        }

        [Fact]
        public void EqualityContract_19()
        {
            var source =
@"sealed record A
{
    protected static System.Type EqualityContract => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,34): warning CS0628: 'A.EqualityContract': new protected member declared in sealed type
                //     protected static System.Type EqualityContract => throw null;
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(3, 34),
                // (3,34): error CS8879: Record member 'A.EqualityContract' must be private.
                //     protected static System.Type EqualityContract => throw null;
                Diagnostic(ErrorCode.ERR_NonPrivateAPIInRecord, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(3, 34),
                // (3,34): error CS8877: Record member 'A.EqualityContract' may not be static.
                //     protected static System.Type EqualityContract => throw null;
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(3, 34),
                // (3,54): warning CS0628: 'A.EqualityContract.get': new protected member declared in sealed type
                //     protected static System.Type EqualityContract => throw null;
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "throw null").WithArguments("A.EqualityContract.get").WithLocation(3, 54)
                );
        }

        [Fact]
        public void EqualityContract_20()
        {
            var source =
@"sealed record A
{
    private static System.Type EqualityContract => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,32): error CS8877: Record member 'A.EqualityContract' may not be static.
                //     private static System.Type EqualityContract => throw null;
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(3, 32)
                );
        }

        [Fact]
        public void EqualityContract_21()
        {
            var source =
@"
sealed record A
{
    static void Main()
    {
        A a1 = new A();
        A a2 = new A();

        System.Console.WriteLine(a1.Equals(a2));
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "True").VerifyDiagnostics();

            var equalityContract = comp.GetMembers("A.EqualityContract").OfType<SynthesizedRecordEqualityContractProperty>().Single();
            Assert.Equal("System.Type A.EqualityContract { get; }", equalityContract.ToTestDisplayString());
            Assert.Equal(Accessibility.Private, equalityContract.DeclaredAccessibility);
            Assert.False(equalityContract.IsAbstract);
            Assert.False(equalityContract.IsVirtual);
            Assert.False(equalityContract.IsOverride);
            Assert.False(equalityContract.IsSealed);
            Assert.True(equalityContract.IsImplicitlyDeclared);
            Assert.Empty(equalityContract.DeclaringSyntaxReferences);
        }

        [Fact]
        public void EqualityContract_22()
        {
            var source =
@"
record A;
sealed record B : A
{
    static void Main()
    {
        A a1 = new B();
        A a2 = new B();

        System.Console.WriteLine(a1.Equals(a2));
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "True").VerifyDiagnostics();

            var equalityContract = comp.GetMembers("B.EqualityContract").OfType<SynthesizedRecordEqualityContractProperty>().Single();
            Assert.Equal("System.Type B.EqualityContract { get; }", equalityContract.ToTestDisplayString());
            Assert.Equal(Accessibility.Protected, equalityContract.DeclaredAccessibility);
            Assert.False(equalityContract.IsAbstract);
            Assert.False(equalityContract.IsVirtual);
            Assert.True(equalityContract.IsOverride);
            Assert.False(equalityContract.IsSealed);
            Assert.True(equalityContract.IsImplicitlyDeclared);
            Assert.Empty(equalityContract.DeclaringSyntaxReferences);
        }

        [Fact]
        public void EqualityContract_23()
        {
            var source =
@"
record A
{
    protected static System.Type EqualityContract => throw null;
}

record B : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,34): error CS8872: 'A.EqualityContract' must allow overriding because the containing record is not sealed.
                //     protected static System.Type EqualityContract => throw null;
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(4, 34),
                // (7,8): error CS0506: 'B.EqualityContract': cannot override inherited member 'A.EqualityContract' because it is not marked virtual, abstract, or override
                // record B : A;
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.EqualityContract", "A.EqualityContract").WithLocation(7, 8)
                );
        }

        [Fact]
        public void EqualityOperators_01()
        {
            var source =
@"
record A(int X) 
{
    public virtual bool Equals(ref A other)
        => throw null;

    static void Main()
    {
        Test(null, null);
        Test(null, new A(0));
        Test(new A(1), new A(1));
        Test(new A(2), new A(3));
        Test(new A(4), new B(4, 5));
        Test(new B(6, 7), new B(6, 7));
        Test(new B(8, 9), new B(8, 10));
        var a = new A(11);
        Test(a, a);
    }

    static void Test(A a1, A a2)
    {
        System.Console.WriteLine(""{0} {1} {2} {3}"", a1 == a2, a2 == a1, a1 != a2, a2 != a1);
    }
}

record B(int X, int Y) : A(X);
";
            var verifier = CompileAndVerify(source, expectedOutput:
@"
True True False False
False False True True
True True False False
False False True True
False False True True
True True False False
False False True True
True True False False
").VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            MethodSymbol op = comp.GetMembers("A." + WellKnownMemberNames.EqualityOperatorName).OfType<SynthesizedRecordEqualityOperator>().Single();
            Assert.Equal("System.Boolean A.op_Equality(A? r1, A? r2)", op.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, op.DeclaredAccessibility);
            Assert.True(op.IsStatic);
            Assert.False(op.IsAbstract);
            Assert.False(op.IsVirtual);
            Assert.False(op.IsOverride);
            Assert.False(op.IsSealed);
            Assert.True(op.IsImplicitlyDeclared);

            op = comp.GetMembers("A." + WellKnownMemberNames.InequalityOperatorName).OfType<SynthesizedRecordInequalityOperator>().Single();
            Assert.Equal("System.Boolean A.op_Inequality(A? r1, A? r2)", op.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, op.DeclaredAccessibility);
            Assert.True(op.IsStatic);
            Assert.False(op.IsAbstract);
            Assert.False(op.IsVirtual);
            Assert.False(op.IsOverride);
            Assert.False(op.IsSealed);
            Assert.True(op.IsImplicitlyDeclared);

            verifier.VerifyIL("bool A.op_Equality(A, A)", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_0011
  IL_0004:  ldarg.0
  IL_0005:  brfalse.s  IL_000f
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  callvirt   ""bool A.Equals(A)""
  IL_000e:  ret
  IL_000f:  ldc.i4.0
  IL_0010:  ret
  IL_0011:  ldc.i4.1
  IL_0012:  ret
}
");

            verifier.VerifyIL("bool A.op_Inequality(A, A)", @"

{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.op_Equality(A, A)""
  IL_0007:  ldc.i4.0
  IL_0008:  ceq
  IL_000a:  ret
}
");
        }

        [Fact]
        public void EqualityOperators_02()
        {
            var source =
@"
record B;

record A(int X) : B
{
    public virtual bool Equals(A other)
    {
        System.Console.WriteLine(""Equals(A other)"");
        return base.Equals(other) && X == other.X;
    }

    static void Main()
    {
        Test(null, null);
        Test(null, new A(0));
        Test(new A(1), new A(1));
        Test(new A(2), new A(3));
        var a = new A(11);
        Test(a, a);
        Test(new A(3), new B());
    }

    static void Test(A a1, A a2)
    {
        System.Console.WriteLine(""{0} {1} {2} {3}"", a1 == a2, a2 == a1, a1 != a2, a2 != a1);
    }

    static void Test(A a1, B b2)
    {
        System.Console.WriteLine(""{0} {1} {2} {3}"", a1 == b2, b2 == a1, a1 != b2, b2 != a1);
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput:
@"
True True False False
Equals(A other)
Equals(A other)
False False True True
Equals(A other)
Equals(A other)
Equals(A other)
Equals(A other)
True True False False
Equals(A other)
Equals(A other)
Equals(A other)
Equals(A other)
False False True True
True True False False
Equals(A other)
Equals(A other)
False False True True
").VerifyDiagnostics(
    // (6,25): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(A other)
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(6, 25)
);

            var comp = (CSharpCompilation)verifier.Compilation;
            MethodSymbol op = comp.GetMembers("A." + WellKnownMemberNames.EqualityOperatorName).OfType<SynthesizedRecordEqualityOperator>().Single();
            Assert.Equal("System.Boolean A.op_Equality(A? r1, A? r2)", op.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, op.DeclaredAccessibility);
            Assert.True(op.IsStatic);
            Assert.False(op.IsAbstract);
            Assert.False(op.IsVirtual);
            Assert.False(op.IsOverride);
            Assert.False(op.IsSealed);
            Assert.True(op.IsImplicitlyDeclared);

            op = comp.GetMembers("A." + WellKnownMemberNames.InequalityOperatorName).OfType<SynthesizedRecordInequalityOperator>().Single();
            Assert.Equal("System.Boolean A.op_Inequality(A? r1, A? r2)", op.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, op.DeclaredAccessibility);
            Assert.True(op.IsStatic);
            Assert.False(op.IsAbstract);
            Assert.False(op.IsVirtual);
            Assert.False(op.IsOverride);
            Assert.False(op.IsSealed);
            Assert.True(op.IsImplicitlyDeclared);

            verifier.VerifyIL("bool A.op_Equality(A, A)", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_0011
  IL_0004:  ldarg.0
  IL_0005:  brfalse.s  IL_000f
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  callvirt   ""bool A.Equals(A)""
  IL_000e:  ret
  IL_000f:  ldc.i4.0
  IL_0010:  ret
  IL_0011:  ldc.i4.1
  IL_0012:  ret
}
");

            verifier.VerifyIL("bool A.op_Inequality(A, A)", @"

{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.op_Equality(A, A)""
  IL_0007:  ldc.i4.0
  IL_0008:  ceq
  IL_000a:  ret
}
");
        }

        [Fact]
        public void EqualityOperators_03()
        {
            var source =
@"
record A
{
    public static bool operator==(A r1, A r2)
        => throw null;
    public static bool operator==(A r1, string r2)
        => throw null;
    public static bool operator!=(A r1, string r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,32): error CS0111: Type 'A' already defines a member called 'op_Equality' with the same parameter types
                //     public static bool operator==(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "==").WithArguments("op_Equality", "A").WithLocation(4, 32)
                );
        }

        [Fact]
        public void EqualityOperators_04()
        {
            var source =
@"
record A
{
    public static bool operator!=(A r1, A r2)
        => throw null;
    public static bool operator!=(string r1, A r2)
        => throw null;
    public static bool operator==(string r1, A r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,32): error CS0111: Type 'A' already defines a member called 'op_Inequality' with the same parameter types
                //     public static bool operator!=(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "!=").WithArguments("op_Inequality", "A").WithLocation(4, 32)
                );
        }

        [Fact]
        public void EqualityOperators_05()
        {
            var source =
@"
record A
{
    public static bool op_Equality(A r1, A r2)
        => throw null;
    public static bool op_Equality(string r1, A r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS0111: Type 'A' already defines a member called 'op_Equality' with the same parameter types
                //     public static bool op_Equality(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Equality").WithArguments("op_Equality", "A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void EqualityOperators_06()
        {
            var source =
@"
record A
{
    public static bool op_Inequality(A r1, A r2)
        => throw null;
    public static bool op_Inequality(A r1, string r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS0111: Type 'A' already defines a member called 'op_Inequality' with the same parameter types
                //     public static bool op_Inequality(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Inequality").WithArguments("op_Inequality", "A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void EqualityOperators_07()
        {
            var source =
@"
record A
{
    public static bool Equals(A other)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,8): error CS0736: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is static.
                // record A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(2, 8),
                // (4,24): error CS8872: 'A.Equals(A)' must allow overriding because the containing record is not sealed.
                //     public static bool Equals(A other)
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 24),
                // (4,24): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public static bool Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void EqualityOperators_08()
        {
            var source =
@"
record A
{
    public virtual string Equals(A other)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,8): error CS0738: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement 'IEquatable<A>.Equals(A)' because it does not have the matching return type of 'bool'.
                // record A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)", "bool").WithLocation(2, 8),
                // (4,27): error CS8874: Record member 'A.Equals(A)' must return 'bool'.
                //     public virtual string Equals(A other)
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "Equals").WithArguments("A.Equals(A)", "bool").WithLocation(4, 27),
                // (4,27): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public virtual string Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 27)
                );
        }

        [Theory]
        [CombinatorialData]
        public void EqualityOperators_09(bool useImageReference)
        {
            var source1 =
@"
public record A(int X) 
{
}
";
            var comp1 = CreateCompilation(source1);

            var source2 =
@"
class Program
{
    static void Main()
    {
        Test(null, null);
        Test(null, new A(0));
        Test(new A(1), new A(1));
        Test(new A(2), new A(3));
    }

    static void Test(A a1, A a2)
    {
        System.Console.WriteLine(""{0} {1} {2} {3}"", a1 == a2, a2 == a1, a1 != a2, a2 != a1);
    }
}
";
            CompileAndVerify(source2, references: new[] { useImageReference ? comp1.EmitToImageReference() : comp1.ToMetadataReference() }, expectedOutput:
@"
True True False False
False False True True
True True False False
False False True True
").VerifyDiagnostics();
        }

        [WorkItem(44692, "https://github.com/dotnet/roslyn/issues/44692")]
        [Fact]
        public void DuplicateProperty_01()
        {
            var src =
@"record C(object Q)
{
    public object P { get; }
    public object P { get; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0102: The type 'C' already contains a definition for 'P'
                //     public object P { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 19));

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.Q { get; init; }",
                "System.Object C.P { get; }",
                "System.Object C.P { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [WorkItem(44692, "https://github.com/dotnet/roslyn/issues/44692")]
        [Fact]
        public void DuplicateProperty_02()
        {
            var src =
@"record C(object P, object Q)
{
    public object P { get; }
    public int P { get; }
    public int Q { get; }
    public object Q { get; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (1,27): error CS8866: Record member 'C.Q' must be a readable instance property of type 'object' to match positional parameter 'Q'.
                // record C(object P, object Q)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Q").WithArguments("C.Q", "object", "Q").WithLocation(1, 27),
                // (4,16): error CS0102: The type 'C' already contains a definition for 'P'
                //     public int P { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 16),
                // (6,19): error CS0102: The type 'C' already contains a definition for 'Q'
                //     public object Q { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Q").WithArguments("C", "Q").WithLocation(6, 19));

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.P { get; }",
                "System.Int32 C.P { get; }",
                "System.Int32 C.Q { get; }",
                "System.Object C.Q { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void DuplicateProperty_03()
        {
            var src =
@"record A
{
    public object P { get; }
    public object P { get; }
    public object Q { get; }
    public int Q { get; }
}
record B(object Q) : A
{
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0102: The type 'A' already contains a definition for 'P'
                //     public object P { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("A", "P").WithLocation(4, 19),
                // (6,16): error CS0102: The type 'A' already contains a definition for 'Q'
                //     public int Q { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Q").WithArguments("A", "Q").WithLocation(6, 16));

            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void NominalRecordWith()
        {
            var src = @"
using System;
record C
{
    public int X { get; init; }
    public string Y;
    public int Z { get; set; }

    public static void Main()
    {
        var c = new C() { X = 1, Y = ""2"", Z = 3 };
        var c2 = new C() { X = 1, Y = ""2"", Z = 3 };
        Console.WriteLine(c.Equals(c2));
        var c3 = c2 with { X = 3, Y = ""2"", Z = 1 };
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c3.Equals(c2));
        Console.WriteLine(c2.X + "" "" + c2.Y + "" "" + c2.Z);
    }
}";
            CompileAndVerify(src, expectedOutput: @"
True
True
False
1 2 3").VerifyDiagnostics();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithExprReference(bool emitRef)
        {
            var src = @"
public record C
{
    public int X { get; init; }
}
public record D(int Y) : C;";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            var src2 = @"
using System;
class E
{
    public static void Main()
    {
        var c = new C() { X = 1 };
        var c2 = c with { X = 2 };
        Console.WriteLine(c.X);
        Console.WriteLine(c2.X);

        var d = new D(2) { X = 1 };
        var d2 = d with { X = 2, Y = 3 };
        Console.WriteLine(d.X + "" "" + d.Y);
        Console.WriteLine(d2.X + "" ""  + d2.Y);

        C c3 = d;
        C c4 = d2;
        c3 = c3 with { X = 3 };
        c4 = c4 with { X = 4 };

        d = (D)c3;
        d2 = (D)c4;
        Console.WriteLine(d.X + "" "" + d.Y);
        Console.WriteLine(d2.X + "" ""  + d2.Y);
    }
}";
            var verifier = CompileAndVerify(src2,
                references: new[] { emitRef ? comp.EmitToImageReference() : comp.ToMetadataReference() },
                expectedOutput: @"
1
2
1 2
2 3
3 2
4 3").VerifyDiagnostics();

            verifier.VerifyIL("E.Main", @"
{
  // Code size      318 (0x13e)
  .maxstack  3
  .locals init (C V_0, //c
                D V_1, //d
                D V_2, //d2
                C V_3, //c3
                C V_4, //c4
                int V_5)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""void C.X.init""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0013:  dup
  IL_0014:  ldc.i4.2
  IL_0015:  callvirt   ""void C.X.init""
  IL_001a:  ldloc.0
  IL_001b:  callvirt   ""int C.X.get""
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  callvirt   ""int C.X.get""
  IL_002a:  call       ""void System.Console.WriteLine(int)""
  IL_002f:  ldc.i4.2
  IL_0030:  newobj     ""D..ctor(int)""
  IL_0035:  dup
  IL_0036:  ldc.i4.1
  IL_0037:  callvirt   ""void C.X.init""
  IL_003c:  stloc.1
  IL_003d:  ldloc.1
  IL_003e:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0043:  castclass  ""D""
  IL_0048:  dup
  IL_0049:  ldc.i4.2
  IL_004a:  callvirt   ""void C.X.init""
  IL_004f:  dup
  IL_0050:  ldc.i4.3
  IL_0051:  callvirt   ""void D.Y.init""
  IL_0056:  stloc.2
  IL_0057:  ldloc.1
  IL_0058:  callvirt   ""int C.X.get""
  IL_005d:  stloc.s    V_5
  IL_005f:  ldloca.s   V_5
  IL_0061:  call       ""string int.ToString()""
  IL_0066:  ldstr      "" ""
  IL_006b:  ldloc.1
  IL_006c:  callvirt   ""int D.Y.get""
  IL_0071:  stloc.s    V_5
  IL_0073:  ldloca.s   V_5
  IL_0075:  call       ""string int.ToString()""
  IL_007a:  call       ""string string.Concat(string, string, string)""
  IL_007f:  call       ""void System.Console.WriteLine(string)""
  IL_0084:  ldloc.2
  IL_0085:  callvirt   ""int C.X.get""
  IL_008a:  stloc.s    V_5
  IL_008c:  ldloca.s   V_5
  IL_008e:  call       ""string int.ToString()""
  IL_0093:  ldstr      "" ""
  IL_0098:  ldloc.2
  IL_0099:  callvirt   ""int D.Y.get""
  IL_009e:  stloc.s    V_5
  IL_00a0:  ldloca.s   V_5
  IL_00a2:  call       ""string int.ToString()""
  IL_00a7:  call       ""string string.Concat(string, string, string)""
  IL_00ac:  call       ""void System.Console.WriteLine(string)""
  IL_00b1:  ldloc.1
  IL_00b2:  stloc.3
  IL_00b3:  ldloc.2
  IL_00b4:  stloc.s    V_4
  IL_00b6:  ldloc.3
  IL_00b7:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_00bc:  dup
  IL_00bd:  ldc.i4.3
  IL_00be:  callvirt   ""void C.X.init""
  IL_00c3:  stloc.3
  IL_00c4:  ldloc.s    V_4
  IL_00c6:  callvirt   ""C C." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_00cb:  dup
  IL_00cc:  ldc.i4.4
  IL_00cd:  callvirt   ""void C.X.init""
  IL_00d2:  stloc.s    V_4
  IL_00d4:  ldloc.3
  IL_00d5:  castclass  ""D""
  IL_00da:  stloc.1
  IL_00db:  ldloc.s    V_4
  IL_00dd:  castclass  ""D""
  IL_00e2:  stloc.2
  IL_00e3:  ldloc.1
  IL_00e4:  callvirt   ""int C.X.get""
  IL_00e9:  stloc.s    V_5
  IL_00eb:  ldloca.s   V_5
  IL_00ed:  call       ""string int.ToString()""
  IL_00f2:  ldstr      "" ""
  IL_00f7:  ldloc.1
  IL_00f8:  callvirt   ""int D.Y.get""
  IL_00fd:  stloc.s    V_5
  IL_00ff:  ldloca.s   V_5
  IL_0101:  call       ""string int.ToString()""
  IL_0106:  call       ""string string.Concat(string, string, string)""
  IL_010b:  call       ""void System.Console.WriteLine(string)""
  IL_0110:  ldloc.2
  IL_0111:  callvirt   ""int C.X.get""
  IL_0116:  stloc.s    V_5
  IL_0118:  ldloca.s   V_5
  IL_011a:  call       ""string int.ToString()""
  IL_011f:  ldstr      "" ""
  IL_0124:  ldloc.2
  IL_0125:  callvirt   ""int D.Y.get""
  IL_012a:  stloc.s    V_5
  IL_012c:  ldloca.s   V_5
  IL_012e:  call       ""string int.ToString()""
  IL_0133:  call       ""string string.Concat(string, string, string)""
  IL_0138:  call       ""void System.Console.WriteLine(string)""
  IL_013d:  ret
}");
        }

        private static ImmutableArray<Symbol> GetProperties(CSharpCompilation comp, string typeName)
        {
            return comp.GetMember<NamedTypeSymbol>(typeName).GetMembers().WhereAsArray(m => m.Kind == SymbolKind.Property);
        }

        [Fact]
        public void BaseArguments_01()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

record C(int X, int Y = 123) : Base(X, Y)
{
    int Z = 123;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.Z);
    }

    C(int X, int Y, int Z = 124) : this(X, Y) {}
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2
123").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor(int, int)", @"

{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   123
  IL_0011:  stfld      ""int C.Z""
  IL_0016:  ldarg.0
  IL_0017:  ldarg.1
  IL_0018:  ldarg.2
  IL_0019:  call       ""Base..ctor(int, int)""
  IL_001e:  ret
}
");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, [System.Int32 Y = 123])", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, symbol.ContainingSymbol.DeclaredAccessibility);
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Single();
                Assert.Equal("Base(X, Y)", baseWithargs.ToString());
                Assert.Equal("Base", model.GetTypeInfo(baseWithargs.Type).Type.ToTestDisplayString());
                Assert.Equal(TypeInfo.None, model.GetTypeInfo(baseWithargs));
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", model.GetSymbolInfo((SyntaxNode)baseWithargs).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", model.GetSymbolInfo(baseWithargs).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", CSharpExtensions.GetSymbolInfo(model, baseWithargs).Symbol.ToTestDisplayString());

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                Assert.Empty(model.GetMemberGroup(baseWithargs));

                model = comp.GetSemanticModel(tree);
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", model.GetSymbolInfo((SyntaxNode)baseWithargs).Symbol.ToTestDisplayString());
                model = comp.GetSemanticModel(tree);
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", model.GetSymbolInfo(baseWithargs).Symbol.ToTestDisplayString());
                model = comp.GetSemanticModel(tree);
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", CSharpExtensions.GetSymbolInfo(model, baseWithargs).Symbol.ToTestDisplayString());

                model = comp.GetSemanticModel(tree);
                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                model = comp.GetSemanticModel(tree);
                Assert.Empty(model.GetMemberGroup(baseWithargs));
                model = comp.GetSemanticModel(tree);

#nullable disable
                var operation = model.GetOperation(baseWithargs);

                VerifyOperationTree(comp, operation,
@"
IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
        IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
        IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

                Assert.Null(model.GetOperation(baseWithargs.Type));
                Assert.Null(model.GetOperation(baseWithargs.Parent));
                Assert.Same(operation.Parent.Parent, model.GetOperation(baseWithargs.Parent.Parent));
                Assert.Equal(SyntaxKind.RecordDeclaration, baseWithargs.Parent.Parent.Kind());

                VerifyOperationTree(comp, operation.Parent.Parent,
@"
IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'record C(in ... }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Base(X, Y)')
      Expression: 
        IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'record C(in ... }')
  ExpressionBody: 
    null
");

                Assert.Null(operation.Parent.Parent.Parent);
                ControlFlowGraphVerifier.VerifyGraph(comp,
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Base(X, Y)')
          Expression: 
            IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                    IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                    IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
", ControlFlowGraph.Create((IConstructorBodyOperation)operation.Parent.Parent));

                var equalsValue = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().First();

                Assert.Equal("= 123", equalsValue.ToString());
                model.VerifyOperationTree(equalsValue,
@"
IParameterInitializerOperation (Parameter: [System.Int32 Y = 123]) (OperationKind.ParameterInitializer, Type: null) (Syntax: '= 123')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 123) (Syntax: '123')
");
#nullable enable
            }
            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();
                Assert.Equal(": this(X, Y)", baseWithargs.ToString());
                Assert.Equal("C..ctor(System.Int32 X, [System.Int32 Y = 123])", model.GetSymbolInfo((SyntaxNode)baseWithargs).Symbol.ToTestDisplayString());
                Assert.Equal("C..ctor(System.Int32 X, [System.Int32 Y = 123])", model.GetSymbolInfo(baseWithargs).Symbol.ToTestDisplayString());
                Assert.Equal("C..ctor(System.Int32 X, [System.Int32 Y = 123])", CSharpExtensions.GetSymbolInfo(model, baseWithargs).Symbol.ToTestDisplayString());

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(model.GetMemberGroup(baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(CSharpExtensions.GetMemberGroup(model, baseWithargs).Select(m => m.ToTestDisplayString()));

                model.VerifyOperationTree(baseWithargs,
@"
IInvocationOperation ( C..ctor(System.Int32 X, [System.Int32 Y = 123])) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this(X, Y)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this(X, Y)')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
        IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
        IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

                var equalsValue = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Last();

                Assert.Equal("= 124", equalsValue.ToString());
                model.VerifyOperationTree(equalsValue,
@"
IParameterInitializerOperation (Parameter: [System.Int32 Z = 124]) (OperationKind.ParameterInitializer, Type: null) (Syntax: '= 124')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 124) (Syntax: '124')
");

                model.VerifyOperationTree(baseWithargs.Parent,
@"
IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'C(int X, in ... is(X, Y) {}')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this(X, Y)')
      Expression: 
        IInvocationOperation ( C..ctor(System.Int32 X, [System.Int32 Y = 123])) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this(X, Y)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this(X, Y)')
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
  ExpressionBody: 
    null
");
            }
        }

        [Fact]
        public void BaseArguments_02()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

record C(int X) : Base(Test(X, out var y), y)
{
    public static void Main()
    {
        var c = new C(1);
    }

    private static int Test(int x, out int y)
    {
        y = 2;
        return x;
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2").VerifyDiagnostics();

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var yDecl = OutVarTests.GetOutVarDeclaration(tree, "y");
            var yRef = OutVarTests.GetReferences(tree, "y").ToArray();
            Assert.Equal(2, yRef.Length);
            OutVarTests.VerifyModelForOutVar(model, yDecl, yRef[0]);
            OutVarTests.VerifyNotAnOutLocal(model, yRef[1]);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Test(X, out var y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            var y = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").First();
            Assert.Equal("y", y.Parent!.ToString());
            Assert.Equal("(Test(X, out var y), y)", y.Parent!.Parent!.ToString());
            Assert.Equal("Base(Test(X, out var y), y)", y.Parent!.Parent!.Parent!.ToString());

            symbol = model.GetSymbolInfo(y).Symbol;
            Assert.Equal(SymbolKind.Local, symbol!.Kind);
            Assert.Equal("System.Int32 y", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "y"));
            Assert.Contains("y", model.LookupNames(x.SpanStart));

            var test = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").First();
            Assert.Equal("(Test(X, out var y), y)", test.Parent!.Parent!.Parent!.ToString());

            symbol = model.GetSymbolInfo(test).Symbol;
            Assert.Equal(SymbolKind.Method, symbol!.Kind);
            Assert.Equal("System.Int32 C.Test(System.Int32 x, out System.Int32 y)", symbol.ToTestDisplayString());
            Assert.Equal("C", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "Test"));
            Assert.Contains("Test", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_03()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

record C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,16): error CS8861: Unexpected argument list.
                // record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(13, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_04()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial record C(int X, int Y)
{
}

partial record C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (17,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(17, 24)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));

            var recordDeclarations = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Skip(1).ToArray();

            Assert.Equal("C", recordDeclarations[0].Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclarations[0]));
            Assert.Equal("C", recordDeclarations[1].Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclarations[1]));
        }

        [Fact]
        public void BaseArguments_05()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial record C : Base(X, Y)
{
}

partial record C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(13, 24),
                // (17,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(17, 24)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            foreach (var x in xs)
            {
                Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

                var symbolInfo = model.GetSymbolInfo(x);
                Assert.Null(symbolInfo.Symbol);
                Assert.Empty(symbolInfo.CandidateSymbols);
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
                Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
                Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
            }

            var recordDeclarations = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Skip(1).ToArray();

            Assert.Equal("C", recordDeclarations[0].Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclarations[0]));

            Assert.Equal("C", recordDeclarations[1].Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclarations[1]));
        }

        [Fact]
        public void BaseArguments_06()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial record C(int X, int Y) : Base(X, Y)
{
}

partial record C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (17,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(17, 24)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            var x = xs[0];
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            x = xs[1];
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));

            var recordDeclarations = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Skip(1).ToArray();

            Assert.Equal("C", recordDeclarations[0].Identifier.ValueText);
            model.VerifyOperationTree(recordDeclarations[0],
@"
IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'partial rec ... }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Base(X, Y)')
      Expression: 
        IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'partial rec ... }')
  ExpressionBody: 
    null
");
            Assert.Equal("C", recordDeclarations[1].Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclarations[1]));
        }

        [Fact]
        public void BaseArguments_07()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial record C : Base(X, Y)
{
}

partial record C(int X, int Y) : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(13, 24)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            var x = xs[1];
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            x = xs[0];
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));

            var recordDeclarations = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Skip(1).ToArray();

            Assert.Equal("C", recordDeclarations[0].Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclarations[0]));

            Assert.Equal("C", recordDeclarations[1].Identifier.ValueText);
            model.VerifyOperationTree(recordDeclarations[1],
@"
IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'partial rec ... }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Base(X, Y)')
      Expression: 
        IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'partial rec ... }')
  ExpressionBody: 
    null
");
        }

        [Fact]
        public void BaseArguments_08()
        {
            var src = @"
record Base
{
    public Base(int Y)
    {
    }

    public Base() {}
}

record C(int X) : Base(Y)
{
    public int Y = 0;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,24): error CS0120: An object reference is required for the non-static field, method, or property 'C.Y'
                // record C(int X) : Base(Y)
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Y").WithArguments("C.Y").WithLocation(11, 24)
                );
        }

        [Fact]
        public void BaseArguments_09()
        {
            var src = @"
record Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

record C(int X) : Base(this.X)
{
    public int Y = 0;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,24): error CS0027: Keyword 'this' is not available in the current context
                // record C(int X) : Base(this.X)
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(11, 24)
                );
        }

        [Fact]
        public void BaseArguments_10()
        {
            var src = @"
record Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

record C(dynamic X) : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,27): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                // record C(dynamic X) : Base(X)
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(X)").WithLocation(11, 27)
                );
        }

        [Fact]
        public void BaseArguments_11()
        {
            var src = @"
record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

record C(int X) : Base(Test(X, out var y), y)
{
    int Z = y;

    private static int Test(int x, out int y)
    {
        y = 2;
        return x;
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,13): error CS0103: The name 'y' does not exist in the current context
                //     int Z = y;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(13, 13)
                );
        }

        [Fact]
        public void BaseArguments_12()
        {
            var src = @"
using System;

class Base
{
    public Base(int X)
    {
    }
}

class C : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (11,7): error CS7036: There is no argument given that corresponds to the required formal parameter 'X' of 'Base.Base(int)'
                // class C : Base(X)
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("X", "Base.Base(int)").WithLocation(11, 7),
                // (11,15): error CS8861: Unexpected argument list.
                // class C : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(11, 15)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_13()
        {
            var src = @"
using System;

interface Base
{
}

struct C : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (8,16): error CS8861: Unexpected argument list.
                // struct C : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(8, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_14()
        {
            var src = @"
using System;

interface Base
{
}

interface C : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (8,19): error CS8861: Unexpected argument list.
                // interface C : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(8, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_15()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

partial record C
{
}

partial record C(int X, int Y) : Base(X, Y)
{
    int Z = 123;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.Z);
    }
}

partial record C
{
}
";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2
123").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor(int, int)", @"

{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   123
  IL_0011:  stfld      ""int C.Z""
  IL_0016:  ldarg.0
  IL_0017:  ldarg.1
  IL_0018:  ldarg.2
  IL_0019:  call       ""Base..ctor(int, int)""
  IL_001e:  ret
}
");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_16()
        {
            var src = @"
using System;

record Base
{
    public Base(Func<int> X)
    {
        Console.WriteLine(X());
    }

    public Base() {}
}

record C(int X) : Base(() => X)
{
    public static void Main()
    {
        var c = new C(1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"1").VerifyDiagnostics();
        }

        [Fact]
        public void BaseArguments_17()
        {
            var src = @"
record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

record C(int X, int y)
    : Base(Test(X, out var y),
           Test(X, out var z))
{
    int Z = z;

    private static int Test(int x, out int y)
    {
        y = 2;
        return x;
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,28): error CS0136: A local or parameter named 'y' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     : Base(Test(X, out var y),
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y").WithArguments("y").WithLocation(12, 28),
                // (15,13): error CS0103: The name 'z' does not exist in the current context
                //     int Z = z;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(15, 13)
                );
        }

        [Fact]
        public void BaseArguments_18()
        {
            var src = @"
record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

record C(int X, int y)
    : Base(Test(X + 1, out var z),
           Test(X + 2, out var z))
{
    private static int Test(int x, out int y)
    {
        y = 2;
        return x;
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,32): error CS0128: A local variable or function named 'z' is already defined in this scope
                //            Test(X + 2, out var z))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "z").WithArguments("z").WithLocation(13, 32)
                );
        }

        [Fact]
        public void BaseArguments_19()
        {
            var src = @"
record Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

record C(int X, int Y) : Base(GetInt(X, out var xx) + xx, Y), I
{
    C(int X, int Y, int Z) : this(X, Y, Z, 1) { return; }

    static int GetInt(int x1, out int x2)
    {
        throw null;
    }
}

interface I {}
";

            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (11,30): error CS1729: 'Base' does not contain a constructor that takes 2 arguments
                // record C(int X, int Y) : Base(GetInt(X, out var xx) + xx, Y)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(GetInt(X, out var xx) + xx, Y)").WithArguments("Base", "2").WithLocation(11, 30),
                // (13,30): error CS1729: 'C' does not contain a constructor that takes 4 arguments
                //     C(int X, int Y, int Z) : this(X, Y, Z, 1) {}
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "this").WithArguments("C", "4").WithLocation(13, 30)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            SymbolInfo symbolInfo;
            PrimaryConstructorBaseTypeSyntax speculativePrimaryInitializer;
            ConstructorInitializerSyntax speculativeBaseInitializer;

            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Single();
                Assert.Equal("Base(GetInt(X, out var xx) + xx, Y)", baseWithargs.ToString());
                Assert.Equal("Base", model.GetTypeInfo(baseWithargs.Type).Type.ToTestDisplayString());
                Assert.Equal(TypeInfo.None, model.GetTypeInfo(baseWithargs));
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                string[] candidates = new[] { "Base..ctor(Base original)", "Base..ctor(System.Int32 X)", "Base..ctor()" };
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                Assert.Empty(model.GetMemberGroup(baseWithargs));

                model = comp.GetSemanticModel(tree);
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                model = comp.GetSemanticModel(tree);
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                model = comp.GetSemanticModel(tree);
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));

                model = comp.GetSemanticModel(tree);
                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                model = comp.GetSemanticModel(tree);
                Assert.Empty(model.GetMemberGroup(baseWithargs));
                model = comp.GetSemanticModel(tree);

                SemanticModel speculativeModel;
                speculativePrimaryInitializer = baseWithargs.WithArgumentList(baseWithargs.ArgumentList.WithArguments(baseWithargs.ArgumentList.Arguments.RemoveAt(1)));

                speculativeBaseInitializer = SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, speculativePrimaryInitializer.ArgumentList);
                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativeBaseInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                Assert.False(model.TryGetSpeculativeSemanticModel(tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().SpanStart,
                                                                  speculativeBaseInitializer, out _));

                var otherBasePosition = ((BaseListSyntax)baseWithargs.Parent!).Types[1].SpanStart;
                Assert.False(model.TryGetSpeculativeSemanticModel(otherBasePosition, speculativePrimaryInitializer, out _));

                Assert.True(model.TryGetSpeculativeSemanticModel(baseWithargs.SpanStart, speculativePrimaryInitializer, out speculativeModel!));
                Assert.Equal("Base..ctor(System.Int32 X)", speculativeModel!.GetSymbolInfo((SyntaxNode)speculativePrimaryInitializer).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X)", speculativeModel.GetSymbolInfo(speculativePrimaryInitializer).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X)", CSharpExtensions.GetSymbolInfo(speculativeModel, speculativePrimaryInitializer).Symbol.ToTestDisplayString());

                Assert.True(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer, out speculativeModel!));

                var xxDecl = OutVarTests.GetOutVarDeclaration(speculativePrimaryInitializer.SyntaxTree, "xx");
                var xxRef = OutVarTests.GetReferences(speculativePrimaryInitializer.SyntaxTree, "xx").ToArray();
                Assert.Equal(1, xxRef.Length);
                OutVarTests.VerifyModelForOutVar(speculativeModel, xxDecl, xxRef);

                Assert.Equal("Base..ctor(System.Int32 X)", speculativeModel!.GetSymbolInfo((SyntaxNode)speculativePrimaryInitializer).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X)", speculativeModel.GetSymbolInfo(speculativePrimaryInitializer).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X)", CSharpExtensions.GetSymbolInfo(speculativeModel, speculativePrimaryInitializer).Symbol.ToTestDisplayString());

                Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (PrimaryConstructorBaseTypeSyntax)null!, out _));
                Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, baseWithargs, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(otherBasePosition, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, otherBasePosition, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.SpanStart, speculativePrimaryInitializer);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single().ArgumentList.OpenParenToken.SpanStart,
                                                                         (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
            }
            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();
                Assert.Equal(": this(X, Y, Z, 1)", baseWithargs.ToString());
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                string[] candidates = new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor(C original)", "C..ctor(System.Int32 X, System.Int32 Y, System.Int32 Z)" };
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(model.GetMemberGroup(baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(CSharpExtensions.GetMemberGroup(model, baseWithargs).Select(m => m.ToTestDisplayString()));

                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativeBaseInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
            }
        }

        [Fact]
        public void BaseArguments_20()
        {
            var src = @"
class Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

class C : Base(GetInt(X, out var xx) + xx, Y), I
{
    C(int X, int Y, int Z) : base(X, Y, Z, 1) { return; }

    static int GetInt(int x1, out int x2)
    {
        throw null;
    }
}

interface I {}
";

            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (11,15): error CS8861: Unexpected argument list.
                // class C : Base(GetInt(X, out var xx) + xx, Y), I
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(11, 15),
                // (13,30): error CS1729: 'Base' does not contain a constructor that takes 4 arguments
                //     C(int X, int Y, int Z) : base(X, Y, Z, 1) { return; }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "base").WithArguments("Base", "4").WithLocation(13, 30)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            SymbolInfo symbolInfo;
            PrimaryConstructorBaseTypeSyntax speculativePrimaryInitializer;
            ConstructorInitializerSyntax speculativeBaseInitializer;

            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Single();
                Assert.Equal("Base(GetInt(X, out var xx) + xx, Y)", baseWithargs.ToString());
                Assert.Equal("Base", model.GetTypeInfo(baseWithargs.Type).Type.ToTestDisplayString());
                Assert.Equal(TypeInfo.None, model.GetTypeInfo(baseWithargs));
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Equal(SymbolInfo.None, symbolInfo);
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Equal(SymbolInfo.None, symbolInfo);
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                Assert.Empty(model.GetMemberGroup(baseWithargs));

                speculativePrimaryInitializer = baseWithargs.WithArgumentList(baseWithargs.ArgumentList.WithArguments(baseWithargs.ArgumentList.Arguments.RemoveAt(1)));

                speculativeBaseInitializer = SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, speculativePrimaryInitializer.ArgumentList);
                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativeBaseInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                Assert.False(model.TryGetSpeculativeSemanticModel(tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().SpanStart,
                                                                  speculativeBaseInitializer, out _));

                var otherBasePosition = ((BaseListSyntax)baseWithargs.Parent!).Types[1].SpanStart;
                Assert.False(model.TryGetSpeculativeSemanticModel(otherBasePosition, speculativePrimaryInitializer, out _));

                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.SpanStart, speculativePrimaryInitializer, out _));
                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer, out _));

                Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (PrimaryConstructorBaseTypeSyntax)null!, out _));
                Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, baseWithargs, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(otherBasePosition, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, otherBasePosition, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.SpanStart, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single().ArgumentList.OpenParenToken.SpanStart,
                                                                         (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
            }
            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();
                Assert.Equal(": base(X, Y, Z, 1)", baseWithargs.ToString());
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                string[] candidates = new[] { "Base..ctor(System.Int32 X)", "Base..ctor()" };
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(model.GetMemberGroup(baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(CSharpExtensions.GetMemberGroup(model, baseWithargs).Select(m => m.ToTestDisplayString()));

                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativeBaseInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
            }
        }

        [Fact(Skip = "record struct")]
        public void Equality_01()
        {
            var source =
@"using static System.Console;
data struct S;
class Program
{
    static void Main()
    {
        var x = new S();
        var y = new S();
        WriteLine(x.Equals(y));
        WriteLine(((object)x).Equals(y));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True").VerifyDiagnostics();

            verifier.VerifyIL("S.Equals(in S)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Type S.EqualityContract.get""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""S""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""System.Type S.EqualityContract.get""
  IL_0014:  ceq
  IL_0016:  ret
}");
            verifier.VerifyIL("S.Equals(object)",
@"{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""S""
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  ldc.i4.0
  IL_0009:  ret
  IL_000a:  ldarg.0
  IL_000b:  ldarg.1
  IL_000c:  unbox.any  ""S""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool S.Equals(in S)""
  IL_0019:  ret
}");
        }

        [Fact]
        public void Equality_02()
        {
            var source =
@"using static System.Console;
record C;
class Program
{
    static void Main()
    {
        var x = new C();
        var y = new C();
        WriteLine(x.Equals(y) && x.GetHashCode() == y.GetHashCode());
        WriteLine(((object)x).Equals(y));
        WriteLine(((System.IEquatable<C>)x).Equals(y));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var ordinaryMethods = comp.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).ToArray();
            Assert.Equal(6, ordinaryMethods.Length);

            foreach (var m in ordinaryMethods)
            {
                Assert.True(m.IsImplicitlyDeclared);
            }

            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True
True").VerifyDiagnostics();

            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0015
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
            verifier.VerifyIL("C.Equals(object)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.GetHashCode()",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<System.Type> System.Collections.Generic.EqualityComparer<System.Type>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<System.Type>.GetHashCode(System.Type)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void Equality_03()
        {
            var source =
@"using static System.Console;
record C
{
    private static int _nextId = 0;
    private int _id;
    public C() { _id = _nextId++; }
}
class Program
{
    static void Main()
    {
        var x = new C();
        var y = new C();
        WriteLine(x.Equals(x));
        WriteLine(x.Equals(y));
        WriteLine(y.Equals(y));
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"True
False
True").VerifyDiagnostics();

            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_002d
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  brfalse.s  IL_002d
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""int C._id""
  IL_0021:  ldarg.1
  IL_0022:  ldfld      ""int C._id""
  IL_0027:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_002c:  ret
  IL_002d:  ldc.i4.0
  IL_002e:  ret
}");
            verifier.VerifyIL("C.GetHashCode()",
@"{
  // Code size       40 (0x28)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<System.Type> System.Collections.Generic.EqualityComparer<System.Type>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<System.Type>.GetHashCode(System.Type)""
  IL_0010:  ldc.i4     0xa5555529
  IL_0015:  mul
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""int C._id""
  IL_0021:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_0026:  add
  IL_0027:  ret
}");

            var clone = ((CSharpCompilation)verifier.Compilation).GetMember<MethodSymbol>("C." + WellKnownMemberNames.CloneMethodName);
            Assert.Equal(Accessibility.Public, clone.DeclaredAccessibility);
            Assert.False(clone.IsOverride);
            Assert.True(clone.IsVirtual);
            Assert.False(clone.IsAbstract);
            Assert.False(clone.IsSealed);
            Assert.True(clone.IsImplicitlyDeclared);
        }

        [Fact]
        public void Equality_04()
        {
            var source =
@"using static System.Console;
record A;
record B1(int P) : A
{
    internal B1() : this(0) { } // Use record base call syntax instead
    internal int P { get; set; } // Use record base call syntax instead
}
record B2(int P) : A
{
    internal B2() : this(0) { } // Use record base call syntax instead
    internal int P { get; set; } // Use record base call syntax instead
}
class Program
{
    static B1 NewB1(int p) => new B1 { P = p }; // Use record base call syntax instead
    static B2 NewB2(int p) => new B2 { P = p }; // Use record base call syntax instead
    static void Main()
    {
        WriteLine(new A().Equals(NewB1(1)));
        WriteLine(NewB1(1).Equals(new A()));
        WriteLine(NewB1(1).Equals(NewB2(1)));
        WriteLine(new A().Equals((A)NewB2(1)));
        WriteLine(((A)NewB2(1)).Equals(new A()));
        WriteLine(((A)NewB2(1)).Equals(NewB2(1)) && ((A)NewB2(1)).GetHashCode() == NewB2(1).GetHashCode());
        WriteLine(NewB2(1).Equals((A)NewB2(1)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"False
False
False
False
False
True
True").VerifyDiagnostics();

            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0015
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
            verifier.VerifyIL("B1.Equals(B1)",
@"{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int B1.<P>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""int B1.<P>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_001f:  ret
  IL_0020:  ldc.i4.0
  IL_0021:  ret
}");
            verifier.VerifyIL("B1.GetHashCode()",
@"{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""int A.GetHashCode()""
  IL_0006:  ldc.i4     0xa5555529
  IL_000b:  mul
  IL_000c:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""int B1.<P>k__BackingField""
  IL_0017:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_001c:  add
  IL_001d:  ret
}");
        }

        [Fact]
        public void Equality_05()
        {
            var source =
@"using static System.Console;
record A(int P)
{
    internal A() : this(0) { } // Use record base call syntax instead
    internal int P { get; set; } // Use record base call syntax instead
}
record B1(int P) : A
{
    internal B1() : this(0) { } // Use record base call syntax instead
}
record B2(int P) : A
{
    internal B2() : this(0) { } // Use record base call syntax instead
}
class Program
{
    static A NewA(int p) => new A { P = p }; // Use record base call syntax instead
    static B1 NewB1(int p) => new B1 { P = p }; // Use record base call syntax instead
    static B2 NewB2(int p) => new B2 { P = p }; // Use record base call syntax instead
    static void Main()
    {
        WriteLine(NewA(1).Equals(NewA(2)));
        WriteLine(NewA(1).Equals(NewA(1)) && NewA(1).GetHashCode() == NewA(1).GetHashCode());
        WriteLine(NewA(1).Equals(NewB1(1)));
        WriteLine(NewB1(1).Equals(NewA(1)));
        WriteLine(NewB1(1).Equals(NewB2(1)));
        WriteLine(NewA(1).Equals((A)NewB2(1)));
        WriteLine(((A)NewB2(1)).Equals(NewA(1)));
        WriteLine(((A)NewB2(1)).Equals(NewB2(1)));
        WriteLine(NewB2(1).Equals((A)NewB2(1)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"False
True
False
False
False
False
False
True
True").VerifyDiagnostics();

            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_002d
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  brfalse.s  IL_002d
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""int A.<P>k__BackingField""
  IL_0021:  ldarg.1
  IL_0022:  ldfld      ""int A.<P>k__BackingField""
  IL_0027:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_002c:  ret
  IL_002d:  ldc.i4.0
  IL_002e:  ret
}");
            verifier.VerifyIL("B1.Equals(B1)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  ret
}");
            verifier.VerifyIL("B1.GetHashCode()",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""int A.GetHashCode()""
  IL_0006:  ret
}");
        }

        [Fact]
        public void Equality_06()
        {
            var source =
@"
using System;
using static System.Console;
record A;
record B : A
{
    protected override Type EqualityContract => typeof(A);
    public virtual bool Equals(B b) => base.Equals((A)b);
}
record C : B;
class Program
{
    static void Main()
    {
        WriteLine(new A().Equals(new A()));
        WriteLine(new A().Equals(new B()));
        WriteLine(new A().Equals(new C()));
        WriteLine(new B().Equals(new A()));
        WriteLine(new B().Equals(new B()));
        WriteLine(new B().Equals(new C()));
        WriteLine(new C().Equals(new A()));
        WriteLine(new C().Equals(new B()));
        WriteLine(new C().Equals(new C()));
        WriteLine(((A)new C()).Equals(new A()));
        WriteLine(((A)new C()).Equals(new B()));
        WriteLine(((A)new C()).Equals(new C()));
        WriteLine(new C().Equals((A)new C()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var recordDeclaration = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().First();
            Assert.Null(model.GetOperation(recordDeclaration));

            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True
False
False
True
False
False
False
True
False
False
True
True").VerifyDiagnostics(
    // (8,25): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(B b) => base.Equals((A)b);
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(8, 25)
);

            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0015
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void Equality_07()
        {
            var source =
@"using System;
using static System.Console;
record A;
record B : A;
record C : B;
class Program
{
    static void Main()
    {
        WriteLine(new A().Equals(new A()));
        WriteLine(new A().Equals(new B()));
        WriteLine(new A().Equals(new C()));
        WriteLine(new B().Equals(new A()));
        WriteLine(new B().Equals(new B()));
        WriteLine(new B().Equals(new C()));
        WriteLine(new C().Equals(new A()));
        WriteLine(new C().Equals(new B()));
        WriteLine(new C().Equals(new C()));
        WriteLine(((A)new B()).Equals(new A()));
        WriteLine(((A)new B()).Equals(new B()));
        WriteLine(((A)new B()).Equals(new C()));
        WriteLine(((A)new C()).Equals(new A()));
        WriteLine(((A)new C()).Equals(new B()));
        WriteLine(((A)new C()).Equals(new C()));
        WriteLine(((B)new C()).Equals(new A()));
        WriteLine(((B)new C()).Equals(new B()));
        WriteLine(((B)new C()).Equals(new C()));
        WriteLine(new C().Equals((A)new C()));
        WriteLine(((IEquatable<A>)new B()).Equals(new A()));
        WriteLine(((IEquatable<A>)new B()).Equals(new B()));
        WriteLine(((IEquatable<A>)new B()).Equals(new C()));
        WriteLine(((IEquatable<A>)new C()).Equals(new A()));
        WriteLine(((IEquatable<A>)new C()).Equals(new B()));
        WriteLine(((IEquatable<A>)new C()).Equals(new C()));
        WriteLine(((IEquatable<B>)new C()).Equals(new A()));
        WriteLine(((IEquatable<B>)new C()).Equals(new B()));
        WriteLine(((IEquatable<B>)new C()).Equals(new C()));
        WriteLine(((IEquatable<C>)new C()).Equals(new C()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
False
False
False
True
False
False
False
True
False
True
False
False
False
True
False
False
True
True
False
True
False
False
False
True
False
False
True
True").VerifyDiagnostics();

            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0015
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
            verifier.VerifyIL("B.Equals(A)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  callvirt   ""bool object.Equals(object)""
  IL_0007:  ret
}");
            verifier.VerifyIL("C.Equals(B)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  callvirt   ""bool object.Equals(object)""
  IL_0007:  ret
}");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  ret
}");

            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("A.get_EqualityContract"), isOverride: false);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("B.get_EqualityContract"), isOverride: true);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("C.get_EqualityContract"), isOverride: true);

            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("A." + WellKnownMemberNames.CloneMethodName), isOverride: false);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("B." + WellKnownMemberNames.CloneMethodName), isOverride: true);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("C." + WellKnownMemberNames.CloneMethodName), isOverride: true);

            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("A.GetHashCode"), isOverride: true);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("B.GetHashCode"), isOverride: true);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("C.GetHashCode"), isOverride: true);

            VerifyVirtualMethods(comp.GetMembers("A.Equals"), ("System.Boolean A.Equals(A? other)", false), ("System.Boolean A.Equals(System.Object? obj)", true));
            VerifyVirtualMethods(comp.GetMembers("B.Equals"), ("System.Boolean B.Equals(B? other)", false), ("System.Boolean B.Equals(A? other)", true), ("System.Boolean B.Equals(System.Object? obj)", true));
            ImmutableArray<Symbol> cEquals = comp.GetMembers("C.Equals");
            VerifyVirtualMethods(cEquals, ("System.Boolean C.Equals(C? other)", false), ("System.Boolean C.Equals(B? other)", true), ("System.Boolean C.Equals(System.Object? obj)", true));

            var baseEquals = cEquals[1];
            Assert.Equal("System.Boolean C.Equals(B? other)", baseEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, baseEquals.DeclaredAccessibility);
            Assert.True(baseEquals.IsOverride);
            Assert.True(baseEquals.IsSealed);
            Assert.True(baseEquals.IsImplicitlyDeclared);
        }

        private static void VerifyVirtualMethod(MethodSymbol method, bool isOverride)
        {
            Assert.Equal(!isOverride, method.IsVirtual);
            Assert.Equal(isOverride, method.IsOverride);
            Assert.True(method.IsMetadataVirtual());
            Assert.Equal(!isOverride, method.IsMetadataNewSlot());
        }

        private static void VerifyVirtualMethods(ImmutableArray<Symbol> members, params (string displayString, bool isOverride)[] values)
        {
            Assert.Equal(members.Length, values.Length);
            for (int i = 0; i < members.Length; i++)
            {
                var method = (MethodSymbol)members[i];
                (string displayString, bool isOverride) = values[i];
                Assert.Equal(displayString, method.ToTestDisplayString(includeNonNullable: true));
                VerifyVirtualMethod(method, isOverride);
            }
        }

        [WorkItem(44895, "https://github.com/dotnet/roslyn/issues/44895")]
        [Fact]
        public void Equality_08()
        {
            var source =
@"
using System;
using static System.Console;
record A(int X)
{
    internal A() : this(0) { } // Use record base call syntax instead
    internal int X { get; set; } // Use record base call syntax instead
}
record B : A
{
    internal B() { } // Use record base call syntax instead
    internal B(int X, int Y) : base(X) { this.Y = Y; }
    internal int Y { get; set; }
    protected override Type EqualityContract => typeof(A);
    public virtual bool Equals(B b) => base.Equals((A)b);
}
record C(int X, int Y, int Z) : B
{
    internal C() : this(0, 0, 0) { } // Use record base call syntax instead
    internal int Z { get; set; } // Use record base call syntax instead
}
class Program
{
    static A NewA(int x) => new A { X = x }; // Use record base call syntax instead
    static B NewB(int x, int y) => new B { X = x, Y = y };
    static C NewC(int x, int y, int z) => new C { X = x, Y = y, Z = z };
    static void Main()
    {
        WriteLine(NewA(1).Equals(NewA(1)));
        WriteLine(NewA(1).Equals(NewB(1, 2)));
        WriteLine(NewA(1).Equals(NewC(1, 2, 3)));
        WriteLine(NewB(1, 2).Equals(NewA(1)));
        WriteLine(NewB(1, 2).Equals(NewB(1, 2)));
        WriteLine(NewB(1, 2).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewA(1)));
        WriteLine(NewC(1, 2, 3).Equals(NewB(1, 2)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(4, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 4, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 4, 4)));
        WriteLine(((A)NewB(1, 2)).Equals(NewA(1)));
        WriteLine(((A)NewB(1, 2)).Equals(NewB(1, 2)));
        WriteLine(((A)NewB(1, 2)).Equals(NewC(1, 2, 3)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewA(1)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewB(1, 2)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewC(1, 2, 3)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewA(1)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewB(1, 2)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewC(1, 2, 3)) && NewC(1, 2, 3).GetHashCode() == NewC(1, 2, 3).GetHashCode());
        WriteLine(NewC(1, 2, 3).Equals((A)NewC(1, 2, 3)));
    }
}";
            // https://github.com/dotnet/roslyn/issues/44895: C.Equals() should compare B.Y.
            var verifier = CompileAndVerify(source, expectedOutput:
@"True
True
False
False
True
False
False
False
True
False
True
False
False
True
False
False
False
True
False
False
True
True").VerifyDiagnostics(
    // (15,25): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(B b) => base.Equals((A)b);
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(15, 25)
);

            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_002d
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  brfalse.s  IL_002d
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""int A.<X>k__BackingField""
  IL_0021:  ldarg.1
  IL_0022:  ldfld      ""int A.<X>k__BackingField""
  IL_0027:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_002c:  ret
  IL_002d:  ldc.i4.0
  IL_002e:  ret
}");
            // https://github.com/dotnet/roslyn/issues/44895: C.Equals() should compare B.Y.
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int C.<Z>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""int C.<Z>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_001f:  ret
  IL_0020:  ldc.i4.0
  IL_0021:  ret
}");
            verifier.VerifyIL("C.GetHashCode()",
@"{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""int B.GetHashCode()""
  IL_0006:  ldc.i4     0xa5555529
  IL_000b:  mul
  IL_000c:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""int C.<Z>k__BackingField""
  IL_0017:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_001c:  add
  IL_001d:  ret
}");
        }

        [Fact]
        public void Equality_09()
        {
            var source =
@"using static System.Console;
record A(int X)
{
    internal A() : this(0) { } // Use record base call syntax instead
    internal int X { get; set; } // Use record base call syntax instead
}
record B(int X, int Y) : A
{
    internal B() : this(0, 0) { } // Use record base call syntax instead
    internal int Y { get; set; }
}
record C(int X, int Y, int Z) : B
{
    internal C() : this(0, 0, 0) { } // Use record base call syntax instead
    internal int Z { get; set; } // Use record base call syntax instead
}
class Program
{
    static A NewA(int x) => new A { X = x }; // Use record base call syntax instead
    static B NewB(int x, int y) => new B { X = x, Y = y };
    static C NewC(int x, int y, int z) => new C { X = x, Y = y, Z = z };
    static void Main()
    {
        WriteLine(NewA(1).Equals(NewA(1)));
        WriteLine(NewA(1).Equals(NewB(1, 2)));
        WriteLine(NewA(1).Equals(NewC(1, 2, 3)));
        WriteLine(NewB(1, 2).Equals(NewA(1)));
        WriteLine(NewB(1, 2).Equals(NewB(1, 2)));
        WriteLine(NewB(1, 2).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewA(1)));
        WriteLine(NewC(1, 2, 3).Equals(NewB(1, 2)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(4, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 4, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 4, 4)));
        WriteLine(((A)NewB(1, 2)).Equals(NewA(1)));
        WriteLine(((A)NewB(1, 2)).Equals(NewB(1, 2)));
        WriteLine(((A)NewB(1, 2)).Equals(NewC(1, 2, 3)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewA(1)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewB(1, 2)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewC(1, 2, 3)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewA(1)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewB(1, 2)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals((A)NewC(1, 2, 3)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var recordDeclaration = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().ElementAt(1);
            Assert.Equal("B", recordDeclaration.Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclaration));

            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
False
False
False
True
False
False
False
True
False
False
False
False
True
False
False
False
True
False
False
True
True").VerifyDiagnostics();

            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_002d
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  brfalse.s  IL_002d
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""int A.<X>k__BackingField""
  IL_0021:  ldarg.1
  IL_0022:  ldfld      ""int A.<X>k__BackingField""
  IL_0027:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_002c:  ret
  IL_002d:  ldc.i4.0
  IL_002e:  ret
}");
            verifier.VerifyIL("B.Equals(A)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  callvirt   ""bool object.Equals(object)""
  IL_0007:  ret
}");
            verifier.VerifyIL("B.Equals(B)",
@"{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int B.<Y>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""int B.<Y>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_001f:  ret
  IL_0020:  ldc.i4.0
  IL_0021:  ret
}");
            verifier.VerifyIL("C.Equals(B)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  callvirt   ""bool object.Equals(object)""
  IL_0007:  ret
}");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int C.<Z>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""int C.<Z>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_001f:  ret
  IL_0020:  ldc.i4.0
  IL_0021:  ret
}");
        }

        [Fact]
        public void Equality_11()
        {
            var source =
@"using System;
record A
{
    protected virtual Type EqualityContract => typeof(object);
}
record B1(object P) : A;
record B2(object P) : A;
class Program
{
    static void Main()
    {
        Console.WriteLine(new A().Equals(new A()));
        Console.WriteLine(new A().Equals(new B1((object)null)));
        Console.WriteLine(new B1((object)null).Equals(new A()));
        Console.WriteLine(new B1((object)null).Equals(new B1((object)null)));
        Console.WriteLine(new B1((object)null).Equals(new B2((object)null)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            // init-only is unverifiable
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"True
False
False
True
False").VerifyDiagnostics();
        }

        [Fact]
        public void Equality_12()
        {
            var source =
@"using System;
abstract record A
{
    public A() { }
    protected abstract Type EqualityContract { get; }
}
record B1(object P) : A;
record B2(object P) : A;
class Program
{
    static void Main()
    {
        var b1 = new B1((object)null);
        var b2 = new B2((object)null);
        Console.WriteLine(b1.Equals(b1));
        Console.WriteLine(b1.Equals(b2));
        Console.WriteLine(((A)b1).Equals(b1));
        Console.WriteLine(((A)b1).Equals(b2));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            // init-only is unverifiable
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"True
False
True
False").VerifyDiagnostics();
        }

        [Fact]
        public void Equality_13()
        {
            var source =
@"record A
{
    protected System.Type EqualityContract => typeof(A);
}
record B : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,27): error CS8872: 'A.EqualityContract' must allow overriding because the containing record is not sealed.
                //     protected System.Type EqualityContract => typeof(A);
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "EqualityContract").WithArguments("A.EqualityContract").WithLocation(3, 27),
                // (5,8): error CS0506: 'B.EqualityContract': cannot override inherited member 'A.EqualityContract' because it is not marked virtual, abstract, or override
                // record B : A;
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.EqualityContract", "A.EqualityContract").WithLocation(5, 8));
        }

        [Fact]
        public void Equality_14()
        {
            var source =
@"record A;
record B : A
{
    protected sealed override System.Type EqualityContract => typeof(B);
}
record C : B;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,43): error CS8872: 'B.EqualityContract' must allow overriding because the containing record is not sealed.
                //     protected sealed override System.Type EqualityContract => typeof(B);
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "EqualityContract").WithArguments("B.EqualityContract").WithLocation(4, 43),
                // (6,8): error CS0239: 'C.EqualityContract': cannot override inherited member 'B.EqualityContract' because it is sealed
                // record C : B;
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "C").WithArguments("C.EqualityContract", "B.EqualityContract").WithLocation(6, 8));

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Type B.EqualityContract.get",
                "System.String B.ToString()",
                "System.Boolean B." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean B.op_Inequality(B? r1, B? r2)",
                "System.Boolean B.op_Equality(B? r1, B? r2)",
                "System.Int32 B.GetHashCode()",
                "System.Boolean B.Equals(System.Object? obj)",
                "System.Boolean B.Equals(A? other)",
                "System.Boolean B.Equals(B? other)",
                "A B." + WellKnownMemberNames.CloneMethodName + "()",
                "B..ctor(B original)",
                "B..ctor()",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Equality_15()
        {
            var source =
@"using System;
record A;
record B1 : A
{
    public B1(int p) { P = p; }
    public int P { get; set; }
    protected override Type EqualityContract => typeof(A);
    public virtual bool Equals(B1 o) => base.Equals((A)o);
}
record B2 : A
{
    public B2(int p) { P = p; }
    public int P { get; set; }
    protected override Type EqualityContract => typeof(B2);
    public virtual bool Equals(B2 o) => base.Equals((A)o);
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new B1(1).Equals(new B1(2)));
        Console.WriteLine(new B1(1).Equals(new B2(1)));
        Console.WriteLine(new B2(1).Equals(new B2(2)));
    }
}";
            CompileAndVerify(source, expectedOutput:
@"True
False
True").VerifyDiagnostics(
    // (8,25): warning CS8851: 'B1' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(B1 o) => base.Equals((A)o);
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B1").WithLocation(8, 25),
    // (15,25): warning CS8851: 'B2' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(B2 o) => base.Equals((A)o);
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B2").WithLocation(15, 25)
);
        }

        [Fact]
        public void Equality_16()
        {
            var source =
@"using System;
record A;
record B1 : A
{
    public B1(int p) { P = p; }
    public int P { get; set; }
    protected override Type EqualityContract => typeof(string);
    public virtual bool Equals(B1 b) => base.Equals((A)b);
}
record B2 : A
{
    public B2(int p) { P = p; }
    public int P { get; set; }
    protected override Type EqualityContract => typeof(string);
    public virtual bool Equals(B2 b) => base.Equals((A)b);
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new B1(1).Equals(new B1(2)));
        Console.WriteLine(new B1(1).Equals(new B2(2)));
        Console.WriteLine(new B2(1).Equals(new B2(2)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"True
False
True").VerifyDiagnostics(
    // (8,25): warning CS8851: 'B1' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(B1 b) => base.Equals((A)b);
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B1").WithLocation(8, 25),
    // (15,25): warning CS8851: 'B2' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(B2 b) => base.Equals((A)b);
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B2").WithLocation(15, 25)
);
        }

        [Fact]
        public void Equality_17()
        {
            var source =
@"using static System.Console;
record A;
record B1(int P) : A
{
}
record B2(int P) : A
{
}
class Program
{
    static void Main()
    {
        WriteLine(new B1(1).Equals(new B1(1)));
        WriteLine(new B1(1).Equals(new B1(2)));
        WriteLine(new B2(3).Equals(new B2(3)));
        WriteLine(new B2(3).Equals(new B2(4)));
        WriteLine(((A)new B1(1)).Equals(new B1(1)));
        WriteLine(((A)new B1(1)).Equals(new B1(2)));
        WriteLine(((A)new B2(3)).Equals(new B2(3)));
        WriteLine(((A)new B2(3)).Equals(new B2(4)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            // init-only is unverifiable
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"True
False
True
False
True
False
True
False").VerifyDiagnostics();

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B1").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "B1..ctor(System.Int32 P)",
                "System.Type B1.EqualityContract.get",
                "System.Type B1.EqualityContract { get; }",
                "System.Int32 B1.<P>k__BackingField",
                "System.Int32 B1.P.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B1.P.init",
                "System.Int32 B1.P { get; init; }",
                "System.String B1.ToString()",
                "System.Boolean B1." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)",
                "System.Boolean B1.op_Inequality(B1? r1, B1? r2)",
                "System.Boolean B1.op_Equality(B1? r1, B1? r2)",
                "System.Int32 B1.GetHashCode()",
                "System.Boolean B1.Equals(System.Object? obj)",
                "System.Boolean B1.Equals(A? other)",
                "System.Boolean B1.Equals(B1? other)",
                "A B1." + WellKnownMemberNames.CloneMethodName + "()",
                "B1..ctor(B1 original)",
                "void B1.Deconstruct(out System.Int32 P)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Equality_18(bool useCompilationReference)
        {
            var sourceA = @"public record A;";
            var comp = CreateCompilation(sourceA);
            var refA = useCompilationReference ? comp.ToMetadataReference() : comp.EmitToImageReference();
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("A.get_EqualityContract"), isOverride: false);
            VerifyVirtualMethods(comp.GetMembers("A.Equals"), ("System.Boolean A.Equals(A? other)", false), ("System.Boolean A.Equals(System.Object? obj)", true));

            var sourceB = @"record B : A;";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("B.get_EqualityContract"), isOverride: true);
            VerifyVirtualMethods(comp.GetMembers("B.Equals"), ("System.Boolean B.Equals(B? other)", false), ("System.Boolean B.Equals(A? other)", true), ("System.Boolean B.Equals(System.Object? obj)", true));
        }

        [Fact]
        public void Equality_19()
        {
            var source =
@"using static System.Console;
record A<T>;
record B : A<int>;
class Program
{
    static void Main()
    {
        WriteLine(new A<int>().Equals(new A<int>()));
        WriteLine(new A<int>().Equals(new B()));
        WriteLine(new B().Equals(new A<int>()));
        WriteLine(new B().Equals(new B()));
        WriteLine(((A<int>)new B()).Equals(new A<int>()));
        WriteLine(((A<int>)new B()).Equals(new B()));
        WriteLine(new B().Equals((A<int>)new B()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
False
False
True
False
True
True").VerifyDiagnostics();

            verifier.VerifyIL("A<T>.Equals(A<T>)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0015
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A<T>.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A<T>.EqualityContract.get""
  IL_000f:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
            verifier.VerifyIL("B.Equals(A<int>)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  callvirt   ""bool object.Equals(object)""
  IL_0007:  ret
}");
            verifier.VerifyIL("B.Equals(B)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A<int>.Equals(A<int>)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void Equality_20()
        {
            var source =
@"
record C;
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.GetHashCode'
                // record C;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C;").WithArguments("System.Collections.Generic.EqualityComparer`1", "GetHashCode").WithLocation(2, 1)
                );
        }

        [Fact]
        public void Equality_21()
        {
            var source =
@"
record C;
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.get_Default'
                // record C;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C;").WithArguments("System.Collections.Generic.EqualityComparer`1", "get_Default").WithLocation(2, 1)
                );
        }

        [Fact]
        [WorkItem(44988, "https://github.com/dotnet/roslyn/issues/44988")]
        public void Equality_22()
        {
            var source =
@"
record C
{
    int x = 0;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.get_Default'
                // record C
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"record C
{
    int x = 0;
}").WithArguments("System.Collections.Generic.EqualityComparer`1", "get_Default").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.get_Default'
                // record C
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"record C
{
    int x = 0;
}").WithArguments("System.Collections.Generic.EqualityComparer`1", "get_Default").WithLocation(2, 1),
                // (4,9): warning CS0414: The field 'C.x' is assigned but its value is never used
                //     int x = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x").WithArguments("C.x").WithLocation(4, 9)
                );
        }

        [Fact]
        public void IEquatableT_01()
        {
            var source =
@"record A<T>;
record B : A<int>;
class Program
{
    static void F<T>(System.IEquatable<T> t)
    {
    }
    static void M<T>()
    {
        F(new A<T>());
        F(new B());
        F<A<int>>(new B());
        F<B>(new B());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,9): error CS0411: The type arguments for method 'Program.F<T>(IEquatable<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(new B());
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.IEquatable<T>)").WithLocation(11, 9));
        }

        [Fact]
        public void IEquatableT_02()
        {
            var source =
@"using System;
record A;
record B<T> : A;
record C : B<int>;
class Program
{
    static string F<T>(IEquatable<T> t)
    {
        return typeof(T).Name;
    }
    static void Main()
    {
        Console.WriteLine(F(new A()));
        Console.WriteLine(F<A>(new C()));
        Console.WriteLine(F<B<int>>(new C()));
        Console.WriteLine(F<C>(new C()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"A
A
B`1
C").VerifyDiagnostics();
        }

        [Fact]
        public void IEquatableT_03()
        {
            var source =
@"#nullable enable
using System;
record A<T> : IEquatable<A<T>>
{
}
record B : A<object>, IEquatable<A<object>>, IEquatable<B?>;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B?>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B?>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_04()
        {
            var source =
@"using System;
record A<T>
{
    internal static bool Report(string s) { Console.WriteLine(s); return false; }
    public virtual bool Equals(A<T> other) => Report(""A<T>.Equals(A<T>)"");
}
record B : A<object>
{
    public virtual bool Equals(B other) => Report(""B.Equals(B)"");
}
class Program
{
    static void Main()
    {
        var a = new A<object>();
        var b = new B();
        _ = a.Equals(b);
        _ = ((A<object>)b).Equals(b);
        _ = b.Equals(a);
        _ = b.Equals(b);
        _ = ((IEquatable<A<object>>)a).Equals(b);
        _ = ((IEquatable<A<object>>)b).Equals(b);
        _ = ((IEquatable<B>)b).Equals(b);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"A<T>.Equals(A<T>)
B.Equals(B)
B.Equals(B)
B.Equals(B)
A<T>.Equals(A<T>)
B.Equals(B)
B.Equals(B)").VerifyDiagnostics(
    // (5,25): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(A<T> other) => Report("A<T>.Equals(A<T>)");
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(5, 25),
    // (9,25): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(B other) => Report("B.Equals(B)");
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(9, 25)
);

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<B>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_05()
        {
            var source =
@"using System;
record A<T> : IEquatable<A<T>>
{
    internal static bool Report(string s) { Console.WriteLine(s); return false; }
    public virtual bool Equals(A<T> other) => Report(""A<T>.Equals(A<T>)"");
}
record B : A<object>, IEquatable<A<object>>, IEquatable<B>
{
    public virtual bool Equals(B other) => Report(""B.Equals(B)"");
}
record C : A<object>, IEquatable<A<object>>, IEquatable<C>
{
}
class Program
{
    static void Main()
    {
        var a = new A<object>();
        var b = new B();
        _ = a.Equals(b);
        _ = ((A<object>)b).Equals(b);
        _ = b.Equals(a);
        _ = b.Equals(b);
        _ = ((IEquatable<A<object>>)a).Equals(b);
        _ = ((IEquatable<A<object>>)b).Equals(b);
        _ = ((IEquatable<B>)b).Equals(b);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"A<T>.Equals(A<T>)
B.Equals(B)
B.Equals(B)
B.Equals(B)
A<T>.Equals(A<T>)
B.Equals(B)
B.Equals(B)",
                symbolValidator: m =>
                {
                    var b = m.GlobalNamespace.GetTypeMember("B");
                    Assert.Equal("B.Equals(B)", b.FindImplementationForInterfaceMember(b.InterfacesNoUseSiteDiagnostics()[1].GetMember("Equals")).ToDisplayString());
                    var c = m.GlobalNamespace.GetTypeMember("C");
                    Assert.Equal("C.Equals(C?)", c.FindImplementationForInterfaceMember(c.InterfacesNoUseSiteDiagnostics()[1].GetMember("Equals")).ToDisplayString());
                }).VerifyDiagnostics(
                    // (5,25): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                    //     public virtual bool Equals(A<T> other) => Report("A<T>.Equals(A<T>)");
                    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(5, 25),
                    // (9,25): warning CS8851: '{B}' defines 'Equals' but not 'GetHashCode'
                    //     public virtual bool Equals(B other) => Report("B.Equals(B)");
                    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(9, 25)
                    );

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_06()
        {
            var source =
@"using System;
record A<T> : IEquatable<A<T>>
{
    internal static bool Report(string s) { Console.WriteLine(s); return false; }
    bool IEquatable<A<T>>.Equals(A<T> other) => Report(""A<T>.Equals(A<T>)"");
}
record B : A<object>, IEquatable<A<object>>, IEquatable<B>
{
    bool IEquatable<A<object>>.Equals(A<object> other) => Report(""B.Equals(A<object>)"");
    bool IEquatable<B>.Equals(B other) => Report(""B.Equals(B)"");
}
class Program
{
    static void Main()
    {
        var a = new A<object>();
        var b = new B();
        _ = a.Equals(b);
        _ = ((A<object>)b).Equals(b);
        _ = b.Equals(a);
        _ = b.Equals(b);
        _ = ((IEquatable<A<object>>)a).Equals(b);
        _ = ((IEquatable<A<object>>)b).Equals(b);
        _ = ((IEquatable<B>)b).Equals(b);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"A<T>.Equals(A<T>)
B.Equals(A<object>)
B.Equals(B)").VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_07()
        {
            var source =
@"using System;
record A<T> : IEquatable<B1>, IEquatable<B2>
{
    bool IEquatable<B1>.Equals(B1 other) => false;
    bool IEquatable<B2>.Equals(B2 other) => false;
}
record B1 : A<object>;
record B2 : A<int>;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<B1>", "System.IEquatable<B2>", "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<B1>", "System.IEquatable<B2>", "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B1");
            AssertEx.Equal(new[] { "System.IEquatable<B1>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<B2>", "System.IEquatable<A<System.Object>>", "System.IEquatable<B1>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B2");
            AssertEx.Equal(new[] { "System.IEquatable<B2>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<B1>", "System.IEquatable<A<System.Int32>>", "System.IEquatable<B2>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_08()
        {
            var source =
@"interface I<T>
{
}
record A<T> : I<A<T>>
{
}
record B : A<object>, I<A<object>>, I<B>
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "I<A<T>>", "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "I<A<T>>", "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "I<A<System.Object>>", "I<B>", "System.IEquatable<B>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "I<A<System.Object>>", "I<B>", "System.IEquatable<B>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_09()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual int GetHashCode() => 0;
        public virtual string ToString() => """";
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"record A<T>;
record B : A<int>;
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(1, 8),
                // (1,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(1, 8),
                // (1,8): error CS0518: Predefined type 'System.Type' is not defined or imported
                // record A<T>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(1, 8),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.Type' is not defined or imported
                // record B : A<int>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.Type").WithLocation(2, 8)
                );

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<B>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Int32>>[missing]", "System.IEquatable<B>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_10()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual int GetHashCode() => 0;
        public virtual string ToString() => """";
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"record A<T> : System.IEquatable<A<T>>;
record B : A<int>, System.IEquatable<B>;
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(1, 8),
                // (1,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(1, 8),
                // (1,8): error CS0518: Predefined type 'System.Type' is not defined or imported
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(1, 8),
                // (1,8): error CS0115: 'A<T>.ToString()': no suitable method found to override
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.ToString()").WithLocation(1, 8),
                // (1,8): error CS0115: 'A<T>.GetHashCode()': no suitable method found to override
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.GetHashCode()").WithLocation(1, 8),
                // (1,8): error CS0115: 'A<T>.EqualityContract': no suitable method found to override
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.EqualityContract").WithLocation(1, 8),
                // (1,8): error CS0115: 'A<T>.Equals(object?)': no suitable method found to override
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.Equals(object?)").WithLocation(1, 8),
                // (1,22): error CS0234: The type or namespace name 'IEquatable<>' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IEquatable<A<T>>").WithArguments("IEquatable<>", "System").WithLocation(1, 22),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>, System.IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>, System.IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.Type' is not defined or imported
                // record B : A<int>, System.IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.Type").WithLocation(2, 8),
                // (2,27): error CS0234: The type or namespace name 'IEquatable<>' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // record B : A<int>, System.IEquatable<B>;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IEquatable<B>").WithArguments("IEquatable<>", "System").WithLocation(2, 27));

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<B>", "System.IEquatable<B>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Int32>>[missing]", "System.IEquatable<B>", "System.IEquatable<B>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_11()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual int GetHashCode() => 0;
        public virtual string ToString() => """";
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"using System;
record A<T> : IEquatable<A<T>>;
record B : A<int>, IEquatable<B>;
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.Type' is not defined or imported
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.ToString()': no suitable method found to override
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.ToString()").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.GetHashCode()': no suitable method found to override
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.GetHashCode()").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.EqualityContract': no suitable method found to override
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.EqualityContract").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.Equals(object?)': no suitable method found to override
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.Equals(object?)").WithLocation(2, 8),
                // (2,15): error CS0246: The type or namespace name 'IEquatable<>' could not be found (are you missing a using directive or an assembly reference?)
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IEquatable<A<T>>").WithArguments("IEquatable<>").WithLocation(2, 15),
                // (3,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>, IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(3, 8),
                // (3,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>, IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(3, 8),
                // (3,8): error CS0518: Predefined type 'System.Type' is not defined or imported
                // record B : A<int>, IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.Type").WithLocation(3, 8),
                // (3,20): error CS0246: The type or namespace name 'IEquatable<>' could not be found (are you missing a using directive or an assembly reference?)
                // record B : A<int>, IEquatable<B>;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IEquatable<B>").WithArguments("IEquatable<>").WithLocation(3, 20));

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "IEquatable<B>", "System.IEquatable<B>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Int32>>[missing]", "IEquatable<B>", "System.IEquatable<B>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_12()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual int GetHashCode() => 0;
        public virtual string ToString() => """";
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
        void Other();
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"record A;
class Program
{
    static void Main()
    {
        System.IEquatable<A> a = new A();
        _ = a.Equals(null);
    }
}";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): error CS0518: Predefined type 'System.Type' is not defined or imported
                // record A;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Type").WithLocation(1, 8),
                // (1,8): error CS0535: 'A' does not implement interface member 'IEquatable<A>.Other()'
                // record A;
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "A").WithArguments("A", "System.IEquatable<A>.Other()").WithLocation(1, 8));
        }

        [Fact]
        public void IEquatableT_13()
        {
            var source =
@"record A
{
    internal virtual bool Equals(A other) => false;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,8): error CS0737: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is not public.
                // record A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(1, 8),
                // (3,27): error CS8873: Record member 'A.Equals(A)' must be public.
                //     internal virtual bool Equals(A other) => false;
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(3, 27),
                // (3,27): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     internal virtual bool Equals(A other) => false;
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(3, 27)
                );
        }

        [Fact]
        public void IEquatableT_14()
        {
            var source =
@"record A
{
    public bool Equals(A other) => false;
}
record B : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,17): error CS8872: 'A.Equals(A)' must allow overriding because the containing record is not sealed.
                //     public bool Equals(A other) => false;
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(3, 17),
                // (3,17): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(A other) => false;
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(3, 17),
                // (5,8): error CS0506: 'B.Equals(A?)': cannot override inherited member 'A.Equals(A)' because it is not marked virtual, abstract, or override
                // record B : A
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.Equals(A?)", "A.Equals(A)").WithLocation(5, 8));
        }

        [WorkItem(45026, "https://github.com/dotnet/roslyn/issues/45026")]
        [Fact]
        public void IEquatableT_15()
        {
            var source =
@"using System;
record R
{
    bool IEquatable<R>.Equals(R other) => false;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void IEquatableT_16()
        {
            var source =
@"using System;
class A<T>
{
    record B<U> : IEquatable<B<T>>
    {
        bool IEquatable<B<T>>.Equals(B<T> other) => false;
        bool IEquatable<B<U>>.Equals(B<U> other) => false;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,12): error CS0695: 'A<T>.B<U>' cannot implement both 'IEquatable<A<T>.B<T>>' and 'IEquatable<A<T>.B<U>>' because they may unify for some type parameter substitutions
                //     record B<U> : IEquatable<B<T>>
                Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "B").WithArguments("A<T>.B<U>", "System.IEquatable<A<T>.B<T>>", "System.IEquatable<A<T>.B<U>>").WithLocation(4, 12));
        }

        [Fact]
        public void InterfaceImplementation()
        {
            var source = @"
interface I
{
    int P1 { get; init; }
    int P2 { get; init; }
    int P3 { get; set; }
}
record R(int P1) : I
{
    public int P2 { get; init; }
    int I.P3 { get; set; }

    public static void Main()
    {
        I r = new R(42) { P2 = 43 };
        r.P3 = 44;
        System.Console.Write((r.P1, r.P2, r.P3));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43, 44)", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void Initializers_01()
        {
            var src = @"
using System;

record C(int X)
{
    int Z = X + 1;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"2").VerifyDiagnostics();

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            var recordDeclaration = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Single();
            Assert.Equal("C", recordDeclaration.Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclaration));
        }

        [Fact]
        public void Initializers_02()
        {
            var src = @"
record C(int X)
{
    static int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,20): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.X'
                //     static int Z = X + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "X").WithArguments("C.X").WithLocation(4, 20)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Property, symbol!.Kind);
            Assert.Equal("System.Int32 C.X { get; init; }", symbol.ToTestDisplayString());
            Assert.Equal("C", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_03()
        {
            var src = @"
record C(int X)
{
    const int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.X'
                //     const int Z = X + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "X").WithArguments("C.X").WithLocation(4, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Property, symbol!.Kind);
            Assert.Equal("System.Int32 C.X { get; init; }", symbol.ToTestDisplayString());
            Assert.Equal("C", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_04()
        {
            var src = @"
using System;

record C(int X)
{
    Func<int> Z = () => X + 1;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Z());
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"2").VerifyDiagnostics();

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("() => X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("lambda expression", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_05()
        {
            var src = @"
using System;

record Base
{
    public Base(Func<int> X)
    {
        Console.WriteLine(X());
    }

    public Base() {}
}

record C(int X) : Base(() => 100 + X++)
{
    Func<int> Y = () => 200 + X++;
    Func<int> Z = () => 300 + X++;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Y());
        Console.WriteLine(c.Z());
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
101
202
303
").VerifyDiagnostics();
        }

        [Fact]
        public void SynthesizedRecordPointerProperty()
        {
            var src = @"
record R(int P1, int* P2, delegate*<int> P3);";

            var comp = CreateCompilation(src);
            var p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P1");
            Assert.False(p.HasPointerType);

            p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P2");
            Assert.True(p.HasPointerType);

            p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P3");
            Assert.True(p.HasPointerType);
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberModifiers_RefOrOut()
        {
            var src = @"
record R(ref int P1, out int P2);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,8): error CS0177: The out parameter 'P2' must be assigned to before control leaves the current method
                // record R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "R").WithArguments("P2").WithLocation(2, 8),
                // (2,10): error CS0631: ref and out are not valid in this context
                // record R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(2, 10),
                // (2,22): error CS0631: ref and out are not valid in this context
                // record R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(2, 22)
                );
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberModifiers_RefOrOut_WithBase()
        {
            var src = @"
record Base(int I);
record R(ref int P1, out int P2) : Base(P2 = 1);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (3,10): error CS0631: ref and out are not valid in this context
                // record R(ref int P1, out int P2) : Base(P2 = 1);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(3, 10),
                // (3,22): error CS0631: ref and out are not valid in this context
                // record R(ref int P1, out int P2) : Base(P2 = 1);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(3, 22)
                );
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberModifiers_In()
        {
            var src = @"
record R(in int P1);

public class C
{
    public static void Main()
    {
        var r = new R(42);
        int i = 43;
        var r2 = new R(in i);
        System.Console.Write((r.P1, r2.P1));
    }
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43)", verify: Verification.Skipped /* init-only */);

            var actualMembers = comp.GetMember<NamedTypeSymbol>("R").Constructors.ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "R..ctor(in System.Int32 P1)",
                "R..ctor(R original)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberModifiers_This()
        {
            var src = @"
record R(this int i);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,10): error CS0027: Keyword 'this' is not available in the current context
                // record R(this int i);
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(2, 10)
                );
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberModifiers_Params()
        {
            var src = @"
record R(params int[] Array);

public class C
{
    public static void Main()
    {
        var r = new R(42, 43);
        var r2 = new R(new[] { 44, 45 });
        System.Console.Write((r.Array[0], r.Array[1], r2.Array[0], r2.Array[1]));
    }
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43, 44, 45)", verify: Verification.Skipped /* init-only */);

            var actualMembers = comp.GetMember<NamedTypeSymbol>("R").Constructors.ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "R..ctor(params System.Int32[] Array)",
                "R..ctor(R original)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberDefaultValue()
        {
            var src = @"
record R(int P = 42)
{
    public static void Main()
    {
        var r = new R();
        System.Console.Write(r.P);
    }
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped /* init-only */);
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberDefaultValue_AndPropertyWithInitializer()
        {
            var src = @"
record R(int P = 1)
{
    public int P { get; init; } = 42;

    public static void Main()
    {
        var r = new R();
        System.Console.Write(r.P);
    }
}
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped /* init-only */);

            verifier.VerifyIL("R..ctor(int)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int R.<P>k__BackingField""
  IL_0008:  ldarg.0
  IL_0009:  call       ""object..ctor()""
  IL_000e:  nop
  IL_000f:  ret
}");
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberDefaultValue_AndPropertyWithoutInitializer()
        {
            var src = @"
record R(int P = 42)
{
    public int P { get; init; }

    public static void Main()
    {
        var r = new R();
        System.Console.Write(r.P);
    }
}
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Skipped /* init-only */);

            verifier.VerifyIL("R..ctor(int)", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  ret
}");
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberDefaultValue_AndPropertyWithInitializer_CopyingParameter()
        {
            var src = @"
record R(int P = 42)
{
    public int P { get; init; } = P;

    public static void Main()
    {
        var r = new R();
        System.Console.Write(r.P);
    }
}
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped /* init-only */);

            verifier.VerifyIL("R..ctor(int)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int R.<P>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  call       ""object..ctor()""
  IL_000d:  nop
  IL_000e:  ret
}");
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_01()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true) ]
public class A : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true) ]
public class B : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class C : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class D : System.Attribute
{
}

public record Test(
    [field: A]
    [property: B]
    [param: C]
    [D]
    int P1)
{
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var prop1 = @class.GetMember<PropertySymbol>("P1");
                AssertEx.SetEqual(new[] { "B" }, getAttributeStrings(prop1));

                var field1 = @class.GetMember<FieldSymbol>("<P1>k__BackingField");
                AssertEx.SetEqual(new[] { "A" }, getAttributeStrings(field1));

                var param1 = @class.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.AsSingleton()?.Name == "P1").Single().Parameters[0];
                AssertEx.SetEqual(new[] { "C", "D" }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                parseOptions: TestOptions.Regular9,
                // init-only is unverifiable
                verify: Verification.Skipped,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            comp.VerifyDiagnostics();

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes().Where(a =>
                {
                    switch (a.AttributeClass!.Name)
                    {
                        case "A":
                        case "B":
                        case "C":
                        case "D":
                            return true;
                    }

                    return false;
                }));
            }
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_02()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class B : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class C : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class D : System.Attribute
{
}

public record Test(
    [field: A]
    [property: B]
    [param: C]
    [D]
    int P1)
{
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var prop1 = @class.GetMember<PropertySymbol>("P1");
                AssertEx.SetEqual(new[] { "B" }, getAttributeStrings(prop1));

                var field1 = @class.GetMember<FieldSymbol>("<P1>k__BackingField");
                AssertEx.SetEqual(new[] { "A" }, getAttributeStrings(field1));

                var param1 = @class.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.AsSingleton()?.Name == "P1").Single().Parameters[0];
                AssertEx.SetEqual(new[] { "C", "D" }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                parseOptions: TestOptions.Regular9,
                // init-only is unverifiable
                verify: Verification.Skipped,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            comp.VerifyDiagnostics();

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes().Where(a =>
                {
                    switch (a.AttributeClass!.Name)
                    {
                        case "A":
                        case "B":
                        case "C":
                        case "D":
                            return true;
                    }

                    return false;
                }));
            }
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_03()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true) ]
public class A : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true) ]
public class B : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class C : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class D : System.Attribute
{
}

public abstract record Base
{
    public abstract int P1 { get; init; }
}

public record Test(
    [field: A]
    [property: B]
    [param: C]
    [D]
    int P1) : Base
{
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var prop1 = @class.GetMember<PropertySymbol>("P1");
                AssertEx.SetEqual(new[] { "B" }, getAttributeStrings(prop1));

                var field1 = @class.GetMember<FieldSymbol>("<P1>k__BackingField");
                AssertEx.SetEqual(new[] { "A" }, getAttributeStrings(field1));

                var param1 = @class.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.AsSingleton()?.Name == "P1").Single().Parameters[0];
                AssertEx.SetEqual(new[] { "C", "D" }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                parseOptions: TestOptions.Regular9,
                // init-only is unverifiable
                verify: Verification.Skipped,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            comp.VerifyDiagnostics();

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes().Where(a =>
                {
                    switch (a.AttributeClass!.Name)
                    {
                        case "A":
                        case "B":
                        case "C":
                        case "D":
                            return true;
                    }

                    return false;
                }));
            }
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_04()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true) ]
public class A : System.Attribute
{
}

public record Test(
    [method: A]
    int P1)
{
    [method: A]
    void M1() {}
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var prop1 = @class.GetMember<PropertySymbol>("P1");
                AssertEx.SetEqual(new string[] { }, getAttributeStrings(prop1));

                var field1 = @class.GetMember<FieldSymbol>("<P1>k__BackingField");
                AssertEx.SetEqual(new string[] { }, getAttributeStrings(field1));

                var param1 = @class.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.AsSingleton()?.Name == "P1").Single().Parameters[0];
                AssertEx.SetEqual(new string[] { }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                parseOptions: TestOptions.Regular9,
                // init-only is unverifiable
                verify: Verification.Skipped,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            comp.VerifyDiagnostics(
                // (8,6): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'field, property, param'. All attributes in this block will be ignored.
                //     [method: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "field, property, param").WithLocation(8, 6)
                );

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes().Where(a =>
                {
                    switch (a.AttributeClass!.Name)
                    {
                        case "A":
                            return true;
                    }

                    return false;
                }));
            }
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_05()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true) ]
public class A : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true) ]
public class B : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class C : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class D : System.Attribute
{
}

public abstract record Base
{
    public virtual int P1 { get; init; }
}

public record Test(
    [field: A]
    [property: B]
    [param: C]
    [D]
    int P1) : Base
{
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                Assert.Null(@class.GetMember<PropertySymbol>("P1"));
                Assert.Null(@class.GetMember<FieldSymbol>("<P1>k__BackingField"));

                var param1 = @class.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.AsSingleton()?.Name == "P1").Single().Parameters[0];
                AssertEx.SetEqual(new[] { "C", "D" }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                parseOptions: TestOptions.Regular9,
                // init-only is unverifiable
                verify: Verification.Skipped,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            comp.VerifyDiagnostics(
                // (27,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                //     [field: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(27, 6),
                // (28,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                //     [property: B]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "param").WithLocation(28, 6)
                );

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes().Where(a =>
                {
                    switch (a.AttributeClass!.Name)
                    {
                        case "A":
                        case "B":
                        case "C":
                        case "D":
                            return true;
                    }

                    return false;
                }));
            }
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_06()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true) ]
public class A : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true) ]
public class B : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class C : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class D : System.Attribute
{
}

public abstract record Base
{
    public int P1 { get; init; }
}

public record Test(
    [field: A]
    [property: B]
    [param: C]
    [D]
    int P1) : Base
{
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                Assert.Null(@class.GetMember<PropertySymbol>("P1"));
                Assert.Null(@class.GetMember<FieldSymbol>("<P1>k__BackingField"));

                var param1 = @class.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.AsSingleton()?.Name == "P1").Single().Parameters[0];
                AssertEx.SetEqual(new[] { "C", "D" }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                parseOptions: TestOptions.Regular9,
                // init-only is unverifiable
                verify: Verification.Skipped,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            comp.VerifyDiagnostics(
                // (27,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                //     [field: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(27, 6),
                // (28,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                //     [property: B]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "param").WithLocation(28, 6)
                );

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes().Where(a =>
                {
                    switch (a.AttributeClass!.Name)
                    {
                        case "A":
                        case "B":
                        case "C":
                        case "D":
                            return true;
                    }

                    return false;
                }));
            }
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_07()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class C : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class D : System.Attribute
{
}

public abstract record Base
{
    public int P1 { get; init; }
}

public record Test(
    [param: C]
    [D]
    int P1) : Base
{
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var param1 = @class.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.AsSingleton()?.Name == "P1").Single().Parameters[0];
                AssertEx.SetEqual(new[] { "C", "D" }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                parseOptions: TestOptions.Regular9,
                // init-only is unverifiable
                verify: Verification.Skipped,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            comp.VerifyDiagnostics();

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes().Where(a =>
                {
                    switch (a.AttributeClass!.Name)
                    {
                        case "C":
                        case "D":
                            return true;
                    }

                    return false;
                }));
            }
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_08()
        {
            string source = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

record C<T>([property: NotNull] T? P1, T? P2) where T : class
{
    protected C(C<T> other)
    {
        T x = P1;
        T y = P2;
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition, NotNullAttributeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (9,15): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         T x = P1;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "P1").WithLocation(9, 15),
                // (10,15): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         T y = P2;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "P2").WithLocation(10, 15)
                );
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_09_CallerMemberName()
        {
            string source = @"
using System.Runtime.CompilerServices;
record R([CallerMemberName] string S = """");

class C
{
    public static void Main()
    {
        var r = new R();
        System.Console.Write(r.S);
    }
}
";
            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, expectedOutput: "Main",
                parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe, verify: Verification.Skipped /* init-only */);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecordWithConstraints_NullableWarning()
        {
            var src = @"
#nullable enable
record R<T>(T P) where T : class;
record R2<T>(T P) where T : class { }

public class C
{
    public static void Main()
    {
        var r = new R<string?>(""R"");
        var r2 = new R2<string?>(""R2"");
        System.Console.Write((r.P, r2.P));
    }
}";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,23): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
                //         var r = new R<string?>("R");
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "string?").WithArguments("R<T>", "T", "string?").WithLocation(10, 23),
                // (11,25): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R2<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
                //         var r2 = new R2<string?>("R2");
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "string?").WithArguments("R2<T>", "T", "string?").WithLocation(11, 25)
                );
            CompileAndVerify(comp, expectedOutput: "(R, R2)", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void RecordWithConstraints_ConstraintError()
        {
            var src = @"
record R<T>(T P) where T : class;
record R2<T>(T P) where T : class { }

public class C
{
    public static void Main()
    {
        _ = new R<int>(1);
        _ = new R2<int>(2);
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,19): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'R<T>'
                //         _ = new R<int>(1);
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("R<T>", "T", "int").WithLocation(9, 19),
                // (10,20): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'R2<T>'
                //         _ = new R2<int>(2);
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("R2<T>", "T", "int").WithLocation(10, 20)
                );
        }

        [Fact]
        public void AccessCheckProtected03()
        {
            CSharpCompilation c = CreateCompilation(@"
record X<T> { }

record A { }

record B
{
    record C : X<C.D.E>
    {
        protected record D : A
        {
            public record E { }
        }
    }
}
");

            c.VerifyDiagnostics(
                // (8,12): error CS0060: Inconsistent accessibility: base type 'X<B.C.D.E>' is less accessible than class 'B.C'
                //     record C : X<C.D.E>
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C").WithArguments("B.C", "X<B.C.D.E>").WithLocation(8, 12),
                // (8,12): error CS0050: Inconsistent accessibility: return type 'X<B.C.D.E>' is less accessible than method 'B.C.<Clone>$()'
                //     record C : X<C.D.E>
                Diagnostic(ErrorCode.ERR_BadVisReturnType, "C").WithArguments("B.C.<Clone>$()", "X<B.C.D.E>").WithLocation(8, 12),
                // (8,12): error CS0051: Inconsistent accessibility: parameter type 'X<B.C.D.E>' is less accessible than method 'B.C.Equals(X<B.C.D.E>?)'
                //     record C : X<C.D.E>
                Diagnostic(ErrorCode.ERR_BadVisParamType, "C").WithArguments("B.C.Equals(X<B.C.D.E>?)", "X<B.C.D.E>").WithLocation(8, 12)
                );
        }

        [Fact]
        public void TestTargetType_Abstract()
        {
            var source = @"
abstract record C
{
    void M()
    {
        C x0 = new();
        var x1 = (C)new();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,16): error CS0144: Cannot create an instance of the abstract type or interface 'C'
                //         C x0 = new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("C").WithLocation(6, 16),
                // (7,21): error CS0144: Cannot create an instance of the abstract type or interface 'C'
                //         var x1 = (C)new();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new()").WithArguments("C").WithLocation(7, 21)
                );
        }

        [Fact]
        public void CyclicBases4()
        {
            var text =
@"
record A<T> : B<A<T>> { }
record B<T> : A<B<T>> {
    A<T> F() { return null; }
}
";
            var comp = CreateCompilation(text);
            comp.GetDeclarationDiagnostics().Verify(
                // (2,8): error CS0146: Circular base type dependency involving 'B<A<T>>' and 'A<T>'
                // record A<T> : B<A<T>> { }
                Diagnostic(ErrorCode.ERR_CircularBase, "A").WithArguments("B<A<T>>", "A<T>").WithLocation(2, 8),
                // (3,8): error CS0146: Circular base type dependency involving 'A<B<T>>' and 'B<T>'
                // record B<T> : A<B<T>> {
                Diagnostic(ErrorCode.ERR_CircularBase, "B").WithArguments("A<B<T>>", "B<T>").WithLocation(3, 8),
                // (2,8): error CS0115: 'A<T>.ToString()': no suitable method found to override
                // record A<T> : B<A<T>> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.ToString()").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.GetHashCode()': no suitable method found to override
                // record A<T> : B<A<T>> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.GetHashCode()").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.EqualityContract': no suitable method found to override
                // record A<T> : B<A<T>> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.EqualityContract").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.Equals(object?)': no suitable method found to override
                // record A<T> : B<A<T>> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.Equals(object?)").WithLocation(2, 8),
                // (3,8): error CS0115: 'B<T>.EqualityContract': no suitable method found to override
                // record B<T> : A<B<T>> {
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B<T>.EqualityContract").WithLocation(3, 8),
                // (3,8): error CS0115: 'B<T>.Equals(object?)': no suitable method found to override
                // record B<T> : A<B<T>> {
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B<T>.Equals(object?)").WithLocation(3, 8),
                // (3,8): error CS0115: 'B<T>.GetHashCode()': no suitable method found to override
                // record B<T> : A<B<T>> {
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B<T>.GetHashCode()").WithLocation(3, 8),
                // (3,8): error CS0115: 'B<T>.ToString()': no suitable method found to override
                // record B<T> : A<B<T>> {
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "B").WithArguments("B<T>.ToString()").WithLocation(3, 8)
                );
        }

        [Fact]
        public void CS0250ERR_CallingBaseFinalizeDeprecated()
        {
            var text = @"
record B
{
}

record C : B
{
    ~C()
    {
        base.Finalize();   // CS0250
    }

    public static void Main()
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (10,7): error CS0250: Do not directly call your base type Finalize method. It is called automatically from your destructor.
                Diagnostic(ErrorCode.ERR_CallingBaseFinalizeDeprecated, "base.Finalize()")
                );
        }

        [Fact]
        public void PartialClassWithDifferentTupleNamesInBaseTypes()
        {
            var source = @"
public record Base<T> { }
public partial record C1 : Base<(int a, int b)> { }
public partial record C1 : Base<(int notA, int notB)> { }
public partial record C2 : Base<(int a, int b)> { }
public partial record C2 : Base<(int, int)> { }
public partial record C3 : Base<(int a, int b)> { }
public partial record C3 : Base<(int a, int b)> { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,23): error CS0263: Partial declarations of 'C2' must not specify different base classes
                // public partial record C2 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_PartialMultipleBases, "C2").WithArguments("C2").WithLocation(5, 23),
                // (3,23): error CS0263: Partial declarations of 'C1' must not specify different base classes
                // public partial record C1 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_PartialMultipleBases, "C1").WithArguments("C1").WithLocation(3, 23),
                // (5,23): error CS0115: 'C2.GetHashCode()': no suitable method found to override
                // public partial record C2 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C2").WithArguments("C2.GetHashCode()").WithLocation(5, 23),
                // (5,23): error CS0115: 'C2.ToString()': no suitable method found to override
                // public partial record C2 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C2").WithArguments("C2.ToString()").WithLocation(5, 23),
                // (5,23): error CS0115: 'C2.EqualityContract': no suitable method found to override
                // public partial record C2 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C2").WithArguments("C2.EqualityContract").WithLocation(5, 23),
                // (5,23): error CS0115: 'C2.Equals(object?)': no suitable method found to override
                // public partial record C2 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C2").WithArguments("C2.Equals(object?)").WithLocation(5, 23),
                // (3,23): error CS0115: 'C1.ToString()': no suitable method found to override
                // public partial record C1 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C1").WithArguments("C1.ToString()").WithLocation(3, 23),
                // (3,23): error CS0115: 'C1.GetHashCode()': no suitable method found to override
                // public partial record C1 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C1").WithArguments("C1.GetHashCode()").WithLocation(3, 23),
                // (3,23): error CS0115: 'C1.EqualityContract': no suitable method found to override
                // public partial record C1 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C1").WithArguments("C1.EqualityContract").WithLocation(3, 23),
                // (3,23): error CS0115: 'C1.Equals(object?)': no suitable method found to override
                // public partial record C1 : Base<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "C1").WithArguments("C1.Equals(object?)").WithLocation(3, 23)
                );
        }

        [Fact]
        public void CS0267ERR_PartialMisplaced()
        {
            var test = @"
partial public record C  // CS0267
{
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial public class C  // CS0267
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(2, 1));
        }

        [Fact]
        public void AttributeContainsGeneric()
        {
            string source = @"
[Goo<int>]
class G
{
}
record Goo<T>
{
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (2,2): error CS0404: Cannot apply attribute class 'Goo<T>' because it is generic
                // [Goo<int>]
                Diagnostic(ErrorCode.ERR_AttributeCantBeGeneric, "Goo<int>").WithArguments("Goo<T>").WithLocation(2, 2)
                );
        }

        [Fact]
        public void CS0406ERR_ClassBoundNotFirst()
        {
            var source =
@"interface I { }
record A { }
record B { }
class C<T, U>
    where T : I, A
    where U : A, B
{
    void M<V>() where V : U, A, B { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,18): error CS0406: The class type constraint 'A' must come before any other constraints
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "A").WithArguments("A").WithLocation(5, 18),
                // (6,18): error CS0406: The class type constraint 'B' must come before any other constraints
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "B").WithArguments("B").WithLocation(6, 18),
                // (8,30): error CS0406: The class type constraint 'A' must come before any other constraints
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "A").WithArguments("A").WithLocation(8, 30),
                // (8,33): error CS0406: The class type constraint 'B' must come before any other constraints
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "B").WithArguments("B").WithLocation(8, 33));
        }

        [Fact]
        public void SealedStaticRecord()
        {
            var source = @"
sealed static record R;
";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,22): error CS0106: The modifier 'static' is not valid for this item
                // sealed static record R;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("static").WithLocation(2, 22)
                );
        }

        [Fact]
        public void CS0513ERR_AbstractInConcreteClass02()
        {
            var text = @"
record C
{
    public abstract event System.Action E;
    public abstract int this[int x] { get; set; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,41): error CS0513: 'C.E' is abstract but it is contained in non-abstract type 'C'
                //     public abstract event System.Action E;
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "E").WithArguments("C.E", "C"),
                // (5,39): error CS0513: 'C.this[int].get' is abstract but it is contained in non-abstract type 'C'
                //     public abstract int this[int x] { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "get").WithArguments("C.this[int].get", "C"),
                // (5,44): error CS0513: 'C.this[int].set' is abstract but it is contained in non-abstract type 'C'
                //     public abstract int this[int x] { get; set; }
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "set").WithArguments("C.this[int].set", "C"));
        }

        [Fact]
        public void ConversionToBase()
        {
            var source = @"
public record Base<T> { }
public record Derived : Base<(int a, int b)>
{
    public static explicit operator Base<(int, int)>(Derived x)
    {
        return null;
    }
    public static explicit operator Derived(Base<(int, int)> x)
    {
        return null;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,37): error CS0553: 'Derived.explicit operator Derived(Base<(int, int)>)': user-defined conversions to or from a base type are not allowed
                //     public static explicit operator Derived(Base<(int, int)> x)
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "Derived").WithArguments("Derived.explicit operator Derived(Base<(int, int)>)").WithLocation(9, 37),
                // (5,37): error CS0553: 'Derived.explicit operator Base<(int, int)>(Derived)': user-defined conversions to or from a base type are not allowed
                //     public static explicit operator Base<(int, int)>(Derived x)
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "Base<(int, int)>").WithArguments("Derived.explicit operator Base<(int, int)>(Derived)").WithLocation(5, 37)
                );
        }

        [Fact]
        public void CS0554ERR_ConversionWithDerived()
        {
            var text = @"
public record B
{
    public static implicit operator B(D d) // CS0554
    {
        return null;
    }
}
public record D : B {}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,37): error CS0554: 'B.implicit operator B(D)': user-defined conversions to or from a derived type are not allowed
                //     public static implicit operator B(D d) // CS0554
                Diagnostic(ErrorCode.ERR_ConversionWithDerived, "B").WithArguments("B.implicit operator B(D)")
                );
        }

        [Fact]
        public void CS0574ERR_BadDestructorName()
        {
            var test = @"
namespace x
{
    public record iii
    {
        ~iiii(){}
        public static void Main()
        {
        }
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (6,10): error CS0574: Name of destructor must match name of type
                //         ~iiii(){}
                Diagnostic(ErrorCode.ERR_BadDestructorName, "iiii").WithLocation(6, 10));
        }

        [Fact]
        public void StaticBasePartial()
        {
            var text = @"
static record NV
{
}

public partial record C1
{
}

partial record C1 : NV
{
}

public partial record C1
{
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,15): error CS0106: The modifier 'static' is not valid for this item
                // static record NV
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "NV").WithArguments("static").WithLocation(2, 15),
                // (6,23): error CS0050: Inconsistent accessibility: return type 'NV' is less accessible than method 'C1.<Clone>$()'
                // public partial record C1
                Diagnostic(ErrorCode.ERR_BadVisReturnType, "C1").WithArguments("C1.<Clone>$()", "NV").WithLocation(6, 23),
                // (6,23): error CS0051: Inconsistent accessibility: parameter type 'NV' is less accessible than method 'C1.Equals(NV?)'
                // public partial record C1
                Diagnostic(ErrorCode.ERR_BadVisParamType, "C1").WithArguments("C1.Equals(NV?)", "NV").WithLocation(6, 23),
                // (10,16): error CS0060: Inconsistent accessibility: base class 'NV' is less accessible than class 'C1'
                // partial record C1 : NV
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "C1").WithArguments("C1", "NV").WithLocation(10, 16)
                );
        }

        [Fact]
        public void StaticRecordWithConstructorAndDestructor()
        {
            var text = @"
static record R(int I)
{
    R() : this(0) { }
    ~R() { }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,15): error CS0106: The modifier 'static' is not valid for this item
                // static record R(int I)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("static").WithLocation(2, 15)
                );
        }

        [Fact]
        public void RecordWithPartialMethodExplicitImplementation()
        {
            var source =
@"record R
{
    partial void M();
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,18): error CS0751: A partial method must be declared within a partial type
                //     partial void M();
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "M").WithLocation(3, 18)
                );
        }

        [Fact]
        public void RecordWithMultipleBaseTypes()
        {
            var source = @"
record Base1;
record Base2;
record R : Base1, Base2
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,19): error CS1721: Class 'R' cannot have multiple base classes: 'Base1' and 'Base2'
                // record R : Base1, Base2
                Diagnostic(ErrorCode.ERR_NoMultipleInheritance, "Base2").WithArguments("R", "Base1", "Base2").WithLocation(4, 19)
                );
        }

        [Fact]
        public void RecordWithInterfaceBeforeBase()
        {
            var source = @"
record Base;
interface I { }
record R : I, Base;
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,15): error CS1722: Base class 'Base' must come before any interfaces
                // record R : I, Base;
                Diagnostic(ErrorCode.ERR_BaseClassMustBeFirst, "Base").WithArguments("Base").WithLocation(4, 15)
                );
        }

        [Fact]
        public void RecordLoadedInVisualBasicDisplaysAsRecord()
        {
            var src = @"
public record A;
";
            var compRef = CreateCompilation(src).EmitToImageReference();
            var vbComp = CreateVisualBasicCompilation("", referencedAssemblies: new[] { compRef });
            var symbol = vbComp.GlobalNamespace.GetTypeMember("A");
            Assert.Equal("record A",
                SymbolDisplay.ToDisplayString(symbol, SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
        }

        [Fact]
        public void AnalyzerActions_01()
        {
            var text1 = @"
record A([Attr1]int X = 0) : I1
{}

record B([Attr2]int Y = 1) : A(2), I1
{
    int M() => 3;
}

record C : A, I1
{
    C([Attr3]int Z = 4) : base(5)
    {}
}

interface I1 {}

class Attr1 : System.Attribute {}
class Attr2 : System.Attribute {}
class Attr3 : System.Attribute {}
";

            var analyzer = new AnalyzerActions_01_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount0);
            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
            Assert.Equal(1, analyzer.FireCount13);
            Assert.Equal(1, analyzer.FireCount14);
            Assert.Equal(1, analyzer.FireCount15);
            Assert.Equal(1, analyzer.FireCount16);
            Assert.Equal(1, analyzer.FireCount17);
            Assert.Equal(1, analyzer.FireCount18);
            Assert.Equal(1, analyzer.FireCount19);
            Assert.Equal(1, analyzer.FireCount20);
            Assert.Equal(1, analyzer.FireCount21);
            Assert.Equal(1, analyzer.FireCount22);
            Assert.Equal(1, analyzer.FireCount23);
            Assert.Equal(1, analyzer.FireCount24);
            Assert.Equal(1, analyzer.FireCount25);
            Assert.Equal(1, analyzer.FireCount26);
            Assert.Equal(1, analyzer.FireCount27);
            Assert.Equal(1, analyzer.FireCount28);
            Assert.Equal(1, analyzer.FireCount29);
            Assert.Equal(1, analyzer.FireCount30);
            Assert.Equal(1, analyzer.FireCount31);
        }

        private class AnalyzerActions_01_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount0;
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;
            public int FireCount8;
            public int FireCount9;
            public int FireCount10;
            public int FireCount11;
            public int FireCount12;
            public int FireCount13;
            public int FireCount14;
            public int FireCount15;
            public int FireCount16;
            public int FireCount17;
            public int FireCount18;
            public int FireCount19;
            public int FireCount20;
            public int FireCount21;
            public int FireCount22;
            public int FireCount23;
            public int FireCount24;
            public int FireCount25;
            public int FireCount26;
            public int FireCount27;
            public int FireCount28;
            public int FireCount29;
            public int FireCount30;
            public int FireCount31;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.NumericLiteralExpression);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.EqualsValueClause);
                context.RegisterSyntaxNodeAction(Handle3, SyntaxKind.BaseConstructorInitializer);
                context.RegisterSyntaxNodeAction(Handle4, SyntaxKind.ConstructorDeclaration);
                context.RegisterSyntaxNodeAction(Handle5, SyntaxKind.PrimaryConstructorBaseType);
                context.RegisterSyntaxNodeAction(Handle6, SyntaxKind.RecordDeclaration);
                context.RegisterSyntaxNodeAction(Handle7, SyntaxKind.IdentifierName);
                context.RegisterSyntaxNodeAction(Handle8, SyntaxKind.SimpleBaseType);
                context.RegisterSyntaxNodeAction(Handle9, SyntaxKind.ParameterList);
                context.RegisterSyntaxNodeAction(Handle10, SyntaxKind.ArgumentList);
            }

            protected void Handle1(SyntaxNodeAnalysisContext context)
            {
                var literal = (LiteralExpressionSyntax)context.Node;

                switch (literal.ToString())
                {
                    case "0":
                        Interlocked.Increment(ref FireCount0);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "1":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "2":
                        Interlocked.Increment(ref FireCount2);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;

                    case "3":
                        Interlocked.Increment(ref FireCount3);
                        Assert.Equal("System.Int32 B.M()", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "4":
                        Interlocked.Increment(ref FireCount4);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "5":
                        Interlocked.Increment(ref FireCount5);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(literal.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle2(SyntaxNodeAnalysisContext context)
            {
                var equalsValue = (EqualsValueClauseSyntax)context.Node;

                switch (equalsValue.ToString())
                {
                    case "= 0":
                        Interlocked.Increment(ref FireCount15);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "= 1":
                        Interlocked.Increment(ref FireCount16);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "= 4":
                        Interlocked.Increment(ref FireCount6);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(equalsValue.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle3(SyntaxNodeAnalysisContext context)
            {
                var initializer = (ConstructorInitializerSyntax)context.Node;

                switch (initializer.ToString())
                {
                    case ": base(5)":
                        Interlocked.Increment(ref FireCount7);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(initializer.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle4(SyntaxNodeAnalysisContext context)
            {
                Interlocked.Increment(ref FireCount8);
                Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
            }

            protected void Handle5(SyntaxNodeAnalysisContext context)
            {
                var baseType = (PrimaryConstructorBaseTypeSyntax)context.Node;

                switch (baseType.ToString())
                {
                    case "A(2)":
                        switch (context.ContainingSymbol.ToTestDisplayString())
                        {
                            case "B..ctor([System.Int32 Y = 1])":
                                Interlocked.Increment(ref FireCount9);
                                break;
                            case "B":
                                Interlocked.Increment(ref FireCount17);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(baseType.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle6(SyntaxNodeAnalysisContext context)
            {
                var record = (RecordDeclarationSyntax)context.Node;

                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount10);
                        break;
                    case "B":
                        Interlocked.Increment(ref FireCount11);
                        break;
                    case "A":
                        Interlocked.Increment(ref FireCount12);
                        break;
                    case "C":
                        Interlocked.Increment(ref FireCount13);
                        break;
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount14);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(record.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle7(SyntaxNodeAnalysisContext context)
            {
                var identifier = (IdentifierNameSyntax)context.Node;

                switch (identifier.Identifier.ValueText)
                {
                    case "A":
                        switch (identifier.Parent!.ToString())
                        {
                            case "A(2)":
                                Interlocked.Increment(ref FireCount18);
                                Assert.Equal("B", context.ContainingSymbol.ToTestDisplayString());
                                break;
                            case "A":
                                Interlocked.Increment(ref FireCount19);
                                Assert.Equal(SyntaxKind.SimpleBaseType, identifier.Parent.Kind());
                                Assert.Equal("C", context.ContainingSymbol.ToTestDisplayString());
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "Attr1":
                        Interlocked.Increment(ref FireCount24);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "Attr2":
                        Interlocked.Increment(ref FireCount25);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "Attr3":
                        Interlocked.Increment(ref FireCount26);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                }
            }

            protected void Handle8(SyntaxNodeAnalysisContext context)
            {
                var baseType = (SimpleBaseTypeSyntax)context.Node;

                switch (baseType.ToString())
                {
                    case "I1":
                        switch (context.ContainingSymbol.ToTestDisplayString())
                        {
                            case "A":
                                Interlocked.Increment(ref FireCount20);
                                break;
                            case "B":
                                Interlocked.Increment(ref FireCount21);
                                break;
                            case "C":
                                Interlocked.Increment(ref FireCount22);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "A":
                        switch (context.ContainingSymbol.ToTestDisplayString())
                        {
                            case "C":
                                Interlocked.Increment(ref FireCount23);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;

                    case "System.Attribute":
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle9(SyntaxNodeAnalysisContext context)
            {
                var parameterList = (ParameterListSyntax)context.Node;

                switch (parameterList.ToString())
                {
                    case "([Attr1]int X = 0)":
                        Interlocked.Increment(ref FireCount27);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "([Attr2]int Y = 1)":
                        Interlocked.Increment(ref FireCount28);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "([Attr3]int Z = 4)":
                        Interlocked.Increment(ref FireCount29);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "()":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle10(SyntaxNodeAnalysisContext context)
            {
                var argumentList = (ArgumentListSyntax)context.Node;

                switch (argumentList.ToString())
                {
                    case "(2)":
                        Interlocked.Increment(ref FireCount30);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "(5)":
                        Interlocked.Increment(ref FireCount31);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_02()
        {
            var text1 = @"
record A(int X = 0)
{}

record C
{
    C(int Z = 4)
    {}
}
";

            var analyzer = new AnalyzerActions_02_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
        }

        private class AnalyzerActions_02_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(Handle, SymbolKind.Method);
                context.RegisterSymbolAction(Handle, SymbolKind.Property);
                context.RegisterSymbolAction(Handle, SymbolKind.Parameter);
                context.RegisterSymbolAction(Handle, SymbolKind.NamedType);
            }

            private void Handle(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Int32 A.X { get; init; }":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    case "[System.Int32 X = 0]":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount4);
                        break;
                    case "[System.Int32 Z = 4]":
                        Interlocked.Increment(ref FireCount5);
                        break;
                    case "A":
                        Interlocked.Increment(ref FireCount6);
                        break;
                    case "C":
                        Interlocked.Increment(ref FireCount7);
                        break;
                    case "System.Runtime.CompilerServices.IsExternalInit":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_03()
        {
            var text1 = @"
record A(int X = 0)
{}

record C
{
    C(int Z = 4)
    {}
}
";

            var analyzer = new AnalyzerActions_03_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(0, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(0, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
        }

        private class AnalyzerActions_03_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;
            public int FireCount8;
            public int FireCount9;
            public int FireCount10;
            public int FireCount11;
            public int FireCount12;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolStartAction(Handle1, SymbolKind.Method);
                context.RegisterSymbolStartAction(Handle1, SymbolKind.Property);
                context.RegisterSymbolStartAction(Handle1, SymbolKind.Parameter);
                context.RegisterSymbolStartAction(Handle1, SymbolKind.NamedType);
            }

            private void Handle1(SymbolStartAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        context.RegisterSymbolEndAction(Handle2);
                        break;
                    case "System.Int32 A.X { get; init; }":
                        Interlocked.Increment(ref FireCount2);
                        context.RegisterSymbolEndAction(Handle3);
                        break;
                    case "[System.Int32 X = 0]":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount4);
                        context.RegisterSymbolEndAction(Handle4);
                        break;
                    case "[System.Int32 Z = 4]":
                        Interlocked.Increment(ref FireCount5);
                        break;
                    case "A":
                        Interlocked.Increment(ref FireCount9);

                        Assert.Equal(0, FireCount1);
                        Assert.Equal(0, FireCount2);
                        Assert.Equal(0, FireCount6);
                        Assert.Equal(0, FireCount7);

                        context.RegisterSymbolEndAction(Handle5);
                        break;
                    case "C":
                        Interlocked.Increment(ref FireCount10);

                        Assert.Equal(0, FireCount4);
                        Assert.Equal(0, FireCount8);

                        context.RegisterSymbolEndAction(Handle6);
                        break;
                    case "System.Runtime.CompilerServices.IsExternalInit":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle2(SymbolAnalysisContext context)
            {
                Assert.Equal("A..ctor([System.Int32 X = 0])", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount6);
            }

            private void Handle3(SymbolAnalysisContext context)
            {
                Assert.Equal("System.Int32 A.X { get; init; }", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount7);
            }

            private void Handle4(SymbolAnalysisContext context)
            {
                Assert.Equal("C..ctor([System.Int32 Z = 4])", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount8);
            }

            private void Handle5(SymbolAnalysisContext context)
            {
                Assert.Equal("A", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount11);

                Assert.Equal(1, FireCount1);
                Assert.Equal(1, FireCount2);
                Assert.Equal(1, FireCount6);
                Assert.Equal(1, FireCount7);
            }

            private void Handle6(SymbolAnalysisContext context)
            {
                Assert.Equal("C", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount12);

                Assert.Equal(1, FireCount4);
                Assert.Equal(1, FireCount8);
            }
        }

        [Fact]
        public void AnalyzerActions_04()
        {
            var text1 = @"
record A([Attr1(100)]int X = 0) : I1
{}

record B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

record C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_04_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(0, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
            Assert.Equal(1, analyzer.FireCount13);
            Assert.Equal(1, analyzer.FireCount14);
            Assert.Equal(1, analyzer.FireCount15);
            Assert.Equal(1, analyzer.FireCount16);
            Assert.Equal(1, analyzer.FireCount17);
        }

        private class AnalyzerActions_04_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;
            public int FireCount8;
            public int FireCount9;
            public int FireCount10;
            public int FireCount11;
            public int FireCount12;
            public int FireCount13;
            public int FireCount14;
            public int FireCount15;
            public int FireCount16;
            public int FireCount17;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationAction(Handle1, OperationKind.ConstructorBody);
                context.RegisterOperationAction(Handle2, OperationKind.Invocation);
                context.RegisterOperationAction(Handle3, OperationKind.Literal);
                context.RegisterOperationAction(Handle4, OperationKind.ParameterInitializer);
                context.RegisterOperationAction(Handle5, OperationKind.PropertyInitializer);
                context.RegisterOperationAction(Handle5, OperationKind.FieldInitializer);
            }

            protected void Handle1(OperationAnalysisContext context)
            {
                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal(SyntaxKind.RecordDeclaration, context.Operation.Syntax.Kind());
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount2);
                        Assert.Equal(SyntaxKind.RecordDeclaration, context.Operation.Syntax.Kind());
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount3);
                        Assert.Equal(SyntaxKind.ConstructorDeclaration, context.Operation.Syntax.Kind());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle2(OperationAnalysisContext context)
            {
                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount4);
                        Assert.Equal(SyntaxKind.PrimaryConstructorBaseType, context.Operation.Syntax.Kind());
                        VerifyOperationTree((CSharpCompilation)context.Compilation, context.Operation,
@"
IInvocationOperation ( A..ctor([System.Int32 X = 0])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'A(2)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'A(2)')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '2')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount5);
                        Assert.Equal(SyntaxKind.BaseConstructorInitializer, context.Operation.Syntax.Kind());
                        VerifyOperationTree((CSharpCompilation)context.Compilation, context.Operation,
@"
IInvocationOperation ( A..ctor([System.Int32 X = 0])) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(5)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: ': base(5)')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '5')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle3(OperationAnalysisContext context)
            {
                switch (context.Operation.Syntax.ToString())
                {
                    case "100":
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount6);
                        break;
                    case "0":
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount7);
                        break;
                    case "200":
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount8);
                        break;
                    case "1":
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount9);
                        break;
                    case "2":
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount10);
                        break;
                    case "300":
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount11);
                        break;
                    case "4":
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount12);
                        break;
                    case "5":
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount13);
                        break;
                    case "3":
                        Assert.Equal("System.Int32 B.M()", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount17);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle4(OperationAnalysisContext context)
            {
                switch (context.Operation.Syntax.ToString())
                {
                    case "= 0":
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount14);
                        break;
                    case "= 1":
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount15);
                        break;
                    case "= 4":
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount16);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle5(OperationAnalysisContext context)
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void AnalyzerActions_05()
        {
            var text1 = @"
record A([Attr1(100)]int X = 0) : I1
{}

record B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

record C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_05_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_05_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockAction(Handle);
            }

            private void Handle(OperationBlockAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal(2, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 0", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr1(100)", context.OperationBlocks[1].Syntax.ToString());

                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount2);
                        Assert.Equal(3, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 1", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr2(200)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[2].Kind);
                        Assert.Equal("A(2)", context.OperationBlocks[2].Syntax.ToString());

                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount3);
                        Assert.Equal(4, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 4", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr3(300)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Block, context.OperationBlocks[2].Kind);

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[3].Kind);
                        Assert.Equal(": base(5)", context.OperationBlocks[3].Syntax.ToString());

                        break;
                    case "System.Int32 B.M()":
                        Interlocked.Increment(ref FireCount4);
                        Assert.Equal(1, context.OperationBlocks.Length);
                        Assert.Equal(OperationKind.Block, context.OperationBlocks[0].Kind);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_06()
        {
            var text1 = @"
record A([Attr1(100)]int X = 0) : I1
{}

record B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

record C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_06_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount100);
            Assert.Equal(1, analyzer.FireCount200);
            Assert.Equal(1, analyzer.FireCount300);
            Assert.Equal(1, analyzer.FireCount400);

            Assert.Equal(0, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
            Assert.Equal(1, analyzer.FireCount13);
            Assert.Equal(1, analyzer.FireCount14);
            Assert.Equal(1, analyzer.FireCount15);
            Assert.Equal(1, analyzer.FireCount16);
            Assert.Equal(1, analyzer.FireCount17);

            Assert.Equal(1, analyzer.FireCount1000);
            Assert.Equal(1, analyzer.FireCount2000);
            Assert.Equal(1, analyzer.FireCount3000);
            Assert.Equal(1, analyzer.FireCount4000);
        }

        private class AnalyzerActions_06_Analyzer : AnalyzerActions_04_Analyzer
        {
            public int FireCount100;
            public int FireCount200;
            public int FireCount300;
            public int FireCount400;

            public int FireCount1000;
            public int FireCount2000;
            public int FireCount3000;
            public int FireCount4000;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockStartAction(Handle);
            }

            private void Handle(OperationBlockStartAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount100);
                        Assert.Equal(2, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 0", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr1(100)", context.OperationBlocks[1].Syntax.ToString());

                        RegisterOperationAction(context);
                        context.RegisterOperationBlockEndAction(Handle6);
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount200);
                        Assert.Equal(3, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 1", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr2(200)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[2].Kind);
                        Assert.Equal("A(2)", context.OperationBlocks[2].Syntax.ToString());

                        RegisterOperationAction(context);
                        context.RegisterOperationBlockEndAction(Handle6);
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount300);
                        Assert.Equal(4, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 4", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr3(300)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Block, context.OperationBlocks[2].Kind);

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[3].Kind);
                        Assert.Equal(": base(5)", context.OperationBlocks[3].Syntax.ToString());

                        RegisterOperationAction(context);
                        context.RegisterOperationBlockEndAction(Handle6);
                        break;
                    case "System.Int32 B.M()":
                        Interlocked.Increment(ref FireCount400);
                        Assert.Equal(1, context.OperationBlocks.Length);
                        Assert.Equal(OperationKind.Block, context.OperationBlocks[0].Kind);
                        RegisterOperationAction(context);
                        context.RegisterOperationBlockEndAction(Handle6);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void RegisterOperationAction(OperationBlockStartAnalysisContext context)
            {
                context.RegisterOperationAction(Handle1, OperationKind.ConstructorBody);
                context.RegisterOperationAction(Handle2, OperationKind.Invocation);
                context.RegisterOperationAction(Handle3, OperationKind.Literal);
                context.RegisterOperationAction(Handle4, OperationKind.ParameterInitializer);
                context.RegisterOperationAction(Handle5, OperationKind.PropertyInitializer);
                context.RegisterOperationAction(Handle5, OperationKind.FieldInitializer);
            }

            private void Handle6(OperationBlockAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1000);
                        Assert.Equal(2, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 0", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr1(100)", context.OperationBlocks[1].Syntax.ToString());

                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount2000);
                        Assert.Equal(3, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 1", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr2(200)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[2].Kind);
                        Assert.Equal("A(2)", context.OperationBlocks[2].Syntax.ToString());

                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount3000);
                        Assert.Equal(4, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 4", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.None, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr3(300)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Block, context.OperationBlocks[2].Kind);

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[3].Kind);
                        Assert.Equal(": base(5)", context.OperationBlocks[3].Syntax.ToString());

                        break;
                    case "System.Int32 B.M()":
                        Interlocked.Increment(ref FireCount4000);
                        Assert.Equal(1, context.OperationBlocks.Length);
                        Assert.Equal(OperationKind.Block, context.OperationBlocks[0].Kind);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_07()
        {
            var text1 = @"
record A([Attr1(100)]int X = 0) : I1
{}

record B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

record C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_07_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_07_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockAction(Handle);
            }

            private void Handle(CodeBlockAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":

                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount1);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "B" } }:
                                Interlocked.Increment(ref FireCount2);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        switch (context.CodeBlock)
                        {
                            case ConstructorDeclarationSyntax { Identifier: { ValueText: "C" } }:
                                Interlocked.Increment(ref FireCount3);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 B.M()":
                        switch (context.CodeBlock)
                        {
                            case MethodDeclarationSyntax { Identifier: { ValueText: "M" } }:
                                Interlocked.Increment(ref FireCount4);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_08()
        {
            var text1 = @"
record A([Attr1]int X = 0) : I1
{}

record B([Attr2]int Y = 1) : A(2), I1
{
    int M() => 3;
}

record C : A, I1
{
    C([Attr3]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_08_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount100);
            Assert.Equal(1, analyzer.FireCount200);
            Assert.Equal(1, analyzer.FireCount300);
            Assert.Equal(1, analyzer.FireCount400);

            Assert.Equal(1, analyzer.FireCount0);
            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(0, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(0, analyzer.FireCount10);
            Assert.Equal(0, analyzer.FireCount11);
            Assert.Equal(0, analyzer.FireCount12);
            Assert.Equal(0, analyzer.FireCount13);
            Assert.Equal(0, analyzer.FireCount14);
            Assert.Equal(1, analyzer.FireCount15);
            Assert.Equal(1, analyzer.FireCount16);
            Assert.Equal(0, analyzer.FireCount17);
            Assert.Equal(0, analyzer.FireCount18);
            Assert.Equal(0, analyzer.FireCount19);
            Assert.Equal(0, analyzer.FireCount20);
            Assert.Equal(0, analyzer.FireCount21);
            Assert.Equal(0, analyzer.FireCount22);
            Assert.Equal(0, analyzer.FireCount23);
            Assert.Equal(1, analyzer.FireCount24);
            Assert.Equal(1, analyzer.FireCount25);
            Assert.Equal(1, analyzer.FireCount26);
            Assert.Equal(0, analyzer.FireCount27);
            Assert.Equal(0, analyzer.FireCount28);
            Assert.Equal(0, analyzer.FireCount29);
            Assert.Equal(1, analyzer.FireCount30);
            Assert.Equal(1, analyzer.FireCount31);

            Assert.Equal(1, analyzer.FireCount1000);
            Assert.Equal(1, analyzer.FireCount2000);
            Assert.Equal(1, analyzer.FireCount3000);
            Assert.Equal(1, analyzer.FireCount4000);
        }

        private class AnalyzerActions_08_Analyzer : AnalyzerActions_01_Analyzer
        {
            public int FireCount100;
            public int FireCount200;
            public int FireCount300;
            public int FireCount400;

            public int FireCount1000;
            public int FireCount2000;
            public int FireCount3000;
            public int FireCount4000;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockStartAction<SyntaxKind>(Handle);
            }

            private void Handle(CodeBlockStartAnalysisContext<SyntaxKind> context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":

                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount100);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "B" } }:
                                Interlocked.Increment(ref FireCount200);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        switch (context.CodeBlock)
                        {
                            case ConstructorDeclarationSyntax { Identifier: { ValueText: "C" } }:
                                Interlocked.Increment(ref FireCount300);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 B.M()":
                        switch (context.CodeBlock)
                        {
                            case MethodDeclarationSyntax { Identifier: { ValueText: "M" } }:
                                Interlocked.Increment(ref FireCount400);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.NumericLiteralExpression);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.EqualsValueClause);
                context.RegisterSyntaxNodeAction(Handle3, SyntaxKind.BaseConstructorInitializer);
                context.RegisterSyntaxNodeAction(Handle4, SyntaxKind.ConstructorDeclaration);
                context.RegisterSyntaxNodeAction(Handle5, SyntaxKind.PrimaryConstructorBaseType);
                context.RegisterSyntaxNodeAction(Handle6, SyntaxKind.RecordDeclaration);
                context.RegisterSyntaxNodeAction(Handle7, SyntaxKind.IdentifierName);
                context.RegisterSyntaxNodeAction(Handle8, SyntaxKind.SimpleBaseType);
                context.RegisterSyntaxNodeAction(Handle9, SyntaxKind.ParameterList);
                context.RegisterSyntaxNodeAction(Handle10, SyntaxKind.ArgumentList);

                context.RegisterCodeBlockEndAction(Handle11);
            }

            private void Handle11(CodeBlockAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":

                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount1000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "B" } }:
                                Interlocked.Increment(ref FireCount2000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        switch (context.CodeBlock)
                        {
                            case ConstructorDeclarationSyntax { Identifier: { ValueText: "C" } }:
                                Interlocked.Increment(ref FireCount3000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 B.M()":
                        switch (context.CodeBlock)
                        {
                            case MethodDeclarationSyntax { Identifier: { ValueText: "M" } }:
                                Interlocked.Increment(ref FireCount4000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_09()
        {
            var text1 = @"
record A([Attr1(100)]int X = 0) : I1
{}

record B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

record C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_09_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
        }

        private class AnalyzerActions_09_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;
            public int FireCount8;
            public int FireCount9;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(Handle1, SymbolKind.Method);
                context.RegisterSymbolAction(Handle2, SymbolKind.Property);
                context.RegisterSymbolAction(Handle3, SymbolKind.Parameter);
            }

            private void Handle1(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case "System.Int32 B.M()":
                        Interlocked.Increment(ref FireCount4);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle2(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "System.Int32 A.X { get; init; }":
                        Interlocked.Increment(ref FireCount5);
                        break;
                    case "System.Int32 B.Y { get; init; }":
                        Interlocked.Increment(ref FireCount6);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle3(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "[System.Int32 X = 0]":
                        Interlocked.Increment(ref FireCount7);
                        break;
                    case "[System.Int32 Y = 1]":
                        Interlocked.Increment(ref FireCount8);
                        break;
                    case "[System.Int32 Z = 4]":
                        Interlocked.Increment(ref FireCount9);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        [WorkItem(46657, "https://github.com/dotnet/roslyn/issues/46657")]
        public void CanDeclareIteratorInRecord()
        {
            var source = @"
using System.Collections.Generic;

public record X(int a)
{
    public static void Main()
    {
        foreach(var i in new X(42).GetItems())
        {
            System.Console.Write(i);
        }
    }
    public IEnumerable<int> GetItems() { yield return a; yield return a + 1; }
}";

            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9)
                .VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "4243", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void DefaultCtor_01()
        {
            var src = @"
record C
{
}

class Program
{
    static void Main()
    {
        var x = new C();
        var y = new C();
        System.Console.WriteLine(x == y);
    }
}
";

            var verifier = CompileAndVerify(src, expectedOutput: "True");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("C..ctor()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void DefaultCtor_02()
        {
            var src = @"
record B(int x);

record C : B
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,8): error CS1729: 'B' does not contain a constructor that takes 0 arguments
                // record C : B
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("B", "0").WithLocation(4, 8)
                );
        }

        [Fact]
        public void DefaultCtor_03()
        {
            var src = @"
record C
{
    public C(C c){}
}

class Program
{
    static void Main()
    {
        var x = new C();
        var y = new C();
        System.Console.WriteLine(x == y);
    }
}
";

            var verifier = CompileAndVerify(src, expectedOutput: "True");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("C..ctor()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void DefaultCtor_04()
        {
            var src = @"
record B(int x);

record C : B
{
    public C(C c) : base(c) {}
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,8): error CS1729: 'B' does not contain a constructor that takes 0 arguments
                // record C : B
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("B", "0").WithLocation(4, 8)
                );
        }

        [Fact]
        public void DefaultCtor_05()
        {
            var src = @"
record C(int x);

class Program
{
    static void Main()
    {
        _ = new C();
    }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (8,17): error CS7036: There is no argument given that corresponds to the required formal parameter 'x' of 'C.C(int)'
                //         _ = new C();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("x", "C.C(int)").WithLocation(8, 17)
                );
        }

        [Fact]
        public void DefaultCtor_06()
        {
            var src = @"
record C
{
    C(int x) {}
}

class Program
{
    static void Main()
    {
        _ = new C();
    }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (11,17): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                //         _ = new C();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("C", "0").WithLocation(11, 17)
                );
        }

        [Fact]
        public void DefaultCtor_07()
        {
            var src = @"
class C
{
    C(C x) {}
}

class Program
{
    static void Main()
    {
        _ = new C();
    }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (11,17): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                //         _ = new C();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("C", "0").WithLocation(11, 17)
                );
        }

        [Fact]
        [WorkItem(47867, "https://github.com/dotnet/roslyn/issues/47867")]
        public void ToString_RecordWithStaticMembers()
        {
            var src = @"
var c1 = new C1(42);
System.Console.Write(c1.ToString());

record C1(int I1)
{
    public static int field1 = 44;
    public const int field2 = 44;
    public static int P1 { set { } }
    public static int P2 { get { return 1; } set { } }
    public static int P3 { get { return 1; } }
}
";

            var compDebug = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe);
            var compRelease = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compDebug, expectedOutput: "C1 { I1 = 42 }", verify: Verification.Skipped /* init-only */);
            compDebug.VerifyEmitDiagnostics();

            CompileAndVerify(compRelease, expectedOutput: "C1 { I1 = 42 }", verify: Verification.Skipped /* init-only */);
            compRelease.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(47867, "https://github.com/dotnet/roslyn/issues/47867")]
        public void ToString_NestedRecord()
        {
            var src = @"
var c1 = new Outer.C1(42);
System.Console.Write(c1.ToString());

public class Outer
{
    public record C1(int I1);
}
";

            var compDebug = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe);
            var compRelease = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compDebug, expectedOutput: "C1 { I1 = 42 }", verify: Verification.Skipped /* init-only */);
            compDebug.VerifyEmitDiagnostics();

            CompileAndVerify(compRelease, expectedOutput: "C1 { I1 = 42 }", verify: Verification.Skipped /* init-only */);
            compRelease.VerifyEmitDiagnostics();
        }
    }
}
