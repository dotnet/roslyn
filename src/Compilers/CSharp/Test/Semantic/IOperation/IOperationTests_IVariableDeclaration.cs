// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        #region Variable Declarations

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SingleVariableDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i1;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'int i1;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0168: The variable 'i1' is declared but never used
                //         /*<bind>*/int i1;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i1").WithArguments("i1").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SingleVariableDeclarationWithInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = 1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i1 = 1;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'int i1 = 1;')
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         /*<bind>*/int i1 = 1;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SingleVariableDeclarationWithInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = ;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int i1 = ;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int i1 = ;')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/int i1 = ;/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MultipleDeclarations()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1, i2;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i1, i2;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'i1')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration) (Syntax: 'i2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0168: The variable 'i1' is declared but never used
                //         /*<bind>*/int i1, i2;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i1").WithArguments("i1").WithLocation(6, 23),
                // CS0168: The variable 'i2' is declared but never used
                //         /*<bind>*/int i1, i2;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i2").WithArguments("i2").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MultipleDeclarationsWithInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = 2, i2 = 2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i1 = 2, ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'i1 = 2')
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration) (Syntax: 'i2 = 2')
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         /*<bind>*/int i1 = 2, i2 = 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 23),
                // CS0219: The variable 'i2' is assigned but its value is never used
                //         /*<bind>*/int i1 = 2, i2 = 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MultipleDeclarationsWithInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = , i2 = 2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int i1 = ,  ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i1 = ')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration) (Syntax: 'i2 = 2')
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ','
                //         /*<bind>*/int i1 = , i2 = 2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 28),
                // CS0219: The variable 'i2' is assigned but its value is never used
                //         /*<bind>*/int i1 = , i2 = 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(6, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void InvalidMultipleVariableDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i,/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i,/*</bind>*/;')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'i')
  IVariableDeclaration: System.Int32  (OperationKind.VariableDeclaration) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //         /*<bind>*/int i,/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(6, 36),
                // CS0168: The variable 'i' is declared but never used
                //         /*<bind>*/int i,/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i").WithArguments("i").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SingleVariableDeclarationExpressionInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i = GetInt()/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i = Get ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'int i = Get ... *</bind>*/;')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MutlipleVariableDeclarationsExpressionInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i = GetInt(), j = GetInt()/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i = Get ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'i = GetInt()')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration) (Syntax: 'j = GetInt()')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void SingleVariableDeclarationLocalReferenceInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 1;
        /*<bind>*/int i1 = i/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i1 = i/*</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'int i1 = i/*</bind>*/;')
    Initializer: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void MultipleDeclarationsLocalReferenceInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 1;
        /*<bind>*/int i1 = i, i2 = i1/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i1 = i, ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'i1 = i')
    Initializer: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration) (Syntax: 'i2 = i1')
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void InvalidArrayDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int[2, 3] a/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int[2, 3] a/*</bind>*/;')
  IVariableDeclaration: System.Int32[,] a (OperationKind.VariableDeclaration) (Syntax: 'int[2, 3] a/*</bind>*/;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[2, 3] a/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "2").WithLocation(6, 23),
                // CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[2, 3] a/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "3").WithLocation(6, 26),
                // CS0168: The variable 'a' is declared but never used
                //         /*<bind>*/int[2, 3] a/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "a").WithArguments("a").WithLocation(6, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void InvalidArrayMultipleDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int[2, 3] a, b/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int[2, 3] a ... *</bind>*/;')
  IVariableDeclaration: System.Int32[,] a (OperationKind.VariableDeclaration) (Syntax: 'a')
  IVariableDeclaration: System.Int32[,] b (OperationKind.VariableDeclaration) (Syntax: 'b')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[2, 3] a, b/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "2").WithLocation(6, 23),
                // CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[2, 3] a, b/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "3").WithLocation(6, 26),
                // CS0168: The variable 'a' is declared but never used
                //         /*<bind>*/int[2, 3] a, b/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "a").WithArguments("a").WithLocation(6, 29),
                // CS0168: The variable 'b' is declared but never used
                //         /*<bind>*/int[2, 3] a, b/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "b").WithArguments("b").WithLocation(6, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        #region Fixed Statements

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementDeclaration()
        {
            string source = @"
class Program
{
    int i1;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p = &reference.i1/*</bind>*/)
            {

            }
        }
    }
}
";


            string expectedOperationTree = @"
