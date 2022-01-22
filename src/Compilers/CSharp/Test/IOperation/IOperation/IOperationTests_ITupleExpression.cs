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
    public class IOperationTests_ITupleExpression : SemanticModelTestBase
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
  NaturalType: (System.Int32, System.Int32)
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
                NaturalType: (System.Int32, System.Int32)
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
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
  NaturalType: (System.Int32, System.Int32)
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
              ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
                NaturalType: (System.Int32, System.Int32)
                Elements(2):
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 2, IsImplicit) (Syntax: '2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversionsWithTypedExpression_01()
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
  NaturalType: (System.Int32 a, System.Int32 b)
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
        public void TupleExpression_ImplicitConversionsWithTypedExpression_02()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        int a = 1;
        int b = 2;
        (long, long) t /*<bind>*/= (a, b)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (a, b)')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int64, System.Int64), IsImplicit) (Syntax: '(a, b)')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ITupleOperation (OperationKind.Tuple, Type: (System.Int64 a, System.Int64 b)) (Syntax: '(a, b)')
        NaturalType: (System.Int32 a, System.Int32 b)
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

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversionsWithTypedExpression_03()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        int a = 1;
        int b = 2;
        (long a, long b) t /*<bind>*/= (a, b)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (a, b)')
  ITupleOperation (OperationKind.Tuple, Type: (System.Int64 a, System.Int64 b)) (Syntax: '(a, b)')
    NaturalType: (System.Int32 a, System.Int32 b)
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

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
                    NaturalType: (System.Int32 a, System.Int32 b)
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'a')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'b')
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
  NaturalType: null
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
              ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.String)) (Syntax: '(1, null)')
                NaturalType: null
                Elements(2):
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
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
  NaturalType: (System.Int32 A, System.Int32 B)
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
                NaturalType: (System.Int32 A, System.Int32 B)
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
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
  NaturalType: (System.Int32, System.Int32)
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
                    NaturalType: (System.Int32, System.Int32)
                    Elements(2):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
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
  NaturalType: null
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
                    NaturalType: null
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, Constant: 1, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
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
  NaturalType: (C, C c1)
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int16 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'new C(0)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int16 C.op_Implicit(C c))
        Operand: 
          IObjectCreationOperation (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0)')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0')
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
                    NaturalType: (C, C c1)
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int16 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'new C(0)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int16 C.op_Implicit(C c))
                          Operand: 
                            IObjectCreationOperation (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0)')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Initializer: 
                                null
                        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.String C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.String C.op_Implicit(C c))
                          Operand: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
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
  NaturalType: null
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
                  ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.String)) (Syntax: '(0, null)')
                    NaturalType: null
                    Elements(2):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
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
  NaturalType: (C, C c1)
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsInvalid, IsImplicit) (Syntax: 'new C(0)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'new C(0)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
            Operand: 
              IObjectCreationOperation (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(0)')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '0')
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
                    NaturalType: (C, C c1)
                    Elements(2):
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsInvalid, IsImplicit) (Syntax: 'new C(0)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'new C(0)')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
                              Operand: 
                                IObjectCreationOperation (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(0)')
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '0')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Initializer: 
                                    null
                        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.String C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.String C.op_Implicit(C c))
                          Operand: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
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
        NaturalType: (System.Int32 x, System.Int32 y)
        Elements(2):
            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: Point..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreation, Type: Point) (Syntax: 'new Point(0, 1)')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: '1')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.UInt32 x
    Local_2: System.UInt32 y
  LoopControlVariable: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.UInt32 x, System.UInt32 y)) (Syntax: 'var (x, y)')
      ITupleOperation (OperationKind.Tuple, Type: (System.UInt32 x, System.UInt32 y)) (Syntax: '(x, y)')
        NaturalType: (System.UInt32 x, System.UInt32 y)
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
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: '1')
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
      NaturalType: (System.UInt32 x, System.UInt32 y)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.UInt32) (Syntax: 'uint x')
            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32) (Syntax: 'x')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.UInt32) (Syntax: 'uint y')
            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: Point..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreation, Type: Point) (Syntax: 'new Point(0, 1)')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: '1')
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
        [Fact]
        public void ExplicitConversionOfANestedTuple_01()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        var t = /*<bind>*/((int, (long c, int d)))(1, (a: 2, b: 3))/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, (System.Int64 c, System.Int32 d))) (Syntax: '((int, (lon ... : 2, b: 3))')
  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int64 c, System.Int32 d))) (Syntax: '(1, (a: 2, b: 3))')
      NaturalType: (System.Int32, (System.Int32 a, System.Int32 b))
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int64 c, System.Int32 d), IsImplicit) (Syntax: '(a: 2, b: 3)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ITupleOperation (OperationKind.Tuple, Type: (System.Int64 a, System.Int32 b)) (Syntax: '(a: 2, b: 3)')
                NaturalType: (System.Int32 a, System.Int32 b)
                Elements(2):
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 2, IsImplicit) (Syntax: '2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (8,56): warning CS8123: The tuple element name 'a' is ignored because a different name or no name is specified by the target type '(long c, int d)'.
                //         var t = /*<bind>*/((int, (long c, int d)))(1, (a: 2, b: 3))/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "a: 2").WithArguments("a", "(long c, int d)").WithLocation(8, 56),
                // (8,62): warning CS8123: The tuple element name 'b' is ignored because a different name or no name is specified by the target type '(long c, int d)'.
                //         var t = /*<bind>*/((int, (long c, int d)))(1, (a: 2, b: 3))/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "b: 3").WithArguments("b", "(long c, int d)").WithLocation(8, 62)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TupleFlow_01()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        (int, int) t = (1, b ? 2 : 3);
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 't' is assigned but its value is never used
                //         (int, int) t = (1, b ? 2 : 3);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t").WithArguments("t").WithLocation(6, 20)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [(System.Int32, System.Int32) t]
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 't = (1, b ? 2 : 3)')
              Left: 
                ILocalReferenceOperation: t (IsDeclaration: True) (OperationKind.LocalReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 't = (1, b ? 2 : 3)')
              Right: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, b ? 2 : 3)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')

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
        public void TupleFlow_02()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        var t = (1, (2, b ? 2 : 3));
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 't' is assigned but its value is never used
                //         var t = (1, (2, b ? 2 : 3));
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t").WithArguments("t").WithLocation(6, 13)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [(System.Int32, (System.Int32, System.Int32)) t]
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: (System.Int32, (System.Int32, System.Int32)), IsImplicit) (Syntax: 't = (1, (2, b ? 2 : 3))')
              Left: 
                ILocalReferenceOperation: t (IsDeclaration: True) (OperationKind.LocalReference, Type: (System.Int32, (System.Int32, System.Int32)), IsImplicit) (Syntax: 't = (1, (2, b ? 2 : 3))')
              Right: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(1, (2, b ? 2 : 3))')
                  NaturalType: (System.Int32, (System.Int32, System.Int32))
                  Elements(2):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(2, b ? 2 : 3)')
                        NaturalType: (System.Int32, System.Int32)
                        Elements(2):
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')

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
        public void TupleFlow_03()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        M2((1, b ? 1 : 2));
    }/*</bind>*/

    void M2((int, int) arg) { }
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2((1, b ? 1 : 2));')
              Expression: 
                IInvocationOperation ( void C.M2((System.Int32, System.Int32) arg)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2((1, b ? 1 : 2))')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'M2')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: '(1, b ? 1 : 2)')
                        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, b ? 1 : 2)')
                          NaturalType: (System.Int32, System.Int32)
                          Elements(2):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 1 : 2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        public void TupleFlow_04()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2, int i3)
    /*<bind>*/{
        (i1, b ? i2 : i3) = (1, 2);
    }/*</bind>*/

    void M2((int, int) arg) { }
}

