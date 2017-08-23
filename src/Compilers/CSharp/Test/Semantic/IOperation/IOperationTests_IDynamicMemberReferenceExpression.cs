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
        public void IDynamicMemberReferenceExpression_SimplePropertyAccess()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            int i = /*<bind>*/d.Prop1/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceExpression (Member Name: ""Prop1"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Prop1')
  Type Arguments(0)
  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_InvalidPropertyAccess()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            int i = /*<bind>*/d./*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceExpression (Member Name: """", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic, IsInvalid) (Syntax: 'd./*</bind>*/')
  Type Arguments(0)
  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //             int i = /*<bind>*/d./*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(11, 44)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_SimpleMethodCall()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'd.GetValue()')
  Expression: IDynamicMemberReferenceExpression (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.GetValue')
      Type Arguments(0)
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_InvalidMethodCall_MissingName()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic, IsInvalid) (Syntax: 'd.()')
  Expression: IDynamicMemberReferenceExpression (Member Name: """", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic, IsInvalid) (Syntax: 'd.')
      Type Arguments(0)
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //             /*<bind>*/d.()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(9, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_InvalidMethodCall_MissingCloseParen()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue(/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue(/*</bind>*/')
  Expression: IDynamicMemberReferenceExpression (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.GetValue')
      Type Arguments(0)
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1026: ) expected
                //             /*<bind>*/d.GetValue(/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(9, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReference_GenericMethodCall_SingleGeneric()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue<int>()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'd.GetValue<int>()')
  Expression: IDynamicMemberReferenceExpression (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.GetValue<int>')
      Type Arguments(1):
        Symbol: System.Int32
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReference_GenericMethodCall_MultipleGeneric()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue<int, C1>()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'd.GetValue<int, C1>()')
  Expression: IDynamicMemberReferenceExpression (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.GetValue<int, C1>')
      Type Arguments(2):
        Symbol: System.Int32
        Symbol: ConsoleApp1.C1
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_GenericPropertyAccess()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue<int, C1>/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceExpression (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue<int, C1>')
  Type Arguments(2):
    Symbol: System.Int32
    Symbol: ConsoleApp1.C1
  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic, IsInvalid) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0307: The property 'GetValue' cannot be used with type arguments
                //             /*<bind>*/d.GetValue<int, C1>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "GetValue<int, C1>").WithArguments("GetValue", "property").WithLocation(9, 25),
                // CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //             /*<bind>*/d.GetValue<int, C1>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "d.GetValue<int, C1>").WithLocation(9, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_GenericMethodCall_InvalidGenericParam()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue<int,>()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue<int,>()')
  Expression: IDynamicMemberReferenceExpression (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue<int,>')
      Type Arguments(2):
        Symbol: System.Int32
        Symbol: ?
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1031: Type expected
                //             /*<bind>*/d.GetValue<int,>()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(9, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_NestedDynamicPropertyAccess()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            object o = /*<bind>*/d.Prop1.Prop2/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceExpression (Member Name: ""Prop2"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Prop1.Prop2')
  Type Arguments(0)
  Instance Receiver: IDynamicMemberReferenceExpression (Member Name: ""Prop1"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Prop1')
      Type Arguments(0)
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_NestedDynamicMethodAccess()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.Method1().Method2()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'd.Method1().Method2()')
  Expression: IDynamicMemberReferenceExpression (Member Name: ""Method2"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Method1().Method2')
      Type Arguments(0)
      Instance Receiver: IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'd.Method1()')
          Expression: IDynamicMemberReferenceExpression (Member Name: ""Method1"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Method1')
              Type Arguments(0)
              Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
          ApplicableSymbols(0)
          Arguments(0)
          ArgumentNames(0)
          ArgumentRefKinds(0)
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_NestedDynamicPropertyAndMethodAccess()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            int i = /*<bind>*/d.Method1<int>().Prop2/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceExpression (Member Name: ""Prop2"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Method1<int>().Prop2')
  Type Arguments(0)
  Instance Receiver: IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'd.Method1<int>()')
      Expression: IDynamicMemberReferenceExpression (Member Name: ""Method1"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Method1<int>')
          Type Arguments(1):
            Symbol: System.Int32
          Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
      ApplicableSymbols(0)
      Arguments(0)
      ArgumentNames(0)
      ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
