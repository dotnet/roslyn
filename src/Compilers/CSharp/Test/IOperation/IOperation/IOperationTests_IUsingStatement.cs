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
    public class IOperationTests_IUsingStatement : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_SimpleUsingNewVariable()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using (var c = new C())
        {
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (var  ... }')
  Locals: Local_1: C c
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c = new C()')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void IUsingAwaitStatement_SimpleAwaitUsing()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{
    public static async Task M1(IAsyncDisposable disposable)
    {
        /*<bind>*/await using (var c = disposable)
        {
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (IsAsynchronous) (DisposeMethod: System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Using, Type: null) (Syntax: 'await using ... }')
  Locals: Local_1: System.IAsyncDisposable c
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c = disposable')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = disposable')
        Declarators:
            IVariableDeclaratorOperation (Symbol: System.IAsyncDisposable c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = disposable')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= disposable')
                  IParameterReferenceOperation: disposable (OperationKind.ParameterReference, Type: System.IAsyncDisposable) (Syntax: 'disposable')
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.IAsyncDisposable) (Syntax: 'c')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void IUsingAwaitStatement_SimpleAwaitUsing_PatternBased()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{
    public async Task M1()
    {
        /*<bind>*/await using (var c = this)
        {
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }

    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        await Task.Yield();
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (IsAsynchronous) (DisposeMethod: System.Threading.Tasks.ValueTask C.DisposeAsync()) (OperationKind.Using, Type: null) (Syntax: 'await using ... }')
  Locals: Local_1: C c
  Resources:
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c = this')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = this')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = this')
              Initializer:
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= this')
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
        Initializer:
          null
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression:
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver:
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                    Instance Receiver:
                      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void IUsingAwaitStatement_SimpleAwaitUsing_InterfaceBased()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C : IAsyncDisposable
{
    public async Task M1()
    {
        /*<bind>*/await using (var c = this)
        {
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await Task.Yield();
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (IsAsynchronous) (OperationKind.Using, Type: null) (Syntax: 'await using ... }')
  Locals: Local_1: C c
  Resources:
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c = this')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = this')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = this')
              Initializer:
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= this')
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
        Initializer:
          null
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression:
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver:
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                    Instance Receiver:
                      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void UsingFlow_SimpleAwaitUsing()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{
    public static async Task M1(IAsyncDisposable disposable)
    /*<bind>*/{
        await using (var c = disposable)
        {
            Console.WriteLine(c.ToString());
        }
    }/*</bind>*/
}
";

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.IAsyncDisposable c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.IAsyncDisposable, IsImplicit) (Syntax: 'c = disposable')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.IAsyncDisposable, IsImplicit) (Syntax: 'c = disposable')
              Right:
                IParameterReferenceOperation: disposable (OperationKind.ParameterReference, Type: System.IAsyncDisposable) (Syntax: 'disposable')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
                  Expression:
                    IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                            IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                              Instance Receiver:
                                ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.IAsyncDisposable) (Syntax: 'c')
                              Arguments(0)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = disposable')
                  Operand:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.IAsyncDisposable, IsImplicit) (Syntax: 'c = disposable')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'c = disposable')
                  Expression:
                    IInvocationOperation ( System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'c = disposable')
                      Instance Receiver:
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.IAsyncDisposable, IsImplicit) (Syntax: 'c = disposable')
                      Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void UsingFlow_SimpleAwaitUsing_PatternBased()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{
    public async Task M1()
    /*<bind>*/{
        await using (var c = this)
        {
            Console.WriteLine(c.ToString());
        }
    }/*</bind>*/

    public async ValueTask DisposeAsync()
    {
        await System.Threading.Tasks.Task.Yield();
    }
}
";

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = this')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = this')
              Right:
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
                  Expression:
                    IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                            IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                              Instance Receiver:
                                ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                              Arguments(0)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = this')
                  Operand:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = this')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'c = this')
                  Expression:
                    IInvocationOperation ( System.Threading.Tasks.ValueTask C.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'c = this')
                      Instance Receiver:
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = this')
                      Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void UsingFlow_SimpleAwaitUsing_InterfaceBased()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C : IAsyncDisposable
{
    public async Task M1()
    /*<bind>*/{
        await using (var c = this)
        {
            Console.WriteLine(c.ToString());
        }
    }/*</bind>*/

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await System.Threading.Tasks.Task.Yield();
    }
}
";

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = this')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = this')
              Right:
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
                  Expression:
                    IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                            IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                              Instance Receiver:
                                ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                              Arguments(0)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = this')
                  Operand:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = this')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'c = this')
                  Expression:
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'c = this')
                      Instance Receiver:
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IAsyncDisposable, IsImplicit) (Syntax: 'c = this')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = this')
                      Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_MultipleNewVariable()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        
        /*<bind>*/using (C c1 = new C(), c2 = new C())
        {
            Console.WriteLine(c1.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (C c1 ... }')
  Locals: Local_1: C c1
    Local_2: C c2
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'C c1 = new  ... 2 = new C()')
      IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C c1 = new  ... 2 = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
            IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_SimpleUsingStatementExistingResource()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        var c = new C();
        /*<bind>*/using (c)
        {
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (c) ... }')
  Resources: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_NestedUsingNewResources()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using (var c1 = new C())
        using (var c2 = new C())
        {
            Console.WriteLine(c1.ToString() + c2.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (var  ... }')
  Locals: Local_1: C c1
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c1 = new C()')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c1 = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (var  ... }')
      Locals: Local_1: C c2
      Resources: 
        IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c2 = new C()')
          IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c2 = new C()')
            Declarators:
                IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = new C()')
                  Initializer: 
                    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                        Arguments(0)
                        Initializer: 
                          null
            Initializer: 
              null
      Body: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString ... .ToString()')
                      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
                        Left: 
                          IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                            Instance Receiver: 
                              ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                            Arguments(0)
                        Right: 
                          IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c2.ToString()')
                            Instance Receiver: 
                              ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
                            Arguments(0)
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_NestedUsingExistingResources()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        var c1 = new C();
        var c2 = new C();
        /*<bind>*/using (c1)
        using (c2)
        {
            Console.WriteLine(c1.ToString() + c2.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (c1) ... }')
  Resources: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
  Body: 
    IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (c2) ... }')
      Resources: 
        ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
      Body: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString ... .ToString()')
                      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
                        Left: 
                          IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                            Instance Receiver: 
                              ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                            Arguments(0)
                        Right: 
                          IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c2.ToString()')
                            Instance Receiver: 
                              ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
                            Arguments(0)
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_InvalidMultipleVariableDeclaration()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using (var c1 = new C(), c2 = new C())
        {
            Console.WriteLine(c1.ToString() + c2.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using (var  ... }')
  Locals: Local_1: C c1
    Local_2: C c2
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'var c1 = ne ... 2 = new C()')
      IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var c1 = ne ... 2 = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
            IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString ... .ToString()')
                  IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
                    Left: 
                      IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                        Instance Receiver: 
                          ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                        Arguments(0)
                    Right: 
                      IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c2.ToString()')
                        Instance Receiver: 
                          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
                        Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0819: Implicitly-typed variables cannot have multiple declarators
                //         /*<bind>*/using (var c1 = new C(), c2 = new C())
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, "var c1 = new C(), c2 = new C()").WithLocation(12, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IOperationTests_MultipleExistingResourcesPassed()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    /*<bind>*/{
        var c1 = new C();
        var c2 = new C();
        using (c1, c2)
        {
            Console.WriteLine(c1.ToString() + c2.ToString());
        }
    }/*</bind>*/
}
";
            // Capturing the whole block here, to show that the using statement is actually being bound as a using statement, followed by
            // an expression and a separate block, rather than being bound as a using statement with an invalid expression as the resources
            string expectedOperationTree = @"
IBlockOperation (5 statements, 2 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: C c1
    Local_2: C c2
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var c1 = new C();')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c1 = new C()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C()')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                  Arguments(0)
                  Initializer: 
                    null
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var c2 = new C();')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c2 = new C()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = new C()')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                  Arguments(0)
                  Initializer: 
                    null
      Initializer: 
        null
  IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using (c1')
    Resources: 
      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c1')
    Body: 
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c2')
    Expression: 
      ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c2')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
      Expression: 
        IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString ... .ToString()')
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
                  Left: 
                    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                      Instance Receiver: 
                        ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                      Arguments(0)
                  Right: 
                    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c2.ToString()')
                      Instance Receiver: 
                        ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
                      Arguments(0)
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1026: ) expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ",").WithLocation(14, 18),
                // CS1525: Invalid expression term ','
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(14, 18),
                // CS1002: ; expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(14, 18),
                // CS1513: } expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(14, 18),
                // CS1002: ; expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(14, 22),
                // CS1513: } expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(14, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_InvalidNonDisposableNewResource()
        {
            string source = @"
using System;

class C
{

    public static void M1()
    {
        /*<bind>*/using (var c1 = new C())
        {
            Console.WriteLine(c1.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using (var  ... }')
  Locals: Local_1: C c1
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'var c1 = new C()')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var c1 = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         /*<bind>*/using (var c1 = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var c1 = new C()").WithArguments("C").WithLocation(9, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_InvalidNonDisposableExistingResource()
        {
            string source = @"
using System;

class C
{

    public static void M1()
    {
        var c1 = new C();
        /*<bind>*/using (c1)
        {
            Console.WriteLine(c1.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using (c1) ... }')
  Resources: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         /*<bind>*/using (c1)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "c1").WithArguments("C").WithLocation(10, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_InvalidEmptyUsingResources()
        {
            string source = @"
using System;

class C
{

    public static void M1()
    {
        /*<bind>*/using ()
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using () ... }')
  Resources: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         /*<bind>*/using ()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingWithoutSavedReference()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using (GetC())
        {
        }/*</bind>*/
    }

    public static C GetC() => new C();
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (GetC ... }')
  Resources: 
    IInvocationOperation (C C.GetC()) (OperationKind.Invocation, Type: C) (Syntax: 'GetC()')
      Instance Receiver: 
        null
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DynamicArgument()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        dynamic d = null;
        /*<bind>*/using (d)
        {
            Console.WriteLine(d);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (d) ... }')
  Resources: 
    ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(d);')
        Expression: 
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'Console.WriteLine(d)')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: ""WriteLine"", Containing Type: System.Console) (OperationKind.DynamicMemberReference, Type: null) (Syntax: 'Console.WriteLine')
                Type Arguments(0)
                Instance Receiver: 
                  null
            Arguments(1):
                ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
            ArgumentNames(0)
            ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_NullResource()
        {
            string source = @"
using System;

class C
{
    public static void M1()
    {
        /*<bind>*/using (null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (null ... }')
  Resources: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingStatementSyntax_Declaration()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        using (/*<bind>*/var c = new C()/*</bind>*/)
        {
            Console.WriteLine(c.ToString());
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = new C()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
            IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
              Arguments(0)
              Initializer: 
                null
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingStatementSyntax_StatementSyntax()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        using (var c = new C())
        /*<bind>*/{
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
    Expression: 
      IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
        Instance Receiver: 
          null
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
              IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                Instance Receiver: 
                  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                Arguments(0)
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingStatementSyntax_ExpressionSyntax()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        var c = new C();
        using (/*<bind>*/c/*</bind>*/)
        {
            Console.WriteLine(c.ToString());
        }
    }
}
";
            string expectedOperationTree = @"
ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingStatementSyntax_VariableDeclaratorSyntax()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        
        using (C /*<bind>*/c1 = new C()/*</bind>*/, c2 = new C())
        {
            Console.WriteLine(c1.ToString());
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
        Arguments(0)
        Initializer: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_OutVarInResource()
        {
            string source = @"
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M(P p)
    {
        /*<bind>*/using (p = M2(out int c))
        {
            c = 1;
        }/*</bind>*/
    }

    P M2(out int c)
    {
        c = 0;
        return new P();
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (p =  ... }')
  Locals: Local_1: System.Int32 c
  Resources: 
    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P) (Syntax: 'p = M2(out int c)')
      Left: 
        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: P) (Syntax: 'p')
      Right: 
        IInvocationOperation ( P P.M2(out System.Int32 c)) (OperationKind.Invocation, Type: P) (Syntax: 'M2(out int c)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'M2')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: 'out int c')
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int c')
                  ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c = 1')
            Left: 
              ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DefaultDisposeArguments()
        {
            string source = @"
class C
{
    public static void M1()
    {
        /*<bind>*/using(var s = new S())
        { 
        }/*</bind>*/
    }
}

ref struct S
{
    public void Dispose(int a = 1, bool b = true, params object[] others) { }
}
";
            string expectedOperationTree = @"
IUsingOperation (DisposeMethod: void S.Dispose([System.Int32 a = 1], [System.Boolean b = true], params System.Object[] others)) (OperationKind.Using, Type: null) (Syntax: 'using(var s ... }')
  Locals: Local_1: S s
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var s = new S()')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var s = new S()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: S s) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's = new S()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new S()')
                  IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  DisposeArguments(3):
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'using(var s ... }')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'using(var s ... }')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: others) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'using(var s ... }')
          Dimension Sizes(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'using(var s ... }')
          Initializer: 
            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
              Element Values(0)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_ExpressionDefaultDisposeArguments()
        {
            string source = @"
class C
{
    public static void M1()
    {
        var s = new S();
        /*<bind>*/using(s)
        { 
        }/*</bind>*/
    }
}

ref struct S
{
    public void Dispose(int a = 1, bool b = true, params object[] others) { }
}
";
            string expectedOperationTree = @"
IUsingOperation (DisposeMethod: void S.Dispose([System.Int32 a = 1], [System.Boolean b = true], params System.Object[] others)) (OperationKind.Using, Type: null) (Syntax: 'using(s) ... }')
  Resources: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: S) (Syntax: 's')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  DisposeArguments(3):
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(s) ... }')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'using(s) ... }')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(s) ... }')
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'using(s) ... }')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: others) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(s) ... }')
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'using(s) ... }')
          Dimension Sizes(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'using(s) ... }')
          Initializer: 
            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'using(s) ... }')
              Element Values(0)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DisposalWithDefaultParams()
        {
            string source = @"
class C
{
    public static void M1()
    /*<bind>*/{
        using(var s = new S())
        { 
        }
    }/*</bind>*/

    public static void M2()
    {
        var s = new S();
        s.Dispose();
    }
}
ref struct S 
{
    public void Dispose(params object[] extras = null) { } 
}
";

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IUsingOperation (DisposeMethod: void S.Dispose(params System.Object[] extras)) (OperationKind.Using, Type: null) (Syntax: 'using(var s ... }')
    Locals: Local_1: S s
    Resources: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var s = new S()')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var s = new S()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: S s) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's = new S()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new S()')
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    DisposeArguments(1):
        IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'using(var s ... }')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'using(var s ... }')
            Initializer: 
              IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
                Element Values(0)
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [S s]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S, IsImplicit) (Syntax: 's = new S()')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: S, IsImplicit) (Syntax: 's = new S()')
              Right: 
                IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation ( void S.Dispose(params System.Object[] extras)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 's = new S()')
                  Instance Receiver: 
                    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: S, IsImplicit) (Syntax: 's = new S()')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'using(var s ... }')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'using(var s ... }')
                          Initializer: 
                            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
                              Element Values(0)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";

            var expectedDiagnostics = new[]
            {
                // file.cs(19,25): error CS1751: Cannot specify a default value for a parameter collection
                //     public void Dispose(params object[] extras = null) { } 
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(19, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        //THEORY: we won't ever call a params in normal form, because we ignore the default value in metadata.
        //        So: it's either a valid params parameter, in which case we call it in the extended way.
        //        Or its an invalid params parameter, in which case we can't use it, and we error out.
        //        Interestingly we check params before we check default, so a params int = 3 will be callable with an 
        //        argument, but not without. 

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DisposalWithDefaultParams_Metadata()
        {
            string source = @"
class C
{
    public static void M1()
    /*<bind>*/{
        using(var s = new S())
        { 
        }
    }/*</bind>*/

    public static void M2()
    {
        var s = new S();
        s.Dispose();
    }
}
";

            var ilSource = @"
.class public sequential ansi sealed beforefieldinit S
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig 
        instance void Dispose (
            [opt] object[] extras
        ) cil managed 
    {
        .param [1] = nullref
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8
        IL_0000: nop
        IL_0001: ret
    }
} 
";

            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);

            var compilation = CreateCompilationWithIL(source, ilSource);
            compilation.VerifyDiagnostics();

            var verifier = CompileAndVerify(compilation);

            verifier.VerifyIL("C.M2", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""object[] System.Array.Empty<object>()""
  IL_000f:  call       ""void S.Dispose(params object[])""
  IL_0014:  ret
}
");
            verifier.VerifyIL("C.M1", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (S V_0) //s
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  .try
  {
    IL_0008:  leave.s    IL_0017
  }
  finally
  {
    IL_000a:  ldloca.s   V_0
    IL_000c:  call       ""object[] System.Array.Empty<object>()""
    IL_0011:  call       ""void S.Dispose(params object[])""
    IL_0016:  endfinally
  }
  IL_0017:  ret
}
");

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IUsingOperation (DisposeMethod: void S.Dispose(params System.Object[] extras)) (OperationKind.Using, Type: null) (Syntax: 'using(var s ... }')
    Locals: Local_1: S s
    Resources: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var s = new S()')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var s = new S()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: S s) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's = new S()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new S()')
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    DisposeArguments(1):
        IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'using(var s ... }')
            Dimension Sizes(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'using(var s ... }')
            Initializer: 
              IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
                Element Values(0)
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [S s]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S, IsImplicit) (Syntax: 's = new S()')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: S, IsImplicit) (Syntax: 's = new S()')
              Right: 
                IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation ( void S.Dispose(params System.Object[] extras)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 's = new S()')
                  Instance Receiver: 
                    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: S, IsImplicit) (Syntax: 's = new S()')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'using(var s ... }')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'using(var s ... }')
                          Initializer: 
                            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'using(var s ... }')
                              Element Values(0)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, expectedOperationTree, expectedDiagnostics);

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DisposalWithNonArrayParams_Metadata()
        {
            string source = @"
class C
{
    public static void M1()
    /*<bind>*/{
        using(var s = new S())
        { 
        }
    }/*</bind>*/

    public static void M2()
    {
        var s = new S();
        s.Dispose();
        s.Dispose(1);
    }
}
";

            var ilSource = @"
.class public sequential ansi sealed beforefieldinit S
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig 
        instance void Dispose (
            int32 extras
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8
        IL_0000: nop
        IL_0001: ret
    }
} 
";

            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);

            var expectedDiagnostics = new[]
            {
                // (6,15): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'S.Dispose(params int)'
                //         using(var s = new S())
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "var s = new S()").WithArguments("extras", "S.Dispose(params int)").WithLocation(6, 15),
                // (6,15): error CS1674: 'S': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using(var s = new S())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var s = new S()").WithArguments("S").WithLocation(6, 15),
                // (14,11): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'S.Dispose(params int)'
                //         s.Dispose();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Dispose").WithArguments("extras", "S.Dispose(params int)").WithLocation(14, 11)
            };

            var compilation = CreateCompilationWithIL(source, ilSource);
            compilation.VerifyDiagnostics(expectedDiagnostics);

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using(var s ... }')
    Locals: Local_1: S s
    Resources: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'var s = new S()')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var s = new S()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: S s) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 's = new S()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new S()')
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [S s]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
              Right: 
                IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        ILocalReferenceOperation: s (OperationKind.LocalReference, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, expectedOperationTree, expectedDiagnostics);

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DisposalWithNonArrayOptionalParams_Metadata()
        {
            string source = @"
class C
{
    public static void M1()
    /*<bind>*/{
        using(var s = new S())
        { 
        }
    }/*</bind>*/

    public static void M2()
    {
        var s = new S();
        s.Dispose();
        s.Dispose(1);
    }
}
";

            var ilSource = @"
.class public sequential ansi sealed beforefieldinit S
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig 
        instance void Dispose (
            [opt] int32 extras
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8
        IL_0000: nop
        IL_0001: ret
    }
} 
";

            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);

            var expectedDiagnostics = new[]
            {
                // (6,15): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'S.Dispose(params int)'
                //         using(var s = new S())
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "var s = new S()").WithArguments("extras", "S.Dispose(params int)").WithLocation(6, 15),
                // (6,15): error CS1674: 'S': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using(var s = new S())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var s = new S()").WithArguments("S").WithLocation(6, 15),
                // (14,11): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'S.Dispose(params int)'
                //         s.Dispose();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Dispose").WithArguments("extras", "S.Dispose(params int)").WithLocation(14, 11)
            };

            var compilation = CreateCompilationWithIL(source, ilSource);
            compilation.VerifyDiagnostics(expectedDiagnostics);

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using(var s ... }')
    Locals: Local_1: S s
    Resources: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'var s = new S()')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var s = new S()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: S s) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 's = new S()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new S()')
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [S s]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
              Right: 
                IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        ILocalReferenceOperation: s (OperationKind.LocalReference, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, expectedOperationTree, expectedDiagnostics);

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DisposalWithNonArrayDefaultParams_Metadata()
        {
            string source = @"
class C
{
    public static void M1()
    /*<bind>*/{
        using(var s = new S())
        { 
        }
    }/*</bind>*/

    public static void M2()
    {
        var s = new S();
        s.Dispose();
        s.Dispose(1);
    }
}
";

            var ilSource = @"
.class public sequential ansi sealed beforefieldinit S
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig 
        instance void Dispose (
            [opt] int32 extras
        ) cil managed 
    {
        .param [1] = int32(3)
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .maxstack 8
        IL_0000: nop
        IL_0001: ret
    }
} 
";

            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);

            var expectedDiagnostics = new[]
            {
                // (6,15): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'S.Dispose(params int)'
                //         using(var s = new S())
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "var s = new S()").WithArguments("extras", "S.Dispose(params int)").WithLocation(6, 15),
                // (6,15): error CS1674: 'S': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using(var s = new S())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var s = new S()").WithArguments("S").WithLocation(6, 15),
                // (14,11): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'S.Dispose(params int)'
                //         s.Dispose();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Dispose").WithArguments("extras", "S.Dispose(params int)").WithLocation(14, 11)
            };

            var compilation = CreateCompilationWithIL(source, ilSource);
            compilation.VerifyDiagnostics(expectedDiagnostics);

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using(var s ... }')
    Locals: Local_1: S s
    Resources: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'var s = new S()')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var s = new S()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: S s) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 's = new S()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new S()')
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [S s]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
              Right: 
                IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        ILocalReferenceOperation: s (OperationKind.LocalReference, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, expectedOperationTree, expectedDiagnostics);

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DisposalWithDefaultParamsNotLast_Metadata()
        {
            string source = @"
class C
{
    public static void M1()
    /*<bind>*/{
        using(var s = new S())
        { 
        }
    }/*</bind>*/

    public static void M2()
    {
        var s = new S();
        s.Dispose();
    }
}
";

            var ilSource = @"
.class public sequential ansi sealed beforefieldinit S
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig 
        instance void Dispose (
            [opt] object[] extras,
            [opt] int32 a
        ) cil managed 
    {
        .param [1] = nullref
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        .param [2] = int32(0)
        .maxstack 8
        IL_0000: nop
        IL_0001: ret
    }
} 
";

            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);

            var expectedDiagnostics = new[]
            {
                // (6,15): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'S.Dispose(params object[], int)'
                //         using(var s = new S())
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "var s = new S()").WithArguments("extras", "S.Dispose(params object[], int)").WithLocation(6, 15),
                // (6,15): error CS1674: 'S': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                //         using(var s = new S())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var s = new S()").WithArguments("S").WithLocation(6, 15),
                // (14,11): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'S.Dispose(params object[], int)'
                //         s.Dispose();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Dispose").WithArguments("extras", "S.Dispose(params object[], int)").WithLocation(14, 11)
            };

            var compilation = CreateCompilationWithIL(source, ilSource);
            compilation.VerifyDiagnostics(expectedDiagnostics);

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using(var s ... }')
    Locals: Local_1: S s
    Resources: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'var s = new S()')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var s = new S()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: S s) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 's = new S()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new S()')
                    IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, expectedOperationTree, expectedDiagnostics);

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [S s]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
              Right: 
                IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S, IsInvalid) (Syntax: 'new S()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        ILocalReferenceOperation: s (OperationKind.LocalReference, Type: S, IsInvalid, IsImplicit) (Syntax: 's = new S()')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_01()
        {
            string source = @"
class P
{
    void M(System.IDisposable input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    System.IDisposable GetDisposable() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IInvocationOperation ( System.IDisposable P.GetDisposable()) (OperationKind.Invocation, Type: System.IDisposable) (Syntax: 'GetDisposable()')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable()')

            Next (Regular) Block[B4]
                Leaving: {R2}
                Entering: {R3} {R4}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.IDisposable) (Syntax: 'input')

        Next (Regular) Block[B4]
            Entering: {R3} {R4}

    .try {R3, R4}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B8]
                Finalizing: {R5}
                Leaving: {R4} {R3} {R1}
    }
    .finally {R5}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Arguments(0)

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B8] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_02()
        {
            string source = @"
class P
{
    void M(MyDisposable input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable GetDisposable() => throw null;
}

class MyDisposable : System.IDisposable
{
    public void Dispose() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IInvocationOperation ( MyDisposable P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable) (Syntax: 'GetDisposable()')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable()')

            Next (Regular) Block[B4]
                Leaving: {R2}
                Entering: {R3} {R4}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable) (Syntax: 'input')

        Next (Regular) Block[B4]
            Entering: {R3} {R4}

    .try {R3, R4}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B8]
                Finalizing: {R5}
                Leaving: {R4} {R3} {R1}
    }
    .finally {R5}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Arguments(0)

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B8] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_03()
        {
            string source = @"
class P
{
    void M(MyDisposable input, bool b)
/*<bind>*/{
        using (b ? GetDisposable() : input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable GetDisposable() => throw null;
}

struct MyDisposable : System.IDisposable
{
    public void Dispose() => throw null;
}
";
            string expectedGraph = @"
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
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
              Value: 
                IInvocationOperation ( MyDisposable P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable) (Syntax: 'GetDisposable()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
                  Arguments(0)

        Next (Regular) Block[B4]
            Entering: {R2} {R3}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable) (Syntax: 'input')

        Next (Regular) Block[B4]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
                  Arguments(0)

            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B6] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_04()
        {
            string source = @"
class P
{
    void M<MyDisposable>(MyDisposable input, bool b) where MyDisposable : System.IDisposable
/*<bind>*/{
        using (b ? GetDisposable<MyDisposable>() : input)
        {
            b = true;
        }
    }/*</bind>*/

    T GetDisposable<T>() => throw null;
}
";
            string expectedGraph = @"
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
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposab ... sposable>()')
              Value: 
                IInvocationOperation ( MyDisposable P.GetDisposable<MyDisposable>()) (OperationKind.Invocation, Type: MyDisposable) (Syntax: 'GetDisposab ... sposable>()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposab ... Disposable>')
                  Arguments(0)

        Next (Regular) Block[B4]
            Entering: {R2} {R3}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable) (Syntax: 'input')

        Next (Regular) Block[B4]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B8]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                  Arguments(0)

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B8] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_05()
        {
            string source = @"
class P
{
    void M(MyDisposable? input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable? GetDisposable() => throw null;
}

struct MyDisposable : System.IDisposable
{
    public void Dispose() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IInvocationOperation ( MyDisposable? P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable?) (Syntax: 'GetDisposable()')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable()')

            Next (Regular) Block[B4]
                Leaving: {R2}
                Entering: {R3} {R4}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable?) (Syntax: 'input')

        Next (Regular) Block[B4]
            Entering: {R3} {R4}

    .try {R3, R4}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B8]
                Finalizing: {R5}
                Leaving: {R4} {R3} {R1}
    }
    .finally {R5}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable() ?? input')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Arguments(0)

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B8] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_06()
        {
            string source = @"
class P
{
    void M(dynamic input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    dynamic GetDisposable() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}

.locals {R1}
{
    CaptureIds: [2]
    .locals {R2}
    {
        CaptureIds: [1]
        .locals {R3}
        {
            CaptureIds: [0]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                      Value: 
                        IInvocationOperation ( dynamic P.GetDisposable()) (OperationKind.Invocation, Type: dynamic) (Syntax: 'GetDisposable()')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
                          Arguments(0)

                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'GetDisposable()')
                    Leaving: {R3}

                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                      Value: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'GetDisposable()')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitDynamic)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'GetDisposable() ?? input')

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .try {R4, R5}
    {
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B9]
                Finalizing: {R6}
                Leaving: {R5} {R4} {R1}
    }
    .finally {R6}
    {
        Block[B6] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Arguments(0)

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_07()
        {
            string source = @"
class P
{
    void M(NotDisposable input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    NotDisposable GetDisposable() => throw null;
}

class NotDisposable
{
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IInvocationOperation ( NotDisposable P.GetDisposable()) (OperationKind.Invocation, Type: NotDisposable, IsInvalid) (Syntax: 'GetDisposable()')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'GetDisposable')
                      Arguments(0)
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
                Leaving: {R2}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
            Next (Regular) Block[B4]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: NotDisposable, IsInvalid) (Syntax: 'input')
        Next (Regular) Block[B4]
            Entering: {R3} {R4}
    .try {R3, R4}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Next (Regular) Block[B8]
                Finalizing: {R5}
                Leaving: {R4} {R3} {R1}
    }
    .finally {R5}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Arguments(0)
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B8] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,16): error CS1674: 'NotDisposable': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (GetDisposable() ?? input)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "GetDisposable() ?? input").WithArguments("NotDisposable").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_08()
        {
            string source = @"
class P
{
    void M(MyDisposable input, bool b)
/*<bind>*/{
        using (b ? GetDisposable() : input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable GetDisposable() => throw null;
}

struct MyDisposable
{
}
";
            string expectedGraph = @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
              Value: 
                IInvocationOperation ( MyDisposable P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable, IsInvalid) (Syntax: 'GetDisposable()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'GetDisposable')
                  Arguments(0)
        Next (Regular) Block[B4]
            Entering: {R2} {R3}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable, IsInvalid) (Syntax: 'input')
        Next (Regular) Block[B4]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,16): error CS1674: 'MyDisposable': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (b ? GetDisposable() : input)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "b ? GetDisposable() : input").WithArguments("MyDisposable").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_09()
        {
            string source = @"
class P
{
    void M<MyDisposable>(MyDisposable input, bool b)
/*<bind>*/{
        using (b ? GetDisposable<MyDisposable>() : input)
        {
            b = true;
        }
    }/*</bind>*/

    T GetDisposable<T>() => throw null;
}
";
            string expectedGraph = @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposab ... sposable>()')
              Value: 
                IInvocationOperation ( MyDisposable P.GetDisposable<MyDisposable>()) (OperationKind.Invocation, Type: MyDisposable, IsInvalid) (Syntax: 'GetDisposab ... sposable>()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'GetDisposab ... Disposable>')
                  Arguments(0)
        Next (Regular) Block[B4]
            Entering: {R2} {R3}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable, IsInvalid) (Syntax: 'input')
        Next (Regular) Block[B4]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Next (Regular) Block[B8]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Unboxing)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                  Arguments(0)
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B8] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,16): error CS1674: 'MyDisposable': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (b ? GetDisposable<MyDisposable>() : input)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "b ? GetDisposable<MyDisposable>() : input").WithArguments("MyDisposable").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_10()
        {
            string source = @"
class P
{
    void M(MyDisposable? input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable? GetDisposable() => throw null;
}

struct MyDisposable
{
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IInvocationOperation ( MyDisposable? P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable?, IsInvalid) (Syntax: 'GetDisposable()')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'GetDisposable')
                      Arguments(0)
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
                Leaving: {R2}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
            Next (Regular) Block[B4]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable?, IsInvalid) (Syntax: 'input')
        Next (Regular) Block[B4]
            Entering: {R3} {R4}
    .try {R3, R4}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Next (Regular) Block[B8]
                Finalizing: {R5}
                Leaving: {R4} {R3} {R1}
    }
    .finally {R5}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Arguments(0)
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B8] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,16): error CS1674: 'MyDisposable?': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (GetDisposable() ?? input)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "GetDisposable() ?? input").WithArguments("MyDisposable?").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_11()
        {
            string source = @"
class P
{
    void M(MyDisposable input, bool b)
/*<bind>*/{
        using (var x = GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable GetDisposable() => throw null;
}

class MyDisposable : System.IDisposable
{
    public void Dispose() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}

.locals {R1}
{
    Locals: [MyDisposable x]
    .locals {R2}
    {
        CaptureIds: [1]
        .locals {R3}
        {
            CaptureIds: [0]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                      Value: 
                        IInvocationOperation ( MyDisposable P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable) (Syntax: 'GetDisposable()')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
                          Arguments(0)

                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable()')
                    Leaving: {R3}

                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
                      Value: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable()')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable) (Syntax: 'input')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: MyDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .try {R4, R5}
    {
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B9]
                Finalizing: {R6}
                Leaving: {R5} {R4} {R1}
    }
    .finally {R6}
    {
        Block[B6] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: MyDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: MyDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                  Arguments(0)

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_12()
        {
            string source = @"
class P
{
    void M(dynamic input1, dynamic input2, bool b)
/*<bind>*/{
        using (dynamic x = input1, y = input2)
        {
            b = true;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [dynamic x] [dynamic y]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsImplicit) (Syntax: 'x = input1')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'x = input1')
              Right: 
                IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [0]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x = input1')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x = input1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitDynamic)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'x = input1')

            Next (Regular) Block[B3]
                Entering: {R3} {R4}

        .try {R3, R4}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsImplicit) (Syntax: 'y = input2')
                      Left: 
                        ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'y = input2')
                      Right: 
                        IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input2')

                Next (Regular) Block[B4]
                    Entering: {R5}

            .locals {R5}
            {
                CaptureIds: [1]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y = input2')
                          Value: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'y = input2')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (ExplicitDynamic)
                              Operand: 
                                ILocalReferenceOperation: y (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'y = input2')

                    Next (Regular) Block[B5]
                        Entering: {R6} {R7}

                .try {R6, R7}
                {
                    Block[B5] - Block
                        Predecessors: [B4]
                        Statements (1)
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                              Expression: 
                                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                                  Left: 
                                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                                  Right: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                        Next (Regular) Block[B12]
                            Finalizing: {R8} {R9}
                            Leaving: {R7} {R6} {R5} {R4} {R3} {R2} {R1}
                }
                .finally {R8}
                {
                    Block[B6] - Block
                        Predecessors (0)
                        Statements (0)
                        Jump if True (Regular) to Block[B8]
                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y = input2')
                              Operand: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'y = input2')

                        Next (Regular) Block[B7]
                    Block[B7] - Block
                        Predecessors: [B6]
                        Statements (1)
                            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y = input2')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'y = input2')
                              Arguments(0)

                        Next (Regular) Block[B8]
                    Block[B8] - Block
                        Predecessors: [B6] [B7]
                        Statements (0)
                        Next (StructuredExceptionHandling) Block[null]
                }
            }
        }
        .finally {R9}
        {
            Block[B9] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B11]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = input1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'x = input1')

                Next (Regular) Block[B10]
            Block[B10] - Block
                Predecessors: [B9]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = input1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'x = input1')
                      Arguments(0)

                Next (Regular) Block[B11]
            Block[B11] - Block
                Predecessors: [B9] [B10]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
}