";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (i1, b ? i2 : i3) = (1, 2);
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "b ? i2 : i3").WithLocation(6, 14)
            };

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i3')
              Value: 
                IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(i1, b ? i2 ... ) = (1, 2);')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, System.Int32), IsInvalid) (Syntax: '(i1, b ? i2 ... 3) = (1, 2)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32), IsInvalid) (Syntax: '(i1, b ? i2 : i3)')
                      NaturalType: (System.Int32 i1, System.Int32)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b ? i2 : i3')
                            Children(1):
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b ? i2 : i3')
                  Right: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                      NaturalType: (System.Int32, System.Int32)
                      Elements(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

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
        public void TupleFlow_05()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2, int i3)
    /*<bind>*/{
        (i1, b ? ref i2 : ref i3) = (1, 2);
    }/*</bind>*/

    void M2((int, int) arg) { }
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
              Value: 
                IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(i1, b ? re ... ) = (1, 2);')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, System.Int32)) (Syntax: '(i1, b ? re ... 3) = (1, 2)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32)) (Syntax: '(i1, b ? re ... 2 : ref i3)')
                      NaturalType: (System.Int32 i1, System.Int32)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? ref i2 : ref i3')
                  Right: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                      NaturalType: (System.Int32, System.Int32)
                      Elements(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

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
        public void TupleFlow_06()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        (int, int) t = (b ? 2 : 3, 1);
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 't' is assigned but its value is never used
                //         (int, int) t = (b ? 2 : 3, 1);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t").WithArguments("t").WithLocation(6, 20)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [(System.Int32, System.Int32) t]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 't = (b ? 2 : 3, 1)')
              Left: 
                ILocalReferenceOperation: t (IsDeclaration: True) (OperationKind.LocalReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 't = (b ? 2 : 3, 1)')
              Right: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(b ? 2 : 3, 1)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

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
        public void TupleFlow_07()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2, int i3, int i4, int i5, int i6)
    /*<bind>*/{
        (b ? (i1, i2) : (i3, i4)) = (i5, i6);
    }/*</bind>*/
}

