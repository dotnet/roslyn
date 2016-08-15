// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces;
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
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(new CSharpAddBracesDiagnosticAnalyzer(),
                new CSharpAddBracesCodeFixProvider());
        }

        private IDictionary<OptionKey, object> AddBraces() =>
            Options(CSharpCodeStyleOptions.UseBracesWherePossible, new CodeStyleOption<bool>(true, NotificationOption.None));

        private IDictionary<OptionKey, object> Options(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>();
            options.Add(option, value);
            return options;
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
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
", options: AddBraces());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForFixedWithBraces()
        {
            await TestMissingAsync(
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
", options: AddBraces());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task DoNotFireForFixedWithChildFixed()
        {
            await TestMissingAsync(
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
", options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
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
   compareTokens: false,
            options: AddBraces());
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
            compareTokens: false,
            options: AddBraces());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForFixedWithoutBraces()
        {
            await TestAsync(
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
}",

            index: 0,
            compareTokens: false,
            options: AddBraces());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        public async Task FireForFixedWithoutBracesNestedInLock()
        {
            await TestAsync(
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
}",

            index: 0,
            compareTokens: false,
            options: AddBraces());
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

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null, options: AddBraces());
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

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null, options: AddBraces());
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

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null, options: AddBraces());
        }
    }
}