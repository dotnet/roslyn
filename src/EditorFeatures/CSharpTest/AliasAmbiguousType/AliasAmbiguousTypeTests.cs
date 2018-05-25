// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AliasAmbiguousType;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AliasAmbiguousType
{
    public class AliasAmbiguousTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAliasAmbiguousTypeCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
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
            var expectedMarkup0 = classDef + @"
namespace Test
{
    using N1;
    using N2;
    using Ambiguous = N1.Ambiguous;

    class C
    {
        void M()
        {
            var a = new Ambiguous();
        }
    }
}";
            var expectedMarkup1 = classDef + @"
namespace Test
{
    using N1;
    using N2;
    using Ambiguous = N2.Ambiguous;

    class C
    {
        void M()
        {
            var a = new Ambiguous();
        }
    }
}";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup0, index: 0);
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup1, index: 1);
            await TestSmartTagTextAsync(initialMarkup, "using Ambiguous = N1.Ambiguous;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
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
using AmbiguousAttribute = N1.AmbiguousAttribute;
" + classDef + @"
namespace Test
{
    [Ambiguous]
    class C
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestNamespaceAndTypenameIdenticalOffersNoDiagnostics()
        {
            // This gives CS0433: The type 'Ambiguous' exists in both 'Assembly1' and 'Assembly2'
            // Couldn't get a CS0104 in this situation. Keep the test anyway if someone finds a way to force CS0104 here
            // or CS0433 is added as a supported diagnostic for this fix.
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousNestedClass()
        {
            var initialMarkup = @"
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
}";
            var expectedMarkup0 = @"
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
}";
            var expectedMarkup1 = @"
using static Static<string>;
using static Static<int>;
using Nested = Static<int>.Nested;

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
}";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup0, index: 0);
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup1, index: 1);
            await TestSmartTagTextAsync(initialMarkup, "using Nested = Static<string>.Nested;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousClassDiagnosedAtBaseList()
        {
            var classDef = GetAmbiguousDefinition(@"public class AmbiguousClass { }");
            var initialMarkup = @"
using N1;
using N2;
" + classDef + @" 
namespace NTest
{
    public class Test : [|AmbiguousClass|] { }
}
";
            var expectedMarkup = @"
using N1;
using N2;
using AmbiguousClass = N1.AmbiguousClass;
" + classDef + @" 
namespace NTest
{
    public class Test : AmbiguousClass { }
}
";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousClassDiagnosedAtTypeConstraint()
        {
            var classDef = GetAmbiguousDefinition(@"public class AmbiguousClass { }");
            var initialMarkup = @"
using N1;
using N2;
" + classDef + @" 
namespace NTest
{
    public class Test<T> where T : [|AmbiguousClass|] { }
}
";
            var expectedMarkup = @"
using N1;
using N2;
using AmbiguousClass = N1.AmbiguousClass;
" + classDef + @" 
namespace NTest
{
    public class Test<T> where T : AmbiguousClass { }
}
";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousEnumDiagnosedAtFieldDeclaration()
        {
            var enumDef = GetAmbiguousDefinition(@"public enum AmbiguousEnum { }");
            var initialMarkup = @"
using N1;
using N2;
" + enumDef + @" 
namespace NTest
{
    public class Test
    {
        private [|AmbiguousEnum|] _AmbiguousEnum;
    }
}
";
            var expectedMarkup = @"
using N1;
using N2;
using AmbiguousEnum = N1.AmbiguousEnum;
" + enumDef + @" 
namespace NTest
{
    public class Test
    {
        private AmbiguousEnum _AmbiguousEnum;
    }
}
";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousStructDiagnosedAtPropertyDeclaration()
        {
            var strcutDef = GetAmbiguousDefinition(@"public struct AmbiguousStruct { }");
            var initialMarkup = @"
using N1;
using N2;
" + strcutDef + @" 
namespace NTest
{
    public class Test
    {
        public [|AmbiguousStruct|] AmbiguousStruct { get; }
    }
}
";
            var expectedMarkup = @"
using N1;
using N2;
using AmbiguousStruct = N1.AmbiguousStruct;
" + strcutDef + @" 
namespace NTest
{
    public class Test
    {
        public AmbiguousStruct AmbiguousStruct { get; }
    }
}
";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousClassDiagnosedAtTypeArgument()
        {
            var classDef = GetAmbiguousDefinition(@"public class AmbiguousClass { }");
            var initialMarkup = @"
using N1;
using N2;
" + classDef + @" 
namespace NTest
{
    public class Test
    {
        public void M()
        {
            var list = new System.Collections.Generic.List<[|AmbiguousClass|]> { new AmbiguousClass() };
        } 
    }
}
";
            var expectedMarkup = @"
using N1;
using N2;
using AmbiguousClass = N1.AmbiguousClass;
" + classDef + @" 
namespace NTest
{
    public class Test
    {
        public void M()
        {
            var list = new System.Collections.Generic.List<AmbiguousClass> { new AmbiguousClass() };
        } 
    }
}
";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousClassDiagnosedAtIdentifierOfIncompleteExpression()
        {
            var classDef = GetAmbiguousDefinition(@"public class AmbiguousClass { }");
            var initialMarkup = @"
using N1;
using N2;
" + classDef + @" 
namespace NTest
{
    public class Test
    {
        public void M()
        {
            [|AmbiguousClass|]
        } 
    }
}
";
            var expectedMarkup = @"
using N1;
using N2;
using AmbiguousClass = N1.AmbiguousClass;
" + classDef + @" 
namespace NTest
{
    public class Test
    {
        public void M()
        {
            AmbiguousClass
        } 
    }
}
";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousClassDiagnosedAtMethodParameter()
        {
            var classDef = GetAmbiguousDefinition(@"public class AmbiguousClass { }");
            var initialMarkup = @"
using N1;
using N2;
" + classDef + @" 
namespace NTest
{
    public class Test
    {
        public void M([|AmbiguousClass|] a)
        {            
        } 
    }
}
";
            var expectedMarkup = @"
using N1;
using N2;
using AmbiguousClass = N1.AmbiguousClass;
" + classDef + @" 
namespace NTest
{
    public class Test
    {
        public void M(AmbiguousClass a)
        {            
        } 
    }
}
";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)]
        public async Task TestAmbiguousClassDiagnosedAtFromClauseTypeIdentifier()
        {
            var classDef = GetAmbiguousDefinition(@"public class AmbiguousClass { }");
            var initialMarkup = @"
using N1;
using N2;
using System.Linq;
" + classDef + @" 
namespace NTest
{
    public class Test
    {
        public void M()
        {            
            var qry = from [|AmbiguousClass|] a in new object[] { }
                      select a;
        } 
    }
}
";
            var expectedMarkup = @"
using N1;
using N2;
using System.Linq;
using AmbiguousClass = N1.AmbiguousClass;
" + classDef + @" 
namespace NTest
{
    public class Test
    {
        public void M()
        {            
            var qry = from AmbiguousClass a in new object[] { }
                      select a;
        } 
    }
}
";
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup);
        }
    }
}
