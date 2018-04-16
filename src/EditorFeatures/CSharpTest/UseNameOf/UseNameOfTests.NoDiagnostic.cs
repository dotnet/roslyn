using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNameOf
{
    public partial class UseNameOfTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNameOf)]
        public async Task IgnoreDebuggerDisplay()
        {
            await TestMissingAsync(
                @"
[System.Diagnostics.DebuggerDisplay(""{Name}"")]
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
        var text = Id(""Foo"");
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
            var text = Id(""text"");
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
            var text = Id(""Namespace"");
        }

        private static string Id(string value) => value;
    }
}");
        }
    }
}
