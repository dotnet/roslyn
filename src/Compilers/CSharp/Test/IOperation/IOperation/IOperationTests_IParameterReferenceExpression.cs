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
    public class IOperationTests_IParameterReferenceExpression : SemanticModelTestBase
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
ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32)) (Syntax: '(x, x + y)')
  NaturalType: (System.Int32 x, System.Int32)
  Elements(2):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + y')
        Left: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
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
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'var (x, y) = point')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'var (x, y)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
        NaturalType: (System.Int32 x, System.Int32 y)
        Elements(2):
            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IParameterReferenceOperation: point (OperationKind.ParameterReference, Type: Point) (Syntax: 'point')
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
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 Amount, System.String Message>) (Syntax: 'new { Amoun ... ello"" + y }')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Amount = x')
        Left: 
          IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 Amount, System.String Message>.Amount { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Amount')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 Amount, System.String Message>, IsImplicit) (Syntax: 'new { Amoun ... ello"" + y }')
        Right: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'Message = ""Hello"" + y')
        Left: 
          IPropertyReferenceOperation: System.String <anonymous type: System.Int32 Amount, System.String Message>.Message { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Message')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 Amount, System.String Message>, IsImplicit) (Syntax: 'new { Amoun ... ello"" + y }')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '""Hello"" + y')
            Left: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Hello"") (Syntax: '""Hello""')
            Right: 
              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
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
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'from cust i ... t cust.Name')
  Expression: 
    IInvocationOperation (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<Customer, System.String>(this System.Collections.Generic.IEnumerable<Customer> source, System.Func<Customer, System.String> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: 'select cust.Name')
      Instance Receiver: 
        null
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from cust in customers')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<Customer>, IsImplicit) (Syntax: 'from cust in customers')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IParameterReferenceOperation: customers (OperationKind.ParameterReference, Type: System.Collections.Generic.List<Customer>) (Syntax: 'customers')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'cust.Name')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<Customer, System.String>, IsImplicit) (Syntax: 'cust.Name')
              Target: 
                IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'cust.Name')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'cust.Name')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'cust.Name')
                      ReturnedValue: 
                        IPropertyReferenceOperation: System.String Customer.Name { get; set; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'cust.Name')
                          Instance Receiver: 
                            IParameterReferenceOperation: cust (OperationKind.ParameterReference, Type: Customer) (Syntax: 'cust')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
