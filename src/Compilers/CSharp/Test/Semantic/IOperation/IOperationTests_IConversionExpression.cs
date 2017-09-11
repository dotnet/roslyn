// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Semantics;
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'dynamic /*< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'dynamic /*< ... *</bind>*/;')
    Variables: Local_1: dynamic d1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: 'o1')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: o1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        /// intAction<summary>
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o2
    Initializer: ILocalReferenceExpression: o1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'double /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'double /*<b ... *</bind>*/;')
    Variables: Local_1: System.Double d1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double) (Syntax: 'f1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: f1 (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'f1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'f1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: f1 (OperationKind.LocalReferenceExpression, Type: System.Single, IsInvalid) (Syntax: 'f1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i1 = f1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "f1").WithArguments("float", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         int /*<bind>*/i1 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(8, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Enum1 e1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Enum1, Constant: 0) (Syntax: '0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'e1' is assigned but its value is never used
                //         Enum1 /*<bind>*/e1 = 0/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "e1").WithArguments("e1").WithLocation(5, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Enum1 e1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Enum1, IsInvalid) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'Program.Enum1'. An explicit conversion exists (are you missing a cast?)
                //         Enum1 /*<bind>*/e1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int", "Enum1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Enum1 e1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Enum1, IsInvalid) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'Program.Enum1'. An explicit conversion exists (are you missing a cast?)
                //         Enum1 /*<bind>*/e1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "Enum1").WithLocation(5, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
        public void ConversionExpression_Implicit_EnumConversion_NoInitalizer_Invalid()
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
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
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o
    Initializer: ICoalesceExpression (OperationKind.CoalesceExpression, Type: System.Object) (Syntax: 'new object( ... Exception()')
        Expression: IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
            Arguments(0)
            Initializer: null
        WhenNull: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'throw new Exception()')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IThrowExpression (OperationKind.ThrowExpression, Type: null) (Syntax: 'throw new Exception()')
                IObjectCreationExpression (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new Exception()')
                  Arguments(0)
                  Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier()
                {
                    SyntaxSelector = (syntax) =>
                    {
                        var initializer = (BinaryExpressionSyntax)((VariableDeclaratorSyntax)syntax).Initializer.Value;
                        return initializer.Right;
                    },
                    OperationSelector = (operation) =>
                    {
                        var initializer = ((IVariableDeclarationStatement)operation).Declarations.Single().Initializer;
                        return (IConversionExpression)((ICoalesceExpression)initializer).WhenNull;
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'object /*<b ... *</bind>*/;')
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
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'string /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'string /*<b ... *</bind>*/;')
    Variables: Local_1: System.String s1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         string /*<bind>*/s1 = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'S1? /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'S1? /*<bind ... *</bind>*/;')
    Variables: Local_1: S1? s1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: S1?, Constant: null) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         S1? /*<bind>*/s1 = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(10, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'null')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         int /*<bind>*/i1 = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'long /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'long /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int64 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 0) (Syntax: 'default')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IDefaultValueExpression (OperationKind.DefaultValueExpression, Type: System.Int64, Constant: 0) (Syntax: 'default')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         long /*<bind>*/i1 = default(int)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(8, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Regular7_1,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'long /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'long /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int64 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 0) (Syntax: 'default(int)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IDefaultValueExpression (OperationKind.DefaultValueExpression, Type: System.Int32, Constant: 0) (Syntax: 'default(int)')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         long /*<bind>*/i1 = default(int)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(8, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        /// <summary>
        /// This test documents the fact that `default(T)` is already T, and does not introduce a conversion
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'string /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'string /*<b ... *</bind>*/;')
    Variables: Local_1: System.String i1
    Initializer: IDefaultValueExpression (OperationKind.DefaultValueExpression, Type: System.String, Constant: null) (Syntax: 'default(string)')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int? /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int? /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int32? i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         int? /*<bind>*/i1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'long? /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'long? /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Int64? l1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64?) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int? /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int? /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int32? i2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32?, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int?' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i2 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int?", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'IFormattabl ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'IFormattabl ... *</bind>*/;')
    Variables: Local_1: System.IFormattable f1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.IFormattable) (Syntax: '$""{1}""')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""{1}""')
            Parts(1):
                IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
                  Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Alignment: null
                  FormatString: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'dynamic /*< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'dynamic /*< ... *</bind>*/;')
    Variables: Local_1: dynamic d1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1) (Syntax: 'new C2()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: C2..ctor()) (OperationKind.ObjectCreationExpression, Type: C2) (Syntax: 'new C2()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new C2()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: C2..ctor()) (OperationKind.ObjectCreationExpression, Type: C2, IsInvalid) (Syntax: 'new C2()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2' to 'C1'
                //         C1 /*<bind>*/c1 = new C2()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C2()").WithArguments("C2", "C1").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new/*</bind>*/')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'new/*</bind>*/')
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
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1, IsInvalid) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C1' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = new C1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new C1()").WithArguments("C1", "I1").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new I1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: I1, IsInvalid) (Syntax: 'new I1()')
            Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0144: Cannot create an instance of the abstract class or interface 'I1'
                //         C1 /*<bind>*/i1 = new I1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new I1()").WithArguments("I1").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'i2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i2 (OperationKind.LocalReferenceExpression, Type: I2) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'i2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i2 (OperationKind.LocalReferenceExpression, Type: I2, IsInvalid) (Syntax: 'i2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'I2' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = i2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i2").WithArguments("I2", "I1").WithLocation(17, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
    Variables: Local_1: C1[] c1arr
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1[]) (Syntax: 'c2arr')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[]) (Syntax: 'c2arr')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion_InvalidDimenionMismatch()
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1[][] /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1[][] /*<b ... *</bind>*/;')
    Variables: Local_1: C1[][] c1arr
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1[][], IsInvalid) (Syntax: 'c2arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[], IsInvalid) (Syntax: 'c2arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2[]' to 'C1[][]'
                //         C1[][] /*<bind>*/c1arr = c2arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2arr").WithArguments("C2[]", "C1[][]").WithLocation(9, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
    Variables: Local_1: C1[] c1arr
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1[], IsInvalid) (Syntax: 'c2arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[], IsInvalid) (Syntax: 'c2arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2[]' to 'C1[]'
                //         C1[] /*<bind>*/c1arr = c2arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2arr").WithArguments("C2[]", "C1[]").WithLocation(9, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1[] /*<bin ... *</bind>*/;')
    Variables: Local_1: I1[] i1arr
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1[], IsInvalid) (Syntax: 'new S1[10]')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IArrayCreationExpression (Element Type: S1) (OperationKind.ArrayCreationExpression, Type: S1[], IsInvalid) (Syntax: 'new S1[10]')
            Dimension Sizes(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
            Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1[]' to 'I1[]'
                //         I1[] /*<bind>*/i1arr = new S1[10]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S1[10]").WithArguments("S1[]", "I1[]").WithLocation(8, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Array /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Array /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Array a1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'new object[10]')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IArrayCreationExpression (Element Type: System.Object) (OperationKind.ArrayCreationExpression, Type: System.Object[]) (Syntax: 'new object[10]')
            Dimension Sizes(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Array /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Array /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Array a1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'new int[10][]')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IArrayCreationExpression (Element Type: System.Int32[]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][]) (Syntax: 'new int[10][]')
            Dimension Sizes(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Array /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Array /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Array a1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Array, IsInvalid) (Syntax: 'new object()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object, IsInvalid) (Syntax: 'new object()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'System.Array'. An explicit conversion exists (are you missing a cast?)
                //         Array /*<bind>*/a1 = new object()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "System.Array").WithLocation(8, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'IList<int>  ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'IList<int>  ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<System.Int32> a1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<System.Int32>) (Syntax: 'new int[10]')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new int[10]')
            Dimension Sizes(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'IList<int>  ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'IList<int>  ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<System.Int32> a1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<System.Int32>, IsInvalid) (Syntax: 'new object()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object, IsInvalid) (Syntax: 'new object()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'System.Collections.Generic.IList<int>'. An explicit conversion exists (are you missing a cast?)
                //         IList<int> /*<bind>*/a1 = new object()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "System.Collections.Generic.IList<int>").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Delegate /* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Delegate /* ... *</bind>*/;')
    Variables: Local_1: System.Delegate d2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Delegate) (Syntax: 'd1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: C1.DType) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
    Variables: Local_1: System.Delegate d2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Delegate, IsInvalid) (Syntax: 'd1()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvocationExpression (virtual void C1.DType.Invoke()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'd1()')
            Instance Receiver: ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: C1.DType, IsInvalid) (Syntax: 'd1')
            Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'void' to 'System.Delegate'
                //         Delegate /*<bind>*/d2 = d1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d1()").WithArguments("void", "System.Delegate").WithLocation(10, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
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
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1) (Syntax: 'new C3()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: C3..ctor()) (OperationKind.ObjectCreationExpression, Type: C3) (Syntax: 'new C3()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C4> c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1<C4>) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C3>) (Syntax: 'c2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1<C3> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1<C3> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C3> c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1<C3>, IsInvalid) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C4>, IsInvalid) (Syntax: 'c2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C2<C4>' to 'I1<C3>'. An explicit conversion exists (are you missing a cast?)
                //         I1<C3> /*<bind>*/c1 = c2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c2").WithArguments("C2<C4>", "I1<C3>").WithLocation(11, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1<C3> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1<C3> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C3> c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1<C3>) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C4>) (Syntax: 'c2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C4> c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1<C4>, IsInvalid) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C3>, IsInvalid) (Syntax: 'c2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C2<C3>' to 'I1<C4>'. An explicit conversion exists (are you missing a cast?)
                //         I1<C4> /*<bind>*/c1 = c2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c2").WithArguments("C2<C3>", "I1<C4>").WithLocation(11, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'IList<strin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'IList<strin ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<System.String> list
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<System.String>) (Syntax: 'new List<string>()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.String>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.String>) (Syntax: 'new List<string>()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T, IsInvalid) (Syntax: 'new T()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'T' to 'C1'
                //         C1 /*<bind>*/c1 = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new T()").WithArguments("T", "C1").WithLocation(7, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T, IsInvalid) (Syntax: 'new T()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'T' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new T()").WithArguments("T", "I1").WithLocation(11, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'U /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'U /*<bind>* ... *</bind>*/;')
    Variables: Local_1: U u
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: U) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'U /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'U /*<bind>* ... *</bind>*/;')
    Variables: Local_1: U u
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: U, IsInvalid) (Syntax: 'new T()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T, IsInvalid) (Syntax: 'new T()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'T' to 'U'
                //         U /*<bind>*/u = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new T()").WithArguments("T", "U").WithLocation(12, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'T /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'T /*<bind>* ... *</bind>*/;')
    Variables: Local_1: T t
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T, Constant: null) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 't' is assigned but its value is never used
                //         T /*<bind>*/t = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t").WithArguments("t").WithLocation(11, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'T /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'T /*<bind>* ... *</bind>*/;')
    Variables: Local_1: T t
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T, IsInvalid) (Syntax: 'null')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0403: Cannot convert null to type parameter 'T' because it could be a non-nullable value type. Consider using 'default(T)' instead.
                //         T /*<bind>*/t = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T").WithLocation(11, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'dynamic /*< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'dynamic /*< ... *</bind>*/;')
    Variables: Local_1: dynamic d
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'ValueType / ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'ValueType / ... *</bind>*/;')
    Variables: Local_1: System.ValueType v1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.ValueType) (Syntax: 'new S1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: S1..ctor()) (OperationKind.ObjectCreationExpression, Type: S1) (Syntax: 'new S1()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'ValueType / ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'ValueType / ... *</bind>*/;')
    Variables: Local_1: System.ValueType v1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.ValueType, IsInvalid) (Syntax: 'new C1()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1, IsInvalid) (Syntax: 'new C1()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C1' to 'System.ValueType'
                //         ValueType /*<bind>*/v1 = new C1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C1()").WithArguments("C1", "System.ValueType").WithLocation(8, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'new S1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: S1..ctor()) (OperationKind.ObjectCreationExpression, Type: S1) (Syntax: 'new S1()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'new S1()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: S1..ctor()) (OperationKind.ObjectCreationExpression, Type: S1, IsInvalid) (Syntax: 'new S1()')
            Arguments(0)
            Initializer: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1' to 'I1'
                //         I1 /*<bind>*/i1 = new S1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S1()").WithArguments("S1", "I1").WithLocation(10, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1) (Syntax: 's1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: S1?) (Syntax: 's1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 's1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: S1?, IsInvalid) (Syntax: 's1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1?' to 'I1'
                //         I1 /*<bind>*/i1 = s1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("S1?", "I1").WithLocation(11, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Enum /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Enum /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Enum e
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Enum) (Syntax: 'E1.E')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: E1.E (Static) (OperationKind.FieldReferenceExpression, Type: E1, Constant: 0) (Syntax: 'E1.E')
            Instance Receiver: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Enum e
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Enum, IsInvalid) (Syntax: '1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'System.Enum'
                //         Enum /*<bind>*/e = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "System.Enum").WithLocation(13, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'string /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'string /*<b ... *</bind>*/;')
    Variables: Local_1: System.String s1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'd1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'd1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'const sbyte ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'const sbyte ... *</bind>*/;')
    Variables: Local_1: System.SByte s1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.SByte, Constant: 1) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(7, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                    AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'const sbyte ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'const sbyte ... *</bind>*/;')
    Variables: Local_1: System.SByte s1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.SByte, IsInvalid) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 4096, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0031: Constant value '4096' cannot be converted to a 'sbyte'
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "i1").WithArguments("4096", "sbyte").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'const sbyte ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'const sbyte ... *</bind>*/;')
    Variables: Local_1: System.SByte s1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.SByte, IsInvalid) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'sbyte'. An explicit conversion exists (are you missing a cast?)
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int", "sbyte").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: C2 C2.op_Implicit(C1 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: 'this')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C2.op_Implicit(C1 c1))
        Operand: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C1) (Syntax: 'this')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: C2 C2.op_Implicit(System.Int64 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C2.op_Implicit(System.Int64 c1))
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64) (Syntax: 'i1')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier()
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: C2 C2.op_Implicit(System.Int64 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: '(int)this')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C2.op_Implicit(System.Int64 c1))
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64) (Syntax: '(int)this')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperatorMethod: System.Int32 C1.op_Implicit(C1 c1)) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)this')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C1.op_Implicit(C1 c1))
                Operand: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C1) (Syntax: 'this')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C2, IsInvalid) (Syntax: 'this')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C1, IsInvalid) (Syntax: 'this')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C3 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C3 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C3 c3
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: C3 C3.op_Implicit(C2 c2)) (OperationKind.ConversionExpression, Type: C3) (Syntax: 'this')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C3 C3.op_Implicit(C2 c2))
        Operand: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C2) (Syntax: 'this')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'void* /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'void* /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Void* v1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Void*) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'void* /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'void* /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Void* v1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Void*) (Syntax: 'i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int* /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int* /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int32* i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32*, IsInvalid) (Syntax: 'v1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: v1 (OperationKind.LocalReferenceExpression, Type: System.Void*, IsInvalid) (Syntax: 'v1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'void*' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         int* /*<bind>*/i1 = v1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "v1").WithArguments("void*", "int*").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'void* /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'void* /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Void* v1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Void*, IsInvalid) (Syntax: '0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'void*'. An explicit conversion exists (are you missing a cast?)
                //         void* /*<bind>*/v1 = 0/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "0").WithArguments("int", "void*").WithLocation(8, 30),
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Expression< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Expression< ... *</bind>*/;')
    Variables: Local_1: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>> exp
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>>) (Syntax: 'num => num < 5')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'num => num < 5')
            IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'num < 5')
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'num < 5')
                ReturnedValue: IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num < 5')
                    Left: IParameterReferenceExpression: num (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'num')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
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

            // Due to https://github.com/dotnet/roslyn/issues/20291, we cannot verify that the types of the ioperation tree and the sematic model
            // match, as they do not actually match.
            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
    Variables: Local_1: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>> exp
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>>, IsInvalid) (Syntax: 'num =>/*</bind>*/')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'num =>/*</bind>*/')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '')
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: '')
                ReturnedValue: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
                    Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         Expression<Func<int, bool>> /*<bind>*/exp = num =>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(9, 70)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return i;')
  ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'return f;')
  ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'f')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: System.Single, IsInvalid) (Syntax: 'f')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         /*<bind>*/return f;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "f").WithArguments("float", "int").WithLocation(7, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
                /*<bind>*/object o = null/*</bind>*/;
            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object o =  ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object o =  ... *</bind>*/;')
    Variables: Local_1: System.Object o
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'o' is assigned but its value is never used
                //                 /*<bind>*/object o = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "o").WithArguments("o").WithLocation(10, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Action<stri ... jectAction;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Action<stri ... jectAction;')
    Variables: Local_1: System.Action<System.String> stringAction
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action<System.String>) (Syntax: 'objectAction')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: objectAction (OperationKind.LocalReferenceExpression, Type: System.Action<System.Object>) (Syntax: 'objectAction')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Action<int> ... jectAction;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Action<int> ... jectAction;')
    Variables: Local_1: System.Action<System.Int32> intAction
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action<System.Int32>, IsInvalid) (Syntax: 'objectAction')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: objectAction (OperationKind.LocalReferenceExpression, Type: System.Action<System.Object>, IsInvalid) (Syntax: 'objectAction')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'System.Action<object>' to 'System.Action<int>'
                //         /*<bind>*/Action<int> intAction = objectAction;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "objectAction").WithArguments("System.Action<object>", "System.Action<int>").WithLocation(8, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: '(int)1')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //         int /*<bind>*/i = (int)1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'long /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'long /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int64 i
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 1) (Syntax: '(int)1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: '(int)1')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i' is assigned but its value is never used
                //         long /*<bind>*/i = (int)1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
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
IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: '(int)1.0')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '(float)1.0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Single, Constant: 1, IsInvalid) (Syntax: '(float)1.0')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1, IsInvalid) (Syntax: '1.0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i = (float)1.0/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(float)1.0").WithArguments("float", "int").WithLocation(6, 27)
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'long /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'long /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int64 i
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, IsInvalid) (Syntax: '(int)/*</bind>*/')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '(int)/*</bind>*/')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'E1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'E1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: E1 e1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E1, Constant: 1) (Syntax: '(E1)1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 0) (Syntax: '(int)E1.One')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: E1.One (Static) (OperationKind.FieldReferenceExpression, Type: E1, Constant: 0) (Syntax: 'E1.One')
            Instance Receiver: null
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'E2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'E2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: E2 e2
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E2, Constant: 0) (Syntax: '(E2)E1.One')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: E1.One (Static) (OperationKind.FieldReferenceExpression, Type: E1, Constant: 0) (Syntax: 'E1.One')
            Instance Receiver: null
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'E2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'E2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: E2 e2
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E2, Constant: null, IsInvalid) (Syntax: '(E2)E1.One')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: E1.One (Static) (OperationKind.FieldReferenceExpression, Type: E1, Constant: 1000, IsInvalid) (Syntax: 'E1.One')
            Instance Receiver: null
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int? /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int? /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int32? i
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: '(int?)l')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: l (OperationKind.LocalReferenceExpression, Type: System.Int64?) (Syntax: 'l')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)l')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: l (OperationKind.LocalReferenceExpression, Type: System.Int64?) (Syntax: 'l')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'string /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'string /*<b ... *</bind>*/;')
    Variables: Local_1: System.String s
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: '(string)o')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'string /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'string /*<b ... *</bind>*/;')
    Variables: Local_1: System.String s
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: '(string)d')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C2) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: C1) (Syntax: 'c1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C2, IsInvalid) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: C1, IsInvalid) (Syntax: 'c1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1) (Syntax: '(C1)i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: I1) (Syntax: 'i1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1) (Syntax: '(C1)i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: I1) (Syntax: 'i1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: '(C1)i1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: I1, IsInvalid) (Syntax: 'i1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I2 i2
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I2) (Syntax: '(I2)i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: I1) (Syntax: 'i1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I2 i2
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I2, IsInvalid) (Syntax: '(I2)()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2[] /*<bin ... *</bind>*/;')
    Variables: Local_1: C2[] c2arr
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C2[]) (Syntax: '(C2[])c1arr')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1arr (OperationKind.LocalReferenceExpression, Type: C1[]) (Syntax: 'c1arr')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C2[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C2[] /*<bin ... *</bind>*/;')
    Variables: Local_1: C2[] c2arr
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C2[], IsInvalid) (Syntax: '(C2[])c1arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1arr (OperationKind.LocalReferenceExpression, Type: C1[], IsInvalid) (Syntax: 'c1arr')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1[][] /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1[][] /*<b ... *</bind>*/;')
    Variables: Local_1: C1[][] c2arr
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1[][], IsInvalid) (Syntax: '(C1[][])c1arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1arr (OperationKind.LocalReferenceExpression, Type: C1[], IsInvalid) (Syntax: 'c1arr')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
    Variables: Local_1: C1[] c2arr
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1[]) (Syntax: '(C1[])c1arr')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1arr (OperationKind.LocalReferenceExpression, Type: System.Array) (Syntax: 'c1arr')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'IList<C1> / ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'IList<C1> / ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<C1> c1list
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<C1>) (Syntax: '(IList<C1>)c1arr')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1arr (OperationKind.LocalReferenceExpression, Type: C1[]) (Syntax: 'c1arr')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'IList<C1> / ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'IList<C1> / ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<C1> c1list
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<C1>, IsInvalid) (Syntax: '(IList<C1>)c1arr')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1arr (OperationKind.LocalReferenceExpression, Type: C1[][], IsInvalid) (Syntax: 'c1arr')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
    Variables: Local_1: C1[] c1arr
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1[]) (Syntax: '(C1[])c1List')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1List (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.IList<C1>) (Syntax: 'c1List')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1[][] /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1[][] /*<b ... *</bind>*/;')
    Variables: Local_1: C1[][] c1arr
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C1[][], IsInvalid) (Syntax: '(C1[][])c1List')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1List (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.IList<C1>, IsInvalid) (Syntax: 'c1List')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Action /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Action /*<b ... *</bind>*/;')
    Variables: Local_1: System.Action a
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action) (Syntax: '(Action)d')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Delegate) (Syntax: 'd')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C4> c1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1<C4>) (Syntax: '(I1<C4>)c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C3>) (Syntax: 'c2')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)o')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)d')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)v')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: v (OperationKind.LocalReferenceExpression, Type: System.ValueType) (Syntax: 'v')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'E1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'E1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: E1 e1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E1) (Syntax: '(E1)e')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Enum) (Syntax: 'e')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'E1? /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'E1? /*<bind ... *</bind>*/;')
    Variables: Local_1: E1? e1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E1?) (Syntax: '(E1?)e')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Enum) (Syntax: 'e')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int? /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int? /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int32? e1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32?, IsInvalid) (Syntax: '(E1?)e')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E1?, IsInvalid) (Syntax: '(E1?)e')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Enum, IsInvalid) (Syntax: 'e')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'S1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'S1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: S1 s1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: S1) (Syntax: '(S1)i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: I1) (Syntax: 'i')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'S1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'S1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: S1 s1
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: S1, IsInvalid) (Syntax: '(S1)i')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: I1, IsInvalid) (Syntax: 'i')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'IList<S1> / ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'IList<S1> / ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<S1> s1List
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<S1>) (Syntax: '(IList<S1>)i1List')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i1List (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.IList<I1>) (Syntax: 'i1List')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'T /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'T /*<bind>* ... *</bind>*/;')
    Variables: Local_1: T t
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T) (Syntax: '(T)u')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: u (OperationKind.ParameterReferenceExpression, Type: U) (Syntax: 'u')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'T /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'T /*<bind>* ... *</bind>*/;')
    Variables: Local_1: T t
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T, IsInvalid) (Syntax: '(T)u')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: u (OperationKind.ParameterReferenceExpression, Type: U, IsInvalid) (Syntax: 'u')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'T /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'T /*<bind>* ... *</bind>*/;')
    Variables: Local_1: T t
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T) (Syntax: '(T)i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: I1) (Syntax: 'i')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1) (Syntax: '(I1)t')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 't')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperatorMethod: C2 C1.op_Implicit(C1 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C1.op_Implicit(C1 c1))
        Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: C1) (Syntax: 'c1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperatorMethod: C2 C1.op_Explicit(C1 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C1.op_Explicit(C1 c1))
        Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: C1) (Syntax: 'c1')
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: I1) (Syntax: '(C2)c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperatorMethod: C2 C1.op_Explicit(C1 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: '(C2)c1')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C2 C1.op_Explicit(C1 c1))
            Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: C1) (Syntax: 'c1')
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
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return (int)1.0;')
  ReturnedValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: '(int)1.0')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
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
IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'return (int)"""";')
  ReturnedValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '(int)""""')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """", IsInvalid) (Syntax: '""""')
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
IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'return (int);')
  ReturnedValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '(int)')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
                /*<bind>*/object o = (object)null/*</bind>*/;
            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object o =  ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object o =  ... *</bind>*/;')
    Variables: Local_1: System.Object o
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: '(object)null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'o' is assigned but its value is never used
                //                 /*<bind>*/object o = (object)null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "o").WithArguments("o").WithLocation(10, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action<System.String>) (Syntax: '(Action<str ... bjectAction')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: ILocalReferenceExpression: objectAction (OperationKind.LocalReferenceExpression, Type: System.Action<System.Object>) (Syntax: 'objectAction')
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
IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action<System.Int32>, IsInvalid) (Syntax: '(Action<int ... bjectAction')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: ILocalReferenceExpression: objectAction (OperationKind.LocalReferenceExpression, Type: System.Action<System.Object>, IsInvalid) (Syntax: 'objectAction')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'System.Action<object>' to 'System.Action<int>'
                //         Action<int> intAction = /*<bind>*/(Action<int>)objectAction/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Action<int>)objectAction").WithArguments("System.Action<object>", "System.Action<int>").WithLocation(8, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        private class ExpectedSymbolVerifier
        {
            public static SyntaxNode VariableDeclaratorSelector(SyntaxNode syntaxNode) =>
                ((VariableDeclaratorSyntax)syntaxNode).Initializer.Value;

            public static SyntaxNode IdentitySelector(SyntaxNode syntaxNode) => syntaxNode;

            public static SyntaxNode ReturnStatementSelector(SyntaxNode syntaxNode) => ((ReturnStatementSyntax)syntaxNode).Expression;

            public static IOperation IVariableDeclarationStatementSelector(IOperation operation) =>
                ((IVariableDeclarationStatement)operation).Declarations.Single().Initializer;

            public static IOperation IReturnDeclarationStatementSelector(IOperation operation) =>
                ((IReturnStatement)operation).ReturnedValue;

            public static IOperation NestedConversionChildSelector(IOperation operation) =>
                ConversionOrDelegateChildSelector(ConversionOrDelegateChildSelector(operation));

            private static IOperation ConversionOrDelegateChildSelector(IOperation operation)
            {
                if (operation.Kind == OperationKind.ConversionExpression)
                {
                    return ((IConversionExpression)operation).Operand;
                }
                else
                {
                    return ((IDelegateCreationExpression)operation).Target;
                }
            }

            public Func<IOperation, IConversionExpression> OperationSelector { get; set; }

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
                    case IVariableDeclarationStatement _:
                        return IVariableDeclarationStatementSelector(operation);
                    case IReturnStatement _:
                        return IReturnDeclarationStatementSelector(operation);
                    case IConversionExpression conv:
                        return conv;
                    default:
                        throw new ArgumentException($"Cannot handle arguments of type {operation.GetType()}");
                }
            }
        }
    }
}
