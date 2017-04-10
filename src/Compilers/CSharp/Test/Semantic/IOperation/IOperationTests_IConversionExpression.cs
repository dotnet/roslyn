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
        public void IdentityConversion()
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
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: dynamic d1 (OperationKind.VariableDeclaration)
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: dynamic)
        ILocalReferenceExpression: o1 (OperationKind.LocalReferenceExpression, Type: System.Object)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitNumericConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        float f1 = 1.0f;
        /*<bind>*/double d1 = f1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'double d1 = ... *</bind>*/;')
  IVariableDeclaration: System.Double d1 (OperationKind.VariableDeclaration) (Syntax: 'double d1 = ... *</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double) (Syntax: 'f1')
        ILocalReferenceExpression: f1 (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'f1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitInvalidNumericConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        float f1 = 1.0f;
        /*<bind>*/int i1 = f1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int i1 = f1/*</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int i1 = f1/*</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'f1')
        ILocalReferenceExpression: f1 (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'f1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         /*<bind>*/int i1 = f1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "f1").WithArguments("float", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitNumericConversionNoInitializer()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 =/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int i1 =/*</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int i1 =/*</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/int i1 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(8, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitEnumConversion()
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
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        /*<bind>*/Enum1 e1 = 0/*</bind>*/;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'Enum1 e1 = 0/*</bind>*/;')
  IVariableDeclaration: Program.Enum1 e1 (OperationKind.VariableDeclaration) (Syntax: 'Enum1 e1 = 0/*</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Program.Enum1, Constant: 0) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplictEnumConversionInvalid()
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
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        /*<bind>*/Enum1 e1 = 1/*</bind>*/;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 e1 = 1/*</bind>*/;')
  IVariableDeclaration: Program.Enum1 e1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 e1 = 1/*</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Program.Enum1, IsInvalid) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'Program.Enum1'. An explicit conversion exists (are you missing a cast?)
                //         /*<bind>*/Enum1 e1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "Program.Enum1").WithLocation(11, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitEnumConversionNoInitializer()
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
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        /*<bind>*/Enum1 e1 =/*</bind>*/;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 e1 =/*</bind>*/;')
  IVariableDeclaration: Program.Enum1 e1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 e1 =/*</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program.Enum1, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/Enum1 e1 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(11, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitThrowExpressionConversion()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = /*<bind>*/new object() ?? throw new Exception()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
INullCoalescingExpression (OperationKind.NullCoalescingExpression, Type: System.Object) (Syntax: 'new object( ... Exception()')
  Left: IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
  Right: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'throw new Exception()')
      IOperation:  (OperationKind.None) (Syntax: 'throw new Exception()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitNullToNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        string /*<bind>*/s1 = null/*</bind>*/;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.String s1 (OperationKind.VariableDeclaration)
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null)
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitNullToNonNullableConversionInvalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int /*<bind>*/i1 = null/*</bind>*/;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         int /*<bind>*/i1 = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitThrowConversion()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = /*<bind>*/new object() ?? throw new Exception()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
INullCoalescingExpression (OperationKind.NullCoalescingExpression, Type: System.Object) (Syntax: 'new object( ... Exception()')
  Left: IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
  Right: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'throw new Exception()')
      IOperation:  (OperationKind.None) (Syntax: 'throw new Exception()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitThrowConversionInvalidSyntax()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o /*<bind>*/= throw new Exception()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'object o /* ... *</bind>*/;')
  IVariableDeclaration: System.Object o (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'object o /* ... *</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'throw new Exception()')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'throw new Exception()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8115: A throw expression is not allowed in this context.
                //         object o /*<bind>*/= throw new Exception()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(8, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitConstantToNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int? i1 = 1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int? i1 = 1/*</bind>*/;')
  IVariableDeclaration: System.Int32? i1 (OperationKind.VariableDeclaration) (Syntax: 'int? i1 = 1/*</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         /*<bind>*/int? i1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitNullableToNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int? i1 = 1;
        /*<bind>*/long? l1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'long? l1 =  ... *</bind>*/;')
  IVariableDeclaration: System.Int64? l1 (OperationKind.VariableDeclaration) (Syntax: 'long? l1 =  ... *</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int64?) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitNonNullableToNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i1 = 1;
        /*<bind>*/int? i2 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int? i2 = i1/*</bind>*/;')
  IVariableDeclaration: System.Int32? i2 (OperationKind.VariableDeclaration) (Syntax: 'int? i2 = i1/*</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ImplicitNullableToNonNullableInvalidConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int? i1 = 1;
        /*<bind>*/int i2 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int i2 = i1/*</bind>*/;')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int i2 = i1/*</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int?' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         /*<bind>*/int i2 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int?", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion
    }
}
