// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void GetLowLevelOperation_FromMethod()
        {
            string source = @"
class C
{
    /*<bind>*/
    static int Method(int p)
    {
        return p;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return p;')
    ReturnedValue: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void GetLowLevelOperation_FromConstructor()
        {
            string source = @"
class C
{
    private int _field = 0;

    /*<bind>*/
    public C(int p)
    {
        _field = p;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'public C(in ... }')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'public C(in ... }')
    Expression: IInvocationExpression ( System.Object..ctor()) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'public C(in ... }')
        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'public C(in ... }')
        Arguments(0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '_field = p;')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '_field = p')
          Left: IFieldReferenceExpression: System.Int32 C._field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '_field')
              Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '_field')
          Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void GetLowLevelOperation_FromFinalizer()
        {
            string source = @"
class C
{
    private int _field = 0;

    /*<bind>*/
    ~C()
    {
        _field += 0;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  ITryStatement (OperationKind.TryStatement) (Syntax: '{ ... }')
    Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '_field += 0;')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '_field += 0')
              Left: IFieldReferenceExpression: System.Int32 C._field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '_field')
                  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '_field')
              Right: IFieldReferenceExpression: System.Int32 C._field (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '_field')
                  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '_field')
    Catch clauses(0)
    Finally: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '{ ... }')
          Expression: IInvocationExpression ( void System.Object.Finalize()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: '{ ... }')
              Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.BaseClass) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '{ ... }')
              Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void GetLowLevelOperation_FromPropertyAccessorGet()
        {
            string source = @"
class C
{
    private int _property = 0;

    int Property
    {
        /*<bind>*/
        get { return _property; }
        /*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ return _property; }')
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return _property;')
    ReturnedValue: IFieldReferenceExpression: System.Int32 C._property (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '_property')
        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '_property')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<AccessorDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void GetLowLevelOperation_FromPropertyAccessorSet()
        {
            string source = @"
class C
{
    private int _property = 0;

    int Property
    {
        get { return _property; }
        /*<bind>*/
        set { _property = value; }
        /*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ _property = value; }')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '_property = value;')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '_property = value')
        Left: IFieldReferenceExpression: System.Int32 C._property (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: '_property')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: '_property')
        Right: IParameterReferenceExpression: value (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'value')

";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<AccessorDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }
    }
}
