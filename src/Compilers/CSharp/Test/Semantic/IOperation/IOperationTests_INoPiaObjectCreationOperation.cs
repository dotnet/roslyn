// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NoPiaObjectCreation_01()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[CoClass(typeof(ClassITest33))]
public interface ITest33 : System.Collections.IEnumerable
{
    void Add(int x);
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest33
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public void M1(ITest33 x, int y)
    {
	    x = /*<bind>*/new ITest33  { y }/*</bind>*/;
    }
} 
";

            string expectedOperationTree = @"
INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33  { y }')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: ITest33) (Syntax: '{ y }')
      Initializers(1):
          IInvocationOperation (virtual void ITest33.Add(System.Int32 x)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: ITest33, IsImplicit) (Syntax: 'ITest33')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(consumer, expectedOperationTree, expectedDiagnostics, references: new []{ piaCompilation.EmitToImageReference(embedInteropTypes: true) });
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NoPiaObjectCreation_02()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[CoClass(typeof(ClassITest33))]
public interface ITest33
{
    int P {get; set;}
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest33
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public void M1(ITest33 x, int y)
    {
	    x = /*<bind>*/new ITest33  { P = y }/*</bind>*/;
    }
} 
";

            string expectedOperationTree = @"
INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33  { P = y }')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: ITest33) (Syntax: '{ P = y }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P = y')
            Left: 
              IPropertyReferenceOperation: System.Int32 ITest33.P { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: ITest33, IsImplicit) (Syntax: 'P')
            Right: 
              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(consumer, expectedOperationTree, expectedDiagnostics, references: new[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NoPiaObjectCreation_03()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[CoClass(typeof(ClassITest33))]
public interface ITest33
{
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest33
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public void M1(ITest33 x, int y)
    {
	    x = /*<bind>*/new ITest33()/*</bind>*/;
    }
} 
";

            string expectedOperationTree = @"
INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33()')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(consumer, expectedOperationTree, expectedDiagnostics, references: new[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoPiaObjectCreationFlow_01()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[CoClass(typeof(ClassITest33))]
public interface ITest33 : System.Collections.IEnumerable
{
    void Add(int x);
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest33
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    /*<bind>*/public void M1(ITest33 x, int y)
    {
	    x = new ITest33  { y };
    }/*</bind>*/
} 
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (4)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: ITest33) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new ITest33  { y }')
          Value: 
            INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33  { y }')
              Initializer: 
                null

        IInvocationOperation (virtual void ITest33.Add(System.Int32 x)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33  { y }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = new ITest33  { y };')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ITest33) (Syntax: 'x = new ITest33  { y }')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'x')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33  { y }')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(consumer, expectedFlowGraph, expectedDiagnostics, references: new[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoPiaObjectCreationFlow_02()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[CoClass(typeof(ClassITest33))]
public interface ITest33
{
    int P {get; set;}
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest33
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    /*<bind>*/public void M1(ITest33 x, int y)
    {
	    x = new ITest33  { P = y };
    }/*</bind>*/
} 
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (4)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: ITest33) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new ITest33  { P = y }')
          Value: 
            INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33  { P = y }')
              Initializer: 
                null

        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'P = y')
          Left: 
            IPropertyReferenceOperation: System.Int32 ITest33.P { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33  { P = y }')
          Right: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = new ITe ...  { P = y };')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ITest33) (Syntax: 'x = new ITe ...   { P = y }')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'x')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33  { P = y }')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(consumer, expectedFlowGraph, expectedDiagnostics, references: new[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoPiaObjectCreationFlow_03()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[CoClass(typeof(ClassITest33))]
public interface ITest33
{
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest33
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    /*<bind>*/public void M1(ITest33 x, int y)
    {
	    x = new ITest33();
    }/*</bind>*/
} 
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = new ITest33();')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ITest33) (Syntax: 'x = new ITest33()')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: ITest33) (Syntax: 'x')
              Right: 
                INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33()')
                  Initializer: 
                    null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(consumer, expectedFlowGraph, expectedDiagnostics, references: new[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NoPiaObjectCreationFlow_04()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[CoClass(typeof(ClassITest33))]
public interface ITest33 : System.Collections.IEnumerable
{
    void Add(object x);
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest33
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    /*<bind>*/public void M1(ITest33 x, object y1, object y2)
    {
	    x = new ITest33  { y1 ?? y2 };
    }/*</bind>*/
} 
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: ITest33) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new ITest33 ...  y1 ?? y2 }')
          Value: 
            INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33 ...  y1 ?? y2 }')
              Initializer: 
                null

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y1')
          Value: 
            IParameterReferenceOperation: y1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'y1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y1')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y1')
          Value: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y2')
          Value: 
            IParameterReferenceOperation: y2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'y2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (2)
        IInvocationOperation (virtual void ITest33.Add(System.Object x)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y1 ?? y2')
          Instance Receiver: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 ...  y1 ?? y2 }')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y1 ?? y2')
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y1 ?? y2')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = new ITe ... y1 ?? y2 };')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ITest33) (Syntax: 'x = new ITe ...  y1 ?? y2 }')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'x')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 ...  y1 ?? y2 }')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(consumer, expectedFlowGraph, expectedDiagnostics, references: new[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });
        }
    }
}
