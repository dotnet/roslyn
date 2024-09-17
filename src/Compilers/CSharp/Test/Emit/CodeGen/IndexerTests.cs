// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class IndexerTests : CSharpTestBase
    {
        #region Declarations

        [Fact]
        public void ReadWriteIndexer()
        {
            var text = @"
class C
{
    public int this[int x] { get { return x; } set { } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, "System.Int32 C.this[System.Int32 x].get", "void C.this[System.Int32 x].set"),
                expectedSignatures: new[]
                {
                    Signature("C", "Item", ".property readwrite instance System.Int32 Item(System.Int32 x)"),
                    Signature("C", "get_Item", ".method public hidebysig specialname instance System.Int32 get_Item(System.Int32 x) cil managed"),
                    Signature("C", "set_Item", ".method public hidebysig specialname instance System.Void set_Item(System.Int32 x, System.Int32 value) cil managed")
                });
        }

        [Fact]
        public void ReadOnlyIndexer()
        {
            var text = @"
class C
{
    public int this[int x, int y] { get { return x; } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, "System.Int32 C.this[System.Int32 x, System.Int32 y].get", null),
                expectedSignatures: new[]
                {
                    Signature("C", "Item", ".property readonly instance System.Int32 Item(System.Int32 x, System.Int32 y)"),
                    Signature("C", "get_Item", ".method public hidebysig specialname instance System.Int32 get_Item(System.Int32 x, System.Int32 y) cil managed")
                });
        }

        [Fact]
        public void WriteOnlyIndexer()
        {
            var text = @"
class C
{
    public int this[int x, int y, int z] { set { } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, null, "void C.this[System.Int32 x, System.Int32 y, System.Int32 z].set"),
                expectedSignatures: new[]
                {
                    Signature("C", "Item", ".property writeonly instance System.Int32 Item(System.Int32 x, System.Int32 y, System.Int32 z)"),
                    Signature("C", "set_Item", ".method public hidebysig specialname instance System.Void set_Item(System.Int32 x, System.Int32 y, System.Int32 z, System.Int32 value) cil managed")
                });
        }

        [Fact]
        public void GenericIndexer()
        {
            var text = @"
class C<T>
{
    public T this[T x] { get { return x; } set { } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, "T C<T>.this[T x].get", "void C<T>.this[T x].set"),
                expectedSignatures: new[]
                {
                    Signature("C`1", "Item", ".property readwrite instance T Item(T x)"),
                    Signature("C`1", "get_Item", ".method public hidebysig specialname instance T get_Item(T x) cil managed"),
                    Signature("C`1", "set_Item", ".method public hidebysig specialname instance System.Void set_Item(T x, T value) cil managed")
                });
        }

        [Fact]
        public void IndexerWithOptionalParameters()
        {
            var text = @"
class C
{
    public int this[int x = 1, int y = 2] { get { return x; } set { } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, "System.Int32 C.this[[System.Int32 x = 1], [System.Int32 y = 2]].get", "void C.this[[System.Int32 x = 1], [System.Int32 y = 2]].set"),
                expectedSignatures: new[]
                {
                    Signature("C", "Item", ".property readwrite instance System.Int32 Item([opt] System.Int32 x = 1, [opt] System.Int32 y = 2)"),
                    Signature("C", "get_Item", ".method public hidebysig specialname instance System.Int32 get_Item([opt] System.Int32 x = 1, [opt] System.Int32 y = 2) cil managed"),
                    Signature("C", "set_Item", ".method public hidebysig specialname instance System.Void set_Item([opt] System.Int32 x = 1, [opt] System.Int32 y = 2, System.Int32 value) cil managed")
                });
        }

        [Fact]
        public void IndexerWithParameterArray()
        {
            var text = @"
class C
{
    public int this[params int[] x] { get { return 0; } set { } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, "System.Int32 C.this[params System.Int32[] x].get", "void C.this[params System.Int32[] x].set"),
                expectedSignatures: new[]
                {
                    Signature("C", "Item", ".property readwrite instance System.Int32 Item([System.ParamArrayAttribute()] System.Int32[] x)"),
                    Signature("C", "get_Item", ".method public hidebysig specialname instance System.Int32 get_Item([System.ParamArrayAttribute()] System.Int32[] x) cil managed"),
                    Signature("C", "set_Item", ".method public hidebysig specialname instance System.Void set_Item([System.ParamArrayAttribute()] System.Int32[] x, System.Int32 value) cil managed")
                });
        }

        [Fact]
        public void ExplicitInterfaceImplementation()
        {
            var text = @"
interface I
{
    int this[int x] { get; set; }
}

class C : I
{
    int I.this[int x] { get { return 0; } set { } }
}
";
            System.Action<ModuleSymbol> validator = module =>
            {
                // Can't use ValidateIndexer because explicit implementations aren't indexers in metadata.

                var @class = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var indexer = @class.GetMembers().Where(member => member.Kind == SymbolKind.Property).Cast<PropertySymbol>().Single();

                Assert.False(indexer.IsIndexer);
                Assert.True(indexer.MustCallMethodsDirectly); //since has parameters, but isn't an indexer
                Assert.Equal(Accessibility.Private, indexer.DeclaredAccessibility);
                Assert.False(indexer.IsStatic);

                var getMethod = indexer.GetMethod;
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, getMethod.MethodKind); //since CallMethodsDirectly
                Assert.Equal("System.Int32 C.I.get_Item(System.Int32 x)", getMethod.ToTestDisplayString());
                getMethod.CheckAccessorModifiers(indexer);

                var setMethod = indexer.SetMethod;
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, setMethod.MethodKind); //since CallMethodsDirectly
                Assert.Equal("void C.I.set_Item(System.Int32 x, System.Int32 value)", setMethod.ToTestDisplayString());
                setMethod.CheckAccessorModifiers(indexer);
            };
            var compVerifier = CompileAndVerify(text, symbolValidator: validator, expectedSignatures: new[]
            {
                Signature("C", "I.Item", ".property readwrite System.Int32 I.Item(System.Int32 x)"),
                Signature("C", "I.get_Item", ".method private hidebysig newslot specialname virtual final instance System.Int32 I.get_Item(System.Int32 x) cil managed"),
                Signature("C", "I.set_Item", ".method private hidebysig newslot specialname virtual final instance System.Void I.set_Item(System.Int32 x, System.Int32 value) cil managed")
            });
        }

        [Fact]
        public void ImplicitInterfaceImplementation()
        {
            var text = @"
interface I
{
    int this[int x] { get; set; }
}

class C : I
{
    public int this[int x] { get { return 0; } set { } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, "System.Int32 C.this[System.Int32 x].get", "void C.this[System.Int32 x].set"),
                expectedSignatures: new[]
                {
                    Signature("C", "Item", ".property readwrite instance System.Int32 Item(System.Int32 x)"),
                    Signature("C", "get_Item", ".method public hidebysig newslot specialname virtual final instance System.Int32 get_Item(System.Int32 x) cil managed"),
                    Signature("C", "set_Item", ".method public hidebysig newslot specialname virtual final instance System.Void set_Item(System.Int32 x, System.Int32 value) cil managed")
                });
        }

        [Fact]
        public void Overriding()
        {
            var text = @"
class B
{
    public virtual int this[int x] { get { return 0; } set { } }
}

class C : B
{
    public override sealed int this[int x] { get { return 0; } set { } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, "System.Int32 C.this[System.Int32 x].get", "void C.this[System.Int32 x].set"),
                expectedSignatures: new[]
                {
                    Signature("C", "Item", ".property readwrite instance System.Int32 Item(System.Int32 x)"),
                    Signature("C", "get_Item", ".method public hidebysig specialname virtual final instance System.Int32 get_Item(System.Int32 x) cil managed"),
                    Signature("C", "set_Item", ".method public hidebysig specialname virtual final instance System.Void set_Item(System.Int32 x, System.Int32 value) cil managed")
                });
        }

        [Fact]
        public void Hiding()
        {
            var text = @"
class B
{
    public virtual int this[int x] { get { return 0; } set { } }
}

class C : B
{
    public new virtual int this[int x] { get { return 0; } set { } }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateIndexer(module, "System.Int32 C.this[System.Int32 x].get", "void C.this[System.Int32 x].set"),
                expectedSignatures: new[]
                {
                    Signature("C", "Item", ".property readwrite instance System.Int32 Item(System.Int32 x)"),
                    Signature("C", "get_Item", ".method public hidebysig newslot specialname virtual instance System.Int32 get_Item(System.Int32 x) cil managed"),
                    Signature("C", "set_Item", ".method public hidebysig newslot specialname virtual instance System.Void set_Item(System.Int32 x, System.Int32 value) cil managed")
                });
        }

        // NOTE: assumes there's a single indexer (type = int) in a type C.
        private static void ValidateIndexer(ModuleSymbol module, string getterDisplayString, string setterDisplayString)
        {
            var @class = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var indexer = @class.Indexers.Single();

            Assert.Equal(SymbolKind.Property, indexer.Kind);
            Assert.True(indexer.IsIndexer);
            Assert.False(indexer.MustCallMethodsDirectly);
            Assert.Equal(Accessibility.Public, indexer.DeclaredAccessibility);
            Assert.False(indexer.IsStatic);

            var getMethod = indexer.GetMethod;
            if (getterDisplayString == null)
            {
                Assert.Null(getMethod);
            }
            else
            {
                Assert.Equal(MethodKind.PropertyGet, getMethod.MethodKind);
                Assert.Equal(getterDisplayString, getMethod.ToTestDisplayString());
                getMethod.CheckAccessorShape(indexer);
            }

            var setMethod = indexer.SetMethod;
            if (setterDisplayString == null)
            {
                Assert.Null(setMethod);
            }
            else
            {
                Assert.Equal(MethodKind.PropertySet, setMethod.MethodKind);
                Assert.Equal(setterDisplayString, setMethod.ToTestDisplayString());
                setMethod.CheckAccessorShape(indexer);
            }
        }

        #endregion Declarations

        #region Lowering

        private const string TypeWithIndexers = @"
