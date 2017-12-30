// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AmbiguityCodeFixProvider;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Ambiguity
{
    public class AmbiguousTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAmbiguousTypeCodeFixProvider());

        private string GetAmbiguousDefinition(string typeDefinion)
            => $@"
namespace N1
{{
    { typeDefinion }
}}
namespace N2
{{
    { typeDefinion }
}}";

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasType)]
        public async Task TestAmbiguousClassObjectCreationUsingsInNamespace()
        {
            var classDef = GetAmbiguousDefinition("public class Ambiguous { }");
            var initialMarkup = classDef + @"
namespace Test
{
    using N1;
    using N2;
    class C
    {
        void M()
        {
            var a = new [|Ambiguous|]();
        }
    }
}";
            var expectedMarkupTemplate = classDef + @"
namespace Test
{
    using N1;
    using N2;
    #

    class C
    {
        void M()
        {
            var a = new Ambiguous();
        }
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkupTemplate.Replace("#", "using Ambiguous = N1.Ambiguous;"), 0);
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkupTemplate.Replace("#", "using Ambiguous = N2.Ambiguous;"), 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasType)]
        public async Task TestAmbiguousClassObjectCreationUsingsInCompilationUnit()
        {
            var classDef = GetAmbiguousDefinition("public class Ambiguous { }");
            await TestInRegularAndScriptAsync(@"
using N1;
using N2;
" + classDef + @"
namespace Test
{
    class C
    {
        void M()
        {
            var a = new [|Ambiguous|]();
        }
    }
}", @"
using N1;
using N2;
using Ambiguous = N1.Ambiguous;
" + classDef + @"
namespace Test
{
    class C
    {
        void M()
        {
            var a = new Ambiguous();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasType)]
        public async Task TestAmbiguousClassObjectCreationGenericsDontOfferDiagnostic()
        {
            var genericAmbiguousClassDefinition = GetAmbiguousDefinition("public class Ambiguous<T> { }");
            await TestMissingAsync(@"
using N1;
using N2;
" + genericAmbiguousClassDefinition + @"
namespace Test
{
    class C
    {
        void M()
        {
            var a = new [|Ambiguous<int>|]();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasType)]
        public async Task TestAmbiguousAttribute()
        {
            var classDef = GetAmbiguousDefinition("public class AmbiguousAttribute: System.Attribute { }");
            await TestInRegularAndScriptAsync(@"
using N1;
using N2;
" + classDef + @"
namespace Test
{
    [[|Ambiguous|]]
    class C
    {
    }
}", @"
using N1;
using N2;
using Ambiguous = N1.AmbiguousAttribute;
" + classDef + @"
namespace Test
{
    [Ambiguous]
    class C
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasType)]
        public async Task TestNamespaceAndTypenameIdenticalOffersNoDiagnostics()
        {
            // This gives CS0433: The type 'Ambiguous' exists in both 'Assembly1' and 'Assembly2'
            // Couldn't get a CS0104 in this situation. Keep the test anyway if someone finds a way to force CS0104 here.
            await TestMissingAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""File1.cs"">
namespace N
{
    public class Ambiguous { }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document FilePath=""File2.cs"">
namespace N
{
    public class Ambiguous { }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Test"" CommonReferences=""true"">
        <ProjectReference Alias=""A1"">Assembly1</ProjectReference>
        <ProjectReference Alias=""A2"">Assembly2</ProjectReference>
        <Document FilePath=""File3.cs"">
extern alias A1;
extern alias A2;
using A1::N;
using A2::N;
namespace N1 
{
    public class C 
    {
        void M() 
        {
            var a = new [|Ambiguous|]();
        }
    }
}
        </Document>
    </Project>
</Workspace>
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasType)]
        public async Task TestAmbiguousAliasNoDiagnostics()
        {
            await TestMissingAsync(@"
extern alias alias;
using alias=alias;
class myClass : [|alias|]::Uri
    {
    }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasType + "1")]
        public async Task TestAmbiguousNestedClass()
        {
            await TestInRegularAndScriptAsync(@"
using static Static<string>;
using static Static<int>;
 
public static class Static<T>
{
    public class Nested
    {
        public void M() { }
    }
}
 
class D
{
    static void Main(string[] args)
    {
        var c = new [|Nested|]();
        c.M();
    }
}", @"
using static Static<string>;
using static Static<int>;
using Nested = Static<string>.Nested;

public static class Static<T>
{
    public class Nested
    {
        public void M() { }
    }
}
 
class D
{
    static void Main(string[] args)
    {
        var c = new Nested();
        c.M();
    }
}");
        }
    }
}
