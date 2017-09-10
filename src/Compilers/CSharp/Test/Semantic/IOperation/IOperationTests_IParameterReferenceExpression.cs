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
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_TupleExpression()
        {
            string source = @"
class Class1
{
    public void M(int x, int y)
    {
        var tuple = /*<bind>*/(x, x + y)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32 x, System.Int32)) (Syntax: '(x, x + y)')
  Elements(2):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
        Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = new[] {
                // file.cs(6,13): warning CS0219: The variable 'tuple' is assigned but its value is never used
                //         var tuple = /*<bind>*/(x, x + y)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "tuple").WithArguments("tuple").WithLocation(6, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_TupleDeconstruction()
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
    public void M(Point point)
    {
        /*<bind>*/var (x, y) = point/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'var (x, y) = point')
  Children(2):
      ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'var (x, y)')
        Elements(2):
            ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            ILocalReferenceExpression: y (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'point')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: point (OperationKind.ParameterReferenceExpression, Type: Point) (Syntax: 'point')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_AnonymousObjectCreation()
        {
            string source = @"
class Class1
{
    public void M(int x, string y)
    {
        var v = /*<bind>*/new { Amount = x, Message = ""Hello"" + y }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: System.Int32 Amount, System.String Message>) (Syntax: 'new { Amoun ... ello"" + y }')
  Initializers(2):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'Amount = x')
        Left: IPropertyReferenceExpression: System.Int32 <anonymous type: System.Int32 Amount, System.String Message>.Amount { get; } (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'Amount')
            Instance Receiver: null
        Right: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String) (Syntax: 'Message = ""Hello"" + y')
        Left: IPropertyReferenceExpression: System.String <anonymous type: System.Int32 Amount, System.String Message>.Message { get; } (Static) (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'Message')
            Instance Receiver: null
        Right: IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: '""Hello"" + y')
            Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Hello"") (Syntax: '""Hello""')
            Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_QueryExpression()
        {
            string source = @"
using System.Linq;
using System.Collections.Generic;

struct Customer
{
    public string Name { get; set; }
    public string Address { get; set; }
}

class Class1
{
    public void M(List<Customer> customers)
    {
        var result = /*<bind>*/from cust in customers
                     select cust.Name/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'from cust i ... t cust.Name')
  Children(1):
      IOperation:  (OperationKind.None) (Syntax: 'select cust.Name')
        Children(1):
            IInvocationExpression (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<Customer, System.String>(this System.Collections.Generic.IEnumerable<Customer> source, System.Func<Customer, System.String> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'select cust.Name')
              Instance Receiver: null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'from cust in customers')
                    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<Customer>) (Syntax: 'from cust in customers')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: IOperation:  (OperationKind.None) (Syntax: 'from cust in customers')
                          Children(1):
                              IParameterReferenceExpression: customers (OperationKind.ParameterReferenceExpression, Type: System.Collections.Generic.List<Customer>) (Syntax: 'customers')
                    InConversion: null
                    OutConversion: null
                  IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument) (Syntax: 'cust.Name')
                    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func<Customer, System.String>) (Syntax: 'cust.Name')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'cust.Name')
                          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'cust.Name')
                            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'cust.Name')
                              ReturnedValue: IPropertyReferenceExpression: System.String Customer.Name { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'cust.Name')
                                  Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'cust')
                    InConversion: null
                    OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<QueryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_ObjectAndCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;

internal class Class
{
    public int X { get; set; }
    public List<int> Y { get; set; }
    public Dictionary<int, int> Z { get; set; }
    public Class C { get; set; }

