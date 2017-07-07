﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class AnonymousTypesSymbolTests : CompilingTestBase
    {
        [ClrOnlyFact]
        public void AnonymousTypeSymbol_InQuery()
        {
            var source = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3);
        List1<int> r1 =
            from int x in c1
            let g = x * 10
            let z = g + x*100
            select x + z;
        System.Console.WriteLine(r1);
    }
}
";
            var verifier = CompileAndVerify(
                source,
                symbolValidator: module => TestAnonymousTypeSymbols(
                                               module,
                                               new TypeDescr() { FieldNames = new string[] { "x", "g" } },
                                               new TypeDescr() { FieldNames = new string[] { "<>h__TransparentIdentifier0", "z" } }
                                           )
            );

            TestAnonymousTypeFieldSymbols_InQuery(verifier.EmittedAssemblyData);
        }

        [ClrOnlyFact]
        public void AnonymousTypeSymbol_Mix()
        {
            var source = @"
using System;

namespace Test
{
    class Program
    {
        static int Main()
        {
            int result = 0;

            var a0 = new { b1 = true, b2 = false };

            var a1 = new
            {
                b1 = 0123456789,
                b2 = 1234567890U,
                b3 = 2345678901u,
                b4 = 3456789012L,
                b5 = 4567890123l,
                b6 = 5678901234UL,
                b7 = 6789012345Ul,
                b8 = 7890123456uL,
                b9 = 8901234567ul,
                b10 = 9012345678LU,
                b11 = 9123456780Lu,
                b12 = 1234567809lU,
                b13 = 2345678091lu
            };
            return result;        
        }
    }
}
";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void AnonymousTypeInConstructedMethod_NonEmpty()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Foo<int>()());
    }

    static Func<object> Foo<T>()
    {
        T x2 = default(T);
        return (Func<object>) (() => new { x2 });
    }
}";
            CompileAndVerify(
                source,
                expectedOutput: "{ x2 = 0 }"
            );
        }

        [ClrOnlyFact]
        public void AnonymousTypeInConstructedMethod_NonEmpty2()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Foo<int>()());
    }

    static Func<object> Foo<T>()
    {
        T x2 = default(T);
        Func<object> x3 = () => new { x2 };
        return x3;
    }
}";
            CompileAndVerify(
                source,
                expectedOutput: "{ x2 = 0 }"
            );
        }

        [ClrOnlyFact]
        public void AnonymousTypeInConstructedMethod_NonEmpty3()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Foo<int>());
        Console.WriteLine(Foo<string>());
        Console.WriteLine(Foo<int?>());
    }

    static object Foo<T>()
    {
        T x2 = default(T);
        return new { x2 };
    }
}";
            CompileAndVerify(
                source,
                expectedOutput:
@"{ x2 = 0 }
{ x2 =  }
{ x2 =  }"
            );
        }

        [ClrOnlyFact]
        public void AnonymousTypeInConstructedMethod_NonEmpty4()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        foreach(var x in Foo<int>())
        {
            Console.Write(x);
        }
    }

    static IEnumerable<object> Foo<T>()
    {
        T x2 = default(T);
        yield return new { x2 }.ToString();
        yield return new { YYY = default(T), z = new { field = x2 } };
    }
}";
            CompileAndVerify(
                source,
                expectedOutput: "{ x2 = 0 }{ YYY = 0, z = { field = 0 } }"
            );
        }

        [ClrOnlyFact]
        public void AnonymousTypeInConstructedMethod_Empty()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Foo<int>()());
    }

    static Func<object> Foo<T>()
    {
        T x2 = default(T);
        return (Func<object>) (() => new { });
    }
}";
            CompileAndVerify(
                source,
                expectedOutput: "{ }"
            );
        }

        #region AnonymousTypeSymbol_InQuery :: Checking fields via reflection

        private void TestAnonymousTypeFieldSymbols_InQuery(ImmutableArray<byte> image)
        {
            Assembly refAsm = CorLightup.Desktop.LoadAssembly(image.ToArray());
            Type type = refAsm.GetType("<>f__AnonymousType0`2");
            Assert.NotNull(type);
            Assert.Equal(2, type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Count());

            CheckField(type.GetField("<x>i__Field", BindingFlags.NonPublic | BindingFlags.Instance), type.GetGenericArguments()[0]);
            CheckField(type.GetField("<g>i__Field", BindingFlags.NonPublic | BindingFlags.Instance), type.GetGenericArguments()[1]);
        }

        private void CheckField(FieldInfo field, Type fieldType)
        {
            Assert.NotNull(field);
            Assert.NotNull(fieldType);
            Assert.Equal(fieldType, field.FieldType);
            Assert.Equal(FieldAttributes.Private | FieldAttributes.InitOnly, field.Attributes);

            var attrs = field.CustomAttributes.ToList();
            Assert.Equal(1, attrs.Count);
            Assert.Equal(typeof(DebuggerBrowsableAttribute), attrs[0].AttributeType);

            var args = attrs[0].ConstructorArguments.ToArray();
            Assert.Equal(1, args.Length);
            Assert.Equal(typeof(DebuggerBrowsableState), args[0].ArgumentType);
            Assert.Equal(DebuggerBrowsableState.Never, (DebuggerBrowsableState)args[0].Value);
        }

        #endregion

        [ClrOnlyFact]
        public void AnonymousTypeSymbol_Simple()
        {
            var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        var at1 = new { a = 1, b = 2 };
        object at2 = new { (new string[2]).Length, at1, C = new Object() };
        object at3 = new { a = '1', b = at1 };

        PrintFields(at1.GetType());
        PrintFields(at2.GetType());
        PrintFields(at3.GetType());
    }
    
    static void PrintFields(Type type)
    {
        Console.WriteLine(type.Name + "": "");
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
        {
            Console.Write(""  "");
            Console.Write(field.Attributes);
            Console.Write("" "");
            Console.Write(field.FieldType.Name);
            Console.Write("" "");
            Console.Write(field.Name);
            Console.WriteLine();
        }
    }
}
";
            CompileAndVerify(
                source,
                symbolValidator: module => TestAnonymousTypeSymbols(
                                               module,
                                               new TypeDescr() { FieldNames = new string[] { "a", "b" } },
                                               new TypeDescr() { FieldNames = new string[] { "Length", "at1", "C" } }
                                           ),
                expectedOutput: @"
<>f__AnonymousType0`2: 
  Private, InitOnly Int32 <a>i__Field
  Private, InitOnly Int32 <b>i__Field
<>f__AnonymousType1`3: 
  Private, InitOnly Int32 <Length>i__Field
  Private, InitOnly <>f__AnonymousType0`2 <at1>i__Field
  Private, InitOnly Object <C>i__Field
<>f__AnonymousType0`2: 
  Private, InitOnly Char <a>i__Field
  Private, InitOnly <>f__AnonymousType0`2 <b>i__Field
"
            ).VerifyIL(
                "<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>..ctor(<Length>j__TPar, <at1>j__TPar, <C>j__TPar)",
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""<Length>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<Length>i__Field""
  IL_000d:  ldarg.0
  IL_000e:  ldarg.2
  IL_000f:  stfld      ""<at1>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<at1>i__Field""
  IL_0014:  ldarg.0
  IL_0015:  ldarg.3
  IL_0016:  stfld      ""<C>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<C>i__Field""
  IL_001b:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.Length.get",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<Length>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<Length>i__Field""
  IL_0006:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.at1.get",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<at1>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<at1>i__Field""
  IL_0006:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.C.get",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<C>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<C>i__Field""
  IL_0006:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.Equals",
@"{
  // Code size       83 (0x53)
  .maxstack  3
  .locals init (<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar> V_0)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0051
  IL_000a:  call       ""System.Collections.Generic.EqualityComparer<<Length>j__TPar> System.Collections.Generic.EqualityComparer<<Length>j__TPar>.Default.get""
  IL_000f:  ldarg.0
  IL_0010:  ldfld      ""<Length>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<Length>i__Field""
  IL_0015:  ldloc.0
  IL_0016:  ldfld      ""<Length>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<Length>i__Field""
  IL_001b:  callvirt   ""bool System.Collections.Generic.EqualityComparer<<Length>j__TPar>.Equals(<Length>j__TPar, <Length>j__TPar)""
  IL_0020:  brfalse.s  IL_0051
  IL_0022:  call       ""System.Collections.Generic.EqualityComparer<<at1>j__TPar> System.Collections.Generic.EqualityComparer<<at1>j__TPar>.Default.get""
  IL_0027:  ldarg.0
  IL_0028:  ldfld      ""<at1>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<at1>i__Field""
  IL_002d:  ldloc.0
  IL_002e:  ldfld      ""<at1>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<at1>i__Field""
  IL_0033:  callvirt   ""bool System.Collections.Generic.EqualityComparer<<at1>j__TPar>.Equals(<at1>j__TPar, <at1>j__TPar)""
  IL_0038:  brfalse.s  IL_0051
  IL_003a:  call       ""System.Collections.Generic.EqualityComparer<<C>j__TPar> System.Collections.Generic.EqualityComparer<<C>j__TPar>.Default.get""
  IL_003f:  ldarg.0
  IL_0040:  ldfld      ""<C>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<C>i__Field""
  IL_0045:  ldloc.0
  IL_0046:  ldfld      ""<C>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<C>i__Field""
  IL_004b:  callvirt   ""bool System.Collections.Generic.EqualityComparer<<C>j__TPar>.Equals(<C>j__TPar, <C>j__TPar)""
  IL_0050:  ret
  IL_0051:  ldc.i4.0
  IL_0052:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.GetHashCode",
@"{
  // Code size       75 (0x4b)
  .maxstack  3
  IL_0000:  ldc.i4     " + GetHashCodeInitialValue("<Length>i__Field", "<at1>i__Field", "<C>i__Field") + @"
  IL_0005:  ldc.i4     0xa5555529
  IL_000a:  mul
  IL_000b:  call       ""System.Collections.Generic.EqualityComparer<<Length>j__TPar> System.Collections.Generic.EqualityComparer<<Length>j__TPar>.Default.get""
  IL_0010:  ldarg.0
  IL_0011:  ldfld      ""<Length>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<Length>i__Field""
  IL_0016:  callvirt   ""int System.Collections.Generic.EqualityComparer<<Length>j__TPar>.GetHashCode(<Length>j__TPar)""
  IL_001b:  add
  IL_001c:  ldc.i4     0xa5555529
  IL_0021:  mul
  IL_0022:  call       ""System.Collections.Generic.EqualityComparer<<at1>j__TPar> System.Collections.Generic.EqualityComparer<<at1>j__TPar>.Default.get""
  IL_0027:  ldarg.0
  IL_0028:  ldfld      ""<at1>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<at1>i__Field""
  IL_002d:  callvirt   ""int System.Collections.Generic.EqualityComparer<<at1>j__TPar>.GetHashCode(<at1>j__TPar)""
  IL_0032:  add
  IL_0033:  ldc.i4     0xa5555529
  IL_0038:  mul
  IL_0039:  call       ""System.Collections.Generic.EqualityComparer<<C>j__TPar> System.Collections.Generic.EqualityComparer<<C>j__TPar>.Default.get""
  IL_003e:  ldarg.0
  IL_003f:  ldfld      ""<C>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<C>i__Field""
  IL_0044:  callvirt   ""int System.Collections.Generic.EqualityComparer<<C>j__TPar>.GetHashCode(<C>j__TPar)""
  IL_0049:  add
  IL_004a:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.ToString",
@"{
  // Code size      199 (0xc7)
  .maxstack  7
  .locals init (<Length>j__TPar V_0,
                <Length>j__TPar V_1,
                <at1>j__TPar V_2,
                <at1>j__TPar V_3,
                <C>j__TPar V_4,
                <C>j__TPar V_5)
  IL_0000:  ldnull
  IL_0001:  ldstr      ""{{ Length = {0}, at1 = {1}, C = {2} }}""
  IL_0006:  ldc.i4.3
  IL_0007:  newarr     ""object""
  IL_000c:  dup
  IL_000d:  ldc.i4.0
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""<Length>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<Length>i__Field""
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  ldloca.s   V_1
  IL_0019:  initobj    ""<Length>j__TPar""
  IL_001f:  ldloc.1
  IL_0020:  box        ""<Length>j__TPar""
  IL_0025:  brtrue.s   IL_003b
  IL_0027:  ldobj      ""<Length>j__TPar""
  IL_002c:  stloc.1
  IL_002d:  ldloca.s   V_1
  IL_002f:  ldloc.1
  IL_0030:  box        ""<Length>j__TPar""
  IL_0035:  brtrue.s   IL_003b
  IL_0037:  pop
  IL_0038:  ldnull
  IL_0039:  br.s       IL_0046
  IL_003b:  constrained. ""<Length>j__TPar""
  IL_0041:  callvirt   ""string object.ToString()""
  IL_0046:  stelem.ref
  IL_0047:  dup
  IL_0048:  ldc.i4.1
  IL_0049:  ldarg.0
  IL_004a:  ldfld      ""<at1>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<at1>i__Field""
  IL_004f:  stloc.2
  IL_0050:  ldloca.s   V_2
  IL_0052:  ldloca.s   V_3
  IL_0054:  initobj    ""<at1>j__TPar""
  IL_005a:  ldloc.3
  IL_005b:  box        ""<at1>j__TPar""
  IL_0060:  brtrue.s   IL_0076
  IL_0062:  ldobj      ""<at1>j__TPar""
  IL_0067:  stloc.3
  IL_0068:  ldloca.s   V_3
  IL_006a:  ldloc.3
  IL_006b:  box        ""<at1>j__TPar""
  IL_0070:  brtrue.s   IL_0076
  IL_0072:  pop
  IL_0073:  ldnull
  IL_0074:  br.s       IL_0081
  IL_0076:  constrained. ""<at1>j__TPar""
  IL_007c:  callvirt   ""string object.ToString()""
  IL_0081:  stelem.ref
  IL_0082:  dup
  IL_0083:  ldc.i4.2
  IL_0084:  ldarg.0
  IL_0085:  ldfld      ""<C>j__TPar <>f__AnonymousType1<<Length>j__TPar, <at1>j__TPar, <C>j__TPar>.<C>i__Field""
  IL_008a:  stloc.s    V_4
  IL_008c:  ldloca.s   V_4
  IL_008e:  ldloca.s   V_5
  IL_0090:  initobj    ""<C>j__TPar""
  IL_0096:  ldloc.s    V_5
  IL_0098:  box        ""<C>j__TPar""
  IL_009d:  brtrue.s   IL_00b5
  IL_009f:  ldobj      ""<C>j__TPar""
  IL_00a4:  stloc.s    V_5
  IL_00a6:  ldloca.s   V_5
  IL_00a8:  ldloc.s    V_5
  IL_00aa:  box        ""<C>j__TPar""
  IL_00af:  brtrue.s   IL_00b5
  IL_00b1:  pop
  IL_00b2:  ldnull
  IL_00b3:  br.s       IL_00c0
  IL_00b5:  constrained. ""<C>j__TPar""
  IL_00bb:  callvirt   ""string object.ToString()""
  IL_00c0:  stelem.ref
  IL_00c1:  call       ""string string.Format(System.IFormatProvider, string, params object[])""
  IL_00c6:  ret
}"
            );
        }

        [Fact]
        public void AnonymousTypeSymbol_Simple_Threadsafety()
        {
            var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        var at1 = new { a = 1, b = 2 };
        var at2 = new { a = 1, b = 2, c = 3};
        var at3 = new { a = 1, b = 2, c = 3, d = 4};
        var at4 = new { a = 1, b = 2, c = 3, d = 4, e = 5};
        var at5 = new { a = 1, b = 2, c = 3, d = 4, e = 5, f = 6};
        var at6 = new { a = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7};
        var at7 = new { a = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8};
        var at8 = new { a = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9};
        var at9 = new { a = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9, k = 10};

        var at11 = new { aa = 1, b = 2 };
        var at12 = new { aa = 1, b = 2, c = 3};
        var at13 = new { aa = 1, b = 2, c = 3, d = 4};
        var at14 = new { aa = 1, b = 2, c = 3, d = 4, e = 5};
        var at15 = new { aa = 1, b = 2, c = 3, d = 4, e = 5, f = 6};
        var at16 = new { aa = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7};
        var at17 = new { aa = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8};
        var at18 = new { aa = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9};
        var at19 = new { aa = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9, k = 10};

        var at21 = new { ba = 1, b = 2 };
        var at22 = new { ba = 1, b = 2, c = 3};
        var at23 = new { ba = 1, b = 2, c = 3, d = 4};
        var at24 = new { ba = 1, b = 2, c = 3, d = 4, e = 5};
        var at25 = new { ba = 1, b = 2, c = 3, d = 4, e = 5, f = 6};
        var at26 = new { ba = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7};
        var at27 = new { ba = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8};
        var at28 = new { ba = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9};
        var at29 = new { ba = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9, k = 10};

        var at31 = new { ca = 1, b = 2 };
        var at32 = new { ca = 1, b = 2, c = 3};
        var at33 = new { ca = 1, b = 2, c = 3, d = 4};
        var at34 = new { ca = 1, b = 2, c = 3, d = 4, e = 5};
        var at35 = new { ca = 1, b = 2, c = 3, d = 4, e = 5, f = 6};
        var at36 = new { ca = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7};
        var at37 = new { ca = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8};
        var at38 = new { ca = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9};
        var at39 = new { ca = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9, k = 10};

        var at41 = new { da = 1, b = 2 };
        var at42 = new { da = 1, b = 2, c = 3};
        var at43 = new { da = 1, b = 2, c = 3, d = 4};
        var at44 = new { da = 1, b = 2, c = 3, d = 4, e = 5};
        var at45 = new { da = 1, b = 2, c = 3, d = 4, e = 5, f = 6};
        var at46 = new { da = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7};
        var at47 = new { da = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8};
        var at48 = new { da = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9};
        var at49 = new { da = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, j = 9, k = 10};


        PrintFields(at1.GetType());
        PrintFields(at2.GetType());
        PrintFields(at3.GetType());
    }
    
    static void PrintFields(Type type)
    {
        Console.WriteLine(type.Name + "": "");
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
        {
            Console.Write(""  "");
            Console.Write(field.Attributes);
            Console.Write("" "");
            Console.Write(field.FieldType.Name);
            Console.Write("" "");
            Console.Write(field.Name);
            Console.WriteLine();
        }
    }
}
";
            for (int i = 0; i < 100; i++)
            {
                var compilation = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);

                var tasks = new Task[10];
                for (int j = 0; j < tasks.Length; j++)
                {
                    var metadataOnly = j % 2 == 0;
                    tasks[j] = Task.Run(() =>
                    {
                        var stream = new MemoryStream();
                        var result = compilation.Emit(stream, options: new EmitOptions(metadataOnly: metadataOnly));
                        result.Diagnostics.Verify();
                    });
                }

                // this should not fail. if you ever see a NRE or some kind of crash here enter a bug.
                // it may be reproducing just once in a while, in Release only... 
                // it is still a bug.
                Task.WaitAll(tasks);
            }
        }

        [ClrOnlyFact]
        public void AnonymousTypeSymbol_Empty()
        {
            var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        var at1 = new { };
        var at2 = new { };

        at2 = at1;

        string x = at1.ToString() + at2.ToString();

        PrintFields(at1.GetType());
    }
    
    static void PrintFields(Type type)
    {
        Console.WriteLine(type.Name + "": "");
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
        {
            Console.Write(""  "");
            Console.Write(field.Attributes);
            Console.Write("" "");
            Console.Write(field.FieldType.Name);
            Console.Write("" "");
            Console.Write(field.Name);
            Console.WriteLine();
        }
    }
}
";
            CompileAndVerify(
                source,
                symbolValidator: module => TestAnonymousTypeSymbols(
                                               module,
                                               new TypeDescr() { FieldNames = new string[] { } }
                                           ),
                expectedOutput: @"
<>f__AnonymousType0:
"
            ).VerifyIL(
                "<>f__AnonymousType0..ctor()",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0.Equals",
@"{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""<>f__AnonymousType0""
  IL_0006:  ldnull
  IL_0007:  cgt.un
  IL_0009:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0.GetHashCode",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0.ToString",
@"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldstr      ""{ }""
  IL_0005:  ret
}"
            );
        }

        [WorkItem(543022, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543022")]
        [ClrOnlyFact]
        public void AnonymousTypeSymbol_StandardNames()
        {
            var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        var at1 = new { GetHashCode = new { } };
        Console.Write(at1.GetType());
        Console.Write(""-"");
        Console.Write(at1.GetHashCode.GetType());
    }
}
";
            CompileAndVerify(
                source,
                expectedOutput: "<>f__AnonymousType0`1[<>f__AnonymousType1]-<>f__AnonymousType1");
        }

        [WorkItem(543022, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543022")]
        [ClrOnlyFact]
        public void AnonymousTypeSymbol_StandardNames2()
        {
            var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        var at1 = new { ToString = ""Field"" };
        Console.Write(at1.ToString());
        Console.Write(""-"");
        Console.Write(at1.ToString);
    }
}
";
            CompileAndVerify(
                source,
                expectedOutput: "{ ToString = Field }-Field");
        }

        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void AnonymousTypeSymbol_StandardNames3()
        {
            var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        var at1 = new { ToString = 1, Equals = new Object(), GetHashCode = ""GetHashCode"" };
        PrintFields(at1.GetType());
    }
    
    static void PrintFields(Type type)
    {
        Console.WriteLine(type.Name + "": "");
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
        {
            Console.Write(""  "");
            Console.Write(field.Attributes);
            Console.Write("" "");
            Console.Write(field.FieldType.Name);
            Console.Write("" "");
            Console.Write(field.Name);
            Console.WriteLine();
        }
    }
}
";
            CompileAndVerify(
                source,
                symbolValidator: module => TestAnonymousTypeSymbols(
                                               module,
                                               new TypeDescr() { FieldNames = new string[] { "ToString", "Equals", "GetHashCode" } }
                                           ),
                expectedOutput: @"
<>f__AnonymousType0`3: 
  Private, InitOnly Int32 <ToString>i__Field
  Private, InitOnly Object <Equals>i__Field
  Private, InitOnly String <GetHashCode>i__Field
"
            ).VerifyIL(
                "<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>..ctor",
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""<ToString>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<ToString>i__Field""
  IL_000d:  ldarg.0
  IL_000e:  ldarg.2
  IL_000f:  stfld      ""<Equals>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<Equals>i__Field""
  IL_0014:  ldarg.0
  IL_0015:  ldarg.3
  IL_0016:  stfld      ""<GetHashCode>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<GetHashCode>i__Field""
  IL_001b:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.Equals",
@"{
  // Code size       83 (0x53)
  .maxstack  3
  .locals init (<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar> V_0)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0051
  IL_000a:  call       ""System.Collections.Generic.EqualityComparer<<ToString>j__TPar> System.Collections.Generic.EqualityComparer<<ToString>j__TPar>.Default.get""
  IL_000f:  ldarg.0
  IL_0010:  ldfld      ""<ToString>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<ToString>i__Field""
  IL_0015:  ldloc.0
  IL_0016:  ldfld      ""<ToString>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<ToString>i__Field""
  IL_001b:  callvirt   ""bool System.Collections.Generic.EqualityComparer<<ToString>j__TPar>.Equals(<ToString>j__TPar, <ToString>j__TPar)""
  IL_0020:  brfalse.s  IL_0051
  IL_0022:  call       ""System.Collections.Generic.EqualityComparer<<Equals>j__TPar> System.Collections.Generic.EqualityComparer<<Equals>j__TPar>.Default.get""
  IL_0027:  ldarg.0
  IL_0028:  ldfld      ""<Equals>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<Equals>i__Field""
  IL_002d:  ldloc.0
  IL_002e:  ldfld      ""<Equals>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<Equals>i__Field""
  IL_0033:  callvirt   ""bool System.Collections.Generic.EqualityComparer<<Equals>j__TPar>.Equals(<Equals>j__TPar, <Equals>j__TPar)""
  IL_0038:  brfalse.s  IL_0051
  IL_003a:  call       ""System.Collections.Generic.EqualityComparer<<GetHashCode>j__TPar> System.Collections.Generic.EqualityComparer<<GetHashCode>j__TPar>.Default.get""
  IL_003f:  ldarg.0
  IL_0040:  ldfld      ""<GetHashCode>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<GetHashCode>i__Field""
  IL_0045:  ldloc.0
  IL_0046:  ldfld      ""<GetHashCode>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<GetHashCode>i__Field""
  IL_004b:  callvirt   ""bool System.Collections.Generic.EqualityComparer<<GetHashCode>j__TPar>.Equals(<GetHashCode>j__TPar, <GetHashCode>j__TPar)""
  IL_0050:  ret
  IL_0051:  ldc.i4.0
  IL_0052:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.GetHashCode",
@"{
  // Code size       75 (0x4b)
  .maxstack  3
" +
  (IntPtr.Size == 4 ?
    "  IL_0000:  ldc.i4     0x78ce6eb1" :
    "  IL_0000:  ldc.i4     0x983c2cef") +
@"
  IL_0005:  ldc.i4     0xa5555529
  IL_000a:  mul
  IL_000b:  call       ""System.Collections.Generic.EqualityComparer<<ToString>j__TPar> System.Collections.Generic.EqualityComparer<<ToString>j__TPar>.Default.get""
  IL_0010:  ldarg.0
  IL_0011:  ldfld      ""<ToString>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<ToString>i__Field""
  IL_0016:  callvirt   ""int System.Collections.Generic.EqualityComparer<<ToString>j__TPar>.GetHashCode(<ToString>j__TPar)""
  IL_001b:  add
  IL_001c:  ldc.i4     0xa5555529
  IL_0021:  mul
  IL_0022:  call       ""System.Collections.Generic.EqualityComparer<<Equals>j__TPar> System.Collections.Generic.EqualityComparer<<Equals>j__TPar>.Default.get""
  IL_0027:  ldarg.0
  IL_0028:  ldfld      ""<Equals>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<Equals>i__Field""
  IL_002d:  callvirt   ""int System.Collections.Generic.EqualityComparer<<Equals>j__TPar>.GetHashCode(<Equals>j__TPar)""
  IL_0032:  add
  IL_0033:  ldc.i4     0xa5555529
  IL_0038:  mul
  IL_0039:  call       ""System.Collections.Generic.EqualityComparer<<GetHashCode>j__TPar> System.Collections.Generic.EqualityComparer<<GetHashCode>j__TPar>.Default.get""
  IL_003e:  ldarg.0
  IL_003f:  ldfld      ""<GetHashCode>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<GetHashCode>i__Field""
  IL_0044:  callvirt   ""int System.Collections.Generic.EqualityComparer<<GetHashCode>j__TPar>.GetHashCode(<GetHashCode>j__TPar)""
  IL_0049:  add
  IL_004a:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.ToString",
@"{
  // Code size      199 (0xc7)
  .maxstack  7
  .locals init (<ToString>j__TPar V_0,
                <ToString>j__TPar V_1,
                <Equals>j__TPar V_2,
                <Equals>j__TPar V_3,
                <GetHashCode>j__TPar V_4,
                <GetHashCode>j__TPar V_5)
  IL_0000:  ldnull
  IL_0001:  ldstr      ""{{ ToString = {0}, Equals = {1}, GetHashCode = {2} }}""
  IL_0006:  ldc.i4.3
  IL_0007:  newarr     ""object""
  IL_000c:  dup
  IL_000d:  ldc.i4.0
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""<ToString>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<ToString>i__Field""
  IL_0014:  stloc.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  ldloca.s   V_1
  IL_0019:  initobj    ""<ToString>j__TPar""
  IL_001f:  ldloc.1
  IL_0020:  box        ""<ToString>j__TPar""
  IL_0025:  brtrue.s   IL_003b
  IL_0027:  ldobj      ""<ToString>j__TPar""
  IL_002c:  stloc.1
  IL_002d:  ldloca.s   V_1
  IL_002f:  ldloc.1
  IL_0030:  box        ""<ToString>j__TPar""
  IL_0035:  brtrue.s   IL_003b
  IL_0037:  pop
  IL_0038:  ldnull
  IL_0039:  br.s       IL_0046
  IL_003b:  constrained. ""<ToString>j__TPar""
  IL_0041:  callvirt   ""string object.ToString()""
  IL_0046:  stelem.ref
  IL_0047:  dup
  IL_0048:  ldc.i4.1
  IL_0049:  ldarg.0
  IL_004a:  ldfld      ""<Equals>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<Equals>i__Field""
  IL_004f:  stloc.2
  IL_0050:  ldloca.s   V_2
  IL_0052:  ldloca.s   V_3
  IL_0054:  initobj    ""<Equals>j__TPar""
  IL_005a:  ldloc.3
  IL_005b:  box        ""<Equals>j__TPar""
  IL_0060:  brtrue.s   IL_0076
  IL_0062:  ldobj      ""<Equals>j__TPar""
  IL_0067:  stloc.3
  IL_0068:  ldloca.s   V_3
  IL_006a:  ldloc.3
  IL_006b:  box        ""<Equals>j__TPar""
  IL_0070:  brtrue.s   IL_0076
  IL_0072:  pop
  IL_0073:  ldnull
  IL_0074:  br.s       IL_0081
  IL_0076:  constrained. ""<Equals>j__TPar""
  IL_007c:  callvirt   ""string object.ToString()""
  IL_0081:  stelem.ref
  IL_0082:  dup
  IL_0083:  ldc.i4.2
  IL_0084:  ldarg.0
  IL_0085:  ldfld      ""<GetHashCode>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<GetHashCode>i__Field""
  IL_008a:  stloc.s    V_4
  IL_008c:  ldloca.s   V_4
  IL_008e:  ldloca.s   V_5
  IL_0090:  initobj    ""<GetHashCode>j__TPar""
  IL_0096:  ldloc.s    V_5
  IL_0098:  box        ""<GetHashCode>j__TPar""
  IL_009d:  brtrue.s   IL_00b5
  IL_009f:  ldobj      ""<GetHashCode>j__TPar""
  IL_00a4:  stloc.s    V_5
  IL_00a6:  ldloca.s   V_5
  IL_00a8:  ldloc.s    V_5
  IL_00aa:  box        ""<GetHashCode>j__TPar""
  IL_00af:  brtrue.s   IL_00b5
  IL_00b1:  pop
  IL_00b2:  ldnull
  IL_00b3:  br.s       IL_00c0
  IL_00b5:  constrained. ""<GetHashCode>j__TPar""
  IL_00bb:  callvirt   ""string object.ToString()""
  IL_00c0:  stelem.ref
  IL_00c1:  call       ""string string.Format(System.IFormatProvider, string, params object[])""
  IL_00c6:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.ToString.get",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<ToString>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<ToString>i__Field""
  IL_0006:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.Equals.get",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<Equals>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<Equals>i__Field""
  IL_0006:  ret
}"
            ).VerifyIL(
                "<>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.GetHashCode.get",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<GetHashCode>j__TPar <>f__AnonymousType0<<ToString>j__TPar, <Equals>j__TPar, <GetHashCode>j__TPar>.<GetHashCode>i__Field""
  IL_0006:  ret
}"
            );
        }

        #region "Utility methods"

        /// <summary>
        /// This method duplicates the generation logic for initial value used 
        /// in anonymous type's GetHashCode function
        /// </summary>
        public static string GetHashCodeInitialValue(params string[] names)
        {
            const int HASH_FACTOR = -1521134295; // (int)0xa5555529
            int init = 0;
            foreach (var name in names)
            {
                init = unchecked(init * HASH_FACTOR + name.GetHashCode());
            }
            return "0x" + init.ToString("X").ToLower();
        }

        private struct TypeDescr
        {
            public string[] FieldNames;
        }

        private void TestAnonymousTypeSymbols(ModuleSymbol module, params TypeDescr[] typeDescrs)
        {
            int cnt = typeDescrs.Length;
            Assert.Equal(0, module.GlobalNamespace.GetMembers("<>f__AnonymousType" + cnt.ToString()).Length);

            //  test template classes
            for (int i = 0; i < cnt; i++)
            {
                TestAnonymousType(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>f__AnonymousType" + i.ToString()), i, typeDescrs[i]);
            }
        }

        private void TestAnonymousType(NamedTypeSymbol type, int typeIndex, TypeDescr typeDescr)
        {
            Assert.NotNull(typeDescr);
            Assert.NotNull(typeDescr.FieldNames);

            //  prepare 
            int fieldsCount = typeDescr.FieldNames.Length;
            string[] genericParameters = new string[fieldsCount];
            string genericParametersList = null;
            for (int i = 0; i < fieldsCount; i++)
            {
                genericParameters[i] = String.Format("<{0}>j__TPar", typeDescr.FieldNames[i]);

                genericParametersList = genericParametersList == null
                                            ? genericParameters[i]
                                            : genericParametersList + ", " + genericParameters[i];
            }
            string genericParametersSuffix = fieldsCount == 0 ? "" : "<" + genericParametersList + ">";

            string typeViewName = String.Format("<>f__AnonymousType{0}{1}", typeIndex.ToString(), genericParametersSuffix);

            //  test
            Assert.Equal(typeViewName, type.ToDisplayString());
            Assert.Equal("object", type.BaseType.ToDisplayString());
            Assert.True(fieldsCount == 0 ? !type.IsGenericType : type.IsGenericType);
            Assert.Equal(fieldsCount, type.Arity);
            Assert.Equal(Accessibility.Internal, type.DeclaredAccessibility);
            Assert.True(type.IsSealed);
            Assert.False(type.IsStatic);
            Assert.Equal(0, type.Interfaces.Length);

            //  test non-existing members
            Assert.Equal(0, type.GetMembers("doesnotexist").Length);

            TestAttributeOnSymbol(
                type,
                new AttributeInfo
                {
                    CtorName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute.CompilerGeneratedAttribute()",
                    ConstructorArguments = new string[] { }
                }
            );

            //  test properties
            for (int i = 0; i < fieldsCount; i++)
            {
                var typeParameter = type.TypeParameters[i];
                Assert.Equal(genericParameters[i], typeParameter.ToDisplayString());
                TestAnonymousTypeProperty(type, typeViewName, typeDescr.FieldNames[i], typeParameter);
            }

            //  methods
            CheckMethodSymbol(
                this.GetMemberByName<MethodSymbol>(type, ".ctor"),
                String.Format("{0}.<>f__AnonymousType{1}({2})", typeViewName, typeIndex.ToString(), genericParametersList),
                "void",
                attr: new AttributeInfo() { CtorName = "System.Diagnostics.DebuggerHiddenAttribute.DebuggerHiddenAttribute()", ConstructorArguments = new string[] { } }
            );
            CheckMethodSymbol(
                this.GetMemberByName<MethodSymbol>(type, "Equals"),
                String.Format("{0}.Equals(object)", typeViewName),
                "bool",
                isVirtualAndOverride: true,
                attr: new AttributeInfo() { CtorName = "System.Diagnostics.DebuggerHiddenAttribute.DebuggerHiddenAttribute()", ConstructorArguments = new string[] { } }
            );
            CheckMethodSymbol(
                this.GetMemberByName<MethodSymbol>(type, "GetHashCode"),
                String.Format("{0}.GetHashCode()", typeViewName),
                "int",
                isVirtualAndOverride: true,
                attr: new AttributeInfo() { CtorName = "System.Diagnostics.DebuggerHiddenAttribute.DebuggerHiddenAttribute()", ConstructorArguments = new string[] { } }
            );
            CheckMethodSymbol(
                this.GetMemberByName<MethodSymbol>(type, "ToString"),
                String.Format("{0}.ToString()", typeViewName),
                "string",
                isVirtualAndOverride: true,
                attr: new AttributeInfo() { CtorName = "System.Diagnostics.DebuggerHiddenAttribute.DebuggerHiddenAttribute()", ConstructorArguments = new string[] { } }
            );
        }

        private T GetMemberByName<T>(NamedTypeSymbol type, string name) where T : Symbol
        {
            foreach (var symbol in type.GetMembers(name))
            {
                if (symbol is T)
                {
                    return (T)symbol;
                }
            }
            return null;
        }

        private void TestAnonymousTypeProperty(NamedTypeSymbol type, string typeViewName, string name, TypeSymbol propType)
        {
            PropertySymbol property = this.GetMemberByName<PropertySymbol>(type, name);
            Assert.NotNull(property);
            Assert.Equal(propType, property.Type);
            Assert.Equal(Accessibility.Public, property.DeclaredAccessibility);
            Assert.False(property.IsAbstract);
            Assert.False(property.IsIndexer);
            Assert.False(property.IsOverride);
            Assert.True(property.IsReadOnly);
            Assert.False(property.IsStatic);
            Assert.False(property.IsSealed);
            Assert.False(property.IsVirtual);
            Assert.False(property.IsWriteOnly);

            var getter = property.GetMethod;
            Assert.NotNull(getter);
            Assert.Equal("get_" + name, getter.Name);
            CheckMethodSymbol(
                getter,
                typeViewName + "." + name + ".get",
                propType.ToDisplayString()
            );
        }

        private void CheckMethodSymbol(
            MethodSymbol method,
            string signature,
            string retType,
            bool isVirtualAndOverride = false,
            AttributeInfo attr = null
        )
        {
            Assert.NotNull(method);
            Assert.Equal(signature, method.ToDisplayString());
            Assert.Equal(Accessibility.Public, method.DeclaredAccessibility);
            Assert.False(method.IsAbstract);
            Assert.Equal(isVirtualAndOverride, method.IsOverride);
            Assert.False(method.IsStatic);
            Assert.False(method.IsSealed);
            Assert.False(method.IsVararg);
            Assert.False(method.IsVirtual);
            Assert.Equal(isVirtualAndOverride, method.IsMetadataVirtual());
            Assert.Equal(retType, method.ReturnType.ToDisplayString());

            TestAttributeOnSymbol(method, attr == null ? new AttributeInfo[] { } : new AttributeInfo[] { attr });
        }

        private class AttributeInfo
        {
            public string CtorName;
            public string[] ConstructorArguments;
        }

        private void TestAttributeOnSymbol(Symbol symbol, params AttributeInfo[] attributes)
        {
            var actual = symbol.GetAttributes();
            Assert.Equal(attributes.Length, actual.Length);

            for (int index = 0; index < attributes.Length; index++)
            {
                var attr = attributes[index];

                Assert.Equal(attr.CtorName, actual[index].AttributeConstructor.ToDisplayString());

                Assert.Equal(attr.ConstructorArguments.Length, actual[index].CommonConstructorArguments.Length);
                for (int argIndex = 0; argIndex < attr.ConstructorArguments.Length; argIndex++)
                {
                    Assert.Equal(
                        attr.ConstructorArguments[argIndex],
                        actual[index].CommonConstructorArguments[argIndex].Value.ToString()
                    );
                }
            }
        }

        #endregion

        [ClrOnlyFact]
        public void AnonymousType_BaseAccess()
        {
            var source = @"
using System;

class Base
{
    protected int field = 123;
}

class Derived: Base
{
    public static void Main(string[] args)
    {
        (new Derived()).Test();
    }
    public void Test()
    {
        var a = new { base.field };
        Console.WriteLine(a.ToString());
    }
}
";
            CompileAndVerify(
                source,
                expectedOutput: "{ field = 123 }");
        }

        [ClrOnlyFact]
        public void AnonymousType_PropertyAccess()
        {
            var source = @"
using System;

class Class1
{
    public static void Main(string[] args)
    {
        var a = new { (new Class1()).PropertyA };
        Console.WriteLine(a.ToString());
    }
    public string PropertyA
    {
        get { return ""pa-value""; }
    }
}
";
            CompileAndVerify(
                source,
                expectedOutput: "{ PropertyA = pa-value }");
        }

        [ClrOnlyFact]
        public void AnonymousType_Simple()
        {
            var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        int a = (new {a = 1, b=""text"", }).a;
        string b = (new {a = 1, b=""text""}).b;
        Console.WriteLine(string.Format(""a={0}; b={1}"", a, b));
    }
}
";
            CompileAndVerify(
                source,
                expectedOutput: "a=1; b=text");
        }

        [ClrOnlyFact]
        public void AnonymousType_CustModifiersOnPropertyFields()
        {
            var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        var intArrMod = Modifiers.F9();
        var at1 = new { f = intArrMod };
        var at2 = new { f = new int[] {} };
        at1 = at2;
        at2 = at1;
    }
}
";
            CompileAndVerify(
                source,
                additionalRefs: new[] { TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll });
        }

        [ClrOnlyFact]
        public void AnonymousType_ToString()
        {
            // test AnonymousType.ToString()
            using (new EnsureInvariantCulture())
            {
                var source = @"
using System;

class Query
{
    public static void Main(string[] args)
    {
        Console.WriteLine((new { a = 1, b=""text"", c=123.456}).ToString());
    }
}
";
                CompileAndVerify(
                    source,
                    expectedOutput: "{ a = 1, b = text, c = 123.456 }");

            }
        }

        [ClrOnlyFact]
        public void AnonymousType_Equals()
        {
            var source = @"
using System;
using System.Collections.Generic;

struct S: IEquatable<S>
{
    public int X;
    public int Y;
    public S(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public override string ToString()
    {
        return string.Format(""({0}, {1})"", this.X, this.Y);
    }

    public bool Equals(S other)
    {
        bool equals = this.X == other.X && this.Y == other.Y;
        this.X = other.X = -1;
        this.Y = other.Y = -1;
        return equals;
    }
}

class Query
{
    public static void Main(string[] args)
    {
        Compare(new { a = new S(1, 2), b = new S(1, 2) }, new { a = new S(1, 2), b = new S(1, 2) });
        Compare(new { a = new S(1, 2), b = new S(1, 2) }, new { b = new S(1, 2), a = new S(1, 2) });
        
        object a = new { a = new S(1, 2) };
        Compare(a, new { a = new S(1, 2) });
        Compare(a, new { a = new S(-1, -1) });

        Compare(new { a = new S(1, 2) }, a);
        Compare(new { a = new S(-1, -1) }, a);
    }
    public static void Compare(object a, object b)
    {
        Console.WriteLine(string.Format(""{0}.Equals({1}) = {2}"", a.ToString(), b.ToString(), a.Equals(b).ToString()));
    }
}";
            CompileAndVerify(
                source,
                expectedOutput: @"
{ a = (1, 2), b = (1, 2) }.Equals({ a = (1, 2), b = (1, 2) }) = True
{ a = (1, 2), b = (1, 2) }.Equals({ b = (1, 2), a = (1, 2) }) = False
{ a = (1, 2) }.Equals({ a = (1, 2) }) = True
{ a = (1, 2) }.Equals({ a = (-1, -1) }) = False
{ a = (1, 2) }.Equals({ a = (1, 2) }) = True
{ a = (-1, -1) }.Equals({ a = (1, 2) }) = False
");
        }

        [ClrOnlyFact]
        public void AnonymousType_GetHashCode()
        {
            var source = @"
using System;

struct S
{
    public int X;
    public int Y;
    public S(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public override bool Equals(object obj)
    {
        throw new Exception();
    }

    public override int GetHashCode()
    {
        return this.X + this.Y;
    }

    public override string ToString()
    {
        return string.Format(""({0}, {1})"", this.X, this.Y);
    }
}

class Query
{
    public static void Main(string[] args)
    {
        Print(new { }, new S(0, 0));
        Print(new { a = new S(2, 2), b = new S(1, 2) }, new { a = new S(4, 0), b = new S(2, 1) });
        Print(new { a = new S(4, 0), b = new S(2, 1) }, new { b = new S(4, 0), a = new S(2, 1) });
    }
    public static void Print(object a, object b)
    {
        Console.WriteLine(string.Format(""{0}.GetHashCode() == {1}.GetHashCode() = {2}"", a.ToString(), b.ToString(), a.GetHashCode() == b.GetHashCode()));
    }
}
";
            CompileAndVerify(
                source,
                expectedOutput: @"
{ }.GetHashCode() == (0, 0).GetHashCode() = True
{ a = (2, 2), b = (1, 2) }.GetHashCode() == { a = (4, 0), b = (2, 1) }.GetHashCode() = True
{ a = (4, 0), b = (2, 1) }.GetHashCode() == { b = (4, 0), a = (2, 1) }.GetHashCode() = False
");
        }

        [ClrOnlyFact]
        public void AnonymousType_MultiplyEmitDoesNotChangeTheOrdering()
        {
            //  this test checks whether or not anonymous types which came from speculative 
            //  semantic API have any effect on the anonymous types emitted and
            //  whether or not the order is still the same across several emits

            var source1 = @"
using System;

class Class2
{
    public static void Main2()
    {
        var d = new { args = 0 };
        var b = new { a = """", b = 1 };
    }
}
";
            var source2 = @"
using System;

class Class1
{
    public static void Main(string[] args)
    {
        var a = new { };
        var b = new { a = """", b = 1 };
        var c = new { b = 1, a = .2 };
        var d = new { args };
    }
}
";
            var source3 = @"
using System;

class Class3
{
    public static void Main2()
    {
        var c = new { b = 1, a = .2 };
        var a = new { };
    }
}
";
            var compilation = (CSharpCompilation)GetCompilationForEmit(new string[] { source1, source2, source3 }, null, TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal), TestOptions.Regular);

            for (int i = 0; i < 10; i++)
            {
                this.CompileAndVerify(
                    compilation,
                    symbolValidator: module =>
                    {
                        var types = module.GlobalNamespace.GetTypeMembers()
                                        .Where(t => t.Name.StartsWith("<>", StringComparison.Ordinal))
                                        .Select(t => t.ToDisplayString())
                                        .OrderBy(t => t)
                                        .ToArray();

                        Assert.Equal(4, types.Length);
                        Assert.Equal("<>f__AnonymousType0<<args>j__TPar>", types[0]);
                        Assert.Equal("<>f__AnonymousType1<<a>j__TPar, <b>j__TPar>", types[1]);
                        Assert.Equal("<>f__AnonymousType2", types[2]);
                        Assert.Equal("<>f__AnonymousType3<<b>j__TPar, <a>j__TPar>", types[3]);
                    },
                    verify: false
                );

                // do some speculative semantic query
                var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
                var position = source1.IndexOf("var d", StringComparison.Ordinal) - 1;
                var expr1 = SyntaxFactory.ParseExpression("new { x = 1, y" + i.ToString() + " = \"---\" }");
                var info1 = model.GetSpeculativeTypeInfo(position, expr1, SpeculativeBindingOption.BindAsExpression);
                Assert.NotNull(info1.Type);
            }
        }

        [WorkItem(543134, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543134")]
        [ClrOnlyFact]
        public void AnonymousTypeSymbol_Simple_1()
        {
            var source = @"
class Test
{
    public static void Main()
    {
        var a = new { };
        var b = new { p1 = 10 };
    }
}
";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void AnonymousTypeSymbol_NamesConflictInsideLambda()
        {
            var source = @"
using System;
class Test
{
    public static void Main()
    {
        M(123);
    }

    public static void M<T>(T p)
    {
        Action a = () => { Console.Write(new { x = 1221, get_x = p }.x.ToString()); };
        a();
    }
}
";
            CompileAndVerify(source, expectedOutput: "1221");
        }

        [WorkItem(543693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543693")]
        [ClrOnlyFact]
        public void Bug11593a()
        {
            var source = @"
delegate T Func<A0, T>(A0 a0);
class Y<U>
{
    public U u;
    public Y(U u)
    {
        this.u = u;
    }
    public Y<T> Select<T>(Func<U, T> selector)
    {
        return new Y<T>(selector(u));
    }
}
class P
{
    static void Main()
    {
        var src = new Y<int>(2);
        var q = from x in src
                let y = x + 3
                select new { X = x, Y = y };
 
        if ((q.u.X != 2 || q.u.Y != 5))
        {
        }
        System.Console.WriteLine(""Success"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "Success");
        }

        [WorkItem(543693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543693")]
        [ClrOnlyFact]
        public void Bug11593b()
        {
            var source = @"
delegate T Func<A0, T>(A0 a0);
class Y<U>
{
    public U u;
    public Y(U u)
    {
        this.u = u;
    }
    public Y<T> Select<T>(Func<U, T> selector)
    {
        return new Y<T>(selector(u));
    }
}
class P
{
    static void Main()
    {
        var xxx = new { X = 1, Y = 2 };

        var src = new Y<int>(2);
        var q = from x in src
                let y = x + 3
                select new { X = x, Y = y };
 
        if ((q.u.X != 2 || q.u.Y != 5))
        {
        }
        System.Console.WriteLine(""Success"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "Success");
        }

        [WorkItem(543177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543177")]
        [ClrOnlyFact]
        public void AnonymousTypePropertyValueWithWarning()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        var a1 = new
        {
            p1 = 0123456789L,
            p2 = 0123456789l, // Warning CS0078
        };

        Console.Write(a1.p1 == a1.p2);
    }
}
";
            CompileAndVerify(source, expectedOutput: "True");
        }

        [Fact(), WorkItem(544323, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544323")]
        public void AnonymousTypeAndMemberSymbolsLocations()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        var an = new  {    id = 1, name = ""QC""    };
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateStandardCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>().Single();

            var sym = model.GetSymbolInfo(expr);
            Assert.NotNull(sym.Symbol);
            Assert.True(((Symbol)sym.Symbol).IsFromCompilation(comp), "IsFromCompilation");
            Assert.False(sym.Symbol.Locations.IsEmpty, "Symbol Location");
            Assert.True(sym.Symbol.Locations[0].IsInSource);

            var info = model.GetTypeInfo(expr);
            Assert.NotNull(info.Type);
            var mems = info.Type.GetMembers();
            foreach (var m in mems)
            {
                Assert.True(((Symbol)m).IsFromCompilation(comp), "IsFromCompilation");
                Assert.False(m.Locations.IsEmpty, String.Format("No Location: {0}", m));
                Assert.True(m.Locations[0].IsInSource);
            }
        }

        [Fact]
        public void SameAnonymousTypeInTwoLocations()
        {
            // This code declares the same anonymous type twice. Make sure the locations
            // reflect this.
            var source = @"
using System;

class Program
{
    static void Main()
    {
        var a1 = new  { id = 1, name = ""QC"" };
        var a2 = new  { id = 1, name = ""QC"" };
        var a3 = a1;
        var a4 = a2;
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateStandardCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var programType = (NamedTypeSymbol)(comp.GlobalNamespace.GetTypeMembers("Program").Single());
            var mainMethod = (MethodSymbol)(programType.GetMembers("Main").Single());
            var mainSyntax = mainMethod.DeclaringSyntaxReferences.Single().GetSyntax() as MethodDeclarationSyntax;
            var mainBlock = mainSyntax.Body;
            var statement1 = mainBlock.Statements[0] as LocalDeclarationStatementSyntax;
            var statement2 = mainBlock.Statements[1] as LocalDeclarationStatementSyntax;
            var statement3 = mainBlock.Statements[2] as LocalDeclarationStatementSyntax;
            var statement4 = mainBlock.Statements[3] as LocalDeclarationStatementSyntax;
            var localA3 = model.GetDeclaredSymbol(statement3.Declaration.Variables[0]) as LocalSymbol;
            var localA4 = model.GetDeclaredSymbol(statement4.Declaration.Variables[0]) as LocalSymbol;
            var typeA3 = localA3.Type;
            var typeA4 = localA4.Type;

            // A3 and A4 should have different type objects, that compare equal. They should have 
            // different locations.
            Assert.Equal(typeA3, typeA4);
            Assert.NotSame(typeA3, typeA4);
            Assert.NotEqual(typeA3.Locations[0], typeA4.Locations[0]);

            // The locations of a3's type should be the type declared in statement 1, the location
            // of a4's type should be the type declared in statement 2.
            Assert.True(statement1.Span.Contains(typeA3.Locations[0].SourceSpan));
            Assert.True(statement2.Span.Contains(typeA4.Locations[0].SourceSpan));
        }

        private static readonly SyntaxTree s_equalityComparerSourceTree = Parse(@"
namespace System.Collections
{
  public interface IEqualityComparer
  {
    bool Equals(object x, object y);
    int GetHashCode(object obj);
  }
}

namespace System.Collections.Generic
{
  public interface IEqualityComparer<T>
  {
    bool Equals(T x, T y);
    int GetHashCode(T obj);
  }

  public abstract class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
  {
      protected EqualityComparer() { }
      public abstract bool Equals(T x, T y);
      public abstract int GetHashCode(T obj);
      bool IEqualityComparer.Equals(object x, object y) { return true; }
      int IEqualityComparer.GetHashCode(object obj) { return 0; }

      // Properties
      public static EqualityComparer<T> Default { get { return null; } }
  }
}
");

        /// <summary>
        /// Bug#15914: Breaking Changes
        /// </summary>
        [Fact, WorkItem(530365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530365")]
        public void NoStdLibNoEmitToStringForAnonymousType()
        {
            var source = @"
class Program
{
    static int Main()
    {
        var t = new { Test = 1 };
        return 0;
    }
}";

            // Dev11: omits methods that are not defined on Object (see also Dev10 bug 487707)
            // Roslyn: we require Equals, ToString, GetHashCode, Format to be defined

            var comp = CreateCompilation(new[] { Parse(source), s_equalityComparerSourceTree }, new[] { MinCorlibRef });
            var result = comp.Emit(new MemoryStream());

            result.Diagnostics.Verify(
                // error CS0656: Missing compiler required member 'System.Object.Equals'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Object", "Equals"),
                // error CS0656: Missing compiler required member 'System.Object.ToString'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Object", "ToString"),
                // error CS0656: Missing compiler required member 'System.Object.GetHashCode'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Object", "GetHashCode"),
                // error CS0656: Missing compiler required member 'System.String.Format'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.String", "Format"));
        }

        [Fact, WorkItem(530365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530365")]
        public void NoDebuggerBrowsableStateType()
        {
            var stateSource = @"
namespace System.Diagnostics
{
    public enum DebuggerBrowsableState
    {
        Collapsed = 2,
        Never = 0,
        RootHidden = 3
    }
}
";
            var stateLib = CreateCompilation(stateSource, new[] { MinCorlibRef });

            var attributeSource = @"
namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false)]
    public sealed class DebuggerBrowsableAttribute : Attribute
    {
        public DebuggerBrowsableAttribute(DebuggerBrowsableState state)
        {
        }
    }
}
";
            var attributeLib = CreateCompilation(attributeSource, new[] { MinCorlibRef, stateLib.ToMetadataReference() });

            var source = @"
class Program
{
    static int Main()
    {
        var t = new { Test = 1 };
        return 0;
    }
}";

            var comp = CreateCompilation(new[] { Parse(source), s_equalityComparerSourceTree }, new[] { MinCorlibRef, attributeLib.ToMetadataReference() });
            var result = comp.Emit(new MemoryStream());

            result.Diagnostics.Verify(
                // error CS0656: Missing compiler required member 'System.Object.Equals'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Object", "Equals"),
                // error CS0656: Missing compiler required member 'System.Object.ToString'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Object", "ToString"),
                // error CS0656: Missing compiler required member 'System.Object.GetHashCode'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Object", "GetHashCode"),
                // error CS0656: Missing compiler required member 'System.String.Format'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.String", "Format"));
        }

        [Fact]
        public void ConditionalAccessErrors()
        {
            var source = @"

class C
{
    int M() { return 0; }

    void Test()
    {
        C local = null;
        C[] array = null;

        var x1 = new { local?.M() };
        var x2 = new { array?[0] };
    }
}
";

            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
                // error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "local?.M()").WithLocation(12, 24),
                // error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "array?[0]").WithLocation(13, 24));
        }

        [ClrOnlyFact]
        [WorkItem(991505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991505")]
        [WorkItem(199, "CodePlex")]
        public void Bug991505()
        {
            var source = @"
class C 
{
    C P { get { return null; } }
    int L { get { return 0; } } 
    int M { get { return 0; } } 
    int N { get { return 0; } } 

    void Test()
    {
        C local = null;
        C[] array = null;

        var x1 = new { local };
        var x2_1 = new { local.P };
        var x2_2 = new { local?.P };
        var x3_1 = new { local.L };
        var x3_2 = new { local?.L };
        var x4_1 = new { local.P.M };
        var x4_2 = new { local?.P.M };
        var x4_3 = new { local?.P?.M };
        var x5_1 = new { array[0].N };
        var x5_2 = new { array?[0].N };
    }
}
";

            CompileAndVerify(
                source,
                symbolValidator: module =>
                    TestAnonymousTypeSymbols(
                        module,
                        new TypeDescr() { FieldNames = new[] { "local" } },
                        new TypeDescr() { FieldNames = new[] { "P" } },
                        new TypeDescr() { FieldNames = new[] { "L" } },
                        new TypeDescr() { FieldNames = new[] { "M" } },
                        new TypeDescr() { FieldNames = new[] { "N" } }));
        }

        [ClrOnlyFact]
        public void CallingCreateAnonymousTypeDoesNotChangeIL()
        {
            var source = @"
class C
{
    public static void Main(string[] args)
    {
        var v = new { m1 = 1, m2 = true };
    }
}";

            var expectedIL = @"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""<>f__AnonymousType0<int, bool>..ctor(int, bool)""
  IL_0007:  pop
  IL_0008:  ret
}";


            CompileAndVerify(source).VerifyIL("C.Main", expectedIL);

            var compilation = GetCompilationForEmit(new[] { source }, additionalRefs: null, options: null, parseOptions: null);
            compilation.CreateAnonymousTypeSymbol(
                ImmutableArray.Create<ITypeSymbol>(compilation.GetSpecialType(SpecialType.System_Int32), compilation.GetSpecialType(SpecialType.System_Boolean)),
                ImmutableArray.Create("m1", "m2"));

            this.CompileAndVerify(compilation).VerifyIL("C.Main", expectedIL);
        }
    }
}
