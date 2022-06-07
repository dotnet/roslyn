// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FunctionPointerOperations : SemanticModelTestBase
    {
        private CSharpCompilation CreateFunctionPointerCompilation(string source)
        {
            return CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9);
        }

        [Fact]
        public void FunctionPointerLoad()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    static void M1() => throw null;
    static void M2()
    {
        delegate*<void> ptr = /*<bind>*/&M1/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IAddressOfOperation (OperationKind.AddressOf, Type: delegate*<System.Void>) (Syntax: '&M1')
  Reference: 
    IMethodReferenceOperation: void C.M1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
      Instance Receiver: 
        null
";

            VerifyOperationTreeAndDiagnosticsForTest<PrefixUnaryExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[0]);
        }

        [Fact]
        public void FunctionPointerLoad_WithThisReference()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    void M1() => throw null;
    void M2()
    {
        delegate*<void> ptr = /*<bind>*/&M1/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IAddressOfOperation (OperationKind.AddressOf, Type: null, IsInvalid) (Syntax: '&M1')
  Reference: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
      Children(1):
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M1')
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,42): error CS8759: Cannot create a function pointer for 'C.M1()' because it is not a static method
                //         delegate*<void> ptr = /*<bind>*/&M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_FuncPtrMethMustBeStatic, "M1").WithArguments("C.M1()").WithLocation(7, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<PrefixUnaryExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void FunctionPointerLoad_WithInstanceReference()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    void M1() => throw null;
    static void M2(C c)
    {
        delegate*<void> ptr = /*<bind>*/&c.M1/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IAddressOfOperation (OperationKind.AddressOf, Type: null, IsInvalid) (Syntax: '&c.M1')
  Reference: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'c.M1')
      Children(1):
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,42): error CS8759: Cannot create a function pointer for 'C.M1()' because it is not a static method
                //         delegate*<void> ptr = /*<bind>*/&c.M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_FuncPtrMethMustBeStatic, "c.M1").WithArguments("C.M1()").WithLocation(7, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<PrefixUnaryExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void FunctionPointerLoad_WithStaticReference()
        {
            var comp = CreateFunctionPointerCompilation(@"
static class Helper { public static void M1() => throw null; }
unsafe class C
{
    static void M2()
    {
        delegate*<void> ptr = /*<bind>*/&Helper.M1/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IAddressOfOperation (OperationKind.AddressOf, Type: delegate*<System.Void>) (Syntax: '&Helper.M1')
  Reference: 
    IMethodReferenceOperation: void Helper.M1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'Helper.M1')
      Instance Receiver: 
        null
";

            var expectedDiagnostics = new DiagnosticDescription[] {
            };

            VerifyOperationTreeAndDiagnosticsForTest<PrefixUnaryExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[0]);
        }

        [Fact]
        public void FunctionPointerLoad_NonExistantMethod()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    static void M2()
    {
        delegate*<void> ptr = /*<bind>*/&M1/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IAddressOfOperation (OperationKind.AddressOf, Type: ?*, IsInvalid) (Syntax: '&M1')
  Reference: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M1')
      Children(0)
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (6,42): error CS0103: The name 'M1' does not exist in the current context
                //         delegate*<void> ptr = /*<bind>*/&M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<PrefixUnaryExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void FunctionPointerLoad_InvalidMethod()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    static string M1() => null;
    static void M2()
    {
        delegate*<void> ptr = /*<bind>*/&M1/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IAddressOfOperation (OperationKind.AddressOf, Type: null, IsInvalid) (Syntax: '&M1')
  Reference: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M1')
      Children(1):
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M1')
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,42): error CS0407: 'string C.M1()' has the wrong return type
                //         delegate*<void> ptr = /*<bind>*/&M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("C.M1()", "string").WithLocation(7, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<PrefixUnaryExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void FunctionPointerInvocationSignatureTest()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    public string Prop { get; }
    void M(delegate*<string, void> ptr)
    {
        /*<bind>*/ptr(Prop)/*</bind>*/;
    }
}");
            var (actualOperation, syntaxNode) = GetOperationAndSyntaxForTest<InvocationExpressionSyntax>(comp);

            var fktPointerOp = (IFunctionPointerInvocationOperation)actualOperation;
            var signature = fktPointerOp.GetFunctionPointerSignature();

            Assert.NotNull(syntaxNode);
            Assert.Equal(1, signature.Parameters.Length);
            Assert.Equal(SpecialType.System_String, signature.Parameters[0].Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, signature.ReturnType.SpecialType);
        }

        [Fact]
        public void FunctionPointerUnsafe()
        {
            var comp = CreateFunctionPointerCompilation(@"
using System;
static unsafe class C
{
    static int Getter(int i) => i;
    static void Print(delegate*<int, int>* p)
    {
        for (int i = 0; i < 3; i++)
            Console.Write(/*<bind>*/p[i](i)/*</bind>*/);
    }

    static void Main()
    {
        delegate*<int, int>* p = stackalloc delegate*<int, int>[] { &Getter, &Getter, &Getter };
        Print(p);
    }
}
");
            var expectedOperationTree = @"
IFunctionPointerInvocationOperation (OperationKind.FunctionPointerInvocation, Type: System.Int32) (Syntax: 'p[i](i)')
  Target:
    IOperation:  (OperationKind.None, Type: delegate*<System.Int32, System.Int32>) (Syntax: 'p[i]')
      Children(2):
          IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: delegate*<System.Int32, System.Int32>*) (Syntax: 'p')
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: ) (OperationKind.Argument, Type: null) (Syntax: 'i')
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            ";

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[0]);
        }

        [Fact]
        public void FunctionPointerInvocation()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    public string Prop { get; }
    void M(delegate*<string, void> ptr)
    {
        /*<bind>*/ptr(Prop)/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IFunctionPointerInvocationOperation (OperationKind.FunctionPointerInvocation, Type: System.Void) (Syntax: 'ptr(Prop)')
  Target: 
    IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.String, System.Void>) (Syntax: 'ptr')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: ) (OperationKind.Argument, Type: null) (Syntax: 'Prop')
        IPropertyReferenceOperation: System.String C.Prop { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Prop')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Prop')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            ";

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[0]);
        }

        [Fact]
        public void FunctionPointerInvocation_TooFewArguments()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    public string Prop { get; }
    void M(delegate*<string, string, void> ptr)
    {
        /*<bind>*/ptr(Prop)/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'ptr(Prop)')
  Children(2):
      IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.String, System.String, System.Void>, IsInvalid) (Syntax: 'ptr')
      IPropertyReferenceOperation: System.String C.Prop { get; } (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'Prop')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'Prop')
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,19): error CS8756: Function pointer 'delegate*<string, string, void>' does not take 1 arguments
                //         /*<bind>*/ptr(Prop)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadFuncPointerArgCount, "ptr(Prop)").WithArguments("delegate*<string, string, void>", "1").WithLocation(7, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void FunctionPointerInvocation_TooManyArguments()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    public string Prop { get; }
    void M(delegate*<void> ptr)
    {
        /*<bind>*/ptr(Prop)/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'ptr(Prop)')
  Children(2):
      IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>, IsInvalid) (Syntax: 'ptr')
      IPropertyReferenceOperation: System.String C.Prop { get; } (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'Prop')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'Prop')
            ";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,19): error CS8756: Function pointer 'delegate*<string, string, void>' does not take 1 arguments
                //         /*<bind>*/ptr(Prop)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadFuncPointerArgCount, "ptr(Prop)").WithArguments("delegate*<void>", "1").WithLocation(7, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void FunctionPointerInvocation_IncorrectParameterType()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    public string Prop { get; }
    void M(delegate*<int, void> ptr)
    {
        /*<bind>*/ptr(Prop)/*</bind>*/;
    }
}");

            var expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'ptr(Prop)')
  Children(2):
      IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Int32, System.Void>) (Syntax: 'ptr')
      IPropertyReferenceOperation: System.String C.Prop { get; } (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'Prop')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'Prop')
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,23): error CS1503: Argument 1: cannot convert from 'string' to 'int'
                //         /*<bind>*/ptr(Prop)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgType, "Prop").WithArguments("1", "string", "int").WithLocation(7, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void FunctionPointerInvocation_IncorrectReturnUsage()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    public string Prop { get; }
    void M(delegate*<string, int> ptr)
    /*<bind>*/{
        string s = ptr(Prop);
        s = ptr(Prop);
    }/*</bind>*/
}");

            var expectedOperationTree = @"