Block[B12] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_13()
        {
            string source = @"
class P
{
    void M(System.IDisposable input, object o)
/*<bind>*/{
        using (input)
        {
            o?.ToString();
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.IDisposable) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o')
                  Value: 
                    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'o?.ToString();')
                  Expression: 
                    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
                      Arguments(0)

            Next (Regular) Block[B7]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'input')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'input')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_14()
        {
            string source = @"
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M(System.IDisposable input, P p)
    /*<bind>*/{
        using (p = M2(out int c))
        {
            c = 1;
        }
    }/*</bind>*/

    P M2(out int c)
    {
        c = 0;
        return new P();
    }
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 c]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p = M2(out int c)')
              Value: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P) (Syntax: 'p = M2(out int c)')
                  Left: 
                    IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: P) (Syntax: 'p')
                  Right: 
                    IInvocationOperation ( P P.M2(out System.Int32 c)) (OperationKind.Invocation, Type: P) (Syntax: 'M2(out int c)')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'M2')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: 'out int c')
                            IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int c')
                              ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = 1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c = 1')
                      Left: 
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'p = M2(out int c)')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'p = M2(out int c)')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'p = M2(out int c)')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'p = M2(out int c)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'p = M2(out int c)')
                  Arguments(0)

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_15()
        {
            string source = @"
class P
{
    void M(System.IDisposable input, object o)
/*<bind>*/{
        using (input)
        {
            o?.ToString();
        }
    }/*</bind>*/
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.MakeMemberMissing(SpecialMember.System_IDisposable__Dispose);

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.IDisposable) (Syntax: 'input')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o')
                  Value: 
                    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'o?.ToString();')
                  Expression: 
                    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
                      Arguments(0)
            Next (Regular) Block[B7]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'input')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'input')
                  Children(1):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'input')
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B7] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_16()
        {
            string source = @"
class P
{
    void M()
    /*<bind>*/{
        using (null)
        {
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NullLiteral)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'null')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
            Next (Regular) Block[B4]
        Block[B4] - Block [UnReachable]
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'null')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
                  Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingFlow_17()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219
using System.Threading.Tasks;

class C
{
    async Task M(S? s)
    /*<bind>*/{
        await using (s)
        {
        }
    }/*</bind>*/
}

struct S
{
    public Task DisposeAsync()
    {
        return default;
    }
}
";

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 's')
              Value: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: S?, IsInvalid) (Syntax: 's')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 's')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S?, IsInvalid, IsImplicit) (Syntax: 's')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 's')
                  Expression: 
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsInvalid, IsImplicit) (Syntax: 's')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IAsyncDisposable, IsInvalid, IsImplicit) (Syntax: 's')
                          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (NoConversion)
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S?, IsInvalid, IsImplicit) (Syntax: 's')
                      Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = new[]
            {
                // (9,22): error CS8410: 'S?': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using (s)
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "s").WithArguments("S?").WithLocation(9, 22)
            };

            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedGraph, expectedDiagnostics);

        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_18()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C 
{
    public async Task M() 
/*<bind>*/{
        await using(this){}
    }/*</bind>*/
    
    Task DisposeAsync(int a = 3, bool b = false) => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'this')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'this')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'this')
                  Expression: 
                    IInvocationOperation ( System.Threading.Tasks.Task C.DisposeAsync([System.Int32 a = 3], [System.Boolean b = false])) (OperationKind.Invocation, Type: System.Threading.Tasks.Task, IsImplicit) (Syntax: 'this')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'this')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using(this){}')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'await using(this){}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using(this){}')
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'await using(this){}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_19()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C 
{
    public async Task M() 
/*<bind>*/{
        await using(this){}
    }/*</bind>*/
    
    Task DisposeAsync(int a = 3, bool b = false, params int[] extras) => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'this')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'this')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'this')
                  Expression: 
                    IInvocationOperation ( System.Threading.Tasks.Task C.DisposeAsync([System.Int32 a = 3], [System.Boolean b = false], params System.Int32[] extras)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task, IsImplicit) (Syntax: 'this')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'this')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using(this){}')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'await using(this){}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using(this){}')
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'await using(this){}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using(this){}')
                            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: 'await using(this){}')
                              Dimension Sizes(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'await using(this){}')
                              Initializer: 
                                IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'await using(this){}')
                                  Element Values(0)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_20()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C 
{
    public async Task M() 
/*<bind>*/{
        await using(this){}
    }/*</bind>*/
    
    Task DisposeAsync(params int[] extras) => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'this')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'this')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'this')
                  Expression: 
                    IInvocationOperation ( System.Threading.Tasks.Task C.DisposeAsync(params System.Int32[] extras)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task, IsImplicit) (Syntax: 'this')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'this')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using(this){}')
                            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: 'await using(this){}')
                              Dimension Sizes(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'await using(this){}')
                              Initializer: 
                                IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'await using(this){}')
                                  Element Values(0)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_21()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C 
{
    public async Task M() 
/*<bind>*/{
        await using(this){}
    }/*</bind>*/
    
    Task DisposeAsync(int a = 3, params int[] extras, bool b = false) => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'this')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid) (Syntax: 'this')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'this')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'this')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'this')
                  Expression: 
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsInvalid, IsImplicit) (Syntax: 'this')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IAsyncDisposable, IsInvalid, IsImplicit) (Syntax: 'this')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ExplicitReference)
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'this')
                      Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = new[]
            {
                // file.cs(8,21): error CS7036: There is no argument given that corresponds to the required parameter 'extras' of 'C.DisposeAsync(int, params int[], bool)'
                //         await using(this){}
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "this").WithArguments("extras", "C.DisposeAsync(int, params int[], bool)").WithLocation(8, 21),
                // file.cs(8,21): error CS8410: 'C': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                //         await using(this){}
                Diagnostic(ErrorCode.ERR_NoConvToIAsyncDisp, "this").WithArguments("C").WithLocation(8, 21),
                // file.cs(11,34): error CS0231: A params parameter must be the last parameter in a parameter list
                //     Task DisposeAsync(int a = 3, params int[] extras, bool b = false) => throw null;
                Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] extras").WithLocation(11, 34)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_01()
        {
            string source = @"

using System;
class P : IDisposable
{
    void M()
    /*<bind>*/{
        using var x = new P();
    }/*</bind>*/

    public void Dispose() { }
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [P x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = new P()')
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
                  Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_02()
        {
            string source = @"

using System;
class P : IDisposable
{
    void M()
    /*<bind>*/{
        using P x = new P(), y = new P(), z = new P();
    }/*</bind>*/

    public void Dispose() { }
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [P x] [P y] [P z]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'y = new P()')
                  Left: 
                    ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = new P()')
                  Right: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B3]
                Entering: {R4} {R5}
        .try {R4, R5}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'z = new P()')
                      Left: 
                        ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'z = new P()')
                      Right: 
                        IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                          Arguments(0)
                          Initializer: 
                            null
                Next (Regular) Block[B4]
                    Entering: {R6} {R7}
            .try {R6, R7}
            {
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (0)
                    Next (Regular) Block[B14]
                        Finalizing: {R8} {R9} {R10}
                        Leaving: {R7} {R6} {R5} {R4} {R3} {R2} {R1}
            }
            .finally {R8}
            {
                Block[B5] - Block
                    Predecessors (0)
                    Statements (0)
                    Jump if True (Regular) to Block[B7]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'z = new P()')
                          Operand: 
                            ILocalReferenceOperation: z (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'z = new P()')
                    Next (Regular) Block[B6]
                Block[B6] - Block
                    Predecessors: [B5]
                    Statements (1)
                        IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'z = new P()')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'z = new P()')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (ImplicitReference)
                              Operand: 
                                ILocalReferenceOperation: z (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'z = new P()')
                          Arguments(0)
                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B5] [B6]
                    Statements (0)
                    Next (StructuredExceptionHandling) Block[null]
            }
        }
        .finally {R9}
        {
            Block[B8] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B10]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y = new P()')
                      Operand: 
                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = new P()')
                Next (Regular) Block[B9]
            Block[B9] - Block
                Predecessors: [B8]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y = new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'y = new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = new P()')
                      Arguments(0)
                Next (Regular) Block[B10]
            Block[B10] - Block
                Predecessors: [B8] [B9]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R10}
    {
        Block[B11] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B13]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = new P()')
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
                  Arguments(0)
            Next (Regular) Block[B13]
        Block[B13] - Block
            Predecessors: [B11] [B12]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B14] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_03()
        {
            string source = @"

using System;
class P : IDisposable
{
    void M()
    /*<bind>*/{
        using P x = null ?? new P(), y = new P();
    }/*</bind>*/

    public void Dispose() { }
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}
.locals {R1}
{
    Locals: [P x] [P y]
    .locals {R2}
    {
        CaptureIds: [1]
        .locals {R3}
        {
            CaptureIds: [0]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'null')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                    Leaving: {R3}
                Next (Regular) Block[B2]
            Block[B2] - Block [UnReachable]
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                      Value: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: P, IsImplicit) (Syntax: 'null')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                Next (Regular) Block[B4]
                    Leaving: {R3}
        }
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new P()')
                  Value: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'x = null ?? new P()')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = null ?? new P()')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'null ?? new P()')
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .try {R4, R5}
    {
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'y = new P()')
                  Left: 
                    ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = new P()')
                  Right: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B6]
                Entering: {R6} {R7}
        .try {R6, R7}
        {
            Block[B6] - Block
                Predecessors: [B5]
                Statements (0)
                Next (Regular) Block[B13]
                    Finalizing: {R8} {R9}
                    Leaving: {R7} {R6} {R5} {R4} {R1}
        }
        .finally {R8}
        {
            Block[B7] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B9]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y = new P()')
                      Operand: 
                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = new P()')
                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y = new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'y = new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = new P()')
                      Arguments(0)
                Next (Regular) Block[B9]
            Block[B9] - Block
                Predecessors: [B7] [B8]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R9}
    {
        Block[B10] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B12]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = null ?? new P()')
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = null ?? new P()')
            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B10]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = null ?? new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x = null ?? new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = null ?? new P()')
                  Arguments(0)
            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B10] [B11]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B13] - Exit
    Predecessors: [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_04()
        {
            string source = @"

using System;
class P : IDisposable
{
    void M()
    /*<bind>*/{
        using P x = new P(), y = null ?? new P();
    }/*</bind>*/

    public void Dispose() { }
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [P x] [P y]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3} {R4} {R5}
    .try {R2, R3}
    {
        .locals {R4}
        {
            CaptureIds: [1]
            .locals {R5}
            {
                CaptureIds: [0]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                          Value: 
                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                    Jump if True (Regular) to Block[B4]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'null')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                        Leaving: {R5}
                    Next (Regular) Block[B3]
                Block[B3] - Block [UnReachable]
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                          Value: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: P, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (ImplicitReference)
                              Operand: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                    Next (Regular) Block[B5]
                        Leaving: {R5}
            }
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new P()')
                      Value: 
                        IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                          Arguments(0)
                          Initializer: 
                            null
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'y = null ?? new P()')
                      Left: 
                        ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = null ?? new P()')
                      Right: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'null ?? new P()')
                Next (Regular) Block[B6]
                    Leaving: {R4}
                    Entering: {R6} {R7}
        }
        .try {R6, R7}
        {
            Block[B6] - Block
                Predecessors: [B5]
                Statements (0)
                Next (Regular) Block[B13]
                    Finalizing: {R8} {R9}
                    Leaving: {R7} {R6} {R3} {R2} {R1}
        }
        .finally {R8}
        {
            Block[B7] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B9]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y = null ?? new P()')
                      Operand: 
                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = null ?? new P()')
                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y = null ?? new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'y = null ?? new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'y = null ?? new P()')
                      Arguments(0)
                Next (Regular) Block[B9]
            Block[B9] - Block
                Predecessors: [B7] [B8]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R9}
    {
        Block[B10] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B12]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = new P()')
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B10]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
                  Arguments(0)
            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B10] [B11]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B13] - Exit
    Predecessors: [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_05()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        int a = 0;
        int b = 1;
        using var c = new P();
        int d = 2;
        using var e = new P();
        int f = 3;
    }/*</bind>*/

}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 a] [System.Int32 b] [P c] [System.Int32 d] [P e] [System.Int32 f]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b = 1')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'b = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'c = new P()')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = 2')
                  Left: 
                    ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = 2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'e = new P()')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                  Right: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B3]
                Entering: {R4} {R5}
        .try {R4, R5}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'f = 3')
                      Left: 
                        ILocalReferenceOperation: f (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'f = 3')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                Next (Regular) Block[B10]
                    Finalizing: {R6} {R7}
                    Leaving: {R5} {R4} {R3} {R2} {R1}
        }
        .finally {R6}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e = new P()')
                      Operand: 
                        ILocalReferenceOperation: e (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e = new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e = new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                      Arguments(0)
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R7}
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = new P()')
                  Operand: 
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'c = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'c = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
                  Arguments(0)
            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B10] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_06()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        int a = 0;
        int b = 1;
        using var c = new P();
        int d = 2;
        System.Action lambda = () => { _ = c.ToString(); };
        using var e = new P();
        int f = 3;
        lambda();
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 a] [System.Int32 b] [P c] [System.Int32 d] [System.Action lambda] [P e] [System.Int32 f]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b = 1')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'b = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'c = new P()')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = 2')
                  Left: 
                    ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = 2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action, IsImplicit) (Syntax: 'lambda = () ... String(); }')
                  Left: 
                    ILocalReferenceOperation: lambda (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Action, IsImplicit) (Syntax: 'lambda = () ... String(); }')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: '() => { _ = ... String(); }')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '() => { _ = ... String(); }')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (1)
                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = c.ToString();')
                                      Expression: 
                                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: '_ = c.ToString()')
                                          Left: 
                                            IDiscardOperation (Symbol: System.String _) (OperationKind.Discard, Type: System.String) (Syntax: '_')
                                          Right: 
                                            IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                                              Instance Receiver: 
                                                ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P) (Syntax: 'c')
                                              Arguments(0)
                                Next (Regular) Block[B2#A0]
                            Block[B2#A0] - Exit
                                Predecessors: [B1#A0]
                                Statements (0)
                        }
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'e = new P()')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                  Right: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B3]
                Entering: {R4} {R5}
        .try {R4, R5}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'f = 3')
                      Left: 
                        ILocalReferenceOperation: f (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'f = 3')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'lambda();')
                      Expression: 
                        IInvocationOperation (virtual void System.Action.Invoke()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'lambda()')
                          Instance Receiver: 
                            ILocalReferenceOperation: lambda (OperationKind.LocalReference, Type: System.Action) (Syntax: 'lambda')
                          Arguments(0)
                Next (Regular) Block[B10]
                    Finalizing: {R6} {R7}
                    Leaving: {R5} {R4} {R3} {R2} {R1}
        }
        .finally {R6}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e = new P()')
                      Operand: 
                        ILocalReferenceOperation: e (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e = new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e = new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                      Arguments(0)
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R7}
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = new P()')
                  Operand: 
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'c = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'c = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
                  Arguments(0)
            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B10] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_07()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        int a = 0;
        using var b = new P();
        {
            int c = 1;
            using var d = new P();
            int e = 2;
        }
        int f = 3;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 a] [P b] [System.Int32 f]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'b = new P()')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'b = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3} {R4}
    .try {R2, R3}
    {
        .locals {R4}
        {
            Locals: [System.Int32 c] [P d] [System.Int32 e]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'c = 1')
                      Left: 
                        ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'c = 1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'd = new P()')
                      Left: 
                        ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'd = new P()')
                      Right: 
                        IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                          Arguments(0)
                          Initializer: 
                            null
                Next (Regular) Block[B3]
                    Entering: {R5} {R6}
            .try {R5, R6}
            {
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'e = 2')
                          Left: 
                            ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'e = 2')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    Next (Regular) Block[B7]
                        Finalizing: {R7}
                        Leaving: {R6} {R5} {R4}
            }
            .finally {R7}
            {
                Block[B4] - Block
                    Predecessors (0)
                    Statements (0)
                    Jump if True (Regular) to Block[B6]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd = new P()')
                          Operand: 
                            ILocalReferenceOperation: d (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'd = new P()')
                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'd = new P()')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'd = new P()')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (ImplicitReference)
                              Operand: 
                                ILocalReferenceOperation: d (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'd = new P()')
                          Arguments(0)
                    Next (Regular) Block[B6]
                Block[B6] - Block
                    Predecessors: [B4] [B5]
                    Statements (0)
                    Next (StructuredExceptionHandling) Block[null]
            }
        }
        Block[B7] - Block
            Predecessors: [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'f = 3')
                  Left: 
                    ILocalReferenceOperation: f (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'f = 3')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            Next (Regular) Block[B11]
                Finalizing: {R8}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R8}
    {
        Block[B8] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B10]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b = new P()')
                  Operand: 
                    ILocalReferenceOperation: b (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'b = new P()')
            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'b = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: b (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'b = new P()')
                  Arguments(0)
            Next (Regular) Block[B10]
        Block[B10] - Block
            Predecessors: [B8] [B9]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B11] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_08()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        int a = 0;
        using var b = new P();
        label1:
        {
            int c = 1;
            if (a > 0)
                 goto label1;
                 
            using var d = new P();
            if (a > 0)
                goto label1;
            int e = 2;
        }
        int f = 3;
        goto label1;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 a] [P b] [System.Int32 f]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'b = new P()')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'b = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3] [B5] [B10]
            Statements (0)
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 c] [P d] [System.Int32 e]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'c = 1')
                      Left: 
                        ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'c = 1')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Jump if False (Regular) to Block[B4]
                    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a > 0')
                      Left: 
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Next (Regular) Block[B2]
                    Leaving: {R4}
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'd = new P()')
                      Left: 
                        ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'd = new P()')
                      Right: 
                        IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                          Arguments(0)
                          Initializer: 
                            null
                Next (Regular) Block[B5]
                    Entering: {R5} {R6}
            .try {R5, R6}
            {
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (0)
                    Jump if False (Regular) to Block[B6]
                        IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a > 0')
                          Left: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Next (Regular) Block[B2]
                        Finalizing: {R7}
                        Leaving: {R6} {R5} {R4}
                Block[B6] - Block
                    Predecessors: [B5]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'e = 2')
                          Left: 
                            ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'e = 2')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    Next (Regular) Block[B10]
                        Finalizing: {R7}
                        Leaving: {R6} {R5} {R4}
            }
            .finally {R7}
            {
                Block[B7] - Block
                    Predecessors (0)
                    Statements (0)
                    Jump if True (Regular) to Block[B9]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd = new P()')
                          Operand: 
                            ILocalReferenceOperation: d (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'd = new P()')
                    Next (Regular) Block[B8]
                Block[B8] - Block
                    Predecessors: [B7]
                    Statements (1)
                        IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'd = new P()')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'd = new P()')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (ImplicitReference)
                              Operand: 
                                ILocalReferenceOperation: d (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'd = new P()')
                          Arguments(0)
                    Next (Regular) Block[B9]
                Block[B9] - Block
                    Predecessors: [B7] [B8]
                    Statements (0)
                    Next (StructuredExceptionHandling) Block[null]
            }
        }
        Block[B10] - Block
            Predecessors: [B6]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'f = 3')
                  Left: 
                    ILocalReferenceOperation: f (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'f = 3')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            Next (Regular) Block[B2]
    }
    .finally {R8}
    {
        Block[B11] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B13]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b = new P()')
                  Operand: 
                    ILocalReferenceOperation: b (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'b = new P()')
            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'b = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: b (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'b = new P()')
                  Arguments(0)
            Next (Regular) Block[B13]
        Block[B13] - Block
            Predecessors: [B11] [B12]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B14] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_09()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219
