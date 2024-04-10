// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IVariableDeclaration : SemanticModelTestBase
    {
        #region Variable Declarations

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void VariableDeclarator()
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
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
        public void VariableDeclaratorWithInitializer()
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1 = 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = 1')
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
        public void VariableDeclaratorWithInvalidInitializer()
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = ')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = ')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1, i2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
          Initializer: 
            null
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1 = 2, i2 = 2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = 2')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2 = 2')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = , i2 = 2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = ')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2 = 2')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i,')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
          Initializer: 
            null
        IVariableDeclaratorOperation (Symbol: System.Int32 ) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '')
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
        public void VariableDeclaratorExpressionInitializer()
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = GetInt()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = GetInt()')
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
        public void MultipleVariableDeclarationsExpressionInitializers()
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = Get ...  = GetInt()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
        IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = GetInt()')
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
        public void VariableDeclaratorLocalReferenceInitializer()
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1 = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = i')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1 = i, i2 = i1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = i')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2 = i1')
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[2, 3] a')
    Ignored Dimensions(2):
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32[,] a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[2, 3] a;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[2, 3]").WithLocation(6, 22),
                // file.cs(6,29): warning CS0168: The variable 'a' is declared but never used
                //         /*<bind>*/int[2, 3] a;/*</bind>*/
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[2, 3] a, b')
    Ignored Dimensions(2):
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32[,] a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
        IVariableDeclaratorOperation (Symbol: System.Int32[,] b) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'b')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[2, 3] a, b;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[2, 3]").WithLocation(6, 22),
                // file.cs(6,29): warning CS0168: The variable 'a' is declared but never used
                //         /*<bind>*/int[2, 3] a, b;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "a").WithArguments("a").WithLocation(6, 29),
                // file.cs(6,32): warning CS0168: The variable 'b' is declared but never used
                //         /*<bind>*/int[2, 3] a, b;/*</bind>*/
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredArguments_WithInitializer()
        {
            string source = @"
class C
{
    void M1()
    {
        int /*<bind>*/x[10] = 1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x[10] = 1')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  IgnoredArguments(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //         int /*<bind>*/x[10] = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CStyleArray, "[10]").WithLocation(6, 24),
                // CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         int /*<bind>*/x[10] = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "10").WithLocation(6, 25),
                // CS0219: The variable 'x' is assigned but its value is never used
                //         int /*<bind>*/x[10] = 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredArguments_NoInitializer()
        {
            string source = @"
class C
{
    void M1()
    {
        int /*<bind>*/x[10]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x[10]')
  Initializer: 
    null
  IgnoredArguments(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //         int /*<bind>*/x[10]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CStyleArray, "[10]").WithLocation(6, 24),
                // CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         int /*<bind>*/x[10]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "10").WithLocation(6, 25),
                // CS0168: The variable 'x' is declared but never used
                //         int /*<bind>*/x[10]/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredArgumentsWithInitializer_VerifyChildren()
        {
            string source = @"
class C
{
    void M1()
    {
        int /*<bind>*/x[10] = 1/*</bind>*/;
    }
}
";

            var compilation = CreateEmptyCompilation(source);
            (var operation, _) = GetOperationAndSyntaxForTest<VariableDeclaratorSyntax>(compilation);
            var declarator = (IVariableDeclaratorOperation)operation;
            Assert.Equal(2, declarator.ChildOperations.Count());
            Assert.Equal(OperationKind.Literal, declarator.ChildOperations.First().Kind);
            Assert.Equal(OperationKind.VariableInitializer, declarator.ChildOperations.ElementAt(1).Kind);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredArguments_VerifyChildren()
        {
            string source = @"
class C
{
    void M1()
    {
        int /*<bind>*/x[10]/*</bind>*/;
    }
}
";

            var compilation = CreateEmptyCompilation(source);
            (var operation, _) = GetOperationAndSyntaxForTest<VariableDeclaratorSyntax>(compilation);
            var declarator = (IVariableDeclaratorOperation)operation;
            Assert.Equal(1, declarator.ChildOperations.Count());
            Assert.Equal(OperationKind.Literal, declarator.ChildOperations.First().Kind);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_WithInitializer_VerifyChildren()
        {
            string source = @"
class C
{
    void M1()
    {
        int /*<bind>*/x = 1/*</bind>*/;
    }
}
";

            var compilation = CreateEmptyCompilation(source);
            (var operation, _) = GetOperationAndSyntaxForTest<VariableDeclaratorSyntax>(compilation);
            var declarator = (IVariableDeclaratorOperation)operation;
            Assert.Equal(1, declarator.ChildOperations.Count());
            Assert.Equal(OperationKind.VariableInitializer, declarator.ChildOperations.ElementAt(0).Kind);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_NoChildren()
        {
            string source = @"
class C
{
    void M1()
    {
        int /*<bind>*/x/*</bind>*/;
    }
}
";

            var compilation = CreateEmptyCompilation(source);
            (var operation, _) = GetOperationAndSyntaxForTest<VariableDeclaratorSyntax>(compilation);
            Assert.Empty(operation.ChildOperations);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_WithInitializer()
        {
            string source = @"
class C
{
    void M1()
    {
        /*<bind>*/int[10] x = { 1 };/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[10] x = { 1 }')
  Ignored Dimensions(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = { 1 }')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= { 1 }')
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: '{ 1 }')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '{ 1 }')
              Initializer: 
                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 1 }')
                  Element Values(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[10] x = { 1 };/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[10]").WithLocation(6, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_NoInitializer()
        {
            string source = @"
class C
{
    void M1()
    {
        /*<bind>*/int[10] x;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[10] x')
  Ignored Dimensions(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[10] x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[10]").WithLocation(6, 22),
                // file.cs(6,27): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int[10] x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_InArrayOfArrays()
        {
            string source = @"
using System;
class C
{
    void M1()
    {
        /*<bind>*/int[][10] x;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[][10] x')
  Ignored Dimensions(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[][] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,24): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[][10] x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[10]").WithLocation(7, 24),
                // file.cs(7,29): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int[][10] x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_2ndDimensionOfMultidimensionalArray()
        {
            string source = @"
using System;
class C
{
    void M1()
    {
        /*<bind>*/int[,10] x;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[,10] x')
  Ignored Dimensions(2):
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[,] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[,10] x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[,10]").WithLocation(7, 22),
                // file.cs(7,23): error CS0443: Syntax error; value expected
                //         /*<bind>*/int[,10] x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(7, 23),
                // file.cs(7,28): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int[,10] x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensionsWithInitializer_VerifyChildren()
        {
            string source = @"
class C
{
    void M1()
    {
        /*<bind>*/int[10] x = { 1 }/*</bind>*/;
    }
}
";

            var compilation = CreateEmptyCompilation(source);
            (var operation, _) = GetOperationAndSyntaxForTest<VariableDeclarationSyntax>(compilation);
            var declaration = (IVariableDeclarationOperation)operation;
            Assert.Equal(2, declaration.ChildOperations.Count());
            Assert.Equal(OperationKind.Literal, declaration.ChildOperations.First().Kind);
            Assert.Equal(OperationKind.VariableDeclarator, declaration.ChildOperations.ElementAt(1).Kind);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_VerifyChildren()
        {
            string source = @"
class C
{
    void M1()
    {
        /*<bind>*/int[10] x;/*</bind>*/
    }
}
";

            var compilation = CreateEmptyCompilation(source);
            (var operation, _) = GetOperationAndSyntaxForTest<VariableDeclarationSyntax>(compilation);
            var declaration = (IVariableDeclarationOperation)operation;
            Assert.Equal(2, declaration.ChildOperations.Count());
            Assert.Equal(OperationKind.Literal, declaration.ChildOperations.First().Kind);
            Assert.Equal(OperationKind.VariableDeclarator, declaration.ChildOperations.ElementAt(1).Kind);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_VerifyInvalidDimensions()
        {
            string source = @"
class C
{
    void M1()
    {
        int[/*<bind>*/10/*</bind>*/] x;
    }
}
";
            string expectedOperationTree = "ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,12): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         int[/*<bind>*/10/*</bind>*/] x;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[/*<bind>*/10/*</bind>*/]").WithLocation(6, 12),
                // file.cs(6,38): warning CS0168: The variable 'x' is declared but never used
                //         int[/*<bind>*/10/*</bind>*/] x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_TestSemanticModel()
        {
            string source = @"
class C
{
    void M1()
    {
        int[10] x;
        int[M2()]
    }

    int M2() => 42;
}
";

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var literalExpr = nodes.OfType<LiteralExpressionSyntax>().ElementAt(0);

            Assert.Equal(@"10", literalExpr.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(literalExpr).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(literalExpr).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(literalExpr));

            var invocExpr = nodes.OfType<InvocationExpressionSyntax>().ElementAt(0);

            Assert.Equal(@"M2()", invocExpr.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(invocExpr).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(invocExpr).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(invocExpr));

            var invocInfo = model.GetSymbolInfo(invocExpr);
            Assert.NotNull(invocInfo.Symbol);
            Assert.Equal(SymbolKind.Method, invocInfo.Symbol.Kind);
            Assert.Equal("M2", invocInfo.Symbol.MetadataName);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_OutVarDeclaration()
        {
            string source = @"
class C
{
    void M1()
    {
        /*<bind>*/int[M2(out var z)] x;/*</bind>*/
        z = 34;
    }
    
    public int M2(out int i) => i = 42;
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[M2(out var z)] x')
  Ignored Dimensions(1):
      IInvocationOperation ( System.Int32 C.M2(out System.Int32 i)) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2(out var z)')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out var z')
              IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var z')
                ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'z')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[M2(out var z)] x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[M2(out var z)]").WithLocation(6, 22),
                // file.cs(6,34): warning CS0219: The variable 'z' is assigned but its value is never used
                //         /*<bind>*/int[M2(out var z)] x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "z").WithArguments("z").WithLocation(6, 34),
                // file.cs(6,38): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int[M2(out var z)] x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_OutVarDeclaration_InNullableArrayType()
        {
            string source = @"
class C
{
    void M1()
    {
        /*<bind>*/int[M2(out var z)]? x;/*</bind>*/
        z = 34;
    }
    
    public int M2(out int i) => i = 42;
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[M2(out var z)]? x')
  Ignored Dimensions(1):
      IInvocationOperation ( System.Int32 C.M2(out System.Int32 i)) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2(out var z)')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out var z')
              IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var z')
                ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'z')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[]? x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[M2(out var z)]? x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[M2(out var z)]").WithLocation(6, 22),
                // file.cs(6,34): warning CS0219: The variable 'z' is assigned but its value is never used
                //         /*<bind>*/int[M2(out var z)]? x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "z").WithArguments("z").WithLocation(6, 34),
                // file.cs(6,37): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //         /*<bind>*/int[M2(out var z)]? x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(6, 37),
                // file.cs(6,39): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int[M2(out var z)]? x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 39)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_OutVarDeclaration_InRefType()
        {
            string source = @"
class C
{
    void M1()
    {
        /*<bind>*/ref int[M2(out var z)] y;/*</bind>*/
        z = 34;
    }
    
    public int M2(out int i) => i = 42;
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'ref int[M2(out var z)] y')
  Ignored Dimensions(1):
      IInvocationOperation ( System.Int32 C.M2(out System.Int32 i)) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2(out var z)')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out var z')
              IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var z')
                ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'z')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[] y) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'y')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,26): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/ref int[M2(out var z)] y;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[M2(out var z)]").WithLocation(6, 26),
                // file.cs(6,38): warning CS0219: The variable 'z' is assigned but its value is never used
                //         /*<bind>*/ref int[M2(out var z)] y;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "z").WithArguments("z").WithLocation(6, 38),
                // file.cs(6,42): error CS8174: A declaration of a by-reference variable must have an initializer
                //         /*<bind>*/ref int[M2(out var z)] y;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "y").WithLocation(6, 42),
                // file.cs(6,42): warning CS0168: The variable 'y' is declared but never used
                //         /*<bind>*/ref int[M2(out var z)] y;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y").WithArguments("y").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_OutVarDeclaration_InDoublyNestedType()
        {
            string source = @"
class C
{
    void M1()
    {
        /*<bind>*/ref int[M2(out var z)]? y;/*</bind>*/
        z = 34;
    }
    
    public int M2(out int i) => i = 42;
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'ref int[M2( ...  var z)]? y')
  Ignored Dimensions(1):
      IInvocationOperation ( System.Int32 C.M2(out System.Int32 i)) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2(out var z)')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out var z')
              IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var z')
                ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'z')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[]? y) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'y')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,26): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/ref int[M2(out var z)]? y;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[M2(out var z)]").WithLocation(6, 26),
                // file.cs(6,38): warning CS0219: The variable 'z' is assigned but its value is never used
                //         /*<bind>*/ref int[M2(out var z)]? y;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "z").WithArguments("z").WithLocation(6, 38),
                // file.cs(6,41): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //         /*<bind>*/ref int[M2(out var z)]? y;/*</bind>*/
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(6, 41),
                // file.cs(6,43): error CS8174: A declaration of a by-reference variable must have an initializer
                //         /*<bind>*/ref int[M2(out var z)]? y;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "y").WithLocation(6, 43),
                // file.cs(6,43): warning CS0168: The variable 'y' is declared but never used
                //         /*<bind>*/ref int[M2(out var z)]? y;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y").WithArguments("y").WithLocation(6, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_NestedArrayType()
        {
            string source = @"
class C
{
#nullable enable
    void M1()
    {
        /*<bind>*/int[10]?[20]? x;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[10]?[20]? x')
  Ignored Dimensions(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20, IsInvalid) (Syntax: '20')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[]?[]? x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[10]?[20]? x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[10]").WithLocation(7, 22),
                // file.cs(7,27): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[10]?[20]? x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[20]").WithLocation(7, 27),
                // file.cs(7,33): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int[10]?[20]? x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_AliasQualifiedName01()
        {
            string source = @"
using Col=System.Collections.Generic;
class C
{
    void M1()
    {
        /*<bind>*/Col::List<int[]> x;/*</bind>*/
    }
}
";
            var syntaxTree = Parse(source, filename: "file.cs");
            var rankSpecifierOld = syntaxTree.GetCompilationUnitRoot().DescendantNodes().OfType<ArrayRankSpecifierSyntax>().First();
            var rankSpecifierNew = rankSpecifierOld
                .WithSizes(SyntaxFactory.SeparatedList<ExpressionSyntax>(SyntaxFactory.NodeOrTokenList(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(10)))));
            syntaxTree = syntaxTree.GetCompilationUnitRoot().ReplaceNode(rankSpecifierOld, rankSpecifierNew).SyntaxTree;

            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Col::List<int[10]> x')
  Ignored Dimensions(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Collections.Generic.List<System.Int32[]> x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,32): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/Col::List<int[10]> x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[10]").WithLocation(7, 32),
                // file.cs(7,38): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/Col::List<int[10]> x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(new[] { syntaxTree }, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_AliasQualifiedName02()
        {
            string source = @"
using List=System.Collections.Generic.List<int[10]>;

class C
{
    void M1()
    {
        /*<bind>*/List x;/*</bind>*/
    }
}
";

            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'List x')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Collections.Generic.List<System.Int32[]> x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(2,47): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                // using List=System.Collections.Generic.List<int[10]>;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[10]").WithLocation(2, 47),
                // file.cs(8,24): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/List x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(8, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_DeclarationPattern()
        {
            string source = @"
class C
{
    void M1()
    {
        int y = 10;
        /*<bind>*/int[y is int z] x;/*</bind>*/
        z = 34;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[y is int z] x')
  Ignored Dimensions(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'y is int z')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'y is int z')
            Value: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 10;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 13),
                // file.cs(7,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[y is int z] x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[y is int z]").WithLocation(7, 22),
                // file.cs(7,23): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                //         /*<bind>*/int[y is int z] x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y is int z").WithArguments("bool", "int").WithLocation(7, 23),
                // file.cs(7,35): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int[y is int z] x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IVariableDeclaration_InvalidIgnoredDimensions_SwitchExpression()
        {
            string source = @"
class C
{
    void M1()
    {
        int y = 10;
        /*<bind>*/int[M(y switch { int z => 42 })] x;/*</bind>*/
    }

    int M(int a) => a;
}
";

            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[M(y swi ... => 42 })] x')
  Ignored Dimensions(1):
      IInvocationOperation ( System.Int32 C.M(System.Int32 a)) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M(y switch  ...  z => 42 })')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'y switch { int z => 42 }')
              ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'y switch { int z => 42 }')
                Value: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
                Arms(1):
                    ISwitchExpressionArmOperation (1 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: 'int z => 42')
                      Pattern: 
                        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
                      Locals: Local_1: System.Int32 z
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
        Initializer: 
          null
  Initializer: 
    null
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 10;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 13),
                // file.cs(7,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         /*<bind>*/int[M(y switch { int z => 42 })] x;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[M(y switch { int z => 42 })]").WithLocation(7, 22),
                // file.cs(7,52): warning CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int[M(y switch { int z => 42 })] x;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 52)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int* p = &reference.i1')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32* p) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p = &reference.i1')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &reference.i1')
            IOperation:  (OperationKind.None, Type: System.Int32*, IsImplicit) (Syntax: '&reference.i1')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int* p1 = & ... eference.i2')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = &reference.i1')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &reference.i1')
            IOperation:  (OperationKind.None, Type: System.Int32*, IsImplicit) (Syntax: '&reference.i1')
              Children(1):
                  IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&reference.i1')
                    Reference: 
                      IFieldReferenceOperation: System.Int32 Program.i1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'reference.i1')
                        Instance Receiver: 
                          ILocalReferenceOperation: reference (OperationKind.LocalReference, Type: Program) (Syntax: 'reference')
      IVariableDeclaratorOperation (Symbol: System.Int32* p2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p2 = &reference.i2')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &reference.i2')
            IOperation:  (OperationKind.None, Type: System.Int32*, IsImplicit) (Syntax: '&reference.i2')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int* p = /*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32* p) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p = /*</bind>*/')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int* p1 = , ... /*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1 = ')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)
      IVariableDeclaratorOperation (Symbol: System.Int32* p2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p2 = /*</bind>*/')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int* p')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32* p) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int* p1, p2')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1')
        Initializer: 
          null
      IVariableDeclaratorOperation (Symbol: System.Int32* p2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p2')
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
        public void FixedStatementInvalidMultipleDeclarations()
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int* p1 = & ... /*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = &reference.i1')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &reference.i1')
            IOperation:  (OperationKind.None, Type: System.Int32*, IsImplicit) (Syntax: '&reference.i1')
              Children(1):
                  IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&reference.i1')
                    Reference: 
                      IFieldReferenceOperation: System.Int32 Program.i1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'reference.i1')
                        Instance Receiver: 
                          ILocalReferenceOperation: reference (OperationKind.LocalReference, Type: Program) (Syntax: 'reference')
      IVariableDeclaratorOperation (Symbol: System.Int32* ) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '')
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

        [Fact]
        public void FixedStatement_InvalidIgnoredDimensions_SwitchExpression()
        {
            string source = @"
class C
{
    void M1()
    {
        int y = 10;
        unsafe
        {
            fixed (/*<bind>*/int[M2(y switch { int z => 42 })] p1 = null/*</bind>*/)
            {

            }
        }
    }

    int M2(int x) => x;
}
";

            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[M2(y sw ... ] p1 = null')
  Ignored Dimensions(1):
      IInvocationOperation ( System.Int32 C.M2(System.Int32 x)) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2(y switch ...  z => 42 })')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'y switch { int z => 42 }')
              ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'y switch { int z => 42 }')
                Value: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
                Arms(1):
                    ISwitchExpressionArmOperation (1 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: 'int z => 42')
                      Pattern: 
                        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
                      Locals: Local_1: System.Int32 z
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[] p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1 = null')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= null')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  Initializer: 
    null
";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 10;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 13),
                // file.cs(7,9): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(7, 9),
                // file.cs(9,33): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //             fixed (/*<bind>*/int[M2(y switch { int z => 42 })] p1 = null/*</bind>*/)
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[M2(y switch { int z => 42 })]").WithLocation(9, 33),
                // file.cs(9,64): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                //             fixed (/*<bind>*/int[M2(y switch { int z => 42 })] p1 = null/*</bind>*/)
                Diagnostic(ErrorCode.ERR_BadFixedInitType, "p1 = null").WithLocation(9, 64)
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Program p1  ... w Program()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = new Program()')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Program p1  ... w Program()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = new Program()')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Program()')
            IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
              Arguments(0)
              Initializer: 
                null
      IVariableDeclaratorOperation (Symbol: Program p2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p2 = new Program()')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Program p1 =/*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1 =/*</bind>*/')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Program p1  ... /*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1 =')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)
      IVariableDeclaratorOperation (Symbol: Program p2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p2 =/*</bind>*/')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Program p1')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Program p1, p2')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1')
        Initializer: 
          null
      IVariableDeclaratorOperation (Symbol: Program p2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p2')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Program p1  ... /*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = new Program()')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new Program()')
            IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
              Arguments(0)
              Initializer: 
                null
      IVariableDeclaratorOperation (Symbol: Program ) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Program p1  ... etProgram()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = GetProgram()')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Program p1  ... etProgram()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = GetProgram()')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetProgram()')
            IInvocationOperation (Program Program.GetProgram()) (OperationKind.Invocation, Type: Program) (Syntax: 'GetProgram()')
              Instance Receiver: 
                null
              Arguments(0)
      IVariableDeclaratorOperation (Symbol: Program p2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p2 = GetProgram()')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Program p2 = p1')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p2 = p1')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Program p2 = p1, p3 = p1')
  Declarators:
      IVariableDeclaratorOperation (Symbol: Program p2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p2 = p1')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= p1')
            ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: Program) (Syntax: 'p1')
      IVariableDeclaratorOperation (Symbol: Program p3) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p3 = p1')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= p1')
            ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: Program) (Syntax: 'p1')
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void UsingBlock_InvalidIgnoredDimensions_SwitchExpression()
        {
            string source = @"
class C
{
    void M1()
    {
        int y = 10;
       using( /*<bind>*/int[] x = new int[0]/*</bind>*/){}
    }
}
";
            var syntaxTree = Parse(source, filename: "file.cs");
            var rankSpecifierOld = syntaxTree.GetCompilationUnitRoot().DescendantNodes().OfType<ArrayRankSpecifierSyntax>().First();
            var rankSpecifierNew = rankSpecifierOld
                .WithSizes(SyntaxFactory.SeparatedList<ExpressionSyntax>(SyntaxFactory.NodeOrTokenList(SyntaxFactory.ParseExpression("y switch { int z => 42 }"))));
            syntaxTree = syntaxTree.GetCompilationUnitRoot().ReplaceNode(rankSpecifierOld, rankSpecifierNew).SyntaxTree;

            string expectedOperationTree = @"
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[y switc ...  new int[0]')
      Ignored Dimensions(1):
          ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'y switch { int z => 42 }')
            Value:
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
            Arms(1):
                ISwitchExpressionArmOperation (1 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: 'int z => 42')
                  Pattern:
                    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
                  Locals: Local_1: System.Int32 z
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = new int[0]')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new int[0]')
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsInvalid) (Syntax: 'new int[0]')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                  Initializer:
                    null
      Initializer:
        null
