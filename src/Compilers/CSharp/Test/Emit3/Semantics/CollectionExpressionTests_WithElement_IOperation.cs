// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.CollectionExpressions)]
public sealed class CollectionExpressionTests_WithElement_IOperation : CSharpTestBase
{
    private const string s_collectionBuilderType = """
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Runtime.CompilerServices;

        [CollectionBuilder(typeof(MyHashSetBuilder), nameof(MyHashSetBuilder.Create))]
        class MyHashSet : IEnumerable<int>
        {
            public void Add(int item) { }

            IEnumerator<int> IEnumerable<int>.GetEnumerator() => null!;
            IEnumerator IEnumerable.GetEnumerator() => null;
        }

        class MyHashSetBuilder
        {
            public static MyHashSet Create(ReadOnlySpan<int> items) => null!;
            public static MyHashSet Create(int capacity, ReadOnlySpan<int> items) => null!;
            public static MyHashSet Create(IEqualityComparer<int> comparer, ReadOnlySpan<int> items) => null!;
            public static MyHashSet Create(int capacity, IEqualityComparer<int> comparer, ReadOnlySpan<int> items) => null!;
        }
        """;

    [Fact]
    public void TestArray_Empty()
    {
        string source = """
            class C
            {
                void M()
                {
                    int[] a = [with(), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (5,20): error CS9401: 'with(...)' elements are not supported for type 'int[]'
            //         int[] a = [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(5, 20));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Int32[], IsInvalid) (Syntax: '[with(), 1, 2, 3]')
            Elements(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestArray_SingleArg()
    {
        string source = """
            class C
            {
                void M()
                {
                    int[] a = [with(0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (5,20): error CS9401: 'with(...)' elements are not supported for type 'int[]'
            //         int[] a = [with(0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(5, 20));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Int32[], IsInvalid) (Syntax: '[with(0), 1, 2, 3]')
            Elements(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestArray_NamedArg()
    {
        string source = """
            class C
            {
                void M()
                {
                    int[] a = [with(capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (5,20): error CS9401: 'with(...)' elements are not supported for type 'int[]'
            //         int[] a = [with(capacity: 0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(5, 20));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Int32[], IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
            Elements(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestSpan_Empty()
    {
        string source = """
            using System;
            class C
            {
                void M()
                {
                    Span<int> a = [with(), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (6,24): error CS9401: 'with(...)' elements are not supported for type 'Span<int>'
            //         Span<int> a = [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(6, 24));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int32>, IsInvalid) (Syntax: '[with(), 1, 2, 3]')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestSpan_SingleArg()
    {
        string source = """
            using System;
            class C
            {
                void M()
                {
                    Span<int> a = [with(0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (6,24): error CS9401: 'with(...)' elements are not supported for type 'Span<int>'
            //         Span<int> a = [with(0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(6, 24));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int32>, IsInvalid) (Syntax: '[with(0), 1, 2, 3]')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestSpan_NamedArg()
    {
        string source = """
            using System;
            class C
            {
                void M()
                {
                    Span<int> a = [with(capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (6,24): error CS9401: 'with(...)' elements are not supported for type 'Span<int>'
            //         Span<int> a = [with(capacity: 0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(6, 24));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int32>, IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Theory]
    [InlineData("IEnumerable<System.Int32>")]
    [InlineData("IReadOnlyCollection<System.Int32>")]
    [InlineData("IReadOnlyList<System.Int32>")]
    public void TestReadOnlyInterface_Empty(string typeName)
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    {{typeName}} a = [with(), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}) (Syntax: '[with(), 1, 2, 3]')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Theory]
    [InlineData("IEnumerable<System.Int32>")]
    [InlineData("IReadOnlyCollection<System.Int32>")]
    [InlineData("IReadOnlyList<System.Int32>")]
    public void TestReadOnlyInterface_SingleArg(string typeName)
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    {{typeName}} a = [with(0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (6,40): error CS9403: 'with(...)' element for a read-only interface must be empty if present
            //         IEnumerable<System.Int32> a = [with(0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with"));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}, IsInvalid) (Syntax: '[with(0), 1, 2, 3]')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Theory]
    [InlineData("IEnumerable<System.Int32>")]
    [InlineData("IReadOnlyCollection<System.Int32>")]
    [InlineData("IReadOnlyList<System.Int32>")]
    public void TestReadOnlyInterface_NamedArg(string typeName)
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    {{typeName}} a = [with(capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (6,40): error CS9403: 'with(...)' element for a read-only interface must be empty if present
            //         IEnumerable<System.Int32> a = [with(capacity: 0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with"));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}, IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Theory]
    [InlineData("ICollection<System.Int32>")]
    [InlineData("IList<System.Int32>")]
    public void TestMutableInterface_Empty(string typeName)
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    {{typeName}} a = [with(), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}) (Syntax: '[with(), 1, 2, 3]')
            Elements(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Theory]
    [InlineData("ICollection<System.Int32>")]
    [InlineData("IList<System.Int32>")]
    public void TestMutableInterface_SingleArg(string typeName)
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    {{typeName}} a = [with(0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor(System.Int32 capacity)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}) (Syntax: '[with(0), 1, 2, 3]')
            ConstructArguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Elements(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Theory]
    [InlineData("ICollection<System.Int32>")]
    [InlineData("IList<System.Int32>")]
    public void TestMutableInterface_NamedArg(string typeName)
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    {{typeName}} a = [with(capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor(System.Int32 capacity)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}) (Syntax: '[with(capac ... ), 1, 2, 3]')
            ConstructArguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Elements(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Theory]
    [InlineData("ICollection<System.Int32>")]
    [InlineData("IList<System.Int32>")]
    public void TestMutableInterface_NamedArg_Incorrect(string typeName)
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    {{typeName}} a = [with(unknown: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (6,36): error CS1739: The best overload for 'List' does not have a parameter named 'unknown'
            //         ICollection<int> a = [with(unknown: 0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "unknown").WithArguments("List", "unknown"));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}, IsInvalid) (Syntax: '[with(unkno ... ), 1, 2, 3]')
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Theory]
    [InlineData("ICollection<System.Int32>")]
    [InlineData("IList<System.Int32>")]
    public void TestMutableInterface_MultipleArgs(string typeName)
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    {{typeName}} a = [with(0, 1), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (6,31): error CS1729: 'List<int>' does not contain a constructor that takes 2 arguments
            //         ICollection<int> a = [with(0, 1), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with").WithArguments("System.Collections.Generic.List<int>", "2"));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}, IsInvalid) (Syntax: '[with(0, 1), 1, 2, 3]')
              ConstructArguments(2):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestObjectCreation_Empty()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    HashSet<int> a = [with(), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(), 1, 2, 3]')
            Elements(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestObjectCreation_NamedArg()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    HashSet<int> a = [with(capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor(System.Int32 capacity)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(capac ... ), 1, 2, 3]')
              ConstructArguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestObjectCreation_SingleArg()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    HashSet<int> a = [with(0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor(System.Int32 capacity)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(0), 1, 2, 3]')
              ConstructArguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestObjectCreation_MultipleArgs()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    HashSet<int> a = [with(0, null), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32>? comparer)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(0, null), 1, 2, 3]')
            ConstructArguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'null')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand:
                      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Elements(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestObjectCreation_MultipleArgs_Named()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    HashSet<int> a = [with(capacity: 0, comparer: null), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32>? comparer)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(capac ... ), 1, 2, 3]')
              ConstructArguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'comparer: null')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestObjectCreation_MultipleArgs_Named_OutOfOrder()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    HashSet<int> a = [with(comparer: null, capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32>? comparer)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(compa ... ), 1, 2, 3]')
              ConstructArguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'comparer: null')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestObjectCreation_TooManyArgs()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    HashSet<int> a = [with(0, null, ""), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (6,27): error CS1729: 'HashSet<int>' does not contain a constructor that takes 3 arguments
            //         HashSet<int> a = [with(0, null, ""), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, @"with(0, null, """")").WithArguments("System.Collections.Generic.HashSet<int>", "3").WithLocation(6, 27));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid) (Syntax: '[with(0, nu ... ), 1, 2, 3]')
              ConstructArguments(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestObjectCreation_WrongArgType()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    HashSet<int> a = [with(""), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (6,32): error CS1503: Argument 1: cannot convert from 'string' to 'System.Collections.Generic.IEnumerable<int>'
            //         HashSet<int> a = [with(""), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadArgType, @"""""").WithArguments("1", "string", "System.Collections.Generic.IEnumerable<int>").WithLocation(6, 32));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid) (Syntax: '[with(""), 1, 2, 3]')
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_Empty()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(), 1, 2, 3]')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_SingleArg()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(0), 1, 2, 3]')
              ConstructArguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_SingleArg_Named()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(capac ... ), 1, 2, 3]')
              ConstructArguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_SingleArg_WrongName()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(unknown: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (5,29): error CS1739: The best overload for 'Create' does not have a parameter named 'unknown'
            //         MyHashSet a = [with(unknown: 0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "unknown").WithArguments("Create", "unknown").WithLocation(5, 29));
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyHashSet, IsInvalid) (Syntax: '[with(unkno ... ), 1, 2, 3]')
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_SingleArg_WrongType()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(capacity: ""), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (5,39): error CS1503: Argument 1: cannot convert from 'string' to 'int'
            //         MyHashSet a = [with(capacity: ""), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadArgType, @"""""").WithArguments("1", "string", "int").WithLocation(5, 39));
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyHashSet, IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_MultipleArgs()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(0, null), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32> comparer, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(0, null), 1, 2, 3]')
              ConstructArguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'null')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_MultipleArgs_Named()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(capacity: 0, comparer: null), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32> comparer, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(capac ... ), 1, 2, 3]')
              ConstructArguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'comparer: null')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_MultipleArgs_Named_OutOfOrder()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(comparer: null, capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32> comparer, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(compa ... ), 1, 2, 3]')
              ConstructArguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'comparer: null')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact]
    public void TestCollectionBuilder_MultipleArgs_TooManyArgs()
    {
        string source = """
            class C
            {
                void M()
                {
                    MyHashSet a = [with(0, null, ""), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation([source, s_collectionBuilderType], targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (5,24): error CS9405: No overload for method 'Create' takes 3 'with(...)' element arguments
            //         MyHashSet a = [with(0, null, ""), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, @"with(0, null, """")").WithArguments("Create", "3").WithLocation(5, 24));
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyHashSet, IsInvalid) (Syntax: '[with(0, nu ... ), 1, 2, 3]')
              ConstructArguments(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }
}