IBlockOperation (2 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: System.String s
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'string s = ptr(Prop);')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'string s = ptr(Prop)')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.String s) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 's = ptr(Prop)')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ptr(Prop)')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'ptr(Prop)')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IFunctionPointerInvocationOperation (OperationKind.FunctionPointerInvocation, Type: System.Int32, IsInvalid) (Syntax: 'ptr(Prop)')
                      Target:
                        IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.String, System.Int32>, IsInvalid) (Syntax: 'ptr')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: ) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'Prop')
                            IPropertyReferenceOperation: System.String C.Prop { get; } (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'Prop')
                              Instance Receiver:
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'Prop')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Initializer:
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 's = ptr(Prop);')
    Expression:
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: 's = ptr(Prop)')
        Left:
          ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
        Right:
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'ptr(Prop)')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              IFunctionPointerInvocationOperation (OperationKind.FunctionPointerInvocation, Type: System.Int32, IsInvalid) (Syntax: 'ptr(Prop)')
                Target:
                  IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.String, System.Int32>, IsInvalid) (Syntax: 'ptr')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: ) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'Prop')
                      IPropertyReferenceOperation: System.String C.Prop { get; } (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: 'Prop')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'Prop')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,20): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         string s = ptr(Prop);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "ptr(Prop)").WithArguments("int", "string").WithLocation(7, 20),
                // (8,13): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         s = ptr(Prop);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "ptr(Prop)").WithArguments("int", "string").WithLocation(8, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void FunctionPointerAddressOf_InCFG()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    static void M1() {}
    static void M2() {}

    static void Test(delegate*<void> ptr, bool b)
    /*<bind>*/{
        ptr = b ? (delegate*<void>)&M1 : &M2;
    }/*</bind>*/
}");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'ptr')
              Value: 
                IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.Void>) (Syntax: 'ptr')
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(delegate*<void>)&M1')
              Value: 
                IAddressOfOperation (OperationKind.AddressOf, Type: delegate*<System.Void>) (Syntax: '(delegate*<void>)&M1')
                  Reference: 
                    IMethodReferenceOperation: void C.M1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'M1')
                      Instance Receiver: 
                        null
        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '&M2')
              Value: 
                IAddressOfOperation (OperationKind.AddressOf, Type: delegate*<System.Void>) (Syntax: '&M2')
                  Reference: 
                    IMethodReferenceOperation: void C.M2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'M2')
                      Instance Receiver: 
                        null
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ptr = b ? ( ... )&M1 : &M2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: delegate*<System.Void>) (Syntax: 'ptr = b ? ( ... >)&M1 : &M2')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: delegate*<System.Void>, IsImplicit) (Syntax: 'ptr')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: delegate*<System.Void>, IsImplicit) (Syntax: 'b ? (delega ... >)&M1 : &M2')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, new DiagnosticDescription[0]);
        }

        [Fact]
        public void FunctionPointerInvocation_InCFG()
        {
            var comp = CreateFunctionPointerCompilation(@"
unsafe class C
{
    static void M1() {}
    static void M2() {}

    static void Test(delegate*<string, void> ptr, bool b, string s1, string s2)
    /*<bind>*/{
        ptr(b ? s1 : s2);
    }/*</bind>*/
}");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'ptr')
              Value:
                IParameterReferenceOperation: ptr (OperationKind.ParameterReference, Type: delegate*<System.String, System.Void>) (Syntax: 'ptr')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's1')
              Value:
                IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's2')
              Value:
                IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ptr(b ? s1 : s2);')
              Expression:
                IFunctionPointerInvocationOperation (OperationKind.FunctionPointerInvocation, Type: System.Void) (Syntax: 'ptr(b ? s1 : s2)')
                  Target:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: delegate*<System.String, System.Void>, IsImplicit) (Syntax: 'ptr')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: ) (OperationKind.Argument, Type: null) (Syntax: 'b ? s1 : s2')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b ? s1 : s2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, new DiagnosticDescription[0]);
        }
    }
}