using System.Threading.Tasks;

class C
{
    public Task DisposeAsync()
    {
        return default;
    }

    async Task M()
    /*<bind>*/{
        await using var c = new C();
    }/*</bind>*/
}
";

            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [C c]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C()')
                  Left: 
                    ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C()')
                  Right: 
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B2]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B2] - Block
                Predecessors: [B1]
                Statements (0)
                Next (Regular) Block[B6]
                    Finalizing: {R4}
                    Leaving: {R3} {R2} {R1}
        }
        .finally {R4}
        {
            Block[B3] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = new C()')
                      Operand: 
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C()')
                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'c = new C()')
                      Expression: 
                        IInvocationOperation ( System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task, IsImplicit) (Syntax: 'c = new C()')
                          Instance Receiver: 
                            ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C()')
                          Arguments(0)
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B6] - Exit
        Predecessors: [B2]
        Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedGraph, expectedDiagnostics);

        }

        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_10()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219
using System.Threading.Tasks;

class C : System.IDisposable
{
    public Task DisposeAsync()
    {
        return default;
    }

    public void Dispose()
    {
    }

    async Task M()
    /*<bind>*/{
        using var c = new C();
        await using var d = new C();
        using var e = new C();
        await using var f = new C();
    }/*</bind>*/
}
";
            string expectedGraph = @"
 Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [C c] [C d] [C e] [C f]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C()')
                  Left: 
                    ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C()')
                  Right: 
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B2]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'd = new C()')
                      Left: 
                        ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'd = new C()')
                      Right: 
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
                Next (Regular) Block[B3]
                    Entering: {R4} {R5}
            .try {R4, R5}
            {
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'e = new C()')
                          Left: 
                            ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'e = new C()')
                          Right: 
                            IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                              Arguments(0)
                              Initializer: 
                                null
                    Next (Regular) Block[B4]
                        Entering: {R6} {R7}
                .try {R6, R7}
                {
                    Block[B4] - Block
                        Predecessors: [B3]
                        Statements (1)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'f = new C()')
                              Left: 
                                ILocalReferenceOperation: f (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'f = new C()')
                              Right: 
                                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                                  Arguments(0)
                                  Initializer: 
                                    null
                        Next (Regular) Block[B5]
                            Entering: {R8} {R9}
                    .try {R8, R9}
                    {
                        Block[B5] - Block
                            Predecessors: [B4]
                            Statements (0)
                            Next (Regular) Block[B18]
                                Finalizing: {R10} {R11} {R12} {R13}
                                Leaving: {R9} {R8} {R7} {R6} {R5} {R4} {R3} {R2} {R1}
                    }
                    .finally {R10}
                    {
                        Block[B6] - Block
                            Predecessors (0)
                            Statements (0)
                            Jump if True (Regular) to Block[B8]
                                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'f = new C()')
                                  Operand: 
                                    ILocalReferenceOperation: f (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'f = new C()')
                            Next (Regular) Block[B7]
                        Block[B7] - Block
                            Predecessors: [B6]
                            Statements (1)
                                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'f = new C()')
                                  Expression: 
                                    IInvocationOperation ( System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task, IsImplicit) (Syntax: 'f = new C()')
                                      Instance Receiver: 
                                        ILocalReferenceOperation: f (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'f = new C()')
                                      Arguments(0)
                            Next (Regular) Block[B8]
                        Block[B8] - Block
                            Predecessors: [B6] [B7]
                            Statements (0)
                            Next (StructuredExceptionHandling) Block[null]
                    }
                }
                .finally {R11}
                {
                    Block[B9] - Block
                        Predecessors (0)
                        Statements (0)
                        Jump if True (Regular) to Block[B11]
                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e = new C()')
                              Operand: 
                                ILocalReferenceOperation: e (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'e = new C()')
                        Next (Regular) Block[B10]
                    Block[B10] - Block
                        Predecessors: [B9]
                        Statements (1)
                            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e = new C()')
                              Instance Receiver: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e = new C()')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand: 
                                    ILocalReferenceOperation: e (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'e = new C()')
                              Arguments(0)
                        Next (Regular) Block[B11]
                    Block[B11] - Block
                        Predecessors: [B9] [B10]
                        Statements (0)
                        Next (StructuredExceptionHandling) Block[null]
                }
            }
            .finally {R12}
            {
                Block[B12] - Block
                    Predecessors (0)
                    Statements (0)
                    Jump if True (Regular) to Block[B14]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd = new C()')
                          Operand: 
                            ILocalReferenceOperation: d (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'd = new C()')
                    Next (Regular) Block[B13]
                Block[B13] - Block
                    Predecessors: [B12]
                    Statements (1)
                        IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'd = new C()')
                          Expression: 
                            IInvocationOperation ( System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task, IsImplicit) (Syntax: 'd = new C()')
                              Instance Receiver: 
                                ILocalReferenceOperation: d (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'd = new C()')
                              Arguments(0)
                    Next (Regular) Block[B14]
                Block[B14] - Block
                    Predecessors: [B12] [B13]
                    Statements (0)
                    Next (StructuredExceptionHandling) Block[null]
            }
        }
        .finally {R13}
        {
            Block[B15] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B17]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = new C()')
                      Operand: 
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C()')
                Next (Regular) Block[B16]
            Block[B16] - Block
                Predecessors: [B15]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'c = new C()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'c = new C()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C()')
                      Arguments(0)
                Next (Regular) Block[B17]
            Block[B17] - Block
                Predecessors: [B15] [B16]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B18] - Exit
        Predecessors: [B5]
        Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_Flow_11()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        int a = 0;
        int b = 1;
        using var c = new P();
        int d = 2;
        using var e = new P();
    }/*</bind>*/

}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 a] [System.Int32 b] [P c] [System.Int32 d] [P e]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b = 1')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'b = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'c = new P()')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = 2')
                  Left: 
                    ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = 2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'e = new P()')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                  Right: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B3]
                Entering: {R4} {R5}
        .try {R4, R5}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)  
                Next (Regular) Block[B10]
                    Finalizing: {R6} {R7}
                    Leaving: {R5} {R4} {R3} {R2} {R1}
        }
        .finally {R6}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e = new P()')
                      Operand: 
                        ILocalReferenceOperation: e (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e = new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e = new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                      Arguments(0)
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R7}
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = new P()')
                  Operand: 
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'c = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'c = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
                  Arguments(0)
            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B10] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_12()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        int a = 0;
        int b = 1;
