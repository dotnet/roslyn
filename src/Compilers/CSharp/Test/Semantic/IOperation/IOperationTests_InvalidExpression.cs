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
IInvocationExpression ( ? ?.()) (OperationKind.InvocationExpression, Type: ?, IsInvalid) (Syntax: 'Console.WriteLine2()')
  Instance Receiver: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'Console.WriteLine2')
      Children(1):
          IOperation:  (OperationKind.None) (Syntax: 'Console')
  Arguments(0)
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
IInvocationExpression ( void Program.F(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'F(string.Empty)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'F')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: 'string.Empty')
        IFieldReferenceExpression: System.String System.String.Empty (IsDeclaration: False) (Static) (OperationKind.FieldReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'string.Empty')
          Instance Receiver: null
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1503: Argument 1: cannot convert from 'string' to 'int'
                //         /*<bind>*/F(string.Empty)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgType, "string.Empty").WithArguments("1", "string", "int").WithLocation(8, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8813"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
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
IInvocationExpression ( void Program.F()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'F(string.Empty)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'F')
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
        var y /*<bind>*/= x.MissingField/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var y /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var y /*<bi ... *</bind>*/;')
    Variables: Local_1: ? y
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x.MissingField')
        Children(1):
            ILocalReferenceExpression: x (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'Program' does not contain a definition for 'MissingField' and no extension method 'MissingField' accepting a first argument of type 'Program' could be found (are you missing a using directive or an assembly reference?)
                //         var y /*<bind>*/= x.MissingField/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "MissingField").WithArguments("Program", "MissingField").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
        string y /*<bind>*/= x.i1/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'string y /* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'string y /* ... *</bind>*/;')
    Variables: Local_1: System.String y
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: 'x.i1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: System.Int32 Program.i1 (IsDeclaration: False) (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x.i1')
            Instance Receiver: ILocalReferenceExpression: x (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: Program, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'string'
                //         string y /*<bind>*/= x.i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x.i1").WithArguments("int", "string").WithLocation(10, 30),
                // CS0649: Field 'Program.i1' is never assigned to, and will always have its default value 0
                //     int i1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i1").WithArguments("Program.i1", "0").WithLocation(6, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
        Program y /*<bind>*/= (Program)x.i1/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Program y / ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Program y / ... *</bind>*/;')
    Variables: Local_1: Program y
    Initializer: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program, IsInvalid) (Syntax: '(Program)x.i1')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: System.Int32 Program.i1 (IsDeclaration: False) (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x.i1')
            Instance Receiver: ILocalReferenceExpression: x (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: Program, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'int' to 'Program'
                //         Program y /*<bind>*/= (Program)x.i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Program)x.i1").WithArguments("int", "Program").WithLocation(10, 31),
                // CS0649: Field 'Program.i1' is never assigned to, and will always have its default value 0
                //     int i1;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i1").WithArguments("Program.i1", "0").WithLocation(6, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18056"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
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
IIncrementExpression (UnaryOperandKind.Invalid) (BinaryOperationKind.Invalid) (OperationKind.IncrementExpression, Type: System.Object, IsInvalid) (Syntax: '++x')
  Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Object, Constant: 1) (Syntax: '++x')
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
IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'x + (y * args.Length)')
  Left: ILocalReferenceExpression: x (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
  Right: IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'y * args.Length')
      Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'y')
          Children(0)
      Right: IPropertyReferenceExpression: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'args.Length')
          Instance Receiver: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'y' does not exist in the current context
                //         Console.Write(/*<bind>*/x + (y * args.Length)/*</bind>*/);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(9, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18057"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidLambdaBinding_UnboundLambda()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x /*<bind>*/= () => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var x /*<bi ... *</bind>*/;')
  IVariableDeclaration: var x (OperationKind.VariableDeclaration) (Syntax: 'var x /*<bi ... *</bind>*/;')
    Initializer: IUnboundLambdaExpression (OperationKind.UnboundLambdaExpression, Type: null) (Syntax: '() => F()
        IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: '() => F()')
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'F()')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'F()')
              IInvocationExpression (static void Program.F()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'F()')')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0815: Cannot assign lambda expression to an implicitly-typed variable
                //         var x /*<bind>*/= () => F()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x /*<bind>*/= () => F()").WithArguments("lambda expression").WithLocation(8, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: '() => F()')
  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'F()')
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'F()')
      Expression: IInvocationExpression (void Program.F()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'F()')
          Instance Receiver: null
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
IFieldInitializer (Field: System.Int32 Program.x) (OperationKind.FieldInitializer, IsInvalid) (Syntax: '= Program')
  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Program')
    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: Program, IsInvalid) (Syntax: 'Program')
        Children(1):
            IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Program')
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
IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{ { { 1, 1  ...  { 2, 2 } }')
  Element Values(2):
      IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{ { 1, 1 } }')
        Element Values(1):
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '{ 1, 1 }')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{ 1, 1 }')
                  Children(1):
                      IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{ 1, 1 }')
                        Element Values(2):
                            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{ 2, 2 }')
        Element Values(2):
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IArrayCreationExpression (Element Type: X) (OperationKind.ArrayCreationExpression, Type: X[], IsInvalid) (Syntax: 'new X[Program] { { 1 } }')
  Dimension Sizes(1):
      IInvalidExpression (OperationKind.InvalidExpression, Type: Program, IsInvalid) (Syntax: 'Program')
        Children(1):
            IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Program')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{ { 1 } }')
      Element Values(1):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: X, IsInvalid) (Syntax: '{ 1 }')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{ 1 }')
                Children(1):
                    IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{ 1 }')
                      Element Values(1):
                          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: '1')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
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
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18059"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
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
IParameterInitializer (Parameter: [System.Int32 p = default(System.Int32)]) (OperationKind.ParameterInitializer, IsInvalid) (Syntax: '= M()')
  IInvocationExpression (static System.Int32 Program.M()) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'M()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1736: Default parameter value for 'p' must be a compile-time constant
                //     void F(int p /*<bind>*/= M()/*</bind>*/)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M()").WithArguments("p").WithLocation(10, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
