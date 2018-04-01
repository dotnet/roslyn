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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotOnSimpleAssignmentToDifferentTargets()
        {
            await TestMissingAsync(
@"
class C
{
    void M(int i, int j)
    {
        [||]if (true)
        {
            i = 0;
        }
        else
        {
            j = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToUndefinedField()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            this.i = 0;
        }
        else
        {
            this.i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        this.i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnNonUniformTargetSyntax()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            this.i = 0;
        }
        else
        {
            this . i = 1;
        }
    }
}",
@"
class C
{
    void M()
    {
        this.i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToDefinedField()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    int i;

    void M()
    {
        [||]if (true)
        {
            this.i = 0;
        }
        else
        {
            this.i = 1;
        }
    }
}",
@"
class C
{
    int i;

    void M()
    {
        this.i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalNoInitializer()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        int i;
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
    void M()
    {
        var i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalLiteralInitializer()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        int i = 0;
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
    void M()
    {
        var i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalDefaultLiteralInitializer()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        int i = default;
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
    void M()
    {
        var i = true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnAssignmentToAboveLocalDefaultExpressionInitializer()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M()
    {
        int i = default(int);
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
    void M()
    {
        var i = true ? 0 : 1;
    }
}");
        }
    }
}
