// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class AccessorDeclarationTests : AbstractOutlinerTests<AccessorDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(AccessorDeclarationSyntax accessorDeclaration)
        {
            var outliner = new AccessorDeclarationOutliner();
            return outliner.GetOutliningSpans(accessorDeclaration, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetter1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[0];

            var actualRegion = GetRegion(accessor);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(46, 60),
                TextSpan.FromBounds(43, 60),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetterWithSingleLineComments1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    // My",
                                        "    // Getter",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[0];

            var actualRegions = GetRegions(accessor).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(43, 63),
                "// My ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(72, 86),
                TextSpan.FromBounds(69, 86),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetterWithMultiLineComments1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    /* My",
                                        "       Getter */",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[0];

            var actualRegions = GetRegions(accessor).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(43, 66),
                "/* My ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(75, 89),
                TextSpan.FromBounds(72, 89),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetter2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[0];

            var actualRegion = GetRegion(accessor);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(46, 60),
                TextSpan.FromBounds(43, 60),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetterWithSingleLineComments2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    // My",
                                        "    // Getter",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[0];

            var actualRegions = GetRegions(accessor).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(43, 63),
                "// My ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(72, 86),
                TextSpan.FromBounds(69, 86),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetterWithMultiLineComments2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    /* My",
                                        "       Getter */",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[0];

            var actualRegions = GetRegions(accessor).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(43, 66),
                "/* My ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(75, 89),
                TextSpan.FromBounds(72, 89),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetter1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[0];

            var actualRegion = GetRegion(accessor);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(46, 60),
                TextSpan.FromBounds(43, 60),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetter2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[1];

            var actualRegion = GetRegion(accessor);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(69, 83),
                TextSpan.FromBounds(66, 83),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetterWithSingleLineComments1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    // My",
                                        "    // Setter",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[0];

            var actualRegions = GetRegions(accessor).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(43, 63),
                "// My ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(72, 86),
                TextSpan.FromBounds(69, 86),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetterWithMultiLineComments1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "    /* My",
                                        "       Setter */",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[1];

            var actualRegions = GetRegions(accessor).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(66, 89),
                "/* My ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(98, 112),
                TextSpan.FromBounds(95, 112),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetterWithSingleLineComments2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "    // My",
                                        "    // Setter",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[1];

            var actualRegions = GetRegions(accessor).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(66, 86),
                "// My ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(95, 109),
                TextSpan.FromBounds(92, 109),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetterWithMultiLineComments2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Text",
                                        "  {",
                                        "    get",
                                        "    {",
                                        "    }",
                                        "    /* My",
                                        "       Setter */",
                                        "    set",
                                        "    {",
                                        "    }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();
            var accessor = propDecl.AccessorList.Accessors[1];

            var actualRegions = GetRegions(accessor).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(66, 89),
                "/* My ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(98, 112),
                TextSpan.FromBounds(95, 112),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }
    }
}
