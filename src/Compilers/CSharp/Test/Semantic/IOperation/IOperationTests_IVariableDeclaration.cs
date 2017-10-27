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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i1;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0168: The variable 'i1' is declared but never used
                //         /*<bind>*/int i1;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i1").WithArguments("i1").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i1 = 1;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1 = 1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1 = 1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         /*<bind>*/int i1 = 1;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int i1 = ;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = ')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1 = ')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/int i1 = ;/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i1, i2;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1, i2')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MultipleDeclarationsWithInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = 2, i2 = 2;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i1 = 2, i2 = 2;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1 = 2, i2 = 2')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1 = 2')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i2 = 2')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         /*<bind>*/int i1 = 2, i2 = 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 23),
                // CS0219: The variable 'i2' is assigned but its value is never used
                //         /*<bind>*/int i1 = 2, i2 = 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MultipleDeclarationsWithInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = , i2 = 2;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int i1 = , i2 = 2;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = , i2 = 2')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1 = ')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i2 = 2')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ','
                //         /*<bind>*/int i1 = , i2 = 2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 28),
                // CS0219: The variable 'i2' is assigned but its value is never used
                //         /*<bind>*/int i1 = , i2 = 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(6, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void InvalidMultipleVariableDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i,;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int i,;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i,')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i')
          Initializer: 
            null
        ISingleVariableDeclarationOperation (Symbol: System.Int32 ) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //         /*<bind>*/int i,/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(6, 25),
                // CS0168: The variable 'i' is declared but never used
                //         /*<bind>*/int i,/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i").WithArguments("i").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SingleVariableDeclarationExpressionInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i = GetInt();/*</bind>*/
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = GetInt();')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i = GetInt()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MutlipleVariableDeclarationsExpressionInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i = GetInt(), j = GetInt();/*</bind>*/
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = Get ... = GetInt();')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i = Get ...  = GetInt()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
        ISingleVariableDeclarationOperation (Symbol: System.Int32 j) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'j = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void SingleVariableDeclarationLocalReferenceInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 1;
        /*<bind>*/int i1 = i;/*</bind>*/
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i1 = i;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1 = i')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1 = i')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void MultipleDeclarationsLocalReferenceInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 1;
        /*<bind>*/int i1 = i, i2 = i1;/*</bind>*/
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i1 = i, i2 = i1;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1 = i, i2 = i1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1 = i')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i2 = i1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
              ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void InvalidArrayDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int[2, 3] a;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int[2, 3] a;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[2, 3] a')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32[,] a) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      null
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

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void InvalidArrayMultipleDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int[2, 3] a, b;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int[2, 3] a, b;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[2, 3] a, b')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32[,] a) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'a')
          Initializer: 
            null
        ISingleVariableDeclarationOperation (Symbol: System.Int32[,] b) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'b')
          Initializer: 
            null
    Initializer: 
      null
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

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestGetOperationForVariableInitializer()
        {
            string source = @"
class Test
{
    void M()
    {
        var x /*<bind>*/= 1/*</bind>*/;
        System.Console.WriteLine(x);
    }
}
";
            string expectedOperationTree = @"
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        #region Fixed Statements

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int* p = &reference.i1')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'int* p = &reference.i1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p = &reference.i1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &reference.i1')
              IOperation:  (OperationKind.None, Type: null) (Syntax: '&reference.i1')
                Children(1):
                    IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&reference.i1')
                      Reference: 
                        IFieldReferenceOperation: System.Int32 Program.i1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'reference.i1')
                          Instance Receiver: 
                            ILocalReferenceOperation: reference (OperationKind.LocalReference, Type: Program) (Syntax: 'reference')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int* p1 = & ... eference.i2')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'int* p1 = & ... eference.i2')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p1 = &reference.i1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &reference.i1')
              IOperation:  (OperationKind.None, Type: null) (Syntax: '&reference.i1')
                Children(1):
                    IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&reference.i1')
                      Reference: 
                        IFieldReferenceOperation: System.Int32 Program.i1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'reference.i1')
                          Instance Receiver: 
                            ILocalReferenceOperation: reference (OperationKind.LocalReference, Type: Program) (Syntax: 'reference')
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p2 = &reference.i2')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &reference.i2')
              IOperation:  (OperationKind.None, Type: null) (Syntax: '&reference.i2')
                Children(1):
                    IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&reference.i2')
                      Reference: 
                        IFieldReferenceOperation: System.Int32 Program.i2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'reference.i2')
                          Instance Receiver: 
                            ILocalReferenceOperation: reference (OperationKind.LocalReference, Type: Program) (Syntax: 'reference')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(8, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int* p = /*</bind>*/')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'int* p = /*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p = /*</bind>*/')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= /*</bind>*/')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int* p1 = , ... /*</bind>*/')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'int* p1 = , ... /*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p1 = ')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p2) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p2 = /*</bind>*/')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= /*</bind>*/')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int* p')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'int* p')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p')
          Initializer: 
            null
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int* p1, p2')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'int* p1, p2')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p1')
          Initializer: 
            null
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p2) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p2')
          Initializer: 
            null
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int* p1 = & ... /*</bind>*/')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'int* p1 = & ... /*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32* p1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p1 = &reference.i1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &reference.i1')
              IOperation:  (OperationKind.None, Type: null) (Syntax: '&reference.i1')
                Children(1):
                    IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&reference.i1')
                      Reference: 
                        IFieldReferenceOperation: System.Int32 Program.i1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'reference.i1')
                          Instance Receiver: 
                            ILocalReferenceOperation: reference (OperationKind.LocalReference, Type: Program) (Syntax: 'reference')
        ISingleVariableDeclarationOperation (Symbol: System.Int32* ) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Program p1  ... w Program()')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'Program p1  ... w Program()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p1 = new Program()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Program()')
              IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                Arguments(0)
                Initializer: 
                  null
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Program p1  ... w Program()')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'Program p1  ... w Program()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p1 = new Program()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Program()')
              IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                Arguments(0)
                Initializer: 
                  null
        ISingleVariableDeclarationOperation (Symbol: Program p2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p2 = new Program()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Program()')
              IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                Arguments(0)
                Initializer: 
                  null
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Program p1 =/*</bind>*/')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'Program p1 =/*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p1 =/*</bind>*/')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=/*</bind>*/')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         using (/*<bind>*/Program p1 =/*</bind>*/)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Program p1  ... /*</bind>*/')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'Program p1  ... /*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p1 =')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        ISingleVariableDeclarationOperation (Symbol: Program p2) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p2 =/*</bind>*/')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=/*</bind>*/')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Program p1')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'Program p1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p1')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0210: You must provide an initializer in a fixed or using statement declaration
                //         using (/*<bind>*/Program p1/*</bind>*/)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "p1").WithLocation(8, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Program p1, p2')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'Program p1, p2')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p1')
          Initializer: 
            null
        ISingleVariableDeclarationOperation (Symbol: Program p2) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'p2')
          Initializer: 
            null
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Program p1  ... /*</bind>*/')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'Program p1  ... /*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p1 = new Program()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Program()')
              IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                Arguments(0)
                Initializer: 
                  null
        ISingleVariableDeclarationOperation (Symbol: Program ) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Program p1  ... etProgram()')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'Program p1  ... etProgram()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p1 = GetProgram()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetProgram()')
              IInvocationOperation (Program Program.GetProgram()) (OperationKind.Invocation, Type: Program) (Syntax: 'GetProgram()')
                Instance Receiver: 
                  null
                Arguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Program p1  ... etProgram()')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'Program p1  ... etProgram()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p1 = GetProgram()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetProgram()')
              IInvocationOperation (Program Program.GetProgram()) (OperationKind.Invocation, Type: Program) (Syntax: 'GetProgram()')
                Instance Receiver: 
                  null
                Arguments(0)
        ISingleVariableDeclarationOperation (Symbol: Program p2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p2 = GetProgram()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetProgram()')
              IInvocationOperation (Program Program.GetProgram()) (OperationKind.Invocation, Type: Program) (Syntax: 'GetProgram()')
                Instance Receiver: 
                  null
                Arguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Program p2 = p1')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'Program p2 = p1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p2 = p1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= p1')
              ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: Program) (Syntax: 'p1')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Program p2 = p1, p3 = p1')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'Program p2 = p1, p3 = p1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: Program p2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p2 = p1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= p1')
              ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: Program) (Syntax: 'p1')
        ISingleVariableDeclarationOperation (Symbol: Program p3) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'p3 = p1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= p1')
              ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: Program) (Syntax: 'p1')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        #region For Loops

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = 0')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'int i = 0')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i = 0')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = 0, j = 0')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'int i = 0, j = 0')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i = 0')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        ISingleVariableDeclarationOperation (Symbol: System.Int32 j) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'j = 0')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'j' is assigned but its value is never used
                //         for (/*<bind>*/int i = 0, j = 0/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j").WithLocation(6, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int i =/*</bind>*/')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'int i =/*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i =/*</bind>*/')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=/*</bind>*/')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         for (/*<bind>*/int i =/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int i =, j =/*</bind>*/')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'int i =, j =/*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i =')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        ISingleVariableDeclarationOperation (Symbol: System.Int32 j) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'j =/*</bind>*/')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=/*</bind>*/')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'int i')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'i'
                //         for (/*<bind>*/int i/*</bind>*/; i < 0; i++)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i, j')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'int i, j')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i')
          Initializer: 
            null
        ISingleVariableDeclarationOperation (Symbol: System.Int32 j) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'j')
          Initializer: 
            null
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int i =,/*</bind>*/')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid, IsImplicit) (Syntax: 'int i =,/*</bind>*/')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i =')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        ISingleVariableDeclarationOperation (Symbol: System.Int32 ) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = GetInt()')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'int i = GetInt()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i = Get ...  = GetInt()')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsImplicit) (Syntax: 'int i = Get ...  = GetInt()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
        ISingleVariableDeclarationOperation (Symbol: System.Int32 j) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'j = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion

        #region Const Local Declarations

        [CompilerTrait(CompilerFeature.IOperation)]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'const int i1 = 1;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1 = 1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1 = 1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         /*<bind>*/const int i1 = 1;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'const int i ...  1, i2 = 2;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1 = 1, i2 = 2')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1 = 1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i2 = 2')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    Initializer: 
      null
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1 = ;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'const int i1 = ;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = ')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1 = ')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/const int i1 = ;/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsInvalidInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1 = , i2 = ;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'const int i1 = , i2 = ;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = , i2 = ')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1 = ')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i2 = ')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ','
                //         const /*<bind>*/int i1 = , i2 = /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 34),
                // CS1525: Invalid expression term ';'
                //         const /*<bind>*/int i1 = , i2 = /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationNoInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'const int i1;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0145: A const field requires a value to be provided
                //         const /*<bind>*/int i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "i1").WithLocation(6, 29),
                // CS0168: The variable 'i1' is declared but never used
                //         const /*<bind>*/int i1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i1").WithArguments("i1").WithLocation(6, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMutlipleDeclarationsNoInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1, i2;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'const int i1, i2;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1, i2')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      null
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

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalInvalidMultipleDeclarations()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1,;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'const int i1,;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1,')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclarationOperation (Symbol: System.Int32 ) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0145: A const field requires a value to be provided
                //         const /*<bind>*/int i1,/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "i1").WithLocation(6, 29),
                // CS1001: Identifier expected
                //         const /*<bind>*/int i1,/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(6, 32),
                // CS0145: A const field requires a value to be provided
                //         const /*<bind>*/int i1,/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, ";").WithLocation(6, 32),
                // CS0168: The variable 'i1' is declared but never used
                //         const /*<bind>*/int i1,/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i1").WithArguments("i1").WithLocation(6, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationExpressionInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1 = GetInt();/*</bind>*/
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'const int i1 = GetInt();')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = GetInt()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1 = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0133: The expression being assigned to 'i1' must be constant
                //         const /*<bind>*/int i1 = GetInt()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "GetInt()").WithArguments("i1").WithLocation(6, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsExpressionInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1 = GetInt(), i2 = GetInt();/*</bind>*/
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'const int i ... = GetInt();')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = Ge ...  = GetInt()')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1 = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null, IsInvalid) (Syntax: 'i2 = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0133: The expression being assigned to 'i1' must be constant
                //         const /*<bind>*/int i1 = GetInt(), i2 = GetInt()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "GetInt()").WithArguments("i1").WithLocation(6, 34),
                // CS0133: The expression being assigned to 'i2' must be constant
                //         const /*<bind>*/int i1 = GetInt(), i2 = GetInt()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "GetInt()").WithArguments("i2").WithLocation(6, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationLocalReferenceInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const int i = 1;
        /*<bind>*/const int i1 = i;/*</bind>*/
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'const int i1 = i;')
  IMultiVariableDeclarationOperation (1 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1 = i')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1 = i')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         const /*<bind>*/int i1 = i/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(7, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsLocalReferenceInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const int i = 1;
        /*<bind>*/const int i1 = i, i2 = i1;/*</bind>*/
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'const int i ... i, i2 = i1;')
  IMultiVariableDeclarationOperation (2 declarations) (OperationKind.MultiVariableDeclaration, Type: null) (Syntax: 'int i1 = i, i2 = i1')
    Declarations:
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i1) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i1 = i')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i')
        ISingleVariableDeclarationOperation (Symbol: System.Int32 i2) (OperationKind.SingleVariableDeclaration, Type: null) (Syntax: 'i2 = i1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
              ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i1')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i2' is assigned but its value is never used
                //         const /*<bind>*/int i1 = i, i2 = i1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(7, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        #endregion
    }
}
