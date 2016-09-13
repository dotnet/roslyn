// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.Braces;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddBraces
{
    public class AddBracesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(new CSharpBracesDiagnosticAnalyzer(),
                new CSharpAddBracesCodeFixProvider());
        }

        private IDictionary<OptionKey, object> AddBraces =>
            new Dictionary<OptionKey, object> { { CSharpCodeStyleOptions.AlwaysUseBraces, new CodeStyleOption<bool>(true, NotificationOption.Warning) } };

        private async Task TestAddBraces(string originalMarkup)
        {
            await TestMissingAsync(originalMarkup, options: AddBraces);
        }

        private async Task TestAddBraces(string originalMarkup, string expectedMarkup)
        {
            await TestAsync(originalMarkup, expectedMarkup, options: AddBraces);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForIfWithBraces()
        {
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
            await TestAddBraces(
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
        public async Task DoNotFireForFixedWithBraces()
        {
            await TestAddBraces(
            @"
class Point 
{ 
    public int x;
    public int y; 
}

class Program
{
    unsafe static void TestMethod()
    {
        var pt = new Point();
        [|fixed|] (int* p = &pt.x)
        {
            *p = 1;
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForFixedWithChildFixed()
        {
            await TestAddBraces(
            @"
class Point 
{ 
    public int x;
    public int y; 
}

class Program
{
    unsafe static void TestMethod()
    {
        var pt = new Point();
        [|fixed|] (int* p = &pt.x)
        fixed (int* q = &pt.y)
        {
            *p = 1;
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForIfWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForElseWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForIfNestedInElseWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForForWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForForEachWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForWhileWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForDoWhileWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForUsingWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForUsingWithoutBracesNestedInUsing()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForLockWithoutBraces()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForLockWithoutBracesNestedInLock()
        {
            await TestAddBraces(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForFixedWithoutBraces()
        {
            await TestAddBraces(
            @"
class Point 
{ 
    public int x;
    public int y; 
}

class Program
{
    unsafe static void TestMethod()
    {
        var pt = new Point();
        [|fixed|] (int* p = &pt.x)
            *p = 1;
    }
}",

   @"
class Point 
{ 
    public int x;
    public int y; 
}

class Program
{
    unsafe static void TestMethod()
    {
        var pt = new Point();
        fixed (int* p = &pt.x)
        {
            *p = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForFixedWithoutBracesNestedInLock()
        {
            await TestAddBraces(
            @"
class Point 
{ 
    public int x;
    public int y; 
}

class Program
{
    unsafe static void TestMethod()
    {
        var pt = new Point();
        fixed (int* p = &pt.x)
        [|fixed|] (int* q = &pt.y)
            *p = 1;
    }
}",

   @"
class Point 
{ 
    public int x;
    public int y; 
}

class Program
{
    unsafe static void TestMethod()
    {
        var pt = new Point();
        fixed (int* p = &pt.x)
        fixed (int* q = &pt.y)
        {
            *p = 1;
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    static void Main()
    {
        {|FixAllInDocument:if|} (true) return;
        if (true) return;
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    static void Main()
    {
        if (true)
        {
            return;
        }

        if (true)
        {
            return;
        }
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAddBraces(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    static void Main()
    {
        {|FixAllInProject:if|} (true) return;
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    static void Main()
    {
        if (true)
        {
            return;
        }
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true)
        {
            return;
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAddBraces(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    static void Main()
    {
        {|FixAllInSolution:if|} (true) return;
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    static void Main()
    {
        if (true) return;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    static void Main()
    {
        if (true)
        {
            return;
        }
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true)
        {
            return;
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    static void Main()
    {
        if (true)
        {
            return;
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAddBraces(input, expected);
        }
    }
}