IVariableDeclaration: System.Int32* p (OperationKind.VariableDeclaration) (Syntax: 'p = &reference.i1')
  Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&reference.i1')
      IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'reference.i1')
        Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'reference')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementMultipleDeclaration()
        {
            string source = @"
class Program
{
    int i1, i2;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p1 = &reference.i1, p2 = &reference.i2/*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int* p1 = & ... eference.i2')
  IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration) (Syntax: 'p1 = &reference.i1')
    Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&reference.i1')
        IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'reference.i1')
            Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'reference')
    IVariableDeclaration: System.Int32* p2 (OperationKind.VariableDeclaration) (Syntax: 'p2 = &reference.i2')
    Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&reference.i2')
        IFieldReferenceExpression: System.Int32 Program.i2 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'reference.i2')
            Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'reference')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementInvalidAssignment()
        {
            string source = @"
class Program
{
    int i1;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p = /*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int* p = ')
  IVariableDeclaration: System.Int32* p (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'p = ')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32*, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //             fixed (/*<bind>*/int* p = /*</bind>*/)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(10, 50),
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9),
                // CS0169: The field 'Program.i1' is never used
                //     int i1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i1").WithArguments("Program.i1").WithLocation(4, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementMultipleDeclarationsInvalidInitializers()
        {
            string source = @"
class Program
{
    int i1, i2;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p1 = , p2 = /*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int* p1 = , p2 = ')
  IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'p1 = ')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32*, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  IVariableDeclaration: System.Int32* p2 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'p2 = ')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32*, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ','
                //             fixed (/*<bind>*/int* p1 = , p2 = /*</bind>*/)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(10, 40),
                // CS1525: Invalid expression term ')'
                //             fixed (/*<bind>*/int* p1 = , p2 = /*</bind>*/)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(10, 58),
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9),
                // CS0169: The field 'Program.i2' is never used
                //     int i1, i2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i2").WithArguments("Program.i2").WithLocation(4, 13),
                // CS0169: The field 'Program.i1' is never used
                //     int i1, i2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i1").WithArguments("Program.i1").WithLocation(4, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementNoInitializer()
        {
            string source = @"
class Program
{
    int i1;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p/*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int* p')
  IVariableDeclaration: System.Int32* p (OperationKind.VariableDeclaration) (Syntax: 'p')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9),
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //             fixed (/*<bind>*/int* p/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "p").WithLocation(10, 35),
                // CS0169: The field 'Program.i1' is never used
                //     int i1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i1").WithArguments("Program.i1").WithLocation(4, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementMultipleDeclarationsNoInitializers()
        {
            string source = @"
class Program
{
    int i1, i2;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p1, p2/*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int* p1, p2')
  IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration) (Syntax: 'p1')
  IVariableDeclaration: System.Int32* p2 (OperationKind.VariableDeclaration) (Syntax: 'p2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9),
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //             fixed (/*<bind>*/int* p1, p2/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "p1").WithLocation(10, 35),
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //             fixed (/*<bind>*/int* p1, p2/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "p2").WithLocation(10, 39),
                // CS0169: The field 'Program.i2' is never used
                //     int i1, i2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i2").WithArguments("Program.i2").WithLocation(4, 13),
                // CS0169: The field 'Program.i1' is never used
                //     int i1, i2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i1").WithArguments("Program.i1").WithLocation(4, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementInvalidMulipleDeclarations()
        {
            string source = @"
class Program
{
    int i1, i2;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p1 = &reference.i1,/*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int* p1 = &reference.i1,')
  IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration) (Syntax: 'p1 = &reference.i1')
    Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&reference.i1')
        IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'reference.i1')
            Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'reference')
  IVariableDeclaration: System.Int32*  (OperationKind.VariableDeclaration) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //             fixed (/*<bind>*/int* p1 = &reference.i1,/*</bind>*/)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(10, 65),
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9),
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //             fixed (/*<bind>*/int* p1 = &reference.i1,/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "").WithLocation(10, 65),
                // CS0169: The field 'Program.i2' is never used
                //     int i1, i2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i2").WithArguments("Program.i2").WithLocation(4, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        #region Using Statements

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementDeclaration()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = new Program()/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'Program p1  ... w Program()')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration) (Syntax: 'p1 = new Program()')
    Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program) (Syntax: 'new Program()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarations()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = new Program(), p2 = new Program()/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'Program p1  ... w Program()')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration) (Syntax: 'p1 = new Program()')
    Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program) (Syntax: 'new Program()')
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration) (Syntax: 'p2 = new Program()')
    Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program) (Syntax: 'new Program()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementInvalidInitializer()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 =/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Program p1 =')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'p1 =')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         using (/*<bind>*/Program p1 =/*</bind>*/)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarationsInvalidInitializers()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 =, p2 =/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Program p1 =, p2 =')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'p1 =')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'p2 =')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ','
                //         using (/*<bind>*/Program p1 =, p2 =/*</bind>*/)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(8, 38),
                // CS1525: Invalid expression term ')'
                //         using (/*<bind>*/Program p1 =, p2 =/*</bind>*/)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 55)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementNoInitializer()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Program p1')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration) (Syntax: 'p1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //         using (/*<bind>*/Program p1/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "p1").WithLocation(8, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarationsNoInitializers()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1, p2/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Program p1, p2')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration) (Syntax: 'p1')
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration) (Syntax: 'p2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //         using (/*<bind>*/Program p1, p2/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "p1").WithLocation(8, 34),
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //         using (/*<bind>*/Program p1, p2/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "p2").WithLocation(8, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementInvalidMultipleDeclarations()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = new Program(),/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Program p1  ...  Program(),')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration) (Syntax: 'p1 = new Program()')
    Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program) (Syntax: 'new Program()')
  IVariableDeclaration: Program  (OperationKind.VariableDeclaration) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //         using (/*<bind>*/Program p1 = new Program(),/*</bind>*/)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(8, 64),
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //         using (/*<bind>*/Program p1 = new Program(),/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "").WithLocation(8, 64)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementExpressionInitializer()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = GetProgram()/*</bind>*/)
        {
        }
    }

    static Program GetProgram() => new Program();

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'Program p1  ... etProgram()')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration) (Syntax: 'p1 = GetProgram()')
    Initializer: IInvocationExpression (static Program Program.GetProgram()) (OperationKind.InvocationExpression, Type: Program) (Syntax: 'GetProgram()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarationsExpressionInitializers()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = GetProgram(), p2 = GetProgram()/*</bind>*/)
        {
        }
    }

    static Program GetProgram() => new Program();

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'Program p1  ... etProgram()')
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration) (Syntax: 'p1 = GetProgram()')
    Initializer: IInvocationExpression (static Program Program.GetProgram()) (OperationKind.InvocationExpression, Type: Program) (Syntax: 'GetProgram()')
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration) (Syntax: 'p2 = GetProgram()')
    Initializer: IInvocationExpression (static Program Program.GetProgram()) (OperationKind.InvocationExpression, Type: Program) (Syntax: 'GetProgram()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementLocalReferenceInitializer()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        Program p1 = new Program();
        using (/*<bind>*/Program p2 = p1/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'Program p2 = p1')
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration) (Syntax: 'p2 = p1')
    Initializer: ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'p1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarationsLocalReferenceInitializers()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        Program p1 = new Program();
        using (/*<bind>*/Program p2 = p1, p3 = p1/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'Program p2 = p1, p3 = p1')
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration) (Syntax: 'p2 = p1')
    Initializer: ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'p1')
  IVariableDeclaration: Program p3 (OperationKind.VariableDeclaration) (Syntax: 'p3 = p1')
    Initializer: ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'p1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        #region For Loops

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i = 0/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
    Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarations()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i = 0, j = 0/*</bind>*/; i < 0; i++)
        {
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i = 0, j = 0')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
    Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration) (Syntax: 'j = 0')
    Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'j' is assigned but its value is never used
                //         for (/*<bind>*/int i = 0, j = 0/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j").WithLocation(6, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i =/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'i =')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i =')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         for (/*<bind>*/int i =/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarationsInvalidInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i =, j =/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int i =, j =')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i =')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'j =')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ','
                //         for (/*<bind>*/int i =, j =/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 31),
                // CS1525: Invalid expression term ';'
                //         for (/*<bind>*/int i =, j =/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 47)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopNoInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'i')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'i')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'i'
                //         for (/*<bind>*/int i/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarationsNoInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i, j/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i, j')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'i')
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration) (Syntax: 'j')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'i'
                //         for (/*<bind>*/int i, j/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(6, 45),
                // CS0168: The variable 'j' is declared but never used
                //         for (/*<bind>*/int i, j/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "j").WithArguments("j").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopInvalidMultipleDeclarations()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i =,/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int i =,')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i =')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  IVariableDeclaration: System.Int32  (OperationKind.VariableDeclaration) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ','
                //         for (/*<bind>*/int i =,/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 31),
                // CS1001: Identifier expected
                //         for (/*<bind>*/int i =,/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(6, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopExpressionInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i = GetInt()/*</bind>*/; i < 0; i++)
        {

        }
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = GetInt()')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'i = GetInt()')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarationsExpressionInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i = GetInt(), j = GetInt()/*</bind>*/; i < 0; i++)
        {

        }
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i = Get ...  = GetInt()')
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration) (Syntax: 'i = GetInt()')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration) (Syntax: 'j = GetInt()')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }


        #endregion

        #region Const Local Declarations

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1 = 1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'const int i1 = 1;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'const int i1 = 1;')
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         /*<bind>*/const int i1 = 1;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarations()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1 = 1, i2 = 2;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'const int i ...  1, i2 = 2;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'i1 = 1')
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration) (Syntax: 'i2 = 2')
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         /*<bind>*/const int i1 = 1, i2 = 2;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(9, 29),
                // CS0219: The variable 'i2' is assigned but its value is never used
                //         /*<bind>*/const int i1 = 1, i2 = 2;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(9, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1 = /*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'const /*<bi ... *</bind>*/;')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         const /*<bind>*/int i1 = /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsInvalidInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1 = , i2 = /*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i1 = ')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i2 = /*</bind>*/')
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ','
                //         const /*<bind>*/int i1 = , i2 = /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 34),
                // CS1525: Invalid expression term ';'
                //         const /*<bind>*/int i1 = , i2 = /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 52)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationNoInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'const /*<bi ... *</bind>*/;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0145: A const field requires a value to be provided
                //         const /*<bind>*/int i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "i1").WithLocation(6, 29),
                // CS0168: The variable 'i1' is declared but never used
                //         const /*<bind>*/int i1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i1").WithArguments("i1").WithLocation(6, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMutlipleDeclarationsNoInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1, i2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'i1')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration) (Syntax: 'i2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0145: A const field requires a value to be provided
                //         const /*<bind>*/int i1, i2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "i1").WithLocation(6, 29),
                // CS0145: A const field requires a value to be provided
                //         const /*<bind>*/int i1, i2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "i2").WithLocation(6, 33),
                // CS0168: The variable 'i1' is declared but never used
                //         const /*<bind>*/int i1, i2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i1").WithArguments("i1").WithLocation(6, 29),
                // CS0168: The variable 'i2' is declared but never used
                //         const /*<bind>*/int i1, i2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i2").WithArguments("i2").WithLocation(6, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalInvalidMultipleDeclarations()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1,/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'i1')
  IVariableDeclaration: System.Int32  (OperationKind.VariableDeclaration) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0145: A const field requires a value to be provided
                //         const /*<bind>*/int i1,/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "i1").WithLocation(6, 29),
                // CS1001: Identifier expected
                //         const /*<bind>*/int i1,/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(6, 43),
                // CS0145: A const field requires a value to be provided
                //         const /*<bind>*/int i1,/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, ";").WithLocation(6, 43),
                // CS0168: The variable 'i1' is declared but never used
                //         const /*<bind>*/int i1,/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i1").WithArguments("i1").WithLocation(6, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationExpressionInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1 = GetInt()/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'const /*<bi ... *</bind>*/;')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0133: The expression being assigned to 'i1' must be constant
                //         const /*<bind>*/int i1 = GetInt()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "GetInt()").WithArguments("i1").WithLocation(6, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsExpressionInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1 = GetInt(), i2 = GetInt()/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'i1 = GetInt()')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration) (Syntax: 'i2 = GetInt()')
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'GetInt()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0133: The expression being assigned to 'i1' must be constant
                //         const /*<bind>*/int i1 = GetInt(), i2 = GetInt()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "GetInt()").WithArguments("i1").WithLocation(6, 34),
                // CS0133: The expression being assigned to 'i2' must be constant
                //         const /*<bind>*/int i1 = GetInt(), i2 = GetInt()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "GetInt()").WithArguments("i2").WithLocation(6, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationLocalReferenceInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const int i = 1;
        const /*<bind>*/int i1 = i/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'const /*<bi ... *</bind>*/;')
    Initializer: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         const /*<bind>*/int i1 = i/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(7, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsLocalReferenceInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const int i = 1;
        const /*<bind>*/int i1 = i, i2 = i1/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'const /*<bi ... *</bind>*/;')
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration) (Syntax: 'i1 = i')
    Initializer: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i')
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration) (Syntax: 'i2 = i1')
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i2' is assigned but its value is never used
                //         const /*<bind>*/int i1 = i, i2 = i1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(7, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion
    }
}
