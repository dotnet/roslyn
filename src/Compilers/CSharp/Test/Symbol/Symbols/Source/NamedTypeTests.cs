// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NamedTypeTests : CSharpTestBase
    {
        public static readonly object[][] TestData =
        {
            new [] { "struct C { }" },
            new [] { "enum C { }" },
            new [] { "interface C { }" },
            new [] { "delegate void C();" },
        };

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType1(string type)
        {
            var compilation = CreateCompilation($"[System.CLSCompliant(false)] {type}");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.False(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType2(string type)
        {
            var compilation = CreateCompilation($"[System.Runtime.InteropServices.TypeIdentifierAttribute] {type}");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType3(string type)
        {
            var compilation = CreateCompilation($"[System.Runtime.InteropServices.TypeIdentifier] {type}");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType4(string type)
        {
            var compilation = CreateCompilation(@$"
using System.Runtime.InteropServices;

[TypeIdentifierAttribute] {type}");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType5(string type)
        {
            var compilation = CreateCompilation(@$"
using System.Runtime.InteropServices;

[TypeIdentifier] {type}");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType6(string type)
        {
            var compilation = CreateCompilation(@$"
using TI = System.Runtime.InteropServices.TypeIdentifierAttribute;

[TI] {type}");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType7(string type)
        {
            var compilation = CreateCompilation(@$"
using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;

[TIAttribute] {type}");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType8(string type)
        {
            var compilation = CreateCompilation(@$"
using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;

[TI] {type}");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType9(string type)
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TI = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @$"
[TI] {type}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType10(string type)
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @$"
[TIAttribute] {type}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType11(string type)
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @$"
[TI] {type}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType12(string type)
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @$"
namespace N
{{
    using X = TIAttribute;
    [X] {type}
}}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("N").GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType13(string type)
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @$"
namespace N
{{
    using XAttribute = TIAttribute;
    [XAttribute] {type}
}}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("N").GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Theory, MemberData(nameof(TestData))]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType14(string type)
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @$"
namespace N
{{
    using XAttribute = TIAttribute;
    [X] {type}
}}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("N").GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }
    }
}
