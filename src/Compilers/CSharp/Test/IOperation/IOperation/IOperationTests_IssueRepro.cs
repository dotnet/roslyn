// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IssueRepro : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NullableUserDefinedConversion_ExplicitCast_Issue81781()
        {
            var src = @"
struct S1
{
    public static implicit operator S1(int x) => default;
}

class Program
{
    static void Main()
    {
        Test1();
    }

    static S1? Test1()
    {
        return /*<bind>*/ (S1?)10 /*</bind>*/;
    }   
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            // This should not throw NullReferenceException
            VerifyOperationTreeForTest<CastExpressionSyntax>(comp, """
IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1 S1.op_Implicit(System.Int32 x)) (OperationKind.Conversion, Type: S1?) (Syntax: '(S1?)10')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: S1 S1.op_Implicit(System.Int32 x))
  Operand:
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
""");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NonNullableUserDefinedConversion_ExplicitCast()
        {
            var src = @"
struct S1
{
    public static implicit operator S1(int x) => default;
}

class Program
{
    static void Main()
    {
        Test1();
    }

    static S1 Test1()
    {
        return /*<bind>*/ (S1)10 /*</bind>*/;
    }   
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            VerifyOperationTreeForTest<CastExpressionSyntax>(comp, """
IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1 S1.op_Implicit(System.Int32 x)) (OperationKind.Conversion, Type: S1) (Syntax: '(S1)10')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: S1 S1.op_Implicit(System.Int32 x))
  Operand:
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
""");
        }
    }
}
