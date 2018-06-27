// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidInvocationExpression_BadReceiver()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/Console.WriteLine2()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Console.WriteLine2()')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Console.WriteLine2')
        Children(1):
            IOperation:  (OperationKind.None, Type: null) (Syntax: 'Console')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0117: 'Console' does not contain a definition for 'WriteLine2'
                //         /*<bind>*/Console.WriteLine2()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "WriteLine2").WithArguments("System.Console", "WriteLine2").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidInvocationExpression_OverloadResolutionFailureBadArgument()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/F(string.Empty)/*</bind>*/;
    }

    void F(int x)
    {
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'F(string.Empty)')
  Children(1):
      IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
        Instance Receiver: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1503: Argument 1: cannot convert from 'string' to 'int'
                //         /*<bind>*/F(string.Empty)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgType, "string.Empty").WithArguments("1", "string", "int").WithLocation(8, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidInvocationExpression_OverloadResolutionFailureExtraArgument()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/F(string.Empty)/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'F(string.Empty)')
  Children(1):
      IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: 'string.Empty')
        Instance Receiver: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1501: No overload for method 'F' takes 1 arguments
                //         /*<bind>*/F(string.Empty)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgCount, "F").WithArguments("F", "1").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidFieldReferenceExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        var /*<bind>*/y = x.MissingField/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: ? y) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'y = x.MissingField')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= x.MissingField')
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x.MissingField')
        Children(1):
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'Program' does not contain a definition for 'MissingField' and no extension method 'MissingField' accepting a first argument of type 'Program' could be found (are you missing a using directive or an assembly reference?)
                //         var y /*<bind>*/= x.MissingField/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "MissingField").WithArguments("Program", "MissingField").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidConversionExpression_ImplicitCast()
        {
            string source = @"
using System;

class Program
{
    int i1;
    static void Main(string[] args)
    {
        var x = new Program();
        /*<bind>*/string y = x.i1;/*</bind>*/
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'string y = x.i1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'string y = x.i1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.String y) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'y = x.i1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= x.i1')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'x.i1')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IFieldReferenceOperation: System.Int32 Program.i1 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'x.i1')
                    Instance Receiver: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'string'
                //         string y /*<bind>*/= x.i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x.i1").WithArguments("int", "string").WithLocation(10, 30),
                // CS0649: Field 'Program.i1' is never assigned to, and will always have its default value 0
                //     int i1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i1").WithArguments("Program.i1", "0").WithLocation(6, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidConversionExpression_ExplicitCast()
        {
            string source = @"
using System;

class Program
{
    int i1;
    static void Main(string[] args)
    {
        var x = new Program();
        /*<bind>*/Program y = (Program)x.i1;/*</bind>*/
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Program y = ... ogram)x.i1;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Program y = ... rogram)x.i1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: Program y) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'y = (Program)x.i1')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (Program)x.i1')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsInvalid) (Syntax: '(Program)x.i1')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IFieldReferenceOperation: System.Int32 Program.i1 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'x.i1')
                    Instance Receiver: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'int' to 'Program'
                //         Program y /*<bind>*/= (Program)x.i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Program)x.i1").WithArguments("int", "Program").WithLocation(10, 31),
                // CS0649: Field 'Program.i1' is never assigned to, and will always have its default value 0
                //     int i1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i1").WithArguments("Program.i1", "0").WithLocation(6, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidUnaryExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        Console.Write(/*<bind>*/++x/*</bind>*/);
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, Type: ?, IsInvalid) (Syntax: '++x')
  Target: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0023: Operator '++' cannot be applied to operand of type 'Program'
                //         Console.Write(/*<bind>*/++x/*</bind>*/);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++x").WithArguments("++", "Program").WithLocation(9, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidBinaryExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        Console.Write(/*<bind>*/x + (y * args.Length)/*</bind>*/);
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'x + (y * args.Length)')
  Left: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program) (Syntax: 'x')
  Right: 
    IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'y * args.Length')
      Left: 
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'y')
          Children(0)
      Right: 
        IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
          Instance Receiver: 
            IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'y' does not exist in the current context
                //         Console.Write(/*<bind>*/x + (y * args.Length)/*</bind>*/);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(9, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidLambdaBinding_UnboundLambda()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var /*<bind>*/x = () => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: var x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = () => F()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= () => F()')
      IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => F()')
        IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
            Expression: 
              IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'F()')
                Instance Receiver: 
                  null
                Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0815: Cannot assign lambda expression to an implicitly-typed variable
                //         var /*<bind>*/x = () => F()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x = () => F()").WithArguments("lambda expression").WithLocation(8, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidLambdaBinding_LambdaExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = /*<bind>*/() => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => F()')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
      Expression: 
        IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'F()')
          Instance Receiver: 
            null
          Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0815: Cannot assign lambda expression to an implicitly-typed variable
                //         var x = /*<bind>*/() => F()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x = /*<bind>*/() => F()").WithArguments("lambda expression").WithLocation(8, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedLambdaExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidFieldInitializer()
        {
            string source = @"
class Program
{
    int x /*<bind>*/= Program/*</bind>*/;
    static void Main(string[] args)
    {
        var x = new Program() { x = Program };
    }
}
";
            string expectedOperationTree = @"
IFieldInitializerOperation (Field: System.Int32 Program.x) (OperationKind.FieldInitializer, Type: null, IsInvalid) (Syntax: '= Program')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Program')
    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      IInvalidOperation (OperationKind.Invalid, Type: Program, IsInvalid, IsImplicit) (Syntax: 'Program')
        Children(1):
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Program')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0119: 'Program' is a type, which is not valid in the given context
                //     int x /*<bind>*/= Program/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(4, 23),
                // CS0119: 'Program' is a type, which is not valid in the given context
                //         var x = new Program() { x = Program };
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(7, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidArrayInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var x = new int[2, 2] /*<bind>*/{ { { 1, 1 } }, { 2, 2 } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ { { 1, 1  ...  { 2, 2 } }')
  Element Values(2):
      IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ { 1, 1 } }')
        Element Values(1):
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '{ 1, 1 }')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '{ 1, 1 }')
                  Children(1):
                      IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ 1, 1 }')
                        Element Values(2):
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{ 2, 2 }')
        Element Values(2):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.
                //         var x = new int[2, 2] /*<bind>*/{ { { 1, 1 } }, { 2, 2 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArrayInitInBadPlace, "{ 1, 1 }").WithLocation(6, 45),
                // CS0847: An array initializer of length '2' is expected
                //         var x = new int[2, 2] /*<bind>*/{ { { 1, 1 } }, { 2, 2 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "{ { 1, 1 } }").WithArguments("2").WithLocation(6, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidArrayCreation()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        var x = /*<bind>*/new X[Program] { { 1 } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationOperation (OperationKind.ArrayCreation, Type: X[], IsInvalid) (Syntax: 'new X[Program] { { 1 } }')
  Dimension Sizes(1):
      IInvalidOperation (OperationKind.Invalid, Type: Program, IsInvalid, IsImplicit) (Syntax: 'Program')
        Children(1):
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Program')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ { 1 } }')
      Element Values(1):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: X, IsInvalid, IsImplicit) (Syntax: '{ 1 }')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '{ 1 }')
                Children(1):
                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{ 1 }')
                      Element Values(1):
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: '1')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         var x = /*<bind>*/new X[Program] { { 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(6, 31),
                // CS0119: 'Program' is a type, which is not valid in the given context
                //         var x = /*<bind>*/new X[Program] { { 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(6, 33),
                // CS0623: Array initializers can only be used in a variable or field initializer. Try using a new expression instead.
                //         var x = /*<bind>*/new X[Program] { { 1 } }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArrayInitInBadPlace, "{ 1 }").WithLocation(6, 44)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidParameterDefaultValueInitializer()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static int M() { return 0; }
    void F(int p /*<bind>*/= M()/*</bind>*/)
    {
    }
}
";
            string expectedOperationTree = @"
IParameterInitializerOperation (Parameter: [System.Int32 p = default(System.Int32)]) (OperationKind.ParameterInitializer, Type: null, IsInvalid) (Syntax: '= M()')
  IInvocationOperation (System.Int32 Program.M()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M()')
    Instance Receiver: 
      null
    Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1736: Default parameter value for 'p' must be a compile-time constant
                //     void F(int p /*<bind>*/= M()/*</bind>*/)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M()").WithArguments("p").WithLocation(10, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_Repro()
        {
            string source = @"
public class C
{
    void M()
    {
        /*<bind>*/string.Format(format: """", format: """")/*</bind>*/;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.String, IsInvalid) (Syntax: 'string.Form ... format: """")')
  Children(3):
      IOperation:  (OperationKind.None, Type: null) (Syntax: 'string')
      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """") (Syntax: '""""')
";
            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(6,45): error CS1740: Named argument 'format' cannot be specified multiple times
                //         /*<bind>*/string.Format(format: "", format: "")/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "format").WithArguments("format").WithLocation(6, 45)
            });
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_CorrectArgumentsOrder_Methods()
        {
            string source = @"
public class C
{
    void N(int a, int b, int c = 4)
    {
    }

    void M()
    {
        /*<bind>*/N(a: 1, a: 2, b: 3)/*</bind>*/;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'N(a: 1, a: 2, b: 3)')
  Children(3):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(10,27): error CS1740: Named argument 'a' cannot be specified multiple times
                //         /*<bind>*/N(a: 1, a: 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "a").WithArguments("a").WithLocation(10, 27)
            });
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_CorrectArgumentsOrder_Delegates()
        {
            string source = @"
public delegate void D(int a, int b, int c = 4);
public class C
{
    void N(D lambda)
    {
        /*<bind>*/lambda(a: 1, a: 2, b: 3)/*</bind>*/;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'lambda(a: 1, a: 2, b: 3)')
  Children(4):
      IParameterReferenceOperation: lambda (OperationKind.ParameterReference, Type: D) (Syntax: 'lambda')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(7,32): error CS1740: Named argument 'a' cannot be specified multiple times
                //         /*<bind>*/lambda(a: 1, a: 2, b: 3)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "a").WithArguments("a").WithLocation(7, 32)
            });
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_CorrectArgumentsOrder_Indexers_Getter()
        {
            string source = @"
public class C
{
    int this[int a, int b, int c = 4]
    {
        get => 0;
    }

    void M()
    {
        var result = /*<bind>*/this[a: 1, a: 2, b: 3]/*</bind>*/;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'this[a: 1, a: 2, b: 3]')
  Children(4):
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(11,43): error CS1740: Named argument 'a' cannot be specified multiple times
                //         var result = /*<bind>*/this[a: 1, a: 2, b: 3]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "a").WithArguments("a").WithLocation(11, 43)
            });
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_CorrectArgumentsOrder_Indexers_Setter()
        {
            string source = @"
public class C
{
    int this[int a, int b, int c = 4]
    {
        set {}
    }

    void M()
    {
        /*<bind>*/this[a: 1, a: 2, b: 3]/*</bind>*/ = 0;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'this[a: 1, a: 2, b: 3]')
  Children(4):
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(11,30): error CS1740: Named argument 'a' cannot be specified multiple times
                //         /*<bind>*/this[a: 1, a: 2, b: 3]/*</bind>*/ = 0;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "a").WithArguments("a").WithLocation(11, 30)
            });
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_IncorrectArgumentsOrder_Methods()
        {
            string source = @"
public class C
{
    void N(int a, int b, int c = 4)
    {
    }

    void M()
    {
        /*<bind>*/N(b: 1, a: 2, a: 3)/*</bind>*/;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'N(b: 1, a: 2, a: 3)')
  Children(3):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(10,33): error CS1740: Named argument 'a' cannot be specified multiple times
                //         /*<bind>*/N(b: 1, a: 2, a: 3)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "a").WithArguments("a").WithLocation(10, 33)
            });
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_IncorrectArgumentsOrder_Delegates()
        {
            string source = @"
public delegate void D(int a, int b, int c = 4);
public class C
{
    void N(D lambda)
    {
        /*<bind>*/lambda(b: 1, a: 2, a: 3)/*</bind>*/;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'lambda(b: 1, a: 2, a: 3)')
  Children(4):
      IParameterReferenceOperation: lambda (OperationKind.ParameterReference, Type: D) (Syntax: 'lambda')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(7,38): error CS1740: Named argument 'a' cannot be specified multiple times
                //         /*<bind>*/lambda(b: 1, a: 2, a: 3)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "a").WithArguments("a").WithLocation(7, 38)
            });
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_IncorrectArgumentsOrder_Indexers_Getter()
        {
            string source = @"
public class C
{
    int this[int a, int b, int c = 4]
    {
        get => 0;
    }

    void M()
    {
        var result = /*<bind>*/this[b: 1, a: 2, a: 3]/*</bind>*/;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'this[b: 1, a: 2, a: 3]')
  Children(4):
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(11,49): error CS1740: Named argument 'a' cannot be specified multiple times
                //         var result = /*<bind>*/this[b: 1, a: 2, a: 3]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "a").WithArguments("a").WithLocation(11, 49)
            });
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(20050, "https://github.com/dotnet/roslyn/issues/20050")]
        public void BuildsArgumentsOperationsForDuplicateExplicitArguments_IncorrectArgumentsOrder_Indexers_Setter()
        {
            string source = @"
public class C
{
    int this[int a, int b, int c = 4]
    {
        set {}
    }

    void M()
    {
        /*<bind>*/this[b: 1, a: 2, a: 3]/*</bind>*/ = 0;
    }
}";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'this[b: 1, a: 2, a: 3]')
  Children(4):
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: new DiagnosticDescription[]
            {
                // file.cs(11,36): error CS1740: Named argument 'a' cannot be specified multiple times
                //         /*<bind>*/this[b: 1, a: 2, a: 3]/*</bind>*/ = 0;
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "a").WithArguments("a").WithLocation(11, 36)
            });
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InvalidExpressionFlow_01()
        {
            string source = @"
using System;
class C
{
    static void M1(int i, int x, int y)
    /*<bind>*/{
        i = M(1, __arglist(x, y));
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M' does not exist in the current context
                //         i = M(1, __arglist(a ? w : x, b ? y : z));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(7, 13)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i = M(1, __ ... ist(x, y));')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'i = M(1, __ ... list(x, y))')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M(1, __arglist(x, y))')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M(1, __arglist(x, y))')
                      Children(3):
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M')
                            Children(0)
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          IOperation:  (OperationKind.None, Type: null) (Syntax: '__arglist(x, y)')
                            Children(2):
                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InvalidExpressionFlow_02()
        {
            string source = @"
using System;
class C
{
    static void M1(int i, bool a,int x, int y, int z)
    /*<bind>*/{
        i = M(1, __arglist(x, a ? y : z));
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M' does not exist in the current context
                //         i = M(1, __arglist(x, a ? y : z));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(7, 13)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (4)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
          Value: 
            IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M')
          Value: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M')
              Children(0)

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'z')
          Value: 
            IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'z')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i = M(1, __ ...  ? y : z));')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'i = M(1, __ ... a ? y : z))')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M(1, __argl ... a ? y : z))')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M(1, __argl ... a ? y : z))')
                      Children(3):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'M')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                          IOperation:  (OperationKind.None, Type: null) (Syntax: '__arglist(x, a ? y : z)')
                            Children(2):
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ? y : z')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InvalidExpressionFlow_03()
        {
            string source = @"
using System;
class C
{
    static void M1(int i, bool a, int w, int x, int y)
    /*<bind>*/{
        i = M(1, __arglist(a ? w : x, y));
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M' does not exist in the current context
                //         i = M(1, __arglist(a ? w : x, y));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(7, 13)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
          Value: 
            IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M')
          Value: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M')
              Children(0)

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'w')
          Value: 
            IParameterReferenceOperation: w (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'w')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i = M(1, __ ... w : x, y));')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'i = M(1, __ ...  w : x, y))')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M(1, __argl ...  w : x, y))')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M(1, __argl ...  w : x, y))')
                      Children(3):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'M')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                          IOperation:  (OperationKind.None, Type: null) (Syntax: '__arglist(a ? w : x, y)')
                            Children(2):
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ? w : x')
                                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InvalidExpressionFlow_04()
        {
            string source = @"
using System;
class C
{
    static void M1(int i, bool a, bool b, int w, int x, int y, int z)
    /*<bind>*/{
        i = M(1, __arglist(a ? w : x, b ? y : z));
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M' does not exist in the current context
                //         i = M(1, __arglist(a ? w : x, b ? y : z));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(7, 13)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
          Value: 
            IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M')
          Value: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M')
              Children(0)

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'w')
          Value: 
            IParameterReferenceOperation: w (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'w')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'z')
          Value: 
            IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'z')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i = M(1, __ ...  ? y : z));')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'i = M(1, __ ... b ? y : z))')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M(1, __argl ... b ? y : z))')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M(1, __argl ... b ? y : z))')
                      Children(3):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'M')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                          IOperation:  (OperationKind.None, Type: null) (Syntax: '__arglist(a ...  b ? y : z)')
                            Children(2):
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ? w : x')
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? y : z')

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
