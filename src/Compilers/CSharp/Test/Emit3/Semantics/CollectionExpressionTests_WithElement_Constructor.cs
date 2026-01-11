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
public sealed class CollectionExpressionTests_WithElement_Constructors : CSharpTestBase
{
    private static string IncludeExpectedOutput(string expectedOutput) => expectedOutput;

    #region Position and Multiple With Tests

    [Fact]
    public void WithElement_MustBeFirstElement()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [1, with(capacity: 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,30): error CS9335: Collection argument element must be the first element.
            //         List<int> list = [1, with(capacity: 10)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(7, 30));
    }

    [Fact]
    public void WithElement_CannotAppearMultipleTimes()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with(capacity: 10), with(capacity: 20)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,47): error CS9335: Collection argument element must be the first element.
            //         List<int> list = [with(capacity: 10), with(capacity: 20)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(7, 47));
    }

    [Fact]
    public void WithElement_CannotAppearAfterElements()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [1, 2, with(capacity: 10), 3];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,33): error CS9335: Collection argument element must be the first element.
            //         List<int> list = [1, 2, with(capacity: 10), 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(7, 33));
    }

    [Fact]
    public void WithElement_CannotAppearAfterSpread()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    var other = new int[] { 1, 2 };
                    List<int> list = [.. other, with(capacity: 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (8,37): error CS9335: Collection argument element must be the first element.
            //         List<int> list = [.. other, with(capacity: 10)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 37));
    }

    [Fact]
    public void WithElement_ValidAsFirstElement()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(capacity: 10), 1, 2, 3];
                    Console.WriteLine(list.Capacity);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10")).VerifyIL(
            "C.Main",
            """
            {
              // Code size       39 (0x27)
              .maxstack  3
              IL_0000:  ldc.i4.s   10
              IL_0002:  newobj     "System.Collections.Generic.List<int>..ctor(int)"
              IL_0007:  dup
              IL_0008:  ldc.i4.1
              IL_0009:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
              IL_000e:  dup
              IL_000f:  ldc.i4.2
              IL_0010:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
              IL_0015:  dup
              IL_0016:  ldc.i4.3
              IL_0017:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
              IL_001c:  callvirt   "int System.Collections.Generic.List<int>.Capacity.get"
              IL_0021:  call       "void System.Console.WriteLine(int)"
              IL_0026:  ret
            }
            """);
    }

    #endregion

    #region Basic Constructor Tests

    [Fact]
    public void WithElement_ParameterlessConstructor()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(), 1, 2, 3];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("3"));
    }

    [Fact]
    public void WithElement_SingleParameterConstructor()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(capacity: 100), 1, 2, 3];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("3"));
    }

    [Fact]
    public void WithElement_MultipleParameterConstructor()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int CustomProperty { get; }
                
                public MyList(int capacity, int customValue) : base(capacity)
                {
                    CustomProperty = customValue;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(capacity: 100, customValue: 42), 1, 2];
                    Console.WriteLine($"{list.Count},{list.CustomProperty}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2,42"));
    }

    [Fact]
    public void WithElement_ExecutedBeforeElements()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int CustomProperty { get; }
                
                public MyList(int capacity, int customValue) : base(capacity)
                {
                    CustomProperty = customValue;
                    Console.Write("ctor called. ");
                }

                public void Add(T value)
                {
                    Console.Write("add called. ");
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(capacity: 100, customValue: 42), 1, 2];
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("ctor called. add called. add called. "));
    }

    #endregion

    #region Argument Count Tests

    [Fact]
    public void WithElement_NonExistentNamedParameter()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with(capacity: 10, extraArg: 20)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,46): error CS1739: The best overload for 'List' does not have a parameter named 'extraArg'
            //         List<int> list = [with(capacity: 10, extraArg: 20)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "extraArg").WithArguments("List", "extraArg").WithLocation(7, 46));
    }

    [Fact]
    public void WithElement_TooFewArguments_RequiredParameter()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int capacity, string name) : base(capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(capacity: 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,29): error CS7036: There is no argument given that corresponds to the required parameter 'name' of 'MyList<int>.MyList(int, string)'
            //         MyList<int> list = [with(capacity: 10)];
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "with(capacity: 10)").WithArguments("name", "MyList<int>.MyList(int, string)").WithLocation(12, 29));
    }

    [Fact]
    public void WithElement_RequiredProperties()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public required int RequiredProp { get; init; }

                public MyList(int capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(capacity: 10)];
                }
            }
            """;

        CreateCompilation([source, IsExternalInitTypeDefinition, RequiredMemberAttribute, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
            // (14,29): error CS9035: Required member 'MyList<int>.RequiredProp' must be set in the object initializer or attribute constructor.
            //         MyList<int> list = [with(capacity: 10)];
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "with(capacity: 10)").WithArguments("MyList<int>.RequiredProp").WithLocation(14, 29));
    }

    [Fact]
    public void WithElement_OptionalParameters()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Name { get; }
                
                public MyList(int capacity = 0, string name = "default") : base(capacity)
                {
                    Name = name;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list1 = [with(), 1];
                    MyList<int> list2 = [with(capacity: 10), 2];
                    MyList<int> list3 = [with(name: "custom"), 3];
                    MyList<int> list4 = [with(capacity: 20, name: "both"), 4];
                    
                    Console.WriteLine($"{list1.Name}-{list1.Capacity},{list2.Name}-{list2.Capacity},{list3.Name}-{list3.Capacity},{list4.Name}-{list4.Capacity}");
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("default-4,default-10,custom-4,both-20"));
        var compilation = (CSharpCompilation)verifier.Compilation;

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var root = semanticModel.SyntaxTree.GetRoot();

        var withElements = root.DescendantNodes().OfType<WithElementSyntax>().ToArray();
        Assert.Equal(4, withElements.Length);

        var constructor1 = (IMethodSymbol?)semanticModel.GetSymbolInfo(withElements[0]).Symbol;
        var constructor2 = (IMethodSymbol?)semanticModel.GetSymbolInfo(withElements[1]).Symbol;
        var constructor3 = (IMethodSymbol?)semanticModel.GetSymbolInfo(withElements[2]).Symbol;
        var constructor4 = (IMethodSymbol?)semanticModel.GetSymbolInfo(withElements[3]).Symbol;

        Assert.NotNull(constructor1);
        Assert.NotNull(constructor2);
        Assert.NotNull(constructor3);
        Assert.NotNull(constructor4);

        Assert.Equal(constructor1, constructor2);
        Assert.Equal(constructor1, constructor3);
        Assert.Equal(constructor1, constructor4);

        Assert.Equal("MyList", constructor1.ContainingType.Name);

        Assert.True(constructor1.Parameters is [{ Name: "capacity", Type.SpecialType: SpecialType.System_Int32 }, { Name: "name", Type.SpecialType: SpecialType.System_String }]);

        var operation = semanticModel.GetOperation(root.DescendantNodes().OfType<CollectionExpressionSyntax>().ToArray()[1]);
        VerifyOperationTree(compilation, operation, """
            ICollectionExpressionOperation (1 elements, ConstructMethod: MyList<System.Int32>..ctor([System.Int32 capacity = 0], [System.String name = "default"])) (OperationKind.CollectionExpression, Type: MyList<System.Int32>) (Syntax: '[with(capacity: 10), 2]')
            ConstructArguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 10')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: name) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(capacity: 10)')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "default", IsImplicit) (Syntax: 'with(capacity: 10)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Elements(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            """);
    }

    [Fact]
    public void WithElement_ControlFlowGraph1()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Name { get; }
                
                public MyList(int capacity = 0, string name = "default") : base(capacity)
                {
                    Name = name;
                }
            }
            
            class C
            {
                static void Main(bool a)
                {
                    MyList<int> list2 = a ? [with(capacity: 10), 2] : [with(capacity: 20, name: "both"), 4];
                }
            }
            """;

        var compilation = CreateCompilation(source);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().ToArray()[1], semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyList<System.Int32> list2]
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Jump if False (Regular) to Block[B3]
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[with(capacity: 10), 2]')
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyList<System.Int32>, IsImplicit) (Syntax: '[with(capacity: 10), 2]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (1 elements, ConstructMethod: MyList<System.Int32>..ctor([System.Int32 capacity = 0], [System.String name = "default"])) (OperationKind.CollectionExpression, Type: MyList<System.Int32>) (Syntax: '[with(capacity: 10), 2]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 10')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: name) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(capacity: 10)')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "default", IsImplicit) (Syntax: 'with(capacity: 10)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[with(capac ... "both"), 4]')
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyList<System.Int32>, IsImplicit) (Syntax: '[with(capac ... "both"), 4]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (1 elements, ConstructMethod: MyList<System.Int32>..ctor([System.Int32 capacity = 0], [System.String name = "default"])) (OperationKind.CollectionExpression, Type: MyList<System.Int32>) (Syntax: '[with(capac ... "both"), 4]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 20')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: name) (OperationKind.Argument, Type: null) (Syntax: 'name: "both"')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "both") (Syntax: '"both"')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyList<System.Int32>, IsImplicit) (Syntax: 'list2 = a ? ... "both"), 4]')
                          Left:
                            ILocalReferenceOperation: list2 (IsDeclaration: True) (OperationKind.LocalReference, Type: MyList<System.Int32>, IsImplicit) (Syntax: 'list2 = a ? ... "both"), 4]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyList<System.Int32>, IsImplicit) (Syntax: 'a ? [with(c ... "both"), 4]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (ConditionalExpression)
                              Operand:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyList<System.Int32>, IsImplicit) (Syntax: 'a ? [with(c ... "both"), 4]')
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4]
                Statements (0)
            """, graph, symbol);
    }

    [Fact]
    public void WithElement_ControlFlowGraph2()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Name { get; }
                
                public MyList(int capacity = 0, string name = "default") : base(capacity)
                {
                    Name = name;
                }
            }
            
            class C
            {
                static void Main(bool a)
                {
                    MyList<int> list2 = [with(capacity: a ? 10 : 20), 2];
                }
            }
            """;

        var compilation = CreateCompilation(source);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var root = semanticModel.SyntaxTree.GetRoot();

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().ToArray()[1], semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [MyList<System.Int32> list2]
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Jump if False (Regular) to Block[B3]
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                    Next (Regular) Block[B4]
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyList<System.Int32>, IsImplicit) (Syntax: 'list2 = [wi ... 0 : 20), 2]')
                          Left:
                            ILocalReferenceOperation: list2 (IsDeclaration: True) (OperationKind.LocalReference, Type: MyList<System.Int32>, IsImplicit) (Syntax: 'list2 = [wi ... 0 : 20), 2]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyList<System.Int32>, IsImplicit) (Syntax: '[with(capac ... 0 : 20), 2]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (1 elements, ConstructMethod: MyList<System.Int32>..ctor([System.Int32 capacity = 0], [System.String name = "default"])) (OperationKind.CollectionExpression, Type: MyList<System.Int32>) (Syntax: '[with(capac ... 0 : 20), 2]')
                                  ConstructArguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: a ? 10 : 20')
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ? 10 : 20')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: name) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(capaci ...  ? 10 : 20)')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "default", IsImplicit) (Syntax: 'with(capaci ...  ? 10 : 20)')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4]
                Statements (0)
            """, graph, symbol);
    }

    #endregion

    #region Named Arguments Tests

    [Fact]
    public void WithElement_CorrectNamedArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int Value1 { get; }
                public int Value2 { get; }
                
                public MyList(int first, int second) : base()
                {
                    Value1 = first;
                    Value2 = second;
                }
            }
            
            class C
            {
                static int GetSecond()
                {
                    Console.Write("GetSecond called. ");
                    return 20;
                }
            
                static int GetFirst()
                {
                    Console.Write("GetFirst called. ");
                    return 10;
                }

                static void Main()
                {
                    MyList<int> list = [with(second: GetSecond(), first: GetFirst()), 1];
                    Console.WriteLine($"{list.Value1},{list.Value2}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("GetSecond called. GetFirst called. 10,20"))
            .VerifyIL("C.Main", """
            {
              // Code size       63 (0x3f)
              .maxstack  3
              .locals init (MyList<int> V_0, //list
                            int V_1)
              IL_0000:  call       "int C.GetSecond()"
              IL_0005:  stloc.1
              IL_0006:  call       "int C.GetFirst()"
              IL_000b:  ldloc.1
              IL_000c:  newobj     "MyList<int>..ctor(int, int)"
              IL_0011:  dup
              IL_0012:  ldc.i4.1
              IL_0013:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
              IL_0018:  stloc.0
              IL_0019:  ldstr      "{0},{1}"
              IL_001e:  ldloc.0
              IL_001f:  callvirt   "int MyList<int>.Value1.get"
              IL_0024:  box        "int"
              IL_0029:  ldloc.0
              IL_002a:  callvirt   "int MyList<int>.Value2.get"
              IL_002f:  box        "int"
              IL_0034:  call       "string string.Format(string, object, object)"
              IL_0039:  call       "void System.Console.WriteLine(string)"
              IL_003e:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_IncorrectNamedArguments()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with(wrongName: 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,32): error CS1739: The best overload for 'List' does not have a parameter named 'wrongName'
            //         List<int> list = [with(wrongName: 10)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "wrongName").WithArguments("List", "wrongName").WithLocation(7, 32));
    }

    [Fact]
    public void WithElement_MixedPositionalAndNamedArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int Value1 { get; }
                public int Value2 { get; }
                public string Name { get; }
                
                public MyList(int first, int second, string name = "default") : base()
                {
                    Value1 = first;
                    Value2 = second;
                    Name = name;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(10, name: "test", second: 20), 1];
                    Console.WriteLine($"{list.Value1},{list.Value2},{list.Name}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10,20,test"));
    }

    [Fact]
    public void WithElement_NamedArgumentAfterPositional_Invalid()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int first, int second) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(10, first: 20)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,38): error CS1744: Named argument 'first' specifies a parameter for which a positional argument has already been given
            //         MyList<int> list = [with(10, first: 20)];
            Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "first").WithArguments("first").WithLocation(12, 38));
    }

    [Fact]
    public void WithElement_NamedArgumentBeforePositional()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int first, int second) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(first: 20, 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void WithElement_NamedArgumentInWrongPosition()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int first, int second) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(second: 20, 10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,34): error CS8323: Named argument 'second' is used out-of-position but is followed by an unnamed argument
            //         MyList<int> list = [with(second: 20, 10)];
            Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "second").WithArguments("second").WithLocation(12, 34));
    }

    #endregion

    #region Ref/In/Out Parameter Tests

    [Fact]
    public void WithElement_RefParameters()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(ref int value) : base()
                {
                    value = 42;
                }
            }
            
            class C
            {
                static void Main()
                {
                    int x = 10;
                    MyList<int> list = [with(ref x)];
                    Console.WriteLine(x);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42")).VerifyIL("C.Main", """
            {
              // Code size       18 (0x12)
              .maxstack  1
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.s   10
              IL_0002:  stloc.0
              IL_0003:  ldloca.s   V_0
              IL_0005:  newobj     "MyList<int>..ctor(ref int)"
              IL_000a:  pop
              IL_000b:  ldloc.0
              IL_000c:  call       "void System.Console.WriteLine(int)"
              IL_0011:  ret
            }
            """);
    }

    [Theory]
    [InlineData("ref ")]
    [InlineData("")]
    public void WithElement_RefReadonlyParameter(string modifier)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(ref readonly int value) : base()
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    int x = 10;
                    MyList<int> list = [with({{modifier}}x)];
                    Console.WriteLine(x);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10")).VerifyIL("C.Main", """
            {
              // Code size       18 (0x12)
              .maxstack  1
              .locals init (int V_0) //x
              IL_0000:  ldc.i4.s   10
              IL_0002:  stloc.0
              IL_0003:  ldloca.s   V_0
              IL_0005:  newobj     "MyList<int>..ctor(ref readonly int)"
              IL_000a:  pop
              IL_000b:  ldloc.0
              IL_000c:  call       "void System.Console.WriteLine(int)"
              IL_0011:  ret
            }
            """);
    }

    [Theory]
    [InlineData("in ")]
    [InlineData("")]
    public void WithElement_InParameters(string modifier)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            
            class MyList<T> : List<T>
            {
                public int Value { get; }
                public MyList(in int value) : base() 
                { 
                    Console.Write(value + " ");
                    Value = value;
                    Unsafe.AsRef(value) = 10;
                }
            }
            
            class C
            {
                static void Main()
                {
                    int x = 42;
                    MyList<int> list = [with({{modifier}}x), 1];
                    Console.WriteLine(x);
                }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net90,
            expectedOutput: ExecutionConditionUtil.IsCoreClr ? IncludeExpectedOutput("42 10") : null, verify: Verification.FailsPEVerify);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/80518")]
    public void WithElement_OutParameters()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(out int value) : base() 
                { 
                    value = 42;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(out var x), x, x + 1];
                    Console.WriteLine($"{list.Count},{x}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2,42"));
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/80518")]
    public void WithElement_OutVar_UsedInLaterElements()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(out int value, int capacity = 0) : base(capacity) 
                { 
                    value = 100;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(out var x), x, x * 2, x * 3];
                    Console.WriteLine($"{list[0]},{list[1]},{list[2]}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("100,200,300"));
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/80518")]
    public void WithElement_MultipleOutParameters()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(out int value1, out int value2) : base() 
                { 
                    value1 = 10;
                    value2 = 20;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(out var x, out var y), x, y, x + y];
                    Console.WriteLine($"{list[0]},{list[1]},{list[2]}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10,20,30"));
    }

    #endregion

    #region Params Tests

    [Fact]
    public void WithElement_ParamsArray()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            class MyList<T> : List<T>
            {
                public int[] Values { get; }
                
                public MyList(params int[] values) : base()
                {
                    Values = values;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list1 = [with(), 1];
                    MyList<int> list2 = [with(10), 2];
                    MyList<int> list3 = [with(10, 20, 30), 3];
                    
                    Console.WriteLine($"{list1.Values.Length},{list2.Values.Length},{list3.Values.Length}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("0,1,3"));
    }

    [Fact]
    public void WithElement_ParamsWithNamedArguments_Legal()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Name { get; }
                public int[] Values { get; }
                
                public MyList(string name, params int[] values) : base()
                {
                    Name = name;
                    Values = values;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(name: "test", values: new int[] { 1, 2, 3 }), 4];
                    Console.WriteLine($"{list.Name},{list.Values.Length},{list[0]}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("test,3,4"));
    }

    [Fact]
    public void WithElement_ParamsWithNamedArguments_Illegal()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Name { get; }
                public int[] Values { get; }
                
                public MyList(string name, params int[] values) : base()
                {
                    Name = name;
                    Values = values;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(name: "test", values: 1, 2, 3), 4];
                    Console.WriteLine($"{list.Name},{list.Values.Length},{list[0]}");
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (20,29): error CS1729: 'MyList<int>' does not contain a constructor that takes 4 arguments
            //         MyList<int> list = [with(name: "test", values: 1, 2, 3), 4];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, @"with(name: ""test"", values: 1, 2, 3)").WithArguments("MyList<int>", "4").WithLocation(20, 29));
    }

    [Fact]
    public void WithElement_ParamsCollection()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            class MyList<T> : List<T>
            {
                public IEnumerable<int> Values { get; }
                
                public MyList(params IEnumerable<int> values) : base()
                {
                    Values = values;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(new int[] { 1, 2, 3 }), 4];
                    Console.WriteLine($"{list.Values.Count()}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("3"));
    }

    #endregion

    #region Dynamic Tests

    [Theory]
    [InlineData("object")]
    [InlineData("dynamic")]
    public void WithElement_DynamicArguments(string parameterType)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public object Value { get; }
                
                public MyList({{parameterType}} value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static void Main()
                {
                    dynamic d = 42;
                    MyList<int> list = [with(d), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CreateCompilation(source, references: [CSharpRef]).VerifyDiagnostics(
            // (19,34): error CS9337: Collection arguments cannot be dynamic
            //         MyList<int> list = [with(d), 1];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(19, 34));
    }

    [Theory]
    [InlineData("object")]
    [InlineData("dynamic")]
    public void WithElement_DynamicParameters(string argumentType)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public object Value { get; }
                
                public MyList(dynamic value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static void Main()
                {
                    {{argumentType}} d = 42;
                    MyList<int> list = [with(d), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        if (argumentType == "dynamic")
        {
            CreateCompilation(source, references: [CSharpRef]).VerifyDiagnostics(
                // (19,34): error CS9337: Collection arguments cannot be dynamic
                //         MyList<int> list = [with(d), 1];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(19, 34));
        }
        else
        {
            CompileAndVerify(source, references: [CSharpRef]).VerifyIL("C.Main", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  3
                  IL_0000:  ldc.i4.s   42
                  IL_0002:  box        "int"
                  IL_0007:  newobj     "MyList<int>..ctor(dynamic)"
                  IL_000c:  dup
                  IL_000d:  ldc.i4.1
                  IL_000e:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_0013:  callvirt   "object MyList<int>.Value.get"
                  IL_0018:  call       "void System.Console.WriteLine(object)"
                  IL_001d:  ret
                }
                """);
        }
    }

    [Fact]
    public void WithElement_DynamicNamedArguments()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(object value) : base() { }
            }
            
            class C
            {
                void M()
                {
                    dynamic d = 42;
                    MyList<int> list = [with(value: d)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (13,41): error CS9337: Collection arguments cannot be dynamic
            //         MyList<int> list = [with(value: d)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(13, 41));
    }

    [Fact]
    public void WithElement_DynamicType()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    dynamic d = new List<int>();
                    var list = [with(capacity: 10)] as dynamic;
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (8,20): error CS9176: There is no target type for the collection expression.
            //         var list = [with(capacity: 10)] as dynamic;
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[with(capacity: 10)]").WithLocation(8, 20));
    }

    #endregion

    #region ArgList Tests

    [Fact]
    public void WithElement_ArgList()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList : List<int>
            {
                public MyList(__arglist) : base()
                {
                    ArgIterator iter = new ArgIterator(__arglist);

                    while (iter.GetRemainingCount() > 0)
                    {
                        TypedReference tr = iter.GetNextArg();
                        Type t = __reftype(tr);

                        if (t == typeof(int))
                            Console.Write(__refvalue(tr, int) + " ");
                        else if (t == typeof(string))
                            Console.WriteLine(__refvalue(tr, string) + " " );
                        else
                            Console.WriteLine($"Unhandled type: {t}");
                    }
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList list = [with(__arglist(10, "test"))];
                }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.NetFramework,
            expectedOutput: ExecutionConditionUtil.IsWindows ? IncludeExpectedOutput("10 test ") : null,
            verify: Verification.FailsILVerify).VerifyIL("C.Main", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              IL_0000:  ldc.i4.s   10
              IL_0002:  ldstr      "test"
              IL_0007:  newobj     "MyList..ctor(__arglist) with __arglist( int, string)"
              IL_000c:  pop
              IL_000d:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_ArgList_Empty()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList : List<int>
            {
                public MyList(__arglist) : base() 
                {
                    Console.Write ("ArgList constructor called ");
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList list = [with(__arglist()), 1];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source,
            expectedOutput: ExecutionConditionUtil.IsWindows ? IncludeExpectedOutput("ArgList constructor called 1") : null);
    }

    #endregion

    #region Constructor Overload Resolution Tests

    [Fact]
    public void WithElement_OverloadResolution_ExactMatch()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string ConstructorUsed { get; }
                
                public MyList(int capacity) : base(capacity)
                {
                    ConstructorUsed = "int";
                }
                
                public MyList(long capacity) : base((int)capacity)
                {
                    ConstructorUsed = "long";
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list1 = [with(10), 1];
                    MyList<int> list2 = [with(10L), 2];
                    Console.WriteLine($"{list1.ConstructorUsed},{list2.ConstructorUsed}");
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("int,long"));
    }

    [Fact]
    public void WithElement_OverloadResolution_Ambiguous()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int value) => Console.WriteLine("int chosen");
                public MyList(long value) => Console.WriteLine("long chosen");
            }
            
            class C
            {
                static void Main()
                {
                    short s = 10;
                    MyList<int> list = [with(s)];
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("int chosen"));
    }

    [Fact]
    public void WithElement_OverloadResolution_BestMatch()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string ConstructorUsed { get; }
                
                public MyList(object value) : base()
                {
                    ConstructorUsed = "object";
                }
                
                public MyList(int value) : base()
                {
                    ConstructorUsed = "int";
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(42), 1];
                    Console.WriteLine(list.ConstructorUsed);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("int"));
    }

    [Fact]
    public void WithElement_NoMatchingConstructor()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(string value) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(42)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,34): error CS1503: Argument 1: cannot convert from 'int' to 'string'
            //         MyList<int> list = [with(42)];
            Diagnostic(ErrorCode.ERR_BadArgType, "42").WithArguments("1", "int", "string").WithLocation(12, 34));
    }

    [Fact]
    public void WithElement_Constructor_UserDefinedConversion1()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(string value) : base() { }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(new C())];
                }

                public static implicit operator string(C c) => "converted";
            }
            """;

        CompileAndVerify(source).VerifyIL("C.Main", """
            {
              // Code size       17 (0x11)
              .maxstack  1
              IL_0000:  newobj     "C..ctor()"
              IL_0005:  call       "string C.op_Implicit(C)"
              IL_000a:  newobj     "MyList<int>..ctor(string)"
              IL_000f:  pop
              IL_0010:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_Constructor_UserDefinedConversion2()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(long value) : base() { }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(new C())];
                }

                public static implicit operator int(C c) => 0;
            }
            """;

        CompileAndVerify(source).VerifyIL("C.Main", """
            {
              // Code size       18 (0x12)
              .maxstack  1
              IL_0000:  newobj     "C..ctor()"
              IL_0005:  call       "int C.op_Implicit(C)"
              IL_000a:  conv.i8
              IL_000b:  newobj     "MyList<int>..ctor(long)"
              IL_0010:  pop
              IL_0011:  ret
            }
            """);
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public void WithElement_PrivateConstructor()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                private MyList(int capacity) : base(capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(10)];
                }
            }
            """;

        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (12,29): error CS0122: 'MyList<int>.MyList(int)' is inaccessible due to its protection level
            //         MyList<int> list = [with(10)];
            Diagnostic(ErrorCode.ERR_BadAccess, "with(10)").WithArguments("MyList<int>.MyList(int)").WithLocation(12, 29));

        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var withExpression = comp.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<WithElementSyntax>().Single();
        var symbolInfo = semanticModel.GetSymbolInfo(withExpression);

        Assert.Null(symbolInfo.Symbol);
        Assert.Empty(symbolInfo.CandidateSymbols);
    }

    [Fact]
    public void WithElement_IncorrectConstructorType()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(int capacity) : base(capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with("")];
                }
            }
            """;

        var comp = CreateCompilation(source).VerifyEmitDiagnostics(
            // (12,34): error CS1503: Argument 1: cannot convert from 'string' to 'int'
            //         MyList<int> list = [with("")];
            Diagnostic(ErrorCode.ERR_BadArgType, @"""""").WithArguments("1", "string", "int").WithLocation(12, 34));

        var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees[0]);
        var withExpression = comp.SyntaxTrees[0].GetRoot().DescendantNodes().OfType<WithElementSyntax>().Single();
        var symbolInfo = semanticModel.GetSymbolInfo(withExpression);

        Assert.Null(symbolInfo.Symbol);
        Assert.Empty(symbolInfo.CandidateSymbols);
    }

    [Fact]
    public void WithElement_PrivateConstructor2()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                private MyList() { }
                private MyList(int capacity) : base(capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (13,29): error CS0122: 'MyList<int>.MyList(int)' is inaccessible due to its protection level
            //         MyList<int> list = [with(10)];
            Diagnostic(ErrorCode.ERR_BadAccess, "with(10)").WithArguments("MyList<int>.MyList(int)").WithLocation(13, 29));
    }

    [Fact]
    public void WithElement_ProtectedConstructor()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                protected MyList(int capacity) : base(capacity) { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(10)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,29): error CS0122: 'MyList<int>.MyList(int)' is inaccessible due to its protection level
            //         MyList<int> list = [with(10)];
            Diagnostic(ErrorCode.ERR_BadAccess, "with(10)").WithArguments("MyList<int>.MyList(int)").WithLocation(12, 29));
    }

    [Fact]
    public void WithElement_ProtectedConstructor_InSubclass()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                protected MyList(int capacity) : base(capacity) { }
            }

            class D : MyList<int>
            {
                D(int i) : base(i) { }

                public static void Create()
                {
                    new MyList<int>(10);
                    MyList<int> list = [with(10)];
                }
            }
            
            class C
            {
                static void Main()
                {
                    D.Create();
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (14,13): error CS0122: 'MyList<int>.MyList(int)' is inaccessible due to its protection level
            //         new MyList<int>(10);
            Diagnostic(ErrorCode.ERR_BadAccess, "MyList<int>").WithArguments("MyList<int>.MyList(int)").WithLocation(14, 13),
            // (15,29): error CS0122: 'MyList<int>.MyList(int)' is inaccessible due to its protection level
            //         MyList<int> list = [with(10)];
            Diagnostic(ErrorCode.ERR_BadAccess, "with(10)").WithArguments("MyList<int>.MyList(int)").WithLocation(15, 29));
    }

    [Fact]
    public void WithElement_ProtectedConstructor_InSameClass()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                protected MyList(int capacity) : base(capacity) { }

                public static void Create()
                {
                    new MyList<int>(10);
                    MyList<int> list = [with(10)];
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int>.Create();
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void WithElement_InternalConstructor_SameAssembly()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                internal MyList(int capacity) : base(capacity) 
                {
                    Console.Write("Internal constructor ");
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(10), 1];
                    Console.WriteLine(list.Capacity + " " + list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("Internal constructor 10 1"));
    }

    #endregion

    #region Constraint Tests

    [Fact]
    public void WithElement_TypeConstraints()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T> where T : class
            {
                public MyList(T item) : base()
                {
                    Add(item);
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<string> list = [with("first"), "second"];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2"));
    }

    [Fact]
    public void WithElement_TypeConstraints_Violation()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T> : List<T> where T : class
            {
                public MyList(T item) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<int> list = [with(42)];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyList<T>'
            //         MyList<int> list = [with(42)];
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("MyList<T>", "T", "int").WithLocation(12, 16));
    }

    [Fact]
    public void WithElement_TypeConstraints2()
    {
        var source = """
            using System.Collections.Generic;
            
            class MyList<T, TConstructorElementType> : List<T> where T : class
            {
                public MyList(TConstructorElementType item) : base() { }
            }
            
            class C
            {
                void M()
                {
                    MyList<string, bool> list = [with(true)];
                }
            }
            """;

        CompileAndVerify(source).VerifyIL("C.M", """
            {
              // Code size        8 (0x8)
              .maxstack  1
              IL_0000:  ldc.i4.1
              IL_0001:  newobj     "MyList<string, bool>..ctor(bool)"
              IL_0006:  pop
              IL_0007:  ret
            }
            """);
    }

    #endregion

    #region Null and Default Tests

    [Theory]
    [InlineData("null")]
    [InlineData("(string)null")]
    public void WithElement_NullArguments(string argument)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Value { get; }
                
                public MyList(string value) : base()
                {
                    Value = value ?? "null";
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with({{argument}}), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("null"));
    }

    [Theory]
    [InlineData("default(int)")]
    [InlineData("default")]
    public void WithElement_DefaultArguments(string argument)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int Value { get; }
                
                public MyList(int value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with({{argument}}), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("0"));
    }

    [Theory]
    [InlineData("List")]
    [InlineData("IList")]
    public void WithElement_NullableFlow(string type)
    {
        var source = $$"""
            #nullable enable
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    string? s = null;
                    {{type}}<int> list = [with((s = "").Length), 1];
                    var v = s.ToString();
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Theory]
    [InlineData("List")]
    [InlineData("IList")]
    public void WithElement_NullableFlow2(string type)
    {
        var source = $$"""
            #nullable enable
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    string? s = null;
                    {{type}}<int> list = [with((s = "").Length), s.Length];
                    var v = s.ToString();
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Error Recovery Tests

    [Fact]
    public void WithElement_SyntaxError_MissingCloseParen()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with(capacity: 10];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,44): error CS1026: ) expected
            //         List<int> list = [with(capacity: 10];
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "]").WithLocation(7, 44),
            // (7,45): error CS1003: Syntax error, ']' expected
            //         List<int> list = [with(capacity: 10];
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(7, 45));
    }

    [Fact]
    public void WithElement_Error_MissingArguments()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int> list = [with];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,27): error CS0103: The name 'with' does not exist in the current context
            //         List<int> list = [with];
            Diagnostic(ErrorCode.ERR_NameNotInContext, "with").WithArguments("with").WithLocation(7, 27));
    }

    [Fact]
    public void WithElement_EmptyCollection()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(capacity: 100)];
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("0"));
    }

    #endregion

    #region Nested Types Tests

    [Fact]
    public void WithElement_NestedType()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class Outer
            {
                public class MyList<T> : List<T>
                {
                    public int Value { get; }
                    
                    public MyList(int value) : base()
                    {
                        Value = value;
                    }
                }
            }
            
            class C
            {
                static void Main()
                {
                    Outer.MyList<int> list = [with(42), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42"));
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void WithElement_WithExpressionInArguments()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public int Value { get; }
                
                public MyList(int value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static void Main()
                {
                    int x = 10;
                    MyList<int> list = [with(x + 32), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42"));
    }

    [Fact]
    public void WithElement_WithMethodCall()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public string Value { get; }
                
                public MyList(string value) : base()
                {
                    Value = value;
                }
            }
            
            class C
            {
                static string GetValue() => "test";
                
                static void Main()
                {
                    MyList<int> list = [with(GetValue()), 1];
                    Console.WriteLine(list.Value);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("test"));
    }

    [Fact]
    public void WithElement_WithLambda()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public Func<int> ValueFunc { get; }
                
                public MyList(Func<int> func) : base()
                {
                    ValueFunc = func;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(() => 42), 1];
                    Console.WriteLine(list.ValueFunc());
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42"));
    }

    [Fact]
    public void WithElement_WithLambda_ToDelegate()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public Delegate ValueFunc { get; }
                
                public MyList(Delegate func) : base()
                {
                    ValueFunc = func;
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(() => 42), 1];
                    Console.WriteLine(list.ValueFunc.DynamicInvoke());
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42"));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("(short)1")]
    public void WithElement_WithLambda_InferenceWithArgAndConstructor(string returnValue)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(T arg) : base()
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    Goo([with(() => {{returnValue}}), () => 2]);
                }

                static void Goo<T>(MyList<T> list) { }
            }
            """;

        CompileAndVerify(source).VerifyIL("C.Main", """
            {
              // Code size       79 (0x4f)
              .maxstack  4
              IL_0000:  ldsfld     "System.Func<int> C.<>c.<>9__0_0"
              IL_0005:  dup
              IL_0006:  brtrue.s   IL_001f
              IL_0008:  pop
              IL_0009:  ldsfld     "C.<>c C.<>c.<>9"
              IL_000e:  ldftn      "int C.<>c.<Main>b__0_0()"
              IL_0014:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
              IL_0019:  dup
              IL_001a:  stsfld     "System.Func<int> C.<>c.<>9__0_0"
              IL_001f:  newobj     "MyList<System.Func<int>>..ctor(System.Func<int>)"
              IL_0024:  dup
              IL_0025:  ldsfld     "System.Func<int> C.<>c.<>9__0_1"
              IL_002a:  dup
              IL_002b:  brtrue.s   IL_0044
              IL_002d:  pop
              IL_002e:  ldsfld     "C.<>c C.<>c.<>9"
              IL_0033:  ldftn      "int C.<>c.<Main>b__0_1()"
              IL_0039:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
              IL_003e:  dup
              IL_003f:  stsfld     "System.Func<int> C.<>c.<>9__0_1"
              IL_0044:  callvirt   "void System.Collections.Generic.List<System.Func<int>>.Add(System.Func<int>)"
              IL_0049:  call       "void C.Goo<System.Func<int>>(MyList<System.Func<int>>)"
              IL_004e:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_WithLambda_InferenceWithArgAndConstructor_2()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(T arg) : base()
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    Goo([with(() => 1), () => (short)2]);
                }

                static void Goo<T>(MyList<T> list) { }
            }
            """;

        CompileAndVerify(source).VerifyIL("C.Main", """
            {
              // Code size       79 (0x4f)
              .maxstack  4
              IL_0000:  ldsfld     "System.Func<short> C.<>c.<>9__0_0"
              IL_0005:  dup
              IL_0006:  brtrue.s   IL_001f
              IL_0008:  pop
              IL_0009:  ldsfld     "C.<>c C.<>c.<>9"
              IL_000e:  ldftn      "short C.<>c.<Main>b__0_0()"
              IL_0014:  newobj     "System.Func<short>..ctor(object, System.IntPtr)"
              IL_0019:  dup
              IL_001a:  stsfld     "System.Func<short> C.<>c.<>9__0_0"
              IL_001f:  newobj     "MyList<System.Func<short>>..ctor(System.Func<short>)"
              IL_0024:  dup
              IL_0025:  ldsfld     "System.Func<short> C.<>c.<>9__0_1"
              IL_002a:  dup
              IL_002b:  brtrue.s   IL_0044
              IL_002d:  pop
              IL_002e:  ldsfld     "C.<>c C.<>c.<>9"
              IL_0033:  ldftn      "short C.<>c.<Main>b__0_1()"
              IL_0039:  newobj     "System.Func<short>..ctor(object, System.IntPtr)"
              IL_003e:  dup
              IL_003f:  stsfld     "System.Func<short> C.<>c.<>9__0_1"
              IL_0044:  callvirt   "void System.Collections.Generic.List<System.Func<short>>.Add(System.Func<short>)"
              IL_0049:  call       "void C.Goo<System.Func<short>>(MyList<System.Func<short>>)"
              IL_004e:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_WithLambda_InferenceWithArgAndConstructor_3()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(T arg) : base()
                {
                }
            }
            
            class C
            {
                static void Main(int i)
                {
                    Goo([with(() => i), () => (short)2]);
                }

                static void Goo<T>(MyList<T> list) { }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using System;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1),
            // (15,25): error CS0266: Cannot implicitly convert type 'int' to 'short'. An explicit conversion exists (are you missing a cast?)
            //         Goo([with(() => i), () => (short)2]);
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i").WithArguments("int", "short").WithLocation(15, 25),
            // (15,25): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
            //         Goo([with(() => i), () => (short)2]);
            Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "i").WithArguments("lambda expression").WithLocation(15, 25));
    }

    [Fact]
    public void WithElement_WithLambda_InferenceWithArgAndConstructor_4()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(T arg) : base()
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    int i = 0;
                    Goo([with(() => i), () => (short)2]);
                }

                static void Goo<T>(MyList<T> list) { }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using System;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1),
            // (16,25): error CS0266: Cannot implicitly convert type 'int' to 'short'. An explicit conversion exists (are you missing a cast?)
            //         Goo([with(() => i), () => (short)2]);
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i").WithArguments("int", "short").WithLocation(16, 25),
            // (16,25): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
            //         Goo([with(() => i), () => (short)2]);
            Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "i").WithArguments("lambda expression").WithLocation(16, 25));
    }

    [Fact]
    public void WithElement_Ambiguous_WithOverloads()
    {
        var source = $$"""
            using System.Collections.Generic;
            
            class C
            {
                void M(List<int> list) { }
                void M(HashSet<int> set) { }

                void G()
                {
                    M([with(capacity: 10)]);
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (10,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(List<int>)' and 'C.M(HashSet<int>)'
            //         M([with(capacity: 10)]);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(System.Collections.Generic.List<int>)", "C.M(System.Collections.Generic.HashSet<int>)").WithLocation(10, 9));
    }

    [Fact]
    public void WithElement_Ambiguous_WithOverloads2()
    {
        var source = $$"""
            using System.Collections;
            using System.Collections.Generic;
            
            class Collection1 : IEnumerable<int>
            {
                public IEnumerator<int> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Collection2(int capacity) : IEnumerable<int>
            {
                public IEnumerator<int> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class D
            {
                void M(Collection1 coll) { }
                void M(Collection2 coll) { }

                void G()
                {
                    M([]);
                    M([with()]);
                    M([with(capacity: 42)]);
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (10,23): warning CS9113: Parameter 'capacity' is unread.
            // class Collection2(int capacity) : IEnumerable<int>
            Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "capacity").WithArguments("capacity").WithLocation(10, 23),
            // (24,9): error CS0121: The call is ambiguous between the following methods or properties: 'D.M(Collection1)' and 'D.M(Collection2)'
            //         M([with()]);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("D.M(Collection1)", "D.M(Collection2)").WithLocation(24, 9),
            // (25,9): error CS0121: The call is ambiguous between the following methods or properties: 'D.M(Collection1)' and 'D.M(Collection2)'
            //         M([with(capacity: 42)]);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("D.M(Collection1)", "D.M(Collection2)").WithLocation(25, 9));
    }

    [Fact]
    public void WithElement_DoesNotContributeToTypeInference1()
    {
        var source = $$"""
            using System.Collections.Generic;
            
            class C
            {
                void G()
                {
                    M([with(capacity: 10)]);
                }

                void M<T>(List<T> list) { }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,9): error CS0411: The type arguments for method 'C.M<T>(List<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         M([with(capacity: 10)]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(System.Collections.Generic.List<T>)").WithLocation(7, 9));
    }

    [Fact]
    public void WithElement_DoesNotContributeToTypeInference2()
    {
        var source = $$"""
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(T value) { }
            }

            class C
            {
                void G()
                {
                    M([with(10)]);
                }

                void M<T>(MyList<T> list) { }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (12,9): error CS0411: The type arguments for method 'C.M<T>(MyList<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         M([with(10)]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(MyList<T>)").WithLocation(12, 9));
    }

    [Fact]
    public void WithElement_ArgumentsAreLowered1()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    List<int> list = [with(Invoke(() => 10))];
                    Console.WriteLine(list.Capacity);
                }

                static T Invoke<T>(System.Func<T> func) => func();
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10")).VerifyIL("C.Main", """
            {
              // Code size       52 (0x34)
              .maxstack  2
              IL_0000:  ldsfld     "System.Func<int> C.<>c.<>9__0_0"
              IL_0005:  dup
              IL_0006:  brtrue.s   IL_001f
              IL_0008:  pop
              IL_0009:  ldsfld     "C.<>c C.<>c.<>9"
              IL_000e:  ldftn      "int C.<>c.<Main>b__0_0()"
              IL_0014:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
              IL_0019:  dup
              IL_001a:  stsfld     "System.Func<int> C.<>c.<>9__0_0"
              IL_001f:  call       "int C.Invoke<int>(System.Func<int>)"
              IL_0024:  newobj     "System.Collections.Generic.List<int>..ctor(int)"
              IL_0029:  callvirt   "int System.Collections.Generic.List<int>.Capacity.get"
              IL_002e:  call       "void System.Console.WriteLine(int)"
              IL_0033:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_ArgumentsAreLowered1_A()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    IList<int> list = [with(Invoke(() => 10))];
                    Console.WriteLine(((List<int>)list).Capacity);
                }

                static T Invoke<T>(System.Func<T> func) => func();
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("10")).VerifyIL("C.Main", """
            {
              // Code size       57 (0x39)
              .maxstack  2
              IL_0000:  ldsfld     "System.Func<int> C.<>c.<>9__0_0"
              IL_0005:  dup
              IL_0006:  brtrue.s   IL_001f
              IL_0008:  pop
              IL_0009:  ldsfld     "C.<>c C.<>c.<>9"
              IL_000e:  ldftn      "int C.<>c.<Main>b__0_0()"
              IL_0014:  newobj     "System.Func<int>..ctor(object, System.IntPtr)"
              IL_0019:  dup
              IL_001a:  stsfld     "System.Func<int> C.<>c.<>9__0_0"
              IL_001f:  call       "int C.Invoke<int>(System.Func<int>)"
              IL_0024:  newobj     "System.Collections.Generic.List<int>..ctor(int)"
              IL_0029:  castclass  "System.Collections.Generic.List<int>"
              IL_002e:  callvirt   "int System.Collections.Generic.List<int>.Capacity.get"
              IL_0033:  call       "void System.Console.WriteLine(int)"
              IL_0038:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_ArgumentsAreLowered2()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main(string[] args)
                {
                    List<int> list = [with(Goo([1, args.Length]))];
                    Console.WriteLine(list.Capacity);
                }

                static int Goo(int[] values) => values.Length;
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyIL("C.Main", """
            {
              // Code size       37 (0x25)
              .maxstack  4
              IL_0000:  ldc.i4.2
              IL_0001:  newarr     "int"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldc.i4.1
              IL_0009:  stelem.i4
              IL_000a:  dup
              IL_000b:  ldc.i4.1
              IL_000c:  ldarg.0
              IL_000d:  ldlen
              IL_000e:  conv.i4
              IL_000f:  stelem.i4
              IL_0010:  call       "int C.Goo(int[])"
              IL_0015:  newobj     "System.Collections.Generic.List<int>..ctor(int)"
              IL_001a:  callvirt   "int System.Collections.Generic.List<int>.Capacity.get"
              IL_001f:  call       "void System.Console.WriteLine(int)"
              IL_0024:  ret
            }
            """);
    }

    [Fact]
    public void WithElement_ArgumentsAreLowered2_A()
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main(string[] args)
                {
                    IList<int> list = [with(Goo([1, args.Length]))];
                    Console.WriteLine(((List<int>)list).Capacity);
                }

                static int Goo(int[] values) => values.Length;
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyIL("C.Main", """
            {
              // Code size       42 (0x2a)
              .maxstack  4
              IL_0000:  ldc.i4.2
              IL_0001:  newarr     "int"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldc.i4.1
              IL_0009:  stelem.i4
              IL_000a:  dup
              IL_000b:  ldc.i4.1
              IL_000c:  ldarg.0
              IL_000d:  ldlen
              IL_000e:  conv.i4
              IL_000f:  stelem.i4
              IL_0010:  call       "int C.Goo(int[])"
              IL_0015:  newobj     "System.Collections.Generic.List<int>..ctor(int)"
              IL_001a:  castclass  "System.Collections.Generic.List<int>"
              IL_001f:  callvirt   "int System.Collections.Generic.List<int>.Capacity.get"
              IL_0024:  call       "void System.Console.WriteLine(int)"
              IL_0029:  ret
            }
            """);
    }

    [Fact]
    public void ExpressionTreeInWithElement()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq.Expressions;

            class MyList : List<int>
            {
                public Expression<Func<int>> Expr;
                public MyList(Expression<Func<int>> expr) : base()
                {
                    Expr = expr;
                }
            }


            class Program
            {
                static void Main()
                {
                    MyList m = [with(() => 42)];
                    var v = m.Expr.Compile().Invoke();
                    Console.WriteLine(v);
                }
            }
            """;
        var comp = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("42")).VerifyDiagnostics().VerifyIL("Program.Main", """
            {
              // Code size       58 (0x3a)
              .maxstack  2
              IL_0000:  ldc.i4.s   42
              IL_0002:  box        "int"
              IL_0007:  ldtoken    "int"
              IL_000c:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_0011:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
              IL_0016:  call       "System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()"
              IL_001b:  call       "System.Linq.Expressions.Expression<System.Func<int>> System.Linq.Expressions.Expression.Lambda<System.Func<int>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])"
              IL_0020:  newobj     "MyList..ctor(System.Linq.Expressions.Expression<System.Func<int>>)"
              IL_0025:  ldfld      "System.Linq.Expressions.Expression<System.Func<int>> MyList.Expr"
              IL_002a:  callvirt   "System.Func<int> System.Linq.Expressions.Expression<System.Func<int>>.Compile()"
              IL_002f:  callvirt   "int System.Func<int>.Invoke()"
              IL_0034:  call       "void System.Console.WriteLine(int)"
              IL_0039:  ret
            }
            """);
    }

    [Fact]
    public void OverloadResolutionPriority()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            class MyCollection<T> : List<T>
            {
                public MyCollection(string s, object o)
                {
                    Console.WriteLine("Called first overload");
                }

                [OverloadResolutionPriority(1)]
                public MyCollection(object o, string s)
                {
                    Console.WriteLine("Called second overload");
                }
            }
            """;
        string sourceB = """
            using System;
            class Program
            {
                static void Main()
                {
                    MyCollection<string> c = [with("", ""), ""];
                }
            }
            """;
        var comp = CompileAndVerify(
            [sourceA, sourceB, OverloadResolutionPriorityAttributeDefinition],
            // targetFramework: TargetFramework.Net100,
            expectedOutput: IncludeExpectedOutput(
                """
                Called second overload
                """)).VerifyIL("Program.Main", """
                {
                  // Code size       28 (0x1c)
                  .maxstack  3
                  IL_0000:  ldstr      ""
                  IL_0005:  ldstr      ""
                  IL_000a:  newobj     "MyCollection<string>..ctor(object, string)"
                  IL_000f:  dup
                  IL_0010:  ldstr      ""
                  IL_0015:  callvirt   "void System.Collections.Generic.List<string>.Add(string)"
                  IL_001a:  pop
                  IL_001b:  ret
                }
                """);
    }

    [Fact]
    public void WithElement_UnscopedRef1()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            
            class C : List<int>
            {
                public C(out Span<string> egress, [UnscopedRef] out string ingress)
                {
                    ingress = "a";
                    egress = new Span<string>(ref ingress);
                }

                Span<string> M()
                {
                    string y = "a";
                    C list = [with(out Span<string> x, out y)];
                    return x;
                }
            
                Span<string> N()
                {
                    string y = "a";
                    C list = new(out Span<string> x, out y);
                    return x;
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics(
            // (17,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
            //         return x;
            Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(17, 16),
            // (24,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
            //         return x;
            Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(24, 16));
    }

    [Fact]
    public void WithElement_NotUnscopedRef1()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C : List<int>
            {
                public C(out Span<string> egress, out string ingress)
                {
                    ingress = "a";
                    egress = [];
                }
            
                Span<string> M()
                {
                    string y = "a";
                    C list = [with(out Span<string> x, out y)];
                    return x;
                }

                Span<string> N()
                {
                    string y = "a";
                    C list = new(out Span<string> x, out y);
                    return x;
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    [Fact]
    public void WithElement_FileLocalType1()
    {
        string sourceA = """
            using System.Collections.Generic;

            file class MyCollection<T> : List<T>
            {
                public MyCollection(string value)
                {
                }
            }

            class Program
            {
                static void Main()
                {
                    MyCollection<int> c = [with("")];
                }
            }
            """;

        CompileAndVerify(
            sourceA,
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void WithElement_FileLocalType2()
    {
        string sourceA = """
            using System;
            using System.Collections.Generic;

            file class MyCollection<T> : List<T>
            {
                public MyCollection(Arg value)
                {
                }
            }

            file class Arg {}

            class Program
            {
                static void Main()
                {
                    MyCollection<int> c = [with(new Arg()), 1, 2];
                    Console.WriteLine(string.Join(", ", c));
                }
            }
            """;

        CompileAndVerify(
            sourceA,
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void Constructor_UseSiteError_Method()
    {
        // public sealed class MyCollection<T> : IEnumerable<T>
        // {
        //     [CompilerFeatureRequired("MyFeature")]
        //     public MyCollection() { }
        //     public IEnumerator<T> GetEnumerator() { }
        // }
        string sourceA = """
                .assembly extern System.Runtime { .ver 8:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }

                .class public sealed MyCollection`1<T>
                    implements class [System.Runtime]System.Collections.Generic.IEnumerable`1<!T>,
                                     [System.Runtime]System.Collections.IEnumerable
                {
                  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                  {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = { string('MyFeature') }
                    ret
                  }
                  .method public instance class [System.Runtime]System.Collections.Generic.IEnumerator`1<!T> GetEnumerator() { ldnull ret }
                }
                """;
        var refA = CompileIL(sourceA);

        string sourceB = """
                #pragma warning disable 219
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [];
                        MyCollection<int> w = [with()];
                    }
                }
                """;
        var comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,31): error CS9041: 'MyCollection<T>.MyCollection()' requires compiler feature 'MyFeature', which is not supported by this version of the C# compiler.
            //         MyCollection<int> x = [];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "[]").WithArguments("MyCollection<T>.MyCollection()", "MyFeature").WithLocation(6, 31),
            // (7,32): error CS9041: 'MyCollection<T>.MyCollection()' requires compiler feature 'MyFeature', which is not supported by this version of the C# compiler.
            //         MyCollection<int> w = [with()];
            Diagnostic(ErrorCode.ERR_UnsupportedCompilerFeature, "with()").WithArguments("MyCollection<T>.MyCollection()", "MyFeature").WithLocation(7, 32));
    }

    #endregion
}