";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (b ? (i1, i2) : (i3, i4)) = (i5, i6);
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "b ? (i1, i2) : (i3, i4)").WithLocation(6, 10)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '(i1, i2)')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.Int32), IsInvalid, IsImplicit) (Syntax: '(i1, i2)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Identity)
                  Operand: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2), IsInvalid) (Syntax: '(i1, i2)')
                      NaturalType: (System.Int32 i1, System.Int32 i2)
                      Elements(2):
                          IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
                          IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '(i3, i4)')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.Int32), IsInvalid, IsImplicit) (Syntax: '(i3, i4)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Identity)
                  Operand: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i3, System.Int32 i4), IsInvalid) (Syntax: '(i3, i4)')
                      NaturalType: (System.Int32 i3, System.Int32 i4)
                      Elements(2):
                          IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i3')
                          IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i4')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(b ? (i1, i ... = (i5, i6);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: '(b ? (i1, i ...  = (i5, i6)')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: (System.Int32, System.Int32), IsInvalid, IsImplicit) (Syntax: 'b ? (i1, i2) : (i3, i4)')
                      Children(1):
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsInvalid, IsImplicit) (Syntax: 'b ? (i1, i2) : (i3, i4)')
                  Right: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i5, System.Int32 i6)) (Syntax: '(i5, i6)')
                      NaturalType: (System.Int32 i5, System.Int32 i6)
                      Elements(2):
                          IParameterReferenceOperation: i5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i5')
                          IParameterReferenceOperation: i6 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i6')

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
        public void TupleFlow_08()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        (b ? (var i1, var i2) : (var i3, var i4)) = (1, 2);
    }/*</bind>*/
}

