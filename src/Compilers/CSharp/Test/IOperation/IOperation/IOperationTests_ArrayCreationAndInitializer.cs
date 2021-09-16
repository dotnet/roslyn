// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_ArrayCreationAndInitializer : SemanticModelTestBase
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
                // file.cs(6,38): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/new string[b]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b").WithArguments("object", "int").WithLocation(6, 38)
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
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
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
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
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
                // file.cs(6,38): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/new string[M()]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "M()").WithArguments("object", "int").WithLocation(6, 38)
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
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
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
                // file.cs(6,38): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/new string[0.0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "0.0").WithArguments("double", "int").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_NoControlFlow()
        {
            string source = @"
class C
{
    const int c1 = 2, c2 = 1, c3 = 1;
    void M(int[] a1, int[] a2, int[,] a3, int[,] a4, int[][] a5, int[][] a6,
           int d1, int d2, int d3, int d4, int v1, int v2, int v3, int v4)
    /*<bind>*/
    {
        a1 = new int[d1];                               // Single dimension, no initializer
        a2 = new int[] { v1 };                          // Single dimension, initializer
        a3 = new int[d2, d3];                           // Multi-dimension, no initializer
        a4 = new int[c1, c2] { { v2 }, { v3 } };        // Multi-dimension, initializer
        a5 = new int[d4][];                             // Jagged, no initializer
        a6 = new int[c3][] { new[] { v4 } };            // Jagged, initializer
        int[] f = { 1, 3, 4 };                          // Array creation with only initializer.
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32[] f]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (7)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new int[d1];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'a1 = new int[d1]')
                  Left: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new int[d1]')
                      Dimension Sizes(1):
                          IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd1')
                      Initializer: 
                        null

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a2 = new int[] { v1 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'a2 = new int[] { v1 }')
                  Left: 
                    IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a2')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new int[] { v1 }')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new int[] { v1 }')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 }')
                          Element Values(1):
                              IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v1')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a3 = new int[d2, d3];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,]) (Syntax: 'a3 = new int[d2, d3]')
                  Left: 
                    IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a3')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,]) (Syntax: 'new int[d2, d3]')
                      Dimension Sizes(2):
                          IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')
                          IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd3')
                      Initializer: 
                        null

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a4 = new in ... , { v3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,]) (Syntax: 'a4 = new in ... }, { v3 } }')
                  Left: 
                    IParameterReferenceOperation: a4 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a4')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,]) (Syntax: 'new int[c1, ... }, { v3 } }')
                      Dimension Sizes(2):
                          IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 2) (Syntax: 'c1')
                            Instance Receiver: 
                              null
                          IFieldReferenceOperation: System.Int32 C.c2 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c2')
                            Instance Receiver: 
                              null
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { v2 }, { v3 } }')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v2 }')
                                Element Values(1):
                                    IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v3 }')
                                Element Values(1):
                                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a5 = new int[d4][];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[][]) (Syntax: 'a5 = new int[d4][]')
                  Left: 
                    IParameterReferenceOperation: a5 (OperationKind.ParameterReference, Type: System.Int32[][]) (Syntax: 'a5')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[][]) (Syntax: 'new int[d4][]')
                      Dimension Sizes(1):
                          IParameterReferenceOperation: d4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd4')
                      Initializer: 
                        null

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a6 = new in ... ] { v4 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[][]) (Syntax: 'a6 = new in ... [] { v4 } }')
                  Left: 
                    IParameterReferenceOperation: a6 (OperationKind.ParameterReference, Type: System.Int32[][]) (Syntax: 'a6')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[][]) (Syntax: 'new int[c3] ... [] { v4 } }')
                      Dimension Sizes(1):
                          IFieldReferenceOperation: System.Int32 C.c3 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c3')
                            Instance Receiver: 
                              null
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ new[] { v4 } }')
                          Element Values(1):
                              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new[] { v4 }')
                                Dimension Sizes(1):
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new[] { v4 }')
                                Initializer: 
                                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v4 }')
                                    Element Values(1):
                                        IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v4')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[], IsImplicit) (Syntax: 'f = { 1, 3, 4 }')
              Left: 
                ILocalReferenceOperation: f (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32[], IsImplicit) (Syntax: 'f = { 1, 3, 4 }')
              Right: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '{ 1, 3, 4 }')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '{ 1, 3, 4 }')
                  Initializer: 
                    IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1, 3, 4 }')
                      Element Values(3):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInFirstDimension_NoInitializer()
        {
            string source = @"
class C
{
    void M(int[,] a1, int? d1, int d2, int c)
    /*<bind>*/
    {
        a1 = new int[d1 ?? d2, c];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new in ...  ?? d2, c];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,]) (Syntax: 'a1 = new in ... 1 ?? d2, c]')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,]) (Syntax: 'new int[d1 ?? d2, c]')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'd1 ?? d2')
                          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                      Initializer: 
                        null

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInFirstDimension_WithInitializer()
        {
            string source = @"
class C
{
    const int c = 1;
    void M(int[,] a1, int? d1, int d2, int v1)
    /*<bind>*/
    {
        a1 = new int[d1 ?? d2, c] { { v1 } };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = new in ... { { v1 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,], IsInvalid) (Syntax: 'a1 = new in ...  { { v1 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,], IsInvalid) (Syntax: 'new int[d1  ...  { { v1 } }')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1 ?? d2')
                          IFieldReferenceOperation: System.Int32 C.c (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c')
                            Instance Receiver: 
                              null
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { v1 } }')
                          Element Values(1):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 }')
                                Element Values(1):
                                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v1')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,22): error CS0150: A constant value is expected
                //         a1 = new int[d1 ?? d2, c] { { v1 } };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "d1 ?? d2").WithLocation(8, 22)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInSecondDimension_NoInitializer()
        {
            string source = @"
class C
{
    void M(int[,] a1, int? d1, int d2, int c)
    /*<bind>*/
    {
        a1 = new int[c, d1 ?? d2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new in ...  d1 ?? d2];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,]) (Syntax: 'a1 = new in ... , d1 ?? d2]')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,]) (Syntax: 'new int[c, d1 ?? d2]')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'd1 ?? d2')
                      Initializer: 
                        null

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInSecondDimension_WithInitializer()
        {
            string source = @"
class C
{
    const int c = 1;
    void M(int[,] a1, int? d1, int d2, int v1)
    /*<bind>*/
    {
        a1 = new int[c, d1 ?? d2] { { v1 } };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = new in ... { { v1 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,], IsInvalid) (Syntax: 'a1 = new in ...  { { v1 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,], IsInvalid) (Syntax: 'new int[c,  ...  { { v1 } }')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1 ?? d2')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { v1 } }')
                          Element Values(1):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 }')
                                Element Values(1):
                                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v1')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,25): error CS0150: A constant value is expected
                //         a1 = new int[c, d1 ?? d2] { { v1 } };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "d1 ?? d2").WithLocation(8, 25)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInMultipleDimensions()
        {
            string source = @"
class C
{
    void M(int[,] a1, int? d1, int d2, int? d3, int d4, int v1)
    /*<bind>*/
    {
        a1 = new int[d1 ?? d2, d3 ?? d4] { { v1 } };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'd2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'd3')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'd3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd3')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd3')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd4')
              Value: 
                IParameterReferenceOperation: d4 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'd4')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = new in ... { { v1 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,], IsInvalid) (Syntax: 'a1 = new in ...  { { v1 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,], IsInvalid) (Syntax: 'new int[d1  ...  { { v1 } }')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1 ?? d2')
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd3 ?? d4')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { v1 } }')
                          Element Values(1):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 }')
                                Element Values(1):
                                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v1')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {                
                // file.cs(7,22): error CS0150: A constant value is expected
                //         a1 = new int[d1 ?? d2, d3 ?? d4] { { v1 } };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "d1 ?? d2").WithLocation(7, 22),
                // file.cs(7,32): error CS0150: A constant value is expected
                //         a1 = new int[d1 ?? d2, d3 ?? d4] { { v1 } };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "d3 ?? d4").WithLocation(7, 32)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInFirstInitializerValue_SingleDimArray()
        {
            string source = @"
class C
{
    const int c1 = 2;
    void M(int[] a1, int? v1, int v2, int v3)
    /*<bind>*/
    {
        a1 = new int[c1] { v1 ?? v2, v3 };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 2) (Syntax: 'c1')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new in ... ? v2, v3 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'a1 = new in ... ?? v2, v3 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new int[c1] ... ?? v2, v3 }')
                      Dimension Sizes(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 ?? v2, v3 }')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')
                              IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInFirstInitializerValue_MultiDimArray()
        {
            string source = @"
class C
{
    const int c1 = 2, c2 = 1;
    void M(int[,] a1, int? v1, int v2, int v3)
    /*<bind>*/
    {
        a1 = new int[c1, c2] { { v1 ?? v2 }, { v3 } };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 2) (Syntax: 'c1')
                  Instance Receiver: 
                    null

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c2 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c2')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new in ... , { v3 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,]) (Syntax: 'a1 = new in ... }, { v3 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,]) (Syntax: 'new int[c1, ... }, { v3 } }')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { v1 ?? v2 }, { v3 } }')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 ?? v2 }')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v3 }')
                                Element Values(1):
                                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInSecondInitializerValue_SingleDimArray()
        {
            string source = @"
class C
{
    const int c1 = 2;
    void M(int[] a1, int? v1, int v2, int v3)
    /*<bind>*/
    {
        a1 = new int[c1] { v3, v1 ?? v2 };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 2) (Syntax: 'c1')
                  Instance Receiver: 
                    null

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
              Value: 
                IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new in ... v1 ?? v2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'a1 = new in ...  v1 ?? v2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new int[c1] ...  v1 ?? v2 }')
                      Dimension Sizes(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v3, v1 ?? v2 }')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInSecondInitializerValue_MultiDimArray()
        {
            string source = @"
class C
{
    const int c1 = 2, c2 = 1;
    void M(int[,] a1, int? v1, int v2, int v3)
    /*<bind>*/
    {
        a1 = new int[c1, c2] { { v3 }, { v1 ?? v2 } };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [3] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 2) (Syntax: 'c1')
                  Instance Receiver: 
                    null

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c2 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c2')
                  Instance Receiver: 
                    null

            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
              Value: 
                IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [4]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new in ...  ?? v2 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,]) (Syntax: 'a1 = new in ... 1 ?? v2 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,]) (Syntax: 'new int[c1, ... 1 ?? v2 } }')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { v3 }, { v1 ?? v2 } }')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v3 }')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 ?? v2 }')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInSecondInitializerValue_MultiDimArray_02()
        {
            // Error case where one array initializer element value is a nested array initializer and another one is not.
            // Verifies that CFG builder handles the mixed kind element values.
            string source = @"
class C
{
    const int c1 = 2, c2 = 1;
    void M(int[,] a1, int? v1, int v2, int v3)
    /*<bind>*/
    {
        a1 = new int[c1, c2] { v3, { v1 ?? v2 } };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [3] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 2) (Syntax: 'c1')
                  Instance Receiver: 
                    null

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c2 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c2')
                  Instance Receiver: 
                    null

            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v3')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'v3')
                  Children(1):
                      IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'v3')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [4]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = new in ...  ?? v2 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,], IsInvalid) (Syntax: 'a1 = new in ... 1 ?? v2 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,], IsInvalid) (Syntax: 'new int[c1, ... 1 ?? v2 } }')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ v3, { v1 ?? v2 } }')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'v3')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 ?? v2 }')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,32): error CS0846: A nested array initializer is expected
                //         a1 = new int[c1, c2] { v3, { v1 ?? v2 } };
                Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "v3").WithLocation(8, 32)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInMultipleInitializerValues_SingleDimArray()
        {
            string source = @"
class C
{
    const int c1 = 2;
    void M(int[] a1, int? v1, int v2, int? v3, int v4)
    /*<bind>*/
    {
        a1 = new int[c1] { v1 ?? v2, v3 ?? v4 };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [3] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 2) (Syntax: 'c1')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [4]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v3')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v3')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v3')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v4')
              Value: 
                IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v4')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new in ... v3 ?? v4 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'a1 = new in ...  v3 ?? v4 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new int[c1] ...  v3 ?? v4 }')
                      Dimension Sizes(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 ?? v2, v3 ?? v4 }')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v3 ?? v4')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInMultipleInitializerValues_MultiDimArray()
        {
            string source = @"
class C
{
    const int c1 = 2, c2 = 1;
    void M(int[,] a1, int? v1, int v2, int? v3, int v4)
    /*<bind>*/
    {
        a1 = new int[c1, c2] { { v1 ?? v2 }, { v3 ?? v4 } };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [4] [6]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c1 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 2) (Syntax: 'c1')
                  Instance Receiver: 
                    null

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IFieldReferenceOperation: System.Int32 C.c2 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c2')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v3')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v3')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v3')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v4')
              Value: 
                IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v4')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = new in ...  ?? v4 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,]) (Syntax: 'a1 = new in ... 3 ?? v4 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,]) (Syntax: 'new int[c1, ... 3 ?? v4 } }')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { v1 ?? v ... 3 ?? v4 } }')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 ?? v2 }')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v3 ?? v4 }')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v3 ?? v4')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInDimensionAndMultipleInitializerValues_SingleDimArray()
        {
            string source = @"
class C
{
    void M(int[] a1, int? v1, int v2, int? v3, int v4, int? d1, int d2)
    /*<bind>*/
    {
        a1 = new int[d1 ?? d2] { v1 ?? v2, v3 ?? v4 };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2] [4] [6]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'd2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
                Entering: {R4}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B8]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [5]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v3')

            Jump if True (Regular) to Block[B10]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v3')
                Leaving: {R4}

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v3')
                      Arguments(0)

            Next (Regular) Block[B11]
                Leaving: {R4}
    }

    Block[B10] - Block
        Predecessors: [B8]
        Statements (1)
            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v4')
              Value: 
                IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v4')

        Next (Regular) Block[B11]
    Block[B11] - Block
        Predecessors: [B9] [B10]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = new in ... v3 ?? v4 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[], IsInvalid) (Syntax: 'a1 = new in ...  v3 ?? v4 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsInvalid) (Syntax: 'new int[d1  ...  v3 ?? v4 }')
                      Dimension Sizes(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1 ?? d2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 ?? v2, v3 ?? v4 }')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')
                              IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v3 ?? v4')

        Next (Regular) Block[B12]
            Leaving: {R1}
}