";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 10;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 13),
                // file.cs(7,25): error CS1674: 'int[]': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //        using( /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/){}
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "int[y switch { int z => 42 }] x = new int[0]").WithArguments("int[]").WithLocation(7, 25),
                // file.cs(7,28): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //        using( /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/){}
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[y switch { int z => 42 }]").WithLocation(7, 28),
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(new[] { syntaxTree }, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void UsingStatement_InvalidIgnoredDimensions_SwitchExpression()
        {
            string source = @"
class C
{
    void M1()
    {
        int y = 10;
       using( /*<bind>*/int[] x = new int[0]/*</bind>*/);
    }
}
";
            var syntaxTree = Parse(source, filename: "file.cs");
            var rankSpecifierOld = syntaxTree.GetCompilationUnitRoot().DescendantNodes().OfType<ArrayRankSpecifierSyntax>().First();
            var rankSpecifierNew = rankSpecifierOld
                .WithSizes(SyntaxFactory.SeparatedList<ExpressionSyntax>(SyntaxFactory.NodeOrTokenList(SyntaxFactory.ParseExpression("y switch { int z => 42 }"))));
            syntaxTree = syntaxTree.GetCompilationUnitRoot().ReplaceNode(rankSpecifierOld, rankSpecifierNew).SyntaxTree;

            string expectedOperationTree = @"
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[y switc ...  new int[0]')
      Ignored Dimensions(1):
          ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'y switch { int z => 42 }')
            Value:
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
            Arms(1):
                ISwitchExpressionArmOperation (1 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: 'int z => 42')
                  Pattern:
                    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
                  Locals: Local_1: System.Int32 z
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = new int[0]')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new int[0]')
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsInvalid) (Syntax: 'new int[0]')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                  Initializer:
                    null
      Initializer:
        null