IObjectCreationOperation (Constructor: Class..ctor()) (OperationKind.ObjectCreation, Type: Class) (Syntax: 'new Class() ... { X = z } }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Class) (Syntax: '{ X = x, Y  ... { X = z } }')
      Initializers(4):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = x')
            Left: 
              IPropertyReferenceOperation: System.Int32 Class.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'X')
            Right: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y = { x, y, 3 }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Collections.Generic.List<System.Int32> Class.Y { get; set; } (OperationKind.PropertyReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'Y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'Y')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '{ x, y, 3 }')
                Initializers(3):
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'Y')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z = { { x, y } }')
            InitializedMember: 
              IPropertyReferenceOperation: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Class.Z { get; set; } (OperationKind.PropertyReference, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: 'Z')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'Z')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '{ { x, y } }')
                Initializers(1):
                    IInvocationOperation ( void System.Collections.Generic.Dictionary<System.Int32, System.Int32>.Add(System.Int32 key, System.Int32 value)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{ x, y }')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, IsImplicit) (Syntax: 'Z')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IMemberInitializerOperation (OperationKind.MemberInitializer, Type: Class) (Syntax: 'C = { X = z }')
            InitializedMember: 
              IPropertyReferenceOperation: Class Class.C { get; set; } (OperationKind.PropertyReference, Type: Class) (Syntax: 'C')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'C')
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Class) (Syntax: '{ X = z }')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = z')
                      Left: 
                        IPropertyReferenceOperation: System.Int32 Class.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'X')
                      Right: 
                        IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'z')
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'new Action( ... })')
  Target: 
    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => ... }')
      IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a();')
          Expression: 
            IInvocationOperation (virtual void System.Action.Invoke()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'a()')
              Instance Receiver: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a')
              Arguments(0)
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{ ... }')
          ReturnedValue: 
            null
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Class.Delegate) (Syntax: 'new Delegate(Method2)')
  Target: 
    IMethodReferenceOperation: void Class.Method2(System.Int32 x, System.Int32 y) (OperationKind.MethodReference, Type: null) (Syntax: 'Method2')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'Method2')
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
IInvalidOperation (OperationKind.Invalid, Type: Class.Delegate, IsInvalid) (Syntax: 'new Delegate(x)')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
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
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Class) (Syntax: '{ X = { { x, y } } }')
  Initializers(1):
      IMemberInitializerOperation (OperationKind.MemberInitializer, Type: dynamic) (Syntax: 'X = { { x, y } }')
        InitializedMember: 
          IPropertyReferenceOperation: dynamic Class.X { get; set; } (OperationKind.PropertyReference, Type: dynamic) (Syntax: 'X')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'X')
        Initializer: 
          IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: dynamic) (Syntax: '{ { x, y } }')
            Initializers(1):
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Void) (Syntax: '{ x, y }')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: ""Add"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: 'X')
                      Type Arguments(0)
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: dynamic, IsImplicit) (Syntax: 'X')
                  Arguments(2):
                      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                  ArgumentNames(0)
                  ArgumentRefKinds(0)
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
INameOfOperation (OperationKind.NameOf, Type: System.String, Constant: ""x"") (Syntax: 'nameof(x)')
  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
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
IOperation:  (OperationKind.None, Type: System.Int32) (Syntax: '*x')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32*) (Syntax: 'x')
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
        fixed (int* /*<bind>*/p = array/*</bind>*/)
        {
            *p = 1;
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: System.Int32* p) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p = array')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= array')
      IOperation:  (OperationKind.None, Type: System.Int32*, IsImplicit) (Syntax: 'array')
        Children(1):
            IParameterReferenceOperation: array (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'array')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public unsafe void M(int[] array)
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "M").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IOperation:  (OperationKind.None, Type: System.Type) (Syntax: '__reftype(x)')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.TypedReference) (Syntax: 'x')
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
IOperation:  (OperationKind.None, Type: System.TypedReference) (Syntax: '__makeref(x)')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Type) (Syntax: 'x')
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
IOperation:  (OperationKind.None, Type: System.Int32) (Syntax: '__refvalue(x, int)')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.TypedReference) (Syntax: 'x')
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
        var /*<bind>*/y = d[x]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: dynamic y) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y = d[x]')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= d[x]')
      IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'd[x]')
        Expression: 
          IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
        Arguments(1):
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
        ArgumentNames(0)
        ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'x.M(y)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""M"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'x.M')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'x')
  Arguments(1):
      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
  ArgumentNames(0)
  ArgumentRefKinds(0)
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
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'x(y)')
  Expression: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'x')
  Arguments(1):
      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
  ArgumentNames(0)
  ArgumentRefKinds(0)
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
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: Class) (Syntax: 'new Class(x)')
  Arguments(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'x')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
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
IOperation:  (OperationKind.None, Type: System.Int32*) (Syntax: 'stackalloc int[x]')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
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
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x,20}')
        Expression: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
        Alignment: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y:D3}')
        Expression: 
          IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
        Alignment: 
          null
        FormatString: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
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
IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw new A ... (nameof(x))')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, IsImplicit) (Syntax: 'new Argumen ... (nameof(x))')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      IObjectCreationOperation (Constructor: System.ArgumentNullException..ctor(System.String paramName)) (OperationKind.ObjectCreation, Type: System.ArgumentNullException) (Syntax: 'new Argumen ... (nameof(x))')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: paramName) (OperationKind.Argument, Type: null) (Syntax: 'nameof(x)')
              INameOfOperation (OperationKind.NameOf, Type: System.String, Constant: ""x"") (Syntax: 'nameof(x)')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Initializer: 
          null
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
ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case var y  ... break;')
  Locals: Local_1: System.Int32 y
    Clauses:
        IPatternCaseClauseOperation (Label Id: 0) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case var y  ...  (x >= 10):')
          Pattern: 
            IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'var y') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y, MatchesNull: True)
          Guard: 
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'x >= 10')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
    Body:
        IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break;')
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
IDefaultCaseClauseOperation (Label Id: 0) (CaseKind.Default) (OperationKind.CaseClause, Type: null) (Syntax: 'default:')
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
IBinaryOperation (BinaryOperatorKind.ConditionalAnd) (OperatorMethod: B B.op_BitwiseAnd(B x, B y)) (OperationKind.Binary, Type: B) (Syntax: 'x && y')
  Left: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: B, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: B, IsImplicit) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
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
            var compilation0 = CreateCompilation(sources0);
            compilation0.VerifyDiagnostics();

            var compilation1 = CreateEmptyCompilation(
                sources1,
                references: new[] { MscorlibRef, SystemRef, compilation0.EmitToImageReference(embedInteropTypes: true) });

            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: I, IsInvalid) (Syntax: 'new I(x)')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (6,24): error CS1729: 'I' does not contain a constructor that takes 1 arguments
                // 		return /*<bind>*/new I(x)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "I").WithArguments("I", "1").WithLocation(6, 24)
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
IOperation:  (OperationKind.None, Type: null) (Syntax: '__arglist(x, y)')
  Children(2):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')
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
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'x is int y')
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
  Pattern: 
    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int y') (InputType: System.Object, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 y, MatchesNull: False)
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
ILocalFunctionOperation (Symbol: System.Collections.Generic.IEnumerable<T> Iterator()) (OperationKind.LocalFunction, Type: null) (Syntax: 'IEnumerable ... }')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... rn element;')
      Locals: Local_1: T element
      LoopControlVariable: 
        IVariableDeclaratorOperation (Symbol: T element) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
          Initializer: 
            null
      Collection: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<T>, IsImplicit) (Syntax: 'source')
          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceOperation: source (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable<T>) (Syntax: 'source')
      Body: 
        IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (predica ... rn element;')
          Condition: 
            IInvocationOperation (virtual System.Boolean System.Func<T, System.Boolean>.Invoke(T arg)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'predicate(element)')
              Instance Receiver: 
                IParameterReferenceOperation: predicate (OperationKind.ParameterReference, Type: System.Func<T, System.Boolean>) (Syntax: 'predicate')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: 'element')
                    ILocalReferenceOperation: element (OperationKind.LocalReference, Type: T) (Syntax: 'element')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          WhenTrue: 
            IReturnOperation (OperationKind.YieldReturn, Type: null) (Syntax: 'yield return element;')
              ReturnedValue: 
                ILocalReferenceOperation: element (OperationKind.LocalReference, Type: T) (Syntax: 'element')
          WhenFalse: 
            null
      NextVariables(0)
    IReturnOperation (OperationKind.YieldBreak, Type: null, IsImplicit) (Syntax: '{ ... }')
      ReturnedValue: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
