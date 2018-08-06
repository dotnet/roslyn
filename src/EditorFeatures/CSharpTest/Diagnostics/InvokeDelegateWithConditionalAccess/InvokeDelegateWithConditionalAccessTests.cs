// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.InvokeDelegateWithConditionalAccess
{
    public partial class InvokeDelegateWithConditionalAccessTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new InvokeDelegateWithConditionalAccessAnalyzer(), new InvokeDelegateWithConditionalAccessCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task Test1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestOnIf()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        [||]if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestOnInvoke()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        if (v != null)
        {
            [||]v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        [WorkItem(13226, "https://github.com/dotnet/roslyn/issues/13226")]
        public async Task TestMissingBeforeCSharp6()
        {
            await TestMissingAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }
    }
}", new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestInvertedIf()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a;
        if (null != v)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestIfWithNoBraces()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a;
        if (null != v)
            v();
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestWithComplexExpression()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        bool b = true;
        [||]var v = b ? a : null;
        if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        bool b = true;
        (b ? a : null)?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingWithElseClause()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }
        else
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingOnDeclarationWithMultipleVariables()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a, x = a;
        if (v != null)
        {
            v();
        }
    }
}");
        }

        /// <remarks>
        /// With multiple variables in the same declaration, the fix _is not_ offered on the declaration
        /// itself, but _is_ offered on the invocation pattern.
        /// </remarks>
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestLocationWhereOfferedWithMultipleVariables()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a, x = a;
        [||]if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a, x = a;
        v?.Invoke();
    }
}");
        }

        /// <remarks>
        /// If we have a variable declaration and if it is read/written outside the delegate 
        /// invocation pattern, the fix is not offered on the declaration.
        /// </remarks>
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingOnDeclarationIfUsedOutside()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }

        v = null;
    }
}");
        }

        /// <remarks>
        /// If we have a variable declaration and if it is read/written outside the delegate 
        /// invocation pattern, the fix is not offered on the declaration but is offered on
        /// the invocation pattern itself.
        /// </remarks>
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestLocationWhereOfferedIfUsedOutside()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        [||]if (v != null)
        {
            v();
        }

        v = null;
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        v?.Invoke();

        v = null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestSimpleForm1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public event EventHandler E;

    void M()
    {
        [||]if (this.E != null)
        {
            this.E(this, EventArgs.Empty);
        }
    }
}",
@"using System;

class C
{
    public event EventHandler E;

    void M()
    {
        this.E?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestSimpleForm2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public event EventHandler E;

    void M()
    {
        if (this.E != null)
        {
            [||]this.E(this, EventArgs.Empty);
        }
    }
}",
@"using System;

class C
{
    public event EventHandler E;

    void M()
    {
        this.E?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestInElseClause1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public event EventHandler E;

    void M()
    {
        if (true != true)
        {
        }
        else [||]if (this.E != null)
        {
            this.E(this, EventArgs.Empty);
        }
    }
}",
@"using System;

class C
{
    public event EventHandler E;

    void M()
    {
        if (true != true)
        {
        }
        else
        {
            this.E?.Invoke(this, EventArgs.Empty);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestInElseClause2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public event EventHandler E;

    void M()
    {
        if (true != true)
        {
        }
        else [||]if (this.E != null)
            this.E(this, EventArgs.Empty);
    }
}",
@"using System;

class C
{
    public event EventHandler E;

    void M()
    {
        if (true != true)
        {
        }
        else this.E?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;
    void Goo()
    {
        // Comment
        [||]var v = a;
        if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;
    void Goo()
    {
        // Comment
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestTrivia2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;
    void Goo()
    {
        // Comment
        [||]if (a != null)
        {
            a();
        }
    }
}",
@"class C
{
    System.Action a;
    void Goo()
    {
        // Comment
        a?.Invoke();
    }
}");
        }

        /// <remarks>
        /// tests locations where the fix is offered.
        /// </remarks>
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestFixOfferedOnIf()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        [||]if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();
    }
}");
        }

        /// <remarks>
        /// tests locations where the fix is offered.
        /// </remarks>
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestFixOfferedInsideIf()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        if (v != null)
        {
            [||]v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingOnConditionalInvocation()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a;
        v?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingOnConditionalInvocation2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        [||]v?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingOnConditionalInvocation3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingOnNonNullCheckExpressions()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        if (v == a)
        {
            [||]v();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingOnNonNullCheckExpressions2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        if (v == null)
        {
            [||]v();
        }
    }
}");
        }

        /// <remarks>
        /// if local declaration is not immediately preceding the invocation pattern, 
        /// the fix is not offered on the declaration.
        /// </remarks>
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestLocalNotImmediatelyPrecedingNullCheckAndInvokePattern()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        [||]var v = a;
        int x;
        if (v != null)
        {
            v();
        }
    }
}");
        }

        /// <remarks>
        /// if local declaration is not immediately preceding the invocation pattern, 
        /// the fix is not offered on the declaration but is offered on the invocation pattern itself.
        /// </remarks>
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestLocalDNotImmediatelyPrecedingNullCheckAndInvokePattern2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        int x;
        [||]if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;

    void Goo()
    {
        var v = a;
        int x;
        v?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task TestMissingOnFunc()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    System.Func<int> a;

    int Goo()
    {
        var v = a;
        [||]if (v != null)
        {
            return v();
        }
    }
}");
        }
    }
}
