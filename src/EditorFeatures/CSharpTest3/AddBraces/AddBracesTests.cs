using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddBraces
{
    public partial class AddBracesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(new CSharpAddBracesDiagnosticAnalyzer(),
                new CSharpAddBracesCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForIfWithBraces()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        [|if|] (true) { return; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForElseWithBraces()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        if (true) { return; }
        [|else|] { return; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForElseWithChildIf()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        if (true) return;
        [|else|] if (false) return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForForWithBraces()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        [|for|] (var i = 0; i < 5; i++) { return; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForForEachWithBraces()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        [|foreach|] (var c in ""test"") { return; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForWhileWithBraces()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        [|while|] (true) { return; }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForDoWhileWithBraces()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        [|do|] { return; } while (true);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForUsingWithBraces()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        [|using|] (var f = new Fizz())
        {
            return;
        }
    }
}

class Fizz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForUsingWithChildUsing()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        [|using|] (var f = new Fizz())
        using (var b = new Buzz())
            return;
    }
}

class Fizz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

class Buzz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForLockWithBraces()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        var str = ""test"";
        [|lock|] (str)
        {
            return;
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForLockWithChildLock()
        {
            await TestMissingAsync(
            @"
class Program
{
    static void Main()
    {
        var str1 = ""test"";
        var str2 = ""test"";

        [|lock|] (str1)
        lock (str2)
            return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForIfWithoutBraces()
        {
            await TestAsync(
   @"
class Program
{
    static void Main()
    {
        [|if|] (true) return;
    }
}",

   @"
class Program
{
    static void Main()
    {
        if (true)
        {
            return;
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForElseWithoutBraces()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        if (true) { return; }
        [|else|] return;
    }
}",

   @"
class Program
{
    static void Main()
    {
        if (true) { return; }
        else
        {
            return;
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForIfNestedInElseWithoutBraces()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        if (true) return;
        else [|if|] (false) return;
    }
}",

   @"
class Program
{
    static void Main()
    {
        if (true) return;
        else if (false)
        {
            return;
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForForWithoutBraces()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        [|for|] (var i = 0; i < 5; i++) return;
    }
}",

   @"
class Program
{
    static void Main()
    {
        for (var i = 0; i < 5; i++)
        {
            return;
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForForEachWithoutBraces()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        [|foreach|] (var c in ""test"") return;
    }
}",

   @"
class Program
{
    static void Main()
    {
        foreach (var c in ""test"")
        {
            return;
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForWhileWithoutBraces()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        [|while|] (true) return;
    }
}",

   @"
class Program
{
    static void Main()
    {
        while (true)
        {
            return;
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForDoWhileWithoutBraces()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        [|do|] return; while (true);
    }
}",

   @"
class Program
{
    static void Main()
    {
        do
        {
            return;
        }
        while (true);
    }
}",
            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForUsingWithoutBraces()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        [|using|] (var f = new Fizz())
            return;
    }
}

class Fizz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}",

   @"
class Program
{
    static void Main()
    {
        using (var f = new Fizz())
        {
            return;
        }
    }
}

class Fizz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}",

   index: 0,
   compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForUsingWithoutBracesNestedInUsing()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        using (var f = new Fizz())
        [|using|] (var b = new Buzz())
            return;
    }
}

class Fizz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

class Buzz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}",

   @"
class Program
{
    static void Main()
    {
        using (var f = new Fizz())
        using (var b = new Buzz())
        {
            return;
        }
    }
}

class Fizz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

class Buzz : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}",

            index: 0,
            compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForLockWithoutBraces()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        var str = ""test"";
        [|lock|] (str)
            return;
    }
}",

   @"
class Program
{
    static void Main()
    {
        var str = ""test"";
        lock (str)
        {
            return;
        }
    }
}",

   index: 0,
   compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForLockWithoutBracesNestedInLock()
        {
            await TestAsync(
            @"
class Program
{
    static void Main()
    {
        var str1 = ""test"";
        var str2 = ""test"";

        lock (str1)
        [|lock|] (str2) // VS thinks this should be indented one more level
            return;
    }
}",

   @"
class Program
{
    static void Main()
    {
        var str1 = ""test"";
        var str2 = ""test"";

        lock (str1)
        lock (str2) // VS thinks this should be indented one more level
            {
                return;
            }
    }
}",

            index: 0,
            compareTokens: false);
        }
    }
}