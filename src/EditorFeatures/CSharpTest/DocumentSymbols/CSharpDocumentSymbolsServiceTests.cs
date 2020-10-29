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

        private protected override IDocumentSymbolsService GetDocumentSymbolsService(Document document1)
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
ClassInternal C1
  ClassPrivate C1.C2
ClassInternal C4
ClassInternal C3",
expectedNonHierarchicalLayout: @"
ClassInternal C1
ClassPrivate C1.C2
ClassInternal C3
ClassInternal C4");
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
ClassInternal C1
  MethodPrivate M1()
    MethodPrivate Local()
  ClassPrivate C1.C2
    MethodPrivate M2()",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  MethodPrivate M1()
ClassPrivate C1.C2
  MethodPrivate M2()");
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
Local i1
ClassInternal C1
  MethodPrivate M1()
    Local i2
    MethodPrivate LocalFunc()
      Local i3",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  MethodPrivate M1()");
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
ClassInternal C1
  FieldPrivate i1
  FieldPublic I2
  ClassPrivate C1.C2
    FieldPrivate i3",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  FieldPrivate i1
  FieldPublic I2
ClassPrivate C1.C2
  FieldPrivate i3");
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
ClassInternal C1
  PropertyPrivate I1
  PropertyPublic I2
  ClassPrivate C1.C2
    PropertyPrivate I3",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  PropertyPrivate I1
  PropertyPublic I2
ClassPrivate C1.C2
  PropertyPrivate I3");
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
ClassInternal C1
  EventPrivate Event",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  EventPrivate Event");
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
Local i1 Constant
ClassInternal C1
  ConstantPrivate i2 Constant
  MethodPrivate M()
    Local i3 Constant",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  ConstantPrivate i2 Constant
  MethodPrivate M()");
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
ClassInternal C1
  MethodPrivate M()
    Local i1
  MethodPrivate M1(out int i)",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  MethodPrivate M()
  MethodPrivate M1(out int i)");
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
ClassInternal C1
  MethodPrivate M(object o)
    Local i1
    Local i2
    Local i3",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  MethodPrivate M(object o)");
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
ClassInternal N1.C1",
expectedNonHierarchicalLayout: @"
ClassInternal N1.C1");
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
ClassInternal C1
  ConstructorPrivate C1(int i1)
  ConstructorPrivate C1()
  ConstructorPrivate C1(object o1, object o2)",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  ConstructorPrivate C1()
  ConstructorPrivate C1(int i1)
  ConstructorPrivate C1(object o1, object o2)");
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
ClassInternal C1
  MethodPrivate M1()
ClassInternal C1
  MethodPrivate M1()",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  MethodPrivate M1()
  MethodPrivate M1()");
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
ClassInternal C1<T1, T2>
  TypeParameter T1
  TypeParameter T2
  MethodPrivate M1<T3, T4>()
    TypeParameter T3
    TypeParameter T4",
expectedNonHierarchicalLayout: @"
ClassInternal C1<T1, T2>
  MethodPrivate M1<T3, T4>()");
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
ClassInternal C1
  MethodPrivate M1()
    Local a",
expectedNonHierarchicalLayout: @"
ClassInternal C1
  MethodPrivate M1()");
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
DelegateInternal D1
ClassInternal C1
  DelegatePrivate C1.D2",
expectedNonHierarchicalLayout: @"
ClassInternal C1
DelegatePrivate C1.D2
DelegateInternal D1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentSymbols), UseExportProvider]
        public async Task TestDocumentSymbols__Obsolete()
        {
            await AssertExpectedContent(@"
using System;

[Obsolete]
class C1
{
    [Obsolete]
    delegate void D1();
    [Obsolete]
    public int i1;
    [Obsolete]
    public int I2 { get; set; }
    [Obsolete]
    public void M1() {}
}
",
expectedHierarchicalLayout: @"
(obsolete) ClassInternal C1
  (obsolete) DelegatePrivate C1.D1
  (obsolete) FieldPublic i1
  (obsolete) PropertyPublic I2
  (obsolete) MethodPublic M1()
",
expectedNonHierarchicalLayout: @"
(obsolete) ClassInternal C1
  (obsolete) FieldPublic i1
  (obsolete) PropertyPublic I2
  (obsolete) MethodPublic M1()
  (obsolete) DelegatePrivate C1.D1");
        }
    }
}