label1:
        using var c = new P();
        int d = 2;
label2:
label3:
        using var e = new P();
    }/*</bind>*/

}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 a] [System.Int32 b] [P c] [System.Int32 d] [P e]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'a = 0')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b = 1')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'b = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'c = new P()')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'd = 2')
                  Left: 
                    ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'd = 2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'e = new P()')
                  Left: 
                    ILocalReferenceOperation: e (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                  Right: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B3]
                Entering: {R4} {R5}
        .try {R4, R5}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)  
                Next (Regular) Block[B10]
                    Finalizing: {R6} {R7}
                    Leaving: {R5} {R4} {R3} {R2} {R1}
        }
        .finally {R6}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e = new P()')
                      Operand: 
                        ILocalReferenceOperation: e (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e = new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e = new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'e = new P()')
                      Arguments(0)
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R7}
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c = new P()')
                  Operand: 
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'c = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'c = new P()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'c = new P()')
                  Arguments(0)
            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B10] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_13()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        if (true)
            label1:
                using var a = new P();
        if (true)
            label2:
            label3:
                using var b = new P();
    }/*</bind>*/

}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B7]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B2]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [P a]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsInvalid, IsImplicit) (Syntax: 'a = new P()')
                  Left: 
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'a = new P()')
                  Right: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P, IsInvalid) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B3]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Next (Regular) Block[B7]
                    Finalizing: {R4}
                    Leaving: {R3} {R2} {R1}
        }
        .finally {R4}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'a = new P()')
                      Operand: 
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'a = new P()')
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'a = new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'a = new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'a = new P()')
                      Arguments(0)
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B7] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B13]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B8]
            Entering: {R5}
    .locals {R5}
    {
        Locals: [P b]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsInvalid, IsImplicit) (Syntax: 'b = new P()')
                  Left: 
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'b = new P()')
                  Right: 
                    IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P, IsInvalid) (Syntax: 'new P()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B9]
                Entering: {R6} {R7}
        .try {R6, R7}
        {
            Block[B9] - Block
                Predecessors: [B8]
                Statements (0)
                Next (Regular) Block[B13]
                    Finalizing: {R8}
                    Leaving: {R7} {R6} {R5}
        }
        .finally {R8}
        {
            Block[B10] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B12]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'b = new P()')
                      Operand: 
                        ILocalReferenceOperation: b (OperationKind.LocalReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'b = new P()')
                Next (Regular) Block[B11]
            Block[B11] - Block
                Predecessors: [B10]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'b = new P()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'b = new P()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'b = new P()')
                      Arguments(0)
                Next (Regular) Block[B12]
            Block[B12] - Block
                Predecessors: [B10] [B11]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B13] - Exit
        Predecessors: [B7] [B9]
        Statements (0)
