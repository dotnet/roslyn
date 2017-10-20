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
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17591")]
        public void Test_UnaryOperatorExpression_Type_Plus_System_SByte()
        {
            string source = @"
class A
{
    System.SByte Method()
    {
        System.SByte i = default(System.SByte);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Byte()
        {
            string source = @"
class A
{
    System.Byte Method()
    {
        System.Byte i = default(System.Byte);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Byte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Int16()
        {
            string source = @"
class A
{
    System.Int16 Method()
    {
        System.Int16 i = default(System.Int16);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_UInt16()
        {
            string source = @"
class A
{
    System.UInt16 Method()
    {
        System.UInt16 i = default(System.UInt16);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Int32()
        {
            string source = @"
class A
{
    System.Int32 Method()
    {
        System.Int32 i = default(System.Int32);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_UInt32()
        {
            string source = @"
class A
{
    System.UInt32 Method()
    {
        System.UInt32 i = default(System.UInt32);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.UInt32) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Int64()
        {
            string source = @"
class A
{
    System.Int64 Method()
    {
        System.Int64 i = default(System.Int64);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int64) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_UInt64()
        {
            string source = @"
class A
{
    System.UInt64 Method()
    {
        System.UInt64 i = default(System.UInt64);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.UInt64) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Char()
        {
            string source = @"
class A
{
    System.Char Method()
    {
        System.Char i = default(System.Char);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Char, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Decimal()
        {
            string source = @"
class A
{
    System.Decimal Method()
    {
        System.Decimal i = default(System.Decimal);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Decimal) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Decimal) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Single()
        {
            string source = @"
class A
{
    System.Single Method()
    {
        System.Single i = default(System.Single);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Single) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Single) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Double()
        {
            string source = @"
class A
{
    System.Double Method()
    {
        System.Double i = default(System.Double);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Double) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Double) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Boolean()
        {
            string source = @"
class A
{
    System.Boolean Method()
    {
        System.Boolean i = default(System.Boolean);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Boolean, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_System_Object()
        {
            string source = @"
class A
{
    System.Object Method()
    {
        System.Object i = default(System.Object);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Object, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_SByte()
        {
            string source = @"
class A
{
    System.SByte Method()
    {
        System.SByte i = default(System.SByte);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Byte()
        {
            string source = @"
class A
{
    System.Byte Method()
    {
        System.Byte i = default(System.Byte);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Byte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Int16()
        {
            string source = @"
class A
{
    System.Int16 Method()
    {
        System.Int16 i = default(System.Int16);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_UInt16()
        {
            string source = @"
class A
{
    System.UInt16 Method()
    {
        System.UInt16 i = default(System.UInt16);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Int32()
        {
            string source = @"
class A
{
    System.Int32 Method()
    {
        System.Int32 i = default(System.Int32);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_UInt32()
        {
            string source = @"
class A
{
    System.UInt32 Method()
    {
        System.UInt32 i = default(System.UInt32);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int64, IsInvalid) (Syntax: '-i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int64, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt32, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Int64()
        {
            string source = @"
class A
{
    System.Int64 Method()
    {
        System.Int64 i = default(System.Int64);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int64) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_UInt64()
        {
            string source = @"
class A
{
    System.UInt64 Method()
    {
        System.UInt64 i = default(System.UInt64);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt64, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Char()
        {
            string source = @"
class A
{
    System.Char Method()
    {
        System.Char i = default(System.Char);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Char, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Decimal()
        {
            string source = @"
class A
{
    System.Decimal Method()
    {
        System.Decimal i = default(System.Decimal);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Decimal) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Decimal) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Single()
        {
            string source = @"
class A
{
    System.Single Method()
    {
        System.Single i = default(System.Single);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Single) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Single) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Double()
        {
            string source = @"
class A
{
    System.Double Method()
    {
        System.Double i = default(System.Double);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Double) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Double) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Boolean()
        {
            string source = @"
class A
{
    System.Boolean Method()
    {
        System.Boolean i = default(System.Boolean);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Boolean, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_System_Object()
        {
            string source = @"
class A
{
    System.Object Method()
    {
        System.Object i = default(System.Object);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Object, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_SByte()
        {
            string source = @"
class A
{
    System.SByte Method()
    {
        System.SByte i = default(System.SByte);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.SByte A.Method()) (OperationKind.Invocation, IsExpression, Type: System.SByte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Byte()
        {
            string source = @"
class A
{
    System.Byte Method()
    {
        System.Byte i = default(System.Byte);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Byte A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Byte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Int16()
        {
            string source = @"
class A
{
    System.Int16 Method()
    {
        System.Int16 i = default(System.Int16);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Int16 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_UInt16()
        {
            string source = @"
class A
{
    System.UInt16 Method()
    {
        System.UInt16 i = default(System.UInt16);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.UInt16 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Int32()
        {
            string source = @"
class A
{
    System.Int32 Method()
    {
        System.Int32 i = default(System.Int32);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.Int32 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_UInt32()
        {
            string source = @"
class A
{
    System.UInt32 Method()
    {
        System.UInt32 i = default(System.UInt32);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.UInt32) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.UInt32 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt32) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Int64()
        {
            string source = @"
class A
{
    System.Int64 Method()
    {
        System.Int64 i = default(System.Int64);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int64) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.Int64 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_UInt64()
        {
            string source = @"
class A
{
    System.UInt64 Method()
    {
        System.UInt64 i = default(System.UInt64);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.UInt64) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.UInt64 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt64) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Char()
        {
            string source = @"
class A
{
    System.Char Method()
    {
        System.Char i = default(System.Char);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Char A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Char, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Decimal()
        {
            string source = @"
class A
{
    System.Decimal Method()
    {
        System.Decimal i = default(System.Decimal);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Decimal) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.Decimal A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Decimal) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Single()
        {
            string source = @"
class A
{
    System.Single Method()
    {
        System.Single i = default(System.Single);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Single) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.Single A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Single) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Double()
        {
            string source = @"
class A
{
    System.Double Method()
    {
        System.Double i = default(System.Double);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Double) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.Double A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Double) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Boolean()
        {
            string source = @"
class A
{
    System.Boolean Method()
    {
        System.Boolean i = default(System.Boolean);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.Boolean A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Boolean, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_System_Object()
        {
            string source = @"
class A
{
    System.Object Method()
    {
        System.Object i = default(System.Object);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( System.Object A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Object, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_SByte()
        {
            string source = @"
class A
{
    System.SByte Method()
    {
        System.SByte i = default(System.SByte);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.SByte A.Method()) (OperationKind.Invocation, IsExpression, Type: System.SByte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Byte()
        {
            string source = @"
class A
{
    System.Byte Method()
    {
        System.Byte i = default(System.Byte);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Byte A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Byte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Int16()
        {
            string source = @"
class A
{
    System.Int16 Method()
    {
        System.Int16 i = default(System.Int16);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Int16 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_UInt16()
        {
            string source = @"
class A
{
    System.UInt16 Method()
    {
        System.UInt16 i = default(System.UInt16);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.UInt16 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Int32()
        {
            string source = @"
class A
{
    System.Int32 Method()
    {
        System.Int32 i = default(System.Int32);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( System.Int32 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_UInt32()
        {
            string source = @"
class A
{
    System.UInt32 Method()
    {
        System.UInt32 i = default(System.UInt32);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int64, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int64, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.UInt32 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt32, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Int64()
        {
            string source = @"
class A
{
    System.Int64 Method()
    {
        System.Int64 i = default(System.Int64);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int64) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( System.Int64 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_UInt64()
        {
            string source = @"
class A
{
    System.UInt64 Method()
    {
        System.UInt64 i = default(System.UInt64);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( System.UInt64 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt64, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Char()
        {
            string source = @"
class A
{
    System.Char Method()
    {
        System.Char i = default(System.Char);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Char A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Char, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Decimal()
        {
            string source = @"
class A
{
    System.Decimal Method()
    {
        System.Decimal i = default(System.Decimal);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Decimal) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( System.Decimal A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Decimal) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Single()
        {
            string source = @"
class A
{
    System.Single Method()
    {
        System.Single i = default(System.Single);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Single) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( System.Single A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Single) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Double()
        {
            string source = @"
class A
{
    System.Double Method()
    {
        System.Double i = default(System.Double);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Double) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( System.Double A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Double) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Boolean()
        {
            string source = @"
class A
{
    System.Boolean Method()
    {
        System.Boolean i = default(System.Boolean);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( System.Boolean A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Boolean, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_System_Object()
        {
            string source = @"
class A
{
    System.Object Method()
    {
        System.Object i = default(System.Object);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( System.Object A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Object, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_LogicalNot_System_Boolean()
        {
            string source = @"
class A
{
    System.Boolean Method()
    {
        System.Boolean i = default(System.Boolean);
        return /*<bind>*/!i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Not) (OperationKind.UnaryOperator, IsExpression, Type: System.Boolean) (Syntax: '!i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_LogicalNot_System_Boolean()
        {
            string source = @"
class A
{
    System.Boolean Method()
    {
        System.Boolean i = default(System.Boolean);
        return /*<bind>*/!Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Not) (OperationKind.UnaryOperator, IsExpression, Type: System.Boolean) (Syntax: '!Method()')
  Operand: 
    IInvocationOperation ( System.Boolean A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Boolean) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_SByte()
        {
            string source = @"
class A
{
    System.SByte Method()
    {
        System.SByte i = default(System.SByte);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Byte()
        {
            string source = @"
class A
{
    System.Byte Method()
    {
        System.Byte i = default(System.Byte);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Byte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Int16()
        {
            string source = @"
class A
{
    System.Int16 Method()
    {
        System.Int16 i = default(System.Int16);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_UInt16()
        {
            string source = @"
class A
{
    System.UInt16 Method()
    {
        System.UInt16 i = default(System.UInt16);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Int32()
        {
            string source = @"
class A
{
    System.Int32 Method()
    {
        System.Int32 i = default(System.Int32);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_UInt32()
        {
            string source = @"
class A
{
    System.UInt32 Method()
    {
        System.UInt32 i = default(System.UInt32);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.UInt32) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Int64()
        {
            string source = @"
class A
{
    System.Int64 Method()
    {
        System.Int64 i = default(System.Int64);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int64) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_UInt64()
        {
            string source = @"
class A
{
    System.UInt64 Method()
    {
        System.UInt64 i = default(System.UInt64);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.UInt64) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.UInt64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Char()
        {
            string source = @"
class A
{
    System.Char Method()
    {
        System.Char i = default(System.Char);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Char, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Decimal()
        {
            string source = @"
class A
{
    System.Decimal Method()
    {
        System.Decimal i = default(System.Decimal);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Decimal, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Single()
        {
            string source = @"
class A
{
    System.Single Method()
    {
        System.Single i = default(System.Single);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Single, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Double()
        {
            string source = @"
class A
{
    System.Double Method()
    {
        System.Double i = default(System.Double);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Double, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Boolean()
        {
            string source = @"
class A
{
    System.Boolean Method()
    {
        System.Boolean i = default(System.Boolean);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Boolean, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_System_Object()
        {
            string source = @"
class A
{
    System.Object Method()
    {
        System.Object i = default(System.Object);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Object, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_SByte()
        {
            string source = @"
class A
{
    System.SByte Method()
    {
        System.SByte i = default(System.SByte);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.SByte A.Method()) (OperationKind.Invocation, IsExpression, Type: System.SByte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Byte()
        {
            string source = @"
class A
{
    System.Byte Method()
    {
        System.Byte i = default(System.Byte);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Byte A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Byte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Int16()
        {
            string source = @"
class A
{
    System.Int16 Method()
    {
        System.Int16 i = default(System.Int16);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Int16 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_UInt16()
        {
            string source = @"
class A
{
    System.UInt16 Method()
    {
        System.UInt16 i = default(System.UInt16);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.UInt16 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Int32()
        {
            string source = @"
class A
{
    System.Int32 Method()
    {
        System.Int32 i = default(System.Int32);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.Int32 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_UInt32()
        {
            string source = @"
class A
{
    System.UInt32 Method()
    {
        System.UInt32 i = default(System.UInt32);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.UInt32) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.UInt32 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt32) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Int64()
        {
            string source = @"
class A
{
    System.Int64 Method()
    {
        System.Int64 i = default(System.Int64);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int64) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.Int64 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_UInt64()
        {
            string source = @"
class A
{
    System.UInt64 Method()
    {
        System.UInt64 i = default(System.UInt64);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.UInt64) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.UInt64 A.Method()) (OperationKind.Invocation, IsExpression, Type: System.UInt64) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Char()
        {
            string source = @"
class A
{
    System.Char Method()
    {
        System.Char i = default(System.Char);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Method()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Char A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Char, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Decimal()
        {
            string source = @"
class A
{
    System.Decimal Method()
    {
        System.Decimal i = default(System.Decimal);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.Decimal A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Decimal, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Single()
        {
            string source = @"
class A
{
    System.Single Method()
    {
        System.Single i = default(System.Single);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.Single A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Single, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Double()
        {
            string source = @"
class A
{
    System.Double Method()
    {
        System.Double i = default(System.Double);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.Double A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Double, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Boolean()
        {
            string source = @"
class A
{
    System.Boolean Method()
    {
        System.Boolean i = default(System.Boolean);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.Boolean A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Boolean, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_System_Object()
        {
            string source = @"
class A
{
    System.Object Method()
    {
        System.Object i = default(System.Object);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( System.Object A.Method()) (OperationKind.Invocation, IsExpression, Type: System.Object, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_dynamic()
        {
            string source = @"
class A
{
    dynamic Method()
    {
        dynamic i = default(dynamic);
        return /*<bind>*/+i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: dynamic) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: dynamic) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_dynamic()
        {
            string source = @"
class A
{
    dynamic Method()
    {
        dynamic i = default(dynamic);
        return /*<bind>*/-i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: dynamic) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: dynamic) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_dynamic()
        {
            string source = @"
class A
{
    dynamic Method()
    {
        dynamic i = default(dynamic);
        return /*<bind>*/~i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: dynamic) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: dynamic) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_LogicalNot_dynamic()
        {
            string source = @"
class A
{
    dynamic Method()
    {
        dynamic i = default(dynamic);
        return /*<bind>*/!i/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Not) (OperationKind.UnaryOperator, IsExpression, Type: dynamic) (Syntax: '!i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: dynamic) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_dynamic()
        {
            string source = @"
class A
{
    dynamic Method()
    {
        dynamic i = default(dynamic);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: dynamic) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( dynamic A.Method()) (OperationKind.Invocation, IsExpression, Type: dynamic) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_dynamic()
        {
            string source = @"
class A
{
    dynamic Method()
    {
        dynamic i = default(dynamic);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: dynamic) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( dynamic A.Method()) (OperationKind.Invocation, IsExpression, Type: dynamic) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_dynamic()
        {
            string source = @"
class A
{
    dynamic Method()
    {
        dynamic i = default(dynamic);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: dynamic) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( dynamic A.Method()) (OperationKind.Invocation, IsExpression, Type: dynamic) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_LogicalNot_dynamic()
        {
            string source = @"
class A
{
    dynamic Method()
    {
        dynamic i = default(dynamic);
        return /*<bind>*/!Method()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Not) (OperationKind.UnaryOperator, IsExpression, Type: dynamic) (Syntax: '!Method()')
  Operand: 
    IInvocationOperation ( dynamic A.Method()) (OperationKind.Invocation, IsExpression, Type: dynamic) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_Enum()
        {
            string source = @"
class A
{
    Enum Method()
    {
        Enum i = default(Enum);
        return /*<bind>*/+i/*</bind>*/;
    }
}
enum Enum { A, B }
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: Enum, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_Enum()
        {
            string source = @"
class A
{
    Enum Method()
    {
        Enum i = default(Enum);
        return /*<bind>*/-i/*</bind>*/;
    }
}
enum Enum { A, B }
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: Enum, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_Enum()
        {
            string source = @"
class A
{
    Enum Method()
    {
        Enum i = default(Enum);
        return /*<bind>*/~i/*</bind>*/;
    }
}
enum Enum { A, B }
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: Enum) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: Enum) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_Enum()
        {
            string source = @"
class A
{
    Enum Method()
    {
        Enum i = default(Enum);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}
enum Enum { A, B }
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( Enum A.Method()) (OperationKind.Invocation, IsExpression, Type: Enum, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_Enum()
        {
            string source = @"
class A
{
    Enum Method()
    {
        Enum i = default(Enum);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}
enum Enum { A, B }
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( Enum A.Method()) (OperationKind.Invocation, IsExpression, Type: Enum, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsInvalid, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_Enum()
        {
            string source = @"
class A
{
    Enum Method()
    {
        Enum i = default(Enum);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}
enum Enum { A, B }
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperationKind.UnaryOperator, IsExpression, Type: Enum) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( Enum A.Method()) (OperationKind.Invocation, IsExpression, Type: Enum) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Plus_CustomType()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/+i/*</bind>*/;
    }
}
public struct CustomType
{
    public static CustomType operator +(CustomType x)
    {
        return x;
    }
    public static CustomType operator -(CustomType x)
    {
        return x;
    }
    public static CustomType operator !(CustomType x)
    {
        return x;
    }
    public static CustomType operator ~(CustomType x)
    {
        return x;
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperatorMethod: CustomType CustomType.op_UnaryPlus(CustomType x)) (OperationKind.UnaryOperator, IsExpression, Type: CustomType) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: CustomType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_Minus_CustomType()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/-i/*</bind>*/;
    }
}
public struct CustomType
{
    public static CustomType operator +(CustomType x)
    {
        return x;
    }
    public static CustomType operator -(CustomType x)
    {
        return x;
    }
    public static CustomType operator !(CustomType x)
    {
        return x;
    }
    public static CustomType operator ~(CustomType x)
    {
        return x;
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperatorMethod: CustomType CustomType.op_UnaryNegation(CustomType x)) (OperationKind.UnaryOperator, IsExpression, Type: CustomType) (Syntax: '-i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: CustomType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_BitwiseNot_CustomType()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/~i/*</bind>*/;
    }
}
public struct CustomType
{
    public static CustomType operator +(CustomType x)
    {
        return x;
    }
    public static CustomType operator -(CustomType x)
    {
        return x;
    }
    public static CustomType operator !(CustomType x)
    {
        return x;
    }
    public static CustomType operator ~(CustomType x)
    {
        return x;
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperatorMethod: CustomType CustomType.op_OnesComplement(CustomType x)) (OperationKind.UnaryOperator, IsExpression, Type: CustomType) (Syntax: '~i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: CustomType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Type_LogicalNot_CustomType()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/!i/*</bind>*/;
    }
}
public struct CustomType
{
    public static CustomType operator +(CustomType x)
    {
        return x;
    }
    public static CustomType operator -(CustomType x)
    {
        return x;
    }
    public static CustomType operator !(CustomType x)
    {
        return x;
    }
    public static CustomType operator ~(CustomType x)
    {
        return x;
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Not) (OperatorMethod: CustomType CustomType.op_LogicalNot(CustomType x)) (OperationKind.UnaryOperator, IsExpression, Type: CustomType) (Syntax: '!i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: CustomType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Plus_CustomType()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/+Method()/*</bind>*/;
    }
}
public struct CustomType
{
    public static CustomType operator +(CustomType x)
    {
        return x;
    }
    public static CustomType operator -(CustomType x)
    {
        return x;
    }
    public static CustomType operator !(CustomType x)
    {
        return x;
    }
    public static CustomType operator ~(CustomType x)
    {
        return x;
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperatorMethod: CustomType CustomType.op_UnaryPlus(CustomType x)) (OperationKind.UnaryOperator, IsExpression, Type: CustomType) (Syntax: '+Method()')
  Operand: 
    IInvocationOperation ( CustomType A.Method()) (OperationKind.Invocation, IsExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_Minus_CustomType()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/-Method()/*</bind>*/;
    }
}
public struct CustomType
{
    public static CustomType operator +(CustomType x)
    {
        return x;
    }
    public static CustomType operator -(CustomType x)
    {
        return x;
    }
    public static CustomType operator !(CustomType x)
    {
        return x;
    }
    public static CustomType operator ~(CustomType x)
    {
        return x;
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Minus) (OperatorMethod: CustomType CustomType.op_UnaryNegation(CustomType x)) (OperationKind.UnaryOperator, IsExpression, Type: CustomType) (Syntax: '-Method()')
  Operand: 
    IInvocationOperation ( CustomType A.Method()) (OperationKind.Invocation, IsExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_BitwiseNot_CustomType()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/~Method()/*</bind>*/;
    }
}
public struct CustomType
{
    public static CustomType operator +(CustomType x)
    {
        return x;
    }
    public static CustomType operator -(CustomType x)
    {
        return x;
    }
    public static CustomType operator !(CustomType x)
    {
        return x;
    }
    public static CustomType operator ~(CustomType x)
    {
        return x;
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.BitwiseNegation) (OperatorMethod: CustomType CustomType.op_OnesComplement(CustomType x)) (OperationKind.UnaryOperator, IsExpression, Type: CustomType) (Syntax: '~Method()')
  Operand: 
    IInvocationOperation ( CustomType A.Method()) (OperationKind.Invocation, IsExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Method_LogicalNot_CustomType()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/!Method()/*</bind>*/;
    }
}
public struct CustomType
{
    public static CustomType operator +(CustomType x)
    {
        return x;
    }
    public static CustomType operator -(CustomType x)
    {
        return x;
    }
    public static CustomType operator !(CustomType x)
    {
        return x;
    }
    public static CustomType operator ~(CustomType x)
    {
        return x;
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Not) (OperatorMethod: CustomType CustomType.op_LogicalNot(CustomType x)) (OperationKind.UnaryOperator, IsExpression, Type: CustomType) (Syntax: '!Method()')
  Operand: 
    IInvocationOperation ( CustomType A.Method()) (OperationKind.Invocation, IsExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: A, IsImplicit) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18135, "https://github.com/dotnet/roslyn/issues/18135")]
        [WorkItem(18160, "https://github.com/dotnet/roslyn/issues/18160")]
        public void Test_UnaryOperatorExpression_Type_And_TrueFalse()
        {
            string source = @"

public struct S
{
    private int value;
    public S(int v)
    {
        value = v;
    }
    public static S operator |(S x, S y)
    {
        return new S(x.value - y.value);
    }
    public static S operator &(S x, S y)
    {
        return new S(x.value + y.value);
    }
    public static bool operator true(S x)
    {
        return x.value > 0;
    }
    public static bool operator false(S x)
    {
        return x.value <= 0;
    }
}
 
class C
{
    public void M()
    {
        var x = new S(2);
        var y = new S(1);
        /*<bind>*/if (x && y) { }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if (x && y) { }')
  Condition: 
    IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: System.Boolean S.op_True(S x)) (OperationKind.UnaryOperator, IsExpression, Type: System.Boolean, IsImplicit) (Syntax: 'x && y')
      Operand: 
        IOperation:  (OperationKind.None, Type: null) (Syntax: 'x && y')
          Children(2):
              ILocalReferenceOperation: x (OperationKind.LocalReference, IsExpression, Type: S) (Syntax: 'x')
              ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: S) (Syntax: 'y')
  WhenTrue: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ }')
  WhenFalse: 
    null
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18135, "https://github.com/dotnet/roslyn/issues/18135")]
        [WorkItem(18160, "https://github.com/dotnet/roslyn/issues/18160")]
        public void Test_UnaryOperatorExpression_Type_Or_TrueFalse()
        {
            string source = @"

public struct S
{
    private int value;
    public S(int v)
    {
        value = v;
    }
    public static S operator |(S x, S y)
    {
        return new S(x.value - y.value);
    }
    public static S operator &(S x, S y)
    {
        return new S(x.value + y.value);
    }
    public static bool operator true(S x)
    {
        return x.value > 0;
    }
    public static bool operator false(S x)
    {
        return x.value <= 0;
    }
}
 
class C
{
    public void M()
    {
        var x = new S(2);
        var y = new S(1);
        /*<bind>*/if (x || y) { }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if (x || y) { }')
  Condition: 
    IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: System.Boolean S.op_True(S x)) (OperationKind.UnaryOperator, IsExpression, Type: System.Boolean, IsImplicit) (Syntax: 'x || y')
      Operand: 
        IOperation:  (OperationKind.None, Type: null) (Syntax: 'x || y')
          Children(2):
              ILocalReferenceOperation: x (OperationKind.LocalReference, IsExpression, Type: S) (Syntax: 'x')
              ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: S) (Syntax: 'y')
  WhenTrue: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ }')
  WhenFalse: 
    null
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_With_CustomType_NoRightOperator()
        {
            string source = @"
class A
{
    CustomType Method()
    {
        CustomType i = default(CustomType);
        return /*<bind>*/+i/*</bind>*/;
    }
}
public struct CustomType
{
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: CustomType, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_With_CustomType_DerivedTypes()
        {
            string source = @"
class A
{
    BaseType Method()
    {
        var i = default(DerivedType);
        return /*<bind>*/+i/*</bind>*/;
    }
}
public class BaseType
{
    public static BaseType operator +(BaseType x)
    {
        return new BaseType();
    }
}

public class DerivedType : BaseType
{
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperatorMethod: BaseType BaseType.op_UnaryPlus(BaseType x)) (OperationKind.UnaryOperator, IsExpression, Type: BaseType) (Syntax: '+i')
  Operand: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: BaseType, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: DerivedType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_With_CustomType_ImplicitConversion()
        {
            string source = @"
class A
{
    BaseType Method()
    {
        var i = default(DerivedType);
        return /*<bind>*/+i/*</bind>*/;
    }
}
public class BaseType
{
    public static BaseType operator +(BaseType x)
    {
        return new BaseType();
    }
}

public class DerivedType 
{
    public static implicit operator BaseType(DerivedType x)
    {
        return new BaseType();
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: DerivedType, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_With_CustomType_ExplicitConversion()
        {
            string source = @"
class A
{
    BaseType Method()
    {
        var i = default(DerivedType);
        return /*<bind>*/+i/*</bind>*/;
    }
}
public class BaseType
{
    public static BaseType operator +(BaseType x)
    {
        return new BaseType();
    }
}

public class DerivedType 
{
    public static explicit operator BaseType(DerivedType x)
    {
        return new BaseType();
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: DerivedType, IsInvalid) (Syntax: 'i')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_With_CustomType_Malformed_Operator()
        {
            string source = @"
class A
{
    BaseType Method()
    {
        var i = default(BaseType);
        return /*<bind>*/+i/*</bind>*/;
    }
}
public class BaseType
{
    public static BaseType operator +(int x)
    {
        return new BaseType();
    }
}
";
            string expectedOperationTree = @"
IUnaryOperation (UnaryOperatorKind.Plus) (OperationKind.UnaryOperator, IsExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: BaseType, IsInvalid) (Syntax: 'i')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(18160, "https://github.com/dotnet/roslyn/issues/18160")]
        public void Test_BinaryExpressionSyntax_Type_And_TrueFalse_Condition()
        {
            string source = @"
public struct S
{
    private int value;
    public S(int v)
    {
        value = v;
    }
    public static S operator |(S x, S y)
    {
        return new S(x.value - y.value);
    }
    public static S operator &(S x, S y)
    {
        return new S(x.value + y.value);
    }
    public static bool operator true(S x)
    {
        return x.value > 0;
    }
    public static bool operator false(S x)
    {
        return x.value <= 0;
    }
}

class C
{
    public void M()
    {
        var x = new S(2);
        var y = new S(1);
        if (/*<bind>*/x && y/*</bind>*/) { }
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: null) (Syntax: 'x && y')
  Children(2):
      ILocalReferenceOperation: x (OperationKind.LocalReference, IsExpression, Type: S) (Syntax: 'x')
      ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: S) (Syntax: 'y')
";
            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_IncrementExpression()
        {
            string source = @"
class A
{
    int Method()
    {
        var i = 1;
        return /*<bind>*/++i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: '++i')
  Target: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_DecrementExpression()
        {
            string source = @"
class A
{
    int Method()
    {
        var i = 1;
        return /*<bind>*/--i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIncrementOrDecrementOperation (Prefix) (OperationKind.Decrement, IsExpression, Type: System.Int32) (Syntax: '--i')
  Target: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Nullable()
        {
            string source = @"
class A
{
    void Method()
    {
        var i = /*<bind>*/(int?)1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConversionOperation (Explicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32?) (Syntax: '(int?)1')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<CastExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Test_UnaryOperatorExpression_Pointer()
        {
            string source = @"
class A
{
    unsafe void Method()
    {
        int[] a = new int[5] {10, 20, 30, 40, 50};
        
        fixed (int* p = &a[0])  
        {  
            int* p2 = p;  
            int p1 = /*<bind>*/*p2/*</bind>*/;  
        }  
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: null) (Syntax: '*p2')
  Children(1):
      ILocalReferenceOperation: p2 (OperationKind.LocalReference, IsExpression, Type: System.Int32*) (Syntax: 'p2')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyLiftedUnaryOperators1()
        {
            var source = @"
 class C
 {
     void F(int? x)
     {
         var y = /*<bind>*/-x/*</bind>*/;
     }
 }";

            string expectedOperationTree =
@"
IUnaryOperation (UnaryOperatorKind.Minus, IsLifted) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32?) (Syntax: '-x')
  Operand: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32?) (Syntax: 'x')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyNonLiftedUnaryOperators1()
        {
            var source = @"
class C
{
    void F(int x)
    {
        var y = /*<bind>*/-x/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"
IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, IsExpression, Type: System.Int32) (Syntax: '-x')
  Operand: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyLiftedUserDefinedUnaryOperators1()
        {
            var source = @"
struct C
{
    public static C operator -(C c) { }
    void F(C? x)
    {
        var y = /*<bind>*/-x/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"
IUnaryOperation (UnaryOperatorKind.Minus, IsLifted) (OperatorMethod: C C.op_UnaryNegation(C c)) (OperationKind.UnaryOperator, IsExpression, Type: C?) (Syntax: '-x')
  Operand: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: C?) (Syntax: 'x')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyNonLiftedUserDefinedUnaryOperators1()
        {
            var source = @"
struct C
{
    public static C operator -(C c) { }
    void F(C x)
    {
        var y = /*<bind>*/-x/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"
IUnaryOperation (UnaryOperatorKind.Minus) (OperatorMethod: C C.op_UnaryNegation(C c)) (OperationKind.UnaryOperator, IsExpression, Type: C) (Syntax: '-x')
  Operand: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: C) (Syntax: 'x')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }
    }
}
