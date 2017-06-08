using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Test list drawn from Microsoft.CodeAnalysis.CSharp.ConversionKind
    public partial class IOperationTests : SemanticModelTestBase
    {
        #region Implicit Conversions

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: 'o1')
        ILocalReferenceExpression: o1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        /// <summary>
        /// This test documents the fact that there is no IConversionExpression between two objects of the same type.
        /// </summary>
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
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double) (Syntax: 'f1')
        ILocalReferenceExpression: f1 (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'f1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'f1')
        ILocalReferenceExpression: f1 (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'f1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i1 = f1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "f1").WithArguments("float", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
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

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_ZeroToEnum()
        {
            string source = @"
class Program
{
    enum Enum1
    {
        Option1, Option2
    }
    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 = 0/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.Enum1 e1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Program.Enum1, Constant: 0) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'e1' is assigned but its value is never used
                //         Enum1 /*<bind>*/e1 = 0/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "e1").WithArguments("e1").WithLocation(10, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_IntToEnum_Invalid()
        {
            string source = @"
class Program
{
    enum Enum1
    {
        Option1, Option2
    }
    static void Main(string[] args)
    {
        int i1 = 1;
        Enum1 /*<bind>*/e1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.Enum1 e1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Program.Enum1, IsInvalid) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'Program.Enum1'. An explicit conversion exists (are you missing a cast?)
                //         Enum1 /*<bind>*/e1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int", "Program.Enum1").WithLocation(11, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_OneToEnum_Invalid()
        {
            string source = @"
class Program
{
    enum Enum1
    {
        Option1, Option2
    }
    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 = 1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.Enum1 e1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Program.Enum1, IsInvalid) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'Program.Enum1'. An explicit conversion exists (are you missing a cast?)
                //         Enum1 /*<bind>*/e1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "Program.Enum1").WithLocation(10, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_NoInitalizer_Invalid()
        {
            string source = @"
class Program
{
    enum Enum1
    {
        Option1, Option2
    }
    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 =/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.Enum1 e1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program.Enum1, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         Enum1 /*<bind>*/e1 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(10, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: INullCoalescingExpression (OperationKind.NullCoalescingExpression, Type: System.Object) (Syntax: 'new object( ... Exception()')
        Left: IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
        Right: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'throw new Exception()')
            IOperation:  (OperationKind.None) (Syntax: 'throw new Exception()')
              Children(1): IObjectCreationExpression (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new Exception()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_NullToNullableConversion()
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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         string /*<bind>*/s1 = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         int /*<bind>*/i1 = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         int? /*<bind>*/i1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int64?) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int?' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i2 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int?", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.IFormattable) (Syntax: '$""{1}""')
        IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""{1}""')
          Parts(1): IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
              Expression: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: C1) (Syntax: 'new C2()')
        IObjectCreationExpression (Constructor: C2..ctor()) (OperationKind.ObjectCreationExpression, Type: C2) (Syntax: 'new C2()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new C2()')
        IObjectCreationExpression (Constructor: C2..ctor()) (OperationKind.ObjectCreationExpression, Type: C2) (Syntax: 'new C2()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2' to 'C1'
                //         C1 /*<bind>*/c1 = new C2()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C2()").WithArguments("C2", "C1").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new/*</bind>*/')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'new/*</bind>*/')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1031: Type expected
                //         C1 /*<bind>*/c1 = new/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(8, 41),
                // CS1526: A new expression requires (), [], or {} after type
                //         C1 /*<bind>*/c1 = new/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadNewExpr, ";").WithLocation(8, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C1' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = new C1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new C1()").WithArguments("C1", "I1").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceIntefaceToClassConversion_Invalid()
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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new I1()')
        IInvalidExpression (OperationKind.InvalidExpression, Type: I1, IsInvalid) (Syntax: 'new I1()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0144: Cannot create an instance of the abstract class or interface 'I1'
                //         C1 /*<bind>*/i1 = new I1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new I1()").WithArguments("I1").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'i2')
        ILocalReferenceExpression: i2 (OperationKind.LocalReferenceExpression, Type: I2) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'i2')
        ILocalReferenceExpression: i2 (OperationKind.LocalReferenceExpression, Type: I2) (Syntax: 'i2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'I2' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = i2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i2").WithArguments("I2", "I1").WithLocation(17, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: C1[]) (Syntax: 'c2arr')
        ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[]) (Syntax: 'c2arr')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1[][], IsInvalid) (Syntax: 'c2arr')
        ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[]) (Syntax: 'c2arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2[]' to 'C1[][]'
                //         C1[][] /*<bind>*/c1arr = c2arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2arr").WithArguments("C2[]", "C1[][]").WithLocation(9, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1[], IsInvalid) (Syntax: 'c2arr')
        ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[]) (Syntax: 'c2arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2[]' to 'C1[]'
                //         C1[] /*<bind>*/c1arr = c2arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2arr").WithArguments("C2[]", "C1[]").WithLocation(9, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: I1[], IsInvalid) (Syntax: 'new S1[10]')
        IArrayCreationExpression (Element Type: S1) (OperationKind.ArrayCreationExpression, Type: S1[]) (Syntax: 'new S1[10]')
          Dimension Sizes(1): ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1[]' to 'I1[]'
                //         I1[] /*<bind>*/i1arr = new S1[10]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S1[10]").WithArguments("S1[]", "I1[]").WithLocation(8, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'new object[10]')
        IArrayCreationExpression (Element Type: System.Object) (OperationKind.ArrayCreationExpression, Type: System.Object[]) (Syntax: 'new object[10]')
          Dimension Sizes(1): ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'new int[10][]')
        IArrayCreationExpression (Element Type: System.Int32[]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][]) (Syntax: 'new int[10][]')
          Dimension Sizes(1): ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Array, IsInvalid) (Syntax: 'new object()')
        IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'System.Array'. An explicit conversion exists (are you missing a cast?)
                //         Array /*<bind>*/a1 = new object()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "System.Array").WithLocation(8, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<System.Int32>) (Syntax: 'new int[10]')
        IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new int[10]')
          Dimension Sizes(1): ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<System.Int32>, IsInvalid) (Syntax: 'new object()')
        IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'System.Collections.Generic.IList<int>'. An explicit conversion exists (are you missing a cast?)
                //         IList<int> /*<bind>*/a1 = new object()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "System.Collections.Generic.IList<int>").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Delegate) (Syntax: 'd1')
        ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: C1.DType) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Delegate, IsInvalid) (Syntax: 'd1()')
        IInvocationExpression (virtual void C1.DType.Invoke()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'd1()')
          Instance Receiver: ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: C1.DType) (Syntax: 'd1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'void' to 'System.Delegate'
                //         Delegate /*<bind>*/d2 = d1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d1()").WithArguments("void", "System.Delegate").WithLocation(10, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        /// <summary>
        /// This method is documenting the fact that, as of right now, there is no conversion expression here.
        /// Once https://github.com/dotnet/roslyn/issues/20095 is resolved, there will be, and this test should
        /// be updated.
        /// </summary>
        [Fact]
        public void ConversionExpression_Implicit_ReferenceMethodToDelegateConversion_NoConversion()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = M1/*</bind>*/;
    }
    void M1()
    { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IMethodBindingExpression: void Program.M1() (OperationKind.MethodBindingExpression, Type: Program.DType) (Syntax: 'M1')
        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        /// <summary>
        /// This method is documenting the fact that, as of right now, there is no conversion expression here.
        /// Once https://github.com/dotnet/roslyn/issues/20095 is resolved, there will be, and this test should
        /// be updated.
        /// </summary>
        [Fact]
        public void ConversionExpression_Implicit_ReferenceMethodToDelegateConversion_InvalidMismatchedTypes_NoConversion()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = M1/*</bind>*/;
    }

    string M1()
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IMethodBindingExpression: System.String Program.M1() (OperationKind.MethodBindingExpression, Type: Program.DType, IsInvalid) (Syntax: 'M1')
        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0407: 'string Program.M1()' has the wrong return type
                //         DType /*<bind>*/d1 = M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "string").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceMethodToDelegateConversion_InvalidIdentifier()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = M1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program.DType, IsInvalid) (Syntax: 'M1')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M1' does not exist in the current context
                //         DType /*<bind>*/d1 = M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceLambdaToDelegateConversion()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = () => { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Program.DType) (Syntax: '() => { }')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: Program.DType) (Syntax: '() => { }')
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion
    }
}