";
            var expectedDiagnostics = new[]{
                // file.cs(12,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             label1:
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, @"label1:
                using var a = new P();").WithLocation(12, 13),
                // file.cs(15,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             label2:
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, @"label2:
            label3:
                using var b = new P();").WithLocation(15, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_14()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        goto label1;
        int x = 0;
        label1:
        using var a = this;
    }/*</bind>*/

}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B2]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.Int32 x] [P a]
        Block[B1] - Block [UnReachable]
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 0')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'x = 0')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B0] [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'a = this')
                  Left: 
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                  Right: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P) (Syntax: 'this')
            Next (Regular) Block[B3]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Next (Regular) Block[B7]
                    Finalizing: {R4}
                    Leaving: {R3} {R2} {R1}
        }
        .finally {R4}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a = this')
                      Operand: 
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a = this')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a = this')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                      Arguments(0)
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B7] - Exit
        Predecessors: [B3]
        Statements (0)
";
            var expectedDiagnostics = new[]{
                // file.cs(12,9): warning CS0162: Unreachable code detected
                //         int x = 0;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(12, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_15()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        label1:
        using var a = this;
        goto label1;
    }/*</bind>*/

}
";
            string expectedGraph = @"
      Block[B0] - Entry
      Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [P a]
        Block[B1] - Block
            Predecessors: [B0] [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'a = this')
                  Left: 
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                  Right: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P) (Syntax: 'this')
            Next (Regular) Block[B2]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B2] - Block
                Predecessors: [B1]
                Statements (0)
                Next (Regular) Block[B1]
                    Finalizing: {R4}
                    Leaving: {R3} {R2}
        }
        .finally {R4}
        {
            Block[B3] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a = this')
                      Operand: 
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a = this')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a = this')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                      Arguments(0)
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B6] - Exit [UnReachable]
        Predecessors (0)
        Statements (0)
