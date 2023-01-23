// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IIsPatternExpression : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_VarPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12;
        if (/*<bind>*/x is var y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is var y')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var y') (InputType: System.Int32?, NarrowedType: System.Int32?, DeclaredSymbol: System.Int32? y, MatchesNull: True)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_PrimitiveTypePatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12;
        if (/*<bind>*/x is int y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is int y')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int y') (InputType: System.Int32?, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_ReferenceTypePatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M(X x)
    {
        if (/*<bind>*/x is X y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is X y')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: X) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: X, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_TypeParameterTypePatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M<T>(T x) where T: class
    {
        if (/*<bind>*/x is T y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is T y')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'T y') (InputType: T, NarrowedType: T, DeclaredSymbol: T y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_DynamicTypePatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M(X x)
    {
        if (/*<bind>*/x is dynamic y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is dynamic y')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: X) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'dynamic y') (InputType: X, NarrowedType: dynamic, DeclaredSymbol: dynamic y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8208: It is not legal to use the type 'dynamic' in a pattern.
                //         if (/*<bind>*/x is dynamic y/*</bind>*/) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_PatternDynamicType, "dynamic").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_ConstantPattern()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12, y = 12;
        if (/*<bind>*/x is 12/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is 12')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '12') (InputType: System.Int32?, NarrowedType: System.Int32)
      Value: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_ConstantPatternWithConversion()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12, y = 12;
        if (/*<bind>*/x is (int)12.0/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is (int)12.0')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '(int)12.0') (InputType: System.Int32?, NarrowedType: System.Int32)
      Value: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 12) (Syntax: '(int)12.0')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 12) (Syntax: '12.0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_ConstantPatternWithNoImplicitConversion()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12, y = 12;
        if (/*<bind>*/x is 12.0/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is 12.0')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '12.0') (InputType: System.Int32?, NarrowedType: System.Int32)
      Value: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 12, IsInvalid, IsImplicit) (Syntax: '12.0')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 12, IsInvalid) (Syntax: '12.0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'double' to 'int?'. An explicit conversion exists (are you missing a cast?)
                //         if (/*<bind>*/x is 12.0/*</bind>*/) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "12.0").WithArguments("double", "int?").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_ConstantPatternWithNoValidImplicitOrExplicitConversion()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int x = 12, y = 12;
        if (/*<bind>*/x is null/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is null')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: 'null') (InputType: System.Int32, NarrowedType: System.Int32)
      Value: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'null')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         if (/*<bind>*/x is null/*</bind>*/) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_UndefinedTypeInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12;
        if (/*<bind>*/x is UndefinedType y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is UndefinedType y')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'UndefinedType y') (InputType: System.Int32?, NarrowedType: UndefinedType, DeclaredSymbol: UndefinedType y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'UndefinedType' could not be found (are you missing a using directive or an assembly reference?)
                //         if (/*<bind>*/x is UndefinedType y/*</bind>*/) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UndefinedType").WithArguments("UndefinedType").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_InvalidConstantPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12, y = 12;
        if (/*<bind>*/x is y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is y')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'y') (InputType: System.Int32?, NarrowedType: System.Int32?)
      Value: 
        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32?, IsInvalid) (Syntax: 'y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0150: A constant value is expected
                //         if (/*<bind>*/x is y/*</bind>*/) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_ConstantExpected, "y").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_InvalidTypeInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12;
        if (/*<bind>*/x is X y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is X y')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'X y') (InputType: System.Int32?, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8121: An expression of type 'int?' cannot be handled by a pattern of type 'X'.
                //         if (/*<bind>*/x is X y/*</bind>*/) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "X").WithArguments("int?", "X").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_DuplicateLocalInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12, y = 12;
        if (/*<bind>*/x is int y/*</bind>*/) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is int y')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int y') (InputType: System.Int32?, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0128: A local variable or function named 'y' is already defined in this scope
                //         if (/*<bind>*/x is int y/*</bind>*/) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(8, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_InvalidMultipleLocalsInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12, y = 12;
        if (/*<bind>*/x is int y2/*</bind>*/, y3) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is int y2')
  Value: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32?) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int y2') (InputType: System.Int32?, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y2, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1026: ) expected
                //         if (/*<bind>*/x is int y2/*</bind>*/, y3) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ",").WithLocation(8, 45),
                // CS1525: Invalid expression term ','
                //         if (/*<bind>*/x is int y2/*</bind>*/, y3) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(8, 45),
                // CS1002: ; expected
                //         if (/*<bind>*/x is int y2/*</bind>*/, y3) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(8, 45),
                // CS1513: } expected
                //         if (/*<bind>*/x is int y2/*</bind>*/, y3) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(8, 45),
                // CS1002: ; expected
                //         if (/*<bind>*/x is int y2/*</bind>*/, y3) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(8, 49),
                // CS1513: } expected
                //         if (/*<bind>*/x is int y2/*</bind>*/, y3) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(8, 49),
                // CS0103: The name 'y3' does not exist in the current context
                //         if (/*<bind>*/x is int y2/*</bind>*/, y3) Console.WriteLine(y);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y3").WithArguments("y3").WithLocation(8, 47)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_InvalidConstDeclarationInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int x = 12;
        if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
    }
}
";
            string expectedOperationTree = @"
    IIsTypeOperation (OperationKind.IsType, Type: System.Boolean, IsInvalid) (Syntax: 'x is /*</bind>*/')
      Operand: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
      IsType: ?
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                    // file.cs(8,39): error CS1525: Invalid expression term 'const'
                    //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "const").WithArguments("const").WithLocation(8, 39),
                    // file.cs(8,39): error CS1026: ) expected
                    //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, "const").WithLocation(8, 39),
                    // file.cs(8,49): error CS0145: A const field requires a value to be provided
                    //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
                    Diagnostic(ErrorCode.ERR_ConstValueRequired, "y").WithLocation(8, 49),
                    // file.cs(8,50): error CS1002: ; expected
                    //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(8, 50),
                    // file.cs(8,50): error CS1513: } expected
                    //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
                    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(8, 50),
                    // file.cs(8,39): error CS1023: Embedded statement cannot be a declaration or labeled statement
                    //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
                    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "const int y").WithLocation(8, 39),
                    // file.cs(8,70): error CS0103: The name 'y' does not exist in the current context
                    //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(8, 70),
                    // file.cs(8,49): warning CS0168: The variable 'y' is declared but never used
                    //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "y").WithArguments("y").WithLocation(8, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_InvalidInDefaultParameterInitializer()
        {
            string source = @"
using System;
class X
{
    void M(string x = /*<bind>*/string.Empty is string y/*</bind>*/)
    {    
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'string.Empty is string y')
  Value: 
    IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
      Instance Receiver: 
        null
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'string y') (InputType: System.String, NarrowedType: System.String, DeclaredSymbol: System.String y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1736: Default parameter value for 'x' must be a compile-time constant
                //     void M(string x = /*<bind>*/string.Empty is string y/*</bind>*/)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "string.Empty is string y").WithArguments("x").WithLocation(5, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_InvalidInFieldInitializer()
        {
            string source = @"
class C
{
    private readonly static object o = 1;
    private readonly bool b = /*<bind>*/o is int x/*</bind>*/ && x >= 5;
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is int x')
  Value: 
    IFieldReferenceOperation: System.Object C.o (Static) (OperationKind.FieldReference, Type: System.Object) (Syntax: 'o')
      Instance Receiver: 
        null
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Object, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_InvalidInConstructorInitializer()
        {
            string source = @"
class C
{
    public C(object o): 
        this (/*<bind>*/o is int x/*</bind>*/ && x >= 5)
    {
    }

    public C (bool b)
    {
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is int x')
  Value: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Object, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_InvalidInAttributeArgument()
        {
            string source = @"
class A: System.Attribute
{
    public A (bool i)
    {
    }
}

[A(/*<bind>*/o is int x/*</bind>*/ && x >= 5)]
class C
{
    private const object o = 1;
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is int x')
  Value: 
    IFieldReferenceOperation: System.Object C.o (Static) (OperationKind.FieldReference, Type: System.Object, Constant: 1, IsInvalid) (Syntax: 'o')
      Instance Receiver: 
        null
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int x') (InputType: System.Object, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0134: 'C.o' is of type 'object'. A const field of a reference type other than string can only be initialized with null.
                //     private const object o = 1;
                Diagnostic(ErrorCode.ERR_NotNullConstRefField, "1").WithArguments("C.o", "object").WithLocation(12, 30),
                // CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(/*<bind>*/o is int x/*</bind>*/ && x >= 5)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "o is int x/*</bind>*/ && x >= 5").WithLocation(9, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_NoControlFlow_01()
        {
            string source = @"
class C
{
    void M(int? x, bool b, int x2, bool b2)
    /*<bind>*/{
        b = x is var y;
        b2 = x2 is 1;
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
    Locals: [System.Int32? y]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = x is var y;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = x is var y')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is var y')
                      Value: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
                      Pattern: 
                        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var y') (InputType: System.Int32?, NarrowedType: System.Int32?, DeclaredSymbol: System.Int32? y, MatchesNull: True)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b2 = x2 is 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b2 = x2 is 1')
                  Left: 
                    IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b2')
                  Right: 
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x2 is 1')
                      Value: 
                        IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x2')
                      Pattern: 
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_NoControlFlow_02()
        {
            string source = @"
class C
{
    void M((int X, int Y)? x, bool b)
    /*<bind>*/{
        b = x is (1, _) { Item1: var z } p;
    }/*</bind>*/
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            string expectedOperationTree = @"
IBlockOperation (1 statements, 2 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: (System.Int32 X, System.Int32 Y) p
    Local_2: System.Int32 z
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = x is (1 ...  var z } p;')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = x is (1 ... : var z } p')
        Left: 
          IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
        Right: 
          IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is (1, _) ... : var z } p')
            Value: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32 X, System.Int32 Y)?) (Syntax: 'x')
            Pattern: 
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '(1, _) { It ... : var z } p') (InputType: (System.Int32 X, System.Int32 Y)?, NarrowedType: (System.Int32 X, System.Int32 Y), DeclaredSymbol: (System.Int32 X, System.Int32 Y) p, MatchedType: (System.Int32 X, System.Int32 Y), DeconstructSymbol: null)
                DeconstructionSubpatterns (2):
                    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
                PropertySubpatterns (1):
                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Item1: var z')
                      Member: 
                        IFieldReferenceOperation: System.Int32 (System.Int32 X, System.Int32 Y).Item1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Item1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: (System.Int32 X, System.Int32 Y), IsImplicit) (Syntax: 'Item1')
                      Pattern: 
                        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: True)
";

            VerifyOperationTreeForTest<BlockSyntax>(compilation, expectedOperationTree);

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [(System.Int32 X, System.Int32 Y) p] [System.Int32 z]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = x is (1 ...  var z } p;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = x is (1 ... : var z } p')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is (1, _) ... : var z } p')
                      Value: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32 X, System.Int32 Y)?) (Syntax: 'x')
                      Pattern: 
                        IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '(1, _) { It ... : var z } p') (InputType: (System.Int32 X, System.Int32 Y)?, NarrowedType: (System.Int32 X, System.Int32 Y), DeclaredSymbol: (System.Int32 X, System.Int32 Y) p, MatchedType: (System.Int32 X, System.Int32 Y), DeconstructSymbol: null)
                          DeconstructionSubpatterns (2):
                              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                                Value: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                              IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.Int32, NarrowedType: System.Int32)
                          PropertySubpatterns (1):
                              IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Item1: var z')
                                Member: 
                                  IFieldReferenceOperation: System.Int32 (System.Int32 X, System.Int32 Y).Item1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Item1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: (System.Int32 X, System.Int32 Y), IsImplicit) (Syntax: 'Item1')
                                Pattern: 
                                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: True)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)";

            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedFlowGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_NoControlFlow_03()
        {
            string source = @"
class C
{
    void M((int X, (int Y, int Z))? tuple, bool b)
    /*<bind>*/{
        b = tuple is (1, _) { Item1: var x, Item2: { Y: 1, Z: var z } } p;
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
    Locals: [(System.Int32 X, (System.Int32 Y, System.Int32 Z)) p] [System.Int32 x] [System.Int32 z]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = tuple i ... ar z } } p;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = tuple i ... var z } } p')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'tuple is (1 ... var z } } p')
                      Value: 
                        IParameterReferenceOperation: tuple (OperationKind.ParameterReference, Type: (System.Int32 X, (System.Int32 Y, System.Int32 Z))?) (Syntax: 'tuple')
                      Pattern: 
                        IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '(1, _) { It ... var z } } p') (InputType: (System.Int32 X, (System.Int32 Y, System.Int32 Z))?, NarrowedType: (System.Int32 X, (System.Int32 Y, System.Int32 Z)), DeclaredSymbol: (System.Int32 X, (System.Int32 Y, System.Int32 Z)) p, MatchedType: (System.Int32 X, (System.Int32 Y, System.Int32 Z)), DeconstructSymbol: null)
                          DeconstructionSubpatterns (2):
                              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                                Value: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                              IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: (System.Int32 Y, System.Int32 Z), NarrowedType: (System.Int32 Y, System.Int32 Z))
                          PropertySubpatterns (2):
                              IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Item1: var x')
                                Member: 
                                  IFieldReferenceOperation: System.Int32 (System.Int32 X, (System.Int32 Y, System.Int32 Z)).Item1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Item1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: (System.Int32 X, (System.Int32 Y, System.Int32 Z)), IsImplicit) (Syntax: 'Item1')
                                Pattern: 
                                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var x') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: True)
                              IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Item2: { Y: ...  Z: var z }')
                                Member: 
                                  IFieldReferenceOperation: (System.Int32 Y, System.Int32 Z) (System.Int32 X, (System.Int32 Y, System.Int32 Z)).Item2 (OperationKind.FieldReference, Type: (System.Int32 Y, System.Int32 Z)) (Syntax: 'Item2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: (System.Int32 X, (System.Int32 Y, System.Int32 Z)), IsImplicit) (Syntax: 'Item2')
                                Pattern: 
                                  IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '{ Y: 1, Z: var z }') (InputType: (System.Int32 Y, System.Int32 Z), NarrowedType: (System.Int32 Y, System.Int32 Z), DeclaredSymbol: null, MatchedType: (System.Int32 Y, System.Int32 Z), DeconstructSymbol: null)
                                    DeconstructionSubpatterns (0)
                                    PropertySubpatterns (2):
                                        IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Y: 1')
                                          Member: 
                                            IFieldReferenceOperation: System.Int32 (System.Int32 Y, System.Int32 Z).Y (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Y')
                                              Instance Receiver: 
                                                IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: (System.Int32 Y, System.Int32 Z), IsImplicit) (Syntax: 'Y')
                                          Pattern: 
                                            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                                              Value: 
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                        IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Z: var z')
                                          Member: 
                                            IFieldReferenceOperation: System.Int32 (System.Int32 Y, System.Int32 Z).Z (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Z')
                                              Instance Receiver: 
                                                IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: (System.Int32 Y, System.Int32 Z), IsImplicit) (Syntax: 'Z')
                                          Pattern: 
                                            IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var z') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 z, MatchesNull: True)
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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_ControlFlowInValue()
        {
            string source = @"
class C
{
    void M(int? x1, int x2, bool b)
    /*<bind>*/{
        b = (x1 ?? x2) is var y;
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
    Locals: [System.Int32 y]
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'x1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
              Value: 
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (x1 ?? x2) is var y;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (x1 ?? x2) is var y')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Right: 
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '(x1 ?? x2) is var y')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x1 ?? x2')
                      Pattern: 
                        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var y') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y, MatchesNull: True)

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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_RecursivePattern()
        {
            string source = @"
class C
{
    void M((int X, int Y) tuple, bool b)
    {
        if (/*<bind>*/tuple is (1, 2) { Item1: int x } y/*</bind>*/) { }
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'tuple is (1 ... : int x } y')
  Value: 
    IParameterReferenceOperation: tuple (OperationKind.ParameterReference, Type: (System.Int32 X, System.Int32 Y)) (Syntax: 'tuple')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '(1, 2) { It ... : int x } y') (InputType: (System.Int32 X, System.Int32 Y), NarrowedType: (System.Int32 X, System.Int32 Y), DeclaredSymbol: (System.Int32 X, System.Int32 Y) y, MatchedType: (System.Int32 X, System.Int32 Y), DeconstructSymbol: null)
      DeconstructionSubpatterns (2):
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '2') (InputType: System.Int32, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Item1: int x')
            Member: 
              IFieldReferenceOperation: System.Int32 (System.Int32 X, System.Int32 Y).Item1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Item1')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: (System.Int32 X, System.Int32 Y), IsImplicit) (Syntax: 'Item1')
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_RecursivePatternWithNestedPropertyPatterns()
        {
            string source = @"
class C
{
    C field;
    C prop { get; }
    void M()
    {
        if (/*<bind>*/this is { prop.field: null }/*</bind>*/) { }
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'this is { p ... eld: null }')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '{ prop.field: null }') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'prop')
            Member:
              IPropertyReferenceOperation: C C.prop { get; } (OperationKind.PropertyReference, Type: C) (Syntax: 'prop')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'prop')
            Pattern:
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'prop') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
                DeconstructionSubpatterns (0)
                PropertySubpatterns (1):
                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'prop.field')
                      Member:
                        IFieldReferenceOperation: C C.field (OperationKind.FieldReference, Type: C) (Syntax: 'prop.field')
                          Instance Receiver:
                            IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'prop.field')
                      Pattern:
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: C, NarrowedType: C)
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, Constant: null, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,7): warning CS0649: Field 'C.field' is never assigned to, and will always have its default value null
                //     C field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "null").WithLocation(4, 7)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_RecursivePatternWithNestedPropertyPatterns_ControlFlow()
        {
            string source = @"
class C
{
    C field;
    C prop { get; }
    void M()
    /*<bind>*/
    {
        if (this is { prop.field: null }) { }
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'this is { p ... eld: null }')
          Value:
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
          Pattern:
            IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '{ prop.field: null }') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
              DeconstructionSubpatterns (0)
              PropertySubpatterns (1):
                  IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'prop')
                    Member:
                      IPropertyReferenceOperation: C C.prop { get; } (OperationKind.PropertyReference, Type: C) (Syntax: 'prop')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'prop')
                    Pattern:
                      IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'prop') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
                        DeconstructionSubpatterns (0)
                        PropertySubpatterns (1):
                            IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'prop.field')
                              Member:
                                IFieldReferenceOperation: C C.field (OperationKind.FieldReference, Type: C) (Syntax: 'prop.field')
                                  Instance Receiver:
                                    IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'prop.field')
                              Pattern:
                                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: C, NarrowedType: C)
                                  Value:
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, Constant: null, IsImplicit) (Syntax: 'null')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                        (ImplicitReference)
                                      Operand:
                                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1*2]
    Statements (0)
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(4,7): warning CS0649: Field 'C.field' is never assigned to, and will always have its default value null
                //     C field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "null").WithLocation(4, 7)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_RecursivePatternWithNestedPropertyPatterns_MissingMember()
        {
            string source = @"
class C
{
    void M()
    {
        if (/*<bind>*/this is { prop.field: null } y/*</bind>*/) { }
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'this is { p ... d: null } y')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '{ prop.field: null } y') (InputType: C, NarrowedType: C, DeclaredSymbol: C y, MatchedType: C, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'prop')
            Member:
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'prop')
                Children(0)
            Pattern:
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'prop') (InputType: ?, NarrowedType: ?, DeclaredSymbol: null, MatchedType: ?, DeconstructSymbol: null)
                DeconstructionSubpatterns (0)
                PropertySubpatterns (1):
                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'prop.field')
                      Member:
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'prop.field')
                          Children(0)
                      Pattern:
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: ?, NarrowedType: ?)
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                    // file.cs(6,33): error CS0117: 'C' does not contain a definition for 'prop'
                    //         if (/*<bind>*/this is { prop.field: null } y/*</bind>*/) { }
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "prop").WithArguments("C", "prop").WithLocation(6, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_RecursivePatternWithNestedPropertyPatterns_EventMember()
        {
            string source = @"
class C
{
    C prop { get; }
    public event System.Action action;
    void M()
    {
        if (/*<bind>*/this is { prop.action: null } y/*</bind>*/) { }
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'this is { p ... n: null } y')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '{ prop.action: null } y') (InputType: C, NarrowedType: C, DeclaredSymbol: C y, MatchedType: C, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'prop')
            Member:
              IPropertyReferenceOperation: C C.prop { get; } (OperationKind.PropertyReference, Type: C) (Syntax: 'prop')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'prop')
            Pattern:
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'prop') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
                DeconstructionSubpatterns (0)
                PropertySubpatterns (1):
                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'prop.action')
                      Member:
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'prop.action')
                          Children(0)
                      Pattern:
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: ?, NarrowedType: ?)
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                    // file.cs(5,32): warning CS0067: The event 'C.action' is never used
                    //     public event System.Action action;
                    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "action").WithArguments("C.action").WithLocation(5, 32),
                    // file.cs(8,38): error CS0154: The property or indexer 'action' cannot be used in this context because it lacks the get accessor
                    //         if (/*<bind>*/this is { prop.action: null } y/*</bind>*/) { }
                    Diagnostic(ErrorCode.ERR_PropertyLacksGet, "action").WithArguments("action").WithLocation(8, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_01()
        {
            string source = @"
class C
{
    void M((int X, int Y) tuple, bool b)
    {
        if (/*<bind>*/tuple is (1, 2) { NotFound: int x } y/*</bind>*/) { }
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'tuple is (1 ... : int x } y')
  Value: 
    IParameterReferenceOperation: tuple (OperationKind.ParameterReference, Type: (System.Int32 X, System.Int32 Y)) (Syntax: 'tuple')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '(1, 2) { No ... : int x } y') (InputType: (System.Int32 X, System.Int32 Y), NarrowedType: (System.Int32 X, System.Int32 Y), DeclaredSymbol: (System.Int32 X, System.Int32 Y) y, MatchedType: (System.Int32 X, System.Int32 Y), DeconstructSymbol: null)
      DeconstructionSubpatterns (2):
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '2') (InputType: System.Int32, NarrowedType: System.Int32)
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'NotFound: int x')
            Member: 
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'NotFound')
                Children(0)
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: ?, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: False)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,41): error CS0117: '(int X, int Y)' does not contain a definition for 'NotFound'
                //         if (/*<bind>*/tuple is (1, 2) { NotFound: int x } y/*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "NotFound").WithArguments("(int X, int Y)", "NotFound").WithLocation(6, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_02()
        {
            var vbSource = @"
Public Class C1
    Public Property Prop(index As Integer) As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class
";

            var vbCompilation = CreateVisualBasicCompilation(vbSource);

            var source = @"
class C
{
    void M1(object o, bool b)
    {
        b = /*<bind>*/o is C1 { Prop[1]: var x }/*</bind>*/;
    }
}";

            var compilation = CreateCompilation(source, new[] { vbCompilation.EmitToImageReference() }, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                // (6,33): error CS8400: Feature 'type pattern' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         b = /*<bind>*/o is C1 { Prop[1]: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "Prop[1]").WithArguments("type pattern", "9.0").WithLocation(6, 33),
                // (6,33): error CS8503: A property subpattern requires a reference to the property or field to be matched, e.g. '{ Name: Prop[1] }'
                //         b = /*<bind>*/o is C1 { Prop[1]: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PropertyPatternNameMissing, "Prop[1]").WithArguments("Prop[1]").WithLocation(6, 33),
                // (6,33): error CS0246: The type or namespace name 'Prop' could not be found (are you missing a using directive or an assembly reference?)
                //         b = /*<bind>*/o is C1 { Prop[1]: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Prop").WithArguments("Prop").WithLocation(6, 33),
                // (6,37): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         b = /*<bind>*/o is C1 { Prop[1]: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[1]").WithLocation(6, 37),
                // (6,40): error CS1003: Syntax error, ',' expected
                //         b = /*<bind>*/o is C1 { Prop[1]: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(6, 40),
                // (6,42): error CS1003: Syntax error, ',' expected
                //         b = /*<bind>*/o is C1 { Prop[1]: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, "var").WithArguments(",").WithLocation(6, 42));

            var expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is C1 { P ... 1]: var x }')
  Value:
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: 'C1 { Prop[1]: var x }') (InputType: System.Object, NarrowedType: C1, DeclaredSymbol: null, MatchedType: C1, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (2):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'Prop[1]')
            Member:
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'Prop[1]')
                Children(0)
            Pattern:
              ITypePatternOperation (OperationKind.TypePattern, Type: null, IsInvalid) (Syntax: 'Prop[1]') (InputType: ?, NarrowedType: Prop[], MatchedType: Prop[])
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'var x')
            Member:
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'var x')
                Children(0)
            Pattern:
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'var x') (InputType: ?, NarrowedType: ?, DeclaredSymbol: ?? x, MatchesNull: True)
";

            VerifyOperationTreeForTest<IsPatternExpressionSyntax>(compilation, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_03()
        {
            var vbSource = @"
Public Class C1
    Public Property Prop(index As Integer) As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class
";

            var vbCompilation = CreateVisualBasicCompilation(vbSource);

            var source = @"
class C
{
    void M1(object o, bool b)
    {
        b = /*<bind>*/o is C1 { Prop: var x }/*</bind>*/;
    }
}";

            var compilation = CreateCompilation(source, new[] { vbCompilation.EmitToImageReference() });
            compilation.VerifyDiagnostics(
                // (6,33): error CS0154: The property or indexer 'Prop' cannot be used in this context because it lacks the get accessor
                //         b = /*<bind>*/o is C1 { Prop: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Prop").WithArguments("Prop").WithLocation(6, 33));

            var expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is C1 { Prop: var x }')
  Value: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: 'C1 { Prop: var x }') (InputType: System.Object, NarrowedType: C1, DeclaredSymbol: null, MatchedType: C1, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'Prop: var x')
            Member: 
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Prop')
                Children(0)
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var x') (InputType: ?, NarrowedType: ?, DeclaredSymbol: ?? x, MatchesNull: True)
";

            VerifyOperationTreeForTest<IsPatternExpressionSyntax>(compilation, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_04()
        {
            var source = @"
class C
{
    void M1(object o)
    {
        _ = /*<bind>*/o is D { A: var a }/*</bind>*/;
    }
}
class D
{
    public event System.Action A;
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS0154: The property or indexer 'A' cannot be used in this context because it lacks the get accessor
                //         _ = /*<bind>*/o is D { A: var a }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "A").WithArguments("A").WithLocation(6, 32),
                // (11,32): warning CS0067: The event 'D.A' is never used
                //     public event System.Action A;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "A").WithArguments("D.A").WithLocation(11, 32)
                );

            var expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is D { A: var a }')
    Value: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
    Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: 'D { A: var a }') (InputType: System.Object, NarrowedType: D, DeclaredSymbol: null, MatchedType: D, DeconstructSymbol: null)
        DeconstructionSubpatterns (0)
        PropertySubpatterns (1):
            IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'A: var a')
            Member: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'A')
                Children(0)
            Pattern: 
                IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var a') (InputType: ?, NarrowedType: ?, DeclaredSymbol: ?? a, MatchesNull: True)
";

            VerifyOperationTreeForTest<IsPatternExpressionSyntax>(compilation, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_05()
        {
            var source = @"
class C
{
    void M1(object o)
    {
        _ = /*<bind>*/o is D { B: var b }/*</bind>*/;
    }
}
class D
{
    public void B() { }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS0154: The property or indexer 'B' cannot be used in this context because it lacks the get accessor
                //         _ = /*<bind>*/o is D { B: var b }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "B").WithArguments("B").WithLocation(6, 32)
                );

            var expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is D { B: var b }')
  Value: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: 'D { B: var b }') (InputType: System.Object, NarrowedType: D, DeclaredSymbol: null, MatchedType: D, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'B: var b')
            Member: 
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'B')
                Children(0)
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var b') (InputType: ?, NarrowedType: ?, DeclaredSymbol: ?? b, MatchesNull: True)
";

            VerifyOperationTreeForTest<IsPatternExpressionSyntax>(compilation, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_06()
        {
            var source = @"
class C
{
    void M1(object o)
    {
        _ = /*<bind>*/o is D { C: var c }/*</bind>*/;
    }
}
class D
{
    public class C { }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS0572: 'C': cannot reference a type through an expression; try 'D.C' instead
                //         _ = /*<bind>*/o is D { C: var c }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadTypeReference, "C").WithArguments("C", "D.C").WithLocation(6, 32),
                // (6,32): error CS0154: The property or indexer 'C' cannot be used in this context because it lacks the get accessor
                //         _ = /*<bind>*/o is D { C: var c }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "C").WithArguments("C").WithLocation(6, 32)
                );

            var expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is D { C: var c }')
      Value: 
        IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
      Pattern: 
        IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: 'D { C: var c }') (InputType: System.Object, NarrowedType: D, DeclaredSymbol: null, MatchedType: D, DeconstructSymbol: null)
          DeconstructionSubpatterns (0)
          PropertySubpatterns (1):
              IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'C: var c')
                Member: 
                  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'C')
                Children(0)
                Pattern: 
                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var c') (InputType: ?, NarrowedType: ?, DeclaredSymbol: ?? c, MatchesNull: True)
