// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Test list drawn from Microsoft.CodeAnalysis.CSharp.ConversionKind
    public partial class IOperationTests : SemanticModelTestBase
    {
        #region Implicit Conversions

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_IdentityConversionDynamic()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o1 = new object();
        dynamic /*<bind>*/d1 = o1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: dynamic d1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd1 = o1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= o1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: 'o1')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        /// <summary>
        /// This test documents the fact that there is no IConversionExpression between two objects of the same type.
        /// </summary>
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_IdentityConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o1 = new object();
        object /*<bind>*/o2 = o1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Object o2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'o2 = o1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= o1')
      ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NumericConversion_Valid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        float f1 = 1.0f;
        double /*<bind>*/d1 = f1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Double d1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd1 = f1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= f1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'f1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: f1 (OperationKind.LocalReference, Type: System.Single) (Syntax: 'f1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NumericConversion_InvalidIllegalTypes()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        float f1 = 1.0f;
        int /*<bind>*/i1 = f1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = f1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= f1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'f1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: f1 (OperationKind.LocalReference, Type: System.Single, IsInvalid) (Syntax: 'f1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i1 = f1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "f1").WithArguments("float", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_NumericConversion_InvalidNoInitializer()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int /*<bind>*/i1 =/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 =/*</bind>*/')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=/*</bind>*/')
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         int /*<bind>*/i1 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(8, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: (operation, compilation, syntax) =>
                {
                    // This scenario, where the syntax has IsMissing set, is special cased. We remove the conversion, and leave
                    // just an IInvalidOperation with null type. First assert that our assumptions are true, then test the actual
                    // result
                    var initializerSyntax = ((VariableDeclaratorSyntax)syntax).Initializer.Value;
                    var typeInfo = compilation.GetSemanticModel(syntax.SyntaxTree).GetTypeInfo(initializerSyntax);
                    Assert.Equal(SyntaxKind.IdentifierName, initializerSyntax.Kind());
                    Assert.True(initializerSyntax.IsMissing);
                    Assert.Null(typeInfo.Type);
                    Assert.Null(typeInfo.ConvertedType);

                    var initializerOperation = ((IVariableDeclaratorOperation)operation).Initializer.Value;
                    Assert.Null(initializerOperation.Type);
                    Assert.Equal(OperationKind.Invalid, initializerOperation.Kind);
                });
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_ZeroToEnum()
        {
            string source = @"
class Program
{    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 = 0/*</bind>*/;
    }
}
enum Enum1
{
    Option1, Option2
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: Enum1 e1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e1 = 0')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Enum1, Constant: 0, IsImplicit) (Syntax: '0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'e1' is assigned but its value is never used
                //         Enum1 /*<bind>*/e1 = 0/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "e1").WithArguments("e1").WithLocation(5, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_IntToEnum_Invalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i1 = 1;
        Enum1 /*<bind>*/e1 = i1/*</bind>*/;
    }
}
enum Enum1
{
    Option1, Option2
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: Enum1 e1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e1 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Enum1, IsInvalid, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'Program.Enum1'. An explicit conversion exists (are you missing a cast?)
                //         Enum1 /*<bind>*/e1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int", "Enum1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_OneToEnum_Invalid()
        {
            string source = @"
class Program
{    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 = 1/*</bind>*/;
    }
}
enum Enum1
{
    Option1, Option2
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: Enum1 e1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e1 = 1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= 1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Enum1, Constant: 1, IsInvalid, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (5,30): error CS0266: Cannot implicitly convert type 'int' to 'Enum1'. An explicit conversion exists (are you missing a cast?)
                //         Enum1 /*<bind>*/e1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "Enum1").WithLocation(5, 30),
                // (5,25): warning CS0219: The variable 'e1' is assigned but its value is never used
                //         Enum1 /*<bind>*/e1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "e1").WithArguments("e1").WithLocation(5, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
        public void ConversionExpression_Implicit_EnumConversion_NoInitializer_Invalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 =/*</bind>*/;
    }
}
enum Enum1
{
    Option1, Option2
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarators) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Enum1 e1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Enum1, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         Enum1 /*<bind>*/e1 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ThrowExpressionConversion()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object /*<bind>*/o = new object() ?? throw new Exception()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Object o) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'o = new obj ... Exception()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new objec ... Exception()')
      ICoalesceOperation (OperationKind.Coalesce, Type: System.Object) (Syntax: 'new object( ... Exception()')
        Expression: 
          IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object) (Syntax: 'new object()')
            Arguments(0)
            Initializer: 
              null
        ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          (Identity)
        WhenNull: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'throw new Exception()')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw new Exception()')
                IObjectCreationOperation (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreation, Type: System.Exception) (Syntax: 'new Exception()')
                  Arguments(0)
                  Initializer: 
                    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier()
                {
                    SyntaxSelector = (syntax) =>
                    {
                        var initializer = (BinaryExpressionSyntax)((VariableDeclaratorSyntax)syntax).Initializer.Value;
                        return initializer.Right;
                    },
                    OperationSelector = (operation) =>
                    {
                        var initializer = ((IVariableDeclaratorOperation)operation).Initializer.Value;
                        return (IConversionOperation)((ICoalesceOperation)initializer).WhenNull;
                    }
                }.Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
        public void ConversionExpression_Implicit_ThrowExpressionConversion_InvalidSyntax()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object /*<bind>*/o = throw new Exception()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarators) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'throw new Exception()')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'throw new Exception()')
          Children(1): IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'throw new Exception()')
              Children(1): IObjectCreationExpression (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new Exception()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8115: A throw expression is not allowed in this context.
                //         object /*<bind>*/o = throw new Exception()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(8, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NullToClassConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        string /*<bind>*/s1 = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.String s1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's1 = null')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= null')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         string /*<bind>*/s1 = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NullToNullableValueConversion()
        {
            string source = @"
interface I1
{
}

struct S1
{
    void M1()
    {
        S1? /*<bind>*/s1 = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: S1? s1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's1 = null')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= null')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S1?, Constant: null, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         S1? /*<bind>*/s1 = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(10, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NullToNonNullableConversion_Invalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int /*<bind>*/i1 = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = null')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= null')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         int /*<bind>*/i1 = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_DefaultToValueConversion()
        {
            string source = @"
using System;

class S1
{
    void M1()
    {
        long /*<bind>*/i1 = default/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int64 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = default')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= default')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 0, IsImplicit) (Syntax: 'default')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int64, Constant: 0) (Syntax: 'default')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         long /*<bind>*/i1 = default(int)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(8, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Regular7_1,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_DefaultOfImplicitlyConvertableTypeToValueConversion()
        {
            string source = @"
using System;

class S1
{
    void M1()
    {
        long /*<bind>*/i1 = default(int)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int64 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = default(int)')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= default(int)')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 0, IsImplicit) (Syntax: 'default(int)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32, Constant: 0) (Syntax: 'default(int)')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         long /*<bind>*/i1 = default(int)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(8, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        /// <summary>
        /// This test documents the fact that <c>default(T)</c> is already T, and does not introduce a conversion
        /// </summary>
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_DefaultToClassNoConversion()
        {
            string source = @"
using System;

class S1
{
    void M1()
    {
        string /*<bind>*/i1 = default(string)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.String i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = default(string)')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= default(string)')
      IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null) (Syntax: 'default(string)')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         string /*<bind>*/i1 = default(string)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(8, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NullableFromConstantConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int? /*<bind>*/i1 = 1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32? i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = 1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         int? /*<bind>*/i1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NullableToNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int? i1 = 1;
        long? /*<bind>*/l1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int64? l1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'l1 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64?, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NullableFromNonNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i1 = 1;
        int? /*<bind>*/i2 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32? i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_NullableToNonNullableConversion_Invalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int? i1 = 1;
        int /*<bind>*/i2 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i2 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32?, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int?' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i2 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int?", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_InterpolatedStringToIFormattableExpression()
        {
            // This needs to be updated once https://github.com/dotnet/roslyn/issues/20046 is addressed.
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        IFormattable /*<bind>*/f1 = $""{1}""/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.IFormattable f1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'f1 = $""{1}""')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $""{1}""')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IFormattable, IsImplicit) (Syntax: '$""{1}""')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{1}""')
            Parts(1):
                IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
                  Expression: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Alignment: 
                    null
                  FormatString: 
                    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceToObjectConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        object /*<bind>*/o1 = new C1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Object o1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'o1 = new C1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceToDynamicConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        dynamic /*<bind>*/d1 = new C1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: dynamic d1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd1 = new C1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceClassToClassConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C1 /*<bind>*/c1 = new C2()/*</bind>*/;
    }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C2()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C2()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsImplicit) (Syntax: 'new C2()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'new C2()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceClassToClassConversion_Invalid()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C1 /*<bind>*/c1 = new C2()/*</bind>*/;
    }
}

class C2
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = new C2()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C2()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C2()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C2..ctor()) (OperationKind.ObjectCreation, Type: C2, IsInvalid) (Syntax: 'new C2()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2' to 'C1'
                //         C1 /*<bind>*/c1 = new C2()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C2()").WithArguments("C2", "C1").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceConversion_InvalidSyntax()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C1 /*<bind>*/c1 = new/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = new/*</bind>*/')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new/*</bind>*/')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new/*</bind>*/')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'new/*</bind>*/')
            Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1031: Type expected
                //         C1 /*<bind>*/c1 = new/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(8, 41),
                // CS1526: A new expression requires (), [], or {} after type
                //         C1 /*<bind>*/c1 = new/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadNewExpr, ";").WithLocation(8, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceClassToInterfaceConversion()
        {
            string source = @"
using System;

interface I1
{
}

class C1 : I1
{
    static void Main(string[] args)
    {
        I1 /*<bind>*/i1 = new C1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = new C1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsImplicit) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceClassToInterfaceConversion_Invalid()
        {
            string source = @"
using System;

interface I1
{
}

class C1
{
    static void Main(string[] args)
    {
        I1 /*<bind>*/i1 = new C1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = new C1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsInvalid, IsImplicit) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C1' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = new C1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new C1()").WithArguments("C1", "I1").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceInterfaceToClassConversion_Invalid()
        {
            string source = @"
using System;

interface I1
{
}

class C1
{
    static void Main(string[] args)
    {
        C1 /*<bind>*/i1 = new I1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = new I1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new I1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new I1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: I1, IsInvalid) (Syntax: 'new I1()')
            Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0144: Cannot create an instance of the abstract class or interface 'I1'
                //         C1 /*<bind>*/i1 = new I1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new I1()").WithArguments("I1").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceInterfaceToInterfaceConversion()
        {
            string source = @"
using System;

interface I1
{
}

interface I2 : I1
{
}

class C1 : I2
{
    static void Main(string[] args)
    {
        I2 i2 = new C1();
        I1 /*<bind>*/i1 = i2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = i2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsImplicit) (Syntax: 'i2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: I2) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceInterfaceToInterfaceConversion_Invalid()
        {
            string source = @"
using System;

interface I1
{
}

interface I2
{
}

class C1 : I2
{
    static void Main(string[] args)
    {
        I2 i2 = new C1();
        I1 /*<bind>*/i1 = i2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = i2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= i2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsInvalid, IsImplicit) (Syntax: 'i2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: I2, IsInvalid) (Syntax: 'i2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'I2' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = i2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i2").WithArguments("I2", "I1").WithLocation(17, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C2[] c2arr = new C2[10];
        C1[] /*<bind>*/c1arr = c2arr/*</bind>*/;
    }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1[] c1arr) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1arr = c2arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c2arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1[], IsImplicit) (Syntax: 'c2arr')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c2arr (OperationKind.LocalReference, Type: C2[]) (Syntax: 'c2arr')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion_InvalidDimensionMismatch()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C2[] c2arr = new C2[10];
        C1[][] /*<bind>*/c1arr = c2arr/*</bind>*/;
    }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1[][] c1arr) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1arr = c2arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c2arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1[][], IsInvalid, IsImplicit) (Syntax: 'c2arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c2arr (OperationKind.LocalReference, Type: C2[], IsInvalid) (Syntax: 'c2arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2[]' to 'C1[][]'
                //         C1[][] /*<bind>*/c1arr = c2arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2arr").WithArguments("C2[]", "C1[][]").WithLocation(9, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion_InvalidNoReferenceConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C2[] c2arr = new C2[10];
        C1[] /*<bind>*/c1arr = c2arr/*</bind>*/;
    }
}

class C2
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1[] c1arr) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1arr = c2arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c2arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1[], IsInvalid, IsImplicit) (Syntax: 'c2arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c2arr (OperationKind.LocalReference, Type: C2[], IsInvalid) (Syntax: 'c2arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2[]' to 'C1[]'
                //         C1[] /*<bind>*/c1arr = c2arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2arr").WithArguments("C2[]", "C1[]").WithLocation(9, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion_InvalidValueTypeToReferenceType()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        I1[] /*<bind>*/i1arr = new S1[10]/*</bind>*/;
    }
}

interface I1
{
}

struct S1 : I1
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1[] i1arr) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1arr = new S1[10]')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new S1[10]')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1[], IsInvalid, IsImplicit) (Syntax: 'new S1[10]')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: S1[], IsInvalid) (Syntax: 'new S1[10]')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1[]' to 'I1[]'
                //         I1[] /*<bind>*/i1arr = new S1[10]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S1[10]").WithArguments("S1[]", "I1[]").WithLocation(8, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToSystemArrayConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        Array /*<bind>*/a1 = new object[10]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Array a1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a1 = new object[10]')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new object[10]')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Array, IsImplicit) (Syntax: 'new object[10]')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[]) (Syntax: 'new object[10]')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToSystemArrayConversion_MultiDimensionalArray()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        Array /*<bind>*/a1 = new int[10][]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Array a1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a1 = new int[10][]')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new int[10][]')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Array, IsImplicit) (Syntax: 'new int[10][]')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[][]) (Syntax: 'new int[10][]')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToSystemArrayConversion_InvalidNotArrayType()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        Array /*<bind>*/a1 = new object()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Array a1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a1 = new object()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new object()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Array, IsInvalid, IsImplicit) (Syntax: 'new object()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'new object()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'System.Array'. An explicit conversion exists (are you missing a cast?)
                //         Array /*<bind>*/a1 = new object()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "System.Array").WithLocation(8, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToIListTConversion()
        {
            string source = @"
using System.Collections.Generic;

class C1
{
    static void Main(string[] args)
    {
        IList<int> /*<bind>*/a1 = new int[10]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IList<System.Int32> a1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a1 = new int[10]')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new int[10]')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: 'new int[10]')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new int[10]')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToIListTConversion_InvalidNonArrayType()
        {
            string source = @"
using System.Collections.Generic;

class C1
{
    static void Main(string[] args)
    {
        IList<int> /*<bind>*/a1 = new object()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IList<System.Int32> a1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'a1 = new object()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new object()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'new object()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'new object()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'System.Collections.Generic.IList<int>'. An explicit conversion exists (are you missing a cast?)
                //         IList<int> /*<bind>*/a1 = new object()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "System.Collections.Generic.IList<int>").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceDelegateTypeToSystemDelegateConversion()
        {
            string source = @"
using System;

class C1
{
    delegate void DType();
    void M1()
    {
        DType d1 = M2;
        Delegate /*<bind>*/d2 = d1/*</bind>*/;
    }

    void M2()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Delegate d2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd2 = d1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= d1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Delegate, IsImplicit) (Syntax: 'd1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: d1 (OperationKind.LocalReference, Type: C1.DType) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceDelegateTypeToSystemDelegateConversion_InvalidNonDelegateType()
        {
            string source = @"
using System;

class C1
{
    delegate void DType();
    void M1()
    {
        DType d1 = M2;
        Delegate /*<bind>*/d2 = d1()/*</bind>*/;
    }

    void M2()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Delegate d2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'd2 = d1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= d1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Delegate, IsInvalid, IsImplicit) (Syntax: 'd1()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationOperation (virtual void C1.DType.Invoke()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'd1()')
            Instance Receiver: 
              ILocalReferenceOperation: d1 (OperationKind.LocalReference, Type: C1.DType, IsInvalid) (Syntax: 'd1')
            Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'void' to 'System.Delegate'
                //         Delegate /*<bind>*/d2 = d1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d1()").WithArguments("void", "System.Delegate").WithLocation(10, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
        public void ConversionExpression_Implicit_ReferenceDelegateTypeToSystemDelegateConversion_InvalidSyntax()
        {
            string source = @"
using System;

class C1
{
    delegate void DType();
    void M1()
    {
        Delegate /*<bind>*/d2 =/*</bind>*/;
    }

    void M2()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarators) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
    Variables: Local_1: System.Delegate d2
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Delegate, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         Delegate /*<bind>*/d2 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(9, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTransitiveConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        C1 /*<bind>*/c1 = new C3()/*</bind>*/;
    }
}

class C2 : C1
{
}

class C3 : C2
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C3()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C3()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsImplicit) (Syntax: 'new C3()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C3..ctor()) (OperationKind.ObjectCreation, Type: C3) (Syntax: 'new C3()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceCovarianceTransitiveConversion()
        {
            string source = @"
interface I1<in T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C3> c2 = new C2<C3>();
        I1<C4> /*<bind>*/c1 = c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1<C4> c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = c2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1<C4>, IsImplicit) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C2<C3>) (Syntax: 'c2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceCovarianceTransitiveConversion_Invalid()
        {
            string source = @"
interface I1<in T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C4> c2 = new C2<C4>();
        I1<C3> /*<bind>*/c1 = c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1<C3> c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = c2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1<C3>, IsInvalid, IsImplicit) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C2<C4>, IsInvalid) (Syntax: 'c2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C2<C4>' to 'I1<C3>'. An explicit conversion exists (are you missing a cast?)
                //         I1<C3> /*<bind>*/c1 = c2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c2").WithArguments("C2<C4>", "I1<C3>").WithLocation(11, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceContravarianceTransitiveConversion()
        {
            string source = @"
interface I1<out T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C4> c2 = new C2<C4>();
        I1<C3> /*<bind>*/c1 = c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1<C3> c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = c2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1<C3>, IsImplicit) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C2<C4>) (Syntax: 'c2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceContravarianceTransitiveConversion_Invalid()
        {
            string source = @"
interface I1<out T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C3> c2 = new C2<C3>();
        I1<C4> /*<bind>*/c1 = c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1<C4> c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = c2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1<C4>, IsInvalid, IsImplicit) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C2<C3>, IsInvalid) (Syntax: 'c2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C2<C3>' to 'I1<C4>'. An explicit conversion exists (are you missing a cast?)
                //         I1<C4> /*<bind>*/c1 = c2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c2").WithArguments("C2<C3>", "I1<C4>").WithLocation(11, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceInvariantTransitiveConversion()
        {
            string source = @"
using System.Collections.Generic;

class C1
{
    static void M1()
    {
        IList<string> /*<bind>*/list = new List<string>()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IList<System.String> list) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'list = new  ... t<string>()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new List<string>()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<System.String>, IsImplicit) (Syntax: 'new List<string>()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.String>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.String>) (Syntax: 'new List<string>()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterClassConversion()
        {
            string source = @"
class C1
{
    static void M1<T>()
        where T : C2, new()
    {
        C1 /*<bind>*/c1 = new T()/*</bind>*/;
    }
}

class C2 : C1
{

}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new T()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new T()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsImplicit) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T()')
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterClassConversion_InvalidConversion()
        {
            string source = @"
class C1
{
    static void M1<T>()
        where T : class, new()
    {
        C1 /*<bind>*/c1 = new T()/*</bind>*/;
    }
}

class C2 : C1
{

}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = new T()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new T()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T, IsInvalid) (Syntax: 'new T()')
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'T' to 'C1'
                //         C1 /*<bind>*/c1 = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new T()").WithArguments("T", "C1").WithLocation(7, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterInterfaceConversion()
        {
            string source = @"
interface I1
{
}

class C1 : I1
{
    static void M1<T>()
        where T : C1, new()
    {
        I1 /*<bind>*/i1 = new T()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = new T()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new T()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsImplicit) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T()')
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterToInterfaceConversion_InvalidConversion()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T>()
        where T : C1, new()
    {
        I1 /*<bind>*/i1 = new T()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = new T()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new T()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsInvalid, IsImplicit) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T, IsInvalid) (Syntax: 'new T()')
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'T' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new T()").WithArguments("T", "I1").WithLocation(11, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterToConstraintParameterConversion()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T, U>()
        where T : U, new()
        where U : class
    {
        U /*<bind>*/u = new T()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: U u) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'u = new T()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new T()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: U, IsImplicit) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T()')
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterToConstraintParameter_InvalidConversion()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T, U>()
        where T : class, new()
        where U : class
    {
        U /*<bind>*/u = new T()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: U u) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'u = new T()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new T()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: U, IsInvalid, IsImplicit) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T, IsInvalid) (Syntax: 'new T()')
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'T' to 'U'
                //         U /*<bind>*/u = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new T()").WithArguments("T", "U").WithLocation(12, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterFromNull()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T, U>()
        where T : class, new()
    {
        T /*<bind>*/t = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: T t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = null')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= null')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, Constant: null, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 't' is assigned but its value is never used
                //         T /*<bind>*/t = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t").WithArguments("t").WithLocation(11, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterFromNull_InvalidNoReferenceConstraint()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T, U>()
        where T : new()
    {
        T /*<bind>*/t = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: T t) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 't = null')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= null')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsInvalid, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0403: Cannot convert null to type parameter 'T' because it could be a non-nullable value type. Consider using 'default(T)' instead.
                //         T /*<bind>*/t = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T").WithLocation(11, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToObjectConversion()
        {
            string source = @"

class C1
{
    static void M1()
    {
        int i = 1;
        object /*<bind>*/o = i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Object o) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'o = i')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToDynamicConversion()
        {
            string source = @"

class C1
{
    static void M1()
    {
        int i = 1;
        dynamic /*<bind>*/d = i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: dynamic d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd = i')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingValueToSystemValueTypeConversion()
        {
            string source = @"
using System;

struct S1
{
    void M1()
    {
        ValueType /*<bind>*/v1 = new S1()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.ValueType v1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v1 = new S1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new S1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ValueType, IsImplicit) (Syntax: 'new S1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: S1..ctor()) (OperationKind.ObjectCreation, Type: S1) (Syntax: 'new S1()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToSystemValueTypeConversion_InvalidNonValueType()
        {
            string source = @"
using System;

class C1
{
    void M1()
    {
        ValueType /*<bind>*/v1 = new C1()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.ValueType v1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'v1 = new C1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ValueType, IsInvalid, IsImplicit) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C1' to 'System.ValueType'
                //         ValueType /*<bind>*/v1 = new C1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C1()").WithArguments("C1", "System.ValueType").WithLocation(8, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToImplementingInterfaceConversion()
        {
            string source = @"
interface I1
{
}

struct S1 : I1
{
    void M1()
    {
        I1 /*<bind>*/i1 = new S1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = new S1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new S1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsImplicit) (Syntax: 'new S1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: S1..ctor()) (OperationKind.ObjectCreation, Type: S1) (Syntax: 'new S1()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToImplementingInterfaceConversion_InvalidNotImplementing()
        {
            string source = @"
interface I1
{
}

struct S1
{
    void M1()
    {
        I1 /*<bind>*/i1 = new S1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = new S1()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new S1()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsInvalid, IsImplicit) (Syntax: 'new S1()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: S1..ctor()) (OperationKind.ObjectCreation, Type: S1, IsInvalid) (Syntax: 'new S1()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1' to 'I1'
                //         I1 /*<bind>*/i1 = new S1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S1()").WithArguments("S1", "I1").WithLocation(10, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingNullableValueToImplementingInterfaceConversion()
        {
            string source = @"
interface I1
{
}

struct S1 : I1
{
    void M1()
    {
        S1? s1 = null;
        I1 /*<bind>*/i1 = s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = s1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= s1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsImplicit) (Syntax: 's1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: s1 (OperationKind.LocalReference, Type: S1?) (Syntax: 's1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingNullableValueToImplementingInterfaceConversion_InvalidNotImplementing()
        {
            string source = @"
interface I1
{
}

struct S1
{
    void M1()
    {
        S1? s1 = null;
        I1 /*<bind>*/i1 = s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = s1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= s1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsInvalid, IsImplicit) (Syntax: 's1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: s1 (OperationKind.LocalReference, Type: S1?, IsInvalid) (Syntax: 's1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1?' to 'I1'
                //         I1 /*<bind>*/i1 = s1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("S1?", "I1").WithLocation(11, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingEnumToSystemEnumConversion()
        {
            string source = @"
using System;

enum E1
{
    E
}

struct S1
{
    void M1()
    {
        Enum /*<bind>*/e = E1.E/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Enum e) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e = E1.E')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= E1.E')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Enum, IsImplicit) (Syntax: 'E1.E')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IFieldReferenceOperation: E1.E (Static) (OperationKind.FieldReference, Type: E1, Constant: 0) (Syntax: 'E1.E')
            Instance Receiver: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_BoxingEnumToSystemEnumConversion_InvalidNotEnum()
        {
            string source = @"
using System;

enum E1
{
    E
}

struct S1
{
    void M1()
    {
        Enum /*<bind>*/e = 1/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Enum e) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e = 1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= 1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Enum, IsInvalid, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'System.Enum'
                //         Enum /*<bind>*/e = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "System.Enum").WithLocation(13, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_DynamicConversionToClass()
        {
            string source = @"
class S1
{
    void M1()
    {
        dynamic d1 = 1;
        string /*<bind>*/s1 = d1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.String s1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's1 = d1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= d1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'd1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: d1 (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_DynamicConversionToValueType()
        {
            string source = @"
class S1
{
    void M1()
    {
        dynamic d1 = null;
        int /*<bind>*/i1 = d1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = d1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= d1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'd1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: d1 (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ConstantExpressionConversion()
        {
            string source = @"
class S1
{
    void M1()
    {
        const int i1 = 1;
        const sbyte /*<bind>*/s1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.SByte s1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's1 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.SByte, Constant: 1, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(7, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                    additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ConstantExpressionConversion_InvalidValueTooLarge()
        {
            string source = @"
class S1
{
    void M1()
    {
        const int i1 = 0x1000;
        const sbyte /*<bind>*/s1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.SByte s1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 's1 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.SByte, IsInvalid, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, Constant: 4096, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,36): error CS0031: Constant value '4096' cannot be converted to a 'sbyte'
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "i1").WithArguments("4096", "sbyte").WithLocation(7, 36),
                // (7,31): warning CS0219: The variable 's1' is assigned but its value is never used
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(7, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ConstantExpressionConversion_InvalidNonConstantExpression()
        {
            string source = @"
class S1
{
    void M1()
    {
        int i1 = 0;
        const sbyte /*<bind>*/s1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.SByte s1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 's1 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.SByte, IsInvalid, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'sbyte'. An explicit conversion exists (are you missing a cast?)
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int", "sbyte").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_UserDefinedConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        C2 /*<bind>*/c2 = this/*</bind>*/;
    }
}

class C2
{
    public static implicit operator C2(C1 c1)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2 c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = this')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= this')
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C2 C2.op_Implicit(C1 c1)) (OperationKind.Conversion, Type: C2, IsImplicit) (Syntax: 'this')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C2.op_Implicit(C1 c1))
        Operand: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1) (Syntax: 'this')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_UserDefinedMultiImplicitStepConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        int i1 = 1;
        C2 /*<bind>*/c2 = i1/*</bind>*/;
    }
}

class C2
{
    public static implicit operator C2(long c1)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2 c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C2 C2.op_Implicit(System.Int64 c1)) (OperationKind.Conversion, Type: C2, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C2.op_Implicit(System.Int64 c1))
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'i1')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier()
                {
                    ConversionChildSelector = ExpectedSymbolVerifier.NestedConversionChildSelector
                }.Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_UserDefinedMultiImplicitAndExplicitStepConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        int i1 = 1;
        C2 /*<bind>*/c2 = (int)this/*</bind>*/;
    }

    public static implicit operator int(C1 c1)
    {
        return 1;
    }
}

class C2
{
    public static implicit operator C2(long c1)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2 c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = (int)this')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int)this')
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C2 C2.op_Implicit(System.Int64 c1)) (OperationKind.Conversion, Type: C2, IsImplicit) (Syntax: '(int)this')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C2.op_Implicit(System.Int64 c1))
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: '(int)this')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int32 C1.op_Implicit(C1 c1)) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)this')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C1.op_Implicit(C1 c1))
                Operand: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1) (Syntax: 'this')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         int i1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_UserDefinedMultiImplicitAndExplicitStepConversion_InvalidMissingExplicitConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        int i1 = 1;
        C2 /*<bind>*/c2 = this/*</bind>*/;
    }

    public static implicit operator int(C1 c1)
    {
        return 1;
    }
}

class C2
{
    public static implicit operator C2(long c1)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2 c2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2 = this')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= this')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C2, IsInvalid, IsImplicit) (Syntax: 'this')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid) (Syntax: 'this')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C1' to 'C2'
                //         C2 /*<bind>*/c2 = this/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "this").WithArguments("C1", "C2").WithLocation(7, 27),
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         int i1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_UserDefinedMultipleCandidateConversion()
        {
            string source = @"
class C1
{
}

class C2 : C1
{
    void M1()
    {
        C3 /*<bind>*/c3 = this/*</bind>*/;
    }
}

class C3
{
    public static implicit operator C3(C1 c1)
    {
        return null;
    }

    public static implicit operator C3(C2 c2)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C3 c3) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c3 = this')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= this')
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C3 C3.op_Implicit(C2 c2)) (OperationKind.Conversion, Type: C3, IsImplicit) (Syntax: 'this')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C3 C3.op_Implicit(C2 c2))
        Operand: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C2) (Syntax: 'this')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_PointerFromNullConversion()
        {
            string source = @"
using System;

class S1
{
    unsafe void M1()
    {
        void* /*<bind>*/v1 = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Void* v1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v1 = null')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= null')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Void*, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_PointerToVoidConversion()
        {
            string source = @"
using System;

class S1
{
    unsafe void M1()
    {
        int* i1 = null;
        void* /*<bind>*/v1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Void* v1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v1 = i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Void*, IsImplicit) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32*) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_PointerFromVoidConversion_Invalid()
        {
            string source = @"
using System;

class S1
{
    unsafe void M1()
    {
        void* v1 = null;
        int* /*<bind>*/i1 = v1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32* i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = v1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= v1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32*, IsInvalid, IsImplicit) (Syntax: 'v1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: v1 (OperationKind.LocalReference, Type: System.Void*, IsInvalid) (Syntax: 'v1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'void*' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         int* /*<bind>*/i1 = v1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "v1").WithArguments("void*", "int*").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_PointerFromIntegerConversion_Invalid()
        {
            string source = @"
using System;

class S1
{
    unsafe void M1()
    {
        void* /*<bind>*/v1 = 0/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Void* v1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'v1 = 0')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= 0')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Void*, IsInvalid, IsImplicit) (Syntax: '0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'void*'. An explicit conversion exists (are you missing a cast?)
                //         void* /*<bind>*/v1 = 0/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "0").WithArguments("int", "void*").WithLocation(8, 30),
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ExpressionTreeConversion()
        {
            string source = @"
using System;
using System.Linq.Expressions;

class Program
{
    static void Main(string[] args)
    {
        Expression<Func<int, bool>> /*<bind>*/exp = num => num < 5/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>> exp) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'exp = num => num < 5')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= num => num < 5')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>>, IsImplicit) (Syntax: 'num => num < 5')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'num => num < 5')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'num < 5')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'num < 5')
                ReturnedValue: 
                  IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'num < 5')
                    Left: 
                      IParameterReferenceOperation: num (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'num')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20291")]
        public void ConversionExpression_Implicit_ExpressionTreeConversion_InvalidIncorrectLambdaType()
        {
            string source = @"
using System;
using System.Linq.Expressions;

class Program
{
    static void Main(string[] args)
    {
        Expression<Func<int, bool>> /*<bind>*/exp = num => num/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarators) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
    Variables: Local_1: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>> exp
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>>, IsInvalid) (Syntax: 'num => num')
        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'num => num')
          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'num')
            IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'num')
              IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'num')
                IParameterReferenceExpression: num (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'num')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         Expression<Func<int, bool>> /*<bind>*/exp = num => num/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "num").WithArguments("int", "bool").WithLocation(9, 60),
                // CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Expression<Func<int, bool>> /*<bind>*/exp = num => num/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "num").WithArguments("lambda expression").WithLocation(9, 60)
            };

            // Due to https://github.com/dotnet/roslyn/issues/20291, we cannot verify that the types of the ioperation tree and the semantic model
            // match, as they do not actually match.
            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ExpressionTreeConversion_InvalidSyntax()
        {
            string source = @"
using System;
using System.Linq.Expressions;

class Program
{
    static void Main(string[] args)
    {
        Expression<Func<int, bool>> /*<bind>*/exp = num =>/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>> exp) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'exp = num =>/*</bind>*/')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= num =>/*</bind>*/')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>>, IsInvalid, IsImplicit) (Syntax: 'num =>/*</bind>*/')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'num =>/*</bind>*/')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                ReturnedValue: 
                  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                    Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         Expression<Func<int, bool>> /*<bind>*/exp = num =>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(9, 70)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReturnStatementConversion()
        {
            string source = @"
class C1
{
    public long M1()
    {
        int i = 1;
        /*<bind>*/return i;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return i;')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReturnStatementConversion_InvalidConversion()
        {
            string source = @"
class C1
{
    public int M1()
    {
        float f = 1;
        /*<bind>*/return f;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return f;')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'f')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: f (OperationKind.LocalReference, Type: System.Single, IsInvalid) (Syntax: 'f')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         /*<bind>*/return f;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "f").WithArguments("float", "int").WithLocation(7, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_CheckedOnlyAppliesToNumeric()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            checked
            {
                /*<bind>*/object o = null;/*</bind>*/
            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'object o = null;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'object o = null')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Object o) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'o = null')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= null')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'o' is assigned but its value is never used
                //                 /*<bind>*/object o = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "o").WithArguments("o").WithLocation(10, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_DelegateTypeConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<object> objectAction = str => { };
        /*<bind>*/Action<string> stringAction = objectAction;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Action<stri ... jectAction;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Action<stri ... bjectAction')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.String> stringAction) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'stringActio ... bjectAction')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= objectAction')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action<System.String>, IsImplicit) (Syntax: 'objectAction')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILocalReferenceOperation: objectAction (OperationKind.LocalReference, Type: System.Action<System.Object>) (Syntax: 'objectAction')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_DelegateTypeConversion_InvalidConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<object> objectAction = str => { };
        /*<bind>*/Action<int> intAction = objectAction;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action<int> ... jectAction;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action<int> ... bjectAction')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.Int32> intAction) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'intAction = objectAction')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= objectAction')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'objectAction')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILocalReferenceOperation: objectAction (OperationKind.LocalReference, Type: System.Action<System.Object>, IsInvalid) (Syntax: 'objectAction')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'System.Action<object>' to 'System.Action<int>'
                //         /*<bind>*/Action<int> intAction = objectAction;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "objectAction").WithArguments("System.Action<object>", "System.Action<int>").WithLocation(8, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConversionFlow_01()
        {
            string source = @"
class C
{
    void M(int i, long l)
    /*<bind>*/{
        l = i;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'l = i;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64) (Syntax: 'l = i')
              Left: 
                IParameterReferenceOperation: l (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'l')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'i')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitNumeric)
                  Operand: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        #endregion

        #region Explicit Conversion

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ExplicitIdentityConversionCreatesIConversionExpression()
        {
            string source = @"
class C1
{
    public void M1()
    {
        int /*<bind>*/i = (int)1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (int)1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int)1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1) (Syntax: '(int)1')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //         int /*<bind>*/i = (int)1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ImplicitAndExplicitConversion()
        {
            string source = @"
class C1
{
    public void M1()
    {
        long /*<bind>*/i = (int)1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int64 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (int)1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int)1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: '(int)1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1) (Syntax: '(int)1')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //         long /*<bind>*/i = (int)1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_SimpleNumericCast()
        {
            string source = @"
class C1
{
    public void M1()
    {
        int i = /*<bind>*/(int)1.0/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1) (Syntax: '(int)1.0')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 1) (Syntax: '1.0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //         int i = /*<bind>*/(int)1.0/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_SimpleNumericConversion_InvalidNoImplicitConversion()
        {
            string source = @"
class C1
{
    public void M1()
    {
        int /*<bind>*/i = (float)1.0/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i = (float)1.0')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (float)1.0')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '(float)1.0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Single, Constant: 1, IsInvalid) (Syntax: '(float)1.0')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 1, IsInvalid) (Syntax: '1.0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (6,27): error CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i = (float)1.0/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(float)1.0").WithArguments("float", "int").WithLocation(6, 27),
                // (6,23): warning CS0219: The variable 'i' is assigned but its value is never used
                //         int /*<bind>*/i = (float)1.0/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_SimpleNumericConversion_InvalidSyntax()
        {
            string source = @"
class C1
{
    public void M1()
    {
        long /*<bind>*/i = (int)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int64 i) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i = (int)/*</bind>*/')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (int)/*</bind>*/')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsInvalid, IsImplicit) (Syntax: '(int)/*</bind>*/')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid) (Syntax: '(int)/*</bind>*/')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         long /*<bind>*/i = (int)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 44)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_EnumFromNumericLiteralConversion()
        {
            string source = @"
class C1
{
    public void M1()
    {
        E1 /*<bind>*/e1 = (E1)1/*</bind>*/;
    }
}

enum E1
{
    One, Two
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: E1 e1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e1 = (E1)1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (E1)1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: E1, Constant: 1) (Syntax: '(E1)1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'e1' is assigned but its value is never used
                //         E1 /*<bind>*/e1 = (E1)1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "e1").WithArguments("e1").WithLocation(6, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_EnumToNumericTypeConversion()
        {
            string source = @"
class C1
{
    public void M1()
    {
        int /*<bind>*/i = (int)E1.One/*</bind>*/;
    }
}

enum E1
{
    One, Two
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (int)E1.One')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int)E1.One')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 0) (Syntax: '(int)E1.One')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IFieldReferenceOperation: E1.One (Static) (OperationKind.FieldReference, Type: E1, Constant: 0) (Syntax: 'E1.One')
            Instance Receiver: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //         int /*<bind>*/i = (int)E1.One/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_EnumToEnumConversion()
        {
            string source = @"
class C1
{
    public void M1()
    {
        E2 /*<bind>*/e2 = (E2)E1.One/*</bind>*/;
    }
}

enum E1
{
    One, Two
}

enum E2
{
    Three, Four
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: E2 e2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e2 = (E2)E1.One')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (E2)E1.One')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: E2, Constant: 0) (Syntax: '(E2)E1.One')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IFieldReferenceOperation: E1.One (Static) (OperationKind.FieldReference, Type: E1, Constant: 0) (Syntax: 'E1.One')
            Instance Receiver: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'e2' is assigned but its value is never used
                //         E2 /*<bind>*/e2 = (E2)E1.One/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "e2").WithArguments("e2").WithLocation(6, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_EnumToEnumConversion_InvalidOutOfRange()
        {
            string source = @"
class C1
{
    public void M1()
    {
        E2 /*<bind>*/e2 = (E2)E1.One/*</bind>*/;
    }
}

enum E1
{
    One = 1000
}

enum E2 : byte
{
    Two
}
";

            // Note: The lack of a constant value for the conversion is expected here, it matches the semantic model.
            // Because the enum value is larger than the destination enum, the conversion is bad
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: E2 e2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e2 = (E2)E1.One')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (E2)E1.One')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: E2, IsInvalid) (Syntax: '(E2)E1.One')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IFieldReferenceOperation: E1.One (Static) (OperationKind.FieldReference, Type: E1, Constant: 1000, IsInvalid) (Syntax: 'E1.One')
            Instance Receiver: 
              null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0221: Constant value '1000' cannot be converted to a 'E2' (use 'unchecked' syntax to override)
                //         E2 /*<bind>*/e2 = (E2)E1.One/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(E2)E1.One").WithArguments("1000", "E2").WithLocation(6, 27),
                // CS0219: The variable 'e2' is assigned but its value is never used
                //         E2 /*<bind>*/e2 = (E2)E1.One/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "e2").WithArguments("e2").WithLocation(6, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_NullableToNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        long? l = null;
        int? /*<bind>*/i = (int?)l/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32? i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (int?)l')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int?)l')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?) (Syntax: '(int?)l')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Int64?) (Syntax: 'l')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_NullableToNonNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        long? l = null;
        int /*<bind>*/i = (int)l/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (int)l')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int)l')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)l')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Int64?) (Syntax: 'l')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceFromObjectConversion()
        {
            string source = @"
class C1
{
    static void M1()
    {
        object o = string.Empty;
        string /*<bind>*/s = (string)o/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.String s) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's = (string)o')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (string)o')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String) (Syntax: '(string)o')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceFromDynamicConversion()
        {
            string source = @"
class C1
{
    static void M1()
    {
        dynamic d = string.Empty;
        string /*<bind>*/s = (string)d/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.String s) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's = (string)d')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (string)d')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String) (Syntax: '(string)d')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceFromSuperclassConversion()
        {
            string source = @"
class C1
{
    static void M1()
    {
        C1 c1 = new C2();
        C2 /*<bind>*/c2 = (C2)c1/*</bind>*/;
    }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2 c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = (C2)c1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C2)c1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C2) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceFromSuperclassConversion_InvalidNoConversion()
        {
            string source = @"
class C1
{
    static void M1()
    {
        C1 c1 = new C1();
        C2 /*<bind>*/c2 = (C2)c1/*</bind>*/;
    }
}

class C2
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2 c2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2 = (C2)c1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (C2)c1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C2, IsInvalid) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1, IsInvalid) (Syntax: 'c1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'C1' to 'C2'
                //         C2 /*<bind>*/c2 = (C2)c1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C2)c1").WithArguments("C1", "C2").WithLocation(7, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceFromImplementedInterfaceConversion()
        {
            string source = @"
interface I1 { }

class C1 : I1
{
    static void M1()
    {
        I1 i1 = new C1();
        C1 /*<bind>*/c1 = (C1)i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = (C1)i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C1)i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1) (Syntax: '(C1)i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: I1) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceFromUnimplementedInterfaceConversion()
        {
            string source = @"
interface I1 { }

class C1
{
    static void M1()
    {
        I1 i1 = null;
        C1 /*<bind>*/c1 = (C1)i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = (C1)i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C1)i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1) (Syntax: '(C1)i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: I1) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceFromUnimplementedInterfaceConversion_InvalidSealedClass()
        {
            string source = @"
interface I1 { }

sealed class C1
{
    static void M1()
    {
        I1 i1 = null;
        C1 /*<bind>*/c1 = (C1)i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1 c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = (C1)i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (C1)i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsInvalid) (Syntax: '(C1)i1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: I1, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'I1' to 'C1'
                //         C1 /*<bind>*/c1 = (C1)i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C1)i1").WithArguments("I1", "C1").WithLocation(9, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceFromInterfaceToInterfaceConversion()
        {
            string source = @"
interface I1 { }

interface I2 { }

sealed class C1
{
    static void M1()
    {
        I1 i1 = null;
        I2 /*<bind>*/i2 = (I2)i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I2 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2 = (I2)i1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (I2)i1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I2) (Syntax: '(I2)i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: I1) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceConversion_InvalidSyntax()
        {
            string source = @"
interface I2 { }

sealed class C1
{
    static void M1()
    {
        I2 /*<bind>*/i2 = (I2)()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I2 i2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i2 = (I2)()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (I2)()')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I2, IsInvalid) (Syntax: '(I2)()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         I2 /*<bind>*/i2 = (I2)()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceArrayTypeToArrayTypeConversion()
        {
            string source = @"
class C1
{
    static void M1()
    {
        C1[] c1arr = new C2[1];
        C2[] /*<bind>*/c2arr = (C2[])c1arr/*</bind>*/;
    }
}

class C2 : C1 { }
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2[] c2arr) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2arr = (C2[])c1arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C2[])c1arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C2[]) (Syntax: '(C2[])c1arr')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1arr (OperationKind.LocalReference, Type: C1[]) (Syntax: 'c1arr')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceArrayTypeToArrayTypeConversion_InvalidNoElementTypeConversion()
        {
            string source = @"
class C1
{
    static void M1()
    {
        C1[] c1arr = new C1[1];
        C2[] /*<bind>*/c2arr = (C2[])c1arr/*</bind>*/;
    }
}

class C2 { }
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2[] c2arr) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2arr = (C2[])c1arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (C2[])c1arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C2[], IsInvalid) (Syntax: '(C2[])c1arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1arr (OperationKind.LocalReference, Type: C1[], IsInvalid) (Syntax: 'c1arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'C1[]' to 'C2[]'
                //         C2[] /*<bind>*/c2arr = (C2[])c1arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C2[])c1arr").WithArguments("C1[]", "C2[]").WithLocation(7, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceArrayTypeToArrayTypeConversion_InvalidMismatchedSized()
        {
            string source = @"
class C1
{
    static void M1()
    {
        C1[] c1arr = new C1[1];
        C1[][] /*<bind>*/c2arr = (C1[][])c1arr/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1[][] c2arr) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2arr = (C1[][])c1arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (C1[][])c1arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1[][], IsInvalid) (Syntax: '(C1[][])c1arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1arr (OperationKind.LocalReference, Type: C1[], IsInvalid) (Syntax: 'c1arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'C1[]' to 'C1[][]'
                //         C1[][] /*<bind>*/c2arr = (C1[][])c1arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C1[][])c1arr").WithArguments("C1[]", "C1[][]").WithLocation(7, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceSystemArrayToArrayTypeConversion()
        {
            string source = @"
using System;

class C1
{
    static void M1()
    {
        Array c1arr = new C1[1];
        C1[] /*<bind>*/c2arr = (C1[])c1arr/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1[] c2arr) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2arr = (C1[])c1arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C1[])c1arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1[]) (Syntax: '(C1[])c1arr')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1arr (OperationKind.LocalReference, Type: System.Array) (Syntax: 'c1arr')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceArrayToIListConversion()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C1
{
    static void M1()
    {
        C1[] c1arr = new C1[1];
        IList<C1> /*<bind>*/c1list = (IList<C1>)c1arr/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IList<C1> c1list) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1list = (I ... t<C1>)c1arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (IList<C1>)c1arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<C1>) (Syntax: '(IList<C1>)c1arr')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1arr (OperationKind.LocalReference, Type: C1[]) (Syntax: 'c1arr')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceArrayToIListConversion_InvalidMismatchedDimensions()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C1
{
    static void M1()
    {
        C1[][] c1arr = new C1[1][];
        IList<C1> /*<bind>*/c1list = (IList<C1>)c1arr/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IList<C1> c1list) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1list = (I ... t<C1>)c1arr')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (IList<C1>)c1arr')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<C1>, IsInvalid) (Syntax: '(IList<C1>)c1arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1arr (OperationKind.LocalReference, Type: C1[][], IsInvalid) (Syntax: 'c1arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'C1[][]' to 'System.Collections.Generic.IList<C1>'
                //         IList<C1> /*<bind>*/c1list = (IList<C1>)c1arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(IList<C1>)c1arr").WithArguments("C1[][]", "System.Collections.Generic.IList<C1>").WithLocation(10, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceIListToArrayTypeConversion()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C1
{
    static void M1()
    {
        IList<C1> c1List = new List<C1>();
        C1[] /*<bind>*/c1arr = (C1[])c1List/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1[] c1arr) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1arr = (C1[])c1List')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C1[])c1List')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1[]) (Syntax: '(C1[])c1List')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1List (OperationKind.LocalReference, Type: System.Collections.Generic.IList<C1>) (Syntax: 'c1List')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceIListToArrayTypeConversion_InvalidMismatchedDimensions()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C1
{
    static void M1()
    {
        IList<C1> c1List = new List<C1>();
        C1[][] /*<bind>*/c1arr = (C1[][])c1List/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C1[][] c1arr) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1arr = (C1[][])c1List')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (C1[][])c1List')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1[][], IsInvalid) (Syntax: '(C1[][])c1List')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c1List (OperationKind.LocalReference, Type: System.Collections.Generic.IList<C1>, IsInvalid) (Syntax: 'c1List')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'System.Collections.Generic.IList<C1>' to 'C1[][]'
                //         C1[][] /*<bind>*/c1arr = (C1[][])c1List/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C1[][])c1List").WithArguments("System.Collections.Generic.IList<C1>", "C1[][]").WithLocation(10, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceDelegateToDelegateTypeConversion()
        {
            string source = @"
using System;

class C1
{
    static void M1()
    {
        Delegate d = (Action)(() => { });
        Action /*<bind>*/a = (Action)d/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Action a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = (Action)d')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (Action)d')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action) (Syntax: '(Action)d')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Delegate) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReferenceContravarianceConversion()
        {
            string source = @"
interface I1<out T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C3> c2 = new C2<C3>();
        I1<C4> /*<bind>*/c1 = (I1<C4>)c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1<C4> c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = (I1<C4>)c2')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (I1<C4>)c2')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1<C4>) (Syntax: '(I1<C4>)c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C2<C3>) (Syntax: 'c2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingObjectToValueTypeConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        object o = 1;
        int /*<bind>*/i = (int)o/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (int)o')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int)o')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)o')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingDynamicToValueTypeConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        dynamic d = 1;
        int /*<bind>*/i = (int)d/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (int)d')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int)d')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)d')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingSystemValueTypeToValueTypeConversion()
        {
            string source = @"
using System;

class C1
{
    void M1()
    {
        ValueType v = 1;
        int /*<bind>*/i = (int)v/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (int)v')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (int)v')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)v')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: v (OperationKind.LocalReference, Type: System.ValueType) (Syntax: 'v')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingSystemEnumToEnumConversion()
        {
            string source = @"
using System;

class C1
{
    void M1()
    {
        Enum e = E1.One;
        E1 /*<bind>*/e1 = (E1)e/*</bind>*/;
    }
}

enum E1
{
    One = 1
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: E1 e1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e1 = (E1)e')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (E1)e')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: E1) (Syntax: '(E1)e')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Enum) (Syntax: 'e')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingReferenceToNullableTypeConversion()
        {
            string source = @"
using System;

class C1
{
    void M1()
    {
        Enum e = null;
        E1? /*<bind>*/e1 = (E1?)e/*</bind>*/;
    }
}

enum E1
{
    One = 1
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: E1? e1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e1 = (E1?)e')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (E1?)e')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: E1?) (Syntax: '(E1?)e')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Enum) (Syntax: 'e')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingReferenceToNullableTypeConversion_InvalidNoConversionToNonNullableType()
        {
            string source = @"
using System;

class C1
{
    void M1()
    {
        Enum e = null;
        int? /*<bind>*/e1 = (E1?)e/*</bind>*/;
    }
}

enum E1
{
    One = 1
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32? e1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'e1 = (E1?)e')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (E1?)e')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: '(E1?)e')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: E1?, IsInvalid) (Syntax: '(E1?)e')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Enum, IsInvalid) (Syntax: 'e')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'E1?' to 'int?'. An explicit conversion exists (are you missing a cast?)
                //         int? /*<bind>*/e1 = (E1?)e/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(E1?)e").WithArguments("E1?", "int?").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingValueTypeFromInterfaceConversion()
        {
            string source = @"
using System;

class C1
{
    void M1()
    {
        I1 i = null;
        S1 /*<bind>*/s1 = (S1)i/*</bind>*/;
    }
}

interface I1 { }

struct S1 : I1 { }
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: S1 s1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's1 = (S1)i')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (S1)i')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S1) (Syntax: '(S1)i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: I1) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingValueTypeFromInterfaceConversion_InvalidNoConversion()
        {
            string source = @"
using System;

class C1
{
    void M1()
    {
        I1 i = null;
        S1 /*<bind>*/s1 = (S1)i/*</bind>*/;
    }
}

interface I1 { }

struct S1 { }
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: S1 s1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 's1 = (S1)i')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (S1)i')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S1, IsInvalid) (Syntax: '(S1)i')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: I1, IsInvalid) (Syntax: 'i')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'I1' to 'S1'
                //         S1 /*<bind>*/s1 = (S1)i/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(S1)i").WithArguments("I1", "S1").WithLocation(9, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_UnboxingVarianceConversion()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C1
{
    void M1()
    {
        IList<I1> i1List = new List<I1>();
        IList<S1> /*<bind>*/s1List = (IList<S1>)i1List/*</bind>*/;
    }
}

interface I1 { }

struct S1 : I1 { }
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IList<S1> s1List) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's1List = (I ... <S1>)i1List')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (IList<S1>)i1List')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<S1>) (Syntax: '(IList<S1>)i1List')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: i1List (OperationKind.LocalReference, Type: System.Collections.Generic.IList<I1>) (Syntax: 'i1List')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_TypeParameterConstraintConversion()
        {
            string source = @"
using System;

class C1
{
    void M1<T, U>(U u) where T : U
    {
        T /*<bind>*/t = (T)u/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: T t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (T)u')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (T)u')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T) (Syntax: '(T)u')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U) (Syntax: 'u')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_TypeParameterConversion_InvalidNoConversion()
        {
            string source = @"
using System;

class C1
{
    void M1<T, U>(U u)
    {
        T /*<bind>*/t = (T)u/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: T t) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 't = (T)u')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (T)u')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsInvalid) (Syntax: '(T)u')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U, IsInvalid) (Syntax: 'u')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'U' to 'T'
                //         T /*<bind>*/t = (T)u/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(T)u").WithArguments("U", "T").WithLocation(8, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_TypeParameterToInterfaceConversion()
        {
            string source = @"
interface I1 { }

class C1
{
    void M1<T>(I1 i)
    {
        T /*<bind>*/t = (T)i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: T t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (T)i')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (T)i')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T) (Syntax: '(T)i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: I1) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_TypeParameterFromInterfaceConversion()
        {
            string source = @"
interface I1 { }

class C1
{
    void M1<T>(T t)
    {
        I1 /*<bind>*/i = (I1)t/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = (I1)t')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (I1)t')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1) (Syntax: '(I1)t')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: T) (Syntax: 't')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ImplicitUserDefinedConversionAsExplicitConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        C1 c1 = new C1();
        C2 /*<bind>*/c2 = (C2)c1/*</bind>*/;
    }

    public static implicit operator C2(C1 c1) => new C2();
}

class C2 { }
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2 c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = (C2)c1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C2)c1')
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C2 C1.op_Implicit(C1 c1)) (OperationKind.Conversion, Type: C2) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C1.op_Implicit(C1 c1))
        Operand: 
          ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ExplicitUserDefinedConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        C1 c1 = new C1();
        C2 /*<bind>*/c2 = (C2)c1/*</bind>*/;
    }

    public static explicit operator C2(C1 c1) => new C2();
}

class C2 { }
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C2 c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = (C2)c1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C2)c1')
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C2 C1.op_Explicit(C1 c1)) (OperationKind.Conversion, Type: C2) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C1.op_Explicit(C1 c1))
        Operand: 
          ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ExplicitUserDefinedConversion_WithImplicitConversionAfter()
        {
            string source = @"
interface I1 { }

class C1
{
    void M1()
    {
        C1 c1 = new C1();
        I1 /*<bind>*/i1 = (C2)c1/*</bind>*/;
    }

    public static explicit operator C2(C1 c1) => new C2();
}

class C2 : I1 { }
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: I1 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = (C2)c1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (C2)c1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: I1, IsImplicit) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C2 C1.op_Explicit(C1 c1)) (OperationKind.Conversion, Type: C2) (Syntax: '(C2)c1')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C1.op_Explicit(C1 c1))
            Operand: 
              ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReturnConversion()
        {
            string source = @"
using System;

class C1
{
    int M1()
    {
        /*<bind>*/return (int)1.0;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (int)1.0;')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1) (Syntax: '(int)1.0')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 1) (Syntax: '1.0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReturnConversion_InvalidConversion()
        {
            string source = @"
using System;

class C1
{
    int M1()
    {
        /*<bind>*/return (int)"""";/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return (int)"""";')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid) (Syntax: '(int)""""')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'string' to 'int'
                //         /*<bind>*/return (int)"";/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"(int)""""").WithArguments("string", "int").WithLocation(8, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_ReturnConversion_InvalidSyntax()
        {
            string source = @"
using System;

class C1
{
    int M1()
    {
        /*<bind>*/return (int);/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return (int);')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid) (Syntax: '(int)')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/return (int);/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(8, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_CheckedOnlyAppliesToNumeric()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            checked
            {
                /*<bind>*/object o = (object)null;/*</bind>*/
            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'object o = (object)null;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'object o = (object)null')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Object o) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'o = (object)null')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (object)null')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null) (Syntax: '(object)null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'o' is assigned but its value is never used
                //                 /*<bind>*/object o = (object)null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "o").WithArguments("o").WithLocation(10, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_DelegateTypeConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<object> objectAction = str => { };
        Action<string> stringAction = /*<bind>*/(Action<string>)objectAction/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action<System.String>) (Syntax: '(Action<str ... bjectAction')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ILocalReferenceOperation: objectAction (OperationKind.LocalReference, Type: System.Action<System.Object>) (Syntax: 'objectAction')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Explicit_DelegateTypeConversion_InvalidConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action<object> objectAction = str => { };
        Action<int> intAction = /*<bind>*/(Action<int>)objectAction/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action<System.Int32>, IsInvalid) (Syntax: '(Action<int ... bjectAction')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ILocalReferenceOperation: objectAction (OperationKind.LocalReference, Type: System.Action<System.Object>, IsInvalid) (Syntax: 'objectAction')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'System.Action<object>' to 'System.Action<int>'
                //         Action<int> intAction = /*<bind>*/(Action<int>)objectAction/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Action<int>)objectAction").WithArguments("System.Action<object>", "System.Action<int>").WithLocation(8, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConversionFlow_02()
        {
            string source = @"
class C
{
    void M(int i, bool b, long l, long m)
    /*<bind>*/{
        i = (int) (b ? l : m);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l')
              Value: 
                IParameterReferenceOperation: l (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'l')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'm')
              Value: 
                IParameterReferenceOperation: m (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'm')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = (int) (b ? l : m);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = (int) (b ? l : m)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int) (b ? l : m)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitNumeric)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int64, IsImplicit) (Syntax: 'b ? l : m')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
        #endregion

        private class ExpectedSymbolVerifier
        {
            public static SyntaxNode VariableDeclaratorSelector(SyntaxNode syntaxNode) =>
                ((VariableDeclaratorSyntax)syntaxNode).Initializer.Value;

            public static SyntaxNode IdentitySelector(SyntaxNode syntaxNode) => syntaxNode;

            public static SyntaxNode ReturnStatementSelector(SyntaxNode syntaxNode) => ((ReturnStatementSyntax)syntaxNode).Expression;

            public static IOperation IVariableDeclarationStatementSelector(IOperation operation) =>
                ((IVariableDeclarationGroupOperation)operation).Declarations.Single().Initializer;

            public static IOperation IVariableDeclarationSelector(IOperation operation) =>
                ((IVariableDeclarationOperation)operation).Initializer.Value;

            public static IOperation IVariableDeclaratorSelector(IOperation operation) =>
                ((IVariableDeclaratorOperation)operation).Initializer.Value;

            public static IOperation IReturnDeclarationStatementSelector(IOperation operation) =>
                ((IReturnOperation)operation).ReturnedValue;

            public static IOperation NestedConversionChildSelector(IOperation operation) =>
                ConversionOrDelegateChildSelector(ConversionOrDelegateChildSelector(operation));

            private static IOperation ConversionOrDelegateChildSelector(IOperation operation)
            {
                if (operation.Kind == OperationKind.Conversion)
                {
                    return ((IConversionOperation)operation).Operand;
                }
                else
                {
                    return ((IDelegateCreationOperation)operation).Target;
                }
            }

            public Func<IOperation, IConversionOperation> OperationSelector { get; set; }

            public Func<IOperation, IOperation> ConversionChildSelector { get; set; } = ConversionOrDelegateChildSelector;

            public Func<SyntaxNode, SyntaxNode> SyntaxSelector { get; set; }

            /// <summary>
            /// Verifies that the given operation has the type information that the semantic model has for the given
            /// syntax node. A selector is used to walk the operation tree and syntax tree for the final
            /// nodes to compare type info for.
            ///
            /// <see cref="SyntaxSelector"/> is used to to select the syntax node to test.
            /// <see cref="OperationSelector"/> is used to select the IConversion node to test.
            /// <see cref="ConversionChildSelector"/> is used to select what child node of the IConversion to compare original types to.
            /// this is useful for multiple conversion scenarios where we end up with multiple IConversion nodes in the tree.
            /// </summary>
            public void Verify(IOperation variableDeclaration, Compilation compilation, SyntaxNode syntaxNode)
            {
                var finalSyntax = GetAndInvokeSyntaxSelector(syntaxNode);
                var semanticModel = compilation.GetSemanticModel(finalSyntax.SyntaxTree);
                var typeInfo = semanticModel.GetTypeInfo(finalSyntax);

                var initializer = GetAndInvokeOperationSelector(variableDeclaration);

                var conversion = initializer;
                Assert.Equal(conversion.Type, typeInfo.ConvertedType);
                Assert.Equal(ConversionChildSelector(conversion).Type, typeInfo.Type);
            }

            private SyntaxNode GetAndInvokeSyntaxSelector(SyntaxNode syntax)
            {
                if (SyntaxSelector != null)
                {
                    return SyntaxSelector(syntax);
                }
                else
                {
                    switch (syntax)
                    {
                        case VariableDeclaratorSyntax _:
                            return VariableDeclaratorSelector(syntax);
                        case ReturnStatementSyntax _:
                            return ReturnStatementSelector(syntax);
                        case CastExpressionSyntax cast:
                            return cast.Expression;
                        default:
                            throw new ArgumentException($"Cannot handle syntax of type {syntax.GetType()}");
                    }
                }
            }

            private IOperation GetAndInvokeOperationSelector(IOperation operation)
            {
                if (OperationSelector != null)
                {
                    return OperationSelector(operation);
                }

                switch (operation)
                {
                    case IVariableDeclarationGroupOperation _:
                        return IVariableDeclarationStatementSelector(operation);
                    case IVariableDeclarationOperation _:
                        return IVariableDeclarationSelector(operation);
                    case IVariableDeclaratorOperation _:
                        return IVariableDeclaratorSelector(operation);
                    case IReturnOperation _:
                        return IReturnDeclarationStatementSelector(operation);
                    case IConversionOperation conv:
                        return conv;
                    default:
                        throw new ArgumentException($"Cannot handle arguments of type {operation.GetType()}");
                }
            }
        }
    }
}