";
            var expectedDiagnostics = new[]{
                // file.cs(13,9): error CS8649: A goto cannot jump to a location before a using declaration within the same block.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label1;").WithLocation(13, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_16()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M()
    /*<bind>*/{
        goto label1;
        int x = 0;
        label1:
        using var a = this;
        goto label1;
    }/*</bind>*/

}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B2]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.Int32 x] [P a]
        Block[B1] - Block [UnReachable]
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 0')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'x = 0')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B0] [B1] [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'a = this')
                  Left: 
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                  Right: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P) (Syntax: 'this')
            Next (Regular) Block[B3]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Next (Regular) Block[B2]
                    Finalizing: {R4}
                    Leaving: {R3} {R2}
        }
        .finally {R4}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a = this')
                      Operand: 
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a = this')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a = this')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                      Arguments(0)
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B7] - Exit [UnReachable]
        Predecessors (0)
        Statements (0)
";
            var expectedDiagnostics = new[]{
                // file.cs(12,9): warning CS0162: Unreachable code detected
                //         int x = 0;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(12, 9),
                // file.cs(15,9): error CS8649: A goto cannot jump to a location before a using declaration within the same block.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label1;").WithLocation(15, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_17()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M(bool b)
    /*<bind>*/{
        if (b)
            goto label1;
        int x = 0;
        label1:
        using var a = this;
    }/*</bind>*/

}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.Int32 x] [P a]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Jump if False (Regular) to Block[B2]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
            Next (Regular) Block[B3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 0')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'x = 0')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B1] [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'a = this')
                  Left: 
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                  Right: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P) (Syntax: 'this')
            Next (Regular) Block[B4]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B4] - Block
                Predecessors: [B3]
                Statements (0)
                Next (Regular) Block[B8]
                    Finalizing: {R4}
                    Leaving: {R3} {R2} {R1}
        }
        .finally {R4}
        {
            Block[B5] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B7]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a = this')
                      Operand: 
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a = this')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a = this')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                      Arguments(0)
                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B5] [B6]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B8] - Exit
        Predecessors: [B4]
        Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_18()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M(bool b)
    /*<bind>*/{
        label1:
        using var a = this;
        if (b)
            goto label1;
    }/*</bind>*/

}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [P a]
        Block[B1] - Block
            Predecessors: [B0] [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'a = this')
                  Left: 
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                  Right: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P) (Syntax: 'this')
            Next (Regular) Block[B2]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B2] - Block
                Predecessors: [B1]
                Statements (0)
                Jump if False (Regular) to Block[B6]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                    Finalizing: {R4}
                    Leaving: {R3} {R2} {R1}
                Next (Regular) Block[B1]
                    Finalizing: {R4}
                    Leaving: {R3} {R2}
        }
        .finally {R4}
        {
            Block[B3] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a = this')
                      Operand: 
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a = this')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a = this')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                      Arguments(0)
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B6] - Exit
        Predecessors: [B2]
        Statements (0)
