// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void SimpleArrayCreation_PrimitiveType()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new string[1]')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void SimpleArrayCreation_UserDefinedType()
        {
            string source = @"
class M { }

class C
{
    public void F()
    {
        var a = /*<bind>*/new M[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[1]')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void SimpleArrayCreation_ConstantDimension()
        {
            string source = @"
class M { }

class C
{
    public void F()
    {
        const int dimension = 1;
        var a = /*<bind>*/new M[dimension]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      ILocalReferenceExpression: dimension (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
  Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void SimpleArrayCreation_NonConstantDimension()
        {
            string source = @"
class M { }

class C
{
    public void F(int dimension)
    {
        var a = /*<bind>*/new M[dimension]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'dimension')
  Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void SimpleArrayCreation_DimensionWithImplicitConversion()
        {
            string source = @"
class M { }

class C
{
    public void F(char dimension)
    {
        var a = /*<bind>*/new M[dimension]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'dimension')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Char) (Syntax: 'dimension')
  Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void SimpleArrayCreation_DimensionWithExplicitConversion()
        {
            string source = @"
class M { }

class C
{
    public void F(object dimension)
    {
        var a = /*<bind>*/new M[(int)dimension]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[(int)dimension]')
  Dimension Sizes(1):
      IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)dimension')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'dimension')
  Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializer_PrimitiveType()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[] { string.Empty }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new string[ ... ing.Empty }')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'string.Empty')
            Instance Receiver: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializer_UserDefinedType()
        {
            string source = @"
class M { }

class C
{
    public void F()
    {
        var a = /*<bind>*/new M[] { new M() }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[] { new M() }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new M[] { new M() }')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ new M() }')
      Element Values(1):
          IObjectCreationExpression (Constructor: M..ctor()) (OperationKind.ObjectCreationExpression, Type: M) (Syntax: 'new M()')
            Arguments(0)
            Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializer_ImplicitlyTyped()
        {
            string source = @"
class M { }

class C
{
    public void F()
    {
        var a = /*<bind>*/new[] { new M() }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new[] { new M() }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new[] { new M() }')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ new M() }')
      Element Values(1):
          IObjectCreationExpression (Constructor: M..ctor()) (OperationKind.ObjectCreationExpression, Type: M) (Syntax: 'new M()')
            Arguments(0)
            Initializer: null
";
            VerifyOperationTreeForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializer_MultipleInitializersWithConversions()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = """";
        var b = /*<bind>*/new[] { ""hello"", a, null }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new[] { ""he ... , a, null }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: 'new[] { ""he ... , a, null }')
  Initializer: IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{ ""hello"", a, null }')
      Element Values(3):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
          ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'a')
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            VerifyOperationTreeForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void MultiDimensionalArrayCreation()
        {
            string source = @"
class C
{
    public void F()
    {
        byte[,,] b = /*<bind>*/new byte[1,2,3]/*</bind>*/;

    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.Byte) (OperationKind.ArrayCreationExpression, Type: System.Byte[,,]) (Syntax: 'new byte[1,2,3]')
  Dimension Sizes(3):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void MultiDimensionalArrayCreation_WithInitializer()
        {
            string source = @"
class C
{
    public void F()
    {
        byte[,,] b = /*<bind>*/new byte[,,] { { { 1, 2, 3 } }, { { 4, 5, 6 } } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.Byte) (OperationKind.ArrayCreationExpression, Type: System.Byte[,,]) (Syntax: 'new byte[,, ...  5, 6 } } }')
  Dimension Sizes(3):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new byte[,, ...  5, 6 } } }')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new byte[,, ...  5, 6 } } }')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: 'new byte[,, ...  5, 6 } } }')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { { 1, 2, ...  5, 6 } } }')
      Element Values(2):
          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { 1, 2, 3 } }')
            Element Values(1):
                IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2, 3 }')
                  Element Values(3):
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 1) (Syntax: '1')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 2) (Syntax: '2')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 3) (Syntax: '3')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { 4, 5, 6 } }')
            Element Values(1):
                IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 4, 5, 6 }')
                  Element Values(3):
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 4) (Syntax: '4')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 5) (Syntax: '5')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 6) (Syntax: '6')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6) (Syntax: '6')
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationOfSingleDimensionalArrays()
        {
            string source = @"
class C
{
    public void F()
    {
        int[][] a = /*<bind>*/new int[][] { new[] { 1, 2, 3 }, new int[5] }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.Int32[]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][]) (Syntax: 'new int[][] ... ew int[5] }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new int[][] ... ew int[5] }')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ new[] { 1 ... ew int[5] }')
      Element Values(2):
          IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new[] { 1, 2, 3 }')
            Dimension Sizes(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: 'new[] { 1, 2, 3 }')
            Initializer: IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2, 3 }')
                Element Values(3):
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new int[5]')
            Dimension Sizes(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
            Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationOfMultiDimensionalArrays()
        {
            string source = @"
class C
{
    public void F()
    {
        int[][,] a = /*<bind>*/new int[1][,]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.Int32[,]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][,]) (Syntax: 'new int[1][,]')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: null
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationOfImplicitlyTypedMultiDimensionalArrays_WithInitializer()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new[] { new[, ,] { { { 1, 2 } } }, new[, ,] { { { 3, 4 } } } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.Int32[,,]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][,,]) (Syntax: 'new[] { new ... , 4 } } } }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new[] { new ... , 4 } } } }')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ new[, ,]  ... , 4 } } } }')
      Element Values(2):
          IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[,,]) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
            Dimension Sizes(3):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
            Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { { 1, 2 } } }')
                Element Values(1):
                    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { 1, 2 } }')
                      Element Values(1):
                          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2 }')
                            Element Values(2):
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[,,]) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
            Dimension Sizes(3):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
            Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { { 3, 4 } } }')
                Element Values(1):
                    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { 3, 4 } }')
                      Element Values(1):
                          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 3, 4 }')
                            Element Values(2):
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
";
            VerifyOperationTreeForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18193"), WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationErrorCase_MissingDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Dimension sizes: 0, Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String[], IsInvalid)
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationErrorCase_InvalidInitializer()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[] { 1 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String[], IsInvalid) (Syntax: 'new string[] { 1 }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'new string[] { 1 }')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{ 1 }')
      Element Values(1):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }
    }
}
