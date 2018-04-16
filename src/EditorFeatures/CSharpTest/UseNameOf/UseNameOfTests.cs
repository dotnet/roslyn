using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseNameOf;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNameOf
{
    public class UseNameOfTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseNameOfDiagnosticAnalyzer(), new CSharpUseNameofCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task Parameter()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public Foo(object o)
    {
        throw new System.ArgumentNullException([|""o""|]);
    }
}",
@"public class Foo
{
    public Foo(object o)
    {
        throw new System.ArgumentNullException(nameof(o));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task Local()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public Foo()
    {
        object o = null;
        var text = Id([|""o""|]);
    }

    private static string Id(string value) => value;
}",
@"public class Foo
{
    public Foo()
    {
        object o = null;
        var text = Id(nameof(o));
    }

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstancePropertyInitializer()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public string Bar { get; } = Id([|""Bar""|]);

    private static string Id(string value) => value;
}",
@"public class Foo
{
    public string Bar { get; } = Id(nameof(Bar));

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstancePropertyExpressionBody()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public string Bar => Id([|""Bar""|]);

    private static string Id(string value) => value;
}",
@"public class Foo
{
    public string Bar => Id(nameof(this.Bar));

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task StaticPropertyInitializer()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public static string Bar { get; } = Id([|""Bar""|]);

    private static string Id(string value) => value;
}",
@"public class Foo
{
    public static string Bar { get; } = Id(nameof(Bar));

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task StaticPropertyExpressionBody()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public static string Bar => Id([|""Bar""|]);

    private static string Id(string value) => value;
}",
@"public class Foo
{
    public static string Bar => Id(nameof(Bar));

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task StaticMethodInstanceProperty()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public int Bar { get; set; }

    public static string Text() => Id([|""Bar""|]);

    private static string Id(string value) => value;
}",
@"public class Foo
{
    public int Bar { get; set; }

    public static string Text() => Id(nameof(Bar));

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstanceMethodStaticProperty()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public static int Bar { get; set; }

    public string Text() => Id([|""Bar""|]);

    private static string Id(string value) => value;
}",
@"public class Foo
{
    public static int Bar { get; set; }

    public string Text() => Id(nameof(Bar));

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstanceMethodInstanceProperty()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public int Bar { get; set; }

    public string Text() => Id([|""Bar""|]);

    private static string Id(string value) => value;
}",
@"public class Foo
{
    public int Bar { get; set; }

    public string Text() => Id(nameof(this.Bar));

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstanceFieldInitializer()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    private readonly string bar = Id([|""bar""|]);

    private static string Id(string value) => value;
}",
@"public class Foo
{
    private readonly string bar = Id(nameof(bar));

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstanceFieldInCtor()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    private readonly string bar;

    public Foo()
    {
        var text = Id([|""bar""|]);
    }

    private static string Id(string value) => value;
}",
@"public class Foo
{
    private readonly string bar;

    public Foo()
    {
        var text = Id(nameof(this.bar));
    }

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task IgnoreDebuggerDisplay()
        {
            await TestDiagnosticMissingAsync(
                @"
[System.Diagnostics.DebuggerDisplay([|""{Name}""|])]
public class Foo
{
    public string Name { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task IgnoreTypeName()
        {
            await TestMissingAsync(
                @"
public class Foo
{
    public void Bar()
    {
        var text = Id([|""Foo""|]);
    }

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task IgnoreLocalWhenNotInScope()
        {
            await TestMissingAsync(
                @"
public class Foo
{
    public Foo()
    {
        {
            var text = Id(""text"");
        }
        {
            var text = Id([|""text""|]);
        }
        {
            var text = Id(""text"");
        }
    }

    private static string Id(string value) => value;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task IgnoreNamespaceName()
        {
            await TestMissingAsync(
                @"
namespace Namespace
{
    public class Foo
    {
        public Foo()
        {
            var text = Id([|""Namespace""|]);
        }

        private static string Id(string value) => value;
    }
}");
        }
    }
}
