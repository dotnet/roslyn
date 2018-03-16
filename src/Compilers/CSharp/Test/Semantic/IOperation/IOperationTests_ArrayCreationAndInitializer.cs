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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[]) (Syntax: 'new string[1]')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M[]) (Syntax: 'new M[1]')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      ILocalReferenceOperation: dimension (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'dimension')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M[]) (Syntax: 'new M[dimension]')
  Dimension Sizes(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'dimension')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'dimension')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M[]) (Syntax: 'new M[(int)dimension]')
  Dimension Sizes(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)dimension')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'dimension')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[]) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new string[ ... ing.Empty }')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: 'string.Empty')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[]) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: 'string.Empty')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[], IsInvalid) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[], IsInvalid) (Syntax: 'new string[ ... ing.Empty }')
  Dimension Sizes(1):
      IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'dimension')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ string.Empty }')
      Element Values(1):
          IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: 'string.Empty')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int[] x = { 1, 2 };')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int[] x = { 1, 2 }')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = { 1, 2 }')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= { 1, 2 }')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '{ 1, 2 }')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{ 1, 2 }')
                Initializer: 
                  IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1, 2 }')
                    Element Values(2):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    Initializer: 
      null
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M[]) (Syntax: 'new M[] { new M() }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new M[] { new M() }')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ new M() }')
      Element Values(1):
          IObjectCreationOperation (Constructor: M..ctor()) (OperationKind.ObjectCreation, Type: M) (Syntax: 'new M()')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M[]) (Syntax: 'new[] { new M() }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { new M() }')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ new M() }')
      Element Values(1):
          IObjectCreationOperation (Constructor: M..ctor()) (OperationKind.ObjectCreation, Type: M) (Syntax: 'new M()')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: ?[], IsInvalid) (Syntax: 'new[]/*</bind>*/')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'new[]/*</bind>*/')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: ?[], IsInvalid) (Syntax: 'new[2]/*</bind>*/')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'new[2]/*</bind>*/')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '')
      Element Values(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,31): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = /*<bind>*/new[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidArray, "2").WithLocation(6, 31),
                // file.cs(6,44): error CS1514: { expected
                //         var x = /*<bind>*/new[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(6, 44),
                // file.cs(6,44): error CS1513: } expected
                //         var x = /*<bind>*/new[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(6, 44),
                // file.cs(6,27): error CS0826: No best type found for implicitly-typed array
                //         var x = /*<bind>*/new[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[2]/*</bind>*/").WithLocation(6, 27)
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[]) (Syntax: 'new[] { ""he ... , a, null }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'new[] { ""he ... , a, null }')
  Initializer: 
    IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ ""hello"", a, null }')
      Element Values(3):
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
          ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.String) (Syntax: 'a')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Byte[,,]) (Syntax: 'new byte[1,2,3]')
  Dimension Sizes(3):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Byte[,,]) (Syntax: 'new byte[,, ...  5, 6 } } }')
  Dimension Sizes(3):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new byte[,, ...  5, 6 } } }')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new byte[,, ...  5, 6 } } }')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'new byte[,, ...  5, 6 } } }')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { { 1, 2, ...  5, 6 } } }')
      Element Values(2):
          IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { 1, 2, 3 } }')
            Element Values(1):
                IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1, 2, 3 }')
                  Element Values(3):
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 1, IsImplicit) (Syntax: '1')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 2, IsImplicit) (Syntax: '2')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 3, IsImplicit) (Syntax: '3')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { 4, 5, 6 } }')
            Element Values(1):
                IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 4, 5, 6 }')
                  Element Values(3):
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 4, IsImplicit) (Syntax: '4')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 5, IsImplicit) (Syntax: '5')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 6, IsImplicit) (Syntax: '6')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[][]) (Syntax: 'new int[][] ... ew int[5] }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new int[][] ... ew int[5] }')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ new[] { 1 ... ew int[5] }')
      Element Values(2):
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { 1, 2, 3 }')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'new[] { 1, 2, 3 }')
            Initializer: 
              IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1, 2, 3 }')
                Element Values(3):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new int[5]')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[][,]) (Syntax: 'new int[1][,]')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[][,,]) (Syntax: 'new[] { new ... , 4 } } } }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[] { new ... , 4 } } } }')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ new[, ,]  ... , 4 } } } }')
      Element Values(2):
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,,]) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
            Dimension Sizes(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[, ,] {  ...  1, 2 } } }')
            Initializer: 
              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { { 1, 2 } } }')
                Element Values(1):
                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { 1, 2 } }')
                      Element Values(1):
                          IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1, 2 }')
                            Element Values(2):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,,]) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
            Dimension Sizes(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'new[, ,] {  ...  3, 4 } } }')
            Initializer: 
              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { { 3, 4 } } }')
                Element Values(1):
                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { 3, 4 } }')
                      Element Values(1):
                          IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 3, 4 }')
                            Element Values(2):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")]
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[], IsInvalid) (Syntax: 'new string[]')
  Dimension Sizes(0)
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1586: Array creation must have array size or array initializer
                //         var a = /*<bind>*/new string[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]").WithLocation(6, 37)
            };

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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[], IsInvalid) (Syntax: 'new string[] { 1 }')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'new string[] { 1 }')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ 1 }')
      Element Values(1):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[], IsInvalid) (Syntax: 'new string[b]')
  Dimension Sizes(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'b')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[]) (Syntax: 'new string[M()]')
  Dimension Sizes(1):
      IInvocationOperation ( System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
        Instance Receiver: 
          IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[]) (Syntax: 'new string[(int)M()]')
  Dimension Sizes(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)M()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationOperation ( System.Object C.M()) (OperationKind.Invocation, Type: System.Object) (Syntax: 'M()')
            Instance Receiver: 
              IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
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

    public static object M() => null;
}
";
            string expectedOperationTree = @"
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[], IsInvalid) (Syntax: 'new string[M()]')
  Dimension Sizes(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationOperation (System.Object C.M()) (OperationKind.Invocation, Type: System.Object, IsInvalid) (Syntax: 'M()')
            Instance Receiver: 
              null
            Arguments(0)
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/new string[M()]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new string[M()]").WithArguments("object", "int").WithLocation(6, 27)
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[], IsInvalid) (Syntax: 'new string[(int)M()]')
  Dimension Sizes(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid) (Syntax: '(int)M()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationOperation ( C C.M()) (OperationKind.Invocation, Type: C, IsInvalid) (Syntax: 'M()')
            Instance Receiver: 
              IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
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

        [Fact, WorkItem(7299, "https://github.com/dotnet/roslyn/issues/7299")]
        public void SimpleArrayCreation_ConstantConversion()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/new string[0.0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String[], IsInvalid) (Syntax: 'new string[0.0]')
  Dimension Sizes(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0.0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsInvalid) (Syntax: '0.0')
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (6,27): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/new string[0.0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new string[0.0]").WithArguments("double", "int").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