Block[B12] - Exit
    Predecessors: [B11]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,22): error CS0150: A constant value is expected
                //         a1 = new int[d1 ?? d2] { v1 ?? v2, v3 ?? v4 };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "d1 ?? d2").WithLocation(7, 22)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayCreationAndInitializer_ControlFlowInMultipleDimensionsAndInitializerValues_MultiDimArray()
        {
            string source = @"
class C
{
    void M(int[,] a1, int? v1, int v2, int? v3, int v4, int? d1, int d2, int? d3, int d4)
    /*<bind>*/
    {
        a1 = new int[d1 ?? d2, d3 ?? d4] { { v1 ?? v2 }, { v3 ?? v4 } };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2] [4] [6] [8]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'd2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'd3')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'd3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd3')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'd3')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
                Entering: {R4}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'd4')
              Value: 
                IParameterReferenceOperation: d4 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'd4')

        Next (Regular) Block[B8]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [5]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v1')

            Jump if True (Regular) to Block[B10]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                Leaving: {R4}

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B11]
                Leaving: {R4}
                Entering: {R5}
    }

    Block[B10] - Block
        Predecessors: [B8]
        Statements (1)
            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B11]
            Entering: {R5}

    .locals {R5}
    {
        CaptureIds: [7]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'v3')

            Jump if True (Regular) to Block[B13]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v3')
                Leaving: {R5}

            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'v3')
                      Arguments(0)

            Next (Regular) Block[B14]
                Leaving: {R5}
    }

    Block[B13] - Block
        Predecessors: [B11]
        Statements (1)
            IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v4')
              Value: 
                IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v4')

        Next (Regular) Block[B14]
    Block[B14] - Block
        Predecessors: [B12] [B13]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = new in ...  ?? v4 } };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[,], IsInvalid) (Syntax: 'a1 = new in ... 3 ?? v4 } }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[,], IsInvalid) (Syntax: 'new int[d1  ... 3 ?? v4 } }')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd1 ?? d2')
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd3 ?? d4')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ { v1 ?? v ... 3 ?? v4 } }')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v1 ?? v2 }')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v1 ?? v2')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ v3 ?? v4 }')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v3 ?? v4')

        Next (Regular) Block[B15]
            Leaving: {R1}
}

Block[B15] - Exit
    Predecessors: [B14]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {                
                // file.cs(7,22): error CS0150: A constant value is expected
                //         a1 = new int[d1 ?? d2, d3 ?? d4] { { v1 ?? v2 }, { v3 ?? v4 } };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "d1 ?? d2").WithLocation(7, 22),
                // file.cs(7,32): error CS0150: A constant value is expected
                //         a1 = new int[d1 ?? d2, d3 ?? d4] { { v1 ?? v2 }, { v3 ?? v4 } };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "d3 ?? d4").WithLocation(7, 32)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
