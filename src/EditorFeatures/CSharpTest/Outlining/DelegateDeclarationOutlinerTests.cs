// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class DelegateDeclarationOutlinerTests : AbstractOutlinerTests<DelegateDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<DelegateDeclarationSyntax> CreateOutliner()
        {
            return new DelegateDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDelegateWithComments()
        {
            const string code = @"
{|span:// Foo
// Bar|}
$$public delegate void C();";

            Regions(code,
                Region("span", "// Foo ...", autoCollapse: true));
        }
    }
}
