// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseConditionalExpression
{
    public partial class UseConditionalExpressionForAssignmentTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer(),
                new CSharpUseConditionalExpressionForAssignmentCodeRefactoringProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleAssignment()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M(int i)
    {
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            i = 1;
        }
    }
}",
@"
class C
{
    void M(int i)
    {
        i = true ? 0 : 1;
    }
}");
        }
    }
}
