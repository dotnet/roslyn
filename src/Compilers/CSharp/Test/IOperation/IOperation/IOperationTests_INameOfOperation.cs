// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_INameOfOperation : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_01()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2)
    /*<bind>*/{
        string test = nameof(b ? i1 : i2);
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8081: Expression does not have a name.
                //         string test = nameof(b ? i1 : i2);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "b ? i1 : i2").WithLocation(6, 30),
                // CS0219: The variable 'test' is assigned but its value is never used
                //         string test = nameof(b ? i1 : i2);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "test").WithArguments("test").WithLocation(6, 16)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.String test]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'test = name ...  ? i1 : i2)')
              Left: 
                ILocalReferenceOperation: test (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'test = name ...  ? i1 : i2)')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsInvalid) (Syntax: 'nameof(b ? i1 : i2)')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_02()
        {
            string source = @"
class C
{
    void M(int i1)
    /*<bind>*/{
        string test = nameof(i1);
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'test' is assigned but its value is never used
                //         string test = nameof(i1);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "test").WithArguments("test").WithLocation(6, 16)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.String test]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'test = nameof(i1)')
              Left: 
                ILocalReferenceOperation: test (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'test = nameof(i1)')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i1"") (Syntax: 'nameof(i1)')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_03()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2)
    /*<bind>*/{
        string test = b ? nameof(i1) : nameof(i2);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.String test]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'nameof(i1)')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i1"") (Syntax: 'nameof(i1)')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'nameof(i2)')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i2"") (Syntax: 'nameof(i2)')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'test = b ?  ...  nameof(i2)')
              Left: 
                ILocalReferenceOperation: test (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'test = b ?  ...  nameof(i2)')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b ? nameof( ...  nameof(i2)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_InvalidName()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
        string test = nameof(test2);
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,30): error CS0103: The name 'test2' does not exist in the current context
                //         string test = nameof(test2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "test2").WithArguments("test2").WithLocation(6, 30)
            };

            string expectedFlowGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.String test]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'test = nameof(test2)')
                  Left: 
                    ILocalReferenceOperation: test (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'test = nameof(test2)')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test2"", IsInvalid) (Syntax: 'nameof(test2)')
            Next (Regular) Block[B2]
                Leaving: {R1}
    }
    Block[B2] - Exit
        Predecessors: [B1]
        Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_InstanceMemberFromStatic_Flat()
        {
            var source = """
                public class C
                {
                    public int Property { get; }
                    public int Field;
                    public event System.Action Event;
                
                    public static string StaticMethod()
                    /*<bind>*/{
                        return nameof(Property) +
                            nameof(Field) +
                            nameof(Event);
                    }/*</bind>*/
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "PropertyFieldEvent") (Syntax: 'nameof(Prop ... meof(Event)')
                          Left:
                            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "PropertyField") (Syntax: 'nameof(Prop ... meof(Field)')
                              Left:
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Property") (Syntax: 'nameof(Property)')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Field") (Syntax: 'nameof(Field)')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Event") (Syntax: 'nameof(Event)')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, DiagnosticDescription.None);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_InstanceMemberFromStatic_Flat_MethodGroup()
        {
            var source = """
                public class C
                {
                    public void Method1() { }
                    public void Method1(int i) { }
                    public void Method2() { }
                    public static void Method2(int i) { }
                
                    public static string StaticMethod()
                    /*<bind>*/{
                        return nameof(Method1) +
                            nameof(Method2);
                    }/*</bind>*/
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "Method1Method2") (Syntax: 'nameof(Meth ... of(Method2)')
                          Left:
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Method1") (Syntax: 'nameof(Method1)')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Method2") (Syntax: 'nameof(Method2)')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, DiagnosticDescription.None);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67565")]
        public void NameOfFlow_InstanceMemberFromStatic_Nested()
        {
            var source = """
                public class C
                {
                    public C1 Property { get; }
                    public C1 Field;
                
                    public static string StaticMethod()
                    /*<bind>*/{
                        return nameof(Property.Property) +
                            nameof(Property.Field) +
                            nameof(Property.Event) +
                            nameof(Field.Property) +
                            nameof(Field.Field) +
                            nameof(Field.Event);
                    }/*</bind>*/
                }
                
                public class C1
                {
                    public int Property { get; }
                    public int Field;
                    public event System.Action Event;
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "PropertyFieldEventPropertyFieldEvent") (Syntax: 'nameof(Prop ... ield.Event)')
                          Left:
                            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "PropertyFieldEventPropertyField") (Syntax: 'nameof(Prop ... ield.Field)')
                              Left:
                                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "PropertyFieldEventProperty") (Syntax: 'nameof(Prop ... d.Property)')
                                  Left:
                                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "PropertyFieldEvent") (Syntax: 'nameof(Prop ... erty.Event)')
                                      Left:
                                        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "PropertyField") (Syntax: 'nameof(Prop ... erty.Field)')
                                          Left:
                                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Property") (Syntax: 'nameof(Prop ... y.Property)')
                                          Right:
                                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Field") (Syntax: 'nameof(Property.Field)')
                                      Right:
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Event") (Syntax: 'nameof(Property.Event)')
                                  Right:
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Property") (Syntax: 'nameof(Field.Property)')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Field") (Syntax: 'nameof(Field.Field)')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Event") (Syntax: 'nameof(Field.Event)')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, DiagnosticDescription.None);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67565")]
        public void NameOfFlow_InstanceMemberFromStatic_Nested_MethodGroup()
        {
            var source = """
                public class C
                {
                    public C1 Property { get; }
                    public C1 Field;
                    public event System.Action Event;
                
                    public static string StaticMethod()
                    /*<bind>*/{
                        return nameof(Property.Method) +
                            nameof(Field.Method) +
                            nameof(Event.Invoke);
                    }/*</bind>*/
                }

                public class C1
                {
                    public void Method() { }
                    public void Method(int i) { }
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (0)
                    Next (Return) Block[B2]
                        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "MethodMethodInvoke") (Syntax: 'nameof(Prop ... ent.Invoke)')
                          Left:
                            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String, Constant: "MethodMethod") (Syntax: 'nameof(Prop ... eld.Method)')
                              Left:
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Method") (Syntax: 'nameof(Property.Method)')
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Method") (Syntax: 'nameof(Field.Method)')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Invoke") (Syntax: 'nameof(Event.Invoke)')
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, DiagnosticDescription.None);
        }
    }
}
