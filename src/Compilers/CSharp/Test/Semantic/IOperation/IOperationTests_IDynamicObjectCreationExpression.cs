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
IDynamicObjectCreationExpression (Name: C) (OperationKind.TypeParameterObjectCreationExpression, Type: C) (Syntax: 'new C(d)')
  ApplicableSymbols(1):
    Symbol: C..ctor(System.Int32 i)
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: null
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
IDynamicObjectCreationExpression (Name: C) (OperationKind.TypeParameterObjectCreationExpression, Type: C) (Syntax: 'new C(d)')
  ApplicableSymbols(2):
    Symbol: C..ctor(System.Int32 i)
    Symbol: C..ctor(System.Int64 i)
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: null
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
IDynamicObjectCreationExpression (Name: C) (OperationKind.TypeParameterObjectCreationExpression, Type: C) (Syntax: 'new C(d, c)')
  ApplicableSymbols(2):
    Symbol: C..ctor(System.Int32 i, System.Char c)
    Symbol: C..ctor(System.Int64 i, System.Char c)
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
      ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'c')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: null
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
IDynamicObjectCreationExpression (Name: C) (OperationKind.TypeParameterObjectCreationExpression, Type: C) (Syntax: 'new C(i: d, c: e)')
  ApplicableSymbols(2):
    Symbol: C..ctor(System.Int32 i, System.Char c)
    Symbol: C..ctor(System.Int64 i, System.Char c)
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'e')
  ArgumentNames(2):
    ""i""
    ""c""
  ArgumentRefKinds(0)
  Initializer: null
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
IDynamicObjectCreationExpression (Name: C) (OperationKind.TypeParameterObjectCreationExpression, Type: C) (Syntax: 'new C(ref d, out k, e)')
  ApplicableSymbols(1):
    Symbol: C..ctor(ref System.Object i, out System.Int32 j, System.Char c)
  Arguments(3):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'k')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds(3):
    Ref
    Out
    None
  Initializer: null
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
IDynamicObjectCreationExpression (Name: C) (OperationKind.TypeParameterObjectCreationExpression, Type: C) (Syntax: 'new C(d) { X = 0 }')
  ApplicableSymbols(1):
    Symbol: C..ctor(System.Char c)
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: C) (Syntax: '{ X = 0 }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'X = 0')
            Left: IFieldReferenceExpression: System.Int32 C.X (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'X')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IDynamicObjectCreationExpression (Name: C) (OperationKind.TypeParameterObjectCreationExpression, Type: C) (Syntax: 'new C(ref i ... ) { X = 0 }')
  ApplicableSymbols(2):
    Symbol: C..ctor(ref System.Int32 i, System.Char c)
    Symbol: C..ctor(ref System.Int32 i, System.Int64 c)
  Arguments(2):
      ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(2):
    ""null""
    ""c""
  ArgumentRefKinds(2):
    Ref
    None
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: C) (Syntax: '{ X = 0 }')
      Initializers(1):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'X = 0')
            Left: IFieldReferenceExpression: System.Int32 C.X (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'X')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IDynamicObjectCreationExpression (Name: C) (OperationKind.TypeParameterObjectCreationExpression, Type: C, IsInvalid) (Syntax: 'new C(delegate { }, y)')
  ApplicableSymbols(1):
    Symbol: C..ctor(System.Action a, System.Action y)
  Arguments(2):
      IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'delegate { }')
        IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ }')
          IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: '{ }')
            ReturnedValue: null
      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'y')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: null
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
IInvalidExpression (OperationKind.InvalidExpression, Type: C, IsInvalid) (Syntax: 'new C(d)')
  Children(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
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
