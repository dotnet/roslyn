// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryCast
{
    public class RemoveUnnecessaryCastTests_FixAllTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public RemoveUnnecessaryCastTests_FixAllTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnnecessaryCastDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryCastCodeFixProvider());

        #region "Fix all occurrences tests"

        [Fact]
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

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
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

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
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

            await TestInRegularAndScriptAsync(input, expected);
        }
        #endregion
    }
}
