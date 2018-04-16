using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseNameOf;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNameOf
{
    public partial class UseNameOfTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
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
        var text = [|""o""|];
    }
}",
@"public class Foo
{
    public Foo()
    {
        object o = null;
        var text = nameof(o);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstancePropertyInitializer()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public string Bar { get; } = [|""Bar""|];
}",
@"public class Foo
{
    public string Bar { get; } = nameof(Bar);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstancePropertyExpressionBody()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public string Bar => [|""Bar""|];
}",
@"public class Foo
{
    public string Bar => nameof(this.Bar);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task StaticPropertyInitializer()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public static string Bar { get; } = [|""Bar""|];
}",
@"public class Foo
{
    public static string Bar { get; } = nameof(Bar);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task StaticPropertyExpressionBody()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public static string Bar => [|""Bar""|];
}",
@"public class Foo
{
    public static string Bar => nameof(Bar);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task StaticMethodInstanceProperty()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public int Bar { get; set; }

    public static string Text() => [|""Bar""|];
}",
@"public class Foo
{
    public int Bar { get; set; }

    public string Text() => nameof(Bar);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstanceMethodStaticProperty()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public static int Bar { get; set; }

    public string Text() => [|""Bar""|];
}",
@"public class Foo
{
    public static int Bar { get; set; }

    public string Text() => nameof(Bar);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstanceMethodInstanceProperty()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public int Bar { get; set; }

    public string Text() => [|""Bar""|];
}",
@"public class Foo
{
    public int Bar { get; set; }

    public string Text() => nameof(this.Bar);
}");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task InstanceFieldInitializer()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    private readonly string bar = [|""bar""|];
}",
@"public class Foo
{
    private readonly string bar = nameof(bar);
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
        var text = [|""bar""|];
    }
}",
@"public class Foo
{
    private readonly string bar;

    public Foo()
    {
        var text = nameof(this.bar);
    }
}");
        }
    }
}
