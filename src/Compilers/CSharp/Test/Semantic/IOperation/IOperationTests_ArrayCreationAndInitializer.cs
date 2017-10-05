// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new string[1]')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[1]')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      ILocalReferenceExpression: dimension (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'dimension')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'dimension')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Char) (Syntax: 'dimension')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[(int)dimension]')
  Dimension Sizes(1):
      IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)dimension')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'dimension')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new string[ ... ing.Empty }')
  Initializer: 
    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'string.Empty')
            Instance Receiver: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializer_PrimitiveTypeWithExplicitDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[1] { string.Empty }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: 
    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'string.Empty')
            Instance Receiver: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializerErrorCase_PrimitiveTypeWithIncorrectExplicitDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[2] { string.Empty }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[], IsInvalid) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  Initializer: 
    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
            Instance Receiver: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0847: An array initializer of length '2' is expected
                //         var a = /*<bind>*/new string[2] { string.Empty }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "{ string.Empty }").WithArguments("2").WithLocation(6, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializerErrorCase_PrimitiveTypeWithNonConstantExplicitDimension()
        {
            string source = @"
class C
{
    public void F(int dimension)
    {
        var a = /*<bind>*/new string[dimension] { string.Empty }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[], IsInvalid) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'dimension')
  Initializer: 
    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'string.Empty')
            Instance Receiver: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0150: A constant value is expected
                //         var a = /*<bind>*/new string[dimension] { string.Empty }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "dimension").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializer_NoExplicitArrayCreationExpression()
        {
            string source = @"
class C
{
    public void F(int dimension)
    {
        /*<bind>*/int[] x = { 1, 2 };/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int[] x = { 1, 2 };')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'x = { 1, 2 }')
    Variables: Local_1: System.Int32[] x
    Initializer: 
      ILocalInitializer (OperationKind.LocalInitializer) (Syntax: '= { 1, 2 }')
        IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: '{ 1, 2 }')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{ 1, 2 }')
          Initializer: 
            IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2 }')
              Element Values(2):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new M[] { new M() }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new M[] { new M() }')
  Initializer: 
    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ new M() }')
      Element Values(1):
          IObjectCreationExpression (Constructor: M..ctor()) (OperationKind.ObjectCreationExpression, Type: M) (Syntax: 'new M()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M[]) (Syntax: 'new[] { new M() }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { new M() }')
  Initializer: 
    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ new M() }')
      Element Values(1):
          IObjectCreationExpression (Constructor: M..ctor()) (OperationKind.ObjectCreationExpression, Type: M) (Syntax: 'new M()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializerErrorCase_ImplicitlyTypedWithoutInitializerAndDimension()
        {
            string source = @"
class C
{
    public void F(int dimension)
    {
        var x = /*<bind>*/new[]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: ?[], IsInvalid) (Syntax: 'new[]/*</bind>*/')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'new[]/*</bind>*/')
  Initializer: 
    IArrayInitializer (0 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '')
      Element Values(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1514: { expected
                //         var x = /*<bind>*/new[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(6, 43),
                // CS1513: } expected
                //         var x = /*<bind>*/new[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(6, 43),
                // CS0826: No best type found for implicitly-typed array
                //         var x = /*<bind>*/new[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[]/*</bind>*/").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationWithInitializerErrorCase_ImplicitlyTypedWithoutInitializer()
        {
            string source = @"
class C
{
    public void F(int dimension)
    {
        var x = /*<bind>*/new[2]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[], IsInvalid) (Syntax: 'new[2]/*</bind>*/')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'new[2]/*</bind>*/')
  Initializer: 
    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '2]/*</bind>*/')
      Element Values(1):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1003: Syntax error, ']' expected
                //         var x = /*<bind>*/new[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, "2").WithArguments("]", "").WithLocation(6, 31),
                // CS1514: { expected
                //         var x = /*<bind>*/new[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "2").WithLocation(6, 31),
                // CS1003: Syntax error, ',' expected
                //         var x = /*<bind>*/new[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, "]").WithArguments(",", "]").WithLocation(6, 32),
                // CS1513: } expected
                //         var x = /*<bind>*/new[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(6, 44)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new[] { ""he ... , a, null }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'new[] { ""he ... , a, null }')
  Initializer: 
    IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{ ""hello"", a, null }')
      Element Values(3):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
          ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'a')
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Byte[,,]) (Syntax: 'new byte[1,2,3]')
  Dimension Sizes(3):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Byte[,,]) (Syntax: 'new byte[,, ...  5, 6 } } }')
  Dimension Sizes(3):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new byte[,, ...  5, 6 } } }')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new byte[,, ...  5, 6 } } }')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'new byte[,, ...  5, 6 } } }')
  Initializer: 
    IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { { 1, 2, ...  5, 6 } } }')
      Element Values(2):
          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { 1, 2, 3 } }')
            Element Values(1):
                IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2, 3 }')
                  Element Values(3):
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 1, IsImplicit) (Syntax: '1')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 2, IsImplicit) (Syntax: '2')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 3, IsImplicit) (Syntax: '3')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { 4, 5, 6 } }')
            Element Values(1):
                IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 4, 5, 6 }')
                  Element Values(3):
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 4, IsImplicit) (Syntax: '4')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 5, IsImplicit) (Syntax: '5')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 6, IsImplicit) (Syntax: '6')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6) (Syntax: '6')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[][]) (Syntax: 'new int[][] ... ew int[5] }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new int[][] ... ew int[5] }')
  Initializer: 
    IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ new[] { 1 ... ew int[5] }')
      Element Values(2):
          IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new[] { 1, 2, 3 }')
            Dimension Sizes(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'new[] { 1, 2, 3 }')
            Initializer: 
              IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2, 3 }')
                Element Values(3):
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new int[5]')
            Dimension Sizes(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[][,]) (Syntax: 'new int[1][,]')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[][,,]) (Syntax: 'new[] { new ... , 4 } } } }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[] { new ... , 4 } } } }')
  Initializer: 
    IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ new[, ,]  ... , 4 } } } }')
      Element Values(2):
          IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[,,]) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
            Dimension Sizes(3):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
            Initializer: 
              IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { { 1, 2 } } }')
                Element Values(1):
                    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { 1, 2 } }')
                      Element Values(1):
                          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2 }')
                            Element Values(2):
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32[,,]) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
            Dimension Sizes(3):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
            Initializer: 
              IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { { 3, 4 } } }')
                Element Values(1):
                    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ { 3, 4 } }')
                      Element Values(1):
                          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 3, 4 }')
                            Element Values(2):
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[], IsInvalid) (Syntax: 'new string[] { 1 }')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'new string[] { 1 }')
  Initializer: 
    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{ 1 }')
      Element Values(1):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'string'
                //         var a = /*<bind>*/new string[] { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationErrorCase_MissingExplicitCast()
        {
            string source = @"
class C
{
    public void F(object b)
    {
        var a = /*<bind>*/new string[b]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[], IsInvalid) (Syntax: 'new string[b]')
  Dimension Sizes(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Object, IsInvalid) (Syntax: 'b')
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/new string[b]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new string[b]").WithArguments("object", "int").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreation_InvocationExpressionAsDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[M()]/*</bind>*/;
    }

    public int M() => 1;
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new string[M()]')
  Dimension Sizes(1):
      IInvocationExpression ( System.Int32 C.M()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'M()')
        Instance Receiver: 
          IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'M')
        Arguments(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreation_InvocationExpressionWithConversionAsDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[(int)M()]/*</bind>*/;
    }

    public object M() => null;
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[]) (Syntax: 'new string[(int)M()]')
  Dimension Sizes(1):
      IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)M()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationExpression ( System.Object C.M()) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'M()')
            Instance Receiver: 
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'M')
            Arguments(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationErrorCase_InvocationExpressionAsDimension()
        {
            string source = @"
class C
{
    public static void F()
    {
        var a = /*<bind>*/new string[M()]/*</bind>*/;
    }

    public object M() => null;
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[], IsInvalid) (Syntax: 'new string[M()]')
  Dimension Sizes(1):
      IInvocationExpression ( System.Object C.M()) (OperationKind.InvocationExpression, Type: System.Object, IsInvalid) (Syntax: 'M()')
        Instance Receiver: 
          IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
        Arguments(0)
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'C.M()'
                //         var a = /*<bind>*/new string[M()]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "M").WithArguments("C.M()").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
        public void ArrayCreationErrorCase_InvocationExpressionWithConversionAsDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[(int)M()]/*</bind>*/;
    }

    public C M() => new C();
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String[], IsInvalid) (Syntax: 'new string[(int)M()]')
  Dimension Sizes(1):
      IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '(int)M()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationExpression ( C C.M()) (OperationKind.InvocationExpression, Type: C, IsInvalid) (Syntax: 'M()')
            Instance Receiver: 
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
            Arguments(0)
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'C' to 'int'
                //         var a = /*<bind>*/new string[(int)M()]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int)M()").WithArguments("C", "int").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