public class C
{
    public static int Goo(int x)
    {
        System.Console.Write(x + "","");
        return x * 10;
    }

    public int this[int x, int y = 9]
    {
        get
        {
            System.Console.Write(x + "","");
            System.Console.Write(y + "","");
            return -(x + y);
        }
        set
        {
            System.Console.Write(x + "","");
            System.Console.Write(y + "","");
            System.Console.Write(value + "","");
        }
    }

    public int this[params int[] x]
    {
        get
        {
            foreach (var i in x) System.Console.Write(i + "","");
            return -(x.Length);
        }
        set
        {
            foreach (var i in x) System.Console.Write(i + "","");
            System.Console.Write(value + "","");
        }
    }
}
";

        [Fact]
        public void LoweringRead()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static void Main()
    {
        C c = new C();
        int x;

        //// Normal

        x = c[C.Goo(1), C.Goo(2)];
        System.Console.WriteLine();

        //// Named parameters

        x = c[C.Goo(1), y: C.Goo(2)]; //NB: Dev10 gets this wrong (2,1,10,20)
        System.Console.WriteLine();

        x = c[x: C.Goo(1), y: C.Goo(2)];
        System.Console.WriteLine();

        x = c[y: C.Goo(2), x: C.Goo(1)];
        System.Console.WriteLine();

        //// Optional parameters

        x = c[C.Goo(1)];
        System.Console.WriteLine();

        x = c[x: C.Goo(1)];
        System.Console.WriteLine();

        //// Parameter arrays

        x = c[C.Goo(1), C.Goo(2), C.Goo(3)];
        System.Console.WriteLine();

        x = c[new int[] { C.Goo(1), C.Goo(2), C.Goo(3) }];
        System.Console.WriteLine();
    }
}
";
            CompileAndVerify(text, expectedOutput: @"
1,2,10,20,
1,2,10,20,
1,2,10,20,
2,1,10,20,
1,10,9,
1,10,9,
1,2,3,10,20,30,
1,2,3,10,20,30,
");
        }

        [Fact]
        public void LoweringReadIL()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static void Main()
    {
        C c = new C();
        int x;

        //// Normal

        x = c[1, 2];

        //// Named parameters

        x = c[1, y: 2];
        x = c[x: 1, y: 2];
        x = c[y: 2, x: 1];

        //// Optional parameters

        x = c[1];
        x = c[x: 1];

        //// Parameter arrays

        x = c[1, 2, 3];
        x = c[new int[] { 1, 2, 3 }];
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));

            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      111 (0x6f)
  .maxstack  4
  .locals init (C V_0) //c
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.2
  IL_0009:  callvirt   ""int C.this[int, int].get""
  IL_000e:  pop
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.2
  IL_0012:  callvirt   ""int C.this[int, int].get""
  IL_0017:  pop
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.1
  IL_001a:  ldc.i4.2
  IL_001b:  callvirt   ""int C.this[int, int].get""
  IL_0020:  pop
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.2
  IL_0024:  callvirt   ""int C.this[int, int].get""
  IL_0029:  pop
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.1
  IL_002c:  ldc.i4.s   9
  IL_002e:  callvirt   ""int C.this[int, int].get""
  IL_0033:  pop
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.1
  IL_0036:  ldc.i4.s   9
  IL_0038:  callvirt   ""int C.this[int, int].get""
  IL_003d:  pop
  IL_003e:  ldloc.0
  IL_003f:  ldc.i4.3
  IL_0040:  newarr     ""int""
  IL_0045:  dup
  IL_0046:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_004b:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0050:  callvirt   ""int C.this[params int[]].get""
  IL_0055:  pop
  IL_0056:  ldloc.0
  IL_0057:  ldc.i4.3
  IL_0058:  newarr     ""int""
  IL_005d:  dup
  IL_005e:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_0063:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0068:  callvirt   ""int C.this[params int[]].get""
  IL_006d:  pop
  IL_006e:  ret
}
");
        }

        [Fact]
        public void LoweringAssignment()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static void Main()
    {
        C c = new C();

        //// Normal

        c[C.Goo(1), C.Goo(2)] = C.Goo(3);
        System.Console.WriteLine();

        //// Named parameters

        c[C.Goo(1), y: C.Goo(2)] = C.Goo(3);
        System.Console.WriteLine();

        c[x: C.Goo(1), y: C.Goo(2)] = C.Goo(3); //NB: dev10 gets this wrong (2,1,3,10,20,30,)
        System.Console.WriteLine();

        c[y: C.Goo(2), x: C.Goo(1)] = C.Goo(3);
        System.Console.WriteLine();

        //// Optional parameters

        c[C.Goo(1)] = C.Goo(3);
        System.Console.WriteLine();

        c[x: C.Goo(1)] = C.Goo(3);
        System.Console.WriteLine();

        //// Parameter arrays

        c[C.Goo(1), C.Goo(2), C.Goo(3)] = C.Goo(4);
        System.Console.WriteLine();

        c[new int[] { C.Goo(1), C.Goo(2), C.Goo(3) }] = C.Goo(4);
        System.Console.WriteLine();
    }
}
";
            CompileAndVerify(text, expectedOutput: @"
1,2,3,10,20,30,
1,2,3,10,20,30,
1,2,3,10,20,30,
2,1,3,10,20,30,
1,3,10,9,30,
1,3,10,9,30,
1,2,3,4,10,20,30,40,
1,2,3,4,10,20,30,40,
");
        }

        [Fact]
        public void LoweringAssignmentIL()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static void Main()
    {
        C c = new C();

        //// Normal

        c[1, 2] = 3;

        //// Named parameters

        c[1, y: 2] = 3;
        c[x: 1, y: 2] = 3;
        c[y: 2, x: 1] = 3;

        //// Optional parameters

        c[1] = 3;
        c[x: 1] = 3;

        //// Parameter arrays

        c[1, 2, 3] = 4;
        c[new int[] { 1, 2, 3 }] = 4;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));

            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      111 (0x6f)
  .maxstack  4
  .locals init (C V_0) //c
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.2
  IL_0009:  ldc.i4.3
  IL_000a:  callvirt   ""void C.this[int, int].set""
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.2
  IL_0012:  ldc.i4.3
  IL_0013:  callvirt   ""void C.this[int, int].set""
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.1
  IL_001a:  ldc.i4.2
  IL_001b:  ldc.i4.3
  IL_001c:  callvirt   ""void C.this[int, int].set""
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.2
  IL_0024:  ldc.i4.3
  IL_0025:  callvirt   ""void C.this[int, int].set""
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.1
  IL_002c:  ldc.i4.s   9
  IL_002e:  ldc.i4.3
  IL_002f:  callvirt   ""void C.this[int, int].set""
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.1
  IL_0036:  ldc.i4.s   9
  IL_0038:  ldc.i4.3
  IL_0039:  callvirt   ""void C.this[int, int].set""
  IL_003e:  ldloc.0
  IL_003f:  ldc.i4.3
  IL_0040:  newarr     ""int""
  IL_0045:  dup
  IL_0046:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_004b:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0050:  ldc.i4.4
  IL_0051:  callvirt   ""void C.this[params int[]].set""
  IL_0056:  ldloc.0
  IL_0057:  ldc.i4.3
  IL_0058:  newarr     ""int""
  IL_005d:  dup
  IL_005e:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_0063:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0068:  ldc.i4.4
  IL_0069:  callvirt   ""void C.this[params int[]].set""
  IL_006e:  ret
}
");
        }

        [Fact]
        public void LoweringIncrement()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static void Main()
    {
        C c = new C();

        //// Normal

        c[C.Goo(1), C.Goo(2)]++;
        System.Console.WriteLine();

        //// Named parameters

        c[C.Goo(1), y: C.Goo(2)]++;
        System.Console.WriteLine();

        c[x: C.Goo(1), y: C.Goo(2)]++; //NB: dev10 gets this wrong (2,1,10,20,10,20,-29,)
        System.Console.WriteLine();

        c[y: C.Goo(2), x: C.Goo(1)]++;
        System.Console.WriteLine();

        //// Optional parameters

        c[C.Goo(1)]++;
        System.Console.WriteLine();

        c[x: C.Goo(1)]++;
        System.Console.WriteLine();

        //// Parameter arrays

        c[C.Goo(1), C.Goo(2), C.Goo(3)]++;
        System.Console.WriteLine();

        c[new int[] { C.Goo(1), C.Goo(2), C.Goo(3) }]++;
        System.Console.WriteLine();
    }
}
";
            var compVerifier = CompileAndVerify(text, expectedOutput: @"
1,2,10,20,10,20,-29,
1,2,10,20,10,20,-29,
1,2,10,20,10,20,-29,
2,1,10,20,10,20,-29,
1,10,9,10,9,-18,
1,10,9,10,9,-18,
1,2,3,10,20,30,10,20,30,-2,
1,2,3,10,20,30,10,20,30,-2,
");
        }

        [Fact]
        public void LoweringIncrementIL()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static void Main()
    {
        C c = new C();

        //// Normal

        c[1, 2]++;

        //// Named parameters

        c[1, y: 2]++;
        c[x: 1, y: 2]++;
        c[y: 2, x: 1]++;

        //// Optional parameters

        c[1]++;
        c[x: 1]++;

        //// Parameter arrays

        c[1, 2, 3]++;
        c[new int[] { 1, 2, 3 }]++;
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));

            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      207 (0xcf)
  .maxstack  5
  .locals init (C V_0, //c
                int V_1,
                C V_2,
                int[] V_3)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  dup
  IL_0008:  ldc.i4.1
  IL_0009:  ldc.i4.2
  IL_000a:  callvirt   ""int C.this[int, int].get""
  IL_000f:  stloc.1
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.2
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4.1
  IL_0014:  add
  IL_0015:  callvirt   ""void C.this[int, int].set""
  IL_001a:  ldloc.0
  IL_001b:  dup
  IL_001c:  ldc.i4.1
  IL_001d:  ldc.i4.2
  IL_001e:  callvirt   ""int C.this[int, int].get""
  IL_0023:  stloc.1
  IL_0024:  ldc.i4.1
  IL_0025:  ldc.i4.2
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4.1
  IL_0028:  add
  IL_0029:  callvirt   ""void C.this[int, int].set""
  IL_002e:  ldloc.0
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.2
  IL_0032:  callvirt   ""int C.this[int, int].get""
  IL_0037:  stloc.1
  IL_0038:  ldc.i4.1
  IL_0039:  ldc.i4.2
  IL_003a:  ldloc.1
  IL_003b:  ldc.i4.1
  IL_003c:  add
  IL_003d:  callvirt   ""void C.this[int, int].set""
  IL_0042:  ldloc.0
  IL_0043:  dup
  IL_0044:  ldc.i4.1
  IL_0045:  ldc.i4.2
  IL_0046:  callvirt   ""int C.this[int, int].get""
  IL_004b:  stloc.1
  IL_004c:  ldc.i4.1
  IL_004d:  ldc.i4.2
  IL_004e:  ldloc.1
  IL_004f:  ldc.i4.1
  IL_0050:  add
  IL_0051:  callvirt   ""void C.this[int, int].set""
  IL_0056:  ldloc.0
  IL_0057:  dup
  IL_0058:  ldc.i4.1
  IL_0059:  ldc.i4.s   9
  IL_005b:  callvirt   ""int C.this[int, int].get""
  IL_0060:  stloc.1
  IL_0061:  ldc.i4.1
  IL_0062:  ldc.i4.s   9
  IL_0064:  ldloc.1
  IL_0065:  ldc.i4.1
  IL_0066:  add
  IL_0067:  callvirt   ""void C.this[int, int].set""
  IL_006c:  ldloc.0
  IL_006d:  dup
  IL_006e:  ldc.i4.1
  IL_006f:  ldc.i4.s   9
  IL_0071:  callvirt   ""int C.this[int, int].get""
  IL_0076:  stloc.1
  IL_0077:  ldc.i4.1
  IL_0078:  ldc.i4.s   9
  IL_007a:  ldloc.1
  IL_007b:  ldc.i4.1
  IL_007c:  add
  IL_007d:  callvirt   ""void C.this[int, int].set""
  IL_0082:  ldloc.0
  IL_0083:  stloc.2
  IL_0084:  ldc.i4.3
  IL_0085:  newarr     ""int""
  IL_008a:  dup
  IL_008b:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_0090:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0095:  stloc.3
  IL_0096:  ldloc.2
  IL_0097:  ldloc.3
  IL_0098:  callvirt   ""int C.this[params int[]].get""
  IL_009d:  stloc.1
  IL_009e:  ldloc.2
  IL_009f:  ldloc.3
  IL_00a0:  ldloc.1
  IL_00a1:  ldc.i4.1
  IL_00a2:  add
  IL_00a3:  callvirt   ""void C.this[params int[]].set""
  IL_00a8:  ldloc.0
  IL_00a9:  stloc.2
  IL_00aa:  ldc.i4.3
  IL_00ab:  newarr     ""int""
  IL_00b0:  dup
  IL_00b1:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_00b6:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_00bb:  stloc.3
  IL_00bc:  ldloc.2
  IL_00bd:  ldloc.3
  IL_00be:  callvirt   ""int C.this[params int[]].get""
  IL_00c3:  stloc.1
  IL_00c4:  ldloc.2
  IL_00c5:  ldloc.3
  IL_00c6:  ldloc.1
  IL_00c7:  ldc.i4.1
  IL_00c8:  add
  IL_00c9:  callvirt   ""void C.this[params int[]].set""
  IL_00ce:  ret
}
");
        }

        [Fact]
        public void LoweringCompoundAssignment()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static void Main()
    {
        C c = new C();

        //// Normal

        c[C.Goo(1), C.Goo(2)] += C.Goo(3);
        System.Console.WriteLine();

        //// Named parameters

        c[C.Goo(1), y: C.Goo(2)] += C.Goo(3);
        System.Console.WriteLine();

        c[x: C.Goo(1), y: C.Goo(2)] += C.Goo(3); //NB: dev10 gets this wrong (2,1,10,20,3,10,20,0,)
        System.Console.WriteLine();

        c[y: C.Goo(2), x: C.Goo(1)] += C.Goo(3);
        System.Console.WriteLine();

        //// Optional parameters

        c[C.Goo(1)] += C.Goo(3);
        System.Console.WriteLine();

        c[x: C.Goo(1)] += C.Goo(3);
        System.Console.WriteLine();

        //// Parameter arrays

        c[C.Goo(1), C.Goo(2), C.Goo(3)] += C.Goo(4);
        System.Console.WriteLine();

        c[new int[] { C.Goo(1), C.Goo(2), C.Goo(3) }] += C.Goo(4);
        System.Console.WriteLine();
    }
}
";
            CompileAndVerify(text, expectedOutput: @"
1,2,10,20,3,10,20,0,
1,2,10,20,3,10,20,0,
1,2,10,20,3,10,20,0,
2,1,10,20,3,10,20,0,
1,10,9,3,10,9,11,
1,10,9,3,10,9,11,
1,2,3,10,20,30,4,10,20,30,37,
1,2,3,10,20,30,4,10,20,30,37,
");
        }

        [Fact]
        public void LoweringCompoundAssignmentIL()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static void Main()
    {
        C c = new C();

        //// Normal

        c[1, 2] += 3;
        System.Console.WriteLine();

        //// Named parameters

        c[1, y: 2] += 3;
        System.Console.WriteLine();

        c[x: 1, y: 2] += 3; //NB: dev10 gets this wrong (2,1,10,20,3,10,20,0,)
        System.Console.WriteLine();

        c[y: 2, x: 1] += 3;
        System.Console.WriteLine();

        //// Optional parameters

        c[1] += 3;
        System.Console.WriteLine();

        c[x: 1] += 3;
        System.Console.WriteLine();

        //// Parameter arrays

        c[1, 2, 3] += 4;
        System.Console.WriteLine();

        c[new int[] { 1, 2, 3 }] += 4;
        System.Console.WriteLine();
    }
}
";
            var compVerifier = CompileAndVerify(text, options: TestOptions.ReleaseExe.WithModuleName("MODULE"));

            compVerifier.VerifyIL("Test.Main", @"
{
  // Code size      243 (0xf3)
  .maxstack  6
  .locals init (C V_0, //c
                C V_1,
                int[] V_2)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.2
  IL_000b:  ldloc.1
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  callvirt   ""int C.this[int, int].get""
  IL_0013:  ldc.i4.3
  IL_0014:  add
  IL_0015:  callvirt   ""void C.this[int, int].set""
  IL_001a:  call       ""void System.Console.WriteLine()""
  IL_001f:  ldloc.0
  IL_0020:  stloc.1
  IL_0021:  ldloc.1
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.2
  IL_0024:  ldloc.1
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.2
  IL_0027:  callvirt   ""int C.this[int, int].get""
  IL_002c:  ldc.i4.3
  IL_002d:  add
  IL_002e:  callvirt   ""void C.this[int, int].set""
  IL_0033:  call       ""void System.Console.WriteLine()""
  IL_0038:  ldloc.0
  IL_0039:  stloc.1
  IL_003a:  ldloc.1
  IL_003b:  ldc.i4.1
  IL_003c:  ldc.i4.2
  IL_003d:  ldloc.1
  IL_003e:  ldc.i4.1
  IL_003f:  ldc.i4.2
  IL_0040:  callvirt   ""int C.this[int, int].get""
  IL_0045:  ldc.i4.3
  IL_0046:  add
  IL_0047:  callvirt   ""void C.this[int, int].set""
  IL_004c:  call       ""void System.Console.WriteLine()""
  IL_0051:  ldloc.0
  IL_0052:  stloc.1
  IL_0053:  ldloc.1
  IL_0054:  ldc.i4.1
  IL_0055:  ldc.i4.2
  IL_0056:  ldloc.1
  IL_0057:  ldc.i4.1
  IL_0058:  ldc.i4.2
  IL_0059:  callvirt   ""int C.this[int, int].get""
  IL_005e:  ldc.i4.3
  IL_005f:  add
  IL_0060:  callvirt   ""void C.this[int, int].set""
  IL_0065:  call       ""void System.Console.WriteLine()""
  IL_006a:  ldloc.0
  IL_006b:  stloc.1
  IL_006c:  ldloc.1
  IL_006d:  ldc.i4.1
  IL_006e:  ldc.i4.s   9
  IL_0070:  ldloc.1
  IL_0071:  ldc.i4.1
  IL_0072:  ldc.i4.s   9
  IL_0074:  callvirt   ""int C.this[int, int].get""
  IL_0079:  ldc.i4.3
  IL_007a:  add
  IL_007b:  callvirt   ""void C.this[int, int].set""
  IL_0080:  call       ""void System.Console.WriteLine()""
  IL_0085:  ldloc.0
  IL_0086:  stloc.1
  IL_0087:  ldloc.1
  IL_0088:  ldc.i4.1
  IL_0089:  ldc.i4.s   9
  IL_008b:  ldloc.1
  IL_008c:  ldc.i4.1
  IL_008d:  ldc.i4.s   9
  IL_008f:  callvirt   ""int C.this[int, int].get""
  IL_0094:  ldc.i4.3
  IL_0095:  add
  IL_0096:  callvirt   ""void C.this[int, int].set""
  IL_009b:  call       ""void System.Console.WriteLine()""
  IL_00a0:  ldloc.0
  IL_00a1:  stloc.1
  IL_00a2:  ldc.i4.3
  IL_00a3:  newarr     ""int""
  IL_00a8:  dup
  IL_00a9:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_00ae:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_00b3:  stloc.2
  IL_00b4:  ldloc.1
  IL_00b5:  ldloc.2
  IL_00b6:  ldloc.1
  IL_00b7:  ldloc.2
  IL_00b8:  callvirt   ""int C.this[params int[]].get""
  IL_00bd:  ldc.i4.4
  IL_00be:  add
  IL_00bf:  callvirt   ""void C.this[params int[]].set""
  IL_00c4:  call       ""void System.Console.WriteLine()""
  IL_00c9:  ldloc.0
  IL_00ca:  stloc.1
  IL_00cb:  ldc.i4.3
  IL_00cc:  newarr     ""int""
  IL_00d1:  dup
  IL_00d2:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D""
  IL_00d7:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_00dc:  stloc.2
  IL_00dd:  ldloc.1
  IL_00de:  ldloc.2
  IL_00df:  ldloc.1
  IL_00e0:  ldloc.2
  IL_00e1:  callvirt   ""int C.this[params int[]].get""
  IL_00e6:  ldc.i4.4
  IL_00e7:  add
  IL_00e8:  callvirt   ""void C.this[params int[]].set""
  IL_00ed:  call       ""void System.Console.WriteLine()""
  IL_00f2:  ret
}
");
        }

        [Fact]
        public void LoweringComplex()
        {
            var text = TypeWithIndexers + @"
class Test
{
    static C NewC() 
    { 
        System.Console.Write(""N,"");
        return new C(); 
    }

    static void Main()
    {
        NewC()[y: C.Goo(1), x: NewC()[C.Goo(2)]] = NewC()[x: C.Goo(3), y: NewC()[C.Goo(4)]] += NewC()[C.Goo(5), C.Goo(6), NewC()[C.Goo(7)]]++;
        System.Console.WriteLine();
    }
}
";
            CompileAndVerify(text, expectedOutput: @"
N,1,N,2,20,9,N,3,N,4,40,9,30,-49,N,5,6,N,7,70,9,50,60,-79,50,60,-79,-2,30,-49,16,-29,10,16,
");
        }

        [Fact]
        public void BoxingParameterArrayArguments()
        {
            var text = TypeWithIndexers + @"
class Test
{
    int this[params object[] args] { get { return args.Length; } }

    static void Main()
    {
        System.Console.WriteLine(new Test()[1, 2]);
    }
}
";
            var verifier = CompileAndVerify(text, expectedOutput: @"2");

            verifier.VerifyIL("Test.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  5
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  ldc.i4.2
  IL_0006:  newarr     ""object""
  IL_000b:  dup
  IL_000c:  ldc.i4.0
  IL_000d:  ldc.i4.1
  IL_000e:  box        ""int""
  IL_0013:  stelem.ref
  IL_0014:  dup
  IL_0015:  ldc.i4.1
  IL_0016:  ldc.i4.2
  IL_0017:  box        ""int""
  IL_001c:  stelem.ref
  IL_001d:  call       ""int Test.this[params object[]].get""
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ret
}
");
        }

        [Fact]
        public void IndexerOverrideNotAllAccessors_DefaultParameterValues()
        {
            var text = @"
using System;

class Base
{
    public virtual int this[int x, int y = 1]
    {
        get
        {
            Console.WriteLine(""Base.get y: "" + y);
            return y;
        }
        set { Console.WriteLine(""Base.set y: "" + y); }
    }
}

class Override : Base
{
    public override int this[int x, int y = 2]
    {
        set { Console.WriteLine(""Override.set y: "" + y); }
    }
}

class Program
{
    static void Main()
    {
        Base b = new Base();
        _ = b[0];
        b[0] = 0;

        Override o = new Override();
        _ = o[0];
        o[0] = 0;
        o[0] += 0;
    }
}
";
            CompileAndVerify(text, expectedOutput:
@"Base.get y: 1
Base.set y: 1
Base.get y: 1
Override.set y: 2
Base.get y: 1
Override.set y: 1
");
        }

        #endregion Lowering

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/75032")]
        public void MissingDefaultMemberAttribute()
        {
            var text1 = @"
public interface I1
{
    public I1 this[I1 args] { get; }
}
";
            var comp1 = CreateCompilation(text1);
            comp1.MakeMemberMissing(WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor);

            CompileAndVerify(comp1).VerifyDiagnostics();

            var text2 = @"
class Program
{
    static void Test(I1 x)
    {
        _ = x[null];
        _ = x.get_Item(null);
    }
}
";
            var comp2 = CreateCompilation(text2, references: [comp1.ToMetadataReference()]);
            comp2.VerifyDiagnostics(
                // (7,15): error CS0571: 'I1.this[I1].get': cannot explicitly call operator or accessor
                //         _ = x.get_Item(null);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Item").WithArguments("I1.this[I1].get").WithLocation(7, 15)
                );

            var comp3 = CreateCompilation(text2, references: [comp1.EmitToImageReference()]);
            comp3.VerifyDiagnostics(
                // (6,13): error CS0021: Cannot apply indexing with [] to an expression of type 'I1'
                //         _ = x[null];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[null]").WithArguments("I1").WithLocation(6, 13)
                );
        }
    }
}
