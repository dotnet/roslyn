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
        public void VerifyLiftedBinaryOperators1()
        {
            var source = @"
class C
{
    void F(int? x, int? y)
    {
        var z = /*<bind>*/x + y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Int32?) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32?) (Syntax: 'y')";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyNonLiftedBinaryOperators1()
        {
            var source = @"
class C
{
    void F(int x, int y)
    {
        var z = /*<bind>*/x + y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyLiftedUserDefinedBinaryOperators1()
        {
            var source = @"
struct C
{
    public static C operator +(C c1, C c2) { }
    void F(C? x, C? y)
    {
        var z = /*<bind>*/x + y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAdd-IsLifted) (OperatorMethod: C C.op_Addition(C c1, C c2)) (OperationKind.BinaryOperatorExpression, Type: C?) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C?) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C?) (Syntax: 'y')";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyNonLiftedUserDefinedBinaryOperators1()
        {
            var source = @"
struct C
{
    public static C operator +(C c1, C c2) { }
    void F(C x, C y)
    {
        var z = /*<bind>*/x + y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAdd) (OperatorMethod: C C.op_Addition(C c1, C c2)) (OperationKind.BinaryOperatorExpression, Type: C) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }
    }
}
