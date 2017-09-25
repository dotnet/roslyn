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
        public void IConditionalAccessExpression_SimpleMethodAccess()

        {
            string source = @"
using System;

public class C1
{
    public void M()
    {
        var o = new object();
        /*<bind>*/o?.ToString()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConditionalAccessExpression (OperationKind.ConditionalAccessExpression, Type: System.String, Language: C#) (Syntax: 'o?.ToString()')
  Expression: 
    ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object, Language: C#) (Syntax: 'o')
  WhenNotNull: 
    IInvocationExpression (virtual System.String System.Object.ToString()) (OperationKind.InvocationExpression, Type: System.String, Language: C#) (Syntax: '.ToString()')
      Instance Receiver: 
        IConditionalAccessInstanceExpression (OperationKind.ConditionalAccessInstanceExpression, Type: System.Object, IsImplicit, Language: C#) (Syntax: 'o')
      Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ConditionalAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IConditionalAccessExpression_SimplePropertyAccess()
        {
            string source = @"
using System;

public class C1
{
    int Prop1 { get; }
    public void M()
    {
        C1 c1 = null;
        var prop = /*<bind>*/c1?.Prop1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConditionalAccessExpression (OperationKind.ConditionalAccessExpression, Type: System.Int32?, Language: C#) (Syntax: 'c1?.Prop1')
  Expression: 
    ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: C1, Language: C#) (Syntax: 'c1')
  WhenNotNull: 
    IPropertyReferenceExpression: System.Int32 C1.Prop1 { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, Language: C#) (Syntax: '.Prop1')
      Instance Receiver: 
        IConditionalAccessInstanceExpression (OperationKind.ConditionalAccessInstanceExpression, Type: C1, IsImplicit, Language: C#) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ConditionalAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }


    }
}
