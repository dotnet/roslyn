// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_DynamicArgument()
        {
            string source = @"
class C
{
    public C(int i)
    {
    }

    void M(dynamic d)
    {
        var x = /*<bind>*/new C(d)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(d)')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_MultipleApplicableSymbols()
        {
            string source = @"
class C
{
    public C(int i)
    {
    }

    public C(long i)
    {
    }

    void M(dynamic d)
    {
        var x = /*<bind>*/new C(d)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(d)')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_MultipleArgumentsAndApplicableSymbols()
        {
            string source = @"
class C
{
    public C(int i, char c)
    {
    }

    public C(long i, char c)
    {
    }

    void M(dynamic d)
    {
        char c = 'c';
        var x = /*<bind>*/new C(d, c)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(d, c)')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Char) (Syntax: 'c')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_ArgumentNames()
        {
            string source = @"
class C
{
    public C(int i, char c)
    {
    }

    public C(long i, char c)
    {
    }

    void M(dynamic d, dynamic e)
    {
        var x = /*<bind>*/new C(i: d, c: e)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(i: d, c: e)')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'e')
  ArgumentNames(2):
    ""i""
    ""c""
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_ArgumentRefKinds()
        {
            string source = @"
class C
{
    public C(ref object i, out int j, char c)
    {
        j = 0;
    }

    void M(object d, dynamic e)
    {
        int k;
        var x = /*<bind>*/new C(ref d, out k, e)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(ref d, out k, e)')
  Arguments(3):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
      ILocalReferenceOperation: k (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'k')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds(3):
    Ref
    Out
    None
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_Initializer()
        {
            string source = @"
class C
{
    public int X;

    public C(char c)
    {
    }

    void M(dynamic d)
    {
        var x = /*<bind>*/new C(d) { X = 0 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(d) { X = 0 }')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ X = 0 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 C.X (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C) (Syntax: 'X')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_AllFields()
        {
            string source = @"
class C
{
    public int X;

    public C(ref int i, char c)
    {
    }

    public C(ref int i, long c)
    {
    }

    void M(dynamic d)
    {
        int i = 0;
        var x = /*<bind>*/new C(ref i, c: d) { X = 0 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(ref i ... ) { X = 0 }')
  Arguments(2):
      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  ArgumentNames(2):
    ""null""
    ""c""
  ArgumentRefKinds(2):
    Ref
    None
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ X = 0 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 C.X (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C) (Syntax: 'X')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_ErrorBadDynamicMethodArgLambda()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        dynamic y = null;
        /*<bind>*/new C(delegate { }, y)/*</bind>*/;
    }

    public C(Action a, Action y)
    {
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(delegate { }, y)')
  Arguments(2):
      IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'delegate { }')
        IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ }')
          IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '{ }')
            ReturnedValue: 
              null
      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: dynamic) (Syntax: 'y')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //         /*<bind>*/new C(delegate { }, y)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "delegate { }").WithLocation(9, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_OVerloadResolutionFailure()
        {
            string source = @"
class C
{
    public C()
    {
    }

    public C(int i, int j)
    {
    }

    void M(dynamic d)
    {
        var x = /*<bind>*/new C(d)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: C, IsInvalid) (Syntax: 'new C(d)')
  Children(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS7036: There is no argument given that corresponds to the required formal parameter 'j' of 'C.C(int, int)'
                //         var x = /*<bind>*/new C(d)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("j", "C.C(int, int)").WithLocation(14, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
