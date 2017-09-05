// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/M1/*</bind>*/;
    }
    void M1() { }
}
";

            // Currently, GetOperation returns a straight BoundMethodGroup for the IdentifierNameSyntax. We can't directly
            // convert a BoundMethodGroup to an IDelegateCreationExpression, so this returns OperationKind.None. When Heejae's
            // rewrite of GetOperation is complete, this should return the IDelegateCreationExpression.
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidIdentifier()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/M1/*</bind>*/;
    }
}
";

            // Currently, GetOperation returns a straight BoundMethodGroup for the IdentifierNameSyntax. We can't directly
            // convert a BoundMethodGroup to an IDelegateCreationExpression, so this returns OperationKind.None. When Heejae's
            // rewrite of GetOperation is complete, this should return the IDelegateCreationExpression.
            string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'M1')
  Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // CS0103: The name 'M1' does not exist in the current context
                //         Action a = /*<bind>*/M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/M1/*</bind>*/;
    }
    int M1() => 1;
}
";

            // Currently, GetOperation returns a straight BoundMethodGroup for the IdentifierNameSyntax. We can't directly
            // convert a BoundMethodGroup to an IDelegateCreationExpression, so this returns OperationKind.None. When Heejae's
            // rewrite of GetOperation is complete, this should return the IDelegateCreationExpression.
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "int").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ImplicitMethodBinding_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/M1/*</bind>*/;
    }
    void M1(object o) { }
}
";

            // Currently, GetOperation returns a straight BoundMethodGroup for the IdentifierNameSyntax. We can't directly
            // convert a BoundMethodGroup to an IDelegateCreationExpression, so this returns OperationKind.None. When Heejae's
            // rewrite of GetOperation is complete, this should return the IDelegateCreationExpression.
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // CS0123: No overload for 'M1' matches delegate 'Action'
                //         Action a = /*<bind>*/M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M1").WithArguments("M1", "System.Action").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)M1/*</bind>*/;
    }
    void M1() { }
}
";
            string expectedOperationTree = @"
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: '(Action)M1')
  Target: IMethodBindingExpression: void Program.M1() (OperationKind.MethodBindingExpression, Type: null) (Syntax: 'M1')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidIdentifier()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)M1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'M1')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M1' does not exist in the current context
                //         Action a = /*<bind>*/(Action)M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)M1/*</bind>*/;
    }
    int M1() => 1;
}
";
            string expectedOperationTree = @"
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
  Target: IMethodBindingExpression: System.Int32 Program.M1() (OperationKind.MethodBindingExpression, Type: null, IsInvalid) (Syntax: 'M1')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program, IsInvalid) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/(Action)M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "(Action)M1").WithArguments("Program.M1()", "int").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitMethodBinding_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/(Action)M1/*</bind>*/;
    }
    void M1(object o) { }
}
";
            string expectedOperationTree = @"
IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: '(Action)M1')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // CS0030: Cannot convert type 'method' to 'Action'
                //         Action a = /*<bind>*/(Action)M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Action)M1").WithArguments("method", "System.Action").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    void M1()
    { }
}
";
            string expectedOperationTree = @"
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'new Action(M1)')
  Target: IMethodBindingExpression: void Program.M1() (OperationKind.MethodBindingExpression, Type: null) (Syntax: 'M1')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversion_InvalidMissingIdentifier()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Children(1):
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'M1')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M1' does not exist in the current context
                //         Action a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversion_InvalidReturnType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    int M1() => 1;
}
";
            string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Children(1):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0407: 'int Program.M1()' has the wrong return type
                //         Action a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "int").WithLocation(7, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndImplicitMethodBindingConversion_InvalidArgumentType()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action(M1)/*</bind>*/;
    }

    void M1(object o)
    { }
}
";
            string expectedOperationTree = @"
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Action, IsInvalid) (Syntax: 'new Action(M1)')
  Children(1):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0123: No overload for 'M1' matches delegate 'Action'
                //         Action a = /*<bind>*/new Action(M1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "new Action(M1)").WithArguments("M1", "System.Action").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        // TODO: Invalid tests for below function

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DelegateCreationExpression_ExplicitDelegateConstructorAndExplicitMethodBindingConversion()
        {
            string source = @"
using System;
class Program
{
    void Main()
    {
        Action a = /*<bind>*/new Action((Action)M1)/*</bind>*/;
    }
    void M1() {}
}
";
            string expectedOperationTree = @"
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'new Action((Action)M1)')
  Target: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: '(Action)M1')
      Target: IMethodBindingExpression: void Program.M1() (OperationKind.MethodBindingExpression, Type: null) (Syntax: 'M1')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