";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8185: A declaration is not allowed in this context.
                //         (b ? (var i1, var i2) : (var i3, var i4)) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var i1").WithLocation(6, 15),
                // CS8185: A declaration is not allowed in this context.
                //         (b ? (var i1, var i2) : (var i3, var i4)) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var i2").WithLocation(6, 23),
                // CS8185: A declaration is not allowed in this context.
                //         (b ? (var i1, var i2) : (var i3, var i4)) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var i3").WithLocation(6, 34),
                // CS8185: A declaration is not allowed in this context.
                //         (b ? (var i1, var i2) : (var i3, var i4)) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var i4").WithLocation(6, 42),
                // CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (b ? (var i1, var i2) : (var i3, var i4)) = (1, 2);
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "b ? (var i1, var i2) : (var i3, var i4)").WithLocation(6, 10)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [var i1] [var i2] [var i3] [var i4]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '(var i1, var i2)')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (var, var), IsInvalid, IsImplicit) (Syntax: '(var i1, var i2)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Identity)
                  Operand: 
                    ITupleOperation (OperationKind.Tuple, Type: (var i1, var i2), IsInvalid) (Syntax: '(var i1, var i2)')
                      NaturalType: (var i1, var i2)
                      Elements(2):
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var, IsInvalid) (Syntax: 'var i1')
                            ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'i1')
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var, IsInvalid) (Syntax: 'var i2')
                            ILocalReferenceOperation: i2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'i2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '(var i3, var i4)')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (var, var), IsInvalid, IsImplicit) (Syntax: '(var i3, var i4)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Identity)
                  Operand: 
                    ITupleOperation (OperationKind.Tuple, Type: (var i3, var i4), IsInvalid) (Syntax: '(var i3, var i4)')
                      NaturalType: (var i3, var i4)
                      Elements(2):
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var, IsInvalid) (Syntax: 'var i3')
                            ILocalReferenceOperation: i3 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'i3')
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var, IsInvalid) (Syntax: 'var i4')
                            ILocalReferenceOperation: i4 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'i4')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(b ? (var i ... ) = (1, 2);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: (var, var), IsInvalid) (Syntax: '(b ? (var i ... )) = (1, 2)')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: (var, var), IsInvalid, IsImplicit) (Syntax: 'b ? (var i1 ... i3, var i4)')
                      Children(1):
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: (var, var), IsInvalid, IsImplicit) (Syntax: 'b ? (var i1 ... i3, var i4)')
                  Right: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                      NaturalType: (System.Int32, System.Int32)
                      Elements(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

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
        public void TupleFlow_09()
        {
            // Nested tuple, no control flow.
            string source = @"
class C
{
    void M(int i1, int i2, int i3, ((int, int), int) result)
    /*<bind>*/
    {
        result = ((i1, i2), i3);
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = ((i1, i2), i3);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ((System.Int32, System.Int32), System.Int32)) (Syntax: 'result = ((i1, i2), i3)')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: ((System.Int32, System.Int32), System.Int32)) (Syntax: 'result')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ((System.Int32, System.Int32), System.Int32), IsImplicit) (Syntax: '((i1, i2), i3)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Identity)
                  Operand: 
                    ITupleOperation (OperationKind.Tuple, Type: ((System.Int32 i1, System.Int32 i2), System.Int32 i3)) (Syntax: '((i1, i2), i3)')
                      NaturalType: ((System.Int32 i1, System.Int32 i2), System.Int32 i3)
                      Elements(2):
                          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
                            NaturalType: (System.Int32 i1, System.Int32 i2)
                            Elements(2):
                                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')
                                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')
                          IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TupleFlow_10()
        {
            // Nested tuple, verify control flow does not spill sibling tuple.
            string source = @"
class C
{
    void M(int i1, int i2, int? i3, int i4, ((int, int), int) result)
    /*<bind>*/
    {
        result = ((i1, i2), i3 ?? i4);
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
    CaptureIds: [0] [1] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: ((System.Int32, System.Int32), System.Int32)) (Syntax: 'result')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                  Value: 
                    IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i3')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i3')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i3')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i4')
              Value: 
                IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i4')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (( ...  i3 ?? i4);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ((System.Int32, System.Int32), System.Int32)) (Syntax: 'result = (( ... , i3 ?? i4)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ((System.Int32, System.Int32), System.Int32), IsImplicit) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ((System.Int32, System.Int32), System.Int32), IsImplicit) (Syntax: '((i1, i2), i3 ?? i4)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        ITupleOperation (OperationKind.Tuple, Type: ((System.Int32 i1, System.Int32 i2), System.Int32)) (Syntax: '((i1, i2), i3 ?? i4)')
                          NaturalType: ((System.Int32 i1, System.Int32 i2), System.Int32)
                          Elements(2):
                              ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
                                NaturalType: (System.Int32 i1, System.Int32 i2)
                                Elements(2):
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3 ?? i4')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void UnexpectedTupleExpressionRecovery()
        {
            var source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(var ((a,b), c), int d);/*</bind>*/
    }
}";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,19): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         /*<bind>*/(var ((a,b), c), int d);/*</bind>*/
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((a,b), c), int d)").WithLocation(6, 19),
                // file.cs(6,20): error CS8185: A declaration is not allowed in this context.
                //         /*<bind>*/(var ((a,b), c), int d);/*</bind>*/
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((a,b), c)").WithLocation(6, 20),
                // file.cs(6,36): error CS8185: A declaration is not allowed in this context.
                //         /*<bind>*/(var ((a,b), c), int d);/*</bind>*/
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(6, 36),
                // file.cs(6,36): error CS0165: Use of unassigned local variable 'd'
                //         /*<bind>*/(var ((a,b), c), int d);/*</bind>*/
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int d").WithArguments("d").WithLocation(6, 36)
            };

            var expectedTree = @"
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(var ((a,b), c), int d);')
  Expression:
    ITupleOperation (OperationKind.Tuple, Type: (((var a, var b), var c), System.Int32 d), IsInvalid) (Syntax: '(var ((a,b), c), int d)')
      NaturalType: (((var a, var b), var c), System.Int32 d)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: ((var a, var b), var c), IsInvalid) (Syntax: 'var ((a,b), c)')
            ITupleOperation (OperationKind.Tuple, Type: ((var a, var b), var c), IsInvalid) (Syntax: '((a,b), c)')
              NaturalType: null
              Elements(2):
                  ITupleOperation (OperationKind.Tuple, Type: (var a, var b), IsInvalid) (Syntax: '(a,b)')
                    NaturalType: null
                    Elements(2):
                        ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'a')
                        ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'b')
                  ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'c')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'int d')
            ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'd')
";

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(source, expectedTree, expectedDiagnostics);
        }
    }
}
