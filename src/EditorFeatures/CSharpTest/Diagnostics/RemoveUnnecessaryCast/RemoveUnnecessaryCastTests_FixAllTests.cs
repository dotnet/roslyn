// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveUnnecessaryCast
{
    public partial class RemoveUnnecessaryCastTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        #region "Fix all occurrences tests"

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = {|FixAllInDocument:(int)0|};
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;

        // cast removal that leads to parenthesis removal
        var prog = new Program();
        ((Program)prog).F();
    }
}
        </Document>
        <Document>
class Program2
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;

        // cast removal that leads to parenthesis removal
        var prog = new Program();
        ((Program)prog).F();
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;

        // cast removal that leads to parenthesis removal
        var prog = new Program();
        ((Program)prog).F();
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program
{
    private char f = 'c';
    public void F(int x = 0)
    {
        // unnecessary casts
        int y = 0;
        bool z = true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? s1 : (object)s2;

        // cast removal that leads to parenthesis removal
        var prog = new Program();
        prog.F();
    }
}
        </Document>
        <Document>
class Program2
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;

        // cast removal that leads to parenthesis removal
        var prog = new Program();
        ((Program)prog).F();
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;

        // cast removal that leads to parenthesis removal
        var prog = new Program();
        ((Program)prog).F();
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = {|FixAllInProject:(int)0|};
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;
    }
}
        </Document>
        <Document>
class Program2
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program
{
    private char f = 'c';
    public void F(int x = 0)
    {
        // unnecessary casts
        int y = 0;
        bool z = true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? s1 : (object)s2;
    }
}
        </Document>
        <Document>
class Program2
{
    private char f = 'c';
    public void F(int x = 0)
    {
        // unnecessary casts
        int y = 0;
        bool z = true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? s1 : (object)s2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = {|FixAllInSolution:(int)0|};
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;
    }
}
        </Document>
        <Document>
class Program2
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    private char f = (char)'c';
    public void F(int x = (int)0)
    {
        // unnecessary casts
        int y = (int)0;
        bool z = (bool)true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? (object)s1 : (object)s2;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program
{
    private char f = 'c';
    public void F(int x = 0)
    {
        // unnecessary casts
        int y = 0;
        bool z = true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? s1 : (object)s2;
    }
}
        </Document>
        <Document>
class Program2
{
    private char f = 'c';
    public void F(int x = 0)
    {
        // unnecessary casts
        int y = 0;
        bool z = true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? s1 : (object)s2;
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    private char f = 'c';
    public void F(int x = 0)
    {
        // unnecessary casts
        int y = 0;
        bool z = true;

        // required cast
        long l = 1;
        int y = (int)l;

        // required cast after cast removal in same statement
        string s1 = null, s2 = null;
        var s3 = z ? s1 : (object)s2;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null);
        }
        #endregion
    }
}
