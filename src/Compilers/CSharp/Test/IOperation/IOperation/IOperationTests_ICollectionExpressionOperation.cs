// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.CollectionExpressions)]
public sealed class IOperationTests_ICollectionExpressionOperation : CSharpTestBase
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

    private const string s_collectionBuilderOptionalConstructorArgType = """
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
            public static MyHashSet Create(int capacity = 42, ReadOnlySpan<int> items = default) => null!;
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
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Int32[] a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Int32[], IsInvalid) (Syntax: '[with(), 1, 2, 3]')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Int32[] a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: '[with(0), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Int32[], IsInvalid) (Syntax: '[with(0), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Int32[] a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Int32[], IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Span<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int32>, IsInvalid) (Syntax: '[with(), 1, 2, 3]')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Span<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: '[with(0), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int32>, IsInvalid) (Syntax: '[with(0), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Span<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Int32>, IsInvalid, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int32>, IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.{{typeName}} a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}) (Syntax: '[with(), 1, 2, 3]')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.{{typeName}} a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: '[with(0), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}, IsInvalid) (Syntax: '[with(0), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
        var operation = semanticModel.GetOperation(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.WithElement).AsNode()!);
        Assert.Null(operation);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.{{typeName}} a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}, IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.{{typeName}} a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}) (Syntax: '[with(), 1, 2, 3]')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.{{typeName}} a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: '[with(0), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
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
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.{{typeName}} a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.{{typeName}}, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
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
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.{{typeName}} a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: 'a = [with(u ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: 'a = [with(u ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: '[with(unkno ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}, IsInvalid) (Syntax: '[with(unkno ... ), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, $$"""
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.{{typeName}} a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.{{typeName}}, IsInvalid, IsImplicit) (Syntax: '[with(0, 1), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}, IsInvalid) (Syntax: '[with(0, 1), 1, 2, 3]')
                                  ConstructArguments(2):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.HashSet<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(), 1, 2, 3]')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.HashSet<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
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
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
            """);
    }

    [Fact]
    public void TestObjectCreation_OptionalArg()
    {
        string source = $$"""
            using System.Collections.Generic;

            class MyCollection<T> : List<T>
            {
                public MyCollection(int capacity = 42) : base(capacity) { }
            }

            class C
            {
                void M()
                {
                    MyCollection<int> a = [with(), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: MyCollection<System.Int32>..ctor([System.Int32 capacity = 42])) (OperationKind.CollectionExpression, Type: MyCollection<System.Int32>) (Syntax: '[with(), 1, 2, 3]')
              ConstructArguments(1):
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: capacity) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsImplicit) (Syntax: 'with()')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyCollection<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: MyCollection<System.Int32>..ctor([System.Int32 capacity = 42])) (OperationKind.CollectionExpression, Type: MyCollection<System.Int32>) (Syntax: '[with(), 1, 2, 3]')
                                  ConstructArguments(1):
                                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: capacity) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsImplicit) (Syntax: 'with()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.HashSet<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: '[with(0), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
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
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.HashSet<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: '[with(0, null), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32>? comparer)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(0, null), 1, 2, 3]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'null')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            (ImplicitReference)
                                          Operand:
                                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.HashSet<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32>? comparer)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'comparer: null')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            (ImplicitReference)
                                          Operand:
                                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.HashSet<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.HashSet<System.Int32>, IsImplicit) (Syntax: '[with(compa ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.HashSet<System.Int32>..ctor(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32>? comparer)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>) (Syntax: '[with(compa ... ), 1, 2, 3]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'comparer: null')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            (ImplicitReference)
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
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.HashSet<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid, IsImplicit) (Syntax: '[with(0, nu ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid) (Syntax: '[with(0, nu ... ), 1, 2, 3]')
                                  ConstructArguments(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                                      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.HashSet<System.Int32> a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(""), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'a = [with(""), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid, IsImplicit) (Syntax: '[with(""), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.HashSet<System.Int32>, IsInvalid) (Syntax: '[with(""), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                    ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with()')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(), 1, 2, 3]')
                                  ConstructArguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
            """);
    }

    [Fact]
    public void TestCollectionBuilder_OptionalArg()
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
        var comp = CreateCompilation([source, s_collectionBuilderOptionalConstructorArgType], targetFramework: TargetFramework.Net90).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.First().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create([System.Int32 capacity = 42], [System.ReadOnlySpan<System.Int32> items = default(System.ReadOnlySpan<System.Int32>)])) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(), 1, 2, 3]')
              ConstructArguments(2):
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: capacity) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsImplicit) (Syntax: 'with()')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                    ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with()')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create([System.Int32 capacity = 42], [System.ReadOnlySpan<System.Int32> items = default(System.ReadOnlySpan<System.Int32>)])) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(), 1, 2, 3]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: capacity) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsImplicit) (Syntax: 'with()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(0)')
                    ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(0)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsImplicit) (Syntax: '[with(0), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(0), 1, 2, 3]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(0)')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(0)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(capacity: 0)')
                    ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(capacity: 0)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(capacity: 0)')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(capacity: 0)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: 'a = [with(u ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: 'a = [with(u ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: '[with(unkno ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyHashSet, IsInvalid) (Syntax: '[with(unkno ... ), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyHashSet, IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(3):
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
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(0, null)')
                    ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(0, null)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsImplicit) (Syntax: '[with(0, null), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32> comparer, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(0, null), 1, 2, 3]')
                                  ConstructArguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: '0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'null')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            (ImplicitReference)
                                          Operand:
                                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(0, null)')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(0, null)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(3):
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
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(capaci ... arer: null)')
                    ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(capaci ... arer: null)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32> comparer, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'comparer: null')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            (ImplicitReference)
                                          Operand:
                                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(capaci ... arer: null)')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(capaci ... arer: null)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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
              ConstructArguments(3):
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
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(compar ... apacity: 0)')
                    ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(compar ... apacity: 0)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsImplicit) (Syntax: '[with(compa ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: MyHashSet MyHashSetBuilder.Create(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<System.Int32> comparer, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyHashSet) (Syntax: '[with(compa ... ), 1, 2, 3]')
                                  ConstructArguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: comparer) (OperationKind.Argument, Type: null) (Syntax: 'comparer: null')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEqualityComparer<System.Int32>, Constant: null, IsImplicit) (Syntax: 'null')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                            (ImplicitReference)
                                          Operand:
                                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(compar ... apacity: 0)')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(compar ... apacity: 0)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
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

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyHashSet a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: 'a = [with(0 ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyHashSet, IsInvalid, IsImplicit) (Syntax: '[with(0, nu ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyHashSet, IsInvalid) (Syntax: '[with(0, nu ... ), 1, 2, 3]')
                                  ConstructArguments(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                                      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
            """);
    }

    [Fact]
    public void TestTypeArgument_Empty()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M<T>() where T : IList<int>, new()
                {
                    T a = [with(), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics();
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: T) (Syntax: '[with(), 1, 2, 3]')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [T a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: T, IsImplicit) (Syntax: 'a = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: T) (Syntax: '[with(), 1, 2, 3]')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
            """);
    }

    [Fact]
    public void TestTypeArgument_SingleArg()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M<T>() where T : IList<int>, new()
                {
                    T a = [with(0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (6,16): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
            //         T a = [with(0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "with(0)").WithArguments("T").WithLocation(6, 16));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: T, IsInvalid) (Syntax: '[with(0), 1, 2, 3]')
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [T a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T, IsInvalid, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'a = [with(0), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsInvalid, IsImplicit) (Syntax: '[with(0), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: T, IsInvalid) (Syntax: '[with(0), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
            """);
    }

    [Fact]
    public void TestTypeArgument_NamedArg()
    {
        string source = $$"""
            using System.Collections.Generic;
            class C
            {
                void M<T>() where T : IList<int>, new()
                {
                    T a = [with(capacity: 0), 1, 2, 3];
                }
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (6,16): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
            //         T a = [with(capacity: 0), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "with(capacity: 0)").WithArguments("T").WithLocation(6, 16));
        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), $$"""
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: T, IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);

        var tree = comp.SyntaxTrees[0];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "M");
        VerifyFlowGraph(comp, method, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [T a]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T, IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'a = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsInvalid, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: T, IsInvalid) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
            """);
    }

    [Fact]
    public void ControlFlow_ObjectCreation()
    {
        var source = """
            using System.Collections.Generic;
            class Program
            {
                static void Main(string[] args)
                {
                    IList<int> y = [with(ComputeCapacity()), args.Length == 0 ? TrueBranch() : FalseBranch()];
                }

                static int ComputeCapacity() => 0;
                static int TrueBranch() => 1;
                static int FalseBranch() => 2;
            }
            """;

        var verifier = CompileAndVerify(source);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().Single(), semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.IList<System.Int32> y]
                CaptureIds: [0] [1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'ComputeCapacity()')
                          Value:
                            IInvocationOperation (System.Int32 Program.ComputeCapacity()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'ComputeCapacity()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Jump if False (Regular) to Block[B3]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 0')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: 'y = [with(C ... seBranch()]')
                          Left:
                            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: 'y = [with(C ... seBranch()]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: '[with(Compu ... seBranch()]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (1 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor(System.Int32 capacity)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IList<System.Int32>) (Syntax: '[with(Compu ... seBranch()]')
                                  ConstructArguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'ComputeCapacity()')
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'ComputeCapacity()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(1):
                                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... lseBranch()')
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4]
                Statements (0)
            """, graph, symbol);
    }

    [Fact]
    public void ControlFlow_BuilderA()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items) {
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(int capacity, ReadOnlySpan<T> items)
                {
                    return new(items);
                }
            }
            """;
        string sourceB = """
            class Program
            {
                static void Main(string[] args)
                {
                    MyCollection<int> c = [with(ComputeCapacity()), args.Length == 0 ? TrueBranch() : FalseBranch()];
                }
            
                static int ComputeCapacity() => 0;
                static int TrueBranch() => 1;
                static int FalseBranch() => 2;
            }
            """;
        var verifier = CompileAndVerify([sourceA, sourceB], targetFramework: TargetFramework.Net80, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().Single(), semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyCollection<System.Int32> c]
                CaptureIds: [0] [1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'ComputeCapacity()')
                          Value:
                            IInvocationOperation (System.Int32 Program.ComputeCapacity()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'ComputeCapacity()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Jump if False (Regular) to Block[B3]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 0')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(C ... seBranch()]')
                          Left:
                            ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(C ... seBranch()]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: '[with(Compu ... seBranch()]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (1 elements, ConstructMethod: MyCollection<System.Int32> MyBuilder.Create<System.Int32>(System.Int32 capacity, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyCollection<System.Int32>) (Syntax: '[with(Compu ... seBranch()]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'ComputeCapacity()')
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'ComputeCapacity()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(ComputeCapacity())')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(ComputeCapacity())')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(1):
                                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... lseBranch()')
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4]
                Statements (0)
            """, graph, symbol);
    }

    [Fact]
    public void ControlFlow_BuilderB()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items) {
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(int capacity, ReadOnlySpan<T> items)
                {
                    return new(items);
                }
            }
            """;
        string sourceB = """
            class Program
            {
                static void Main(string[] args)
                {
                    MyCollection<int> c = [with(args.Length == 0 ? TrueBranch() : FalseBranch()), ComputeCapacity()];
                }
            
                static int ComputeCapacity() => 0;
                static int TrueBranch() => 1;
                static int FalseBranch() => 2;
            }
            """;
        var verifier = CompileAndVerify([sourceA, sourceB], targetFramework: TargetFramework.Net80, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().Single(), semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyCollection<System.Int32> c]
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Jump if False (Regular) to Block[B3]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 0')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... Capacity()]')
                          Left:
                            ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... Capacity()]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: '[with(args. ... Capacity()]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (1 elements, ConstructMethod: MyCollection<System.Int32> MyBuilder.Create<System.Int32>(System.Int32 capacity, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyCollection<System.Int32>) (Syntax: '[with(args. ... Capacity()]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'args.Length ... lseBranch()')
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... lseBranch()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(args.L ... seBranch())')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(args.L ... seBranch())')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(1):
                                      IInvocationOperation (System.Int32 Program.ComputeCapacity()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'ComputeCapacity()')
                                        Instance Receiver:
                                          null
                                        Arguments(0)
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4]
                Statements (0)
            """, graph, symbol);
    }

    [Fact]
    public void ControlFlow_BuilderC()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items) {
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(int capacity, ReadOnlySpan<T> items)
                {
                    return new(items);
                }
            }
            """;
        string sourceB = """
            class Program
            {
                static void Main(string[] args)
                {
                    MyCollection<int> c = [with(args.Length == 0 ? TrueBranch() : FalseBranch()), args.Length == 1 ? FalseBranch() : TrueBranch()];
                }
            
                static int ComputeCapacity() => 0;
                static int TrueBranch() => 1;
                static int FalseBranch() => 2;
            }
            """;
        var verifier = CompileAndVerify([sourceA, sourceB], targetFramework: TargetFramework.Net80, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().Single(), semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyCollection<System.Int32> c]
                CaptureIds: [0] [1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Jump if False (Regular) to Block[B3]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 0')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B6]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 1')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B7]
                Block[B6] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B5] [B6]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... ueBranch()]')
                          Left:
                            ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... ueBranch()]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: '[with(args. ... ueBranch()]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (1 elements, ConstructMethod: MyCollection<System.Int32> MyBuilder.Create<System.Int32>(System.Int32 capacity, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyCollection<System.Int32>) (Syntax: '[with(args. ... ueBranch()]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'args.Length ... lseBranch()')
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... lseBranch()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(args.L ... seBranch())')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(args.L ... seBranch())')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(1):
                                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... rueBranch()')
                    Next (Regular) Block[B8]
                        Leaving: {R1}
            }
            Block[B8] - Exit
                Predecessors: [B7]
                Statements (0)
            """, graph, symbol);
    }

    [Fact]
    public void ControlFlow_BuilderD()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items) {
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(int arg1, int arg2, ReadOnlySpan<T> items)
                {
                    return new(items);
                }
            }
            """;
        string sourceB = """
            class Program
            {
                static void Main(string[] args)
                {
                    MyCollection<int> c = [with(args.Length == 0 ? TrueBranch() : FalseBranch(), args.Length == 1 ? FalseBranch() : TrueBranch())];
                }
            
                static int ComputeCapacity() => 0;
                static int TrueBranch() => 1;
                static int FalseBranch() => 2;
            }
            """;
        var verifier = CompileAndVerify([sourceA, sourceB], targetFramework: TargetFramework.Net80, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().Single(), semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyCollection<System.Int32> c]
                CaptureIds: [0] [1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Jump if False (Regular) to Block[B3]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 0')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B6]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 1')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B7]
                Block[B6] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B5] [B6]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... eBranch())]')
                          Left:
                            ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... eBranch())]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: '[with(args. ... eBranch())]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (0 elements, ConstructMethod: MyCollection<System.Int32> MyBuilder.Create<System.Int32>(System.Int32 arg1, System.Int32 arg2, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyCollection<System.Int32>) (Syntax: '[with(args. ... eBranch())]')
                                  ConstructArguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: null) (Syntax: 'args.Length ... lseBranch()')
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... lseBranch()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg2) (OperationKind.Argument, Type: null) (Syntax: 'args.Length ... rueBranch()')
                                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... rueBranch()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(args.L ... ueBranch())')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(args.L ... ueBranch())')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(0)
                    Next (Regular) Block[B8]
                        Leaving: {R1}
            }
            Block[B8] - Exit
                Predecessors: [B7]
                Statements (0)
            """, graph, symbol);
    }

    [Fact]
    public void ControlFlow_BuilderE()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items) {
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(int arg1, int arg2, ReadOnlySpan<T> items)
                {
                    return new(items);
                }
            }
            """;
        string sourceB = """
            class Program
            {
                static void Main(string[] args)
                {
                    MyCollection<int> c = [with(arg2: args.Length == 0 ? TrueBranch() : FalseBranch(), arg1: args.Length == 1 ? FalseBranch() : TrueBranch())];
                }
            
                static int ComputeCapacity() => 0;
                static int TrueBranch() => 1;
                static int FalseBranch() => 2;
            }
            """;
        var verifier = CompileAndVerify([sourceA, sourceB], targetFramework: TargetFramework.Net80, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().Single(), semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyCollection<System.Int32> c]
                CaptureIds: [0] [1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Jump if False (Regular) to Block[B3]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 0')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B6]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 1')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B7]
                Block[B6] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B5] [B6]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... eBranch())]')
                          Left:
                            ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... eBranch())]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: '[with(arg2: ... eBranch())]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (0 elements, ConstructMethod: MyCollection<System.Int32> MyBuilder.Create<System.Int32>(System.Int32 arg1, System.Int32 arg2, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyCollection<System.Int32>) (Syntax: '[with(arg2: ... eBranch())]')
                                  ConstructArguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg2) (OperationKind.Argument, Type: null) (Syntax: 'arg2: args. ... lseBranch()')
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... lseBranch()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: null) (Syntax: 'arg1: args. ... rueBranch()')
                                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... rueBranch()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(arg2:  ... ueBranch())')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(arg2:  ... ueBranch())')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(0)
                    Next (Regular) Block[B8]
                        Leaving: {R1}
            }
            Block[B8] - Exit
                Predecessors: [B7]
                Statements (0)
            """, graph, symbol);
    }

    [Fact]
    public void ControlFlow_BuilderF()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items) {
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(int arg1, int arg2, ReadOnlySpan<T> items)
                {
                    return new(items);
                }
            }
            """;
        string sourceB = """
            class Program
            {
                static void Main(string[] args)
                {
                    MyCollection<int> c = [with(arg2: args.Length == 0 ? TrueBranch() : FalseBranch(), arg1: args.Length == 1 ? FalseBranch() : TrueBranch()), args.Length == 2 ? ComputeCapacity() : (ComputeCapacity() + 1)];
                }
            
                static int ComputeCapacity() => 0;
                static int TrueBranch() => 1;
                static int FalseBranch() => 2;
            }
            """;
        var verifier = CompileAndVerify([sourceA, sourceB], targetFramework: TargetFramework.Net80, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().Single(), semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyCollection<System.Int32> c]
                CaptureIds: [0] [1] [2]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Jump if False (Regular) to Block[B3]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 0')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B6]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 1')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'FalseBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.FalseBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'FalseBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B7]
                Block[B6] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'TrueBranch()')
                          Value:
                            IInvocationOperation (System.Int32 Program.TrueBranch()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'TrueBranch()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B5] [B6]
                    Statements (0)
                    Jump if False (Regular) to Block[B9]
                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'args.Length == 2')
                          Left:
                            IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                              Instance Receiver:
                                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    Next (Regular) Block[B8]
                Block[B8] - Block
                    Predecessors: [B7]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'ComputeCapacity()')
                          Value:
                            IInvocationOperation (System.Int32 Program.ComputeCapacity()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'ComputeCapacity()')
                              Instance Receiver:
                                null
                              Arguments(0)
                    Next (Regular) Block[B10]
                Block[B9] - Block
                    Predecessors: [B7]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'ComputeCapacity() + 1')
                          Value:
                            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32) (Syntax: 'ComputeCapacity() + 1')
                              Left:
                                IInvocationOperation (System.Int32 Program.ComputeCapacity()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'ComputeCapacity()')
                                  Instance Receiver:
                                    null
                                  Arguments(0)
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    Next (Regular) Block[B10]
                Block[B10] - Block
                    Predecessors: [B8] [B9]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... ity() + 1)]')
                          Left:
                            ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: 'c = [with(a ... ity() + 1)]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<System.Int32>, IsImplicit) (Syntax: '[with(arg2: ... ity() + 1)]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (1 elements, ConstructMethod: MyCollection<System.Int32> MyBuilder.Create<System.Int32>(System.Int32 arg1, System.Int32 arg2, System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyCollection<System.Int32>) (Syntax: '[with(arg2: ... ity() + 1)]')
                                  ConstructArguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg2) (OperationKind.Argument, Type: null) (Syntax: 'arg2: args. ... lseBranch()')
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... lseBranch()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: null) (Syntax: 'arg1: args. ... rueBranch()')
                                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... rueBranch()')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(arg2:  ... ueBranch())')
                                        ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'with(arg2:  ... ueBranch())')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(1):
                                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length ... city() + 1)')
                    Next (Regular) Block[B11]
                        Leaving: {R1}
            }
            Block[B11] - Exit
                Predecessors: [B10]
                Statements (0)
            """, graph, symbol);
    }

    [Fact]
    public void TestErrorRecovery_Constructor_NullArg()
    {
        string sourceA = $$"""
            using System.Collections.Generic;

            class MyList<T> : List<T> { }

            class Program
            {
                static void Main()
                {
                    MyList<int> s = [with(null)];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (9,26): error CS1729: 'MyList<int>' does not contain a constructor that takes 1 arguments
            //         MyList<int> s = [with(null)];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with(null)").WithArguments("MyList<int>", "1").WithLocation(9, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(null)]')
              ConstructArguments(1):
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_Constructor_ImplicitObjectArg()
    {
        string sourceA = $$"""
            using System.Collections.Generic;

            class MyList<T> : List<T> { }

            class Program
            {
                static void Main()
                {
                    MyList<int> s = [with(new())];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (9,26): error CS1729: 'MyList<int>' does not contain a constructor that takes 1 arguments
            //         MyList<int> s = [with(new())];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with(new())").WithArguments("MyList<int>", "1").WithLocation(9, 26),
            // (9,31): error CS8754: There is no target type for 'new()'
            //         MyList<int> s = [with(new())];
            Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(9, 31));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(new())]')
              ConstructArguments(1):
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new()')
                    Children(0)
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_Constructor_LambdaArg()
    {
        string sourceA = $$"""
            using System.Collections.Generic;

            class MyList<T> : List<T> { }

            class Program
            {
                static void Main()
                {
                    MyList<int> s = [with(a => a)];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (9,26): error CS1729: 'MyList<int>' does not contain a constructor that takes 1 arguments
            //         MyList<int> s = [with(a => a)];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with(a => a)").WithArguments("MyList<int>", "1").WithLocation(9, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(a => a)]')
              ConstructArguments(1):
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'a => a')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                        ReturnedValue:
                          IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: ?, IsInvalid) (Syntax: 'a')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_Constructor_TypedLambdaArg()
    {
        string sourceA = $$"""
            using System.Collections.Generic;

            class MyList<T> : List<T> { }

            class Program
            {
                static void Main()
                {
                    MyList<int> s = [with((int a) => a)];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (9,26): error CS1729: 'MyList<int>' does not contain a constructor that takes 1 arguments
            //         MyList<int> s = [with((int a) => a)];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with((int a) => a)").WithArguments("MyList<int>", "1").WithLocation(9, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with((int a) => a)]')
              ConstructArguments(1):
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '(int a) => a')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                        ReturnedValue:
                          IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'a')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_Constructor_SwitchArg()
    {
        string sourceA = $$"""
            using System.Collections.Generic;

            class MyList<T> : List<T> { }

            class Program
            {
                static void Main()
                {
                    int a = 0;
                    MyList<int> s = [with(a switch { _ => null })];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (10,26): error CS1729: 'MyList<int>' does not contain a constructor that takes 1 arguments
            //         MyList<int> s = [with(a switch { _ => null })];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with(a switch { _ => null })").WithArguments("MyList<int>", "1").WithLocation(10, 26),
            // (10,33): error CS8506: No best type was found for the switch expression.
            //         MyList<int> s = [with(a switch { _ => null })];
            Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(10, 33));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(a swi ... => null })]')
              ConstructArguments(1):
                  ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: ?, IsInvalid) (Syntax: 'a switch { _ => null }')
                    Value:
                      ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'a')
                    Arms(1):
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => null')
                          Pattern:
                            IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_Constructor_ConditionalArg()
    {
        string sourceA = $$"""
            using System.Collections.Generic;

            class MyList<T> : List<T> { }

            class Program
            {
                static void Main()
                {
                    bool a = true;
                    MyList<int> s = [with(a ? null : null)];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (10,26): error CS1729: 'MyList<int>' does not contain a constructor that takes 1 arguments
            //         MyList<int> s = [with(a ? null : null)];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with(a ? null : null)").WithArguments("MyList<int>", "1").WithLocation(10, 26),
            // (10,31): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between '<null>' and '<null>'
            //         MyList<int> s = [with(a ? null : null)];
            Diagnostic(ErrorCode.ERR_InvalidQM, "a ? null : null").WithArguments("<null>", "<null>").WithLocation(10, 31));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(a ? null : null)]')
              ConstructArguments(1):
                  IConditionalOperation (OperationKind.Conditional, Type: ?, IsInvalid) (Syntax: 'a ? null : null')
                    Condition:
                      ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')
                    WhenTrue:
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                    WhenFalse:
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_Constructor_CollectionArg()
    {
        string sourceA = $$"""
            using System.Collections.Generic;

            class MyList<T> : List<T> { }

            class Program
            {
                static void Main()
                {
                    bool a = true;
                    MyList<int> s = [with([a])];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (10,26): error CS1729: 'MyList<int>' does not contain a constructor that takes 1 arguments
            //         MyList<int> s = [with([a])];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with([a])").WithArguments("MyList<int>", "1").WithLocation(10, 26),
            // (10,31): error CS9176: There is no target type for the collection expression.
            //         MyList<int> s = [with([a])];
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[a]").WithLocation(10, 31));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with([a])]')
              ConstructArguments(1):
                  ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: ?, IsInvalid) (Syntax: '[a]')
                    Elements(1):
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_CollectionBuilder_NullArg()
    {
        string sourceA = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyList<T> : List<T> { }

            class MyBuilder
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> items) => new();
            }

            class Program
            {
                static void Main()
                {
                    MyList<int> s = [with(null)];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (17,26): error CS9358: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         MyList<int> s = [with(null)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(null)").WithArguments("Create", "1").WithLocation(17, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: MyList<System.Int32> MyBuilder.Create<System.Int32>(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(null)]')
              ConstructArguments(2):
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                  ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'with(null)')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_CollectionBuilder_ImplicitObjectArg()
    {
        string sourceA = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyList<T> : List<T> { }
            
            class MyBuilder
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> items) => new();
            }

            class Program
            {
                static void Main()
                {
                    MyList<int> s = [with(new())];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (17,26): error CS9358: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         MyList<int> s = [with(new())];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(new())").WithArguments("Create", "1").WithLocation(17, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: MyList<System.Int32> MyBuilder.Create<System.Int32>(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(new())]')
              ConstructArguments(2):
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new()')
                    Children(0)
                  ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'with(new())')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_CollectionBuilder_LambdaArg()
    {
        string sourceA = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyList<T> : List<T> { }
            
            class MyBuilder
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> items) => new();
            }

            class Program
            {
                static void Main()
                {
                    MyList<int> s = [with(a => a)];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (17,26): error CS9358: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         MyList<int> s = [with(a => a)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(a => a)").WithArguments("Create", "1").WithLocation(17, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: MyList<System.Int32> MyBuilder.Create<System.Int32>(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(a => a)]')
              ConstructArguments(2):
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'a => a')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                        ReturnedValue:
                          IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: ?, IsInvalid) (Syntax: 'a')
                  ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'with(a => a)')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_CollectionBuilder_TypedLambdaArg()
    {
        string sourceA = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyList<T> : List<T> { }
            
            class MyBuilder
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> items) => new();
            }

            class Program
            {
                static void Main()
                {
                    MyList<int> s = [with((int a) => a)];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (17,26): error CS9358: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         MyList<int> s = [with((int a) => a)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with((int a) => a)").WithArguments("Create", "1").WithLocation(17, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: MyList<System.Int32> MyBuilder.Create<System.Int32>(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with((int a) => a)]')
              ConstructArguments(2):
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '(int a) => a')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                        ReturnedValue:
                          IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'a')
                  ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'with((int a) => a)')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_CollectionBuilder_SwitchArg()
    {
        string sourceA = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyList<T> : List<T> { }
            
            class MyBuilder
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> items) => new();
            }

            class Program
            {
                static void Main()
                {
                    int a = 0;
                    MyList<int> s = [with(a switch { _ => null })];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (18,26): error CS9358: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         MyList<int> s = [with(a switch { _ => null })];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(a switch { _ => null })").WithArguments("Create", "1").WithLocation(18, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: MyList<System.Int32> MyBuilder.Create<System.Int32>(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(a swi ... => null })]')
              ConstructArguments(2):
                  ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: ?, IsInvalid) (Syntax: 'a switch { _ => null }')
                    Value:
                      ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'a')
                    Arms(1):
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => null')
                          Pattern:
                            IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                  ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'with(a swit ...  => null })')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_CollectionBuilder_ConditionalArg()
    {
        string sourceA = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyList<T> : List<T> { }
            
            class MyBuilder
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> items) => new();
            }

            class Program
            {
                static void Main()
                {
                    bool a = true;
                    MyList<int> s = [with(a ? null : null)];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (18,26): error CS9358: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         MyList<int> s = [with(a ? null : null)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(a ? null : null)").WithArguments("Create", "1").WithLocation(18, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: MyList<System.Int32> MyBuilder.Create<System.Int32>(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with(a ? null : null)]')
              ConstructArguments(2):
                  IConditionalOperation (OperationKind.Conditional, Type: ?, IsInvalid) (Syntax: 'a ? null : null')
                    Condition:
                      ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')
                    WhenTrue:
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                    WhenFalse:
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                  ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'with(a ? null : null)')
              Elements(0)
            """);
    }

    [Fact]
    public void TestErrorRecovery_CollectionBuilder_CollectionArg()
    {
        string sourceA = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyList<T> : List<T> { }
            
            class MyBuilder
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> items) => new();
            }

            class Program
            {
                static void Main()
                {
                    bool a = true;
                    MyList<int> s = [with([a])];
                }
            }
            """;

        var comp = CreateCompilation(sourceA, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (18,26): error CS9358: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         MyList<int> s = [with([a])];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with([a])").WithArguments("Create", "1").WithLocation(18, 26));

        comp.VerifyOperationTree(comp.SyntaxTrees.Single().FindNodeOrTokenByKind(SyntaxKind.CollectionExpression).AsNode(), """
            ICollectionExpressionOperation (0 elements, ConstructMethod: MyList<System.Int32> MyBuilder.Create<System.Int32>(System.ReadOnlySpan<System.Int32> items)) (OperationKind.CollectionExpression, Type: MyList<System.Int32>, IsInvalid) (Syntax: '[with([a])]')
              ConstructArguments(2):
                  ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: ?, IsInvalid) (Syntax: '[a]')
                    Elements(1):
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')
                  ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'with([a])')
              Elements(0)
            """);
    }
}
