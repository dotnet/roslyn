// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NoConversions()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (int, int) t = /*<bind>*/(1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NoConversions_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(int, int) t = (1, 2);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: '(int, int) t = (1, 2);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: '(int, int) t = (1, 2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int32, System.Int32) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (1, 2)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (1, 2)')
              ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversions()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (uint, uint) t = /*<bind>*/(1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 2, IsImplicit) (Syntax: '2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversions_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(uint, uint) t = (1, 2);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: '(uint, uint) t = (1, 2);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: '(uint, uint) t = (1, 2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.UInt32, System.UInt32) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (1, 2)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (1, 2)')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.UInt32, System.UInt32), IsImplicit) (Syntax: '(1, 2)')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 2, IsImplicit) (Syntax: '2')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversionsWithTypedExpression()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        int a = 1;
        int b = 2;
        (long, long) t = /*<bind>*/(a, b)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int64 a, System.Int64 b)) (Syntax: '(a, b)')
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'a')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILocalReferenceOperation: b (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversionsWithTypedExpression_WithParentDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        int a = 1;
        int b = 2;
        /*<bind>*/(long, long) t = (a, b);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: '(long, long) t = (a, b);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: '(long, long) t = (a, b)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int64, System.Int64) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (a, b)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (a, b)')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int64, System.Int64), IsImplicit) (Syntax: '(a, b)')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ITupleOperation (OperationKind.Tuple, Type: (System.Int64 a, System.Int64 b)) (Syntax: '(a, b)')
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'a')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'b')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversionFromNull()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (uint, string) t = /*<bind>*/(1, null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.String)) (Syntax: '(1, null)')
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversionFromNull_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(uint, string) t = (1, null);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: '(uint, stri ...  (1, null);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: '(uint, stri ... = (1, null)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.UInt32, System.String) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (1, null)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (1, null)')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.UInt32, System.String), IsImplicit) (Syntax: '(1, null)')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.String)) (Syntax: '(1, null)')
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElements()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        var t = /*<bind>*/(A: 1, B: 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int32 A, System.Int32 B)) (Syntax: '(A: 1, B: 2)')
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElements_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/var t = (A: 1, B: 2);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var t = (A: 1, B: 2);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var t = (A: 1, B: 2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int32 A, System.Int32 B) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (A: 1, B: 2)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (A: 1, B: 2)')
              ITupleOperation (OperationKind.Tuple, Type: (System.Int32 A, System.Int32 B)) (Syntax: '(A: 1, B: 2)')
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElementsInTupleType()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (int A, int B) t = /*<bind>*/(1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElementsInTupleType_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(int A, int B) t = (1, 2);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: '(int A, int ... t = (1, 2);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: '(int A, int ...  t = (1, 2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int32 A, System.Int32 B) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (1, 2)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (1, 2)')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32 A, System.Int32 B), IsImplicit) (Syntax: '(1, 2)')
                Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                    Elements(2):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElementsAndImplicitConversions()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (short, string) t = /*<bind>*/(A: 1, B: null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int16 A, System.String B)) (Syntax: '(A: 1, B: null)')
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, Constant: 1, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8123: The tuple element name 'A' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         (short, string) t = /*<bind>*/(A: 1, B: null)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "A: 1").WithArguments("A", "(short, string)").WithLocation(8, 40),
                // CS8123: The tuple element name 'B' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         (short, string) t = /*<bind>*/(A: 1, B: null)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "B: null").WithArguments("B", "(short, string)").WithLocation(8, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElementsAndImplicitConversions_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(short, string) t = (A: 1, B: null);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: '(short, str ... , B: null);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: '(short, str ... 1, B: null)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int16, System.String) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (A: 1, B: null)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (A: 1, B: null)')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int16, System.String), IsImplicit) (Syntax: '(A: 1, B: null)')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ITupleOperation (OperationKind.Tuple, Type: (System.Int16 A, System.String B)) (Syntax: '(A: 1, B: null)')
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, Constant: 1, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8123: The tuple element name 'A' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         /*<bind>*/(short, string) t = (A: 1, B: null)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "A: 1").WithArguments("A", "(short, string)").WithLocation(8, 40),
                // CS8123: The tuple element name 'B' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         /*<bind>*/(short, string) t = (A: 1, B: null)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "B: null").WithArguments("B", "(short, string)").WithLocation(8, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionsForArguments()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C(int value)
    {
        return new C(value);
    }

    public static implicit operator short(C c)
    {
        return (short)c._x;
    }

    public static implicit operator string(C c)
    {
        return c._x.ToString();
    }

    public void M(C c1)
    {
        (short, string) t = /*<bind>*/(new C(0), c1)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int16, System.String c1)) (Syntax: '(new C(0), c1)')
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int16 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'new C(0)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int16 C.op_Implicit(C c))
        Operand: 
          IObjectCreationOperation (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0)')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer: 
              null
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.String C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.String C.op_Implicit(C c))
        Operand: 
          IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionsForArguments_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C(int value)
    {
        return new C(value);
    }

    public static implicit operator short(C c)
    {
        return (short)c._x;
    }

    public static implicit operator string(C c)
    {
        return c._x.ToString();
    }

    public void M(C c1)
    {
        /*<bind>*/(short, string) t = (new C(0), c1);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: '(short, str ...  C(0), c1);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: '(short, str ... w C(0), c1)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int16, System.String) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (new C(0), c1)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (new C(0), c1)')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int16, System.String), IsImplicit) (Syntax: '(new C(0), c1)')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ITupleOperation (OperationKind.Tuple, Type: (System.Int16, System.String c1)) (Syntax: '(new C(0), c1)')
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int16 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'new C(0)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int16 C.op_Implicit(C c))
                          Operand: 
                            IObjectCreationOperation (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0)')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32) (Syntax: '0')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Initializer: 
                                null
                        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.String C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.String C.op_Implicit(C c))
                          Operand: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionFromTupleExpression()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C((int, string) x)
    {
        return new C(x.Item1);
    }

    public static implicit operator (int, string) (C c)
    {
        return (c._x, c._x.ToString());
    }

    public void M(C c1)
    {
        C t = /*<bind>*/(0, null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.String)) (Syntax: '(0, null)')
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionFromTupleExpression_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C((int, string) x)
    {
        return new C(x.Item1);
    }

    public static implicit operator (int, string) (C c)
    {
        return (c._x, c._x.ToString());
    }

    public void M(C c1)
    {
        /*<bind>*/C t = (0, null);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'C t = (0, null);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C t = (0, null)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: C t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = (0, null)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (0, null)')
              IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C C.op_Implicit((System.Int32, System.String) x)) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: '(0, null)')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C C.op_Implicit((System.Int32, System.String) x))
                Operand: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.String), IsImplicit) (Syntax: '(0, null)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.String)) (Syntax: '(0, null)')
                        Elements(2):
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionToTupleType()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C((int, string) x)
    {
        return new C(x.Item1);
    }

    public static implicit operator (int, string) (C c)
    {
        return (c._x, c._x.ToString());
    }

    public void M(C c1)
    {
        (int, string) t = /*<bind>*/c1/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionToTupleType_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C((int, string) x)
    {
        return new C(x.Item1);
    }

    public static implicit operator (int, string) (C c)
    {
        return (c._x, c._x.ToString());
    }

    public void M(C c1)
    {
        /*<bind>*/(int, string) t = c1;/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: '(int, string) t = c1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: '(int, string) t = c1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int32, System.String) t) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't = c1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c1')
              IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: (System.Int32, System.String) C.op_Implicit(C c)) (OperationKind.Conversion, Type: (System.Int32, System.String), IsImplicit) (Syntax: 'c1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: (System.Int32, System.String) C.op_Implicit(C c))
                Operand: 
                  IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_InvalidConversion()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C(int value)
    {
        return new C(value);
    }

    public static implicit operator int(C c)
    {
        return (short)c._x;
    }

    public static implicit operator string(C c)
    {
        return c._x.ToString();
    }

    public void M(C c1)
    {
        (short, string) t = /*<bind>*/(new C(0), c1)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int16, System.String c1), IsInvalid) (Syntax: '(new C(0), c1)')
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsInvalid, IsImplicit) (Syntax: 'new C(0)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'new C(0)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
            Operand: 
              IObjectCreationOperation (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(0)')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32, IsInvalid) (Syntax: '0')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Initializer: 
                  null
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.String C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.String C.op_Implicit(C c))
        Operand: 
          IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C' to 'short'
                //         (short, string) t = /*<bind>*/(new C(0), c1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C(0)").WithArguments("C", "short").WithLocation(29, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_InvalidConversion_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C(int value)
    {
        return new C(value);
    }

    public static implicit operator int(C c)
    {
        return (short)c._x;
    }

    public static implicit operator string(C c)
    {
        return c._x.ToString();
    }

    public void M(C c1)
    {
        /*<bind>*/(short, string) t = (new C(0), c1);/*</bind>*/
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: '(short, str ...  C(0), c1);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: '(short, str ... w C(0), c1)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int16, System.String) t) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 't = (new C(0), c1)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (new C(0), c1)')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int16, System.String), IsInvalid, IsImplicit) (Syntax: '(new C(0), c1)')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ITupleOperation (OperationKind.Tuple, Type: (System.Int16, System.String c1), IsInvalid) (Syntax: '(new C(0), c1)')
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsInvalid, IsImplicit) (Syntax: 'new C(0)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'new C(0)')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
                              Operand: 
                                IObjectCreationOperation (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(0)')
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32, IsInvalid) (Syntax: '0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Initializer: 
                                    null
                        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.String C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.String C.op_Implicit(C c))
                          Operand: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C' to 'short'
                //         /*<bind>*/(short, string) t = (new C(0), c1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C(0)").WithArguments("C", "short").WithLocation(29, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_Deconstruction()
        {
            string source = @"
class Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}

class Class1
{
    public void M()
    {
        /*<bind>*/var (x, y) = new Point(0, 1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'var (x, y)  ... Point(0, 1)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'var (x, y)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
        Elements(2):
            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: Point..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreation, Type: Point) (Syntax: 'new Point(0, 1)')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32) (Syntax: '0')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: System.Int32) (Syntax: '1')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Initializer: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_Deconstruction_ForEach()
        {
            string source = @"
class Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Deconstruct(out uint x, out uint y)
    {
        x = 0;
        y = 0;
    }
}

class Class1
{
    public void M()
    {
        /*<bind>*/foreach (var (x, y) in new Point[]{ new Point(0, 1) })
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.UInt32 x
    Local_2: System.UInt32 y
  LoopControlVariable: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.UInt32 x, System.UInt32 y)) (Syntax: 'var (x, y)')
      ITupleOperation (OperationKind.Tuple, Type: (System.UInt32 x, System.UInt32 y)) (Syntax: '(x, y)')
        Elements(2):
            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32) (Syntax: 'x')
            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32) (Syntax: 'y')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'new Point[] ... int(0, 1) }')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: Point[]) (Syntax: 'new Point[] ... int(0, 1) }')
          Dimension Sizes(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'new Point[] ... int(0, 1) }')
          Initializer: 
            IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ new Point(0, 1) }')
              Element Values(1):
                  IObjectCreationOperation (Constructor: Point..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreation, Type: Point) (Syntax: 'new Point(0, 1)')
                    Arguments(2):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32) (Syntax: '0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: System.Int32) (Syntax: '1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer: 
                      null
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  NextVariables(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachVariableStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_DeconstructionWithConversion()
        {
            string source = @"
class Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Deconstruct(out uint x, out uint y)
    {
        x = 0;
        y = 0;
    }
}

class Class1
{
    public void M()
    {
        /*<bind>*/(uint x, uint y) = new Point(0, 1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.UInt32 x, System.UInt32 y)) (Syntax: '(uint x, ui ... Point(0, 1)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.UInt32 x, System.UInt32 y)) (Syntax: '(uint x, uint y)')
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.UInt32) (Syntax: 'uint x')
            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32) (Syntax: 'x')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.UInt32) (Syntax: 'uint y')
            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: Point..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreation, Type: Point) (Syntax: 'new Point(0, 1)')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32) (Syntax: '0')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: System.Int32) (Syntax: '1')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Initializer: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