    public void M(int x, int y, int z)
    {
        var c = /*<bind>*/new Class() { X = x, Y = { x, y, 3 }, Z = { { x, y } }, C = { X = z } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectCreationExpression (Constructor: Class..ctor()) (OperationKind.ObjectCreationExpression, Type: Class) (Syntax: 'new Class() ... { X = z } }')
  Arguments(0)
  Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Class) (Syntax: '{ X = x, Y  ... { X = z } }')
      Initializers(4):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'X = x')
            Left: IPropertyReferenceExpression: System.Int32 Class.X { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'X')
            Right: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y = { x, y, 3 }')
            InitializedMember: IPropertyReferenceExpression: System.Collections.Generic.List<System.Int32> Class.Y { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'Y')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, 3 }')
                Initializers(3):
                    ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'x')
                      Arguments(1):
                          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                    ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: 'y')
                      Arguments(1):
                          IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
                    ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '3')
                      Arguments(1):
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z = { { x, y } }')
            InitializedMember: IPropertyReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Class.Z { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'Z')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '{ { x, y } }')
                Initializers(1):
                    ICollectionElementInitializerExpression (AddMethod: void System.Collections.Generic.Dictionary<System.Int32, System.Int32>.Add(System.Int32 key, System.Int32 value)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '{ x, y }')
                      Arguments(2):
                          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                          IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
          IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: Class) (Syntax: 'C = { X = z }')
            InitializedMember: IPropertyReferenceExpression: Class Class.C { get; set; } (OperationKind.PropertyReferenceExpression, Type: Class) (Syntax: 'C')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'C')
            Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Class) (Syntax: '{ X = z }')
                Initializers(1):
                    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'X = z')
                      Left: IPropertyReferenceExpression: System.Int32 Class.X { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'X')
                          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'X')
                      Right: IParameterReferenceExpression: z (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'z')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DelegateCreationExpressionWithLambdaArgument()
        {
            string source = @"
using System;

class Class
{
    // Used parameter methods
    public void UsedParameterMethod1(Action a)
    {
        Action a2 = /*<bind>*/new Action(() =>
        {
            a();
        })/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'new Action( ... })')
  Children(1):
      IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: '() => ... }')
        IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'a();')
            Expression: IInvocationExpression (virtual void System.Action.Invoke()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'a()')
                Instance Receiver: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Action) (Syntax: 'a')
                Arguments(0)
          IReturnStatement (OperationKind.ReturnStatement) (Syntax: '{ ... }')
            ReturnedValue: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DelegateCreationExpressionWithMethodArgument()
        {
            string source = @"
using System;

class Class
{
    public delegate void Delegate(int x, int y);

    public void Method(Delegate d)
    {
        var a = /*<bind>*/new Delegate(Method2)/*</bind>*/;
    }

    public void Method2(int x, int y)
    {
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'new Delegate(Method2)')
  Children(1):
      IOperation:  (OperationKind.None) (Syntax: 'Method2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DelegateCreationExpressionWithInvalidArgument()
        {
            string source = @"
using System;

class Class
{
    public delegate void Delegate(int x, int y);

    public void Method(int x)
    {
        var a = /*<bind>*/new Delegate(x)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: Class.Delegate, IsInvalid) (Syntax: 'new Delegate(x)')
  Children(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0149: Method name expected
                //         var a = /*<bind>*/new Delegate(x)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "x").WithLocation(10, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DynamicCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;

internal class Class
{
    public dynamic X { get; set; }

    public void M(int x, int y)
    {
        var c = new Class() /*<bind>*/{ X = { { x, y } } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: Class) (Syntax: '{ X = { { x, y } } }')
  Initializers(1):
      IMemberInitializerExpression (OperationKind.MemberInitializerExpression, Type: dynamic) (Syntax: 'X = { { x, y } }')
        InitializedMember: IPropertyReferenceExpression: dynamic Class.X { get; set; } (OperationKind.PropertyReferenceExpression, Type: dynamic) (Syntax: 'X')
            Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'X')
        Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: dynamic) (Syntax: '{ { x, y } }')
            Initializers(1):
                ICollectionElementInitializerExpression (IsDynamic: True) (OperationKind.CollectionElementInitializerExpression, Type: System.Void) (Syntax: '{ x, y }')
                  Arguments(2):
                      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InitializerExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_NameOfExpression()
        {
            string source = @"
class Class1
{
    public string M(int x)
    {
        return /*<bind>*/nameof(x)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
INameOfExpression (OperationKind.NameOfExpression, Type: System.String, Constant: ""x"") (Syntax: 'nameof(x)')
  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_PointerIndirectionExpression()
        {
            string source = @"
class Class1
{
    public unsafe int M(int *x)
    {
        return /*<bind>*/*x/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: '*x')
  Children(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32*) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public unsafe int M(int *x)
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "M").WithLocation(4, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_FixedLocalInitializer()
        {
            string source = @"
using System.Collections.Generic;

internal class Class
{
    public unsafe void M(int[] array)
    {
        fixed (int* p /*<bind>*/= array/*</bind>*/)
        {
            *p = 1;
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'p /*<bind>*/= array')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'p /*<bind>*/= array')
    Variables: Local_1: System.Int32* p
    Initializer: IOperation:  (OperationKind.None) (Syntax: 'array')
        Children(1):
            IParameterReferenceExpression: array (OperationKind.ParameterReferenceExpression, Type: System.Int32[]) (Syntax: 'array')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public unsafe void M(int[] array)
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "M").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_RefTypeOperator()
        {
            string source = @"
class Class1
{
    public System.Type M(System.TypedReference x)
    {
        return /*<bind>*/__reftype(x)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: '__reftype(x)')
  Children(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.TypedReference) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<RefTypeExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_MakeRefOperator()
        {
            string source = @"
class Class1
{
    public void M(System.Type x)
    {
        var y = /*<bind>*/__makeref(x)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: '__makeref(x)')
  Children(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Type) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MakeRefExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_RefValueOperator()
        {
            string source = @"
class Class1
{
    public void M(System.TypedReference x)
    {
        var y = /*<bind>*/__refvalue(x, int)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: '__refvalue(x, int)')
  Children(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.TypedReference) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<RefValueExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DynamicIndexerAccess()
        {
            string source = @"
class Class1
{
    public void M(dynamic d, int x)
    {
        var y /*<bind>*/= d[x]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var y /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var y /*<bi ... *</bind>*/;')
    Variables: Local_1: dynamic y
    Initializer: IOperation:  (OperationKind.None) (Syntax: 'd[x]')
        Children(2):
            IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
            IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DynamicMemberAccess()
        {
            string source = @"
class Class1
{
    public void M(dynamic x, int y)
    {
        var z = /*<bind>*/x.M(y)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'x.M(y)')
  Children(2):
      IDynamicMemberReferenceExpression (Member Name: ""M"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'x.M')
        Type Arguments(0)
        Instance Receiver: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'x')
      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DynamicInvocation()
        {
            string source = @"
class Class1
{
    public void M(dynamic x, int y)
    {
        var z = /*<bind>*/x(y)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'x(y)')
  Children(2):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'x')
      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DynamicObjectCreation()
        {
            string source = @"
internal class Class
{
    public Class(Class x) { }
    public Class(string x) { }

    public void M(dynamic x)
    {
        var c = /*<bind>*/new Class(x)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationExpression (Name: Class) (OperationKind.TypeParameterObjectCreationExpression, Type: Class) (Syntax: 'new Class(x)')
  ApplicableSymbols(2):
    Symbol: Class..ctor(Class x)
    Symbol: Class..ctor(System.String x)
  Arguments(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'x')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_StackAllocArrayCreation()
        {
            string source = @"
using System.Collections.Generic;

internal class Class
{
    public unsafe void M(int x)
    {
        int* block = /*<bind>*/stackalloc int[x]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'stackalloc int[x]')
  Children(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public unsafe void M(int x)
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "M").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_InterpolatedStringExpression()
        {
            string source = @"
using System;

internal class Class
{
    public void M(string x, int y)
    {
        Console.WriteLine(/*<bind>*/$""String {x,20} and {y:D3} and constant {1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x,20}')
        Expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
        Alignment: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" and "") (Syntax: ' and ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{y:D3}')
        Expression: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
        Alignment: null
        FormatString: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_ThrowExpression()
        {
            string source = @"
using System;

internal class Class
{
    public void M(string x)
    {
        var y = x ?? /*<bind>*/throw new ArgumentNullException(nameof(x))/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IThrowExpression (OperationKind.ThrowExpression, Type: null) (Syntax: 'throw new A ... (nameof(x))')
  IObjectCreationExpression (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreationExpression, Type: System.ArgumentNullException) (Syntax: 'new Argumen ... (nameof(x))')
    Arguments(1):
        IArgument (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument) (Syntax: 'nameof(x)')
          INameOfExpression (OperationKind.NameOfExpression, Type: System.String, Constant: ""x"") (Syntax: 'nameof(x)')
            IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
          InConversion: null
          OutConversion: null
    Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ThrowExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_PatternSwitchStatement()
        {
            string source = @"
internal class Class
{
    public void M(int x)
    {
        switch (x)
        {
            /*<bind>*/case var y when (x >= 10):
                break;/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (x) ... }')
  Switch expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case var y  ... break;')
          Clauses:
              IPatternCaseClause (Label Symbol: case var y when (x >= 10):) (CaseKind.Pattern) (OperationKind.PatternCaseClause) (Syntax: 'case var y  ...  (x >= 10):')
                Pattern: IDeclarationPattern (Declared Symbol: System.Int32 y) (OperationKind.DeclarationPattern) (Syntax: 'var y')
                Guard Expression: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x >= 10')
                    Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchSectionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_DefaultPatternSwitchStatement()
        {
            string source = @"
internal class Class
{
    public void M(int x)
    {
        switch (x)
        {
            case var y when (x >= 10):
                break;

            /*<bind>*/default:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (x) ... }')
  Switch expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case var y  ... break;')
          Clauses:
              IPatternCaseClause (Label Symbol: case var y when (x >= 10):) (CaseKind.Pattern) (OperationKind.PatternCaseClause) (Syntax: 'case var y  ...  (x >= 10):')
                Pattern: IDeclarationPattern (Declared Symbol: System.Int32 y) (OperationKind.DeclarationPattern) (Syntax: 'var y')
                Guard Expression: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x >= 10')
                    Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'default:/*< ... break;')
          Clauses:
              IDefaultCaseClause (CaseKind.Default) (OperationKind.DefaultCaseClause) (Syntax: 'default:')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DefaultSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_UserDefinedLogicalConditionalOperator()
        {
            string source = @"
class A<T>
{
    public static bool operator true(A<T> o) { return true; }
    public static bool operator false(A<T> o) { return false; }
}
class B : A<object>
{
    public static B operator &(B x, B y) { return x; }
}
class C : B
{
    public static C operator |(C x, C y) { return x; }
}
class P
{
    static void M(C x, C y)
    {
        if (/*<bind>*/x && y/*</bind>*/)
        {
        }
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'x && y')
  Children(2):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: B) (Syntax: 'x')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: B) (Syntax: 'y')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_NoPiaObjectCreation()
        {
            var sources0 = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""_.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
[CoClass(typeof(C))]
public interface I
        {
            int P { get; set; }
        }
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public class C
{
    public C(object o)
    {
    }
}
";
            var sources1 = @"
struct S
{
	public I F(object x)
	{
		return /*<bind>*/new I(x)/*</bind>*/;
    }
}
";
            var compilation0 = CreateStandardCompilation(sources0);
            compilation0.VerifyDiagnostics();

            var compilation1 = CreateStandardCompilation(
                sources1,
                references: new[] { MscorlibRef, SystemRef, compilation0.EmitToImageReference(embedInteropTypes: true) });

            string expectedOperationTree = @"
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'new I(x)')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                    // (6,25): error CS1729: 'I' does not contain a constructor that takes 1 arguments
                    // 		return /*<bind>*/new I(x)/*</bind>*/;
                    Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(x)").WithArguments("I", "1").WithLocation(6, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(compilation1, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void ParameterReference_ArgListOperator()
        {
            string source = @"
using System;
class C
{
    static void Method(int x, bool y)
    {
        M(1, /*<bind>*/__arglist(x, y)/*</bind>*/);
    }
    
    static void M(int x, __arglist)
    {
    } 
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: '__arglist(x, y)')
  Children(2):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Boolean) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19790, "https://github.com/dotnet/roslyn/issues/19790")]
        public void ParameterReference_IsPatternExpression()
        {
            string source = @"
class Class1
{
    public void Method1(object x)
    {
        if (/*<bind>*/x is int y/*</bind>*/) { }
    }
}
";
            string expectedOperationTree = @"
IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'x is int y')
  Expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'x')
  Pattern: IDeclarationPattern (Declared Symbol: System.Int32 y) (OperationKind.DeclarationPattern) (Syntax: 'int y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IsPatternExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19902, "https://github.com/dotnet/roslyn/issues/19902")]
        public void ParameterReference_LocalFunctionStatement()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Class
{
    static IEnumerable<T> MyIterator<T>(IEnumerable<T> source, Func<T, bool> predicate)
    {
        /*<bind>*/IEnumerable<T> Iterator()
        {
            foreach (var element in source)
                if (predicate(element))
                    yield return element;
        }/*</bind>*/

        return Iterator();
    }
}

";
            string expectedOperationTree = @"
ILocalFunctionStatement (Symbol: System.Collections.Generic.IEnumerable<T> Iterator()) (OperationKind.LocalFunctionStatement) (Syntax: 'IEnumerable ... }')
  IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
    IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (va ... rn element;')
      Locals: Local_1: T element
      LoopControlVariable: ILocalReferenceExpression: element (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: T, Constant: null) (Syntax: 'foreach (va ... rn element;')
      Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<T>) (Syntax: 'source')
          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: IParameterReferenceExpression: source (OperationKind.ParameterReferenceExpression, Type: System.Collections.Generic.IEnumerable<T>) (Syntax: 'source')
      Body: IIfStatement (OperationKind.IfStatement) (Syntax: 'if (predica ... rn element;')
          Condition: IInvocationExpression (virtual System.Boolean System.Func<T, System.Boolean>.Invoke(T arg)) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'predicate(element)')
              Instance Receiver: IParameterReferenceExpression: predicate (OperationKind.ParameterReferenceExpression, Type: System.Func<T, System.Boolean>) (Syntax: 'predicate')
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument) (Syntax: 'element')
                    ILocalReferenceExpression: element (OperationKind.LocalReferenceExpression, Type: T) (Syntax: 'element')
                    InConversion: null
                    OutConversion: null
          IfTrue: IReturnStatement (OperationKind.YieldReturnStatement) (Syntax: 'yield return element;')
              ReturnedValue: ILocalReferenceExpression: element (OperationKind.LocalReferenceExpression, Type: T) (Syntax: 'element')
          IfFalse: null
      NextVariables(0)
    IReturnStatement (OperationKind.YieldBreakStatement) (Syntax: '{ ... }')
      ReturnedValue: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