";
            var expectedDiagnostics = new[]{
                // file.cs(14,13): error CS8649: A goto cannot jump to a location before a using declaration within the same block.
                //             goto label1;
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label1;").WithLocation(14, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_19()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M(bool b)
    /*<bind>*/{
        if (b)
            goto label1;
        int x = 0;
        label1:
        using var a = this;
        if (b)
            goto label1;
    }/*</bind>*/

}
";
            string expectedGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1}
    .locals {R1}
    {
        Locals: [System.Int32 x] [P a]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Jump if False (Regular) to Block[B2]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
            Next (Regular) Block[B3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 0')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'x = 0')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B1] [B2] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'a = this')
                  Left: 
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                  Right: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P) (Syntax: 'this')
            Next (Regular) Block[B4]
                Entering: {R2} {R3}
        .try {R2, R3}
        {
            Block[B4] - Block
                Predecessors: [B3]
                Statements (0)
                Jump if False (Regular) to Block[B8]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                    Finalizing: {R4}
                    Leaving: {R3} {R2} {R1}
                Next (Regular) Block[B3]
                    Finalizing: {R4}
                    Leaving: {R3} {R2}
        }
        .finally {R4}
        {
            Block[B5] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B7]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a = this')
                      Operand: 
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a = this')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a = this')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: a (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'a = this')
                      Arguments(0)
                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B5] [B6]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    Block[B8] - Exit
        Predecessors: [B4]
        Statements (0)
";
            var expectedDiagnostics = new[]{
                // file.cs(17,13): error CS8649: A goto cannot jump to a location before a using declaration within the same block.
                //             goto label1;
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label1;").WithLocation(17, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_20()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
ref struct P 
{
    public void Dispose()
    {
    }

    void M(bool b)
    /*<bind>*/{
            using var x = new P();
    }/*</bind>*/

}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [P x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation ( void P.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = new P()')
                  Instance Receiver: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_21()
        {
            string source = @"
ref struct P 
{
    public object Dispose() => null;

    void M(bool b)
    /*<bind>*/{
            using var x = new P();
    }/*</bind>*/

}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [P x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsInvalid, IsImplicit) (Syntax: 'x = new P()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'x = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P, IsInvalid) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'x = new P()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'x = new P()')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'x = new P()')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = new[]
            {
                    // file.cs(8,13): warning CS0280: 'P' does not implement the 'disposable' pattern. 'P.Dispose()' has the wrong signature.
                    //             using var x = new P();
                    Diagnostic(ErrorCode.WRN_PatternBadSignature, "using var x = new P();").WithArguments("P", "disposable", "P.Dispose()").WithLocation(8, 13),
                    // file.cs(8,13): error CS1674: 'P': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                    //             using var x = new P();
                    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "using var x = new P();").WithArguments("P").WithLocation(8, 13)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_22()
        {
            string source = @"
#pragma  warning disable CS0815, CS0219, CS0164
ref struct P 
{
    public void Dispose(int a = 1, bool b = true, params object[] extras)
    {
    }

    void M(bool b)
    /*<bind>*/{
            using var x = new P();
    }/*</bind>*/

}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [P x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation ( void P.Dispose([System.Int32 a = 1], [System.Boolean b = true], params System.Object[] extras)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = new P()')
                  Instance Receiver: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using var x = new P();')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'using var x = new P();')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using var x = new P();')
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'using var x = new P();')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using var x = new P();')
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'using var x = new P();')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'using var x = new P();')
                          Initializer: 
                            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'using var x = new P();')
                              Element Values(0)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_23()
        {
            string source = @"
using System.Threading.Tasks;
class P 
{
    public virtual Task DisposeAsync() => throw null;

    async Task M(bool b)
    /*<bind>*/{
            await using var x = new P();
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [P x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = new P()')
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'x = new P()')
                  Expression: 
                    IInvocationOperation (virtual System.Threading.Tasks.Task P.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task, IsImplicit) (Syntax: 'x = new P()')
                      Instance Receiver: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
                      Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingDeclaration_Flow_24()
        {
            string source = @"
using System.Threading.Tasks;
class P 
{
    public virtual Task DisposeAsync(int a = 1, bool b = true, params object[] extras) => throw null;

    async Task M(bool b)
    /*<bind>*/{
            await using var x = new P();
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [P x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
              Right: 
                IObjectCreationOperation (Constructor: P..ctor()) (OperationKind.ObjectCreation, Type: P) (Syntax: 'new P()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = new P()')
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'x = new P()')
                  Expression: 
                    IInvocationOperation (virtual System.Threading.Tasks.Task P.DisposeAsync([System.Int32 a = 1], [System.Boolean b = true], params System.Object[] extras)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task, IsImplicit) (Syntax: 'x = new P()')
                      Instance Receiver: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: P, IsImplicit) (Syntax: 'x = new P()')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using ...  = new P();')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'await using ...  = new P();')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using ...  = new P();')
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'await using ...  = new P();')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await using ...  = new P();')
                            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'await using ...  = new P();')
                              Dimension Sizes(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'await using ...  = new P();')
                              Initializer: 
                                IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'await using ...  = new P();')
                                  Element Values(0)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + IOperationTests_IForEachLoopStatement.s_ValueTask, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_SingleDeclaration()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using var c = new C();/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
    IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using var c = new C();')
      DeclarationGroup: 
        IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using var c = new C();')
          IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = new C()')
            Declarators:
                IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
                  Initializer: 
                    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                        Arguments(0)
                        Initializer: 
                          null
            Initializer: 
              null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_MultipleDeclarations()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
/*<bind>*/{
        using var c = new C();
        using var d = new C();
        using var e = new C();
    }  /*</bind>*/
}
";
            string expectedOperationTree = @"
    IBlockOperation (3 statements, 3 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: C c
        Local_2: C d
        Local_3: C e
      IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using var c = new C();')
        DeclarationGroup: 
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using var c = new C();')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = new C()')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
              Initializer: 
                null
      IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using var d = new C();')
        DeclarationGroup: 
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using var d = new C();')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var d = new C()')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: C d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd = new C()')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
              Initializer: 
                null
      IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using var e = new C();')
        DeclarationGroup: 
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using var e = new C();')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var e = new C()')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: C e) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e = new C()')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
              Initializer: 
                null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_SingleDeclaration_MultipleVariables()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using C c = new C(), d = new C(), e = new C();/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
    IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using C c = ...  = new C();')
      DeclarationGroup: 
        IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using C c = ...  = new C();')
          IVariableDeclarationOperation (3 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C c = new C ... e = new C()')
            Declarators:
                IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
                  Initializer: 
                    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                        Arguments(0)
                        Initializer: 
                          null
                IVariableDeclaratorOperation (Symbol: C d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd = new C()')
                  Initializer: 
                    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                        Arguments(0)
                        Initializer: 
                          null
                IVariableDeclaratorOperation (Symbol: C e) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e = new C()')
                  Initializer: 
                    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                        Arguments(0)
                        Initializer: 
                          null
            Initializer: 
              null";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_MultipleDeclaration_WithLabels()
        {
            string source = @"
using System;
#pragma warning disable CS0164
class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    /*<bind>*/{
    label1:
        using var a = new C();
    label2:
    label3:
        using var b = new C();
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockOperation (2 statements, 2 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: C a
    Local_2: C b
  ILabeledOperation (Label: label1) (OperationKind.Labeled, Type: null) (Syntax: 'label1: ...  = new C();')
    Statement: 
      IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using var a = new C();')
        DeclarationGroup: 
          IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using var a = new C();')
            IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var a = new C()')
              Declarators:
                  IVariableDeclaratorOperation (Symbol: C a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = new C()')
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
              Initializer: 
                null
  ILabeledOperation (Label: label2) (OperationKind.Labeled, Type: null) (Syntax: 'label2: ...  = new C();')
    Statement: 
      ILabeledOperation (Label: label3) (OperationKind.Labeled, Type: null) (Syntax: 'label3: ...  = new C();')
        Statement: 
          IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using var b = new C();')
            DeclarationGroup: 
              IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using var b = new C();')
                IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var b = new C()')
                  Declarators:
                      IVariableDeclaratorOperation (Symbol: C b) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'b = new C()')
                        Initializer: 
                          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                            IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                              Arguments(0)
                              Initializer: 
                                null
                  Initializer: 
                    null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_SingleDeclaration_Async()
        {
            string source = @"
using System;
using System.Threading.Tasks;

namespace System { interface IAsyncDisposable { } }

class C
{
    public Task DisposeAsync()
    {
        return default;
    }

    public static async Task M1()
    {
        /*<bind>*/await using var c = new C();/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
  DeclarationGroup: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_MultipleDeclarations_Async()
        {
            string source = @"
using System;
using System.Threading.Tasks;

namespace System { interface IAsyncDisposable { } }

class C
{
    public Task DisposeAsync()
    {
        return default;
    }

    public static async Task M1()
/*<bind>*/{
        await using var c = new C();
        await using var d = new C();
        await using var e = new C();
    }  /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockOperation (3 statements, 3 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: C c
    Local_2: C d
    Local_3: C e
  IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = new C()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
  IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var d = new C()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: C d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
  IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var e = new C()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: C e) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_SingleDeclaration_MultipleVariables_Async()
        {
            string source = @"
using System;
using System.Threading.Tasks;

namespace System { interface IAsyncDisposable { } }

class C
{
    public Task DisposeAsync()
    {
        return default;
    }

    public static async Task M1()
    {
        /*<bind>*/await using C c = new C(), d = new C(), e = new C();/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
  DeclarationGroup: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
      IVariableDeclarationOperation (3 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C c = new C ... e = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
            IVariableDeclaratorOperation (Symbol: C d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
            IVariableDeclaratorOperation (Symbol: C e) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(32100, "https://github.com/dotnet/roslyn/issues/32100")]
        public void UsingDeclaration_RegularAsync_Mix()
        {
            string source = @"
using System;
using System.Threading.Tasks;

namespace System { interface IAsyncDisposable { } }

class C : IDisposable
{
    public Task DisposeAsync()
    {
        return default;
    }

    public void Dispose()
    {
    }

    public static async Task M1()
/*<bind>*/{
        using C c = new C();
        await using C d = new C();
        using C e = new C(), f = new C();
        await using C g = new C(), h = new C();
    }  /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockOperation (4 statements, 6 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: C c
    Local_2: C d
    Local_3: C e
    Local_4: C f
    Local_5: C g
    Local_6: C h
  IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using C c = new C();')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using C c = new C();')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C c = new C()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
  IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C d = new C()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: C d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
  IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using C e = ...  = new C();')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using C e = ...  = new C();')
        IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C e = new C ... f = new C()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: C e) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
              IVariableDeclaratorOperation (Symbol: C f) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'f = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
  IUsingDeclarationOperation(IsAsynchronous: True, DisposeMethod: System.Threading.Tasks.Task C.DisposeAsync()) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'await using ...  = new C();')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'await using ...  = new C();')
        IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C g = new C ... h = new C()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: C g) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'g = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
              IVariableDeclaratorOperation (Symbol: C h) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'h = new C()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void UsingDeclaration_DefaultDisposeArguments()
        {
            string source = @"
class C
{
    public static void M1()
    {
        /*<bind>*/using var s = new S();/*</bind>*/
    }
}

ref struct S
{
    public void Dispose(int a = 1, bool b = true, params object[] others) { }
}
";
            string expectedOperationTree = @"
IUsingDeclarationOperation(IsAsynchronous: False, DisposeMethod: void S.Dispose([System.Int32 a = 1], [System.Boolean b = true], params System.Object[] others)) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using var s = new S();')
  DeclarationGroup:
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using var s = new S();')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var s = new S()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: S s) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's = new S()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new S()')
                  IObjectCreationOperation (Constructor: S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'new S()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  DisposeArguments(3):
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using var s = new S();')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'using var s = new S();')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using var s = new S();')
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'using var s = new S();')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: others) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'using var s = new S();')
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'using var s = new S();')
          Dimension Sizes(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'using var s = new S();')
          Initializer: 
            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'using var s = new S();')
              Element Values(0)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void UsingDeclaration_LocalFunctionDefinedAfterUsingReferenceBeforeUsing()
        {
            var comp = CreateCompilation(@"
using System;

class C
{
    void M()
    /*<bind>*/{
        localFunc2();
        static void localFunc() {}

        using IDisposable i = null;
        localFunc();

        static void localFunc2() {}
        localFunc3();

        static void localFunc3() {}
    }/*</bind>*/
}
");

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (7 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: System.IDisposable i
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'localFunc2();')
    Expression: 
      IInvocationOperation (void localFunc2()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'localFunc2()')
        Instance Receiver: 
          null
        Arguments(0)
  ILocalFunctionOperation (Symbol: void localFunc()) (OperationKind.LocalFunction, Type: null) (Syntax: 'static void ... alFunc() {}')
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{}')
        ReturnedValue: 
          null
  IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using IDisp ... e i = null;')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using IDisp ... e i = null;')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'IDisposable i = null')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.IDisposable i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = null')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= null')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          Initializer: 
            null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'localFunc();')
    Expression: 
      IInvocationOperation (void localFunc()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'localFunc()')
        Instance Receiver: 
          null
        Arguments(0)
  ILocalFunctionOperation (Symbol: void localFunc2()) (OperationKind.LocalFunction, Type: null) (Syntax: 'static void ... lFunc2() {}')
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{}')
        ReturnedValue: 
          null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'localFunc3();')
    Expression: 
      IInvocationOperation (void localFunc3()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'localFunc3()')
        Instance Receiver: 
          null
        Arguments(0)
  ILocalFunctionOperation (Symbol: void localFunc3()) (OperationKind.LocalFunction, Type: null) (Syntax: 'static void ... lFunc3() {}')
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '{}')
        ReturnedValue: 
          null
", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.IDisposable i]
    Methods: [void localFunc()] [void localFunc2()] [void localFunc3()]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'localFunc2();')
              Expression: 
                IInvocationOperation (void localFunc2()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'localFunc2()')
                  Instance Receiver: 
                    null
                  Arguments(0)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.IDisposable, IsImplicit) (Syntax: 'i = null')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.IDisposable, IsImplicit) (Syntax: 'i = null')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'localFunc();')
                  Expression: 
                    IInvocationOperation (void localFunc()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'localFunc()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'localFunc3();')
                  Expression: 
                    IInvocationOperation (void localFunc3()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'localFunc3()')
                      Instance Receiver: 
                        null
                      Arguments(0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i = null')
                  Operand: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.IDisposable, IsImplicit) (Syntax: 'i = null')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'i = null')
                  Instance Receiver: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.IDisposable, IsImplicit) (Syntax: 'i = null')
                  Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
    
    {   void localFunc()
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Exit
            Predecessors: [B0#0R1]
            Statements (0)
    }
    
    {   void localFunc2()
    
        Block[B0#1R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#1R1]
        Block[B1#1R1] - Exit
            Predecessors: [B0#1R1]
            Statements (0)
    }
    
    {   void localFunc3()
    
        Block[B0#2R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#2R1]
        Block[B1#2R1] - Exit
            Predecessors: [B0#2R1]
            Statements (0)
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void UsingDeclaration_LocalDefinedAfterUsingReferenceBeforeUsing()
        {
            var comp = CreateCompilation(@"
using System;

class C
{
    void M()
    /*<bind>*/{
        _ = local;
        using IDisposable i = null;
        object local = null;
    }/*</bind>*/
}
");

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (8,13): error CS0841: Cannot use local variable 'local' before it is declared
                //         _ = local;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "local").WithArguments("local").WithLocation(8, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (3 statements, 2 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: System.IDisposable i
    Local_2: System.Object local
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_ = local;')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: var, IsInvalid) (Syntax: '_ = local')
        Left: 
          IDiscardOperation (Symbol: var _) (OperationKind.Discard, Type: var) (Syntax: '_')
        Right: 
          ILocalReferenceOperation: local (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'local')
  IUsingDeclarationOperation(IsAsynchronous: False) (OperationKind.UsingDeclaration, Type: null) (Syntax: 'using IDisp ... e i = null;')
    DeclarationGroup: 
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'using IDisp ... e i = null;')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'IDisposable i = null')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.IDisposable i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = null')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= null')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          Initializer: 
            null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'object local = null;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'object local = null')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Object local) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'local = null')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= null')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
      Initializer: 
        null
", expectedDiagnostics);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.IDisposable i] [System.Object local]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_ = local;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: var, IsInvalid) (Syntax: '_ = local')
                  Left: 
                    IDiscardOperation (Symbol: var _) (OperationKind.Discard, Type: var) (Syntax: '_')
                  Right: 
                    ILocalReferenceOperation: local (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'local')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.IDisposable, IsImplicit) (Syntax: 'i = null')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.IDisposable, IsImplicit) (Syntax: 'i = null')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'local = null')
                  Left: 
                    ILocalReferenceOperation: local (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'local = null')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i = null')
                  Operand: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.IDisposable, IsImplicit) (Syntax: 'i = null')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'i = null')
                  Instance Receiver: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.IDisposable, IsImplicit) (Syntax: 'i = null')
                  Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void UsingDeclaration_InsideSwitchCaseEmbeddedStatements()
        {
            var source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { }
}
class C2
{
    public static void M(int x)
    /*<bind>*/{
        switch (x)
        {
            case 5:
                using C1 o1 = new C1();
                break;
        }
    }/*</bind>*/
}";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(14,17): error CS8647: A using variable cannot be used directly within a switch section (consider using braces). 
                //                 using C1 o1 = new C1();
                Diagnostic(ErrorCode.ERR_UsingVarInSwitchCase, "using C1 o1 = new C1();").WithLocation(14, 17)
            };

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        Locals: [C1 o1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '5')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                Leaving: {R2} {R1}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                  Left: 
                    ILocalReferenceOperation: o1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                  Right: 
                    IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B4]
                Entering: {R3} {R4}
        .try {R3, R4}
        {
            Block[B4] - Block
                Predecessors: [B3]
                Statements (0)
                Next (Regular) Block[B8]
                    Finalizing: {R5}
                    Leaving: {R4} {R3} {R2} {R1}
        }
        .finally {R5}
        {
            Block[B5] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B7]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                      Operand: 
                        ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                      Arguments(0)
                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B5] [B6]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
}
Block[B8] - Exit
    Predecessors: [B2] [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void UsingDeclaration_InsideIfEmbeddedStatement()
        {
            var source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { }
}
class C2
{
    public static void M(bool b)
    /*<bind>*/{
        if (b)
            using C1 o1 = new C1();
    }/*</bind>*/
}";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(12,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             using C1 o1 = new C1();
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "using C1 o1 = new C1();").WithLocation(12, 13)
            };

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
    Next (Regular) Block[B2]
        Entering: {R1}
