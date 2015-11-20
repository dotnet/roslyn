// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class CompilationUnitOutlinerTests : AbstractOutlinerTests<CompilationUnitSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<CompilationUnitSyntax> CreateOutliner()
        {
            return new CompilationUnitOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestUsings()
        {
            const string code = @"
$${|hint:using {|collapse:System;
using System.Core;|}|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestUsingAliases()
        {
            const string code = @"
$${|hint:using {|collapse:System;
using System.Core;
using text = System.Text;
using linq = System.Linq;|}|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestExternAliases()
        {
            const string code = @"
$${|hint:extern {|collapse:alias Foo;
extern alias Bar;|}|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestExternAliasesAndUsings()
        {
            const string code = @"
$${|hint:extern {|collapse:alias Foo;
extern alias Bar;
using System;
using System.Core;|}|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestExternAliasesAndUsingsWithLeadingTrailingAndNestedComments()
        {
            const string code = @"
$${|span1:// Foo
// Bar|}
{|hint2:extern {|collapse2:alias Foo;
extern alias Bar;
// Foo
// Bar
using System;
using System.Core;|}|}
{|span3:// Foo
// Bar|}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true),
                Region("span3", "// Foo ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestUsingsWithComments()
        {
            const string code = @"
$${|span1:// Foo
// Bar|}
{|hint2:using {|collapse2:System;
using System.Core;|}|}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestExternAliasesWithComments()
        {
            const string code = @"
$${|span1:// Foo
// Bar|}
{|hint2:extern {|collapse2:alias Foo;
extern alias Bar;|}|}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestWithComments()
        {
            const string code = @"
$${|span1:// Foo
// Bar|}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestWithCommentsAtEnd()
        {
            const string code = @"
$${|hint1:using {|collapse1:System;|}|}
{|span2:// Foo
// Bar|}";

            Regions(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: true),
                Region("span2", "// Foo ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(539359)]
        public void TestUsingKeywordWithSpace()
        {
            const string code = @"
$${|hint:using|} {|collapse:|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