";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 10;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 13),
                // file.cs(7,25): error CS1674: 'int[]': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //        using( /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/);
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "int[y switch { int z => 42 }] x = new int[0]").WithArguments("int[]").WithLocation(7, 25),
                // file.cs(7,28): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //        using( /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/);
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[y switch { int z => 42 }]").WithLocation(7, 28),
                // file.cs(7,81): warning CS0642: Possible mistaken empty statement
                //        using( /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/);
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(7, 81)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(new[] { syntaxTree }, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void UsingDeclaration_InvalidIgnoredDimensions_SwitchExpression()
        {
            string source = @"
class C
{
    void M1()
    {
        int y = 10;
       using /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[y switc ...  new int[0]')
      Ignored Dimensions(1):
          ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'y switch { int z => 42 }')
            Value:
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
            Arms(1):
                ISwitchExpressionArmOperation (1 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: 'int z => 42')
                  Pattern:
                    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
                  Locals: Local_1: System.Int32 z
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = new int[0]')
            Initializer:
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new int[0]')
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsInvalid) (Syntax: 'new int[0]')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                  Initializer:
                    null
      Initializer:
        null
";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 10;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 13),
                // file.cs(7,8): error CS1674: 'int[]': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //        using /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "using /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/;").WithArguments("int[]").WithLocation(7, 8),
                // file.cs(7,27): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //        using /*<bind>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[y switch { int z => 42 }]").WithLocation(7, 27)
            };

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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0, j = 0')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i =/*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i =/*</bind>*/')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i =, j =/*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i =')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)
      IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'j =/*</bind>*/')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i, j')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
        Initializer: 
          null
      IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i =,/*</bind>*/')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i =')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)
      IVariableDeclaratorOperation (Symbol: System.Int32 ) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = GetInt()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = GetInt()')
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
IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = Get ...  = GetInt()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = GetInt()')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= GetInt()')
            IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'GetInt()')
              Instance Receiver: 
                null
              Arguments(0)
      IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = GetInt()')
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

        [Fact]
        public void ForLoop_InvalidIgnoredDimensions_SwitchExpression()
        {
            string source = @"
class C
{
    void M1()
    {
        int y = 10;
        for (/*<bind/>*/int[] x = new int[0]/*</bind>*/;;);
    }
}
";
            var syntaxTree = Parse(source, filename: "file.cs");
            var rankSpecifierOld = syntaxTree.GetCompilationUnitRoot().DescendantNodes().OfType<ArrayRankSpecifierSyntax>().First();
            var rankSpecifierNew = rankSpecifierOld
                .WithSizes(SyntaxFactory.SeparatedList<ExpressionSyntax>(SyntaxFactory.NodeOrTokenList(SyntaxFactory.ParseExpression("y switch { int z => 42 }"))));
            syntaxTree = syntaxTree.GetCompilationUnitRoot().ReplaceNode(rankSpecifierOld, rankSpecifierNew).SyntaxTree;

            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int[y switc ...  new int[0]')
  Ignored Dimensions(1):
      ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'y switch { int z => 42 }')
        Value: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
        Arms(1):
            ISwitchExpressionArmOperation (1 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: 'int z => 42')
              Pattern: 
                IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: False)
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
              Locals: Local_1: System.Int32 z
  Declarators:
      IVariableDeclaratorOperation (Symbol: System.Int32[] x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = new int[0]')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new int[0]')
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[]) (Syntax: 'new int[0]')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              Initializer: 
                null
  Initializer: 
    null
