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
    public class IOperationTests_IPatternSwitchCase : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_VarPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12;
        switch (x)
        {
            /*<bind>*/case var y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case var y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var y') (InputType: System.Int32?, NarrowedType: System.Int32?, DeclaredSymbol: System.Int32? y, MatchesNull: True)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_PrimitiveTypePatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M()
    {
        int? x = 12;
        switch (x)
        {
            /*<bind>*/case int y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case int y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int y') (InputType: System.Int32?, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_ReferenceTypePatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            /*<bind>*/case X y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case X y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_TypeParameterTypePatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M<T>(object x) where T : class
    {
        switch (x)
        {
            /*<bind>*/case T y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case T y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'T y') (InputType: System.Object, NarrowedType: T, DeclaredSymbol: T y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_DynamicTypePatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M<T>(object x) where T : class
    {
        switch (x)
        {
            /*<bind>*/case dynamic y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case dynamic y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'dynamic y') (InputType: System.Object, NarrowedType: dynamic, DeclaredSymbol: dynamic y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8208: It is not legal to use the type 'dynamic' in a pattern.
                //             /*<bind>*/case dynamic y:/*</bind>*/
                Diagnostic(ErrorCode.ERR_PatternDynamicType, "dynamic").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_MixedDeclarationPatternAndConstantPatternClauses()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            case null:
                break;
            /*<bind>*/case X y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case X y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_MixedDeclarationPatternAndConstantPatternClausesInSameSwitchSection()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            case null:
            /*<bind>*/case X y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case X y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_MixedDeclarationPatternAndConstantPatternWithDefaultLabel()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            case null:
            /*<bind>*/case X y:/*</bind>*/
            default:
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case X y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_GuardExpressionInPattern()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            /*<bind>*/case X y when x != null:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case X y when x != null:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
  Guard: 
    IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'x != null')
      Left: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
      Right: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_PatternInGuardExpressionInPattern()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            /*<bind>*/case X y when x is X z :/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case X y when x is X z :')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
  Guard: 
    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is X z')
      Value: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
      Pattern: 
        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X z') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X z, MatchesNull: False)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_SyntaxErrorInGuardExpressionInPattern()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            /*<bind>*/case X y when :/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case X y when :')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
  Guard: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ':'
                //             /*<bind>*/case X y when :/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(9, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_SemanticErrorInGuardExpressionInPattern()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            /*<bind>*/case X y when x:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case X y when x:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X y') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
  Guard: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'bool'. An explicit conversion exists (are you missing a cast?)
                //             /*<bind>*/case X y when x:/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("object", "bool").WithLocation(9, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_ConstantPattern()
        {
            string source = @"
using System;
class X
{
    void M(bool x)
    {
        switch (x)
        {
            case /*<bind>*/x is true/*</bind>*/:
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'x is true')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'x')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: 'true') (InputType: System.Boolean, NarrowedType: System.Boolean)
      Value: 
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsInvalid) (Syntax: 'true')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0150: A constant value is expected
                //             case /*<bind>*/x is true/*</bind>*/:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "x is true").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_DefaultLabel()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            case X y:
            /*<bind>*/default:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IDefaultCaseClauseOperation (Label Id: 0) (CaseKind.Default) (OperationKind.CaseClause, Type: null) (Syntax: 'default:')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DefaultSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_InvalidTypeSwitch()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x.GetType())
        {
            /*<bind>*/case typeof(X):/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case typeof(X):')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'typeof(X)') (InputType: System.Type, NarrowedType: System.Type)
      Value: 
        ITypeOfOperation (OperationKind.TypeOf, Type: System.Type, IsInvalid) (Syntax: 'typeof(X)')
          TypeOperand: X
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0150: A constant value is expected
                //             /*<bind>*/case typeof(X):/*</bind>*/
                Diagnostic(ErrorCode.ERR_ConstantExpected, "typeof(X)").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CaseSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_UndefinedTypeInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M(object x)
    {
        switch (x)
        {
            /*<bind>*/case UndefinedType y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case UndefinedType y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'UndefinedType y') (InputType: System.Object, NarrowedType: UndefinedType, DeclaredSymbol: UndefinedType y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'UndefinedType' could not be found (are you missing a using directive or an assembly reference?)
                //             /*<bind>*/case UndefinedType y:/*</bind>*/
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UndefinedType").WithArguments("UndefinedType").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_InvalidTypeInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M(int? x)
    {
        switch (x)
        {
            /*<bind>*/case X y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case X y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'X y') (InputType: System.Int32?, NarrowedType: X, DeclaredSymbol: X y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(9,28): error CS8121: An expression of type 'int?' cannot be handled by a pattern of type 'X'.
                //             /*<bind>*/case X y:/*</bind>*/
                Diagnostic(ErrorCode.ERR_PatternWrongType, "X").WithArguments("int?", "X").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_DuplicateLocalInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M(int? x)
    {
        int? y = 0;
        switch (x)
        {
            /*<bind>*/case int y:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case int y:')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int y') (InputType: System.Int32?, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y, MatchesNull: False)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0136: A local or parameter named 'y' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             /*<bind>*/case int y:/*</bind>*/
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y").WithArguments("y").WithLocation(10, 32),
                // CS0219: The variable 'y' is assigned but its value is never used
                //         int? y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(7, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_InvalidConstDeclarationInPatternDeclaration()
        {
            string source = @"
using System;
class X
{
    void M(int? x)
    {
        switch (x)
        {
            /*<bind>*/case /*</bind>*/const int y:
                break;
        }
    }
}
";
            string expectedOperationTree = @"
    ISingleValueCaseClauseOperation (Label Id: 0) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case /*</bind>*/')
      Value: 
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term 'const'
                //             /*<bind>*/case /*</bind>*/const int y:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "const").WithArguments("const").WithLocation(9, 39),
                // CS1003: Syntax error, ':' expected
                //             /*<bind>*/case /*</bind>*/const int y:
                Diagnostic(ErrorCode.ERR_SyntaxError, "const").WithArguments(":").WithLocation(9, 39),
                // CS0145: A const field requires a value to be provided
                //             /*<bind>*/case /*</bind>*/const int y:
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "y").WithLocation(9, 49),
                // CS1002: ; expected
                //             /*<bind>*/case /*</bind>*/const int y:
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(9, 50),
                // CS1513: } expected
                //             /*<bind>*/case /*</bind>*/const int y:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(9, 50),
                // CS0168: The variable 'y' is declared but never used
                //             /*<bind>*/case /*</bind>*/const int y:
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y").WithArguments("y").WithLocation(9, 49)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CaseSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19927, "https://github.com/dotnet/roslyn/issues/19927")]
        public void TestPatternCaseClause_RedundantPatternDeclarationClauses()
        {
            string source = @"
using System;
class X
{
    void M(object p)
    {
        /*<bind>*/switch (p)
        {
            case int x:
                break;
            case int y:
                break;
            case X z:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (3 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null, IsInvalid) (Syntax: 'switch (p) ... }')
  Switch expression: 
    IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case int x: ... break;')
        Locals: Local_1: System.Int32 x
          Clauses:
              IPatternCaseClauseOperation (Label Id: 1) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case int x:')
                Pattern: 
                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Object, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: False)
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null, IsInvalid) (Syntax: 'case int y: ... break;')
        Locals: Local_1: System.Int32 y
          Clauses:
              IPatternCaseClauseOperation (Label Id: 2) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case int y:')
                Pattern: 
                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int y') (InputType: System.Object, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y, MatchesNull: False)
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case X z: ... break;')
        Locals: Local_1: X z
          Clauses:
              IPatternCaseClauseOperation (Label Id: 3) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case X z:')
                Pattern: 
                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'X z') (InputType: System.Object, NarrowedType: X, DeclaredSymbol: X z, MatchesNull: False)
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(11,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case int y:
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "int y").WithLocation(11, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestPatternCaseClause_PatternCombinatorsAndRelationalPatterns_01()
        {
            string source = @"
class X
{
    void M(char c)
    {
        switch (c)
        {
            /*<bind>*/case (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'):/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
    IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case (>= 'A ... nd <= 'z'):')
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

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularWithPatternCombinators);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestPatternCaseClause_TypePatterns_01()
        {
            string source = @"
class X
{
    void M(object o)
    {
        switch (o)
        {
            /*<bind>*/case int or long or bool:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
    IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case int or ... ng or bool:')
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

            VerifyOperationTreeAndDiagnosticsForTest<CasePatternSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularWithPatternCombinators);
        }
    }
}
