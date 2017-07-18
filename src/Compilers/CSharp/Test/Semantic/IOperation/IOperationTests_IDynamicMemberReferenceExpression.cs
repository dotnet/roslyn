using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
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
IDynamicMemberReferenceExpression (Member name: Prop1, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.Prop1')
  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IDynamicMemberReferenceExpression (Member name: , Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic, IsInvalid) (Syntax: 'd./*</bind>*/')
  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //             int i = /*<bind>*/d./*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(11, 44)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IOperation:  (OperationKind.None) (Syntax: 'd.GetValue()')
  Children(1):
      IDynamicMemberReferenceExpression (Member name: GetValue, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.GetValue')
        Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'd.()')
  Children(1):
      IDynamicMemberReferenceExpression (Member name: , Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic, IsInvalid) (Syntax: 'd.')
        Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //             /*<bind>*/d.()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(9, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'd.GetValue(/*</bind>*/')
  Children(1):
      IDynamicMemberReferenceExpression (Member name: GetValue, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.GetValue')
        Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1026: ) expected
                //             /*<bind>*/d.GetValue(/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(9, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IOperation:  (OperationKind.None) (Syntax: 'd.GetValue<int>()')
  Children(1):
      IDynamicMemberReferenceExpression (Member name: GetValue, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.GetValue<int>')
        Type Arguments: System.Int32
        Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IOperation:  (OperationKind.None) (Syntax: 'd.GetValue<int, C1>()')
  Children(1):
      IDynamicMemberReferenceExpression (Member name: GetValue, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.GetValue<int, C1>')
        Type Arguments:
          System.Int32
          ConsoleApp1.C1
        Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IDynamicMemberReferenceExpression (Member name: GetValue, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue<int, C1>')
  Type Arguments:
    System.Int32
    ConsoleApp1.C1
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
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'd.GetValue<int,>()')
  Children(1):
      IDynamicMemberReferenceExpression (Member name: GetValue, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue<int,>')
        Type Arguments:
          System.Int32
          ?
        Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1031: Type expected
                //             /*<bind>*/d.GetValue<int,>()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(9, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IDynamicMemberReferenceExpression (Member name: Prop2, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.Prop1.Prop2')
  Instance Receiver: IDynamicMemberReferenceExpression (Member name: Prop1, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.Prop1')
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IOperation:  (OperationKind.None) (Syntax: 'd.Method1().Method2()')
  Children(1):
      IDynamicMemberReferenceExpression (Member name: Method2, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.Method1().Method2')
        Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'd.Method1()')
            Children(1):
                IDynamicMemberReferenceExpression (Member name: Method1, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.Method1')
                  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IDynamicMemberReferenceExpression (Member name: Prop2, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.Method1<int>().Prop2')
  Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'd.Method1<int>()')
      Children(1):
          IDynamicMemberReferenceExpression (Member name: Method1, Containing Type: null) (OperationKind.DynamicAccessExpression, Type: dynamic) (Syntax: 'd.Method1<int>')
            Type Arguments: System.Int32
            Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
