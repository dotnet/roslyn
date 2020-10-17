﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeLens;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeLens
{
    public sealed class CSharpCodeLensTests : AbstractCodeLensTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestCount()
        {
            const string input = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public class A
{
    {|0: public void B()
    {
        C();
    }|}

    {|2: public void C()
    {
        D();
    }|}

    {|1: public void D()
    {
        C();
    }|}
}
]]>
        </Document>
    </Project>
</Workspace>";
            await RunCountTest(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestCapping()
        {
            const string input = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public class A
{
    {|0: public void B()
    {
        C();
    }|}

    {|capped1: public void C()
    {
        D();
    }|}

    {|1: public void D()
    {
        C();
    }|}
}
]]>
        </Document>
    </Project>
</Workspace>";

            await RunCountTest(input, 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestLinkedFiles()
        {
            const string input = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public class A
{
    {|0: public void B()
    {
        C();
    }|}

    {|3: public void C()
    {
        D();
    }|}

    {|3: public void D()
    {
        C();
    }|}
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
        <Document FilePath=""AdditionalDocument.cs""><![CDATA[
class E
{
    void F()
    {
        A.C();
        A.D();
        A.D();
    }
}
]]>
        </Document>
    </Project>
</Workspace>";

            await RunReferenceTest(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestDisplay()
        {
            const string input = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public class A
{
    {|0: public void B()
    {
        C();
    }|}

    {|2: public void C()
    {
        D();
    }|}

    {|1: public void D()
    {
        C();
    }|}
}
]]>
        </Document>
    </Project>
</Workspace>";

            await RunReferenceTest(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestMethodReferences()
        {
            const string input = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public class A
{
    {|0: public void B()
    {
        C();
    }|}

    {|2: public void C()
    {
        D();
    }|}

    {|1: public void D()
    {
        C();
    }|}
}
]]>
        </Document>
    </Project>
</Workspace>";
            await RunMethodReferenceTest(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestMethodReferencesWithDocstrings()
        {
            const string input = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public class A
{
    /// <summary>
    ///     <see cref=""A.C""/>
    /// </summary>
    {|0: public void B()
    {
        C();
    }|}

    {|2: public void C()
    {
        D();
    }|}

    {|1: public void D()
    {
        C();
    }|}
}
]]>
        </Document>
    </Project>
</Workspace>";
            await RunMethodReferenceTest(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestFullyQualifiedName()
        {
            const string input = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public class A
{
    {|A.C: public void C()
    {
        C();
    }|}

    public class B
    {
        {|A+B.C: public void C()
        {
            C();
        }|}

        public class D
        {
            {|A+B+D.C: public void C()
            {
                C();
            }|}
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>";
            await RunFullyQualifiedNameTest(input);
        }
    }
}
