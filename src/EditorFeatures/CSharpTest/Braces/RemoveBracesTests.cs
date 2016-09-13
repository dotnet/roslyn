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
    public class RemoveBracesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(new CSharpBracesDiagnosticAnalyzer(),
                new CSharpRemoveBracesCodeFixProvider());
        }

        private IDictionary<OptionKey, object> RemoveBraces =>
            new Dictionary<OptionKey, object> { { CSharpCodeStyleOptions.AlwaysUseBraces, new CodeStyleOption<bool>(false, NotificationOption.Warning) } };

        private async Task TestRemoveBraces(string originalMarkup)
        {
            await TestMissingAsync(originalMarkup, options: RemoveBraces);
        }

        private async Task TestRemoveBraces(string originalMarkup, string expectedMarkup)
        {
            await TestAsync(originalMarkup, expectedMarkup, options: RemoveBraces);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForIfWithoutBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|if|] (true) return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForElseWithoutBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        if (true) return;
        [|else|] return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForElseWithChildIf()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        if (true) return;
        [|else|] if (false) return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForForWithoutBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|for|] (var i = 0; i < 5; i++) return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForForEachWithoutBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|foreach|] (var c in ""test"") return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForWhileWithoutBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|while|] (true) return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForDoWhileWithoutBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|do|] return; while (true);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForUsingWithoutBraces()
        {
            await TestRemoveBraces(
@"class Program
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
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForUsingWithChildUsing()
        {
            await TestRemoveBraces(
@"class Program
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForLockWithoutBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        var str = ""test"";
        [|lock|] (str)
            return;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForLockWithChildLock()
        {
            await TestRemoveBraces(
@"class Program
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForFixedoutWithBraces()
        {
            await TestRemoveBraces(
@"class Point 
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
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task DoNotFireForFixedWithChildFixed()
        {
            await TestRemoveBraces(
@"class Point 
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
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForIfWithBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|if|] (true) { return; }
    }
}",

@"class Program
{
    static void Main()
    {
        if (true) return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForElseWithBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        if (true) { return; }
        [|else|] { return; }
    }
}",

@"class Program
{
    static void Main()
    {
        if (true) { return; }
        else return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForIfNestedInElseWithBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        if (true) return;
        else [|if|] (false) { return; }
    }
}",

@"class Program
{
    static void Main()
    {
        if (true) return;
        else if (false) return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForForWithBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|for|] (var i = 0; i < 5; i++)
        {
            return;
        }
    }
}",

@"class Program
{
    static void Main()
    {
        for (var i = 0; i < 5; i++)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForForEachWithBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|foreach|] (var c in ""test"") { return; }
    }
}",

@"class Program
{
    static void Main()
    {
        foreach (var c in ""test"") return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForWhileWithBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|while|] (true) { return; }
    }
}",

@"class Program
{
    static void Main()
    {
        while (true) return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForDoWhileWithBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        [|do|]
        {
            return;
        } while (true);
    }
}",

@"class Program
{
    static void Main()
    {
        do
            return;
        while (true);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForUsingWithBraces()
        {
            await TestRemoveBraces(
@"class Program
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
}",

@"class Program
{
    static void Main()
    {
        using (var f = new Fizz())
            return;
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForUsingWithBracesNestedInUsing()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        using (var f = new Fizz())
        [|using|] (var b = new Buzz())
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

@"class Program
{
    static void Main()
    {
        using (var f = new Fizz())
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForLockWithBraces()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        var str = ""test"";
        [|lock|] (str)
        {
            return;
        }
    }
}",

@"class Program
{
    static void Main()
    {
        var str = ""test"";
        lock (str)
            return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForLockWithBracesNestedInLock()
        {
            await TestRemoveBraces(
@"class Program
{
    static void Main()
    {
        var str1 = ""test"";
        var str2 = ""test"";

        lock (str1)
            [|lock|] (str2)
            {
                return;
            }
    }
}",

@"class Program
{
    static void Main()
    {
        var str1 = ""test"";
        var str2 = ""test"";

        lock (str1)
            lock (str2)
                return;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForFixedWithBraces()
        {
            await TestRemoveBraces(
@"class Point 
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
}",

@"class Point 
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
            *p = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
        public async Task FireForFixedWithoutBracesNestedInLock()
        {
            await TestRemoveBraces(
@"class Point 
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
        {
            *p = 1;
        }
    }
}",

@"class Point 
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
            *p = 1;
    }
}");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
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
        {|FixAllInDocument:if|} (true) { return; }

        if (true) { return; }
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true) { return; }
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
        if (true) { return; }
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
        if (true) return;

        if (true) return;
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true) { return; }
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
        if (true) { return; }
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestRemoveBraces(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
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
        {|FixAllInProject:if|} (true) { return; }
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true) { return; }
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
        if (true) { return; }
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
        if (true) { return; }
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestRemoveBraces(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveBraces)]
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
        {|FixAllInSolution:if|} (true) { return; }
    }
}
        </Document>
        <Document>
class Program2
{
    static void Main()
    {
        if (true) { return; }
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
        if (true) { return; }
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

            await TestRemoveBraces(input, expected);
        }
    }
}