.locals {R1}
{
    Locals: [C1 o1]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
              Left: 
                ILocalReferenceOperation: o1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
              Right: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B3]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Regular) Block[B7]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                  Operand: 
                    ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                  Arguments(0)
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B7] - Exit
    Predecessors: [B1] [B3]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void UsingDeclaration_InsideForEmbeddedStatement()
        {
            var source = @"
using System;
using System.Collections;
class C1 : IDisposable
{
    public void Dispose() { }
}
class C2
{
    public static void M()
    /*<bind>*/{
        for (;;)
            using C1 o1 = new C1();
    }/*</bind>*/
}";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(13,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             using C1 o1 = new C1();
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "using C1 o1 = new C1();").WithLocation(13, 13)
            };

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C1 o1]
    Block[B1] - Block
        Predecessors: [B0] [B6]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
              Left: 
                ILocalReferenceOperation: o1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
              Right: 
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                  Operand: 
                    ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                  Arguments(0)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B6] - Block
    Predecessors: [B2]
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
Block[B7] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void UsingDeclaration_InsideForEachEmbeddedStatement()
        {
            var source = @"
using System;
using System.Collections;
class C1 : IDisposable
{
    public void Dispose() { }
}
class C2
{
    public static void M(IEnumerable e)
    /*<bind>*/{
        foreach (var o in e)
            using C1 o1 = new C1();
    }/*</bind>*/
}";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(13,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             using C1 o1 = new C1();
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "using C1 o1 = new C1();").WithLocation(13, 13)
            };

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation (virtual System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Collections.IEnumerable) (Syntax: 'e')
                  Arguments(0)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B5]
            Statements (0)
            Jump if False (Regular) to Block[B12]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')
                  Arguments(0)
                Finalizing: {R9}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Object o]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: o (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Object System.Collections.IEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')
                Next (Regular) Block[B4]
                    Entering: {R5}
            .locals {R5}
            {
                Locals: [C1 o1]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                          Left: 
                            ILocalReferenceOperation: o1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                          Right: 
                            IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1()')
                              Arguments(0)
                              Initializer: 
                                null
                    Next (Regular) Block[B5]
                        Entering: {R6} {R7}
                .try {R6, R7}
                {
                    Block[B5] - Block
                        Predecessors: [B4]
                        Statements (0)
                        Next (Regular) Block[B2]
                            Finalizing: {R8}
                            Leaving: {R7} {R6} {R5} {R4}
                }
                .finally {R8}
                {
                    Block[B6] - Block
                        Predecessors (0)
                        Statements (0)
                        Jump if True (Regular) to Block[B8]
                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                              Operand: 
                                ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                        Next (Regular) Block[B7]
                    Block[B7] - Block
                        Predecessors: [B6]
                        Statements (1)
                            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                              Instance Receiver: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand: 
                                    ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'o1 = new C1()')
                              Arguments(0)
                        Next (Regular) Block[B8]
                    Block[B8] - Block
                        Predecessors: [B6] [B7]
                        Statements (0)
                        Next (StructuredExceptionHandling) Block[null]
                }
            }
        }
    }
    .finally {R9}
    {
        CaptureIds: [1]
        Block[B9] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')
            Jump if True (Regular) to Block[B11]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
            Next (Regular) Block[B10]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
                  Arguments(0)
            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B12] - Exit
    Predecessors: [B2]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
