// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void FieldReference_Attribute()
        {
            string source = @"
using System.Diagnostics;

class C
{
    private const string field = nameof(field);

    [/*<bind>*/Conditional(field)/*</bind>*/]
    void M()
    {
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'Conditional(field)')
  Children(1): IFieldReferenceExpression: System.String C.field (Static) (OperationKind.FieldReferenceExpression, Type: System.String, Constant: ""field"") (Syntax: 'field')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