";

            VerifyOperationTreeForTest<IsPatternExpressionSyntax>(compilation, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_07()
        {
            var source = @"
class C
{
    void M1(object o)
    {
        _ = /*<bind>*/o is D { X: var x }/*</bind>*/;
    }
}
class D
{
    public const int X = 3;
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS0176: Member 'D.X' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = /*<bind>*/o is D { X: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "X").WithArguments("D.X").WithLocation(6, 32)
                );

            var expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is D { X: var x }')
  Value: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: 'D { X: var x }') (InputType: System.Object, NarrowedType: D, DeclaredSymbol: null, MatchedType: D, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'X: var x')
            Member: 
              IFieldReferenceOperation: System.Int32 D.X (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: 'X')
                Instance Receiver: 
                  null
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var x') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: True)
";

            VerifyOperationTreeForTest<IsPatternExpressionSyntax>(compilation, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_08()
        {
            var source = @"
class C
{
    void M1(object o)
    {
        _ = /*<bind>*/o is D { X: var x }/*</bind>*/;
    }
}
class D
{
    public static int X = 3;
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS0176: Member 'D.X' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = /*<bind>*/o is D { X: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "X").WithArguments("D.X").WithLocation(6, 32)
                );

            var expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is D { X: var x }')
  Value: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: 'D { X: var x }') (InputType: System.Object, NarrowedType: D, DeclaredSymbol: null, MatchedType: D, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'X: var x')
            Member: 
              IFieldReferenceOperation: System.Int32 D.X (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'X')
                Instance Receiver: 
                  null
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var x') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: True)
";

            VerifyOperationTreeForTest<IsPatternExpressionSyntax>(compilation, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_BadRecursivePattern_09()
        {
            var source = @"
class C
{
    void M1(object o)
    {
        _ = /*<bind>*/o is D { X: var x }/*</bind>*/;
    }
}
class D
{
    public static int X => 3;
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS0176: Member 'D.X' cannot be accessed with an instance reference; qualify it with a type name instead
                //         _ = /*<bind>*/o is D { X: var x }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "X").WithArguments("D.X").WithLocation(6, 32)
                );

            var expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o is D { X: var x }')
  Value: 
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
  Pattern: 
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: 'D { X: var x }') (InputType: System.Object, NarrowedType: D, DeclaredSymbol: null, MatchedType: D, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'X: var x')
            Member: 
              IPropertyReferenceOperation: System.Int32 D.X { get; } (Static) (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'X')
                Instance Receiver: 
                  null
            Pattern: 
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var x') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: True)
";

            VerifyOperationTreeForTest<IsPatternExpressionSyntax>(compilation, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_ControlFlowInPattern()
        {
            string source = @"
class C
{
    void M(int? x, bool b)
    /*<bind>*/
    {
        b = x is (true ? 1 : 0);
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')

        Jump if False (Regular) to Block[B3]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block [UnReachable]
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = x is (true ? 1 : 0);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = x is (true ? 1 : 0)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Right: 
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is (true ? 1 : 0)')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                      Pattern: 
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '(true ? 1 : 0)') (InputType: System.Int32?, NarrowedType: System.Int32)
                          Value: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'true ? 1 : 0')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_ControlFlowInPattern_02()
        {
            string source = @"
class C
{
    void M((int, int) x, bool b)
    /*<bind>*/
    {
        b = x is ((true ? 1 : 2), (false ? 3 : 4));
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
    CaptureIds: [0] [1] [2] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'x')

        Jump if False (Regular) to Block[B3]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block [UnReachable]
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
    Block[B5] - Block [UnReachable]
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B7]
    Block[B6] - Block
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B5] [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = x is (( ...  ? 3 : 4));')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = x is (( ... e ? 3 : 4))')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Right: 
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is ((true ... e ? 3 : 4))')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'x')
                      Pattern: 
                        IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '((true ? 1  ... e ? 3 : 4))') (InputType: (System.Int32, System.Int32), NarrowedType: (System.Int32, System.Int32), DeclaredSymbol: null, MatchedType: (System.Int32, System.Int32), DeconstructSymbol: null)
                          DeconstructionSubpatterns (2):
                              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '(true ? 1 : 2)') (InputType: System.Int32, NarrowedType: System.Int32)
                                Value: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'true ? 1 : 2')
                              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '(false ? 3 : 4)') (InputType: System.Int32, NarrowedType: System.Int32)
                                Value: 
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 4, IsImplicit) (Syntax: 'false ? 3 : 4')
                          PropertySubpatterns (0)

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.Patterns)]
        [Fact]
        public void IsPattern_ControlFlowInValueAndPattern()
        {
            string source = @"
class C
{
    void M(int? x1, int x2, bool b)
    /*<bind>*/
    {
        b = (x1 ?? x2) is (true ? 1 : 0);
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
    CaptureIds: [0] [2] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'x1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
              Value: 
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (0)
        Jump if False (Regular) to Block[B7]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B8]
    Block[B7] - Block [UnReachable]
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (x1 ??  ... e ? 1 : 0);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (x1 ??  ... ue ? 1 : 0)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Right: 
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: '(x1 ?? x2)  ... ue ? 1 : 0)')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x1 ?? x2')
                      Pattern: 
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '(true ? 1 : 0)') (InputType: System.Int32, NarrowedType: System.Int32)
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'true ? 1 : 0')

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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_PatternCombinatorsAndRelationalPatterns_01()
        {
            string source = @"
class X
{
    void M(char c)
    {
        _ = /*<bind>*/c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'c is (>= 'A ... and <= 'z')')
      Value: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'c')
      Pattern: 
        IBinaryPatternOperation (BinaryOperatorKind.Or) (OperationKind.BinaryPattern, Type: null) (Syntax: '(>= 'A' and ... and <= 'z')') (InputType: System.Char, NarrowedType: System.Char)
          LeftPattern: 
            IBinaryPatternOperation (BinaryOperatorKind.And) (OperationKind.BinaryPattern, Type: null) (Syntax: '>= 'A' and <= 'Z'') (InputType: System.Char, NarrowedType: System.Char)
              LeftPattern: 
                IRelationalPatternOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '>= 'A'') (InputType: System.Char, NarrowedType: System.Char)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: A) (Syntax: ''A'')
              RightPattern: 
                IRelationalPatternOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '<= 'Z'') (InputType: System.Char, NarrowedType: System.Char)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: Z) (Syntax: ''Z'')
          RightPattern: 
            IBinaryPatternOperation (BinaryOperatorKind.And) (OperationKind.BinaryPattern, Type: null) (Syntax: '>= 'a' and <= 'z'') (InputType: System.Char, NarrowedType: System.Char)
              LeftPattern: 
                IRelationalPatternOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '>= 'a'') (InputType: System.Char, NarrowedType: System.Char)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: a) (Syntax: ''a'')
              RightPattern: 
                IRelationalPatternOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.RelationalPattern, Type: null) (Syntax: '<= 'z'') (InputType: System.Char, NarrowedType: System.Char)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: z) (Syntax: ''z'')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularWithPatternCombinators);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_TypePatterns_01()
        {
            string source = @"
class X
{
    void M(object o)
    {
        _ = /*<bind>*/o is int or long or bool/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is int or long or bool')
      Value: 
        IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
      Pattern: 
        IBinaryPatternOperation (BinaryOperatorKind.Or) (OperationKind.BinaryPattern, Type: null) (Syntax: 'int or long or bool') (InputType: System.Object, NarrowedType: System.Object)
          LeftPattern: 
            IBinaryPatternOperation (BinaryOperatorKind.Or) (OperationKind.BinaryPattern, Type: null) (Syntax: 'int or long') (InputType: System.Object, NarrowedType: System.Object)
              LeftPattern: 
                ITypePatternOperation (OperationKind.TypePattern, Type: null) (Syntax: 'int') (InputType: System.Object, NarrowedType: System.Int32, MatchedType: System.Int32)
              RightPattern: 
                ITypePatternOperation (OperationKind.TypePattern, Type: null) (Syntax: 'long') (InputType: System.Object, NarrowedType: System.Int64, MatchedType: System.Int64)
          RightPattern: 
            ITypePatternOperation (OperationKind.TypePattern, Type: null) (Syntax: 'bool') (InputType: System.Object, NarrowedType: System.Boolean, MatchedType: System.Boolean)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularWithPatternCombinators);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_Array_01()
        {
            string source = @"
class X
{
    void M(int[] o)
    {
        _ = /*<bind>*/o is [42, ..]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is [42, ..]')
  Value:
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'o')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[42, ..]') (InputType: System.Int32[], NarrowedType: System.Int32[], DeclaredSymbol: null, LengthSymbol: System.Int32 System.Array.Length { get; }, IndexerSymbol: null)
      Patterns (2):
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '42') (InputType: System.Int32, NarrowedType: System.Int32)
            Value:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
          ISlicePatternOperation (OperationKind.SlicePattern, Type: null) (Syntax: '..') (InputType: System.Int32[], NarrowedType: System.Int32[], SliceSymbol: null
            Pattern:
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndex(source);
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_Array_02()
        {
            string source = @"
class X
{
    void M(int[] o)
    {
        _ = /*<bind>*/o is [42, .. var slice] list/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is [42, . ... slice] list')
  Value:
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'o')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[42, .. var slice] list') (InputType: System.Int32[], NarrowedType: System.Int32[], DeclaredSymbol: System.Int32[] list, LengthSymbol: System.Int32 System.Array.Length { get; }, IndexerSymbol: null)
      Patterns (2):
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '42') (InputType: System.Int32, NarrowedType: System.Int32)
            Value:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
          ISlicePatternOperation (OperationKind.SlicePattern, Type: null) (Syntax: '.. var slice') (InputType: System.Int32[], NarrowedType: System.Int32[], SliceSymbol: null
            Pattern:
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var slice') (InputType: System.Int32[], NarrowedType: System.Int32[], DeclaredSymbol: System.Int32[]? slice, MatchesNull: True)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_Array_Nullability()
        {
            string source = @"
#nullable enable
class X
{
    void M(string s)
    {
        s = null;
        var a = new[] { s };
        _ = /*<bind>*/a is [var item]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'a is [var item]')
  Value:
    ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.String?[]) (Syntax: 'a')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[var item]') (InputType: System.String[], NarrowedType: System.String[], DeclaredSymbol: null, LengthSymbol: System.Int32 System.Array.Length { get; }, IndexerSymbol: null)
      Patterns (1):
          IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var item') (InputType: System.String, NarrowedType: System.String, DeclaredSymbol: System.String? item, MatchesNull: True)
";
            var expectedDiagnostics = new[]
            {
                // (7,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         s = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(7, 13)
            };

            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(57884, "https://github.com/dotnet/roslyn/issues/57884")]
        public void TestIsPatternExpression_ListPatterns_Collection_Nullability()
        {
            string source = @"
#nullable enable
class X<T>
{
    static X<U> Create<U>(U u) => throw null!;
    void M(string s)
    {
        s = null;
        var a = Create(s);
        _ = /*<bind>*/a is [var item]/*</bind>*/;
    }
    public int Length => throw null!;
    public char this[int i] => throw null!;
}
";
            // The LengthSymbol and IndexerSymbol should reflect updated nullability:
            // LengthSymbol: System.Int32 X<System.String?>.Length { get; }, IndexerSymbol: System.Char X<System.String?>.this[System.Int32 i] { get; })
            // Tracked by https://github.com/dotnet/roslyn/issues/57884
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'a is [var item]')
  Value:
    ILocalReferenceOperation: a (OperationKind.LocalReference, Type: X<System.String?>) (Syntax: 'a')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[var item]') (InputType: X<System.String>, NarrowedType: X<System.String>, DeclaredSymbol: null, LengthSymbol: System.Int32 X<System.String>.Length { get; }, IndexerSymbol: System.Char X<System.String>.this[System.Int32 i] { get; })
      Patterns (1):
          IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var item') (InputType: System.Char, NarrowedType: System.Char, DeclaredSymbol: System.Char item, MatchesNull: True)
";
            var expectedDiagnostics = new[]
            {
                // (8,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         s = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(8, 13)
            };

            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_Span_01()
        {
            string source = @"
class X
{
    void M(System.Span<int> o)
    {
        _ = /*<bind>*/o is [.., 42]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is [.., 42]')
  Value:
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Span<System.Int32>) (Syntax: 'o')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[.., 42]') (InputType: System.Span<System.Int32>, NarrowedType: System.Span<System.Int32>, DeclaredSymbol: null, LengthSymbol: System.Int32 System.Span<System.Int32>.Length { get; }, IndexerSymbol: ref System.Int32 System.Span<System.Int32>.this[System.Int32 i] { get; })
      Patterns (2):
          ISlicePatternOperation (OperationKind.SlicePattern, Type: null) (Syntax: '..') (InputType: System.Span<System.Int32>, NarrowedType: System.Span<System.Int32>, SliceSymbol: null
            Pattern:
              null
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '42') (InputType: System.Int32, NarrowedType: System.Int32)
            Value:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_Span_02()
        {
            string source = @"
class X
{
    void M(System.Span<int> o)
    {
        _ = /*<bind>*/o is [.. var slice, 42] list/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is [.. va ... e, 42] list')
  Value:
    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Span<System.Int32>) (Syntax: 'o')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[.. var slice, 42] list') (InputType: System.Span<System.Int32>, NarrowedType: System.Span<System.Int32>, DeclaredSymbol: System.Span<System.Int32> list, LengthSymbol: System.Int32 System.Span<System.Int32>.Length { get; }, IndexerSymbol: ref System.Int32 System.Span<System.Int32>.this[System.Int32 i] { get; })
      Patterns (2):
          ISlicePatternOperation (OperationKind.SlicePattern, Type: null) (Syntax: '.. var slice') (InputType: System.Span<System.Int32>, NarrowedType: System.Span<System.Int32>, SliceSymbol: System.Span<System.Int32> System.Span<System.Int32>.Slice(System.Int32 offset, System.Int32 length)
            Pattern:
              IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var slice') (InputType: System.Span<System.Int32>, NarrowedType: System.Span<System.Int32>, DeclaredSymbol: System.Span<System.Int32> slice, MatchesNull: True)
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '42') (InputType: System.Int32, NarrowedType: System.Int32)
            Value:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRangeAndSpan(source);
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_MissingMember_Length()
        {
            string source = @"
class X
{
    public int this[int i] => throw null;
    
    void M()
    {
        _ = /*<bind>*/this is []/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'this is []')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: X) (Syntax: 'this')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null, IsInvalid) (Syntax: '[]') (InputType: X, NarrowedType: X, DeclaredSymbol: null, LengthSymbol: null, IndexerSymbol: System.Int32 X.this[System.Int32 i] { get; })
      Patterns (0)
";
            var expectedDiagnostics = new[]
            {
                // (8,31): error CS8985: List patterns may not be used for a value of type 'X'. No suitable 'Length' or 'Count' property was found.
                //         _ = /*<bind>*/this is []/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("X").WithLocation(8, 31),
                // (8,31): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         _ = /*<bind>*/this is []/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(8, 31)
            };

            var comp = CreateCompilation(source);
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_MissingMember_Indexer()
        {
            string source = @"
class X
{
    public int Count { get; }
    
    void M()
    {
        _ = /*<bind>*/this is []/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'this is []')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: X) (Syntax: 'this')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null, IsInvalid) (Syntax: '[]') (InputType: X, NarrowedType: X, DeclaredSymbol: null, LengthSymbol: System.Int32 X.Count { get; }, IndexerSymbol: null)
      Patterns (0)
";
            var expectedDiagnostics = new[]
            {
                // (8,31): error CS0021: Cannot apply indexing with [] to an expression of type 'X'
                //         _ = /*<bind>*/this is []/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("X").WithLocation(8, 31)
            };

            var comp = CreateCompilationWithIndex(source);
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_MissingMember_Slice()
        {
            string source = @"
class X
{
    public int this[int i] => throw null;
    public int Count => throw null;

    void M()
    {
        _ = /*<bind>*/this is [.. 0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'this is [.. 0]')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: X) (Syntax: 'this')
  Pattern:
    IListPatternOperation (OperationKind.ListPattern, Type: null, IsInvalid) (Syntax: '[.. 0]') (InputType: X, NarrowedType: X, DeclaredSymbol: null, LengthSymbol: System.Int32 X.Count { get; }, IndexerSymbol: System.Int32 X.this[System.Int32 i] { get; })
      Patterns (1):
          ISlicePatternOperation (OperationKind.SlicePattern, Type: null, IsInvalid) (Syntax: '.. 0') (InputType: X, NarrowedType: X, SliceSymbol: System.Int32 X.this[System.Int32 i] { get; }
            Pattern:
              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '0') (InputType: System.Int32, NarrowedType: System.Int32)
                Value:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
";
            var expectedDiagnostics = new[]
            {
                // (9,32): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = /*<bind>*/this is [.. 0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgType, ".. 0").WithArguments("1", "System.Range", "int").WithLocation(9, 32)
            };

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_ErrorCases_01()
        {
            string source = @"
class X
{
    void M(int[] a)
    {
        _ = /*<bind>*/a is ../*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'a is ..')
  Value:
    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
  Pattern:
    ISlicePatternOperation (OperationKind.SlicePattern, Type: null, IsInvalid) (Syntax: '..') (InputType: System.Int32[], NarrowedType: System.Int32[], SliceSymbol: null
      Pattern:
        null
";
            var expectedDiagnostics = new[]
            {
                // (6,28): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
                //         _ = /*<bind>*/a is ../*</bind>*/;
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(6, 28)
            };

            var comp = CreateCompilation(source);
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_ErrorCases_02()
        {
            string source = @"
class X
{
    void M(int[] a)
    {
        _ = /*<bind>*/a is .. 42/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'a is .. 42')
  Value:
    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
  Pattern:
    ISlicePatternOperation (OperationKind.SlicePattern, Type: null, IsInvalid) (Syntax: '.. 42') (InputType: System.Int32[], NarrowedType: System.Int32[], SliceSymbol: null
      Pattern:
        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '42') (InputType: System.Int32[], NarrowedType: System.Int32[])
          Value:
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32[], IsInvalid, IsImplicit) (Syntax: '42')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
";
            var expectedDiagnostics = new[]
            {
                // (6,28): error CS9202: Slice patterns may only be used once and directly inside a list pattern.
                //         _ = /*<bind>*/a is .. 42/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, ".. 42").WithLocation(6, 28),
                // (6,31): error CS0029: Cannot implicitly convert type 'int' to 'int[]'
                //         _ = /*<bind>*/a is .. 42/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "42").WithArguments("int", "int[]").WithLocation(6, 31)
            };

            var comp = CreateCompilation(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray });
            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsPatternExpression_ListPatterns_ControlFlow_01()
        {
            string source = @"
class C
{
    void M(int[] o)
    /*<bind>*/
    {
        if (o is [1, .. var slice, 2]) { }
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
    Locals: [System.Int32[]? slice]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B2]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is [1, .. ... r slice, 2]')
              Value:
                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'o')
              Pattern:
                IListPatternOperation (OperationKind.ListPattern, Type: null) (Syntax: '[1, .. var slice, 2]') (InputType: System.Int32[], NarrowedType: System.Int32[], DeclaredSymbol: null, LengthSymbol: System.Int32 System.Array.Length { get; }, IndexerSymbol: null)
                  Patterns (3):
                      IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Int32, NarrowedType: System.Int32)
                        Value:
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ISlicePatternOperation (OperationKind.SlicePattern, Type: null) (Syntax: '.. var slice') (InputType: System.Int32[], NarrowedType: System.Int32[], SliceSymbol: null
                        Pattern:
                          IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var slice') (InputType: System.Int32[], NarrowedType: System.Int32[], DeclaredSymbol: System.Int32[]? slice, MatchesNull: True)
                      IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '2') (InputType: System.Int32, NarrowedType: System.Int32)
                        Value:
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Leaving: {R1}
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1*2]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { source, TestSources.Index, TestSources.Range, TestSources.GetSubArray },
                expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
        }
    }
}
