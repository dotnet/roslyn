// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is var y')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: System.Int32? y) (OperationKind.DeclarationPattern) (Syntax: 'var y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is int y')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: System.Int32 y) (OperationKind.DeclarationPattern) (Syntax: 'int y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is X y')
  Expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: X) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: X y) (OperationKind.DeclarationPattern) (Syntax: 'X y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is T y')
  Expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: T y) (OperationKind.DeclarationPattern) (Syntax: 'T y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x is dynamic y')
  Expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: X) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: dynamic y) (OperationKind.DeclarationPattern, IsInvalid) (Syntax: 'dynamic y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8208: It is not legal to use the type 'dynamic' in a pattern.
                //         if (/*<bind>*/x is dynamic y/*</bind>*/) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_PatternDynamicType, "dynamic").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is 12')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IConstantPattern (OperationKind.ConstantPattern) (Syntax: '12')
      Value: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is (int)12.0')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IConstantPattern (OperationKind.ConstantPattern) (Syntax: '(int)12.0')
      Value: IConversionExpression (ConversionKind.CSharp, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 12) (Syntax: '(int)12.0')
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 12) (Syntax: '12.0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is 12.0')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IConstantPattern (OperationKind.ConstantPattern) (Syntax: '12.0')
      Value: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 12) (Syntax: '12.0')
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 12) (Syntax: '12.0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'double' to 'int?'. An explicit conversion exists (are you missing a cast?)
                //         if (/*<bind>*/x is 12.0/*</bind>*/) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "12.0").WithArguments("double", "int?").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_ConstantPatternWithNoConversion()
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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x is null')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Pattern: IConstantPattern (OperationKind.ConstantPattern, IsInvalid) (Syntax: 'null')
      Value: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'null')
          ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         if (/*<bind>*/x is null/*</bind>*/) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x is UndefinedType y')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: UndefinedType y) (OperationKind.DeclarationPattern, IsInvalid) (Syntax: 'UndefinedType y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'UndefinedType' could not be found (are you missing a using directive or an assembly reference?)
                //         if (/*<bind>*/x is UndefinedType y/*</bind>*/) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UndefinedType").WithArguments("UndefinedType").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestIsPatternExpression_NonConstantExpressionInsteadOfPatternDeclaration()
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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x is y')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IConstantPattern (OperationKind.ConstantPattern, IsInvalid) (Syntax: 'y')
      Value: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0150: A constant value is expected
                //         if (/*<bind>*/x is y/*</bind>*/) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_ConstantExpected, "y").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x is X y')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: X y) (OperationKind.DeclarationPattern, IsInvalid) (Syntax: 'X y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8121: An expression of type 'int?' cannot be handled by a pattern of type 'X'.
                //         if (/*<bind>*/x is X y/*</bind>*/) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_PatternWrongType, "X").WithArguments("int?", "X").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x is int y')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: System.Int32 y) (OperationKind.DeclarationPattern, IsInvalid) (Syntax: 'int y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0128: A local variable or function named 'y' is already defined in this scope
                //         if (/*<bind>*/x is int y/*</bind>*/) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(8, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is int y2')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: System.Int32 y2) (OperationKind.DeclarationPattern) (Syntax: 'int y2')
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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x is /*</bind>*/')
  Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Pattern: IConstantPattern (OperationKind.ConstantPattern, IsInvalid) (Syntax: '')
      Value: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term 'const'
                //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "const").WithArguments("const").WithLocation(8, 39),
                // CS1026: ) expected
                //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "const").WithLocation(8, 39),
                // CS1023: Embedded statement cannot be a declaration or labeled statement
                //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "const int y").WithLocation(8, 39),
                // CS0145: A const field requires a value to be provided
                //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "y").WithLocation(8, 49),
                // CS1002: ; expected
                //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(8, 50),
                // CS1513: } expected
                //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(8, 50),
                // CS0103: The name 'y' does not exist in the current context
                //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);        
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(8, 70),
                // CS0168: The variable 'y' is declared but never used
                //         if (/*<bind>*/x is /*</bind>*/const int y) Console.WriteLine(y);        
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y").WithArguments("y").WithLocation(8, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'string.Empty is string y')
  Expression: IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'string.Empty')
  Pattern: IDeclarationPattern (Declared Symbol: System.String y) (OperationKind.DeclarationPattern) (Syntax: 'string y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1736: Default parameter value for 'x' must be a compile-time constant
                //     void M(string x = /*<bind>*/string.Empty is string y/*</bind>*/)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "string.Empty is string y").WithArguments("x").WithLocation(5, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}