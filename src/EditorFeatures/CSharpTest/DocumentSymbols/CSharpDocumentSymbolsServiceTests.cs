// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.DocumentSymbols;
using Microsoft.CodeAnalysis.DocumentSymbols;
using Microsoft.CodeAnalysis.Editor.UnitTests.DocumentSymbols;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentSymbols
{
    public class CSharpDocumentSymbolsServiceTests : AbstractDocumentSymbolsServiceTests<CSharpTestWorkspaceFixture>
    {
        protected override Type GetDocumentSymbolsServicePartType()
        {
            return typeof(CSharpDocumentSymbolsService);
        }

        protected override IDocumentSymbolsService GetDocumentSymbolsService(Document document1)
        {
            return Assert.IsType<CSharpDocumentSymbolsService>(base.GetDocumentSymbolsService(document1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Classes()
        {
            await AssertExpectedContent(
@"class C1
{
    class C2
    {
    }
}
class C4 // Intentionally out of order to demonstrate sorting
{
}
class C3
{
}",
expectedHierarchicalLayout: @"
C1
  C1.C2
C4
C3",
expectedNonHierarchicalLayout: @"
C1
C1.C2
C3
C4");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__NestedMethods()
        {
            await AssertExpectedContent(@"
class C1
{
    void M1()
    {
        void Local()
        {
        }
    }

    class C2
    {
        void M2()
        {
        }
    }
}",
expectedHierarchicalLayout: @"
C1
  void C1.M1()
    void Local()
  C1.C2
    void C1.C2.M2()",
expectedNonHierarchicalLayout: @"
C1
  void C1.M1()
C1.C2
  void C1.C2.M2()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Locals()
        {

            await AssertExpectedContent(@"
int i1;
class C1
{
    void M1()
    {
        int i2;

        void LocalFunc()
        {
            int i3;
        }
    }
}",
expectedHierarchicalLayout: @"
System.Int32 i1
C1
  void C1.M1()
    System.Int32 i2
    void LocalFunc()
    System.Int32 i3
",
expectedNonHierarchicalLayout: @"
C1
  void C1.M1()
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Fields()
        {
            await AssertExpectedContent(@"
class C1
{
    private int i1;
    public int I2;
    class C2
    {
        private int i3;
    }
}",
expectedHierarchicalLayout: @"
C1
  System.Int32 C1.i1
  System.Int32 C1.I2
  C1.C2
    System.Int32 C1.C2.i3
",
expectedNonHierarchicalLayout: @"
C1
  System.Int32 C1.i1
  System.Int32 C1.I2
C1.C2
  System.Int32 C1.C2.i3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Properties()
        {
            await AssertExpectedContent(@"
class C1
{
    private int I1 { get; set; }
    public int I2 => 1;
    class C2
    {
        private int I3 { get => 1; set {} }
    }
}",
expectedHierarchicalLayout: @"
C1
  System.Int32 C1.I1 { get; set; }
  System.Int32 C1.I2 { get; }
  C1.C2
    System.Int32 C1.C2.I3 { get; set; }
",
expectedNonHierarchicalLayout: @"
C1
  System.Int32 C1.I1 { get; set; }
  System.Int32 C1.I2 { get; }
C1.C2
  System.Int32 C1.C2.I3 { get; set; }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Events()
        {
            await AssertExpectedContent(@"
using System;
class C1
{
    event Action<string> Event;
}",
expectedHierarchicalLayout: @"
C1
  event System.Action<System.String> C1.Event
",
expectedNonHierarchicalLayout: @"
C1
  event System.Action<System.String> C1.Event
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Constants()
        {
            await AssertExpectedContent(@"
const int i1 = 1;
class C1
{
    const int i2 = 2;
    void M()
    {
        const int i3 = 3;
    }
}",
expectedHierarchicalLayout: @"
System.Int32 i1
C1
  System.Int32 C1.i2
  void C1.M()
    System.Int32 i3
",
expectedNonHierarchicalLayout: @"
C1
  System.Int32 C1.i2
  void C1.M()
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__OutVarDeclarations()
        {
            await AssertExpectedContent(@"
class C1
{
    void M()
    {
        M(out var i1);
    }
    void M1(out int i) => i = 0;
}",
expectedHierarchicalLayout: @"
C1
  void C1.M()
    var i1
  void C1.M1(out System.Int32 i)
",
expectedNonHierarchicalLayout: @"
C1
  void C1.M()
  void C1.M1(out System.Int32 i)
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__PatternDeclarations()
        {
            await AssertExpectedContent(@"
class C1
{
    void M(object o)
    {
        _ = o is int i1;
        switch (o)
        {
            case int i2: break;
        }
        _ = o switch
        {
            int i3 => new object(),
        };
    }
}",
expectedHierarchicalLayout: @"
C1
  void C1.M(System.Object o)
    System.Int32 i1
    System.Int32 i2
    System.Int32 i3
",
expectedNonHierarchicalLayout: @"
C1
  void C1.M(System.Object o)
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__NamespaceSymbolsNotIncluded()
        {
            await AssertExpectedContent(@"
namespace N1
{
    class C1 {}
}",
expectedHierarchicalLayout: @"
N1.C1
",
expectedNonHierarchicalLayout: @"
N1.C1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Constructors()
        {
            await AssertExpectedContent(@"
class C1
{
    C1(int i1) {}
    C1() {}
    C1(object o1, object o2) {}
}
",
expectedHierarchicalLayout: @"
C1
  C1..ctor(System.Int32 i1)
  C1..ctor()
  C1..ctor(System.Object o1, System.Object o2)
",
expectedNonHierarchicalLayout: @"
C1
  C1..ctor()
  C1..ctor(System.Int32 i1)
  C1..ctor(System.Object o1, System.Object o2)
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Partials()
        {
            await AssertExpectedContent(@"
partial class C1
{
    partial void M1();
}
partial class C1
{
    partial void M1() {}
}
",
expectedHierarchicalLayout: @"
C1
  void C1.M1()
C1
  void C1.M1()
",
expectedNonHierarchicalLayout: @"
C1
  void C1.M1()
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__TypeParameters()
        {
            await AssertExpectedContent(@"
class C1<T1, T2>
{
    void M1<T3, T4>() {}
}
",
expectedHierarchicalLayout: @"
C1<T1, T2>
  T1
  T2
  void C1<T1, T2>.M1<T3, T4>()
    T3
    T4
",
expectedNonHierarchicalLayout: @"
C1<T1, T2>
  void C1<T1, T2>.M1<T3, T4>()
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__NothingReturnedInsideLambdas()
        {
            await AssertExpectedContent(@"
using System;
class C1
{
    void M1()
    {
        Action a = () => {
            int i = 1;
            void M() {}
        };
    }
}
",
expectedHierarchicalLayout: @"
C1
  void C1.M1()
    System.Action a
",
expectedNonHierarchicalLayout: @"
C1
  void C1.M1()
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Delegates()
        {
            await AssertExpectedContent(@"
using System;

delegate void D1();

class C1
{
    delegate void D2();
}
",
expectedHierarchicalLayout: @"
D1
C1
  C1.D2
",
expectedNonHierarchicalLayout: @"
C1
C1.D2
D1
");
        }
    }
}
