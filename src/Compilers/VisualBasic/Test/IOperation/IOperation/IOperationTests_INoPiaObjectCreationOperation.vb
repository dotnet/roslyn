' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub NoPiaObjectCreation_01()
            Dim pia = "
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<Assembly: PrimaryInteropAssemblyAttribute(1,1)>
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
<CoClass(GetType(ClassITest33))>
public interface ITest33 
    Inherits System.Collections.IEnumerable

    Sub Add(x As Integer)
End interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")>
public mustinherit class ClassITest33
End class
"

            Dim piaCompilation = CreateCompilationWithMscorlib45(pia, options:=TestOptions.ReleaseDll)

            CompileAndVerify(piaCompilation)

            Dim source = <![CDATA[
Imports System.Collections.Generic

Class C
    Sub M1(x as ITest33, y As Integer)
        x = new ITest33 From { y } 'BIND:"new ITest33 From { y }"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33 From { y }')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: ITest33) (Syntax: 'From { y }')
      Initializers(1):
          IInvocationOperation (virtual Sub ITest33.Add(x As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 From { y }')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, references:={piaCompilation.EmitToImageReference(embedInteropTypes:=True)})
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub NoPiaObjectCreation_02()
            Dim pia = "
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<Assembly: PrimaryInteropAssemblyAttribute(1,1)>
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
<CoClass(GetType(ClassITest33))>
public interface ITest33 

    Property P As Integer
End interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")>
public mustinherit class ClassITest33
End class
"

            Dim piaCompilation = CreateCompilationWithMscorlib45(pia, options:=TestOptions.ReleaseDll)

            CompileAndVerify(piaCompilation)

            Dim source = <![CDATA[
Imports System.Collections.Generic

Class C
    Sub M1(x as ITest33, y As Integer)
        x = new ITest33 With { .P = y } 'BIND:"new ITest33 With { .P = y }"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33 ...  { .P = y }')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: ITest33) (Syntax: 'With { .P = y }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.P = y')
            Left: 
              IPropertyReferenceOperation: Property ITest33.P As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 ...  { .P = y }')
            Right: 
              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, references:={piaCompilation.EmitToImageReference(embedInteropTypes:=True)})
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub NoPiaObjectCreation_03()
            Dim pia = "
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<Assembly: PrimaryInteropAssemblyAttribute(1,1)>
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
<CoClass(GetType(ClassITest33))>
public interface ITest33 
End interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")>
public mustinherit class ClassITest33
End class
"

            Dim piaCompilation = CreateCompilationWithMscorlib45(pia, options:=TestOptions.ReleaseDll)

            CompileAndVerify(piaCompilation)

            Dim source = <![CDATA[
Imports System.Collections.Generic

Class C
    Sub M1(x as ITest33, y As Integer)
        x = new ITest33() 'BIND:"new ITest33()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33()')
  Initializer: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, references:={piaCompilation.EmitToImageReference(embedInteropTypes:=True)})
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub NoPiaObjectCreationFlow_01()
            Dim pia = "
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<Assembly: PrimaryInteropAssemblyAttribute(1,1)>
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
<CoClass(GetType(ClassITest33))>
public interface ITest33 
    Inherits System.Collections.IEnumerable

    Sub Add(x As Integer)
End interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")>
public mustinherit class ClassITest33
End class
"

            Dim piaCompilation = CreateCompilationWithMscorlib45(pia, options:=TestOptions.ReleaseDll)

            CompileAndVerify(piaCompilation)

            Dim source = <![CDATA[
Imports System.Collections.Generic

Class C
    Sub M1(x as ITest33, y As Integer) 'BIND:"Sub M1"
        x = new ITest33 From { y } 
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: ITest33) (Syntax: 'x')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new ITest33 From { y }')
              Value: 
                INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33 From { y }')
                  Initializer: 
                    null

            IInvocationOperation (virtual Sub ITest33.Add(x As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 From { y }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = new ITe ...  From { y }')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ITest33, IsImplicit) (Syntax: 'x = new ITe ...  From { y }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'x')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 From { y }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, additionalReferences:={piaCompilation.EmitToImageReference(embedInteropTypes:=True)})
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub NoPiaObjectCreationFlow_02()
            Dim pia = "
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<Assembly: PrimaryInteropAssemblyAttribute(1,1)>
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
<CoClass(GetType(ClassITest33))>
public interface ITest33 

    Property P As Integer
End interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")>
public mustinherit class ClassITest33
End class
"

            Dim piaCompilation = CreateCompilationWithMscorlib45(pia, options:=TestOptions.ReleaseDll)

            CompileAndVerify(piaCompilation)

            Dim source = <![CDATA[
Imports System.Collections.Generic

Class C
    Sub M1(x as ITest33, y As Integer) 'BIND:"Sub M1"
        x = new ITest33 With { .P = y }
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: ITest33) (Syntax: 'x')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new ITest33 ...  { .P = y }')
              Value: 
                INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33 ...  { .P = y }')
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.P = y')
              Left: 
                IPropertyReferenceOperation: Property ITest33.P As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 ...  { .P = y }')
              Right: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = new ITe ...  { .P = y }')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ITest33, IsImplicit) (Syntax: 'x = new ITe ...  { .P = y }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'x')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 ...  { .P = y }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, additionalReferences:={piaCompilation.EmitToImageReference(embedInteropTypes:=True)})
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub NoPiaObjectCreationFlow_03()
            Dim pia = "
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<Assembly: PrimaryInteropAssemblyAttribute(1,1)>
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
<CoClass(GetType(ClassITest33))>
public interface ITest33 
End interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")>
public mustinherit class ClassITest33
End class
"

            Dim piaCompilation = CreateCompilationWithMscorlib45(pia, options:=TestOptions.ReleaseDll)

            CompileAndVerify(piaCompilation)

            Dim source = <![CDATA[
Imports System.Collections.Generic

Class C
    Sub M1(x as ITest33, y As Integer) 'BIND:"Sub M1"
        x = new ITest33()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = new ITest33()')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ITest33, IsImplicit) (Syntax: 'x = new ITest33()')
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
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, additionalReferences:={piaCompilation.EmitToImageReference(embedInteropTypes:=True)})
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub NoPiaObjectCreationFlow_04()
            Dim pia = "
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<Assembly: PrimaryInteropAssemblyAttribute(1,1)>
<Assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
<CoClass(GetType(ClassITest33))>
public interface ITest33 

    Property P As Object
End interface

<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")>
public mustinherit class ClassITest33
End class
"

            Dim piaCompilation = CreateCompilationWithMscorlib45(pia, options:=TestOptions.ReleaseDll)

            CompileAndVerify(piaCompilation)

            Dim source = <![CDATA[
Imports System.Collections.Generic

Class C
    Sub M1(x as ITest33, y1 As Object, y2 As Object) 'BIND:"Sub M1"
        x = new ITest33 With { .P = If(y1, y2) }
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: ITest33) (Syntax: 'x')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new ITest33 ... f(y1, y2) }')
              Value: 
                INoPiaObjectCreationOperation (OperationKind.None, Type: ITest33) (Syntax: 'new ITest33 ... f(y1, y2) }')
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y1')
                      Value: 
                        IParameterReferenceOperation: y1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'y1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y1')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y2')
                  Value: 
                    IParameterReferenceOperation: y2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'y2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.P = If(y1, y2)')
                  Left: 
                    IPropertyReferenceOperation: Property ITest33.P As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'P')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 ... f(y1, y2) }')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(y1, y2)')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = new ITe ... f(y1, y2) }')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ITest33, IsImplicit) (Syntax: 'x = new ITe ... f(y1, y2) }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'x')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ITest33, IsImplicit) (Syntax: 'new ITest33 ... f(y1, y2) }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, additionalReferences:={piaCompilation.EmitToImageReference(embedInteropTypes:=True)})
        End Sub

    End Class
End Namespace
