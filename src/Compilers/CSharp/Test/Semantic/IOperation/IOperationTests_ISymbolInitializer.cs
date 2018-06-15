// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void NoInitializers()
        {
            var source = @"
class C
{
    static int s1;
    int i1;
    int P1 { get; }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().Where(n => n is VariableDeclarationSyntax || n is PropertyDeclarationSyntax).ToArray();
            Assert.Equal(3, nodes.Length);

            var semanticModel = compilation.GetSemanticModel(tree);
            foreach (var node in nodes)
            {
                Assert.Null(semanticModel.GetOperation(node));
            }
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_StaticField()
        {
            string source = @"
class C
{
    static int s1 /*<bind>*/= 1/*</bind>*/;
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Int32 C.s1) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.s1' is assigned but its value is never used
                //     static int s1 /*<bind>*/= 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s1").WithArguments("C.s1").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_InstanceField()
        {
            string source = @"
class C
{
    int i1 = 1, i2 /*<bind>*/= 2/*</bind>*/;
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Int32 C.i2) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 2')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.i2' is assigned but its value is never used
                //     int i1 = 1, i2 /*<bind>*/= 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i2").WithArguments("C.i2").WithLocation(4, 17),
                // CS0414: The field 'C.i1' is assigned but its value is never used
                //     int i1 = 1, i2 /*<bind>*/= 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(4, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_Property()
        {
            string source = @"
class C
{
    int P1 { get; } /*<bind>*/= 1/*</bind>*/;
}
";
            string expectedOperationTree = @"
IPropertyInitializerOperation (Property: System.Int32 C.P1 { get; }) (OperationKind.PropertyInitializer, Type: null) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_DefaultValueParameter()
        {
            string source = @"
class C
{
    void M(int p1 /*<bind>*/= 0/*</bind>*/, params int[] p2 = null) { }
}
";
            string expectedOperationTree = @"
IParameterInitializerOperation (Parameter: [System.Int32 p1 = 0]) (OperationKind.ParameterInitializer, Type: null) (Syntax: '= 0')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1751: Cannot specify a default value for a parameter array
                //     void M(int p1 /*<bind>*/= 0/*</bind>*/, params int[] p2 = null) { }
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(4, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_DefaultValueParamsArray()
        {
            string source = @"
class C
{
    void M(int p1 = 0, params int[] p2 /*<bind>*/= null/*</bind>*/) { }
}
";
            string expectedOperationTree = @"
IParameterInitializerOperation (Parameter: params System.Int32[] p2) (OperationKind.ParameterInitializer, Type: null) (Syntax: '= null')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32[], Constant: null, IsImplicit) (Syntax: 'null')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1751: Cannot specify a default value for a parameter array
                //     void M(int p1 = 0, params int[] p2 /*<bind>*/= null/*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(4, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ExpressionInitializers_StaticField()
        {
            string source = @"
class C
{
    static int s1 /*<bind>*/= 1 + F()/*</bind>*/;

    static int F() { return 1; }
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Int32 C.s1) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1 + F()')
  IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationOperation (System.Int32 C.F()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: 
          null
        Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ExpressionInitializers_InstanceField()
        {
            string source = @"
class C
{
    static int s1 /*<bind>*/= 1 + F()/*</bind>*/;
    int i1 = 1 + F();
    int P1 { get; } = 1 + F();

    static int F() { return 1; }
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Int32 C.s1) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1 + F()')
  IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationOperation (System.Int32 C.F()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: 
          null
        Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ExpressionInitializers_Property()
        {
            string source = @"
class C
{
    int i1 /*<bind>*/= 1 + F()/*</bind>*/;

    static int F() { return 1; }
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Int32 C.i1) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1 + F()')
  IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationOperation (System.Int32 C.F()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: 
          null
        Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void PartialClasses_StaticField()
        {
            string source = @"
partial class C
{
    static int s1 /*<bind>*/= 1/*</bind>*/;
    int i1 = 1;
}

partial class C
{
    static int s2 = 2;
    int i2 = 2;
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Int32 C.s1) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.i1' is assigned but its value is never used
                //     int i1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(5, 9),
                // CS0414: The field 'C.s2' is assigned but its value is never used
                //     static int s2 = 2;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s2").WithArguments("C.s2").WithLocation(10, 16),
                // CS0414: The field 'C.s1' is assigned but its value is never used
                //     static int s1 /*<bind>*/= 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s1").WithArguments("C.s1").WithLocation(4, 16),
                // CS0414: The field 'C.i2' is assigned but its value is never used
                //     int i2 = 2;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i2").WithArguments("C.i2").WithLocation(11, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void PartialClasses_InstanceField()
        {
            string source = @"
partial class C
{
    static int s1 = 1;
    int i1 = 1;
}

partial class C
{
    static int s2 = 2;
    int i2 /*<bind>*/= 2/*</bind>*/;
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Int32 C.i2) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 2')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.s2' is assigned but its value is never used
                //     static int s2 = 2;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s2").WithArguments("C.s2").WithLocation(10, 16),
                // CS0414: The field 'C.i2' is assigned but its value is never used
                //     int i2 /*<bind>*/= 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i2").WithArguments("C.i2").WithLocation(11, 9),
                // CS0414: The field 'C.s1' is assigned but its value is never used
                //     static int s1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s1").WithArguments("C.s1").WithLocation(4, 16),
                // CS0414: The field 'C.i1' is assigned but its value is never used
                //     int i1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(5, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void Events_StaticField()
        {
            string source = @"
class C
{
    static event System.Action e /*<bind>*/= MakeAction(1)/*</bind>*/;

    static System.Action MakeAction(int x) { return null; }
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Action C.e) (OperationKind.FieldInitializer, Type: null) (Syntax: '= MakeAction(1)')
  IInvocationOperation (System.Action C.MakeAction(System.Int32 x)) (OperationKind.Invocation, Type: System.Action) (Syntax: 'MakeAction(1)')
    Instance Receiver: 
      null
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void Events_InstanceField()
        {
            string source = @"
class C
{
    event System.Action f /*<bind>*/= MakeAction(2)/*</bind>*/;

    static System.Action MakeAction(int x) { return null; }
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Action C.f) (OperationKind.FieldInitializer, Type: null) (Syntax: '= MakeAction(2)')
  IInvocationOperation (System.Action C.MakeAction(System.Int32 x)) (OperationKind.Invocation, Type: System.Action) (Syntax: 'MakeAction(2)')
    Instance Receiver: 
      null
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '2')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(7299, "https://github.com/dotnet/roslyn/issues/7299")]
        public void FieldInitializer_ConstantConversions_01()
        {
            string source = @"
class C
{
    private float f /*<bind>*/= 0.0/*</bind>*/;
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Single C.f) (OperationKind.FieldInitializer, Type: null, IsInvalid) (Syntax: '= 0.0')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Single, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0.0')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsInvalid) (Syntax: '0.0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (4,33): error CS0664: Literal of type double cannot be implicitly converted to type 'float'; use an 'F' suffix to create a literal of this type
                //     private float f /*<bind>*/= 0.0/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LiteralDoubleCast, "0.0").WithArguments("F", "float").WithLocation(4, 33),
                // (4,19): warning CS0414: The field 'C.f' is assigned but its value is never used
                //     private float f /*<bind>*/= 0.0/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("C.f").WithLocation(4, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(7299, "https://github.com/dotnet/roslyn/issues/7299")]
        public void FieldInitializer_ConstantConversions_02()
        {
            string source = @"
class C
{
    private float f /*<bind>*/= 0/*</bind>*/;
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Single C.f) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 0')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Single, Constant: 0, IsImplicit) (Syntax: '0')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (4,19): warning CS0414: The field 'C.f' is assigned but its value is never used
                //     private float f /*<bind>*/= 0/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("C.f").WithLocation(4, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_ConstantInitializer_NonConstantField()
        {
            string source = @"
class C
{
    public static int s1 /*<bind>*/= 1/*</bind>*/;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IFieldReferenceOperation: System.Int32 C.s1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                null
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_ConstantInitializer_ConstantField()
        {
            string source = @"
class C
{
    public const int c1 /*<bind>*/= 1/*</bind>*/;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                null
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantInitializer_NonConstantStaticField()
        {
            // This unit test also includes declaration with multiple variables.
            string source = @"
class C
{
    public static int s = 0, s1 /*<bind>*/= M()/*</bind>*/, s2;
    public static int M() { s2 = s; return s2; }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M()')
          Left: 
            IFieldReferenceOperation: System.Int32 C.s1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                null
          Right: 
            IInvocationOperation (System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantInitializer_NonConstantInstanceField()
        {
            string source = @"
class C
{
    public int s1 /*<bind>*/= M()/*</bind>*/;
    public static int M() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M()')
          Left: 
            IFieldReferenceOperation: System.Int32 C.s1 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M()')
          Right: 
            IInvocationOperation (System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantInitializer_ConstantField()
        {
            string source = @"
class C
{
    public const int c1 /*<bind>*/= M()/*</bind>*/;
    public static int M() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M()')
          Left: 
            IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                null
          Right: 
            IInvocationOperation (System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,37): error CS0133: The expression being assigned to 'C.c1' must be constant
                //     public const int c1 /*<bind>*/= M()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "M()").WithArguments("C.c1").WithLocation(4, 37)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantInitializer_FieldLikeEvent()
        {
            string source = @"
class C
{
    static event System.Action e /*<bind>*/= M()/*</bind>*/;

    static System.Action M() { return null; }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action, IsImplicit) (Syntax: '= M()')
          Left: 
            IFieldReferenceOperation: System.Action C.e (Static) (OperationKind.FieldReference, Type: System.Action, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                null
          Right: 
            IInvocationOperation (System.Action C.M()) (OperationKind.Invocation, Type: System.Action) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_ConstantInitializer_ConstantField()
        {
            string source = @"
class C
{
    public const int c1 /*<bind>*/= true ? 1 : 2/*</bind>*/;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B4]
Block[B3] - Block [UnReachable]
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= true ? 1 : 2')
          Left: 
            IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= true ? 1 : 2')
              Instance Receiver: 
                null
          Right: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'true ? 1 : 2')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantInitializer_NonConstantStaticField()
        {
            string source = @"
class C
{
    public static int s1 /*<bind>*/= M() ?? M2()/*</bind>*/;
    public static int? M() => 0;
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation (System.Int32? C.M()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M()')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2()')
          Value: 
            IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M() ?? M2()')
          Left: 
            IFieldReferenceOperation: System.Int32 C.s1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= M() ?? M2()')
              Instance Receiver: 
                null
          Right: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'M() ?? M2()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantInitializer_NonConstantInstanceField()
        {
            string source = @"
class C
{
    public int s1 /*<bind>*/= M() ?? M2()/*</bind>*/;
    public static int? M() => 0;
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation (System.Int32? C.M()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M()')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2()')
          Value: 
            IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M() ?? M2()')
          Left: 
            IFieldReferenceOperation: System.Int32 C.s1 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= M() ?? M2()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M() ?? M2()')
          Right: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'M() ?? M2()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantInitializer_ConstantField()
        {
            string source = @"
class C
{
    public const int c1 /*<bind>*/= M() ?? M2()/*</bind>*/;
    public static int? M() => 0;
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation (System.Int32? C.M()) (OperationKind.Invocation, Type: System.Int32?, IsInvalid) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'M()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'M()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'M()')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M2()')
          Value: 
            IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M() ?? M2()')
          Left: 
            IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M() ?? M2()')
              Instance Receiver: 
                null
          Right: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M() ?? M2()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,37): error CS0133: The expression being assigned to 'C.c1' must be constant
                //     public const int c1 /*<bind>*/= M() ?? M2()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "M() ?? M2()").WithArguments("C.c1").WithLocation(4, 37)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantFieldInitializerWithLocals()
        {
            string source = @"
class C
{
    public int s1 /*<bind>*/= M(out int local)/*</bind>*/;
    public static int M(out int x)
    {
        x = 0;
        return 0;
    }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 local]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M(out int local)')
              Left: 
                IFieldReferenceOperation: System.Int32 C.s1 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= M(out int local)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M(out int local)')
              Right: 
                IInvocationOperation (System.Int32 C.M(out System.Int32 x)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M(out int local)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'out int local')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int local')
                          ILocalReferenceOperation: local (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'local')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantFieldInitializerWithLocals()
        {
            string source = @"
class C
{
    public int s1 /*<bind>*/= M(out int local) ?? M2()/*</bind>*/;
    public static int? M(out int x)
    {
        x = 0;
        return 0;
    }
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 local]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M(out int local)')
              Value: 
                IInvocationOperation (System.Int32? C.M(out System.Int32 x)) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'M(out int local)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'out int local')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int local')
                          ILocalReferenceOperation: local (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'local')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Jump if True (Regular) to Block[B3]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M(out int local)')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M(out int local)')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M(out int local)')
              Value: 
                IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M(out int local)')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M(out int local)')
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2()')
              Value: 
                IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M(out int ... al) ?? M2()')
              Left: 
                IFieldReferenceOperation: System.Int32 C.s1 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= M(out int ... al) ?? M2()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M(out int ... al) ?? M2()')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'M(out int local) ?? M2()')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_MissingFieldInitializerValue()
        {
            string source = @"
class C
{
    public int s1 /*<bind>*/= /*</bind>*/;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= /*</bind>*/')
          Left: 
            IFieldReferenceOperation: System.Int32 C.s1 (OperationKind.FieldReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= /*</bind>*/')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: '= /*</bind>*/')
          Right: 
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,42): error CS1525: Invalid expression term ';'
                //     public int s1 /*<bind>*/= /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(4, 42)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_ConstantPropertyInitializer_StaticProperty()
        {
            string source = @"
class C
{
    public static int P1 { get; } /*<bind>*/= 1/*</bind>*/;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IPropertyReferenceOperation: System.Int32 C.P1 { get; } (Static) (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                null
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_ConstantPropertyInitializer_InstanceProperty()
        {
            string source = @"
class C
{
    public int P1 { get; } /*<bind>*/= 1/*</bind>*/;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IPropertyReferenceOperation: System.Int32 C.P1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= 1')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantPropertyInitializer_StaticProperty()
        {
            string source = @"
class C
{
    public static int P1 { get; } /*<bind>*/= M()/*</bind>*/;
    public static int M() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M()')
          Left: 
            IPropertyReferenceOperation: System.Int32 C.P1 { get; } (Static) (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                null
          Right: 
            IInvocationOperation (System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantPropertyInitializer_InstanceProperty()
        {
            string source = @"
class C
{
    public int P1 { get; } /*<bind>*/= M()/*</bind>*/;
    public static int M() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M()')
          Left: 
            IPropertyReferenceOperation: System.Int32 C.P1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M()')
          Right: 
            IInvocationOperation (System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantPropertyInitializer_StaticProperty()
        {
            string source = @"
class C
{
    public static int P1 { get; } /*<bind>*/= M() ?? M2()/*</bind>*/;
    public static int? M() => 0;
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation (System.Int32? C.M()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M()')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2()')
          Value: 
            IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M() ?? M2()')
          Left: 
            IPropertyReferenceOperation: System.Int32 C.P1 { get; } (Static) (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= M() ?? M2()')
              Instance Receiver: 
                null
          Right: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'M() ?? M2()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantPropertyInitializer_InstanceProperty()
        {
            string source = @"
class C
{
    public int P1 { get; } /*<bind>*/= M() ?? M2()/*</bind>*/;
    public static int? M() => 0;
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation (System.Int32? C.M()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M()')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2()')
          Value: 
            IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M() ?? M2()')
          Left: 
            IPropertyReferenceOperation: System.Int32 C.P1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= M() ?? M2()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M() ?? M2()')
          Right: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'M() ?? M2()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantPropertyInitializerWithLocals()
        {
            string source = @"
class C
{
    public int P1 { get; } /*<bind>*/= M(out int local)/*</bind>*/;
    public static int M(out int x)
    {
        x = 0;
        return 0;
    }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 local]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M(out int local)')
              Left: 
                IPropertyReferenceOperation: System.Int32 C.P1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= M(out int local)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M(out int local)')
              Right: 
                IInvocationOperation (System.Int32 C.M(out System.Int32 x)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M(out int local)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'out int local')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int local')
                          ILocalReferenceOperation: local (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'local')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantPropertyInitializerWithLocals()
        {
            string source = @"
class C
{
    public int P1 { get; } /*<bind>*/= M(out int local) ?? M2()/*</bind>*/;
    public static int? M(out int x)
    {
        x = 0;
        return 0;
    }
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 local]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M(out int local)')
              Value: 
                IInvocationOperation (System.Int32? C.M(out System.Int32 x)) (OperationKind.Invocation, Type: System.Int32?) (Syntax: 'M(out int local)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'out int local')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int local')
                          ILocalReferenceOperation: local (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'local')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Jump if True (Regular) to Block[B3]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M(out int local)')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M(out int local)')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M(out int local)')
              Value: 
                IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M(out int local)')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'M(out int local)')
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2()')
              Value: 
                IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M(out int ... al) ?? M2()')
              Left: 
                IPropertyReferenceOperation: System.Int32 C.P1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= M(out int ... al) ?? M2()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M(out int ... al) ?? M2()')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'M(out int local) ?? M2()')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_MissingPropertyInitializerValue()
        {
            string source = @"
class C
{
    public int P1 { get; } /*<bind>*/= /*</bind>*/;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= /*</bind>*/')
          Left: 
            IPropertyReferenceOperation: System.Int32 C.P1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= /*</bind>*/')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: '= /*</bind>*/')
          Right: 
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,51): error CS1525: Invalid expression term ';'
                //     public int P1 { get; } /*<bind>*/= /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(4, 51)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_ConstantParameterInitializer()
        {
            string source = @"
class C
{
    public void M(int x /*<bind>*/= 1/*</bind>*/) { }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantParameterInitializer()
        {
            string source = @"
class C
{
    public void M(int x /*<bind>*/= M2()/*</bind>*/) { }
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M2()')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M2()')
          Right: 
            IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,37): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     public void M(int x /*<bind>*/= M2()/*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M2()").WithArguments("x").WithLocation(4, 37)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantParameterInitializer()
        {
            string source = @"
class C
{
    public void M(int x /*<bind>*/= M1() ?? M2()/*</bind>*/) { }
    public static int? M1() => 0;
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M1()')
          Value: 
            IInvocationOperation (System.Int32? C.M1()) (OperationKind.Invocation, Type: System.Int32?, IsInvalid) (Syntax: 'M1()')
              Instance Receiver: 
                null
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'M1()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'M1()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M1()')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M1()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'M1()')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M2()')
          Value: 
            IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M1() ?? M2()')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M1() ?? M2()')
          Right: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M1() ?? M2()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,37): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     public void M(int x /*<bind>*/= M1() ?? M2()/*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M1() ?? M2()").WithArguments("x").WithLocation(4, 37)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_NonConstantParameterInitializerWithLocals()
        {
            string source = @"
class C
{
    public void M(int x /*<bind>*/= M1(out int local)/*</bind>*/) { }
    public static int M1(out int x)
    {
        x = 0;
        return 0;
    }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 local]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M1(out int local)')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M1(out int local)')
              Right: 
                IInvocationOperation (System.Int32 C.M1(out System.Int32 x)) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M1(out int local)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out int local')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'int local')
                          ILocalReferenceOperation: local (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'local')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,37): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     public void M(int x /*<bind>*/= M1(out int local)/*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M1(out int local)").WithArguments("x").WithLocation(4, 37)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ControlFlow_NonConstantParameterInitializerWithLocals()
        {
            string source = @"
class C
{
    public void M(int x /*<bind>*/= M1(out int local) ?? M2()/*</bind>*/) { }
    public static int? M1(out int x)
    {
        x = 0;
        return 0;
    }
    public static int M2() => 0;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 local]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M1(out int local)')
              Value: 
                IInvocationOperation (System.Int32? C.M1(out System.Int32 x)) (OperationKind.Invocation, Type: System.Int32?, IsInvalid) (Syntax: 'M1(out int local)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out int local')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'int local')
                          ILocalReferenceOperation: local (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'local')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Jump if True (Regular) to Block[B3]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'M1(out int local)')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'M1(out int local)')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M1(out int local)')
              Value: 
                IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M1(out int local)')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'M1(out int local)')
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M2()')
              Value: 
                IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M1(out in ... al) ?? M2()')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M1(out in ... al) ?? M2()')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M1(out int  ... al) ?? M2()')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,37): error CS1736: Default parameter value for 'x' must be a compile-time constant
                //     public void M(int x /*<bind>*/= M1(out int local) ?? M2()/*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M1(out int local) ?? M2()").WithArguments("x").WithLocation(4, 37)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoControlFlow_MissingParameterInitializerValue()
        {
            string source = @"
class C
{
    public void M(int x /*<bind>*/= /*</bind>*/) { }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= /*</bind>*/')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= /*</bind>*/')
          Right: 
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,48): error CS1525: Invalid expression term ')'
                //     public void M(int x /*<bind>*/= /*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(4, 48)
            };

            VerifyFlowGraphAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
