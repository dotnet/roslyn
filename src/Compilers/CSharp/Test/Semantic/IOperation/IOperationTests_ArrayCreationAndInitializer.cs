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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String[])
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[])
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[])
  ILocalReferenceExpression: dimension (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[])
  IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Int32)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[])
  IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32)
    IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Char)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[])
  IConversionExpression (ConversionKind.Cast, Explicit) (OperationKind.ConversionExpression, Type: System.Int32)
    IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Object)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String[])
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[])
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IObjectCreationExpression (Constructor: M..ctor()) (OperationKind.ObjectCreationExpression, Type: M)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M[])
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IObjectCreationExpression (Constructor: M..ctor()) (OperationKind.ObjectCreationExpression, Type: M)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String[])
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
  IArrayInitializer (OperationKind.ArrayInitializer)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: hello)
    ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.String)
    IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null)
      ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)
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
IArrayCreationExpression (Dimension sizes: 3, Element Type: System.Byte) (OperationKind.ArrayCreationExpression, Type: System.Byte[,,])
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
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
IArrayCreationExpression (Dimension sizes: 3, Element Type: System.Byte) (OperationKind.ArrayCreationExpression, Type: System.Byte[,,])
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IArrayInitializer (OperationKind.ArrayInitializer)
      IArrayInitializer (OperationKind.ArrayInitializer)
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 1)
          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 2)
          ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 3)
          ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
    IArrayInitializer (OperationKind.ArrayInitializer)
      IArrayInitializer (OperationKind.ArrayInitializer)
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 4)
          ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4)
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 5)
          ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 6)
          ILiteralExpression (Text: 6) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32[]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][])
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[])
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
      IArrayInitializer (OperationKind.ArrayInitializer)
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
        ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
    IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[])
      ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32[,]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][,])
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32[,,]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][,,])
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IArrayCreationExpression (Dimension sizes: 3, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[,,])
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      IArrayInitializer (OperationKind.ArrayInitializer)
        IArrayInitializer (OperationKind.ArrayInitializer)
          IArrayInitializer (OperationKind.ArrayInitializer)
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IArrayCreationExpression (Dimension sizes: 3, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[,,])
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      IArrayInitializer (OperationKind.ArrayInitializer)
        IArrayInitializer (OperationKind.ArrayInitializer)
          IArrayInitializer (OperationKind.ArrayInitializer)
            ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
            ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4)
";
            VerifyOperationTreeForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }
    }
}