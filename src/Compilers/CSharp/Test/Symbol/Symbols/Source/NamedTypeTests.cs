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
        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType1()
        {
            var compilation = CreateCompilation("[System.CLSCompliant(false)] struct C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.False(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType2_struct()
        {
            var compilation = CreateCompilation("[System.Runtime.InteropServices.TypeIdentifierAttribute] struct C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType2_enum()
        {
            var compilation = CreateCompilation("[System.Runtime.InteropServices.TypeIdentifierAttribute] enum C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType2_interface()
        {
            var compilation = CreateCompilation("[System.Runtime.InteropServices.TypeIdentifierAttribute] interface C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType2_delegate()
        {
            var compilation = CreateCompilation("[System.Runtime.InteropServices.TypeIdentifierAttribute] delegate void C();");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType3()
        {
            var compilation = CreateCompilation("[System.Runtime.InteropServices.TypeIdentifier] struct C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType4()
        {
            var compilation = CreateCompilation(@"
using System.Runtime.InteropServices;

[TypeIdentifierAttribute] struct C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType5()
        {
            var compilation = CreateCompilation(@"
using System.Runtime.InteropServices;

[TypeIdentifier] struct C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType6()
        {
            var compilation = CreateCompilation(@"
using TI = System.Runtime.InteropServices.TypeIdentifierAttribute;

[TI] struct C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType7()
        {
            var compilation = CreateCompilation(@"
using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;

[TIAttribute] struct C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType8()
        {
            var compilation = CreateCompilation(@"
using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;

[TI] struct C { }");
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType9()
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TI = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @"
[TI] struct C { }"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType10()
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @"
[TIAttribute] struct C { }"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType11()
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @"
[TI] struct C { }"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType12()
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @"
namespace N
{
    using X = TIAttribute;
    [X] struct C { }
}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("N").GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType13()
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @"
namespace N
{
    using XAttribute = TIAttribute;
    [XAttribute] struct C { }
}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("N").GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }

        [Fact]
        [WorkItem(1393763, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393763")]
        public void IsExplicitDefinitionOfNoPiaLocalType14()
        {
            var compilation = CreateCompilation(new[]
            {
                @"global using TIAttribute = System.Runtime.InteropServices.TypeIdentifierAttribute;",
                @"
namespace N
{
    using XAttribute = TIAttribute;
    [X] struct C { }
}"
});
            var namedType = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("N").GetMember<NamedTypeSymbol>("C");
            Assert.True(namedType.IsExplicitDefinitionOfNoPiaLocalType);
        }
    }
}
