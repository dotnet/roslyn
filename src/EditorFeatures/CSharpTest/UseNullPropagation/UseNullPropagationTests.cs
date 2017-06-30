// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseNullPropagation;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNullPropagation
{
    public partial class UseNullPropagationTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseNullPropagationDiagnosticAnalyzer(), new CSharpUseNullPropagationCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestLeft_Equals()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o == null ? null : o.ToString();
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v = o?.ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestMissingOnCSharp5()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o == null ? null : o.ToString();
    }
}", new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestRight_Equals()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]null == o ? null : o.ToString();
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v = o?.ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestLeft_NotEquals()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o != null ? o.ToString() : null;
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v = o?.ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestRight_NotEquals()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]null != o ? o.ToString() : null;
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v = o?.ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestIndexer()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o == null ? null : o[0];
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v = o?[0];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestConditionalAccess()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o == null ? null : o.B?.C;
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v = o?.B?.C;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestMemberAccess()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o == null ? null : o.B;
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v = o?.B;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestMissingOnSimpleMatch()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o == null ? null : o;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestParenthesizedCondition()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||](o == null) ? null : o.ToString();
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v = o?.ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v1 = {|FixAllInDocument:o|} == null ? null : o.ToString();
        var v2 = o != null ? o.ToString() : null;
    }
}",
@"using System;

class C
{
    void M(object o)
    {
        var v1 = o?.ToString();
        var v2 = o?.ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o1, object o2)
    {
        var v1 = {|FixAllInDocument:o1|} == null ? null : o1.ToString(o2 == null ? null : o2.ToString());
    }
}",
@"using System;

class C
{
    void M(object o1, object o2)
    {
        var v1 = o1?.ToString(o2?.ToString());
    }
}");
        }

        [WorkItem(15505, "https://github.com/dotnet/roslyn/issues/15505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestOtherValueIsNotNull1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o == null ? 0 : o.ToString();
    }
}");
        }

        [WorkItem(15505, "https://github.com/dotnet/roslyn/issues/15505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestOtherValueIsNotNull2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M(object o)
    {
        var v = [||]o != null ? o.ToString() : 0;
    }
}");
        }

        [WorkItem(16287, "https://github.com/dotnet/roslyn/issues/16287")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestMethodGroup()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;

class D
{
    void Foo()
    {
        var c = new C();
        Action<string> a = [||]c != null ? c.M : (Action<string>)null;
    }
}
class C { public void M(string s) { } }");
        }

        [WorkItem(17623, "https://github.com/dotnet/roslyn/issues/17623")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestInExpressionTree()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System;
using System.Linq.Expressions;

class Program
{
    void Main(string s)
    {
        Method<string>(t => [||]s != null ? s.ToString() : null); // works
    }

    public void Method<T>(Expression<Func<T, string>> functor)
    {
    }
}");
        }

        [WorkItem(19774, "https://github.com/dotnet/roslyn/issues/19774")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestNullableMemberAccess()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void Main(DateTime? toDate)
    {
        var v = [||]toDate == null ? null : toDate.Value.ToString(""yyyy/MM/ dd"");
    }
}
",

@"
using System;

class C
{
    void Main(DateTime? toDate)
    {
        var v = toDate?.ToString(""yyyy/MM/ dd"");
    }
}
");
        }

        [WorkItem(19774, "https://github.com/dotnet/roslyn/issues/19774")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
        public async Task TestNullableElementAccess()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

struct S
{
    public string this[int i] => """";
}

class C
{
    void Main(S? s)
    {
        var x = [||]s == null ? null : s.Value[0];
    }
}
",

@"
using System;

struct S
{
    public string this[int i] => """";
}

class C
{
    void Main(S? s)
    {
        var x = s?[0];
    }
}
");
        }
    }
}
