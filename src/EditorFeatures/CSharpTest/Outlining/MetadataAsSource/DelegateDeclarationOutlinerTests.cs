// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;
using MaSOutliners = Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    public class DelegateDeclarationOutlinerTests : AbstractOutlinerTests<DelegateDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<DelegateDeclarationSyntax> CreateOutliner()
        {
            return new MaSOutliners.DelegateDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            const string code = @"
public delegate TResult $$Blah<in T, out TResult>(T arg);";

            NoRegions(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            const string code = @"
{|hint:{|collapse:[Foo]
|}public delegate TResult $$Blah<in T, out TResult>(T arg);|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            const string code = @"
{|hint:{|collapse:// Summary:
//     This is a summary.
[Foo]
|}delegate TResult $$Blah<in T, out TResult>(T arg);|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAttributesAndModifiers()
        {
            const string code = @"
{|hint:{|collapse:// Summary:
//     This is a summary.
[Foo]
|}public delegate TResult $$Blah<in T, out TResult>(T arg);|}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