";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 10;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 13),
                // file.cs(7,28): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         for (/*<bind/>*/int[y switch { int z => 42 }] x = new int[0]/*</bind>*/;;);
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[y switch { int z => 42 }]").WithLocation(7, 28),
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(new[] { syntaxTree }, expectedOperationTree, expectedDiagnostics);
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1 = 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = 1')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1 = 1, i2 = 2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = 1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2 = 2')
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = ')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = ')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = , i2 = ')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = ')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                Children(0)
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i2 = ')
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1')
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
        public void ConstLocalMultipleDeclarationsNoInitializers()
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1, i2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1')
          Initializer: 
            null
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i2')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1,')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1')
          Initializer: 
            null
        IVariableDeclaratorOperation (Symbol: System.Int32 ) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '')
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = GetInt()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = GetInt()')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i1 = Ge ...  = GetInt()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i1 = GetInt()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= GetInt()')
              IInvocationOperation (System.Int32 Program.GetInt()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'GetInt()')
                Instance Receiver: 
                  null
                Arguments(0)
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i2 = GetInt()')
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
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1 = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = i')
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
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i1 = i, i2 = i1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 i1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1 = i')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i')
        IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2 = i1')
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

        #region Control Flow Graph
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_01()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int a = 1;
        var b = 2;
        int c = 3, d = 4;
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
    Locals: [System.Int32 a] [System.Int32 b] [System.Int32 c] [System.Int32 d]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 1')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b = 2')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'b = 2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'c = 3')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'c = 3')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = 4')
              Left: 
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = 4')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_02()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int a;
        a = 1;
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
    Locals: [System.Int32 a]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = 1')
                  Left: 
                    ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_03()
        {
            string source = @"
class C
{
    void M(bool a, int b, int c)
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int d = a ? b : c;
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
    Locals: [System.Int32 d]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = a ? b : c')
              Left: 
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = a ? b : c')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ? b : c')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_04()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int d = ;
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         int d = ;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(7, 17)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 d]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd = ')
              Left: 
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd = ')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_05()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        const int d = 1;
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
    Locals: [System.Int32 d]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = 1')
              Left: 
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_05_WithControlFlow()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        const int d = true ? 1 : 2;
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
    Locals: [System.Int32 d]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block [UnReachable]
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = true ? 1 : 2')
              Left: 
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = true ? 1 : 2')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'true ? 1 : 2')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_06()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int d[10] = 1;
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //         int d[10] = 1;
                Diagnostic(ErrorCode.ERR_CStyleArray, "[10]").WithLocation(7, 14),
                // CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         int d[10] = 1;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "10").WithLocation(7, 15)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 d]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd[10] = 1')
              Left: 
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'd[10] = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_07()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int = 5;
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //         int = 5;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=").WithLocation(7, 13)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 ]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= 5')
              Left: 
                ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= 5')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_08()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int a = 1;
        ref int b = ref a;
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
    Locals: [System.Int32 a] [System.Int32 b]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 1')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b = ref a')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'b = ref a')
              Right: 
                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_09()
        {
            string source = @"
class C
{
    int _c = 1;
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        ref int b = ref _c;
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
    Locals: [System.Int32 b]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b = ref _c')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'b = ref _c')
              Right: 
                IFieldReferenceOperation: System.Int32 C._c (OperationKind.FieldReference, Type: System.Int32) (Syntax: '_c')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '_c')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_10()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        ref int b = 1;
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8172: Cannot initialize a by-reference variable with a value
                //         ref int b = 1;
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "b = 1").WithLocation(7, 17),
                // CS1510: A ref or out value must be an assignable variable
                //         ref int b = 1;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "1").WithLocation(7, 21)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 b]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b = 1')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b = 1')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '1')
                  Children(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_11()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int a = 1;
        ref int b = a;
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8172: Cannot initialize a by-reference variable with a value
                //         ref int b = a;
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "b = a").WithLocation(8, 17)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 a] [System.Int32 b]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 1')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b = a')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b = a')
              Right: 
                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'a')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_12()
        {
            string source = @"
class C
{
    void M(bool a, int b, int c)
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int d = b, e = a ? b : c;
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
    Locals: [System.Int32 d] [System.Int32 e]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = b')
              Left: 
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = b')
              Right: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [0]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'e = a ? b : c')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'e = a ? b : c')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ? b : c')

            Next (Regular) Block[B6]
                Leaving: {R2} {R1}
    }
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void VariableDeclaration_13()
        {
            string source = @"
class C
{
    void M()
    /*<bind>*/{
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        a = 1;
        int a;
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0841: Cannot use local variable 'a' before it is declared
                //         a = 1;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "a").WithArguments("a").WithLocation(7, 9)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 a]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: var, IsInvalid) (Syntax: 'a = 1')
                  Left: 
                    ILocalReferenceOperation: a (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'a')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        #endregion
    }
}
