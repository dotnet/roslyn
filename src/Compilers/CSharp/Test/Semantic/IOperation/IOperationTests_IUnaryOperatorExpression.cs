﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Byte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.UInt32) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.UInt64) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Char, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DecimalPlus) (OperationKind.UnaryOperatorExpression, Type: System.Decimal) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Decimal) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.FloatingPlus) (OperationKind.UnaryOperatorExpression, Type: System.Single) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.FloatingPlus) (OperationKind.UnaryOperatorExpression, Type: System.Double) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Boolean, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Object, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Byte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int64, IsInvalid) (Syntax: '-i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int64, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt32, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt64, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Char, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DecimalMinus) (OperationKind.UnaryOperatorExpression, Type: System.Decimal) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Decimal) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.FloatingMinus) (OperationKind.UnaryOperatorExpression, Type: System.Single) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.FloatingMinus) (OperationKind.UnaryOperatorExpression, Type: System.Double) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Boolean, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Object, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.SByte A.Method()) (OperationKind.InvocationExpression, Type: System.SByte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Byte A.Method()) (OperationKind.InvocationExpression, Type: System.Byte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Int16 A.Method()) (OperationKind.InvocationExpression, Type: System.Int16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.UInt16 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.Int32 A.Method()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.UInt32) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.UInt32 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.Int64 A.Method()) (OperationKind.InvocationExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.UInt64) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.UInt64 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerPlus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '+Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Char A.Method()) (OperationKind.InvocationExpression, Type: System.Char, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DecimalPlus) (OperationKind.UnaryOperatorExpression, Type: System.Decimal) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.Decimal A.Method()) (OperationKind.InvocationExpression, Type: System.Decimal) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.FloatingPlus) (OperationKind.UnaryOperatorExpression, Type: System.Single) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.Single A.Method()) (OperationKind.InvocationExpression, Type: System.Single) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.FloatingPlus) (OperationKind.UnaryOperatorExpression, Type: System.Double) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.Double A.Method()) (OperationKind.InvocationExpression, Type: System.Double) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.Boolean A.Method()) (OperationKind.InvocationExpression, Type: System.Boolean, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+Method()')
  Operand: IInvocationExpression ( System.Object A.Method()) (OperationKind.InvocationExpression, Type: System.Object, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.SByte A.Method()) (OperationKind.InvocationExpression, Type: System.SByte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Byte A.Method()) (OperationKind.InvocationExpression, Type: System.Byte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Int16 A.Method()) (OperationKind.InvocationExpression, Type: System.Int16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.UInt16 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-Method()')
  Operand: IInvocationExpression ( System.Int32 A.Method()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int64, IsInvalid) (Syntax: '-Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int64, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.UInt32 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt32, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '-Method()')
  Operand: IInvocationExpression ( System.Int64 A.Method()) (OperationKind.InvocationExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '-Method()')
  Operand: IInvocationExpression ( System.UInt64 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt64, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '-Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Char A.Method()) (OperationKind.InvocationExpression, Type: System.Char, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DecimalMinus) (OperationKind.UnaryOperatorExpression, Type: System.Decimal) (Syntax: '-Method()')
  Operand: IInvocationExpression ( System.Decimal A.Method()) (OperationKind.InvocationExpression, Type: System.Decimal) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.FloatingMinus) (OperationKind.UnaryOperatorExpression, Type: System.Single) (Syntax: '-Method()')
  Operand: IInvocationExpression ( System.Single A.Method()) (OperationKind.InvocationExpression, Type: System.Single) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.FloatingMinus) (OperationKind.UnaryOperatorExpression, Type: System.Double) (Syntax: '-Method()')
  Operand: IInvocationExpression ( System.Double A.Method()) (OperationKind.InvocationExpression, Type: System.Double) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '-Method()')
  Operand: IInvocationExpression ( System.Boolean A.Method()) (OperationKind.InvocationExpression, Type: System.Boolean, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '-Method()')
  Operand: IInvocationExpression ( System.Object A.Method()) (OperationKind.InvocationExpression, Type: System.Object, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.BooleanLogicalNot) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: '!i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.BooleanLogicalNot) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: '!Method()')
  Operand: IInvocationExpression ( System.Boolean A.Method()) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Byte, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt16, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.UInt32) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.UInt64) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt64) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~i')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Char, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Decimal, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Single, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Boolean, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Object, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.SByte A.Method()) (OperationKind.InvocationExpression, Type: System.SByte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Byte A.Method()) (OperationKind.InvocationExpression, Type: System.Byte, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Int16 A.Method()) (OperationKind.InvocationExpression, Type: System.Int16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.UInt16 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt16, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.Int32 A.Method()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.UInt32) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.UInt32 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.Int64 A.Method()) (OperationKind.InvocationExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.UInt64) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.UInt64 A.Method()) (OperationKind.InvocationExpression, Type: System.UInt64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.IntegerBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '~Method()')
  Operand: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Method()')
      Operand: IInvocationExpression ( System.Char A.Method()) (OperationKind.InvocationExpression, Type: System.Char, IsInvalid) (Syntax: 'Method()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
          Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.Decimal A.Method()) (OperationKind.InvocationExpression, Type: System.Decimal, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.Single A.Method()) (OperationKind.InvocationExpression, Type: System.Single, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.Double A.Method()) (OperationKind.InvocationExpression, Type: System.Double, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.Boolean A.Method()) (OperationKind.InvocationExpression, Type: System.Boolean, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '~Method()')
  Operand: IInvocationExpression ( System.Object A.Method()) (OperationKind.InvocationExpression, Type: System.Object, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DynamicPlus) (OperationKind.UnaryOperatorExpression, Type: dynamic) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DynamicMinus) (OperationKind.UnaryOperatorExpression, Type: dynamic) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DynamicBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: dynamic) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DynamicLogicalNot) (OperationKind.UnaryOperatorExpression, Type: dynamic) (Syntax: '!i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DynamicPlus) (OperationKind.UnaryOperatorExpression, Type: dynamic) (Syntax: '+Method()')
  Operand: IInvocationExpression ( dynamic A.Method()) (OperationKind.InvocationExpression, Type: dynamic) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DynamicMinus) (OperationKind.UnaryOperatorExpression, Type: dynamic) (Syntax: '-Method()')
  Operand: IInvocationExpression ( dynamic A.Method()) (OperationKind.InvocationExpression, Type: dynamic) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DynamicBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: dynamic) (Syntax: '~Method()')
  Operand: IInvocationExpression ( dynamic A.Method()) (OperationKind.InvocationExpression, Type: dynamic) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.DynamicLogicalNot) (OperationKind.UnaryOperatorExpression, Type: dynamic) (Syntax: '!Method()')
  Operand: IInvocationExpression ( dynamic A.Method()) (OperationKind.InvocationExpression, Type: dynamic) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: Enum, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: Enum, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: Enum) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: Enum) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Enum A.Method()) (OperationKind.InvocationExpression, Type: Enum, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Enum A.Method()) (OperationKind.InvocationExpression, Type: Enum, IsInvalid) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A, IsInvalid) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: Enum) (Syntax: '~Method()')
  Operand: IInvocationExpression ( Enum A.Method()) (OperationKind.InvocationExpression, Type: Enum) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodPlus) (OperatorMethod: CustomType CustomType.op_UnaryPlus(CustomType x)) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodMinus) (OperatorMethod: CustomType CustomType.op_UnaryNegation(CustomType x)) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodBitwiseNegation) (OperatorMethod: CustomType CustomType.op_OnesComplement(CustomType x)) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '~i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodLogicalNot) (OperatorMethod: CustomType CustomType.op_LogicalNot(CustomType x)) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '!i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodPlus) (OperatorMethod: CustomType CustomType.op_UnaryPlus(CustomType x)) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '+Method()')
  Operand: IInvocationExpression ( CustomType A.Method()) (OperationKind.InvocationExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodMinus) (OperatorMethod: CustomType CustomType.op_UnaryNegation(CustomType x)) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '-Method()')
  Operand: IInvocationExpression ( CustomType A.Method()) (OperationKind.InvocationExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodBitwiseNegation) (OperatorMethod: CustomType CustomType.op_OnesComplement(CustomType x)) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '~Method()')
  Operand: IInvocationExpression ( CustomType A.Method()) (OperationKind.InvocationExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodLogicalNot) (OperatorMethod: CustomType CustomType.op_LogicalNot(CustomType x)) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '!Method()')
  Operand: IInvocationExpression ( CustomType A.Method()) (OperationKind.InvocationExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (x && y) { }')
  Condition: IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodTrue) (OperatorMethod: System.Boolean S.op_True(S x)) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'x && y')
      Operand: IOperation:  (OperationKind.None) (Syntax: 'x && y')
          Children(2):
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: S) (Syntax: 'x')
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: S) (Syntax: 'y')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
  IfFalse: null
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

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
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (x || y) { }')
  Condition: IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodTrue) (OperatorMethod: System.Boolean S.op_True(S x)) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'x || y')
      Operand: IOperation:  (OperationKind.None) (Syntax: 'x || y')
          Children(2):
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: S) (Syntax: 'x')
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: S) (Syntax: 'y')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
  IfFalse: null
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodPlus) (OperatorMethod: BaseType BaseType.op_UnaryPlus(BaseType x)) (OperationKind.UnaryOperatorExpression, Type: BaseType) (Syntax: '+i')
  Operand: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: BaseType) (Syntax: 'i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: DerivedType) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: DerivedType, IsInvalid) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: DerivedType, IsInvalid) (Syntax: 'i')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: BaseType, IsInvalid) (Syntax: 'i')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IOperation:  (OperationKind.None) (Syntax: 'x && y')
  Children(2):
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: S) (Syntax: 'x')
      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: S) (Syntax: 'y')
";
            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: '++i')
  Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IIncrementExpression (UnaryOperandKind.IntegerPrefixDecrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: '--i')
  Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
IConversionExpression (ConversionKind.CSharp, Explicit) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: '(int?)1')
  Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<CastExpressionSyntax>(source, expectedOperationTree);
        }

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
IPointerIndirectionReferenceExpression (OperationKind.PointerIndirectionReferenceExpression, Type: System.Int32) (Syntax: '*p2')
  Pointer: ILocalReferenceExpression: p2 (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'p2')
";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
@"IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus-IsLifted) (OperationKind.UnaryOperatorExpression, Type: System.Int32?) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32?) (Syntax: 'x')";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
@"IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
@"IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodMinus-IsLifted) (OperatorMethod: C C.op_UnaryNegation(C c)) (OperationKind.UnaryOperatorExpression, Type: C?) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C?) (Syntax: 'x')";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

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
@"IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodMinus) (OperatorMethod: C C.op_UnaryNegation(C c)) (OperationKind.UnaryOperatorExpression, Type: C) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')";

            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }
    }
}
