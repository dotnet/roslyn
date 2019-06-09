// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldInitializerBindingTests : CompilingTestBase
    {
        [Fact]
        public void NoInitializers()
        {
            var source = @"
class C
{
    static int s1;
    int i1;
}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = null;
            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = null;

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        [Fact]
        public void ConstantInstanceInitializer()
        {
            var source = @"
class C
{
    static int s1;
    int i1 = 1;
}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = null;

            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("i1", "1", lineNumber: 4),
            };

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        [Fact]
        public void ConstantStaticInitializer()
        {
            var source = @"
class C
{
    static int s1 = 1;
    int i1;
}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("s1", "1", lineNumber: 3),
            };

            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = null;

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        [Fact]
        public void ExpressionInstanceInitializer()
        {
            var source = @"
class C
{
    static int s1;
    int i1 = 1 + Goo();

    static int Goo() { return 1; }
}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = null;

            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("i1", "1 + Goo()", lineNumber: 4),
            };

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        [Fact]
        public void ExpressionStaticInitializer()
        {
            var source = @"
class C
{
    static int s1 = 1 + Goo();
    int i1;

    static int Goo() { return 1; }
}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("s1", "1 + Goo()", lineNumber: 3),
            };

            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = null;

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        [Fact]
        public void InitializerOrder()
        {
            var source = @"
class C
{
    static int s1 = 1;
    static int s2 = 2;
    static int s3 = 3;
    int i1 = 1;
    int i2 = 2;
    int i3 = 3;
}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("s1", "1", lineNumber: 3),
                new ExpectedInitializer("s2", "2", lineNumber: 4),
                new ExpectedInitializer("s3", "3", lineNumber: 5),
            };

            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("i1", "1", lineNumber: 6),
                new ExpectedInitializer("i2", "2", lineNumber: 7),
                new ExpectedInitializer("i3", "3", lineNumber: 8),
            };

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        [Fact]
        public void AllPartialClasses()
        {
            var source = @"
partial class C
{
    static int s1 = 1;
    int i1 = 1;
}
partial class C
{
    static int s2 = 2;
    int i2 = 2;
}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("s1", "1", lineNumber: 3),
                new ExpectedInitializer("s2", "2", lineNumber: 8),
            };

            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("i1", "1", lineNumber: 4),
                new ExpectedInitializer("i2", "2", lineNumber: 9),
            };

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        [Fact]
        public void SomePartialClasses()
        {
            var source = @"
partial class C
{
    static int s1 = 1;
    int i1 = 1;
}
partial class C
{
    static int s2 = 2;
    int i2 = 2;
}
partial class C
{
    static int s3;
    int i3;
}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("s1", "1", lineNumber: 3),
                new ExpectedInitializer("s2", "2", lineNumber: 8),
            };

            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("i1", "1", lineNumber: 4),
                new ExpectedInitializer("i2", "2", lineNumber: 9),
            };

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        [Fact]
        public void Events()
        {
            var source = @"
class C
{
    static event System.Action e = MakeAction(1);
    event System.Action f = MakeAction(2);

    static System.Action MakeAction(int x) { return null; }
}}";

            IEnumerable<ExpectedInitializer> expectedStaticInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("e", "MakeAction(1)", lineNumber: 3),
            };

            IEnumerable<ExpectedInitializer> expectedInstanceInitializers = new ExpectedInitializer[]
            {
                new ExpectedInitializer("f", "MakeAction(2)", lineNumber: 4),
            };

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers);
        }

        private static void CompileAndCheckInitializers(string source, IEnumerable<ExpectedInitializer> expectedInstanceInitializers, IEnumerable<ExpectedInitializer> expectedStaticInitializers)
        {
            var compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees.First();
            var typeSymbol = (SourceNamedTypeSymbol)compilation.GlobalNamespace.GetMembers("C").Single();

            var boundInstanceInitializers = BindInitializersWithoutDiagnostics(typeSymbol, typeSymbol.InstanceInitializers);
            CheckBoundInitializers(expectedInstanceInitializers, syntaxTree, boundInstanceInitializers, isStatic: false);

            var boundStaticInitializers = BindInitializersWithoutDiagnostics(typeSymbol, typeSymbol.StaticInitializers);
            CheckBoundInitializers(expectedStaticInitializers, syntaxTree, boundStaticInitializers, isStatic: true);
        }

        private static void CheckBoundInitializers(IEnumerable<ExpectedInitializer> expectedInitializers, SyntaxTree syntaxTree, ImmutableArray<BoundInitializer> boundInitializers, bool isStatic)
        {
            if (expectedInitializers == null)
            {
                Assert.Equal(0, boundInitializers.Length);
            }
            else
            {
                Assert.True(!boundInitializers.IsEmpty, "Expected non-null non-empty bound initializers");

                int numInitializers = expectedInitializers.Count();

                Assert.Equal(numInitializers, boundInitializers.Length);

                int i = 0;
                foreach (var expectedInitializer in expectedInitializers)
                {
                    var boundInit = boundInitializers[i++];
                    Assert.Equal(BoundKind.FieldEqualsValue, boundInit.Kind);

                    var boundFieldInit = (BoundFieldEqualsValue)boundInit;

                    var initValueSyntax = boundFieldInit.Value.Syntax;
                    Assert.Same(initValueSyntax.Parent, boundInit.Syntax);
                    Assert.Equal(expectedInitializer.InitialValue, initValueSyntax.ToFullString());

                    var initValueLineNumber = syntaxTree.GetLineSpan(initValueSyntax.Span).StartLinePosition.Line;
                    Assert.Equal(expectedInitializer.LineNumber, initValueLineNumber);

                    Assert.Equal(expectedInitializer.FieldName, boundFieldInit.Field.Name);
                }
            }
        }

        private static ImmutableArray<BoundInitializer> BindInitializersWithoutDiagnostics(SourceNamedTypeSymbol typeSymbol, ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers)
        {
            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            ImportChain unused;
            var boundInitializers = ArrayBuilder<BoundInitializer>.GetInstance();
            Binder.BindRegularCSharpFieldInitializers(
                typeSymbol.DeclaringCompilation,
                initializers,
                boundInitializers,
                diagnostics,
                firstDebugImports: out unused);
            diagnostics.Verify();
            diagnostics.Free();
            return boundInitializers.ToImmutableAndFree();
        }

        private class ExpectedInitializer
        {
            public string FieldName { get; }
            public string InitialValue { get; }
            public int LineNumber { get; } //0-indexed

            public ExpectedInitializer(string fieldName, string initialValue, int lineNumber)
            {
                this.FieldName = fieldName;
                this.InitialValue = initialValue;
                this.LineNumber = lineNumber;
            }
        }
    }
}
