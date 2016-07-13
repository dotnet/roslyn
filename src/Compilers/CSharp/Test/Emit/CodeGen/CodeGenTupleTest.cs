﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

using static TestResources.NetFX.ValueTuple;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Tuples)]
    public class CodeGenTupleTests : CSharpTestBase
    {
        private static readonly MetadataReference[] s_valueTupleRefs = new[] { SystemRuntimeFacadeRef, ValueTupleRef };

        private static readonly string trivial2uple =
                    @"

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }
    }
}

            ";

        private static readonly string trivial3uple =
                @"

    namespace System
    {
        // struct with two values
        public struct ValueTuple<T1, T2, T3>
        {
            public T1 Item1;
            public T2 Item2;
            public T3 Item3;

            public ValueTuple(T1 item1, T2 item2, T3 item3)
            {
                this.Item1 = item1;
                this.Item2 = item2;
                this.Item3 = item3;
            }

            public override string ToString()
            {
                return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + "", "" + Item3?.ToString() + '}';
            }
        }
    }
        ";

        private static readonly string trivialRemainingTuples = @"
namespace System
{
    public struct ValueTuple<T1>
    {
        public T1 Item1;

        public ValueTuple(T1 item1)
        {
            this.Item1 = item1;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + '}';
        }
    }

    public struct ValueTuple<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4, T5>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
            Rest = rest;
        }

        public override string ToString()
        {
            return base.ToString();
    }
}
}
";

        [Fact]
        public void BadTupleNameMetadata()
        {
            var comp = CreateCompilationWithCustomILSource("",
@"
.assembly extern mscorlib { }
.assembly extern System.ValueTuple
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )
  .ver 4:0:1:0
}

.class public auto ansi C
       extends [mscorlib]System.Object
{
    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> ValidField

    .field public int32 ValidFieldWithAttribute
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[1](""name1"")}
        = ( 01 00 01 00 00 00 05 6E 61 6D 65 31 )

    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> TooFewNames
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[1](""name1"")}
        = ( 01 00 01 00 00 00 05 6E 61 6D 65 31 )

    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> TooManyNames
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[3](""e1"", ""e2"", ""e3"")}
        = ( 01 00 03 00 00 00 02 65 31 02 65 32 02 65 33 )

    // In source, all or no names must be specified for a tuple
    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> MismatchedNames
    .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[2](""e1"", null)}
        = ( 01 00 02 00 00 00 02 65 31 FF )

    .method public hidebysig instance class [System.ValueTuple]System.ValueTuple`2<int32,int32> 
            TooFewNamesMethod() cil managed
    {
      .param [0]
      .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
          // = {string[1](""name1"")}
          = ( 01 00 01 00 00 00 05 6E 61 6D 65 31 )
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  ldc.i4.0
      IL_0002:  newobj     instance void class [System.ValueTuple]System.ValueTuple`2<int32,int32>::.ctor(!0,
                                                                                                          !1)
      IL_0007:  ret
    } // end of method C::TooFewNamesMethod

    .method public hidebysig instance class [System.ValueTuple]System.ValueTuple`2<int32,int32> 
            TooManyNamesMethod() cil managed
    {
      .param [0]
      .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // = {string[3](""e1"", ""e2"", ""e3"")}
           = ( 01 00 03 00 00 00 02 65 31 02 65 32 02 65 33 )
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  ldc.i4.0
      IL_0002:  newobj     instance void class [System.ValueTuple]System.ValueTuple`2<int32,int32>::.ctor(!0,
                                                                                                          !1)
      IL_0007:  ret
    } // end of method C::TooManyNamesMethod

    .method public hidebysig instance void MismatchedNamesMethod(
        class [System.ValueTuple]System.ValueTuple`1<class [System.ValueTuple]System.ValueTuple`2<int32,int32>> c) cil managed
    {
      .param [1]
      .custom instance void [System.ValueTuple]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[])
        // First null is fine (unnamed tuple) but the second is half-named
        // = {string[3](null, ""e1"", null)}
        = ( 01 00 03 00 00 00 FF 02 65 31 FF )
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ret
    } // end of method C::MismatchedNamesMethod
} // end of class C
",
references: s_valueTupleRefs);

            var c = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var validField = c.GetMember<FieldSymbol>("ValidField");
            Assert.False(validField.Type.IsErrorType());
            Assert.True(validField.Type.IsTupleType);
            Assert.True(validField.Type.TupleElementNames.IsDefault);

            var validFieldWithAttribute = c.GetMember<FieldSymbol>("ValidFieldWithAttribute");
            Assert.True(validFieldWithAttribute.Type.IsErrorType());
            Assert.False(validFieldWithAttribute.Type.IsTupleType);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(validFieldWithAttribute.Type);

            var tooFewNames = c.GetMember<FieldSymbol>("TooFewNames");
            Assert.True(tooFewNames.Type.IsErrorType());
            Assert.IsType<UnsupportedMetadataTypeSymbol>(tooFewNames.Type);

            var tooManyNames = c.GetMember<FieldSymbol>("TooManyNames");
            Assert.True(tooManyNames.Type.IsErrorType());
            Assert.IsType<UnsupportedMetadataTypeSymbol>(tooManyNames.Type);

            var mismatchedNames = c.GetMember<FieldSymbol>("MismatchedNames");
            Assert.True(mismatchedNames.Type.IsErrorType());
            Assert.IsType<UnsupportedMetadataTypeSymbol>(mismatchedNames.Type);

            var tooFewNamesMethod = c.GetMember<MethodSymbol>("TooFewNamesMethod");
            Assert.True(tooFewNamesMethod.ReturnType.IsErrorType());
            Assert.IsType<UnsupportedMetadataTypeSymbol>(tooFewNamesMethod.ReturnType);

            var tooManyNamesMethod = c.GetMember<MethodSymbol>("TooManyNamesMethod");
            Assert.True(tooManyNamesMethod.ReturnType.IsErrorType());
            Assert.IsType<UnsupportedMetadataTypeSymbol>(tooManyNamesMethod.ReturnType);

            var mismatchedNamesMethod = c.GetMember<MethodSymbol>("MismatchedNamesMethod");
            var mismatchedParamType = mismatchedNamesMethod.Parameters.Single().Type;
            Assert.True(mismatchedParamType.IsErrorType());
            Assert.IsType<UnsupportedMetadataTypeSymbol>(mismatchedParamType);
        }

        [Fact]
        public void NestedTuplesNoAttribute()
        {
            var comp = CreateCompilationWithCustomILSource("",
@"
.assembly extern mscorlib { }
.assembly extern System.ValueTuple
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )
  .ver 4:0:1:0
}

.class public auto ansi beforefieldinit Base`1<T>
       extends [mscorlib]System.Object
{
}

.class public auto ansi C
       extends [mscorlib]System.Object
{
    .field public class [System.ValueTuple]System.ValueTuple`2<int32, int32> Field1

    .field public class Base`1<class [System.ValueTuple]System.ValueTuple`1<
        class [System.ValueTuple]System.ValueTuple`2<int32, int32>>>  Field2;

} // end of class C
",
references: s_valueTupleRefs);

            var c = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var base1 = comp.GlobalNamespace.GetTypeMember("Base");
            Assert.NotNull(base1);

            var field1 = c.GetMember<FieldSymbol>("Field1");
            Assert.False(field1.Type.IsErrorType());
            Assert.True(field1.Type.IsTupleType);
            Assert.True(field1.Type.TupleElementNames.IsDefault);

            NamedTypeSymbol field2Type = (NamedTypeSymbol)c.GetMember<FieldSymbol>("Field2").Type;
            Assert.Equal(base1, field2Type.OriginalDefinition);
            Assert.True(field2Type.IsGenericType);

            var first = field2Type.TypeArguments[0];
            Assert.True(first.IsTupleType);
            Assert.Equal(1, first.TupleElementTypes.Length);
            Assert.True(first.TupleElementNames.IsDefault);

            var second = first.TupleElementTypes[0];
            Assert.True(second.IsTupleType);
            Assert.True(second.TupleElementNames.IsDefault);
            Assert.Equal(2, second.TupleElementTypes.Length);
            Assert.All(second.TupleElementTypes, t =>
                Assert.Equal(SpecialType.System_Int32, t.SpecialType));
        }

        [Fact]
        public void SimpleTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 2);
        System.Console.WriteLine(x.ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: "{1, 2}");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. ""System.ValueTuple<int, int>""
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  ret
}");
        }

        [Fact]
        public void SimpleTupleNew()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = new (int, int)(1, 2);
        System.Console.WriteLine(x.ToString());

        x = new (int, int)();
        System.Console.WriteLine(x.ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
{1, 2}
{0, 0}
");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. ""System.ValueTuple<int, int>""
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""System.ValueTuple<int, int>""
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. ""System.ValueTuple<int, int>""
  IL_002b:  callvirt   ""string object.ToString()""
  IL_0030:  call       ""void System.Console.WriteLine(string)""
  IL_0035:  ret
}");
        }

        [Fact]
        public void SimpleTupleNew1()
        {
            var source = @"
class C
{
    static void Main()
    {
        dynamic arg = 2;
        var x = new (int, int)(1, arg);
        System.Console.WriteLine(x.ToString());
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
{1, 2}");
            
            verifier.VerifyIL("C.Main", @"
{
  // Code size      129 (0x81)
  .maxstack  7
  .locals init (object V_0, //arg
                System.ValueTuple<int, int> V_1) //x
  IL_0000:  ldc.i4.2
  IL_0001:  box        ""int""
  IL_0006:  stloc.0
  IL_0007:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)>> C.<>o__0.<>p__0""
  IL_000c:  brtrue.s   IL_004d
  IL_000e:  ldc.i4.0
  IL_000f:  ldtoken    ""C""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  ldc.i4.3
  IL_001a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  ldc.i4.s   33
  IL_0023:  ldnull
  IL_0024:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0029:  stelem.ref
  IL_002a:  dup
  IL_002b:  ldc.i4.1
  IL_002c:  ldc.i4.3
  IL_002d:  ldnull
  IL_002e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0033:  stelem.ref
  IL_0034:  dup
  IL_0035:  ldc.i4.2
  IL_0036:  ldc.i4.0
  IL_0037:  ldnull
  IL_0038:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_003d:  stelem.ref
  IL_003e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeConstructor(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0043:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0048:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)>> C.<>o__0.<>p__0""
  IL_004d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)>> C.<>o__0.<>p__0""
  IL_0052:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)>>.Target""
  IL_0057:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)>> C.<>o__0.<>p__0""
  IL_005c:  ldtoken    ""System.ValueTuple<int, int>""
  IL_0061:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0066:  ldc.i4.1
  IL_0067:  ldloc.0
  IL_0068:  callvirt   ""(int, int) System.Func<System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic, (int, int)>.Invoke(System.Runtime.CompilerServices.CallSite, System.Type, int, dynamic)""
  IL_006d:  stloc.1
  IL_006e:  ldloca.s   V_1
  IL_0070:  constrained. ""System.ValueTuple<int, int>""
  IL_0076:  callvirt   ""string object.ToString()""
  IL_007b:  call       ""void System.Console.WriteLine(string)""
  IL_0080:  ret
}");
        }

        [Fact]
        public void SimpleTupleNew2()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = new (int a, int b)(1, 2);
        System.Console.WriteLine(x.a.ToString());

        var x1 = new (int a, int b)(1, 2) { a = 3, Item2 = 4};
        System.Console.WriteLine(x1.a.ToString());

        var x2 = new (int a, (int b, int c) d)(1, new (int, int)(2, 3)) { a = 5, d = {b = 6, c = 7}};
        System.Console.WriteLine(x2.a.ToString());
        System.Console.WriteLine(x2.d.b.ToString());
        System.Console.WriteLine(x2.d.c.ToString());

    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
1
3
5
6
7");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size      184 (0xb8)
  .maxstack  4
  .locals init (System.ValueTuple<int, int> V_0, //x
                System.ValueTuple<int, int> V_1, //x1
                System.ValueTuple<int, (int b, int c)> V_2, //x2
                System.ValueTuple<int, int> V_3,
                System.ValueTuple<int, (int b, int c)> V_4)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_3
  IL_001c:  ldc.i4.1
  IL_001d:  ldc.i4.2
  IL_001e:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0023:  ldloca.s   V_3
  IL_0025:  ldc.i4.3
  IL_0026:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_002b:  ldloca.s   V_3
  IL_002d:  ldc.i4.4
  IL_002e:  stfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0033:  ldloc.3
  IL_0034:  stloc.1
  IL_0035:  ldloca.s   V_1
  IL_0037:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_003c:  call       ""string int.ToString()""
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  ldloca.s   V_4
  IL_0048:  ldc.i4.1
  IL_0049:  ldc.i4.2
  IL_004a:  ldc.i4.3
  IL_004b:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0050:  call       ""System.ValueTuple<int, (int b, int c)>..ctor(int, (int b, int c))""
  IL_0055:  ldloca.s   V_4
  IL_0057:  ldc.i4.5
  IL_0058:  stfld      ""int System.ValueTuple<int, (int b, int c)>.Item1""
  IL_005d:  ldloca.s   V_4
  IL_005f:  ldflda     ""(int b, int c) System.ValueTuple<int, (int b, int c)>.Item2""
  IL_0064:  ldc.i4.6
  IL_0065:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_006a:  ldloca.s   V_4
  IL_006c:  ldflda     ""(int b, int c) System.ValueTuple<int, (int b, int c)>.Item2""
  IL_0071:  ldc.i4.7
  IL_0072:  stfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0077:  ldloc.s    V_4
  IL_0079:  stloc.2
  IL_007a:  ldloca.s   V_2
  IL_007c:  ldflda     ""int System.ValueTuple<int, (int b, int c)>.Item1""
  IL_0081:  call       ""string int.ToString()""
  IL_0086:  call       ""void System.Console.WriteLine(string)""
  IL_008b:  ldloca.s   V_2
  IL_008d:  ldflda     ""(int b, int c) System.ValueTuple<int, (int b, int c)>.Item2""
  IL_0092:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0097:  call       ""string int.ToString()""
  IL_009c:  call       ""void System.Console.WriteLine(string)""
  IL_00a1:  ldloca.s   V_2
  IL_00a3:  ldflda     ""(int b, int c) System.ValueTuple<int, (int b, int c)>.Item2""
  IL_00a8:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_00ad:  call       ""string int.ToString()""
  IL_00b2:  call       ""void System.Console.WriteLine(string)""
  IL_00b7:  ret
}");
        }

        [WorkItem(10874, "https://github.com/dotnet/roslyn/issues/10874")]
        [Fact]
        public void SimpleTupleNew3()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x0 = new (int a, int b)(1, 2, 3);
        System.Console.WriteLine(x0.ToString());

        var x1 = new (int, int)(1, 2, 3);
        System.Console.WriteLine(x1.ToString());

        var x2 = new (int, int)(1, ""2"");
        System.Console.WriteLine(x2.ToString());

        var x3 = new (int, int)(1);
        System.Console.WriteLine(x3.ToString());

        var x4 = new (int, int)(1, 1) {a = 1, Item3 = 2} ;
        System.Console.WriteLine(x3.ToString());

    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,22): error CS1729: '(int a, int b)' does not contain a constructor that takes 3 arguments
                //         var x0 = new (int a, int b)(1, 2, 3);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(int a, int b)").WithArguments("(int a, int b)", "3").WithLocation(6, 22),
                // (9,22): error CS1729: '(int, int)' does not contain a constructor that takes 3 arguments
                //         var x1 = new (int, int)(1, 2, 3);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(int, int)").WithArguments("(int, int)", "3").WithLocation(9, 22),
                // (12,36): error CS1503: Argument 2: cannot convert from 'string' to 'int'
                //         var x2 = new (int, int)(1, "2");
                Diagnostic(ErrorCode.ERR_BadArgType, @"""2""").WithArguments("2", "string", "int").WithLocation(12, 36),
                // (15,22): error CS7036: There is no argument given that corresponds to the required formal parameter 'item2' of '(int, int).(int, int)'
                //         var x3 = new (int, int)(1);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "(int, int)").WithArguments("item2", "(int, int).(int, int)").WithLocation(15, 22),
                // (18,40): error CS0117: '(int, int)' does not contain a definition for 'a'
                //         var x4 = new (int, int)(1, 1) {a = 1, Item3 = 2} ;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "a").WithArguments("(int, int)", "a").WithLocation(18, 40),
                // (18,47): error CS0117: '(int, int)' does not contain a definition for 'Item3'
                //         var x4 = new (int, int)(1, 1) {a = 1, Item3 = 2} ;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Item3").WithArguments("(int, int)", "Item3").WithLocation(18, 47)
                );
        }

        [Fact]
        public void SimpleTuple2()
        {
            var source = @"
class C
{
    static void Main()
    {
        var s = Single((a:1, b:2));
        System.Console.WriteLine(s[0].b.ToString());
    }

    static T[] Single<T>(T x)
    {
        return new T[]{x};
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: "2");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  call       ""(int a, int b)[] C.Single<(int a, int b)>((int a, int b))""
  IL_000c:  ldc.i4.0
  IL_000d:  ldelema    ""System.ValueTuple<int, int>""
  IL_0012:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_0017:  call       ""string int.ToString()""
  IL_001c:  call       ""void System.Console.WriteLine(string)""
  IL_0021:  ret
}");
        }

        [Fact]
        public void SimpleTupleTargetTyped()
        {
            var source = @"
class C
{
    static void Main()
    {
        (object, object) x = (null, null);
        System.Console.WriteLine(x.ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: "{, }");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (System.ValueTuple<object, object> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldnull
  IL_0003:  ldnull
  IL_0004:  call       ""System.ValueTuple<object, object>..ctor(object, object)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  constrained. ""System.ValueTuple<object, object>""
  IL_0011:  callvirt   ""string object.ToString()""
  IL_0016:  call       ""void System.Console.WriteLine(string)""
  IL_001b:  ret
}");
        }

        [Fact]
        public void SimpleTupleNested()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, (2, (3, 4)).ToString());
        System.Console.WriteLine(x.ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: "{1, {2, {3, 4}}}");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  5
  .locals init (System.ValueTuple<int, string> V_0, //x
                System.ValueTuple<int, (int, int)> V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  ldc.i4.4
  IL_0006:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000b:  newobj     ""System.ValueTuple<int, (int, int)>..ctor(int, (int, int))""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  constrained. ""System.ValueTuple<int, (int, int)>""
  IL_0019:  callvirt   ""string object.ToString()""
  IL_001e:  call       ""System.ValueTuple<int, string>..ctor(int, string)""
  IL_0023:  ldloca.s   V_0
  IL_0025:  constrained. ""System.ValueTuple<int, string>""
  IL_002b:  callvirt   ""string object.ToString()""
  IL_0030:  call       ""void System.Console.WriteLine(string)""
  IL_0035:  ret
}");
        }

        [Fact]
        public void TupleUnderlyingItemAccess()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 2);
        System.Console.WriteLine(x.Item2.ToString());
        x.Item1 = 40;
        System.Console.WriteLine(x.Item1 + x.Item2);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_002f:  add
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void TupleUnderlyingItemAccess01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: 2);
        System.Console.WriteLine(x.Item2.ToString());
        x.Item1 = 40;
        System.Console.WriteLine(x.Item1 + x.Item2);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_002f:  add
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void TupleItemAccess()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: 2);
        System.Console.WriteLine(x.b.ToString());
        x.a = 40;
        System.Console.WriteLine(x.a + x.b);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldflda     ""int System.ValueTuple<int, int>.Item2""
  IL_0010:  call       ""string int.ToString()""
  IL_0015:  call       ""void System.Console.WriteLine(string)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.s   40
  IL_001e:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0023:  ldloc.0
  IL_0024:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0029:  ldloc.0
  IL_002a:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_002f:  add
  IL_0030:  call       ""void System.Console.WriteLine(int)""
  IL_0035:  ret
}
");
        }

        [Fact]
        public void TupleItemAccess01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: (c: 2, d: 3));
        System.Console.WriteLine(x.b.c.ToString());
        x.b.d = 39;
        System.Console.WriteLine(x.a + x.b.c + x.b.d);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"2
42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       87 (0x57)
  .maxstack  4
  .locals init (System.ValueTuple<int, (int c, int d)> V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldc.i4.2
  IL_0004:  ldc.i4.3
  IL_0005:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000a:  call       ""System.ValueTuple<int, (int c, int d)>..ctor(int, (int c, int d))""
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldflda     ""(int c, int d) System.ValueTuple<int, (int c, int d)>.Item2""
  IL_0016:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_001b:  call       ""string int.ToString()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  ldflda     ""(int c, int d) System.ValueTuple<int, (int c, int d)>.Item2""
  IL_002c:  ldc.i4.s   39
  IL_002e:  stfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0033:  ldloc.0
  IL_0034:  ldfld      ""int System.ValueTuple<int, (int c, int d)>.Item1""
  IL_0039:  ldloc.0
  IL_003a:  ldfld      ""(int c, int d) System.ValueTuple<int, (int c, int d)>.Item2""
  IL_003f:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0044:  add
  IL_0045:  ldloc.0
  IL_0046:  ldfld      ""(int c, int d) System.ValueTuple<int, (int c, int d)>.Item2""
  IL_004b:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0050:  add
  IL_0051:  call       ""void System.Console.WriteLine(int)""
  IL_0056:  ret
}
");
        }

        [Fact]
        public void TupleTypeDeclaration()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string, int) x = (1, ""hello"", 2);
        System.Console.WriteLine(x.ToString());
    }
}

" + trivial3uple;

            var comp = CompileAndVerify(source, expectedOutput: @"{1, hello, 2}");
        }

        [Fact]
        [WorkItem(11281, "https://github.com/dotnet/roslyn/issues/11281")]
        public void TupleTypeMismatch_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string) x = (1, ""hello"", 2);
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,27): error CS0029: Cannot implicitly convert type '(int, string, int)' to '(int, string)'
                //         (int, string) x = (1, "hello", 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"(1, ""hello"", 2)").WithArguments("(int, string, int)", "(int, string)").WithLocation(6, 27));
        }

        [Fact]
        [WorkItem(11282, "https://github.com/dotnet/roslyn/issues/11282")]
        public void TupleTypeMismatch_02()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string) x = (1, null, 2);
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,27): error CS8206: Tuple with 3 elements cannot be converted to type '(int, string)'.
                //         (int, string) x = (1, null, 2);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, "(1, null, 2)").WithArguments("3", "(int, string)").WithLocation(6, 27)
            );
        }

        [Fact]
        [WorkItem(11283, "https://github.com/dotnet/roslyn/issues/11283")]
        public void LongTupleTypeMismatch()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int, int, int, int, int, int, int) x = (""Alice"", 2, 3, 4, 5, 6, 7, 8);
        (int, int, int, int, int, int, int, int) y = (1, 2, 3, 4, 5, 6, 7, 8, 9);
    }
}
";

            CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }).VerifyDiagnostics(
                // (6,55): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         (int, int, int, int, int, int, int, int) x = ("Alice", 2, 3, 4, 5, 6, 7, 8);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""Alice""").WithArguments("string", "int").WithLocation(6, 55),
                // (7,54): error CS0029: Cannot implicitly convert type '(int, int, int, int, int, int, int, int, int)' to '(int, int, int, int, int, int, int, int)'
                //         (int, int, int, int, int, int, int, int) y = (1, 2, 3, 4, 5, 6, 7, 8, 9);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1, 2, 3, 4, 5, 6, 7, 8, 9)").WithArguments("(int, int, int, int, int, int, int, int, int)", "(int, int, int, int, int, int, int, int)").WithLocation(7, 54)
                );
        }

        [Fact]
        public void TupleTypeWithLateDiscoveredName()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string a) x = (1, ""hello"", c: 2);
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics(
                // (6,9): error CS8204: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (1, "hello", c: 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, "(int, string a)").WithLocation(6, 9),
                // (6,29): error CS8204: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (1, "hello", c: 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, @"(1, ""hello"", c: 2)").WithLocation(6, 29)
                );

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(1, ""hello"", c: 2)", node.ToString());
            Assert.Equal("(System.Int32 Item1, System.String Item2, System.Int32 c)", model.GetTypeInfo(node).Type.ToTestDisplayString());

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            var xSymbol = ((SourceLocalSymbol)model.GetDeclaredSymbol(x)).Type;
            Assert.Equal("(System.Int32 Item1, System.String a)", xSymbol.ToTestDisplayString());
            Assert.True(xSymbol.IsTupleType);

            Assert.Equal(new[] { "System.Int32", "System.String" }, xSymbol.TupleElementTypes.SelectAsArray(t => t.ToTestDisplayString()));
            Assert.Equal(new[] { "Item1", "a" }, xSymbol.TupleElementNames);
        }

        [Fact]
        public void TupleTypeDeclarationWithNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b) x = (1, ""hello"");
        System.Console.WriteLine(x.a.ToString());
        System.Console.WriteLine(x.b.ToString());
    }
}
" + trivial2uple;
            var comp = CompileAndVerify(source, expectedOutput: @"1
hello");
        }

        [Fact]
        public void TupleWithOnlySomeNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string a) x = (b: 1, ""hello"", 2);
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,9): error CS8204: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, "(int, string a)").WithLocation(6, 9),
                // (6,29): error CS8204: Tuple member names must all be provided, if any one is provided.
                //         (int, string a) x = (b: 1, "hello", 2);
                Diagnostic(ErrorCode.ERR_TupleExplicitNamesOnAllMembersOrNone, @"(b: 1, ""hello"", 2)").WithLocation(6, 29)
                );
        }

        [Fact]
        public void TupleDictionary01()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    static void Main()
    {
        var k = (1, 2);
        var v = (a: 1, b: (c: 2, d: (e: 3, f: 4)));

        var d = Test(k, v);

        System.Console.WriteLine(d[(1, 2)].b.d.Item2);
    }

    static Dictionary<K, V> Test<K, V>(K key, V value)
    {
        var d = new Dictionary<K, V>();

        d[key] = value;

        return d;
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"4");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       67 (0x43)
  .maxstack  6
  .locals init (System.ValueTuple<int, (int c, (int e, int f) d)> V_0) //v
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.2
  IL_000b:  ldc.i4.3
  IL_000c:  ldc.i4.4
  IL_000d:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0012:  newobj     ""System.ValueTuple<int, (int e, int f)>..ctor(int, (int e, int f))""
  IL_0017:  call       ""System.ValueTuple<int, (int c, (int e, int f) d)>..ctor(int, (int c, (int e, int f) d))""
  IL_001c:  ldloc.0
  IL_001d:  call       ""System.Collections.Generic.Dictionary<(int, int), (int a, (int c, (int e, int f) d) b)> C.Test<(int, int), (int a, (int c, (int e, int f) d) b)>((int, int), (int a, (int c, (int e, int f) d) b))""
  IL_0022:  ldc.i4.1
  IL_0023:  ldc.i4.2
  IL_0024:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0029:  callvirt   ""(int a, (int c, (int e, int f) d) b) System.Collections.Generic.Dictionary<(int, int), (int a, (int c, (int e, int f) d) b)>.this[(int, int)].get""
  IL_002e:  ldfld      ""(int c, (int e, int f) d) System.ValueTuple<int, (int c, (int e, int f) d)>.Item2""
  IL_0033:  ldfld      ""(int e, int f) System.ValueTuple<int, (int e, int f)>.Item2""
  IL_0038:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  ret
}
");
        }

        [Fact]
        public void TupleLambdaCapture01()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42));
    }

    public static T Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        Func<T> f = () => x.f2;

        return f();
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.<>c__DisplayClass1_0<T>.<Test>b__0()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""(T f1, T f2) C.<>c__DisplayClass1_0<T>.x""
  IL_0006:  ldfld      ""T System.ValueTuple<T, T>.Item2""
  IL_000b:  ret
}
");
        }

        [Fact]
        public void TupleLambdaCapture02()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42));
    }

    public static string Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        Func<string> f = () => x.ToString();

        return f();
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"{42, 42}");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.<>c__DisplayClass1_0<T>.<Test>b__0()", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""(T f1, T f2) C.<>c__DisplayClass1_0<T>.x""
  IL_0006:  constrained. ""System.ValueTuple<T, T>""
  IL_000c:  callvirt   ""string object.ToString()""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void TupleLambdaCapture03()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42));
    }

    public static T Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        Func<T> f = () => x.Test(a);

        return f();
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public U Test<U>(U val)
        {
            return val;
        }
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.<>c__DisplayClass1_0<T>.<Test>b__0()", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""(T f1, T f2) C.<>c__DisplayClass1_0<T>.x""
  IL_0006:  ldarg.0
  IL_0007:  ldfld      ""T C.<>c__DisplayClass1_0<T>.a""
  IL_000c:  call       ""T System.ValueTuple<T, T>.Test<T>(T)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void TupleLambdaCapture04()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42));
    }

    public static T Test<T>(T a)
    {
        var x = (f1: 1, f2: 2);

        Func<T> f = () => x.Test(a);

        return f();
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public U Test<U>(U val)
        {
            return val;
        }
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.<>c__DisplayClass1_0<T>.<Test>b__0()", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""(int f1, int f2) C.<>c__DisplayClass1_0<T>.x""
  IL_0006:  ldarg.0
  IL_0007:  ldfld      ""T C.<>c__DisplayClass1_0<T>.a""
  IL_000c:  call       ""T System.ValueTuple<int, int>.Test<T>(T)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void TupleLambdaCapture05()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42));
    }

    public static T Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        Func<T> f = () => x.P1;

        return f();
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public T1 P1
        {
            get
            {
                return Item1;
            }
        }
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"42");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.<>c__DisplayClass1_0<T>.<Test>b__0()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""(T f1, T f2) C.<>c__DisplayClass1_0<T>.x""
  IL_0006:  call       ""T System.ValueTuple<T, T>.P1.get""
  IL_000b:  ret
}
");
        }

        [Fact]
        public void TupleLambdaCapture06()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var v1 = M1();
        System.Action<int> d1 = (int i1) => System.Console.WriteLine(i1); 
        v1.Test(v1, d1);
        Test(v1);
    }

    static void Test<T1, T2>((T1, T2) v1)
    {
        System.Action f = () =>
        {
            System.Action<T1> d1 = (T1 i1) => System.Console.WriteLine(i1); 
            System.Action<T2> d2 = (T2 i2) => System.Console.WriteLine(i2); 
            v1.E1 += d1;
            v1.RaiseE1();
            v1.E1 -= d1;
            v1.RaiseE1();
            v1.E2 += d2;
            v1.RaiseE2();
            v1.E2 -= d2;
            v1.RaiseE2();   
        };

        f();
    }

    static (int, long) M1()
    {
        return (1, 11);
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.E1 = null;
            this._e2 = null;
        }

        public event System.Action<T1> E1;

        private System.Action<T2> _e2;
        public event System.Action<T2> E2
        {
            add
            {
                _e2 += value;
            }
            remove
            {
                _e2 -= value;
            }
        }

        public void RaiseE1()
        {
            System.Console.WriteLine(""-"");
            if (E1 == null)
            {
                System.Console.WriteLine(""null"");
            }
            else
            {
                E1(Item1);
            }
            System.Console.WriteLine(""-"");
        }

        public void RaiseE2()
        {
            System.Console.WriteLine(""--"");
            if (_e2 == null)
            {
                System.Console.WriteLine(""null"");
            }
            else
            {
                _e2(Item2);
            }
            System.Console.WriteLine(""--"");
        }

        public void Test<S1, S2>((S1, S2) val, System.Action<S1> d)
        {
            System.Action f= () =>
            {
                val.E1 += d;
                System.Console.WriteLine(val.E1);
                val.E1(val.Item1);
                val.E1 -= d;
                System.Console.WriteLine(val.E1 == null);
            };
    
            f();
        }
    }
}
" + tupleattributes_cs;

            var comp = CompileAndVerify(source, expectedOutput:
@"System.Action`1[System.Int32]
1
True
-
1
-
-
null
-
--
11
--
--
null
--");
        }

        [Fact]
        public void TupleAsyncCapture01()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42).Result);
    }

    public static async Task<T> Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        await Task.Yield();

        return x.f1;
    }
}
" + trivial2uple;
            var verifier = CompileAndVerify(source, additionalRefs: new[] { MscorlibRef_v46 }, expectedOutput: @"42", options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.<Test>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      191 (0xbf)
  .maxstack  3
  .locals init (int V_0,
                T V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0058
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0017:  newobj     ""System.ValueTuple<T, T>..ctor(T, T)""
    IL_001c:  stfld      ""(T f1, T f2) C.<Test>d__1<T>.<x>5__1""
    IL_0021:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_002e:  stloc.2
    IL_002f:  ldloca.s   V_2
    IL_0031:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0036:  brtrue.s   IL_0074
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.2
    IL_0043:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
    IL_004e:  ldloca.s   V_2
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<Test>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<Test>d__1<T>)""
    IL_0056:  leave.s    IL_00be
    IL_0058:  ldarg.0
    IL_0059:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_005e:  stloc.2
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0065:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.m1
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_007b:  ldloca.s   V_2
    IL_007d:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0083:  ldarg.0
    IL_0084:  ldflda     ""(T f1, T f2) C.<Test>d__1<T>.<x>5__1""
    IL_0089:  ldfld      ""T System.ValueTuple<T, T>.Item1""
    IL_008e:  stloc.1
    IL_008f:  leave.s    IL_00aa
  }
  catch System.Exception
  {
    IL_0091:  stloc.s    V_4
    IL_0093:  ldarg.0
    IL_0094:  ldc.i4.s   -2
    IL_0096:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_009b:  ldarg.0
    IL_009c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
    IL_00a1:  ldloc.s    V_4
    IL_00a3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetException(System.Exception)""
    IL_00a8:  leave.s    IL_00be
  }
  IL_00aa:  ldarg.0
  IL_00ab:  ldc.i4.s   -2
  IL_00ad:  stfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_00b2:  ldarg.0
  IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
  IL_00b8:  ldloc.1
  IL_00b9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetResult(T)""
  IL_00be:  ret
}
");
        }

        [Fact]
        public void TupleAsyncCapture02()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42).Result);
    }

    public static async Task<string> Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        await Task.Yield();

        return x.ToString();
    }
}
" + trivial2uple;
            var verifier = CompileAndVerify(source, additionalRefs: new[] { MscorlibRef_v46 }, expectedOutput: @"{42, 42}", options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.<Test>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      197 (0xc5)
  .maxstack  3
  .locals init (int V_0,
                string V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0058
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0017:  newobj     ""System.ValueTuple<T, T>..ctor(T, T)""
    IL_001c:  stfld      ""(T f1, T f2) C.<Test>d__1<T>.<x>5__1""
    IL_0021:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_002e:  stloc.2
    IL_002f:  ldloca.s   V_2
    IL_0031:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0036:  brtrue.s   IL_0074
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.2
    IL_0043:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<Test>d__1<T>.<>t__builder""
    IL_004e:  ldloca.s   V_2
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<Test>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<Test>d__1<T>)""
    IL_0056:  leave.s    IL_00c4
    IL_0058:  ldarg.0
    IL_0059:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_005e:  stloc.2
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0065:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.m1
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_007b:  ldloca.s   V_2
    IL_007d:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0083:  ldarg.0
    IL_0084:  ldflda     ""(T f1, T f2) C.<Test>d__1<T>.<x>5__1""
    IL_0089:  constrained. ""System.ValueTuple<T, T>""
    IL_008f:  callvirt   ""string object.ToString()""
    IL_0094:  stloc.1
    IL_0095:  leave.s    IL_00b0
  }
  catch System.Exception
  {
    IL_0097:  stloc.s    V_4
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.s   -2
    IL_009c:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<Test>d__1<T>.<>t__builder""
    IL_00a7:  ldloc.s    V_4
    IL_00a9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetException(System.Exception)""
    IL_00ae:  leave.s    IL_00c4
  }
  IL_00b0:  ldarg.0
  IL_00b1:  ldc.i4.s   -2
  IL_00b3:  stfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_00b8:  ldarg.0
  IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> C.<Test>d__1<T>.<>t__builder""
  IL_00be:  ldloc.1
  IL_00bf:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetResult(string)""
  IL_00c4:  ret
}
");
        }

        [Fact]
        public void TupleAsyncCapture03()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42).Result);
    }

    public static async Task<T> Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        await Task.Yield();

        return x.Test(a);
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public U Test<U>(U val)
        {
            return val;
        }
    }
}
";
            var verifier = CompileAndVerify(source, additionalRefs: new[] { MscorlibRef_v46 }, expectedOutput: @"42", options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.<Test>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      197 (0xc5)
  .maxstack  3
  .locals init (int V_0,
                T V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0058
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0017:  newobj     ""System.ValueTuple<T, T>..ctor(T, T)""
    IL_001c:  stfld      ""(T f1, T f2) C.<Test>d__1<T>.<x>5__1""
    IL_0021:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0026:  stloc.3
    IL_0027:  ldloca.s   V_3
    IL_0029:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_002e:  stloc.2
    IL_002f:  ldloca.s   V_2
    IL_0031:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0036:  brtrue.s   IL_0074
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.2
    IL_0043:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
    IL_004e:  ldloca.s   V_2
    IL_0050:  ldarg.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<Test>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<Test>d__1<T>)""
    IL_0056:  leave.s    IL_00c4
    IL_0058:  ldarg.0
    IL_0059:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_005e:  stloc.2
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0065:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.m1
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_007b:  ldloca.s   V_2
    IL_007d:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0083:  ldarg.0
    IL_0084:  ldflda     ""(T f1, T f2) C.<Test>d__1<T>.<x>5__1""
    IL_0089:  ldarg.0
    IL_008a:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_008f:  call       ""T System.ValueTuple<T, T>.Test<T>(T)""
    IL_0094:  stloc.1
    IL_0095:  leave.s    IL_00b0
  }
  catch System.Exception
  {
    IL_0097:  stloc.s    V_4
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.s   -2
    IL_009c:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
    IL_00a7:  ldloc.s    V_4
    IL_00a9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetException(System.Exception)""
    IL_00ae:  leave.s    IL_00c4
  }
  IL_00b0:  ldarg.0
  IL_00b1:  ldc.i4.s   -2
  IL_00b3:  stfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_00b8:  ldarg.0
  IL_00b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
  IL_00be:  ldloc.1
  IL_00bf:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetResult(T)""
  IL_00c4:  ret
}
");
        }

        [Fact]
        public void TupleAsyncCapture04()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42).Result);
    }

    public static async Task<T> Test<T>(T a)
    {
        var x = (f1: 1, f2: 2);

        await Task.Yield();

        return x.Test(a);
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public U Test<U>(U val)
        {
            return val;
        }
    }
}
";
            var verifier = CompileAndVerify(source, additionalRefs: new[] { MscorlibRef_v46 }, expectedOutput: @"42", options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.<Test>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (int V_0,
                T V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004e
    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  ldc.i4.2
    IL_000d:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
    IL_0012:  stfld      ""(int f1, int f2) C.<Test>d__1<T>.<x>5__1""
    IL_0017:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_001c:  stloc.3
    IL_001d:  ldloca.s   V_3
    IL_001f:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0024:  stloc.2
    IL_0025:  ldloca.s   V_2
    IL_0027:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_002c:  brtrue.s   IL_006a
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.0
    IL_0030:  dup
    IL_0031:  stloc.0
    IL_0032:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0037:  ldarg.0
    IL_0038:  ldloc.2
    IL_0039:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_003e:  ldarg.0
    IL_003f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
    IL_0044:  ldloca.s   V_2
    IL_0046:  ldarg.0
    IL_0047:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, C.<Test>d__1<T>>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref C.<Test>d__1<T>)""
    IL_004c:  leave.s    IL_00ba
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_0054:  stloc.2
    IL_0055:  ldarg.0
    IL_0056:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter C.<Test>d__1<T>.<>u__1""
    IL_005b:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_006a:  ldloca.s   V_2
    IL_006c:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0071:  ldloca.s   V_2
    IL_0073:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0079:  ldarg.0
    IL_007a:  ldflda     ""(int f1, int f2) C.<Test>d__1<T>.<x>5__1""
    IL_007f:  ldarg.0
    IL_0080:  ldfld      ""T C.<Test>d__1<T>.a""
    IL_0085:  call       ""T System.ValueTuple<int, int>.Test<T>(T)""
    IL_008a:  stloc.1
    IL_008b:  leave.s    IL_00a6
  }
  catch System.Exception
  {
    IL_008d:  stloc.s    V_4
    IL_008f:  ldarg.0
    IL_0090:  ldc.i4.s   -2
    IL_0092:  stfld      ""int C.<Test>d__1<T>.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
    IL_009d:  ldloc.s    V_4
    IL_009f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetException(System.Exception)""
    IL_00a4:  leave.s    IL_00ba
  }
  IL_00a6:  ldarg.0
  IL_00a7:  ldc.i4.s   -2
  IL_00a9:  stfld      ""int C.<Test>d__1<T>.<>1__state""
  IL_00ae:  ldarg.0
  IL_00af:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> C.<Test>d__1<T>.<>t__builder""
  IL_00b4:  ldloc.1
  IL_00b5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetResult(T)""
  IL_00ba:  ret
}
");
        }

        [Fact]
        public void TupleAsyncCapture05()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42).Result);
    }

    public static async Task<T> Test<T>(T a)
    {
        var x = (f1: a, f2: a);

        await Task.Yield();

        return x.P1;
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public T1 P1
        {
            get
            {
                return Item1;
            }
        }
    }
}
";
            var verifier = CompileAndVerify(source, additionalRefs: new[] { MscorlibRef_v46 }, expectedOutput: @"42", options: TestOptions.ReleaseExe);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TupleAsyncCapture06()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        var v1 = M1();
        System.Action<int> d1 = (int i1) => System.Console.WriteLine(i1); 
        v1.Test(v1, d1).Wait();
        Test(v1).Wait();
    }

    static async Task Test<T1, T2>((T1, T2) v1)
    {
        System.Action<T1> d1 = (T1 i1) => System.Console.WriteLine(i1); 
        System.Action<T2> d2 = (T2 i2) => System.Console.WriteLine(i2); 

        await Task.Yield();

        v1.E1 += d1;
        v1.RaiseE1();
        v1.E1 -= d1;
        v1.RaiseE1();
        v1.E2 += d2;
        v1.RaiseE2();
        v1.E2 -= d2;
        v1.RaiseE2();   
    }

    static (int, long) M1()
    {
        return (1, 11);
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.E1 = null;
            this._e2 = null;
        }

        public event System.Action<T1> E1;

        private System.Action<T2> _e2;
        public event System.Action<T2> E2
        {
            add
            {
                _e2 += value;
            }
            remove
            {
                _e2 -= value;
            }
        }

        public void RaiseE1()
        {
            System.Console.WriteLine(""-"");
            if (E1 == null)
            {
                System.Console.WriteLine(""null"");
            }
            else
            {
                E1(Item1);
            }
            System.Console.WriteLine(""-"");
        }

        public void RaiseE2()
        {
            System.Console.WriteLine(""--"");
            if (_e2 == null)
            {
                System.Console.WriteLine(""null"");
            }
            else
            {
                _e2(Item2);
            }
            System.Console.WriteLine(""--"");
        }

        public async Task Test<S1, S2>((S1, S2) val, System.Action<S1> d)
        {
            d = d;
            await Task.Yield();

            val.E1 += d;
            System.Console.WriteLine(val.E1);
            val.E1(val.Item1);
            val.E1 -= d;
            System.Console.WriteLine(val.E1 == null);
        }
    }
}
" + tupleattributes_cs;

            var comp = CompileAndVerify(source,
                additionalRefs: new[] { MscorlibRef_v46 }, expectedOutput:
@"System.Action`1[System.Int32]
1
True
-
1
-
-
null
-
--
11
--
--
null
--");
        }

        [Fact]
        public void LongTupleWithSubstitution()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        Console.WriteLine(Test(42).Result);
    }

    public static async Task<T> Test<T>(T a)
    {
        var x = (f1: 1, f2: 2, f3: 3, f4: 4, f5: 5, f6: 6, f7: 7, f8: a);

        await Task.Yield();

        return x.f8;
    }
}
" + trivial2uple + trivial3uple + trivialRemainingTuples;

            CompileAndVerify(source, expectedOutput: @"42", additionalRefs: new[] { MscorlibRef_v46 }, options: TestOptions.ReleaseExe);
        }

        [Fact]
        public void TupleUsageWithoutTupleLibrary()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string) x = (1, ""hello"");
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (int, string) x = (1, "hello");
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(int, string)").WithArguments("System.ValueTuple`2").WithLocation(6, 9),
                // (6,27): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         (int, string) x = (1, "hello");
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"(1, ""hello"")").WithArguments("System.ValueTuple`2").WithLocation(6, 27)
                );
        }

        [Fact]
        public void TupleUsageWithMissingTupleMembers()
        {
            var source = @"
namespace System
{
    public struct ValueTuple<T1, T2> { }
}

class C
{
    static void Main()
    {
        (int, int) x = (1, 2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp");
            comp.VerifyEmitDiagnostics(
                // (11,24): error CS8205: Member '.ctor' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (int, int) x = (1, 2);
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "(1, 2)").WithArguments(".ctor", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(11, 24)
                               );
        }

        [Fact]
        public void TupleWithDuplicateNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string a) x = (b: 1, b: ""hello"", b: 2);
    }
}
" + trivial2uple + trivial3uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,24): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, b: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "a").WithLocation(6, 24),
                // (6,38): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, b: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 38),
                // (6,50): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, b: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 50)
               );
        }

        [Fact]
        public void TupleWithDuplicateReservedNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int Item1, string Item1) x = (Item1: 1, Item1: ""hello"");
        (int Item2, string Item2) y = (Item2: 1, Item2: ""hello"");
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,28): error CS8201: Tuple member name 'Item1' is only allowed at position 1.
                //         (int Item1, string Item1) x = (Item1: 1, Item1: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "1").WithLocation(6, 28),
                // (6,50): error CS8201: Tuple member name 'Item1' is only allowed at position 1.
                //         (int Item1, string Item1) x = (Item1: 1, Item1: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "1").WithLocation(6, 50),
                // (7,14): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //         (int Item2, string Item2) y = (Item2: 1, Item2: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(7, 14),
                // (7,40): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //         (int Item2, string Item2) y = (Item2: 1, Item2: "hello");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(7, 40)
                );
        }

        [Fact]
        public void TupleWithNonReservedNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int Item1, int Item01, int Item10) x = (Item01: 1, Item1: 2, Item10: 3);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef });
            comp.VerifyDiagnostics(
                // (6,37): error CS8201: Tuple member name 'Item10' is only allowed at position 10.
                //         (int Item1, int Item01, int Item10) x = (Item01: 1, Item1: 2, Item10: 3);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item10").WithArguments("Item10", "10").WithLocation(6, 37),
                // (6,61): error CS8201: Tuple member name 'Item1' is only allowed at position 1.
                //         (int Item1, int Item01, int Item10) x = (Item01: 1, Item1: 2, Item10: 3);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "1").WithLocation(6, 61),
                // (6,71): error CS8201: Tuple member name 'Item10' is only allowed at position 10.
                //         (int Item1, int Item01, int Item10) x = (Item01: 1, Item1: 2, Item10: 3);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item10").WithArguments("Item10", "10").WithLocation(6, 71)
                );
        }

        [Fact]
        public void DefaultValueForTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b) x = (1, ""hello"");
        x = default((int, string));
        System.Console.WriteLine(x.a);
        System.Console.WriteLine(x.b ?? ""null"");
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"0
null");
        }

        [Fact]
        public void TupleWithDuplicateMemberNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string a) x = (b: 1, c: ""hello"", b: 2);
    }
}
" + trivial2uple + trivial3uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,24): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, c: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "a").WithLocation(6, 24),
                // (6,50): error CS8203: Tuple member names must be unique.
                //         (int a, string a) x = (b: 1, c: "hello", b: 2);
                Diagnostic(ErrorCode.ERR_TupleDuplicateMemberName, "b").WithLocation(6, 50)
                );
        }

        [Fact]
        public void TupleWithReservedMemberNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: ""bad"", Item4: ""bad"", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: ""bad"");
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef });
            comp.VerifyDiagnostics(
                // (6,28): error CS8201: Tuple member name 'Item3' is only allowed at position 3.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item3").WithArguments("Item3", "3").WithLocation(6, 28),
                // (6,42): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(6, 42),
                // (6,100): error CS8202: Tuple membername 'Rest' is disallowed at any position.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "Rest").WithArguments("Rest").WithLocation(6, 100),
                // (6,111): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(6, 111),
                // (6,125): error CS8201: Tuple member name 'Item4' is only allowed at position 4.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item4").WithArguments("Item4", "4").WithLocation(6, 125),
                // (6,189): error CS8202: Tuple membername 'Rest' is disallowed at any position.
                //         (int Item1, string Item3, string Item2, int Item4, int Item5, int Item6, int Item7, string Rest) x = (Item2: "bad", Item4: "bad", Item3: 3, Item4: 4, Item5: 5, Item6: 6, Item7: 7, Rest: "bad");
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "Rest").WithArguments("Rest").WithLocation(6, 189)
               );
        }

        [Fact]
        public void TupleWithExistingUnderlyingMemberNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (CompareTo: 2, Create: 3, Deconstruct: 4, Equals: 5, GetHashCode: 6, Rest: 8, ToString: 10);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef });
            comp.VerifyDiagnostics(
                // (6,18): error CS8202: Tuple membername 'CompareTo' is disallowed at any position.
                //         var x = (CompareTo: 2, Create: 3, Deconstruct: 4, Equals: 5, GetHashCode: 6, Rest: 8, ToString: 10);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "CompareTo").WithArguments("CompareTo").WithLocation(6, 18),
                // (6,43): error CS8202: Tuple membername 'Deconstruct' is disallowed at any position.
                //         var x = (CompareTo: 2, Create: 3, Deconstruct: 4, Equals: 5, GetHashCode: 6, Rest: 8, ToString: 10);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "Deconstruct").WithArguments("Deconstruct").WithLocation(6, 43),
                // (6,59): error CS8202: Tuple membername 'Equals' is disallowed at any position.
                //         var x = (CompareTo: 2, Create: 3, Deconstruct: 4, Equals: 5, GetHashCode: 6, Rest: 8, ToString: 10);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "Equals").WithArguments("Equals").WithLocation(6, 59),
                // (6,70): error CS8202: Tuple membername 'GetHashCode' is disallowed at any position.
                //         var x = (CompareTo: 2, Create: 3, Deconstruct: 4, Equals: 5, GetHashCode: 6, Rest: 8, ToString: 10);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "GetHashCode").WithArguments("GetHashCode").WithLocation(6, 70),
                // (6,86): error CS8202: Tuple membername 'Rest' is disallowed at any position.
                //         var x = (CompareTo: 2, Create: 3, Deconstruct: 4, Equals: 5, GetHashCode: 6, Rest: 8, ToString: 10);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "Rest").WithArguments("Rest").WithLocation(6, 86),
                // (6,95): error CS8202: Tuple membername 'ToString' is disallowed at any position.
                //         var x = (CompareTo: 2, Create: 3, Deconstruct: 4, Equals: 5, GetHashCode: 6, Rest: 8, ToString: 10);
                Diagnostic(ErrorCode.ERR_TupleReservedMemberNameAnyPosition, "ToString").WithArguments("ToString").WithLocation(6, 95)
                );
        }

        [Fact]
        public void TupleWithExistingInaccessibleUnderlyingMemberNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (CombineHashCodes: 1, Create: 2, GetHashCodeCore: 3, Size: 4, ToStringEnd: 5);
        System.Console.WriteLine(x.CombineHashCodes + "" "" + x.Create + "" "" + x.GetHashCodeCore + "" "" + x.Size + "" "" + x.ToStringEnd);
    }
}
";

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5", additionalRefs: new[] { MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef });
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LongTupleDeclaration()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int, int, int, int, int, int, string, int, int, int, int) x = (1, 2, 3, 4, 5, 6, 7, ""Alice"", 2, 3, 4, 5);
        System.Console.WriteLine($""{x.Item1} {x.Item2} {x.Item3} {x.Item4} {x.Item5} {x.Item6} {x.Item7} {x.Item8} {x.Item9} {x.Item10} {x.Item11} {x.Item12}"");
    }
}
" + trivial2uple + trivial3uple + trivialRemainingTuples;

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var x = nodes.OfType<VariableDeclaratorSyntax>().First();

                Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, "
                    + "System.String, System.Int32, System.Int32, System.Int32, System.Int32) x",
                    model.GetDeclaredSymbol(x).ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5 6 7 Alice 2 3 4 5", additionalRefs: new[] { MscorlibRef }, sourceSymbolValidator: validator);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LongTupleDeclarationWithNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, int b, int c, int d, int e, int f, int g, string h, int i, int j, int k, int l) x = (1, 2, 3, 4, 5, 6, 7, ""Alice"", 2, 3, 4, 5);
        System.Console.WriteLine($""{x.a} {x.b} {x.c} {x.d} {x.e} {x.f} {x.g} {x.h} {x.i} {x.j} {x.k} {x.l}"");
    }
}
";

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var x = nodes.OfType<VariableDeclaratorSyntax>().First();

                Assert.Equal("(System.Int32 a, System.Int32 b, System.Int32 c, System.Int32 d, System.Int32 e, System.Int32 f, System.Int32 g, "
                    + "System.String h, System.Int32 i, System.Int32 j, System.Int32 k, System.Int32 l) x",
                    model.GetDeclaredSymbol(x).ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5 6 7 Alice 2 3 4 5", additionalRefs: new[] { MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceSymbolValidator: validator);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void HugeTupleCreationParses()
        {
            StringBuilder b = new StringBuilder();
            b.Append("(");
            for (int i = 0; i < 3000; i++)
            {
                b.Append("1, ");
            }
            b.Append("1)");

            var source = @"
class C
{
    static void Main()
    {
        var x = " + b.ToString() + @";
    }
}
";
            CreateCompilationWithMscorlib(source);
        }

        [Fact]
        public void HugeTupleDeclarationParses()
        {
            StringBuilder b = new StringBuilder();
            b.Append("(");
            for (int i = 0; i < 3000; i++)
            {
                b.Append("int, ");
            }
            b.Append("int)");

            var source = @"
class C
{
    static void Main()
    {
        " + b.ToString() + @" x;
    }
}
";
            CreateCompilationWithMscorlib(source);
        }

        [Fact]
        public void GenericTupleWithoutTupleLibrary_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = M<int, bool>();
        System.Console.WriteLine($""{x.first} {x.second}"");
    }

    static (T1 first, T2 second) M<T1, T2>()
    {
        return (default(T1), default(T2));
    }
}
" + tupleattributes_cs;
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (10,12): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     static (T1 first, T2 second) M<T1, T2>()
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(T1 first, T2 second)").WithArguments("System.ValueTuple`2").WithLocation(10, 12),
                // (12,16): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         return (default(T1), default(T2));
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(default(T1), default(T2))").WithArguments("System.ValueTuple`2").WithLocation(12, 16)
                );

            var c = comp.GetTypeByMetadataName("C");

            var mTuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M").ReturnType;

            Assert.True(mTuple.IsTupleType);
            Assert.Equal(TypeKind.Error, mTuple.TupleUnderlyingType.TypeKind);
            Assert.Equal(SymbolKind.ErrorType, mTuple.TupleUnderlyingType.Kind);
            Assert.IsAssignableFrom<ErrorTypeSymbol>(mTuple.TupleUnderlyingType); 
            Assert.Equal(TypeKind.Struct, mTuple.TypeKind);
            AssertTupleTypeEquality(mTuple);
            Assert.False(mTuple.IsImplicitlyDeclared);
            Assert.Equal("Predefined type 'System.ValueTuple`2' is not defined or imported", mTuple.GetUseSiteDiagnostic().GetMessage(CultureInfo.InvariantCulture));
            Assert.Null(mTuple.BaseType);
            Assert.False(((TupleTypeSymbol)mTuple).UnderlyingDefinitionToMemberMap.Any());

            var mFirst = (FieldSymbol)mTuple.GetMembers("first").Single();

            Assert.IsType<TupleErrorFieldSymbol>(mFirst);

            Assert.True(mFirst.IsTupleField);
            Assert.Equal("first", mFirst.Name);
            Assert.Same(mFirst, mFirst.OriginalDefinition);
            Assert.True(mFirst.Equals(mFirst));
            Assert.Null(mFirst.TupleUnderlyingField);
            Assert.Null(mFirst.AssociatedSymbol);
            Assert.Same(mTuple, mFirst.ContainingSymbol);
            Assert.True(mFirst.CustomModifiers.IsEmpty);
            Assert.True(mFirst.GetAttributes().IsEmpty);
            Assert.Null(mFirst.GetUseSiteDiagnostic());
            Assert.False(mFirst.Locations.IsDefaultOrEmpty);
            Assert.Equal("first", mFirst.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.False(mFirst.IsImplicitlyDeclared);
            Assert.Null(mFirst.TypeLayoutOffset);

            var mItem1 = (FieldSymbol)mTuple.GetMembers("Item1").Single();

            Assert.IsType<TupleErrorFieldSymbol>(mItem1);

            Assert.True(mItem1.IsTupleField);
            Assert.Equal("Item1", mItem1.Name);
            Assert.Same(mItem1, mItem1.OriginalDefinition);
            Assert.True(mItem1.Equals(mItem1));
            Assert.Null(mItem1.TupleUnderlyingField);
            Assert.Null(mItem1.AssociatedSymbol);
            Assert.Same(mTuple, mItem1.ContainingSymbol);
            Assert.True(mItem1.CustomModifiers.IsEmpty);
            Assert.True(mItem1.GetAttributes().IsEmpty);
            Assert.Null(mItem1.GetUseSiteDiagnostic());
            Assert.True(mItem1.Locations.IsEmpty);
            Assert.False(mItem1.IsImplicitlyDeclared);
            Assert.Null(mItem1.TypeLayoutOffset);
        }

        [Fact]
        [WorkItem(11287, "https://github.com/dotnet/roslyn/issues/11287")]
        public void GenericTupleWithoutTupleLibrary_02()
        {
            var source = @"
class C
{
    static void Main()
    {
    }

    static (T1, T2, T3, T4, T5, T6, T7, T8, T9) M<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
    {
        throw new System.NotSupportedException();
    }
}
" + trivial2uple + tupleattributes_cs;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (8,12): error CS0518: Predefined type 'System.ValueTuple`8' is not defined or imported
                //     static (T1, T2, T3, T4, T5, T6, T7, T8, T9) M<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(T1, T2, T3, T4, T5, T6, T7, T8, T9)").WithArguments("System.ValueTuple`8").WithLocation(8, 12)
                );
        }

        [Fact]
        public void GenericTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = M<int, bool>();
        System.Console.WriteLine($""{x.first} {x.second}"");
    }

    static (T1 first, T2 second) M<T1, T2>()
    {
        return (default(T1), default(T2));
    }
}
";
            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"0 False");
        }

        [Fact]
        public void LongTupleCreation()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 2, 3, 4, 5, 6, 7, ""Alice"", 2, 3, 4, 5, 6, 7, ""Bob"", 2, 3);
        System.Console.WriteLine($""{x.Item1} {x.Item2} {x.Item3} {x.Item4} {x.Item5} {x.Item6} {x.Item7} {x.Item8} {x.Item9} {x.Item10} {x.Item11} {x.Item12} {x.Item13} {x.Item14} {x.Item15} {x.Item16} {x.Item17}"");
    }
}
";

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var node = nodes.OfType<TupleExpressionSyntax>().Single();

                Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, "
                     + "System.String, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, "
                     + "System.String, System.Int32, System.Int32)",
                     model.GetTypeInfo(node).Type.ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5 6 7 Alice 2 3 4 5 6 7 Bob 2 3", additionalRefs: new[] { MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceSymbolValidator: validator);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TupleInLambda()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Action<(int, string)> f = ((int, string) x) => System.Console.WriteLine($""{x.Item1} {x.Item2}"");
        f((42, ""Alice""));
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"42 Alice");
        }

        [Fact]
        public void TupleWithNamesInLambda()
        {
            var source = @"
class C
{
    static void Main()
    {
        int a, b = 0;
        System.Action<(int, string)> f = ((int a, string b) x) => System.Console.WriteLine($""{x.a} {x.b}"");
        f((c: 42, d: ""Alice""));
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"42 Alice");
        }

        [Fact]
        public void TupleInProperty()
        {
            var source = @"
class C
{
    static (int a, string b) P { get; set; }

    static void Main()
    {
        P = (42, ""Alice"");
        System.Console.WriteLine($""{P.a} {P.b}"");
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"42 Alice");
        }

        [Fact]
        public void ExtensionMethodOnTuple()
        {
            var source = @"
static class C
{
    static void Extension(this (int a, string b) x)
    {
        System.Console.WriteLine($""{x.a} {x.b}"");
    }
    static void Main()
    {
        (42, ""Alice"").Extension();
    }
}
";

            CompileAndVerify(source,
                additionalRefs: new[] { SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef },
                expectedOutput: @"42 Alice");
        }

        [Fact]
        public void TupleInOptionalParam()
        {
            var source = @"
class C
{
    void M(int x, (int a, string b) y = (42, ""Alice"")) { }
}
";

            CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs).VerifyDiagnostics(
                // (4,41): error CS1736: Default parameter value for 'y' must be a compile-time constant
                //     void M(int x, (int a, string b) y = (42, "Alice"))
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"(42, ""Alice"")").WithArguments("y").WithLocation(4, 41));
        }

        [Fact]
        public void TupleDefaultInOptionalParam()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M();
    }

    static void M((int a, string b) x = default((int, string)))
    {
        System.Console.WriteLine($""{x.a} {x.b}"");
    }
}
";
            CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs.Concat(new[] { SystemCoreRef }),
                expectedOutput: @"0 ");
        }

        [Fact]
        public void TupleAsNamedParam()
        {
            var source = @"
class C
{
    static void Main()
    {
        M(y : (42, ""Alice""), x : 1);
    }
    static void M(int x, (int a, string b) y)
    {
        System.Console.WriteLine($""{y.a} {y.Item2}"");
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"42 Alice");
        }

        [Fact]
        public void LongTupleCreationWithNames()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: ""Alice"", i: 2, j: 3, k: 4, l: 5, m: 6, n: 7, o: ""Bob"", p: 2, q: 3);
        System.Console.WriteLine($""{x.a} {x.b} {x.c} {x.d} {x.e} {x.f} {x.g} {x.h} {x.i} {x.j} {x.k} {x.l} {x.m} {x.n} {x.o} {x.p} {x.q}"");
    }
}
";

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var node = nodes.OfType<TupleExpressionSyntax>().Single();

                Assert.Equal("(System.Int32 a, System.Int32 b, System.Int32 c, System.Int32 d, System.Int32 e, System.Int32 f, System.Int32 g, "
                     + "System.String h, System.Int32 i, System.Int32 j, System.Int32 k, System.Int32 l, System.Int32 m, System.Int32 n, "
                     + "System.String o, System.Int32 p, System.Int32 q)",
                     model.GetTypeInfo(node).Type.ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"1 2 3 4 5 6 7 Alice 2 3 4 5 6 7 Bob 2 3", additionalRefs: new[] { MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceSymbolValidator: validator);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LongTupleWithArgumentEvaluation()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: PrintAndReturn(1), b: 2, c: 3, d: PrintAndReturn(4), e: 5, f: 6, g: PrintAndReturn(7), h: PrintAndReturn(""Alice""), i: 2, j: 3, k: 4, l: 5, m: 6, n: PrintAndReturn(7), o: PrintAndReturn(""Bob""), p: 2, q: PrintAndReturn(3));
    }

    static T PrintAndReturn<T>(T i)
    {
        System.Console.Write(i + "" "");
        return i;
    }
}
";

            var verifier = CompileAndVerify(source, expectedOutput: @"1 4 7 Alice 7 Bob 3", additionalRefs: new[] { MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef });
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LongTupleGettingRest()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: ""Alice"", i: 1);
        System.Console.WriteLine($""{x.Rest.Item1} {x.Rest.Item2}"");
    }
}
";

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var node = nodes.OfType<MemberAccessExpressionSyntax>().Where(n => n.ToString() == "x.Rest").First();
                Assert.Equal("(System.String, System.Int32)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            };

            var verifier = CompileAndVerify(source, expectedOutput: @"Alice 1", additionalRefs: new[] { MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceSymbolValidator: validator);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void MethodReturnsValueTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(M().ToString());
    }

    static (int, string) M()
    {
        return (1, ""hello"");
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"(1, hello)");
        }

        [Fact]
        public void DistinctTupleTypesInCompilation()
        {
            var source1 = @"
public class C1
{
    public static (int a, int b) M()
    {
        return (1, 2);
    }
}
";

            var source2 = @"
public class C2
{
    public static (int c, int d) M()
    {
        return (3, 4);
    }
}
";

            var source = @"
class C3
{
    public static void Main()
    {
        System.Console.Write(C1.M().Item1 + "" "");
        System.Console.Write(C1.M().a + "" "");
        System.Console.Write(C1.M().Item2 + "" "");
        System.Console.Write(C1.M().b + "" "");
        System.Console.Write(C2.M().Item1 + "" "");
        System.Console.Write(C2.M().c + "" "");
        System.Console.Write(C2.M().Item2 + "" "");
        System.Console.Write(C2.M().d);
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(source1,
                references: s_valueTupleRefs);
            comp1.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlib(source2,
                references: s_valueTupleRefs);
            comp2.VerifyDiagnostics();
            var comp = CompileAndVerify(source,
                expectedOutput: @"1 1 2 2 3 3 4 4",
                additionalRefs: new[] {
                    new CSharpCompilationReference(comp1),
                    new CSharpCompilationReference(comp2),
                    ValueTupleRef,
                    SystemRuntimeFacadeRef
                });
        }

        [Fact]
        public void DistinctTupleTypesInCompilationCanAssign()
        {
            var source1 = @"
public class C1
{
    public static (int a, int b) M()
    {
        System.Console.WriteLine(""C1.M"");
        return (1, 2);
    }
}
" + trivial2uple + tupleattributes_cs;

            var source2 = @"
public class C2
{
    public static (int c, int d) M()
    {
        System.Console.WriteLine(""C2.M"");
        return (3, 4);
    }
}
" + trivial2uple + tupleattributes_cs;

            var source = @"
class C3
{
    public static void Main()
    {
        var x = C1.M();
        x = C2.M();
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(source1);
            comp1.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlib(source2);
            comp2.VerifyDiagnostics();
            var comp = CreateCompilationWithMscorlib(source, references: new[] { new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) }, options: TestOptions.ReleaseExe);

            var v = CompileAndVerify(comp, expectedOutput:@"
C1.M
C2.M
");

v.VerifyIL("C3.Main()",
@"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0)
  IL_0000:  call       ""(int a, int b) C1.M()""
  IL_0005:  pop
  IL_0006:  call       ""(int c, int d) C2.M()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0012:  ldloc.0
  IL_0013:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0018:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_001d:  pop
  IL_001e:  ret
}
");

        }

        [Fact]
        public void AmbiguousTupleTypesForCreation()
        {
            var source = @"
class C3
{
    public static void Main()
    {
        var x = (1, 1);
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(trivial2uple, assemblyName: "comp1");
            var comp2 = CreateCompilationWithMscorlib(trivial2uple);

            var comp = CreateCompilationWithMscorlib(source, references: new[] { new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) });
            comp.VerifyDiagnostics(
                // (6,17): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         var x = (1, 1);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(1, 1)").WithArguments("System.ValueTuple`2").WithLocation(6, 17)
                );
        }

        [Fact]
        public void AmbiguousTupleTypesForDeclaration()
        {
            var source = @"
class C3
{
    public void M((int, int) x) { }
}
";
            var comp1 = CreateCompilationWithMscorlib(trivial2uple, assemblyName: "comp1");
            var comp2 = CreateCompilationWithMscorlib(trivial2uple);

            var comp = CreateCompilationWithMscorlib(source,
                references: new[] { new CSharpCompilationReference(comp1),
                    new CSharpCompilationReference(comp2),
                    ValueTupleRef });
            comp.VerifyDiagnostics(
                // (4,19): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     public void M((int, int) x) { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(int, int)").WithArguments("System.ValueTuple`2").WithLocation(4, 19)
                );
        }

        [Fact]
        public void LocalTupleTypeWinsWhenTupleTypesInCompilation()
        {
            var source1 = @"
public class C1
{
    public static (int a, int b) M()
    {
        return (1, 2);
    }
}
" + trivial2uple + tupleattributes_cs;

            var source2 = @"
public class C2
{
    public static (int c, int d) M()
    {
        return (3, 4);
    }
}
" + trivial2uple + tupleattributes_cs;

            var source = @"
class C3
{
    public static void Main()
    {
        System.Console.Write(C1.M().Item1 + "" "");
        System.Console.Write(C1.M().a + "" "");
        System.Console.Write(C1.M().Item2 + "" "");
        System.Console.Write(C1.M().b + "" "");
        System.Console.Write(C2.M().Item1 + "" "");
        System.Console.Write(C2.M().c + "" "");
        System.Console.Write(C2.M().Item2 + "" "");
        System.Console.Write(C2.M().d + "" "");

        var x = (e: 5, f: 6);
        System.Console.Write(x.Item1 + "" "");
        System.Console.Write(x.e + "" "");
        System.Console.Write(x.Item2 + "" "");
        System.Console.Write(x.f + "" "");
        System.Console.Write(x.GetType().Assembly == typeof(C3).Assembly);
    }
}
" + trivial2uple;

            var comp1 = CreateCompilationWithMscorlib(source1);
            comp1.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlib(source2);
            comp2.VerifyDiagnostics();
            var comp = CompileAndVerify(source, expectedOutput: @"1 1 2 2 3 3 4 4 5 5 6 6 True", additionalRefs: new[] { new CSharpCompilationReference(comp1), new CSharpCompilationReference(comp2) });
        }

        [Fact]
        public void UnderlyingTypeMemberWithWrongSignature_1()
        {
            string source = @"
class C
{
    static void M()
    {
        var x = (""Alice"", ""Bob"");
        System.Console.WriteLine($""{x.Item1}"");
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public string Item1; // Not T1
        public int Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = ""1"";
            this.Item2 = 2;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp");
            comp.VerifyDiagnostics(
                // (7,39): error CS0229: Ambiguity between '(string, string).Item1' and '(string, string).Item1'
                //         System.Console.WriteLine($"{x.Item1}");
                Diagnostic(ErrorCode.ERR_AmbigMember, "Item1").WithArguments("(string, string).Item1", "(string, string).Item1").WithLocation(7, 39)
                );
        }

        [Fact]
        public void UnderlyingTypeMemberWithWrongSignature_2()
        {
            string source = @"
class C
{
    (string, string) M()
    {
        var x = (""Alice"", ""Bob"");
        System.Console.WriteLine($""{x.Item1}"");
        return x;
    }

    void M2((int a, int b) y)
    {
        System.Console.WriteLine($""{y.Item1}"");
        System.Console.WriteLine($""{y.a}"");
        System.Console.WriteLine($""{y.Item2}"");
        System.Console.WriteLine($""{y.b}"");
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public int Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item2 = 2;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp");
            comp.VerifyDiagnostics(
                // (11,13): error CS8207: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     void M2((int a, int b) y)
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(int a, int b)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(11, 13),
                // (4,5): error CS8207: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                //     (string, string) M()
                Diagnostic(ErrorCode.ERR_TupleElementNamesAttributeMissing, "(string, string)").WithArguments("System.Runtime.CompilerServices.TupleElementNamesAttribute").WithLocation(4, 5),
                // (7,39): error CS8205: Member 'Item1' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{x.Item1}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "Item1").WithArguments("Item1", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 39),
                // (13,39): error CS8205: Member 'Item1' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{y.Item1}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "Item1").WithArguments("Item1", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(13, 39),
                // (14,39): error CS8205: Member 'Item1' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{y.a}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "a").WithArguments("Item1", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(14, 39),
                // (15,39): error CS0229: Ambiguity between '(int a, int b).Item2' and '(int a, int b).Item2'
                //         System.Console.WriteLine($"{y.Item2}");
                Diagnostic(ErrorCode.ERR_AmbigMember, "Item2").WithArguments("(int a, int b).Item2", "(int a, int b).Item2").WithLocation(15, 39),
                // (16,39): error CS8205: Member 'Item2' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{y.b}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "b").WithArguments("Item2", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(16, 39)
                );

            var mTuple = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M").ReturnType;
            AssertTupleTypeEquality(mTuple);

            var mItem1 = (FieldSymbol)mTuple.GetMembers("Item1").Single();

            Assert.IsType<TupleErrorFieldSymbol>(mItem1);

            Assert.True(mItem1.IsTupleField);
            Assert.Same(mItem1, mItem1.OriginalDefinition);
            Assert.True(mItem1.Equals(mItem1));
            Assert.Equal("Item1", mItem1.Name);
            Assert.Null(mItem1.TupleUnderlyingField);
            Assert.Null(mItem1.AssociatedSymbol);
            Assert.Same(mTuple, mItem1.ContainingSymbol);
            Assert.True(mItem1.CustomModifiers.IsEmpty);
            Assert.True(mItem1.GetAttributes().IsEmpty);
            Assert.Equal("error CS8205: Member 'Item1' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.",
                         mItem1.GetUseSiteDiagnostic().ToString());
            Assert.False(mItem1.Locations.IsDefaultOrEmpty);
            Assert.Equal("string", mItem1.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.False(mItem1.IsImplicitlyDeclared);
            Assert.Null(mItem1.TypeLayoutOffset);

            AssertTestDisplayString(mTuple.GetMembers(),
                "System.Int32 (System.String, System.String).Item2",
                "(System.String, System.String)..ctor(System.String item1, System.String item2)", 
                "(System.String, System.String)..ctor()", 
                "System.String (System.String, System.String).Item1", 
                "System.String (System.String, System.String).Item2");

            var m2Tuple = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M2").Parameters[0].Type;
            AssertTupleTypeEquality(m2Tuple);
            AssertTestDisplayString(m2Tuple.GetMembers(),
                "System.Int32 (System.Int32 a, System.Int32 b).Item2",
                "(System.Int32 a, System.Int32 b)..ctor(System.Int32 item1, System.Int32 item2)", 
                "(System.Int32 a, System.Int32 b)..ctor()",
                "System.Int32 (System.Int32 a, System.Int32 b).Item1",
                "System.Int32 (System.Int32 a, System.Int32 b).a",
                "System.Int32 (System.Int32 a, System.Int32 b).Item2",
                "System.Int32 (System.Int32 a, System.Int32 b).b"
                );
        }

        [Fact]
        public void UnderlyingTypeMemberWithWrongSignature_3()
        {
            string source = @"
class C
{
    static void M()
    {
        var x = (a: ""Alice"", b: ""Bob"");
        System.Console.WriteLine($""{x.a}"");
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public int Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item2 = 2;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp");
            comp.VerifyDiagnostics(
                // (7,39): error CS8205: Member 'Item1' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{x.a}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "a").WithArguments("Item1", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 39)
                );
        }

        [Fact]
        public void UnderlyingTypeMemberWithWrongSignature_4()
        {
            string source = @"
class C
{
    static void M()
    {
        var x = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8, i: 9);
        System.Console.WriteLine($""{x.a}"");
        System.Console.WriteLine($""{x.g}"");
        System.Console.WriteLine($""{x.h}"");
        System.Console.WriteLine($""{x.i}"");
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, assemblyName: "comp");
            comp.VerifyDiagnostics(
                // (7,39): error CS8205: Member 'Item1' was not found on type 'ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{x.a}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "a").WithArguments("Item1", "System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 39),
                // (8,39): error CS8205: Member 'Item7' was not found on type 'ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{x.g}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "g").WithArguments("Item7", "System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 39),
                // (9,39): error CS8205: Member 'Item1' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{x.h}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "h").WithArguments("Item1", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 39),
                // (10,39): error CS8205: Member 'Item2' was not found on type 'ValueTuple<T1, T2>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         System.Console.WriteLine($"{x.i}");
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "i").WithArguments("Item2", "System.ValueTuple<T1, T2>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(10, 39)
                );
        }

        [Fact]
        public void ImplementTupleInterface()
        {
            string source = @"
public interface I
{
    (int, int) M((string, string) a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((""Alice"", ""Bob""));
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    public (int, int) M((string, string) a)
    {
        return (a.Item1.Length, a.Item2.Length);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"5 3", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementTupleInterfaceWithDifferentNames()
        {
            string source = @"
public interface I
{
    (int i1, int i2) M((string s1, string s2) a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((""Alice"", ""Bob""));
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    public (int i3, int i4) M((string s3, string s4) a)
    {
        return (a.Item1.Length, a.Item2.Length);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"5 3", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementLongTupleInterface()
        {
            string source = @"
public interface I
{
    (int, int, int, int, int, int, int, int) M((int, int, int, int, int, int, int, int) a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((1, 2, 3, 4, 5, 6, 7, 8));
        System.Console.WriteLine($""{r.Item1} {r.Item7} {r.Item8}"");
    }

    public (int, int, int, int, int, int, int, int) M((int, int, int, int, int, int, int, int) a)
    {
        return a;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"1 7 8", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementTupleInterfaceWithValueTuple()
        {
            string source = @"
public interface I
{
    (int i1, int i2) M((string, string) a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((""Alice"", ""Bob""));
        System.Console.WriteLine($""{r.i1} {r.i2}"");
    }

    public System.ValueTuple<int, int> M(System.ValueTuple<string, string> a)
    {
        return (a.Item1.Length, a.Item2.Length);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"5 3", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementValueTupleInterfaceWithTuple()
        {
            string source = @"
public interface I
{
    System.ValueTuple<int, int> M(System.ValueTuple<string, string> a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M((""Alice"", ""Bob""));
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    public (int, int) M((string, string) a)
    {
        return new System.ValueTuple<int, int>(a.Item1.Length, a.Item2.Length);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"5 3", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementInterfaceWithGeneric()
        {
            string source = @"
public interface I<TB, TA> where TB : TA
{
    (TB, TA, TC) M<TC>((TB, (TA, TC)) arg) where TC : TB;
}

public class CA { public override string ToString() { return ""CA""; } }

public class CB : CA { public override string ToString() { return ""CB""; } }

public class CC : CB { public override string ToString() { return ""CC""; } }

class C : I<CB, CA>
{
    static void Main()
    {
        I<CB, CA> i = new C();
        var r = i.M((new CB(), (new CA(), new CC())));
        System.Console.WriteLine($""{r.Item1} {r.Item2} {r.Item3}"");
    }

    public (CB, CA, TC) M<TC>((CB, (CA, TC)) arg) where TC : CB
    {
        return (arg.Item1, arg.Item2.Item1, arg.Item2.Item2);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: @"CB CA CC", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplementInterfaceWithGenericError()
        {
            string source = @"
public interface I<TB, TA> where TB : TA
{
    (TB, TA, TC) M<TC>((TB, (TA, TC)) arg) where TC : TB;
}

public class CA { }

public class CB : CA { }

public class CC : CB { }

class C1 : I<CB, CA>
{
    public (CB, CA, TC) M<TC>((CB, (CA, TC)) arg)
    {
        return (arg.Item1, arg.Item2.Item1, arg.Item2.Item2);
    }
}

class C2 : I<CB, CA>
{
    public (CB, CA, TC) M<TC>((CB, (CA, TC)) arg) where TC : CB
    {
        return (arg.Item2.Item1, arg.Item1, arg.Item2.Item2);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (15,25): error CS0425: The constraints for type parameter 'TC' of method 'C1.M<TC>((CB, (CA, TC)))' must match the constraints for type parameter 'TC' of interface method 'I<CB, CA>.M<TC>((CB, (CA, TC)))'. Consider using an explicit interface implementation instead.
                //     public (CB, CA, TC) M<TC>((CB, (CA, TC)) arg)
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("TC", "C1.M<TC>((CB, (CA, TC)))", "TC", "I<CB, CA>.M<TC>((CB, (CA, TC)))").WithLocation(15, 25),
                // (25,17): error CS0029: Cannot implicitly convert type 'CA' to 'CB'
                //         return (arg.Item2.Item1, arg.Item1, arg.Item2.Item2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg.Item2.Item1").WithArguments("CA", "CB").WithLocation(25, 17)
                );
        }

        [Fact]
        public void OverrideTupleMethodWithDifferentNames()
        {
            string source = @"
class C
{
    public virtual (int a, int b) M((int c, int d) x)
    {
        throw new System.Exception();
    }
}
class D : C
{
    static void Main()
    {
        C c = new D();
        var r = c.M((1, 2));
        System.Console.WriteLine($""{r.a} {r.b}"");
    }

    public override (int e, int f) M((int g, int h) y)
    {
        return y;
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"1 2");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NewTupleMethodWithDifferentNames()
        {
            string source = @"
class C
{
    public virtual (int a, int b) M((int c, int d) x)
    {
        System.Console.WriteLine(""base"");
        return x;
    }
}
class D : C
{
    static void Main()
    {
        D d = new D();
        d.M((1, 2));
        C c = d;
        c.M((1, 2));
    }

    public new (int e, int f) M((int g, int h) y)
    {
        System.Console.Write(""new "");
        return y;
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"new base");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DuplicateTupleMethodsNotAllowed()
        {
            string source = @"
class C
{
    public (int, int) M((string, string) a)
    {
        return new System.ValueTuple<int, int>(a.Item1.Length, a.Item2.Length);
    }

    public System.ValueTuple<int, int> M(System.ValueTuple<string, string> a)
    {
        return (a.Item1.Length, a.Item2.Length);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (9,40): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
                //     public System.ValueTuple<int, int> M(System.ValueTuple<string, string> a)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "C").WithLocation(9, 40)
                );
        }

        [Fact]
        public void TupleArrays()
        {
            string source = @"
public interface I
{
    System.ValueTuple<int, int>[] M((int, int)[] a);
}

class C : I
{
    static void Main()
    {
        I i = new C();
        var r = i.M(new [] { new System.ValueTuple<int, int>(1, 2) });
        System.Console.WriteLine($""{r[0].Item1} {r[0].Item2}"");
    }

    public (int, int)[] M(System.ValueTuple<int, int>[] a)
    {
        return new [] { (a[0].Item1, a[0].Item2) };
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"1 2");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleRef()
        {
            string source = @"
class C
{
    static void Main()
    {
        var r = (1, 2);
        M(ref r);
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    static void M(ref (int, int) a)
    {
        System.Console.WriteLine($""{a.Item1} {a.Item2}"");
        a = (3, 4);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput:
@"1 2
3 4");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleOut()
        {
            string source = @"
class C
{
    static void Main()
    {
        (int, int) r;
        M(out r);
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    static void M(out (int, int) a)
    {
        a = (1, 2);
    }
}
";

            var comp = CompileAndVerify(source, 
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"1 2");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleTypeArgs()
        {
            string source = @"
class C
{
    static void Main()
    {
        var a = (1, ""Alice"");
        var r = M<int, string>(a);
        System.Console.WriteLine($""{r.Item1} {r.Item2}"");
    }

    static (T1, T2) M<T1, T2>((T1, T2) a)
    {
        return a;
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"1 Alice");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullableTuple()
        {
            string source = @"
class C
{
    static void Main()
    {
        M((1, ""Alice""));
    }

    static void M((int, string)? a)
    {
        System.Console.WriteLine($""{a.HasValue} {a.Value.Item1} {a.Value.Item2}"");
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"True 1 Alice");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Tuple2To8Members()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.Write((1, 2).Item1);
        System.Console.Write((1, 2).Item2);
        System.Console.Write((3, 4, 5).Item1);
        System.Console.Write((3, 4, 5).Item2);
        System.Console.Write((3, 4, 5).Item3);
        System.Console.Write((6, 7, 8, 9).Item1);
        System.Console.Write((6, 7, 8, 9).Item2);
        System.Console.Write((6, 7, 8, 9).Item3);
        System.Console.Write((6, 7, 8, 9).Item4);
        System.Console.Write((0, 1, 2, 3, 4).Item1);
        System.Console.Write((0, 1, 2, 3, 4).Item2);
        System.Console.Write((0, 1, 2, 3, 4).Item3);
        System.Console.Write((0, 1, 2, 3, 4).Item4);
        System.Console.Write((0, 1, 2, 3, 4).Item5);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item1);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item2);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item3);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item4);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item5);
        System.Console.Write((5, 6, 7, 8, 9, 0).Item6);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item1);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item2);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item3);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item4);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item5);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item6);
        System.Console.Write((1, 2, 3, 4, 5, 6, 7).Item7);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item1);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item2);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item3);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item4);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item5);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item6);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item7);
        System.Console.Write((8, 9, 0, 1, 2, 3, 4, 5).Item8);
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "12345678901234567890123456789012345", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
        }

        [Fact]
        public void CreateTupleTypeSymbol_BadArguments()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);

            Assert.Throws<ArgumentNullException>(() => comp.CreateTupleTypeSymbol(null, default(ImmutableArray<string>)));

            // if names are provided, you need one for each element
            var vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType);
            try
            {
                comp.CreateTupleTypeSymbol(vt2, new[] { "Item1" }.AsImmutable());
                Assert.True(false);
            }
            catch (ArgumentException e)
            {
                Assert.Contains(CodeAnalysisResources.TupleNamesAllOrNone, e.Message);
            }

            // if names are provided, they can't be null
            Assert.Throws<ArgumentNullException>(() => comp.CreateTupleTypeSymbol(vt2, new[] { "Item1", null }.AsImmutable()));
        }

        [Fact]
        public void CreateTupleTypeSymbol_WithValueTuple()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            var vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType);
            var tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, default(ImmutableArray<string>));

            Assert.True(tupleWithoutNames.IsTupleType);
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString());
            Assert.True(tupleWithoutNames.TupleElementNames.IsDefault);
            Assert.Equal(new[] { "System.Int32", "System.String" }, tupleWithoutNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol_NoNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            var vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType);

            var tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, default(ImmutableArray<string>));

            Assert.True(tupleWithoutNames.IsTupleType);
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString());
            Assert.True(tupleWithoutNames.TupleElementNames.IsDefault);
            Assert.Equal(new[] { "System.Int32", "System.String" }, tupleWithoutNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol_WithNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType);
            var tupleWithNames = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("Alice", "Bob"));

            Assert.True(tupleWithNames.IsTupleType);
            Assert.Equal("(System.Int32 Alice, System.String Bob)", tupleWithNames.ToTestDisplayString());
            Assert.Equal(new[] { "Alice", "Bob" }, tupleWithNames.TupleElementNames);
            Assert.Equal(new[] { "System.Int32", "System.String" }, tupleWithNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
            Assert.Equal(SymbolKind.NamedType, tupleWithNames.Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol_WithBadNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            var vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType);

            var tupleWithNames = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("Item2", "Item1"));

            Assert.True(tupleWithNames.IsTupleType);
            Assert.Equal("(System.Int32 Item2, System.Int32 Item1)", tupleWithNames.ToTestDisplayString());
            Assert.Equal(new[] { "Item2", "Item1" }, tupleWithNames.TupleElementNames);
            Assert.Equal(new[] { "System.Int32", "System.Int32" }, tupleWithNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
            Assert.Equal(SymbolKind.NamedType, tupleWithNames.Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol_Tuple8NoNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            var vt8 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest)
                          .Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                     comp.GetWellKnownType(WellKnownType.System_ValueTuple_T1).Construct(stringType));

            var tuple8WithoutNames = comp.CreateTupleTypeSymbol(vt8, default(ImmutableArray<string>));

            Assert.True(tuple8WithoutNames.IsTupleType);
            Assert.Equal("(System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32, System.String)",
                        tuple8WithoutNames.ToTestDisplayString());

            Assert.True(tuple8WithoutNames.TupleElementNames.IsDefault);

            Assert.Equal(new[] { "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String" },
                        tuple8WithoutNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
        }

        [Fact]
        public void CreateTupleTypeSymbol_Tuple8WithNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var vt8 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest)
                          .Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                     comp.GetWellKnownType(WellKnownType.System_ValueTuple_T1).Construct(stringType));

            var tuple8WithNames = comp.CreateTupleTypeSymbol(vt8, ImmutableArray.Create("Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8"));

            Assert.True(tuple8WithNames.IsTupleType);
            Assert.Equal("(System.Int32 Alice1, System.String Alice2, System.Int32 Alice3, System.String Alice4, System.Int32 Alice5, System.String Alice6, System.Int32 Alice7, System.String Alice8)",
                        tuple8WithNames.ToTestDisplayString());

            Assert.Equal(new[] { "Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8" }, tuple8WithNames.TupleElementNames);

            Assert.Equal(new[] { "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String" },
                        tuple8WithNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
        }

        [Fact]
        public void CreateTupleTypeSymbol_Tuple9NoNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var vt9 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest)
                          .Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                     comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(stringType, intType));

            var tuple9WithoutNames = comp.CreateTupleTypeSymbol(vt9, default(ImmutableArray<string>));

            Assert.True(tuple9WithoutNames.IsTupleType);
            Assert.Equal("(System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32)",
                        tuple9WithoutNames.ToTestDisplayString());

            Assert.True(tuple9WithoutNames.TupleElementNames.IsDefault);

            Assert.Equal(new[] { "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32" },
                        tuple9WithoutNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
        }

        [Fact]
        public void CreateTupleTypeSymbol_Tuple9WithNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var vt9 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest)
                          .Construct(intType, stringType, intType, stringType, intType, stringType, intType,
                                     comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(stringType, intType));

            var tuple9WithNames = comp.CreateTupleTypeSymbol(vt9, ImmutableArray.Create("Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9"));

            Assert.True(tuple9WithNames.IsTupleType);
            Assert.Equal("(System.Int32 Alice1, System.String Alice2, System.Int32 Alice3, System.String Alice4, System.Int32 Alice5, System.String Alice6, System.Int32 Alice7, System.String Alice8, System.Int32 Alice9)",
                        tuple9WithNames.ToTestDisplayString());

            Assert.Equal(new[] { "Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9" }, tuple9WithNames.TupleElementNames);

            Assert.Equal(new[] { "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32" },
                        tuple9WithNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
        }

        [Fact]
        public void CreateTupleTypeSymbol_ElementTypeIsError()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            TypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);

            var vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, ErrorTypeSymbol.UnknownResultType);

            var tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, default(ImmutableArray<string>));

            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind);

            var types = tupleWithoutNames.TupleElementTypes;
            Assert.Equal(2, types.Length);
            Assert.Equal(SymbolKind.NamedType, types[0].Kind);
            Assert.Equal(SymbolKind.ErrorType, types[1].Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol_BadNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef });

            NamedTypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            var vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, intType);

            // illegal C# identifiers and blank
            var tuple2 = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("123", ""));
            Assert.Equal(new[] { "123", "" }, tuple2.TupleElementNames);

            // reserved identifiers
            var tuple3 = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("return", "class"));
            Assert.Equal(new[] { "return", "class" }, tuple3.TupleElementNames);

            // not tuple-compatible underlying type
            try
            {
                comp.CreateTupleTypeSymbol(intType);
                Assert.True(false);
            }
            catch (ArgumentException e)
            {
                Assert.Contains(CodeAnalysisResources.TupleUnderlyingTypeMustBeTupleCompatible, e.Message);
            }
        }

        [Fact]
        public void CreateTupleTypeSymbol_VisualBasicElements()
        {
            var vbSource = @"Public Class C
End Class";

            var vbComp = CreateVisualBasicCompilation("VB", vbSource,
                                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            vbComp.VerifyDiagnostics();
            INamedTypeSymbol vbType = (INamedTypeSymbol)vbComp.GlobalNamespace.GetMembers("C").Single();

            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef });
            try
            {
                comp.CreateTupleTypeSymbol(vbType, default(ImmutableArray<string>));
                Assert.True(false);
            }
            catch (ArgumentException e)
            {
                Assert.Contains(CSharpResources.NotACSharpSymbol, e.Message);
            }
        }

        [Fact]
        public void CreateTupleTypeSymbol2_BadArguments()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);

            Assert.Throws<ArgumentNullException>(() => comp.CreateTupleTypeSymbol(default(ImmutableArray<ITypeSymbol>), default(ImmutableArray<string>)));

            // 0-tuple and 1-tuple are not supported at this point
            Assert.Throws<ArgumentException>(() => comp.CreateTupleTypeSymbol(ImmutableArray<ITypeSymbol>.Empty, default(ImmutableArray<string>)));
            Assert.Throws<ArgumentException>(() => comp.CreateTupleTypeSymbol(new[] { intType }.AsImmutable(), default(ImmutableArray<string>)));

            // if names are provided, you need one for each element
            Assert.Throws<ArgumentException>(() => comp.CreateTupleTypeSymbol(new[] { intType, intType }.AsImmutable(), new[] { "Item1" }.AsImmutable()));

            // if names are provided, they can't be null
            Assert.Throws<ArgumentNullException>(() => comp.CreateTupleTypeSymbol(new[] { intType, intType }.AsImmutable(), new[] { "Item1", null }.AsImmutable()));

            // null types aren't allowed
            Assert.Throws<ArgumentNullException>(() => comp.CreateTupleTypeSymbol(new[] { intType, null }.AsImmutable(), default(ImmutableArray<string>)));
        }

        [Fact]
        public void CreateTupleTypeSymbol2_WithValueTuple()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), default(ImmutableArray<string>));

            Assert.True(tupleWithoutNames.IsTupleType);
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString());
            Assert.True(tupleWithoutNames.TupleElementNames.IsDefault);
            Assert.Equal(new[] { "System.Int32", "System.String" }, tupleWithoutNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol2_NoNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), default(ImmutableArray<string>));

            Assert.True(tupleWithoutNames.IsTupleType);
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString());
            Assert.True(tupleWithoutNames.TupleElementNames.IsDefault);
            Assert.Equal(new[] { "System.Int32", "System.String" }, tupleWithoutNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol2_WithNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var tupleWithNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), ImmutableArray.Create("Alice", "Bob"));

            Assert.True(tupleWithNames.IsTupleType);
            Assert.Equal("(System.Int32 Alice, System.String Bob)", tupleWithNames.ToTestDisplayString());
            Assert.Equal(new[] { "Alice", "Bob" }, tupleWithNames.TupleElementNames);
            Assert.Equal(new[] { "System.Int32", "System.String" }, tupleWithNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
            Assert.Equal(SymbolKind.NamedType, tupleWithNames.Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol2_WithBadNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);

            var tupleWithNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("Item2", "Item1"));

            Assert.True(tupleWithNames.IsTupleType);
            Assert.Equal("(System.Int32 Item2, System.Int32 Item1)", tupleWithNames.ToTestDisplayString());
            Assert.Equal(new[] { "Item2", "Item1" }, tupleWithNames.TupleElementNames);
            Assert.Equal(new[] { "System.Int32", "System.Int32" }, tupleWithNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
            Assert.Equal(SymbolKind.NamedType, tupleWithNames.Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol2_Tuple8NoNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var tuple8WithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType, intType, stringType, intType, stringType, intType, stringType),
                                                                default(ImmutableArray<string>));

            Assert.True(tuple8WithoutNames.IsTupleType);
            Assert.Equal("(System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32, System.String)",
                        tuple8WithoutNames.ToTestDisplayString());

            Assert.True(tuple8WithoutNames.TupleElementNames.IsDefault);

            Assert.Equal(new[] { "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String" },
                        tuple8WithoutNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
        }

        [Fact]
        public void CreateTupleTypeSymbol2_Tuple8WithNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var tuple8WithNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType, intType, stringType, intType, stringType, intType, stringType),
                                                            ImmutableArray.Create("Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8"));

            Assert.True(tuple8WithNames.IsTupleType);
            Assert.Equal("(System.Int32 Alice1, System.String Alice2, System.Int32 Alice3, System.String Alice4, System.Int32 Alice5, System.String Alice6, System.Int32 Alice7, System.String Alice8)",
                        tuple8WithNames.ToTestDisplayString());

            Assert.Equal(new[] { "Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8" }, tuple8WithNames.TupleElementNames);

            Assert.Equal(new[] { "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String" },
                        tuple8WithNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
        }

        [Fact]
        public void CreateTupleTypeSymbol2_Tuple9NoNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var tuple9WithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType, intType, stringType, intType, stringType, intType, stringType, intType),
                                                                default(ImmutableArray<string>));

            Assert.True(tuple9WithoutNames.IsTupleType);
            Assert.Equal("(System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32, System.String, System.Int32)",
                        tuple9WithoutNames.ToTestDisplayString());

            Assert.True(tuple9WithoutNames.TupleElementNames.IsDefault);

            Assert.Equal(new[] { "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32" },
                        tuple9WithoutNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
        }

        [Fact]
        public void CreateTupleTypeSymbol2_Tuple9WithNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef }); // no ValueTuple
            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var tuple9WithNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType, intType, stringType, intType, stringType, intType, stringType, intType),
                                                            ImmutableArray.Create("Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9"));

            Assert.True(tuple9WithNames.IsTupleType);
            Assert.Equal("(System.Int32 Alice1, System.String Alice2, System.Int32 Alice3, System.String Alice4, System.Int32 Alice5, System.String Alice6, System.Int32 Alice7, System.String Alice8, System.Int32 Alice9)",
                        tuple9WithNames.ToTestDisplayString());

            Assert.Equal(new[] { "Alice1", "Alice2", "Alice3", "Alice4", "Alice5", "Alice6", "Alice7", "Alice8", "Alice9" }, tuple9WithNames.TupleElementNames);

            Assert.Equal(new[] { "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32", "System.String", "System.Int32" },
                        tuple9WithNames.TupleElementTypes.Select(t => t.ToTestDisplayString()));
        }

        [Fact]
        public void CreateTupleTypeSymbol2_ElementTypeIsError()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);

            var tupleWithoutNames = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, ErrorTypeSymbol.UnknownResultType), default(ImmutableArray<string>));

            Assert.Equal(SymbolKind.NamedType, tupleWithoutNames.Kind);

            var types = tupleWithoutNames.TupleElementTypes;
            Assert.Equal(2, types.Length);
            Assert.Equal(SymbolKind.NamedType, types[0].Kind);
            Assert.Equal(SymbolKind.ErrorType, types[1].Kind);
        }

        [Fact]
        public void CreateTupleTypeSymbol2_BadNames()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);

            // illegal C# identifiers and blank
            var tuple2 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("123", ""));
            Assert.Equal(new[] { "123", "" }, tuple2.TupleElementNames);

            // reserved identifiers
            var tuple3 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType), ImmutableArray.Create("return", "class"));
            Assert.Equal(new[] { "return", "class" }, tuple3.TupleElementNames);
        }

        [Fact]
        public void CreateTupleTypeSymbol2_VisualBasicElements()
        {
            var vbSource = @"Public Class C
End Class";

            var vbComp = CreateVisualBasicCompilation("VB", vbSource,
                                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            vbComp.VerifyDiagnostics();
            ITypeSymbol vbType = (ITypeSymbol)vbComp.GlobalNamespace.GetMembers("C").Single();

            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef });
            INamedTypeSymbol intType = comp.GetSpecialType(SpecialType.System_String);

            Assert.Throws<ArgumentException>(() => comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, vbType), default(ImmutableArray<string>)));
        }

        [Fact]
        public void CreateTupleTypeSymbol_ComparingSymbols()
        {
            var source = @"
class C
{
    System.ValueTuple<int, int, int, int, int, int, int, (string a, string b)> F;
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            var tuple1 = comp.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<SourceMemberFieldSymbol>("F").Type;

            var intType = comp.GetSpecialType(SpecialType.System_Int32);
            var stringType = comp.GetSpecialType(SpecialType.System_String);

            var twoStrings = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(stringType, stringType);
            var twoStringsWithNames = (TypeSymbol)comp.CreateTupleTypeSymbol(twoStrings, ImmutableArray.Create("a", "b"));
            var tuple2Underlying = comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest).Construct(intType, intType, intType, intType, intType, intType, intType, twoStringsWithNames);
            var tuple2 = (TypeSymbol)comp.CreateTupleTypeSymbol(tuple2Underlying);

            var tuple3 = (TypeSymbol)comp.CreateTupleTypeSymbol(ImmutableArray.Create<ITypeSymbol>(intType, intType, intType, intType, intType, intType, intType, stringType, stringType),
                                                    ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "a", "b"));

            var tuple4 = (TypeSymbol)comp.CreateTupleTypeSymbol((INamedTypeSymbol)tuple1.TupleUnderlyingType, ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "a", "b"));

            Assert.True(tuple1.Equals(tuple2));
            Assert.True(tuple1.Equals(tuple2, ignoreDynamic: true));

            Assert.False(tuple1.Equals(tuple3));
            Assert.True(tuple1.Equals(tuple3, ignoreDynamic: true));

            Assert.False(tuple1.Equals(tuple4));
            Assert.True(tuple1.Equals(tuple4, ignoreDynamic: true));
        }

        [Fact]
        public void TupleMethodsOnNonTupleType()
        {
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef });
            INamedTypeSymbol intType = comp.GetSpecialType(SpecialType.System_String);

            Assert.False(intType.IsTupleType);
            Assert.True(intType.TupleElementNames.IsDefault);
            Assert.True(intType.TupleElementTypes.IsDefault);
        }

        [Fact]
        public void TupleTargetTypeAndConvert01()
        {
            var source = @"
class C
{
    static void Main()
    {
        // this works
        // (short, string) x1 = (1, ""hello"");

        // this does not
        (short, string) x2 = ((long, string))(1, ""hello"");

        // this does not
        (short a, string b) x3 = ((long c, string d))(1, ""hello"");
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (10,30): error CS0266: Cannot implicitly convert type '(long, string)' to '(short, string)'. An explicit conversion exists (are you missing a cast?)
                //         (short, string) x2 = ((long, string))(1, "hello");
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"((long, string))(1, ""hello"")").WithArguments("(long, string)", "(short, string)").WithLocation(10, 30),
                // (13,34): error CS0266: Cannot implicitly convert type '(long c, string d)' to '(short a, string b)'. An explicit conversion exists (are you missing a cast?)
                //         (short a, string b) x3 = ((long c, string d))(1, "hello");
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"((long c, string d))(1, ""hello"")").WithArguments("(long c, string d)", "(short a, string b)").WithLocation(13, 34)
            );
        }

        [Fact]
        public void TupleTargetTypeAndConvert02()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short, string) x2 = ((byte, string))(1, ""hello"");
        System.Console.WriteLine(x2);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
{1, hello}");
            
            comp.VerifyIL("C.Main()",
@"
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (System.ValueTuple<byte, string> V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldstr      ""hello""
  IL_0008:  call       ""System.ValueTuple<byte, string>..ctor(byte, string)""
  IL_000d:  ldloc.0
  IL_000e:  ldfld      ""byte System.ValueTuple<byte, string>.Item1""
  IL_0013:  ldloc.0
  IL_0014:  ldfld      ""string System.ValueTuple<byte, string>.Item2""
  IL_0019:  newobj     ""System.ValueTuple<short, string>..ctor(short, string)""
  IL_001e:  box        ""System.ValueTuple<short, string>""
  IL_0023:  call       ""void System.Console.WriteLine(object)""
  IL_0028:  ret
}
"); ;
        }

        [Fact]
        [WorkItem(11875, "https://github.com/dotnet/roslyn/issues/11875")]
        public void TupleImplicitConversionFail01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int) x;

        x = (null, null, null);
        x = (1, 1, 1);
        x = (1, ""string"");
        x = (1, 1, garbage);
        x = (1, 1, );
        x = (null, null);
        x = (1, null);
        x = (1, (t)=>t);
        x = null;
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,20): error CS1525: Invalid expression term ')'
                //         x = (1, 1, );
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(12, 20),
                // (8,13): error CS8206: Tuple with 3 elements cannot be converted to type '(int, int)'.
                //         x = (null, null, null);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, "(null, null, null)").WithArguments("3", "(int, int)").WithLocation(8, 13),
                // (9,13): error CS0029: Cannot implicitly convert type '(int, int, int)' to '(int, int)'
                //         x = (1, 1, 1);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1, 1, 1)").WithArguments("(int, int, int)", "(int, int)").WithLocation(9, 13),
                // (10,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         x = (1, "string");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""string""").WithArguments("string", "int").WithLocation(10, 17),
                // (11,20): error CS0103: The name 'garbage' does not exist in the current context
                //         x = (1, 1, garbage);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "garbage").WithArguments("garbage").WithLocation(11, 20),
                // (13,14): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = (null, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(13, 14),
                // (13,20): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = (null, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(13, 20),
                // (14,17): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = (1, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(14, 17),
                // (15,17): error CS1660: Cannot convert lambda expression to type 'int' because it is not a delegate type
                //         x = (1, (t)=>t);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "(t)=>t").WithArguments("lambda expression", "int").WithLocation(15, 17),
                // (16,13): error CS0037: Cannot convert null to '(int, int)' because it is a non-nullable value type
                //         x = null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("(int, int)").WithLocation(16, 13)
                );
        }

        [Fact]
        public void TupleExplicitConversionFail01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int) x;

        x = ((int, int))(null, null, null);
        x = ((int, int))(1, 1, 1);
        x = ((int, int))(1, ""string"");
        x = ((int, int))(1, 1, garbage);
        x = ((int, int))(1, 1, );
        x = ((int, int))(null, null);
        x = ((int, int))(1, null);
        x = ((int, int))(1, (t)=>t);
        x = ((int, int))null;
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular).VerifyDiagnostics(
                // (12,32): error CS1525: Invalid expression term ')'
                //         x = ((int, int))(1, 1, );
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(12, 32),
                // (8,13): error CS8206: Tuple with 3 elements cannot be converted to type '(int, int)'.
                //         x = ((int, int))(null, null, null);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, "((int, int))(null, null, null)").WithArguments("3", "(int, int)").WithLocation(8, 13),
                // (9,13): error CS0030: Cannot convert type '(int, int, int)' to '(int, int)'
                //         x = ((int, int))(1, 1, 1);
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "((int, int))(1, 1, 1)").WithArguments("(int, int, int)", "(int, int)").WithLocation(9, 13),
                // (10,29): error CS0030: Cannot convert type 'string' to 'int'
                //         x = ((int, int))(1, "string");
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""string""").WithArguments("string", "int").WithLocation(10, 29),
                // (11,32): error CS0103: The name 'garbage' does not exist in the current context
                //         x = ((int, int))(1, 1, garbage);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "garbage").WithArguments("garbage").WithLocation(11, 32),
                // (13,26): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = ((int, int))(null, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(13, 26),
                // (13,32): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = ((int, int))(null, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(13, 32),
                // (14,29): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = ((int, int))(1, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(14, 29),
                // (15,29): error CS1660: Cannot convert lambda expression to type 'int' because it is not a delegate type
                //         x = ((int, int))(1, (t)=>t);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "(t)=>t").WithArguments("lambda expression", "int").WithLocation(15, 29),
                // (16,13): error CS0037: Cannot convert null to '(int, int)' because it is a non-nullable value type
                //         x = ((int, int))null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "((int, int))null").WithArguments("(int, int)").WithLocation(16, 13)
                );
        }

        [Fact]
        [WorkItem(11875, "https://github.com/dotnet/roslyn/issues/11875")]
        public void TupleImplicitConversionFail02()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.ValueTuple<int, int> x;

        x = (null, null, null);
        x = (1, 1, 1);
        x = (1, ""string"");
        x = (1, 1, garbage);
        x = (1, 1, );
        x = (null, null);
        x = (1, null);
        x = (1, (t)=>t);
        x = null;
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,20): error CS1525: Invalid expression term ')'
                //         x = (1, 1, );
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(12, 20),
                // (8,13): error CS8206: Tuple with 3 elements cannot be converted to type '(int, int)'.
                //         x = (null, null, null);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, "(null, null, null)").WithArguments("3", "(int, int)").WithLocation(8, 13),
                // (9,13): error CS0029: Cannot implicitly convert type '(int, int, int)' to '(int, int)'
                //         x = (1, 1, 1);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1, 1, 1)").WithArguments("(int, int, int)", "(int, int)").WithLocation(9, 13),
                // (10,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         x = (1, "string");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""string""").WithArguments("string", "int").WithLocation(10, 17),
                // (11,20): error CS0103: The name 'garbage' does not exist in the current context
                //         x = (1, 1, garbage);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "garbage").WithArguments("garbage").WithLocation(11, 20),
                // (13,14): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = (null, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(13, 14),
                // (13,20): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = (null, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(13, 20),
                // (14,17): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = (1, null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(14, 17),
                // (15,17): error CS1660: Cannot convert lambda expression to type 'int' because it is not a delegate type
                //         x = (1, (t)=>t);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "(t)=>t").WithArguments("lambda expression", "int").WithLocation(15, 17),
                // (16,13): error CS0037: Cannot convert null to '(int, int)' because it is a non-nullable value type
                //         x = null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("(int, int)").WithLocation(16, 13)

            );
        }

        [Fact]
        [WorkItem(11875, "https://github.com/dotnet/roslyn/issues/11875")]
        public void TupleImplicitConversionFail03()
        {
            var source = @"
class C
{
    static void Main()
    {
        (string, string) x;

        x = (null, null, null);
        x = (1, 1, 1);
        x = (1, ""string"");
        x = (1, 1, garbage);
        x = (1, 1, );
        x = (null, null);
        x = (1, null);
        x = (1, (t)=>t);
        x = null;
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,20): error CS1525: Invalid expression term ')'
                //         x = (1, 1, );
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(12, 20),
                // (8,13): error CS8206: Tuple with 3 elements cannot be converted to type '(string, string)'.
                //         x = (null, null, null);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, "(null, null, null)").WithArguments("3", "(string, string)").WithLocation(8, 13),
                // (9,13): error CS0029: Cannot implicitly convert type '(int, int, int)' to '(string, string)'
                //         x = (1, 1, 1);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1, 1, 1)").WithArguments("(int, int, int)", "(string, string)").WithLocation(9, 13),
                // (10,14): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         x = (1, "string");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(10, 14),
                // (11,20): error CS0103: The name 'garbage' does not exist in the current context
                //         x = (1, 1, garbage);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "garbage").WithArguments("garbage").WithLocation(11, 20),
                // (14,14): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         x = (1, null);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(14, 14),
                // (15,14): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         x = (1, (t)=>t);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(15, 14),
                // (15,17): error CS1660: Cannot convert lambda expression to type 'string' because it is not a delegate type
                //         x = (1, (t)=>t);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "(t)=>t").WithArguments("lambda expression", "string").WithLocation(15, 17),
                // (16,13): error CS0037: Cannot convert null to '(string, string)' because it is a non-nullable value type
                //         x = null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("(string, string)").WithLocation(16, 13)
                );

        }

        [Fact]
        [WorkItem(11875, "https://github.com/dotnet/roslyn/issues/11875")]
        public void TupleImplicitConversionFail04()
        {
            var source = @"
class C
{
    static void Main()
    {
        ((int, int), int) x;

        x = ((null, null, null), 1);
        x = ((1, 1, 1), 1);
        x = ((1, ""string""), 1);
        x = ((1, 1, garbage), 1);
        x = ((1, 1, ), 1);
        x = ((null, null), 1);
        x = ((1, null), 1);
        x = ((1, (t)=>t), 1);
        x = (null, 1);
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,21): error CS1525: Invalid expression term ')'
                //         x = ((1, 1, ), 1);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(12, 21),
                // (8,14): error CS8206: Tuple with 3 elements cannot be converted to type '(int, int)'.
                //         x = ((null, null, null), 1);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, "(null, null, null)").WithArguments("3", "(int, int)").WithLocation(8, 14),
                // (9,14): error CS0029: Cannot implicitly convert type '(int, int, int)' to '(int, int)'
                //         x = ((1, 1, 1), 1);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1, 1, 1)").WithArguments("(int, int, int)", "(int, int)").WithLocation(9, 14),
                // (10,18): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         x = ((1, "string"), 1);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""string""").WithArguments("string", "int").WithLocation(10, 18),
                // (11,21): error CS0103: The name 'garbage' does not exist in the current context
                //         x = ((1, 1, garbage), 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "garbage").WithArguments("garbage").WithLocation(11, 21),
                // (13,15): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = ((null, null), 1);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(13, 15),
                // (13,21): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = ((null, null), 1);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(13, 21),
                // (14,18): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         x = ((1, null), 1);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(14, 18),
                // (15,18): error CS1660: Cannot convert lambda expression to type 'int' because it is not a delegate type
                //         x = ((1, (t)=>t), 1);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "(t)=>t").WithArguments("lambda expression", "int").WithLocation(15, 18),
                // (16,14): error CS0037: Cannot convert null to '(int, int)' because it is a non-nullable value type
                //         x = (null, 1);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("(int, int)").WithLocation(16, 14)

             );
        }

        [Fact]
        [WorkItem(11875, "https://github.com/dotnet/roslyn/issues/11875")]
        public void TupleImplicitConversionFail05()
        {
            var source = @"
class C
{
    static void Main()
    {
        (System.ValueTuple<int, int> x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9, int x10) x;

        x = (0,1,2,3,4,5,6,7,8,9,10);
        x = ((0, 0.0),1,2,3,4,5,6,7,8,9,10);
        x = ((0, 0),1,2,3,4,5,6,7,8,9,10,11);
        x = ((0, 0),1,2,3,4,5,6,7,8);
        x = ((0, 0),1,2,3,4,5,6,7,8,9.1,10);
        x = ((0, 0),1,2,3,4,5,6,7,8,;
        x = ((0, 0),1,2,3,4,5,6,7,8,9
        x = ((0, 0),1,2,3,4,oops,6,7,oopsss,9,10);

        x = ((0, 0),1,2,3,4,5,6,7,8,9);
        x = ((0, 0),1,2,3,4,5,6,7,8,(1,1,1), 10);
    }
}

"+ trivial2uple + trivialRemainingTuples; //intentionally not including 3-tuple for usesite errors

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (13,37): error CS1525: Invalid expression term ';'
                //         x = ((0, 0),1,2,3,4,5,6,7,8,;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(13, 37),
                // (13,37): error CS1026: ) expected
                //         x = ((0, 0),1,2,3,4,5,6,7,8,;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(13, 37),
                // (14,38): error CS1026: ) expected
                //         x = ((0, 0),1,2,3,4,5,6,7,8,9
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(14, 38),
                // (14,38): error CS1002: ; expected
                //         x = ((0, 0),1,2,3,4,5,6,7,8,9
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(14, 38),
                // (8,14): error CS0029: Cannot implicitly convert type 'int' to '(int, int)'
                //         x = (0,1,2,3,4,5,6,7,8,9,10);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "(int, int)").WithLocation(8, 14),
                // (9,18): error CS0029: Cannot implicitly convert type 'double' to 'int'
                //         x = ((0, 0.0),1,2,3,4,5,6,7,8,9,10);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0.0").WithArguments("double", "int").WithLocation(9, 18),
                // (10,13): error CS0029: Cannot implicitly convert type '((int, int), int, int, int, int, int, int, int, int, int, int, int)' to '((int, int) x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9, int x10)'
                //         x = ((0, 0),1,2,3,4,5,6,7,8,9,10,11);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "((0, 0),1,2,3,4,5,6,7,8,9,10,11)").WithArguments("((int, int), int, int, int, int, int, int, int, int, int, int, int)", "((int, int) x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9, int x10)").WithLocation(10, 13),
                // (11,13): error CS0029: Cannot implicitly convert type '((int, int), int, int, int, int, int, int, int, int)' to '((int, int) x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9, int x10)'
                //         x = ((0, 0),1,2,3,4,5,6,7,8);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "((0, 0),1,2,3,4,5,6,7,8)").WithArguments("((int, int), int, int, int, int, int, int, int, int)", "((int, int) x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9, int x10)").WithLocation(11, 13),
                // (12,37): error CS0029: Cannot implicitly convert type 'double' to 'int'
                //         x = ((0, 0),1,2,3,4,5,6,7,8,9.1,10);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "9.1").WithArguments("double", "int").WithLocation(12, 37),
                // (13,13): error CS0518: Predefined type 'System.ValueTuple`3' is not defined or imported
                //         x = ((0, 0),1,2,3,4,5,6,7,8,;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "((0, 0),1,2,3,4,5,6,7,8,").WithArguments("System.ValueTuple`3").WithLocation(13, 13),
                // (14,13): error CS0518: Predefined type 'System.ValueTuple`3' is not defined or imported
                //         x = ((0, 0),1,2,3,4,5,6,7,8,9
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"((0, 0),1,2,3,4,5,6,7,8,9
").WithArguments("System.ValueTuple`3").WithLocation(14, 13),
                // (15,29): error CS0103: The name 'oops' does not exist in the current context
                //         x = ((0, 0),1,2,3,4,oops,6,7,oopsss,9,10);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "oops").WithArguments("oops").WithLocation(15, 29),
                // (15,38): error CS0103: The name 'oopsss' does not exist in the current context
                //         x = ((0, 0),1,2,3,4,oops,6,7,oopsss,9,10);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "oopsss").WithArguments("oopsss").WithLocation(15, 38),
                // (17,13): error CS0518: Predefined type 'System.ValueTuple`3' is not defined or imported
                //         x = ((0, 0),1,2,3,4,5,6,7,8,9);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "((0, 0),1,2,3,4,5,6,7,8,9)").WithArguments("System.ValueTuple`3").WithLocation(17, 13),
                // (17,13): error CS0029: Cannot implicitly convert type '((int, int), int, int, int, int, int, int, int, int, int)' to '((int, int) x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9, int x10)'
                //         x = ((0, 0),1,2,3,4,5,6,7,8,9);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "((0, 0),1,2,3,4,5,6,7,8,9)").WithArguments("((int, int), int, int, int, int, int, int, int, int, int)", "((int, int) x0, int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9, int x10)").WithLocation(17, 13),
                // (18,37): error CS0518: Predefined type 'System.ValueTuple`3' is not defined or imported
                //         x = ((0, 0),1,2,3,4,5,6,7,8,(1,1,1), 10);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(1,1,1)").WithArguments("System.ValueTuple`3").WithLocation(18, 37),
                // (18,37): error CS0029: Cannot implicitly convert type '(int, int, int)' to 'int'
                //         x = ((0, 0),1,2,3,4,5,6,7,8,(1,1,1), 10);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1,1,1)").WithArguments("(int, int, int)", "int").WithLocation(18, 37)

            );
        }

        [Fact]
        [WorkItem(11875, "https://github.com/dotnet/roslyn/issues/11875")]
        public void TupleImplicitConversionFail06()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Func<string> l = ()=>1; // reference

        (string, Func<string>) x = (null, ()=>1);  // actual error, should be the same as above.

        Func<(string, string)> l1 = ()=>(null, 1.1); // reference

        (string, Func<(string, string)>) x1 = (null, ()=>(null, 1.1));  // actual error, should be the same as above.
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,30): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         Func<string> l = ()=>1; // reference
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(8, 30),
                // (8,30): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<string> l = ()=>1; // reference
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "1").WithArguments("lambda expression").WithLocation(8, 30),
                // (10,47): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (string, Func<string>) x = (null, ()=>1);  // actual error, should be the same as above.
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(10, 47),
                // (10,47): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         (string, Func<string>) x = (null, ()=>1);  // actual error, should be the same as above.
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "1").WithArguments("lambda expression").WithLocation(10, 47),
                // (12,48): error CS0029: Cannot implicitly convert type 'double' to 'string'
                //         Func<(string, string)> l1 = ()=>(null, 1.1); // reference
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1.1").WithArguments("double", "string").WithLocation(12, 48),
                // (12,41): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<(string, string)> l1 = ()=>(null, 1.1); // reference
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "(null, 1.1)").WithArguments("lambda expression").WithLocation(12, 41),
                // (14,65): error CS0029: Cannot implicitly convert type 'double' to 'string'
                //         (string, Func<(string, string)>) x1 = (null, ()=>(null, 1.1));  // actual error, should be the same as above.
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1.1").WithArguments("double", "string").WithLocation(14, 65),
                // (14,58): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         (string, Func<(string, string)>) x1 = (null, ()=>(null, 1.1));  // actual error, should be the same as above.
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "(null, 1.1)").WithArguments("lambda expression").WithLocation(14, 58)

            );
        }

        [Fact]
        public void TupleExplicitConversionFail06()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Func<string> l = (Func<string>)(()=>1); // reference

        (string, Func<string>) x = ((string, Func<string>))(null, () => 1);  // actual error, should be the same as above.

        Func<(string, string)> l1 = (Func<(string, string)>)(()=>(null, 1.1)); // reference

        (string, Func<(string, string)>) x1 = ((string, Func<(string, string)>))(null, () => (null, 1.1));  // actual error, should be the same as above.
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular).VerifyDiagnostics(
                // (8,45): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         Func<string> l = (Func<string>)(()=>1); // reference
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(8, 45),
                // (8,45): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<string> l = (Func<string>)(()=>1); // reference
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "1").WithArguments("lambda expression").WithLocation(8, 45),
                // (10,73): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         (string, Func<string>) x = ((string, Func<string>))(null, () => 1);  // actual error, should be the same as above.
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(10, 73),
                // (10,73): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         (string, Func<string>) x = ((string, Func<string>))(null, () => 1);  // actual error, should be the same as above.
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "1").WithArguments("lambda expression").WithLocation(10, 73),
                // (12,73): error CS0029: Cannot implicitly convert type 'double' to 'string'
                //         Func<(string, string)> l1 = (Func<(string, string)>)(()=>(null, 1.1)); // reference
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1.1").WithArguments("double", "string").WithLocation(12, 73),
                // (12,66): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<(string, string)> l1 = (Func<(string, string)>)(()=>(null, 1.1)); // reference
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "(null, 1.1)").WithArguments("lambda expression").WithLocation(12, 66),
                // (14,101): error CS0029: Cannot implicitly convert type 'double' to 'string'
                //         (string, Func<(string, string)>) x1 = ((string, Func<(string, string)>))(null, () => (null, 1.1));  // actual error, should be the same as above.
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1.1").WithArguments("double", "string").WithLocation(14, 101),
                // (14,94): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         (string, Func<(string, string)>) x1 = ((string, Func<(string, string)>))(null, () => (null, 1.1));  // actual error, should be the same as above.
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "(null, 1.1)").WithArguments("lambda expression").WithLocation(14, 94)
            );
        }

        [Fact]
        public void TupleTargetTypeLambda()
        {
            var source = @"

using System;

class C
{
    static void Test(Func<Func<(short, short)>> d)
    {
        Console.WriteLine(""short"");
    }

    static void Test(Func<Func<(byte, byte)>> d)
    {
        Console.WriteLine(""byte"");
    }

    static void Main()
    {
        // this works because of identity conversion
        Test( ()=>()=>((byte, byte))(1,1)) ;

        // this works because of the betterness of the targets
        Test(()=>()=>(1,1));
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
byte
byte
");
        }

        [Fact]
        public void TupleTargetTypeLambda1()
        {
            var source = @"

using System;

class C
{
    static void Test(Func<(Func<short>, int)> d)
    {
        Console.WriteLine(""short"");
    }

    static void Test(Func<(Func<byte>, int)> d)
    {
        Console.WriteLine(""byte"");
    }

    static void Main()
    {
        // this works
        Test(()=>(()=>(byte)1, 1));

        // this does not
        Test(()=>(()=>1, 1));
    }
}
";

            CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs).VerifyDiagnostics(
                // (23,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.Test(Func<(Func<short>, int)>)' and 'C.Test(Func<(Func<byte>, int)>)'
                //         Test(()=>(()=>1, 1));
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("C.Test(System.Func<(System.Func<short>, int)>)", "C.Test(System.Func<(System.Func<byte>, int)>)").WithLocation(23, 9)
            );
        }

        [Fact]
        public void TargetTypingOverload01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((null, null));
        Test((1, 1));
        Test((()=>7, ()=>8), 2);
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>) x, T y)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
second
first
third
7
");
        }

        [Fact]
        public void TargetTypingOverload02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((()=>7, ()=>8));
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>) x)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
third
7
");
        }

        [Fact]
        public void TargetTypingNullable01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = M1();
        Test(x);
    }

    static (int a, double b)? M1()
    {
        return (1, 2);
    }

    static void Test<T>(T arg)
    {
        System.Console.WriteLine(typeof(T));
        System.Console.WriteLine(arg);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
System.Nullable`1[System.ValueTuple`2[System.Int32,System.Double]]
(1, 2)
");
        }

        [Fact]
        public void TargetTypingOverload01Long()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((null, null, null, null, null, null, null, null, null, null));
        Test((1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
        Test((()=>7, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8, ()=>8), 2);
    }

    static void Test<T>((T, T, T, T, T, T, T, T, T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object, object, object, object, object, object, object, object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>, Func<T>) x, T y)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
";

            var comp = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, expectedOutput: @"
second
first
third
7
");
        }

        [Fact]
        public void TargetTypingNullable02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = M1();
        Test(x);
    }

    static (int a, string b)? M1()
    {
        return (1, null);
    }

    static void Test<T>(T arg)
    {
        System.Console.WriteLine(typeof(T));
        System.Console.WriteLine(arg);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
System.Nullable`1[System.ValueTuple`2[System.Int32,System.String]]
(1, )
");
        }

        [Fact]
        public void TargetTypingNullable02Long()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = M1();
        System.Console.WriteLine(x?.a);
        System.Console.WriteLine(x?.a8);
        Test(x);
    }

    static (int a, string b, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8)? M1()
    {
        return (1, null, 1, 2, 3, 4, 5, 6, 7, 8);
    }

    static void Test<T>(T arg)
    {
        System.Console.WriteLine(arg);
    }
}
";
            var comp = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, expectedOutput:
@"1
8
(1, , 1, 2, 3, 4, 5, 6, 7, 8)
");
        }

        [Fact]
        public void TargetTypingNullableOverload()
        {
            var source = @"
class C
{
    static void Main()
    {
        Test((null, null, null, null, null, null, null, null, null, null));
        Test((""a"", ""a"", ""a"", ""a"", ""a"", ""a"", ""a"", ""a"", ""a"", ""a""));
        Test((1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
    }

    static void Test((string, string, string, string, string, string, string, string, string, string) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((string, string, string, string, string, string, string, string, string, string)? x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test((int, int, int, int, int, int, int, int, int, int)? x)
    {
        System.Console.WriteLine(""third"");
    }

    static void Test((int, int, int, int, int, int, int, int, int, int) x)
    {
        System.Console.WriteLine(""fourth"");
    }
}
";

            var comp = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, expectedOutput: @"
first
first
fourth
");
        }

        [Fact]
        public void TupleConversion01()
        {
            var source = @"
class C
{
    static void Main()
    {
        // error must mention   (long c, long d)
        (int a, int b) x1 = ((long c, long d))(e: 1, f:2);
        // error must mention   (int c, long d)
        (short a, short b) x2 = ((int c, int d))(e: 1, f:2);

        // error must mention   (int e, string f)
        (int a, int b) x3 = ((long c, long d))(e: 1, f:""qq"");
    }
}
" + trivial2uple;

            CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }).VerifyDiagnostics(
                // (7,29): error CS0266: Cannot implicitly convert type '(long c, long d)' to '(int a, int b)'. An explicit conversion exists (are you missing a cast?)
                //         (int a, int b) x1 = ((long c, long d))(e: 1, f:2);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "((long c, long d))(e: 1, f:2)").WithArguments("(long c, long d)", "(int a, int b)").WithLocation(7, 29),
                // (9,33): error CS0266: Cannot implicitly convert type '(int c, int d)' to '(short a, short b)'. An explicit conversion exists (are you missing a cast?)
                //         (short a, short b) x2 = ((int c, int d))(e: 1, f:2);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "((int c, int d))(e: 1, f:2)").WithArguments("(int c, int d)", "(short a, short b)").WithLocation(9, 33),
                // (12,56): error CS0030: Cannot convert type 'string' to 'long'
                //         (int a, int b) x3 = ((long c, long d))(e: 1, f:"qq");
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"""qq""").WithArguments("string", "long").WithLocation(12, 56)
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/11288")]
        [WorkItem(11288, "https://github.com/dotnet/roslyn/issues/11288")]
        public void TupleConversion02()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, int b) x4 = ((long c, long d))(1, null, 2);
    }
}
" + trivial2uple + trivial3uple;

            CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }).VerifyDiagnostics(
            );
        }

        [Fact]
        public void TupleConvertedType01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b)? x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitNullable, model.GetConversion(node).Kind);

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType01insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        // explicit conversion exists, cast in the code picks that.
        (short a, string b)? x = ((short c, string d)?)(e: 1, f: ""hello"");
        short? y = (short?)11;
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var l11 = nodes.OfType<LiteralExpressionSyntax>().ElementAt(2);

            Assert.Equal(@"11", l11.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(l11).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(l11).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(l11));

            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d)?)(e: 1, f: ""hello"")
            Assert.Equal("(System.Int16 c, System.String d)?", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType01insourceImplicit()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b)? x =(1, ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(1, ""hello"")", node.ToString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("(System.Int32, System.String)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitNullable, model.GetConversion(node).Kind);

            CompileAndVerify(comp);
        }

        [Fact]
        public void TupleConvertedType02()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b)? x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitNullable, model.GetConversion(node).Kind);

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType02insource00()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b)? x = ((short c, string d))(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitTupleLiteral, model.GetConversion(node).Kind);

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d))(e: 1, f: ""hello"")
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)?", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitNullable, model.GetConversion(node.Parent).Kind);


            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType02insource01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (e: 1, f: ""hello"");
        (object a, string b) x1 = ((long c, string d))(x);
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<ParenthesizedExpressionSyntax>().Single().Parent;

            Assert.Equal(@"((long c, string d))(x)", node.ToString());
            Assert.Equal("(System.Int64 c, System.String d)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Object a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTuple, model.GetConversion(node).Kind);

            node = nodes.OfType<ParenthesizedExpressionSyntax>().Single();

            Assert.Equal(@"(x)", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));
        }

        [Fact]
        public void TupleConvertedType02insource02()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (e: 1, f: ""hello"");
        (object a, string b)? x1 = ((long c, string d))(x);
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<ParenthesizedExpressionSyntax>().Single().Parent;

            Assert.Equal(@"((long c, string d))(x)", node.ToString());
            Assert.Equal("(System.Int64 c, System.String d)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Object a, System.String b)?", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitNullable, model.GetConversion(node).Kind);

            node = nodes.OfType<ParenthesizedExpressionSyntax>().Single();

            Assert.Equal(@"(x)", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));
        }

        [Fact]
        public void TupleConvertedType03()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b)? x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.String b)?", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitNullable, model.GetConversion(node).Kind);

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType03insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b)? x = ((int c, string d)?)(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((int c, string d)?)(e: 1, f: ""hello"")
            Assert.Equal("(System.Int32 c, System.String d)?", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.String b)?", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType04()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b)? x = ((int c, string d))(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((int c, string d))(e: 1, f: ""hello"")
            Assert.Equal("(System.Int32 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.String b)?", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitNullable, model.GetConversion(node.Parent).Kind);

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b)? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType05()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b) x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType05insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, string b) x = ((int c, string d))(e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int32 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType06()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = (e: 1, f: ""hello"");
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(node).Kind);

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedType06insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = ((short c, string d))(e: 1, f: ""hello"");
        short y = (short)11;
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var l11 = nodes.OfType<LiteralExpressionSyntax>().ElementAt(2);

            Assert.Equal(@"11", l11.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(l11).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(l11).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(l11));

            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: ""hello"")", node.ToString());
            Assert.Equal("(System.Int32 e, System.String f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitTupleLiteral, model.GetConversion(node).Kind);

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d))(e: 1, f: ""hello"")
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedTypeNull01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = (e: 1, f: null);
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: null)", node.ToString());
            Assert.Null(model.GetTypeInfo(node).Type);
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(node).Kind);

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedTypeNull01insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = ((short c, string d))(e: 1, f: null);
        string y = (string)null;
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var lnull = nodes.OfType<LiteralExpressionSyntax>().ElementAt(2);

            Assert.Equal(@"null", lnull.ToString());
            Assert.Null(model.GetTypeInfo(lnull).Type);
            Assert.Null(model.GetTypeInfo(lnull).ConvertedType);
            Assert.Equal(Conversion.Identity, model.GetConversion(lnull));

            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: null)", node.ToString());
            Assert.Null(model.GetTypeInfo(node).Type);
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitTupleLiteral, model.GetConversion(node).Kind);

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d))(e: 1, f: null)
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact]
        public void TupleConvertedTypeUDC01()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = (e: 1, f: new C1(""qq""));
        System.Console.WriteLine(x.ToString());
    }

    class C1
    {
        private string s;

        public C1(string arg)
        {
            s = arg + 1;
        }

        public static implicit operator string (C1 arg)
        {
            return arg.s;
        }
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: new C1(""qq""))", node.ToString());
            Assert.Equal("(System.Int32 e, C.C1 f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(node).Kind);

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "{1, qq1}");
        }

        [Fact]
        public void TupleConvertedTypeUDC01insource()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short a, string b) x = ((short c, string d))(e: 1, f: new C1(""qq""));
        System.Console.WriteLine(x.ToString());
    }

    class C1
    {
        private string s;

        public C1(string arg)
        {
            s = arg + 1;
        }

        public static implicit operator string (C1 arg)
        {
            return arg.s;
        }
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(e: 1, f: new C1(""qq""))", node.ToString());
            Assert.Equal("(System.Int32 e, C.C1 f)", model.GetTypeInfo(node).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ExplicitTupleLiteral, model.GetConversion(node).Kind);

            // semantic model returns topmost conversion from the sequence of conversions for
            // ((short c, string d))(e: 1, f: null)
            Assert.Equal("(System.Int16 c, System.String d)", model.GetTypeInfo(node.Parent).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16 a, System.String b)", model.GetTypeInfo(node.Parent).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(node.Parent));

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            Assert.Equal("(System.Int16 a, System.String b) x", model.GetDeclaredSymbol(x).ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "{1, qq1}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/11289")]
        [WorkItem(11289, "https://github.com/dotnet/roslyn/issues/11289")]
        public void TupleConvertedTypeUDC02()
        {
            var source = @"
class C
{
    static void Main()
    {
        C1 x = (1, ""qq"");
        System.Console.WriteLine(x.ToString());
    }

    class C1
    {
        private (byte, string) val;

        private C1((byte, string) arg)
        {
            val = arg;
        }

        public static implicit operator C1 ((byte, string) arg)
        {
            return new C1(arg);
        }

        public override string ToString()
        {
            return val.ToString();       
        }
    }
}
" + trivial2uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(1, ""qq"")", node.ToString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("(System.Int32, System.String)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("C.C1", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitUserDefined, model.GetConversion(node).Kind);

            CompileAndVerify(comp, expectedOutput: "{1, qq}");
        }

        [Fact]
        public void TupleConvertedTypeUDC03()
        {
            var source = @"
class C
{
    static void Main()
    {
        C1 x = (""1"", ""qq"");
        System.Console.WriteLine(x.ToString());
    }

    class C1
    {
        private (byte, string) val;

        private C1((byte, string) arg)
        {
            val = arg;
        }

        public static implicit operator C1 ((byte, string) arg)
        {
            return new C1(arg);
        }

        public override string ToString()
        {
            return val.ToString();       
        }
    }
}
";

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree,
                references: s_valueTupleRefs,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (6,16): error CS0029: Cannot implicitly convert type '(string, string)' to 'C.C1'
                //         C1 x = ("1", "qq");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"(""1"", ""qq"")").WithArguments("(string, string)", "C.C1").WithLocation(6, 16)
                );

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(""1"", ""qq"")", node.ToString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("(System.String, System.String)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("C.C1", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.NoConversion, model.GetConversion(node));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/11289")]
        [WorkItem(11289, "https://github.com/dotnet/roslyn/issues/11289")]
        public void TupleConvertedTypeUDC04()
        {
            var source = @"
public class C
{
    static void Main()
    {
        C1 x = (1, ""qq"");
        System.Console.WriteLine(x.ToString());
    }

    public class C1
    {
        private (byte, string) val;

        public C1((byte, string) arg)
        {
            val = arg;
        }

        public override string ToString()
        {
            return val.ToString();       
        }
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public static implicit operator C.C1 ((T1, T2) arg)
        {
            return new C.C1(((byte)(int)(object)arg.Item1, (string)(object)arg.Item2));
        }
    }
}
";

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().First();

            Assert.Equal(@"(1, ""qq"")", node.ToString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("(System.Int32, System.String)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("C.C1", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitUserDefined, model.GetConversion(node).Kind);

            CompileAndVerify(comp, expectedOutput: "{1, qq}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/11289")]
        [WorkItem(11289, "https://github.com/dotnet/roslyn/issues/11289")]
        public void TupleConvertedTypeUDC05()
        {
            var source = @"
public class C
{
    static void Main()
    {
        C1 x = (1, ""qq"");
        System.Console.WriteLine(x.ToString());
    }

    public class C1
    {
        private (byte, string) val;

        public C1((byte, string) arg)
        {
            val = arg;
        }

        public static implicit operator C1 ((byte, string) arg)
        {
            System.Console.WriteLine(""C1"");
            return new C1(arg);
        }

        public override string ToString()
        {
            return val.ToString();       
        }
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public static implicit operator C.C1 ((T1, T2) arg)
        {
            System.Console.WriteLine(""VT"");
            return new C.C1(((byte)(int)(object)arg.Item1, (string)(object)arg.Item2));
        }
    }
}
";

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().First();

            Assert.Equal(@"(1, ""qq"")", node.ToString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("(System.Int32, System.String)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("C.C1", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitUserDefined, model.GetConversion(node).Kind);

            CompileAndVerify(comp, expectedOutput:
@"VT
{1, qq}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/11289")]
        [WorkItem(11289, "https://github.com/dotnet/roslyn/issues/11289")]
        public void TupleConvertedTypeUDC06()
        {
            var source = @"
public class C
{
    static void Main()
    {
        C1 x = (1, null);
        System.Console.WriteLine(x.ToString());
    }

    public class C1
    {
        private (byte, string) val;

        public C1((byte, string) arg)
        {
            val = arg;
        }

        public static implicit operator C1 ((byte, string) arg)
        {
            System.Console.WriteLine(""C1"");
            return new C1(arg);
        }

        public override string ToString()
        {
            return val.ToString();       
        }
    }
}

namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public static implicit operator C.C1 ((T1, T2) arg)
        {
            System.Console.WriteLine(""VT"");
            return new C.C1(((byte)(int)(object)arg.Item1, (string)(object)arg.Item2));
        }
    }
}
";

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().First();

            Assert.Equal(@"(1, null)", node.ToString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Null(typeInfo.Type);
            Assert.Equal("C.C1", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitUserDefined, model.GetConversion(node).Kind);

            CompileAndVerify(comp, expectedOutput:
@"C1
{1, }");
        }

        [Fact]
        public void TupleConvertedTypeUDC07()
        {
            var source = @"
class C
{
    static void Main()
    {
        C1 x = M1();
        System.Console.WriteLine(x.ToString());
    }

    static (int, string) M1()
    {
        return (1, ""qq"");
    }

    class C1
    {
        private (byte, string) val;

        private C1((byte, string) arg)
        {
            val = arg;
        }

        public static implicit operator C1 ((byte, string) arg)
        {
            return new C1(arg);
        }
    }
}
";

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree,
                references: s_valueTupleRefs,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (6,16): error CS0029: Cannot implicitly convert type '(int, string)' to 'C.C1'
                //         C1 x = M1();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "M1()").WithArguments("(int, string)", "C.C1").WithLocation(6, 16)
                );
        }

        [Fact]
        public void Inference01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((null, null));
        Test((1, 1));
        Test((()=>7, ()=>8), 2);
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>) x, T y)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
second
first
third
7
");
        }

        [Fact]
        public void Inference02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((()=>7, ()=>8));
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<T>, Func<T>) x)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1().ToString());
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
third
7
");
        }

        [Fact]
        public void Inference03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test(((x)=>x, (x)=>x));
    }

    static void Test<T>((T, T) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((object, object) x)
    {
        System.Console.WriteLine(""second"");
    }

    static void Test<T>((Func<int, T>, Func<T, T>) x)
    {
        System.Console.WriteLine(""third"");
        System.Console.WriteLine(x.Item1(5).ToString());
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"
third
5
");
        }

        [Fact]
        public void Inference04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test( (x)=>x.y );
        Test( (x)=>x.bob );
    }

    static void Test<T>( Func<(byte x, byte y), T> x)
    {
        System.Console.WriteLine(""first"");
        System.Console.WriteLine(x((2,3)).ToString());
    }

    static void Test<T>( Func<(int alice, int bob), T> x)
    {
        System.Console.WriteLine(""second"");
        System.Console.WriteLine(x((4,5)).ToString());
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
first
3
second
5
");
        }

        [Fact]
        public void Inference05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test( ((x)=>x.x, (x)=>x.Item2) );
        Test( ((x)=>x.bob, (x)=>x.Item1) );
    }

    static void Test<T>( (Func<(byte x, byte y), T> f1, Func<(int, int), T> f2) x)
    {
        System.Console.WriteLine(""first"");
        System.Console.WriteLine(x.f1((2,3)).ToString());
        System.Console.WriteLine(x.f2((2,3)).ToString());
    }

    static void Test<T>( (Func<(int alice, int bob), T> f1, Func<(int, int), T> f2) x)
    {
        System.Console.WriteLine(""second"");
        System.Console.WriteLine(x.f1((4,5)).ToString());
        System.Console.WriteLine(x.f2((4,5)).ToString());
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
first
2
3
second
5
4
");
        }

        [WorkItem(10801, "https://github.com/dotnet/roslyn/issues/10801")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/10801")]
        public void Inference06()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        M1((() => ""qq"", null));
    }

    static void M1((Func<object> f, object o) a)
    {
        System.Console.WriteLine(1);
    }

    static void M1((Func<string> f, object o) a)
    {
        System.Console.WriteLine(2);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
2
");
        }

        [Fact]
        public void Inference07()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Test((x) => (x, x), (t) => 1);
        Test1((x) => (x, x), (t) => 1);
        Test2((a: 1, b: 2), (t) => (t.a, t.b));
    }

    static void Test<U>(Func<int, ValueTuple<U, U>> f1, Func<ValueTuple<U, U>, int> f2)
    {
        System.Console.WriteLine(f2(f1(1)));
    }
    static void Test1<U>(Func<int, (U, U)> f1, Func<(U, U), int> f2)
    {
        System.Console.WriteLine(f2(f1(1)));
    }
    static void Test2<U, T>(U f1, Func<U, (T x, T y)> f2)
    {
        System.Console.WriteLine(f2(f1).y);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
1
1
2
");
        }

        [Fact]
        public void Inference08()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        Test1((a: 1, b: 2), (c: 3, d: 4));
        Test2((a: 1, b: 2), (c: 3, d: 4), t => t.Item2);
        Test2((a: 1, b: 2), (a: 3, b: 4), t => t.a);
        Test2((a: 1, b: 2), (c: 3, d: 4), t => t.a);
    }

    static void Test1<T>(T x, T y)
    {
        System.Console.WriteLine(""test1"");
        System.Console.WriteLine(x);
    }

    static void Test2<T>(T x, T y, Func<T, int> f)
    {
        System.Console.WriteLine(""test2_1"");
        System.Console.WriteLine(f(x));
    }

    static void Test2<T>(T x, object y, Func<T, int> f)
    {
        System.Console.WriteLine(""test2_2"");
        System.Console.WriteLine(f(x));
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
test1
{1, 2}
test2_1
2
test2_1
1
test2_2
1
");
        }

        [Fact]
        public void Inference08t()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        var ab = (a: 1, b: 2);
        var cd = (c: 3, d: 4);

        Test1(ab, cd);
        Test2(ab, cd, t => t.Item2);
        Test2(ab, ab, t => t.a);
        Test2(ab, cd, t => t.a);
    }

    static void Test1<T>(T x, T y)
    {
        System.Console.WriteLine(""test1"");
        System.Console.WriteLine(x);
    }

    static void Test2<T>(T x, T y, Func<T, int> f)
    {
        System.Console.WriteLine(""test2_1"");
        System.Console.WriteLine(f(x));
    }

    static void Test2<T>(T x, object y, Func<T, int> f)
    {
        System.Console.WriteLine(""test2_2"");
        System.Console.WriteLine(f(x));
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
test1
{1, 2}
test2_1
2
test2_1
1
test2_2
1
");
        }

        [Fact]
        public void Inference09()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        // byval tuple, as a whole, sets a lower bound
        Test1((a: 1, b: 2), (ValueType)1);
    }

    static void Test1<T>(T x, T y)
    {
        System.Console.WriteLine(typeof(T));
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
System.ValueType
");
        }

        [Fact]
        public void Inference10()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        // byref tuple, as a whole, sets an exact bound
        var t = (a: 1, b: 2);
        Test1(ref t, (ValueType)1);
    }

    static void Test1<T>(ref T x, T y)
    {
        System.Console.WriteLine(typeof(T));
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (10,9): error CS0411: The type arguments for method 'C.Test1<T>(ref T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Test1(ref t, (ValueType)1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("C.Test1<T>(ref T, T)").WithLocation(10, 9)
                );
        }

        [WorkItem(10800, "https://github.com/dotnet/roslyn/issues/10800")]
        [Fact]
        public void Inference11()
        {
            var source = @"
class C
{
    static void Main()
    {
        // byref tuple, as a whole, sets an exact bound, names are honored (just like in the case of dynamic
        var ab = (a: 1, b: 2);
        var cd = (c: 1, d: 2);

        // this is an error  since we have two exact bounds
        Test3(ref ab, ref cd);

        // these are ok, since one of the bounds is a lower bound
        Test1(ref ab, cd);
        Test2(ab, ref cd);
}

    static void Test1<T>(ref T x, T y)
    {
        System.Console.WriteLine(typeof(T));
    }

    static void Test2<T>(T x, ref T y)
    {
        System.Console.WriteLine(typeof(T));
    }

    static void Test3<T>(ref T x, ref T y)
    {
        System.Console.WriteLine(typeof(T));
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (11,9): error CS0411: The type arguments for method 'C.Test3<T>(ref T, ref T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Test3(ref ab, ref cd);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test3").WithArguments("C.Test3<T>(ref T, ref T)").WithLocation(11, 9)
                );
        }

        [Fact]
        public void Inference12()
        {
            var source = @"
class C
{
    static void Main()
    {
        // nested tuple literals set lower bounds
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (object)1));
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (c: 1, d: 2)));
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (1, 2)));
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (a: 1, b: 2)));
}

    static void Test1<T, U>((T, U) x, (T, U) y)
    {
        System.Console.WriteLine(typeof(U));
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
System.Object
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
");
        }

        [Fact]
        public void Inference13()
        {
            var source = @"
class C
{
    static void Main()
    {
        // nested tuple literals set lower bounds
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (object)1));
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (c: 1, d: 2)));
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (1, 2)));
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (a: 1, b: 2)));
}

    static void Test1<T, U>((T, U)? x, (T, U) y)
    {
        System.Console.WriteLine(typeof(U));
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
System.Object
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
System.ValueTuple`2[System.Int32,System.Int32]
");
        }

        [Fact]
        public void Inference14()
        {
            var source = @"
class C
{
    static void Main()
    {
        // nested tuple literals set lower bounds
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (c: 1, d: 2)));
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (1, 2)));
        Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (a: 1, b: 2)));
}

    static void Test1<T, U>((T, U)? x, (T, U?) y) where U: struct
    {
        System.Console.WriteLine(typeof(U));
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (7,9): error CS0411: The type arguments for method 'C.Test1<T, U>((T, U)?, (T, U?))' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (c: 1, d: 2)));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("C.Test1<T, U>((T, U)?, (T, U?))").WithLocation(7, 9),
                // (8,9): error CS0411: The type arguments for method 'C.Test1<T, U>((T, U)?, (T, U?))' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (1, 2)));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("C.Test1<T, U>((T, U)?, (T, U?))").WithLocation(8, 9),
                // (9,9): error CS0411: The type arguments for method 'C.Test1<T, U>((T, U)?, (T, U?))' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Test1((a: 1, b: (a: 1, b: 2)), (a: 1, b: (a: 1, b: 2)));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test1").WithArguments("C.Test1<T, U>((T, U)?, (T, U?))").WithLocation(9, 9)
                );
        }

        [Fact]
        public void Inference15()
        {
            var source = @"
class C
{
    static void Main()
    {
        Test1((a: ""q"", b: null), (a: null, b: ""w""), (x) => x.z);
    }

    static void Test1<T, U>((T, U) x, (T, U) y, System.Func<(T x, U z), T> f)
    {
        System.Console.WriteLine(typeof(U));
        System.Console.WriteLine(f(y));
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
System.String
w
");
        }

        [Fact]
        public void Inference16()
        {
            var source = @"
class C
{
    static void Main()
    {
        // tuples set lower bounds
        var x = (1,2,3);
        Test(x);

        var x1 = (1,2,(long)3);
        Test(x1);

        var x2 = (1,(object)2,(long)3);
        Test(x2);
    }

    static void Test<T>((T, T, T) x)
    {
        System.Console.WriteLine(typeof(T));
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
System.Int32
System.Int64
System.Object
");
        }

        [Fact]
        public void RestrictedTypes1()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 2, new System.ArgIterator());
        (int x, object y) y = (1, 2, new System.ArgIterator());
        (int x, System.ArgIterator y) z = (1, 2, new System.ArgIterator());
    }
}
" + trivial2uple + trivial3uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,24): error CS0610: Field or property cannot be of type 'ArgIterator'
                //         var x = (1, 2, new System.ArgIterator());
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "new System.ArgIterator()").WithArguments("System.ArgIterator").WithLocation(6, 24),
                // (7,38): error CS0610: Field or property cannot be of type 'ArgIterator'
                //         (int x, object y) y = (1, 2, new System.ArgIterator());
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "new System.ArgIterator()").WithArguments("System.ArgIterator").WithLocation(7, 38),
                // (8,17): error CS0610: Field or property cannot be of type 'ArgIterator'
                //         (int x, System.ArgIterator y) z = (1, 2, new System.ArgIterator());
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator y").WithArguments("System.ArgIterator").WithLocation(8, 17),
                // (8,50): error CS0610: Field or property cannot be of type 'ArgIterator'
                //         (int x, System.ArgIterator y) z = (1, 2, new System.ArgIterator());
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "new System.ArgIterator()").WithArguments("System.ArgIterator").WithLocation(8, 50)
                );
        }

        [Fact]
        public void RestrictedTypes2()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int x, System.ArgIterator y) y;
    }
}
" + trivial2uple + trivial3uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,17): error CS0610: Field or property cannot be of type 'ArgIterator'
                //         (int x, System.ArgIterator y) y;
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator y").WithArguments("System.ArgIterator").WithLocation(6, 17),
                // (6,39): warning CS0168: The variable 'y' is declared but never used
                //         (int x, System.ArgIterator y) y;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y").WithArguments("y").WithLocation(6, 39)
                );
        }

        [Fact, WorkItem(10569, "https://github.com/dotnet/roslyn/issues/10569")]
        public void IncompleteInterfaceMethod()
        {
            var source = @"
public interface MyInterface {
    (int, int) Foo()
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (3,21): error CS1002: ; expected
                //     (int, int) Foo()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 21)
                );
        }

        [Fact]
        public void ImplementInterface()
        {
            var source = @"
interface I
{
    (int Alice, string Bob) M((int x, string y) value);
    (int Alice, string Bob) P1 { get; }
}
class C : I
{
    static void Main()
    {
        var c = new C();
        var x = c.M(c.P1);
        System.Console.WriteLine(x);
    }

    public (int, string) M((int, string) value)
    {
        return value;
    }

    public (int, string) P1 => (r: 1, s: ""hello"");
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"(1, hello)
");
        }

        [Fact]
        public void ImplementInterfaceExplicitly()
        {
            var source = @"
interface I
{
    (int Alice, string Bob) M((int x, string y) value);
    (int Alice, string Bob) P1 { get; }
}
class C : I
{
    static void Main()
    {
        I c = new C();
        var x = c.M(c.P1);
        System.Console.WriteLine(x);
        System.Console.WriteLine(x.Alice);
    }

    (int, string) I.M((int, string) value)
    {
        return value;
    }

    public (int, string) P1 => (r: 1, s: ""hello"");
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"(1, hello)
1");
        }

        [Fact]
        public void TupleTypeArguments()
        {
            var source = @"
interface I<TA, TB> where TB : TA
{
    (TA, TB) M(TA a, TB b);
}

class C : I<(int, string), (int Alice, string Bob)>
{
    static void Main()
    {
        var c = new C();
        var x = c.M((1, ""Australia""), (2, ""Brazil""));
        System.Console.WriteLine(x);
    }

    public ((int Charlie, string Dylan), (int, string)) M((int Item1, string Item2) a, (int, string) b)
    {
        return (a, b);
    }

}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"((1, Australia), (2, Brazil))");
        }

        [Fact]
        public void LongTupleTypeArguments()
        {
            var source = @"
interface I<TA, TB> where TB : TA
{
    (TA, TB) M(TA a, TB b);
}

class C : I<(int, string, int, string, int, string, int, string), (int A, string B, int C, string D, int E, string F, int G, string H)>
{
    static void Main()
    {
        var c = new C();
        var x = c.M((1, ""Australia"", 2, ""Brazil"", 3, ""Columbia"", 4, ""Ecuador""), (5, ""France"", 6, ""Germany"", 7, ""Honduras"", 8, ""India""));
        System.Console.WriteLine(x);
    }

    public ((int, string, int, string, int, string, int, string), (int, string, int, string, int, string, int, string)) M((int, string, int, string, int, string, int, string) a, (int, string, int, string, int, string, int, string) b)
    {
        return (a, b);
    }

}
";

            var comp = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                        expectedOutput: @"((1, Australia, 2, Brazil, 3, Columbia, 4, Ecuador), (5, France, 6, Germany, 7, Honduras, 8, India))");
        }

        [Fact]
        public void OverrideGenericInterfaceWithDifferentNames()
        {
            string source = @"
interface I<TA, TB> where TB : TA
{
   (TA returnA, TB returnB) M((TA paramA, TB paramB) x);
}

class C : I<(int b, int a), (int a, int b)>
{
    public virtual ((int, int) x, (int, int) y) M(((int, int), (int, int)) x)
    {
        throw new System.Exception();
    }
}

class D : C, I<(int a, int b), (int c, int d)>
{
    static void Main()
    {
        C c = new D();
        var r = c.M(((1, 2), (3, 4)));
        System.Console.WriteLine($""{r.x} {r.y}"");
    }

    public override ((int b, int a), (int b, int a)) M(((int a, int b), (int b, int a)) y)
    {
        return y;
    }
}
";

            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                TypeSymbol y = (TypeSymbol)compilation.GlobalNamespace.GetMember("D");

                Assert.Equal(2, y.AllInterfaces.Length);
                Assert.Equal("I<(System.Int32 b, System.Int32 a), (System.Int32 a, System.Int32 b)>", y.AllInterfaces[0].ToTestDisplayString());
                Assert.Equal("I<(System.Int32 a, System.Int32 b), (System.Int32 c, System.Int32 d)>", y.AllInterfaces[1].ToTestDisplayString());
            };

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: @"(1, 2) (3, 4)", sourceSymbolValidator: validator);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleWithoutFeatureFlag()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int) x = (1, 1);
    }
CS0151ERR_IntegralTypeValueExpected}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular6);
            comp.VerifyDiagnostics(
                // (6,9): error CS8059: Feature 'tuples' is not available in C# 6.  Please use language version 7 or greater.
                //         (int, int) x = (1, 1);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int, int)").WithArguments("tuples", "7").WithLocation(6, 9),
                // (6,24): error CS8059: Feature 'tuples' is not available in C# 6.  Please use language version 7 or greater.
                //         (int, int) x = (1, 1);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(1, 1)").WithArguments("tuples", "7").WithLocation(6, 24),
                // (8,36): error CS1519: Invalid token '}' in class, struct, or interface member declaration
                // CS0151ERR_IntegralTypeValueExpected}
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "}").WithArguments("}").WithLocation(8, 36)
                );
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var v1 = M1();
        System.Console.WriteLine(v1.Item1);
        System.Console.WriteLine(v1.Item2);

        var v2 = M2();
        System.Console.WriteLine(v2.Item1);
        System.Console.WriteLine(v2.Item2);
        System.Console.WriteLine(v2.a2);
        System.Console.WriteLine(v2.b2);

        var v6 = M6();
        System.Console.WriteLine(v6.Item1);
        System.Console.WriteLine(v6.Item2);
        System.Console.WriteLine(v6.item1);
        System.Console.WriteLine(v6.item2);

        System.Console.WriteLine(v1.ToString());
        System.Console.WriteLine(v2.ToString());
        System.Console.WriteLine(v6.ToString());
    }

    static (int, int) M1()
    {
        return (1, 11);
    }

    static (int a2, int b2) M2()
    {
        return (2, 22);
    }

    static (int item1, int item2) M6()
    {
        return (6, 66);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"1
11
2
22
2
22
6
66
6
66
(1, 11)
(2, 22)
(6, 66)
");

            var c = ((CSharpCompilation)comp.Compilation).GetTypeByMetadataName("C");

            var m1Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M1").ReturnType;
            var m2Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M2").ReturnType;
            var m6Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M6").ReturnType;

            AssertTestDisplayString(m1Tuple.GetMembers(),
                "System.Int32 (System.Int32, System.Int32).Item1",
                "System.Int32 (System.Int32, System.Int32).Item2",
                "(System.Int32, System.Int32)..ctor()",
                "(System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.Boolean (System.Int32, System.Int32).Equals(System.Object obj)",
                "System.Boolean (System.Int32, System.Int32).Equals((System.Int32, System.Int32) other)",
                "System.Boolean (System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32, System.Int32).CompareTo((System.Int32, System.Int32) other)",
                "System.Int32 (System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).GetHashCode()",
                "System.Int32 (System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32, System.Int32).ToString()",
                "System.String (System.Int32, System.Int32).System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.Size { get; }");

            Assert.Equal(new string[] {
".ctor",
".ctor",
"CompareTo",
"Equals",
"Equals",
"GetHashCode",
"Item1",
"Item2",
"System.Collections.IStructuralComparable.CompareTo",
"System.Collections.IStructuralEquatable.Equals",
"System.Collections.IStructuralEquatable.GetHashCode",
"System.IComparable.CompareTo",
"System.ITupleInternal.get_Size",
"System.ITupleInternal.GetHashCode",
"System.ITupleInternal.Size",
"System.ITupleInternal.ToStringEnd",
"ToString" }, ((TupleTypeSymbol)m1Tuple).UnderlyingDefinitionToMemberMap.Values.Select(s => s.Name).OrderBy(n => n).ToArray());

            AssertTestDisplayString(m2Tuple.GetMembers(),
                "System.Int32 (System.Int32 a2, System.Int32 b2).Item1",
                "System.Int32 (System.Int32 a2, System.Int32 b2).a2",
                "System.Int32 (System.Int32 a2, System.Int32 b2).Item2",
                "System.Int32 (System.Int32 a2, System.Int32 b2).b2",
                "(System.Int32 a2, System.Int32 b2)..ctor()",
                "(System.Int32 a2, System.Int32 b2)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.Boolean (System.Int32 a2, System.Int32 b2).Equals(System.Object obj)",
                "System.Boolean (System.Int32 a2, System.Int32 b2).Equals((System.Int32, System.Int32) other)",
                "System.Boolean (System.Int32 a2, System.Int32 b2).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32 a2, System.Int32 b2).System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32 a2, System.Int32 b2).CompareTo((System.Int32, System.Int32) other)",
                "System.Int32 (System.Int32 a2, System.Int32 b2).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32 a2, System.Int32 b2).GetHashCode()",
                "System.Int32 (System.Int32 a2, System.Int32 b2).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32 a2, System.Int32 b2).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32 a2, System.Int32 b2).ToString()",
                "System.String (System.Int32 a2, System.Int32 b2).System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32 a2, System.Int32 b2).System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32 a2, System.Int32 b2).System.ITupleInternal.Size { get; }");

            Assert.Equal(new string[] {
".ctor",
".ctor",
"CompareTo",
"Equals",
"Equals",
"GetHashCode",
"Item1",
"Item2",
"System.Collections.IStructuralComparable.CompareTo",
"System.Collections.IStructuralEquatable.Equals",
"System.Collections.IStructuralEquatable.GetHashCode",
"System.IComparable.CompareTo",
"System.ITupleInternal.get_Size",
"System.ITupleInternal.GetHashCode",
"System.ITupleInternal.Size",
"System.ITupleInternal.ToStringEnd",
"ToString" },
                         ((TupleTypeSymbol)m2Tuple).UnderlyingDefinitionToMemberMap.Values.Select(s => s.Name).OrderBy(n => n).ToArray());

            AssertTestDisplayString(m6Tuple.GetMembers(),
                "System.Int32 (System.Int32 item1, System.Int32 item2).Item1",
                "System.Int32 (System.Int32 item1, System.Int32 item2).item1",
                "System.Int32 (System.Int32 item1, System.Int32 item2).Item2",
                "System.Int32 (System.Int32 item1, System.Int32 item2).item2",
                "(System.Int32 item1, System.Int32 item2)..ctor()",
                "(System.Int32 item1, System.Int32 item2)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.Boolean (System.Int32 item1, System.Int32 item2).Equals(System.Object obj)",
                "System.Boolean (System.Int32 item1, System.Int32 item2).Equals((System.Int32, System.Int32) other)",
                "System.Boolean (System.Int32 item1, System.Int32 item2).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32 item1, System.Int32 item2).System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32 item1, System.Int32 item2).CompareTo((System.Int32, System.Int32) other)",
                "System.Int32 (System.Int32 item1, System.Int32 item2).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32 item1, System.Int32 item2).GetHashCode()",
                "System.Int32 (System.Int32 item1, System.Int32 item2).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32 item1, System.Int32 item2).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32 item1, System.Int32 item2).ToString()",
                "System.String (System.Int32 item1, System.Int32 item2).System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32 item1, System.Int32 item2).System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32 item1, System.Int32 item2).System.ITupleInternal.Size { get; }");

            Assert.Equal(new string[] {
".ctor",
".ctor",
"CompareTo",
"Equals",
"Equals",
"GetHashCode",
"Item1",
"Item2",
"System.Collections.IStructuralComparable.CompareTo",
"System.Collections.IStructuralEquatable.Equals",
"System.Collections.IStructuralEquatable.GetHashCode",
"System.IComparable.CompareTo",
"System.ITupleInternal.get_Size",
"System.ITupleInternal.GetHashCode",
"System.ITupleInternal.Size",
"System.ITupleInternal.ToStringEnd",
"ToString" },
                         ((TupleTypeSymbol)m6Tuple).UnderlyingDefinitionToMemberMap.Values.Select(s => s.Name).OrderBy(n => n).ToArray());

            Assert.Equal("", m1Tuple.Name);
            Assert.Equal(SymbolKind.NamedType, m1Tuple.Kind);
            Assert.Equal(TypeKind.Struct, m1Tuple.TypeKind);
            Assert.False(m1Tuple.IsImplicitlyDeclared);
            Assert.True(m1Tuple.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32>", m1Tuple.TupleUnderlyingType.ToTestDisplayString());
            Assert.Same(m1Tuple, m1Tuple.ConstructedFrom);
            Assert.Same(m1Tuple, m1Tuple.OriginalDefinition);
            AssertTupleTypeEquality(m1Tuple);
            Assert.Same(m1Tuple.TupleUnderlyingType.ContainingSymbol, m1Tuple.ContainingSymbol);
            Assert.Null(m1Tuple.GetUseSiteDiagnostic());
            Assert.Null(m1Tuple.EnumUnderlyingType);
            Assert.Equal(new string[] {
"Item1",
"Item2",
".ctor",
"Equals",
"System.Collections.IStructuralEquatable.Equals",
"System.IComparable.CompareTo",
"CompareTo",
"System.Collections.IStructuralComparable.CompareTo",
"GetHashCode",
"System.Collections.IStructuralEquatable.GetHashCode",
"System.ITupleInternal.GetHashCode",
"ToString",
"System.ITupleInternal.ToStringEnd",
"System.ITupleInternal.get_Size",
"System.ITupleInternal.Size" },
                         m1Tuple.MemberNames.ToArray());
            Assert.Equal(new string[] {
"Item1",
"a2",
"Item2",
"b2",
".ctor",
"Equals",
"System.Collections.IStructuralEquatable.Equals",
"System.IComparable.CompareTo",
"CompareTo",
"System.Collections.IStructuralComparable.CompareTo",
"GetHashCode",
"System.Collections.IStructuralEquatable.GetHashCode",
"System.ITupleInternal.GetHashCode",
"ToString",
"System.ITupleInternal.ToStringEnd",
"System.ITupleInternal.get_Size",
"System.ITupleInternal.Size" },
                         m2Tuple.MemberNames.ToArray());
            Assert.Equal(0, m1Tuple.Arity);
            Assert.True(m1Tuple.TypeParameters.IsEmpty);
            Assert.Equal("System.ValueType", m1Tuple.BaseType.ToTestDisplayString());
            Assert.Null(m1Tuple.ComImportCoClass);
            Assert.False(m1Tuple.HasTypeArgumentsCustomModifiers);
            Assert.False(m1Tuple.IsComImport);
            Assert.True(m1Tuple.TypeArgumentsCustomModifiers.IsEmpty);
            Assert.True(m1Tuple.TypeArgumentsNoUseSiteDiagnostics.IsEmpty);
            Assert.True(m1Tuple.GetAttributes().IsEmpty);
            Assert.Equal("System.Int32 (System.Int32 a2, System.Int32 b2).Item1", m2Tuple.GetMembers("Item1").Single().ToTestDisplayString());
            Assert.Equal("System.Int32 (System.Int32 a2, System.Int32 b2).a2", m2Tuple.GetMembers("a2").Single().ToTestDisplayString());
            Assert.True(m1Tuple.GetTypeMembers().IsEmpty);
            Assert.True(m1Tuple.GetTypeMembers("C9").IsEmpty);
            Assert.True(m1Tuple.GetTypeMembers("C9", 0).IsEmpty);
            Assert.Equal(6, m1Tuple.Interfaces.Length);
            Assert.Equal(m1Tuple.TupleUnderlyingType.GetEarlyAttributeDecodingMembers().Select(m => m.Name).ToArray(),
                         m1Tuple.GetEarlyAttributeDecodingMembers().Select(m => m.Name).ToArray());
            Assert.Equal("System.Int32 (System.Int32, System.Int32).Item1", m1Tuple.GetEarlyAttributeDecodingMembers("Item1").Single().ToTestDisplayString());
            Assert.True(m1Tuple.GetTypeMembersUnordered().IsEmpty);
            Assert.Equal(1, m1Tuple.Locations.Length);
            Assert.Equal("(int, int)", m1Tuple.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("(int a2, int b2)", m2Tuple.DeclaringSyntaxReferences.Single().GetSyntax().ToString());

            AssertTupleTypeEquality(m2Tuple);
            AssertTupleTypeEquality(m6Tuple);

            Assert.False(m1Tuple.Equals(m2Tuple));
            Assert.False(m1Tuple.Equals(m6Tuple));
            Assert.False(m6Tuple.Equals(m2Tuple));
            AssertTupleTypeMembersEquality(m1Tuple, m2Tuple);
            AssertTupleTypeMembersEquality(m1Tuple, m6Tuple);
            AssertTupleTypeMembersEquality(m2Tuple, m6Tuple);

            var m1Item1 = (FieldSymbol)m1Tuple.GetMembers()[0];
            var m2Item1 = (FieldSymbol)m2Tuple.GetMembers()[0];
            var m2a2 = (FieldSymbol)m2Tuple.GetMembers()[1];

            Assert.IsType<TupleElementFieldSymbol>(m1Item1);
            Assert.IsType<TupleFieldSymbol>(m2Item1);
            Assert.IsType<TupleRenamedElementFieldSymbol>(m2a2);

            Assert.True(m1Item1.IsTupleField);
            Assert.Same(m1Item1, m1Item1.OriginalDefinition);
            Assert.True(m1Item1.Equals(m1Item1));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m1Item1.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m1Item1.AssociatedSymbol);
            Assert.Same(m1Tuple, m1Item1.ContainingSymbol);
            Assert.Same(m1Tuple.TupleUnderlyingType, m1Item1.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m1Item1.CustomModifiers.IsEmpty);
            Assert.True(m1Item1.GetAttributes().IsEmpty);
            Assert.Null(m1Item1.GetUseSiteDiagnostic());
            Assert.False(m1Item1.Locations.IsDefaultOrEmpty);
            Assert.Equal("int", m1Item1.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("Item1", m1Item1.TupleUnderlyingField.Name);
            Assert.False(m1Item1.IsImplicitlyDeclared);
            Assert.Null(m1Item1.TypeLayoutOffset);

            Assert.True(m2Item1.IsTupleField);
            Assert.Same(m2Item1, m2Item1.OriginalDefinition);
            Assert.True(m2Item1.Equals(m2Item1));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m2Item1.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m2Item1.AssociatedSymbol);
            Assert.Same(m2Tuple, m2Item1.ContainingSymbol);
            Assert.Same(m2Tuple.TupleUnderlyingType, m2Item1.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m2Item1.CustomModifiers.IsEmpty);
            Assert.True(m2Item1.GetAttributes().IsEmpty);
            Assert.Null(m2Item1.GetUseSiteDiagnostic());
            Assert.False(m2Item1.Locations.IsDefaultOrEmpty);
            Assert.Equal("Item1", m2Item1.Name);
            Assert.Equal("Item1", m2Item1.TupleUnderlyingField.Name);
            Assert.Equal(m2Item1.Locations.Single(), m2Item1.TupleUnderlyingField.Locations.Single());
            Assert.False(m2Item1.IsImplicitlyDeclared);
            Assert.Null(m2Item1.TypeLayoutOffset);

            Assert.True(m2a2.IsTupleField);
            Assert.Same(m2a2, m2a2.OriginalDefinition);
            Assert.True(m2a2.Equals(m2a2));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m2a2.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m2a2.AssociatedSymbol);
            Assert.Same(m2Tuple, m2a2.ContainingSymbol);
            Assert.Same(m2Tuple.TupleUnderlyingType, m2a2.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m2a2.CustomModifiers.IsEmpty);
            Assert.True(m2a2.GetAttributes().IsEmpty);
            Assert.Null(m2a2.GetUseSiteDiagnostic());
            Assert.False(m2a2.Locations.IsDefaultOrEmpty);
            Assert.Equal("a2", m2a2.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("Item1", m2a2.TupleUnderlyingField.Name);
            Assert.False(m2a2.IsImplicitlyDeclared);
            Assert.Null(m2a2.TypeLayoutOffset);
        }

        private static void AssertTupleTypeEquality(TypeSymbol tuple)
        {
            Assert.True(tuple.Equals(tuple));
            Assert.True(tuple.Equals(tuple, false, false));
            Assert.True(tuple.Equals(tuple, false, true));
            Assert.False(tuple.Equals(tuple.TupleUnderlyingType, false, false));
            Assert.False(tuple.TupleUnderlyingType.Equals(tuple, false, false));
            Assert.True(tuple.Equals(tuple.TupleUnderlyingType, false, true));
            Assert.True(tuple.TupleUnderlyingType.Equals(tuple, false, true));

            var members = tuple.GetMembers();

            for (int i = 0; i < members.Length; i++)
            {
                for (int j = 0; j < members.Length; j++)
                {
                    if (i != j)
                    {
                        Assert.NotSame(members[i], members[j]);
                        Assert.False(members[i].Equals(members[j]));
                        Assert.False(members[j].Equals(members[i]));
                    }
                }
            }

            var underlyingMembers = tuple.TupleUnderlyingType.GetMembers();
            foreach (var m in members)
            {
                Assert.False(underlyingMembers.Any(u => u.Equals(m)));
                Assert.False(underlyingMembers.Any(u => m.Equals(u)));
            }
        }

        private static void AssertTupleTypeMembersEquality(TypeSymbol tuple1, TypeSymbol tuple2)
        {
            Assert.NotSame(tuple1, tuple2);

            if (tuple1.Equals(tuple2))
            {
                Assert.True(tuple2.Equals(tuple1));
                var members1 = tuple1.GetMembers();
                var members2 = tuple2.GetMembers();

                Assert.Equal(members1.Length, members2.Length);
                for (int i = 0; i < members1.Length; i++)
                {
                    Assert.NotSame(members1[i], members2[i]);
                    Assert.True(members1[i].Equals(members2[i]));
                    Assert.True(members2[i].Equals(members1[i]));
                    Assert.Equal(members2[i].GetHashCode(), members1[i].GetHashCode());

                    if (members1[i].Kind == SymbolKind.Method)
                    {
                        var parameters1 = ((MethodSymbol)members1[i]).Parameters;
                        var parameters2 = ((MethodSymbol)members2[i]).Parameters;
                        AssertTupleMembersParametersEquality(parameters1, parameters2);

                        var typeParameters1 = ((MethodSymbol)members1[i]).TypeParameters;
                        var typeParameters2 = ((MethodSymbol)members2[i]).TypeParameters;
                        Assert.Equal(typeParameters1.Length, typeParameters2.Length);
                        for (int j = 0; j < typeParameters1.Length; j++)
                        {
                            Assert.NotSame(typeParameters1[j], typeParameters2[j]);
                            Assert.True(typeParameters1[j].Equals(typeParameters2[j]));
                            Assert.True(typeParameters2[j].Equals(typeParameters1[j]));
                            Assert.Equal(typeParameters2[j].GetHashCode(), typeParameters1[j].GetHashCode());
                        }
                    }
                    else if (members1[i].Kind == SymbolKind.Property)
                    {
                        var parameters1 = ((PropertySymbol)members1[i]).Parameters;
                        var parameters2 = ((PropertySymbol)members2[i]).Parameters;
                        AssertTupleMembersParametersEquality(parameters1, parameters2);
                    }
                }

                for (int i = 0; i < members1.Length; i++)
                {
                    for (int j = 0; j < members2.Length; j++)
                    {
                        if (i != j)
                        {
                            Assert.NotSame(members1[i], members2[j]);
                            Assert.False(members1[i].Equals(members2[j]));
                        }
                    }
                }
            }
            else
            {
                Assert.False(tuple2.Equals(tuple1));
                var members1 = tuple1.GetMembers();
                var members2 = tuple2.GetMembers();
                foreach (var m in members1)
                {
                    Assert.False(members2.Any(u => u.Equals(m)));
                    Assert.False(members2.Any(u => m.Equals(u)));
                }
            }
        }

        private static void AssertTupleMembersParametersEquality(ImmutableArray<ParameterSymbol> parameters1, ImmutableArray<ParameterSymbol> parameters2)
        {
            Assert.Equal(parameters1.Length, parameters2.Length);
            for (int j = 0; j < parameters1.Length; j++)
            {
                Assert.NotSame(parameters1[j], parameters2[j]);
                Assert.True(parameters1[j].Equals(parameters2[j]));
                Assert.True(parameters2[j].Equals(parameters1[j]));
                Assert.Equal(parameters2[j].GetHashCode(), parameters1[j].GetHashCode());
            }
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var v3 = M3();
        System.Console.WriteLine(v3.Item1);
        System.Console.WriteLine(v3.Item2);
        System.Console.WriteLine(v3.Item3);
        System.Console.WriteLine(v3.Item4);
        System.Console.WriteLine(v3.Item5);
        System.Console.WriteLine(v3.Item6);
        System.Console.WriteLine(v3.Item7);
        System.Console.WriteLine(v3.Item8);
        System.Console.WriteLine(v3.Item9);
        System.Console.WriteLine(v3.Rest.Item1);
        System.Console.WriteLine(v3.Rest.Item2);

        System.Console.WriteLine(v3.ToString());
    }

    static (int, int, int, int, int, int, int, int, int) M3()
    {
        return (31, 32, 33, 34, 35, 36, 37, 38, 39);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"31
32
33
34
35
36
37
38
39
38
39
(31, 32, 33, 34, 35, 36, 37, 38, 39)
");

            var c = ((CSharpCompilation)comp.Compilation).GetTypeByMetadataName("C");

            var m3Tuple = c.GetMember<MethodSymbol>("M3").ReturnType;
            AssertTupleTypeEquality(m3Tuple);

            AssertTestDisplayString(m3Tuple.GetMembers(),
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item1",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item2",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item3",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item4",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item5",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item6",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item7",
                "(System.Int32, System.Int32) (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Rest",
                "(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor()",
                "(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2, System.Int32 item3, System.Int32 item4, System.Int32 item5, System.Int32 item6, System.Int32 item7, (System.Int32, System.Int32) rest)",
                "System.Boolean (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals(System.Object obj)",
                "System.Boolean (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
                "System.Boolean (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).CompareTo((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).GetHashCode()",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).ToString()",
                "System.String (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.Size { get; }",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item8",
                "System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item9");

            var m3Item8 = (FieldSymbol)m3Tuple.GetMembers("Item8").Single();

            Assert.IsType<TupleRenamedElementFieldSymbol>(m3Item8);

            Assert.True(m3Item8.IsTupleField);
            Assert.Same(m3Item8, m3Item8.OriginalDefinition);
            Assert.True(m3Item8.Equals(m3Item8));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m3Item8.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m3Item8.AssociatedSymbol);
            Assert.Same(m3Tuple, m3Item8.ContainingSymbol);
            Assert.NotEqual(m3Tuple.TupleUnderlyingType, m3Item8.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m3Item8.CustomModifiers.IsEmpty);
            Assert.True(m3Item8.GetAttributes().IsEmpty);
            Assert.Null(m3Item8.GetUseSiteDiagnostic());
            Assert.False(m3Item8.Locations.IsDefaultOrEmpty);
            Assert.Equal("int", m3Item8.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("Item1", m3Item8.TupleUnderlyingField.Name);
            Assert.False(m3Item8.IsImplicitlyDeclared);
            Assert.Null(m3Item8.TypeLayoutOffset);

            var m3TupleRestTuple = (NamedTypeSymbol)((FieldSymbol)m3Tuple.GetMembers("Rest").Single()).Type;
            AssertTestDisplayString(m3TupleRestTuple.GetMembers(),
                "System.Int32 (System.Int32, System.Int32).Item1",
                "System.Int32 (System.Int32, System.Int32).Item2",
                "(System.Int32, System.Int32)..ctor()",
                "(System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.Boolean (System.Int32, System.Int32).Equals(System.Object obj)",
                "System.Boolean (System.Int32, System.Int32).Equals((System.Int32, System.Int32) other)",
                "System.Boolean (System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32, System.Int32).CompareTo((System.Int32, System.Int32) other)",
                "System.Int32 (System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).GetHashCode()",
                "System.Int32 (System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32, System.Int32).ToString()",
                "System.String (System.Int32, System.Int32).System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.Size { get; }");

            Assert.True(m3TupleRestTuple.IsTupleType);
            AssertTupleTypeEquality(m3TupleRestTuple);
            Assert.True(m3TupleRestTuple.Locations.IsEmpty);
            Assert.True(m3TupleRestTuple.DeclaringSyntaxReferences.IsEmpty);

            foreach (var m in m3TupleRestTuple.GetMembers().OfType<FieldSymbol>())
            {
                Assert.True(m.Locations.IsEmpty);
            }
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var v4 = M4 ();
        System.Console.WriteLine(v4.Item1);
        System.Console.WriteLine(v4.Item2);
        System.Console.WriteLine(v4.Item3);
        System.Console.WriteLine(v4.Item4);
        System.Console.WriteLine(v4.Item5);
        System.Console.WriteLine(v4.Item6);
        System.Console.WriteLine(v4.Item7);
        System.Console.WriteLine(v4.Item8);
        System.Console.WriteLine(v4.Item9);
        System.Console.WriteLine(v4.Rest.Item1);
        System.Console.WriteLine(v4.Rest.Item2);
        System.Console.WriteLine(v4.a4);
        System.Console.WriteLine(v4.b4);
        System.Console.WriteLine(v4.c4);
        System.Console.WriteLine(v4.d4);
        System.Console.WriteLine(v4.e4);
        System.Console.WriteLine(v4.f4);
        System.Console.WriteLine(v4.g4);
        System.Console.WriteLine(v4.h4);
        System.Console.WriteLine(v4.i4);

        System.Console.WriteLine(v4.ToString());
    }

    static (int a4, int b4, int c4, int d4, int e4, int f4, int g4, int h4, int i4) M4()
    {
        return (41, 42, 43, 44, 45, 46, 47, 48, 49);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"41
42
43
44
45
46
47
48
49
48
49
41
42
43
44
45
46
47
48
49
(41, 42, 43, 44, 45, 46, 47, 48, 49)
");

            var c = ((CSharpCompilation)comp.Compilation).GetTypeByMetadataName("C");

            var m4Tuple = c.GetMember<MethodSymbol>("M4").ReturnType;
            AssertTupleTypeEquality(m4Tuple);

            AssertTestDisplayString(m4Tuple.GetMembers(),
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item1",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".a4",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item2",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".b4",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item3",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".c4",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item4",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".d4",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item5",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".e4",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item6",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".f4",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item7",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".g4",
                "(System.Int32, System.Int32) (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Rest",
                "(System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    "..ctor()",
                "(System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    "..ctor" +
                        "(System.Int32 item1, System.Int32 item2, System.Int32 item3, System.Int32 item4, System.Int32 item5, System.Int32 item6, System.Int32 item7, (System.Int32, System.Int32) rest)",
                "System.Boolean (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Equals(System.Object obj)",
                "System.Boolean (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Equals((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
                "System.Boolean (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".CompareTo((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".GetHashCode()",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".ToString()",
                "System.String (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".System.ITupleInternal.Size { get; }",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item8",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".h4",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".Item9",
                "System.Int32 (System.Int32 a4, System.Int32 b4, System.Int32 c4, System.Int32 d4, System.Int32 e4, System.Int32 f4, System.Int32 g4, System.Int32 h4, System.Int32 i4)" +
                    ".i4"
                );

            var m4Item8 = (FieldSymbol)m4Tuple.GetMembers("Item8").Single();

            Assert.IsType<TupleRenamedElementFieldSymbol>(m4Item8);

            Assert.True(m4Item8.IsTupleField);
            Assert.Same(m4Item8, m4Item8.OriginalDefinition);
            Assert.True(m4Item8.Equals(m4Item8));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m4Item8.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m4Item8.AssociatedSymbol);
            Assert.Same(m4Tuple, m4Item8.ContainingSymbol);
            Assert.NotEqual(m4Tuple.TupleUnderlyingType, m4Item8.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m4Item8.CustomModifiers.IsEmpty);
            Assert.True(m4Item8.GetAttributes().IsEmpty);
            Assert.Null(m4Item8.GetUseSiteDiagnostic());
            Assert.True(m4Item8.Locations.IsDefaultOrEmpty);
            Assert.Equal("Item1", m4Item8.TupleUnderlyingField.Name);
            Assert.False(m4Item8.IsImplicitlyDeclared);
            Assert.Null(m4Item8.TypeLayoutOffset);

            var m4h4 = (FieldSymbol)m4Tuple.GetMembers("h4").Single();

            Assert.IsType<TupleRenamedElementFieldSymbol>(m4h4);

            Assert.True(m4h4.IsTupleField);
            Assert.Same(m4h4, m4h4.OriginalDefinition);
            Assert.True(m4h4.Equals(m4h4));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m4h4.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m4h4.AssociatedSymbol);
            Assert.Same(m4Tuple, m4h4.ContainingSymbol);
            Assert.NotEqual(m4Tuple.TupleUnderlyingType, m4h4.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m4h4.CustomModifiers.IsEmpty);
            Assert.True(m4h4.GetAttributes().IsEmpty);
            Assert.Null(m4h4.GetUseSiteDiagnostic());
            Assert.False(m4h4.Locations.IsDefaultOrEmpty);
            Assert.Equal("h4", m4h4.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("Item1", m4h4.TupleUnderlyingField.Name);
            Assert.False(m4h4.IsImplicitlyDeclared);
            Assert.Null(m4h4.TypeLayoutOffset);

            var m4TupleRestTuple = ((FieldSymbol)m4Tuple.GetMembers("Rest").Single()).Type;
            AssertTupleTypeEquality(m4TupleRestTuple);

            AssertTestDisplayString(m4TupleRestTuple.GetMembers(),
                "System.Int32 (System.Int32, System.Int32).Item1",
                "System.Int32 (System.Int32, System.Int32).Item2",
                "(System.Int32, System.Int32)..ctor()",
                "(System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.Boolean (System.Int32, System.Int32).Equals(System.Object obj)",
                "System.Boolean (System.Int32, System.Int32).Equals((System.Int32, System.Int32) other)",
                "System.Boolean (System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32, System.Int32).CompareTo((System.Int32, System.Int32) other)",
                "System.Int32 (System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).GetHashCode()",
                "System.Int32 (System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32, System.Int32).ToString()",
                "System.String (System.Int32, System.Int32).System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32, System.Int32).System.ITupleInternal.Size { get; }");

            foreach (var m in m4TupleRestTuple.GetMembers().OfType<FieldSymbol>())
            {
                Assert.True(m.Locations.IsEmpty);
            }
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_04()
        {
            var source = @"

class C
{
    static void Main()
    {
        var v4 = M4 ();
        System.Console.WriteLine(v4.Rest.a4);
        System.Console.WriteLine(v4.Rest.b4);
        System.Console.WriteLine(v4.Rest.c4);
        System.Console.WriteLine(v4.Rest.d4);
        System.Console.WriteLine(v4.Rest.e4);
        System.Console.WriteLine(v4.Rest.f4);
        System.Console.WriteLine(v4.Rest.g4);
        System.Console.WriteLine(v4.Rest.h4);
        System.Console.WriteLine(v4.Rest.i4);
    }

    static (int a4, int b4, int c4, int d4, int e4, int f4, int g4, int h4, int i4) M4()
    {
        return (41, 42, 43, 44, 45, 46, 47, 48, 49);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, 
                references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (8,42): error CS1061: '(int, int)' does not contain a definition for 'a4' and no extension method 'a4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.a4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "a4").WithArguments("(int, int)", "a4").WithLocation(8, 42),
                // (9,42): error CS1061: '(int, int)' does not contain a definition for 'b4' and no extension method 'b4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.b4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "b4").WithArguments("(int, int)", "b4").WithLocation(9, 42),
                // (10,42): error CS1061: '(int, int)' does not contain a definition for 'c4' and no extension method 'c4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.c4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "c4").WithArguments("(int, int)", "c4").WithLocation(10, 42),
                // (11,42): error CS1061: '(int, int)' does not contain a definition for 'd4' and no extension method 'd4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.d4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "d4").WithArguments("(int, int)", "d4").WithLocation(11, 42),
                // (12,42): error CS1061: '(int, int)' does not contain a definition for 'e4' and no extension method 'e4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.e4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "e4").WithArguments("(int, int)", "e4").WithLocation(12, 42),
                // (13,42): error CS1061: '(int, int)' does not contain a definition for 'f4' and no extension method 'f4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.f4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "f4").WithArguments("(int, int)", "f4").WithLocation(13, 42),
                // (14,42): error CS1061: '(int, int)' does not contain a definition for 'g4' and no extension method 'g4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.g4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "g4").WithArguments("(int, int)", "g4").WithLocation(14, 42),
                // (15,42): error CS1061: '(int, int)' does not contain a definition for 'h4' and no extension method 'h4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.h4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "h4").WithArguments("(int, int)", "h4").WithLocation(15, 42),
                // (16,42): error CS1061: '(int, int)' does not contain a definition for 'i4' and no extension method 'i4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v4.Rest.i4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "i4").WithArguments("(int, int)", "i4").WithLocation(16, 42)
                );
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var v5 = M5();
        System.Console.WriteLine(v5.Item1);
        System.Console.WriteLine(v5.Item2);
        System.Console.WriteLine(v5.Item3);
        System.Console.WriteLine(v5.Item4);
        System.Console.WriteLine(v5.Item5);
        System.Console.WriteLine(v5.Item6);
        System.Console.WriteLine(v5.Item7);
        System.Console.WriteLine(v5.Item8);
        System.Console.WriteLine(v5.Item9);
        System.Console.WriteLine(v5.Item10);
        System.Console.WriteLine(v5.Item11);
        System.Console.WriteLine(v5.Item12);
        System.Console.WriteLine(v5.Item13);
        System.Console.WriteLine(v5.Item14);
        System.Console.WriteLine(v5.Item15);
        System.Console.WriteLine(v5.Item16);
        System.Console.WriteLine(v5.Rest.Item1);
        System.Console.WriteLine(v5.Rest.Item2);
        System.Console.WriteLine(v5.Rest.Item3);
        System.Console.WriteLine(v5.Rest.Item4);
        System.Console.WriteLine(v5.Rest.Item5);
        System.Console.WriteLine(v5.Rest.Item6);
        System.Console.WriteLine(v5.Rest.Item7);
        System.Console.WriteLine(v5.Rest.Item8);
        System.Console.WriteLine(v5.Rest.Item9);
        System.Console.WriteLine(v5.Rest.Rest.Item1);
        System.Console.WriteLine(v5.Rest.Rest.Item2);

        System.Console.WriteLine(v5.ToString());
    }

    static (int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8, 
            int Item9, int Item10, int Item11, int Item12, int Item13, int Item14, int Item15, int Item16) M5()
    {
        return (501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"501
502
503
504
505
506
507
508
509
510
511
512
513
514
515
516
508
509
510
511
512
513
514
515
516
515
516
(501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516)
");

            var c = ((CSharpCompilation)comp.Compilation).GetTypeByMetadataName("C");

            var m5Tuple = c.GetMember<MethodSymbol>("M5").ReturnType;
            AssertTupleTypeEquality(m5Tuple);

            AssertTestDisplayString(m5Tuple.GetMembers(),
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item1",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item2",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item3",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item4",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item5",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item6",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item7",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Rest",
"(System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16)..ctor()",
"(System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16)..ctor(System.Int32 item1, System.Int32 item2, System.Int32 item3, System.Int32 item4, System.Int32 item5, System.Int32 item6, System.Int32 item7, (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) rest)",
"System.Boolean (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Equals(System.Object obj)",
"System.Boolean (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Equals((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
"System.Boolean (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).System.IComparable.CompareTo(System.Object other)",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).CompareTo((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).GetHashCode()",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
"System.String (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).ToString()",
"System.String (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).System.ITupleInternal.ToStringEnd()",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).System.ITupleInternal.Size.get",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).System.ITupleInternal.Size { get; }",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item8",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item9",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item10",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item11",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item12",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item13",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item14",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item15",
"System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9, System.Int32 Item10, System.Int32 Item11, System.Int32 Item12, System.Int32 Item13, System.Int32 Item14, System.Int32 Item15, System.Int32 Item16).Item16");

            var m5Item8 = (FieldSymbol)m5Tuple.GetMembers("Item8").Single();

            Assert.IsType<TupleRenamedElementFieldSymbol>(m5Item8);

            Assert.True(m5Item8.IsTupleField);
            Assert.Same(m5Item8, m5Item8.OriginalDefinition);
            Assert.True(m5Item8.Equals(m5Item8));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32, System.Int32)>.Item1", 
                         m5Item8.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m5Item8.AssociatedSymbol);
            Assert.Same(m5Tuple, m5Item8.ContainingSymbol);
            Assert.NotEqual(m5Tuple.TupleUnderlyingType, m5Item8.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m5Item8.CustomModifiers.IsEmpty);
            Assert.True(m5Item8.GetAttributes().IsEmpty);
            Assert.Null(m5Item8.GetUseSiteDiagnostic());
            Assert.False(m5Item8.Locations.IsDefaultOrEmpty);
            Assert.Equal("Item8", m5Item8.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("Item1", m5Item8.TupleUnderlyingField.Name);
            Assert.False(m5Item8.IsImplicitlyDeclared);
            Assert.Null(m5Item8.TypeLayoutOffset);

            var m5TupleRestTuple = ((FieldSymbol)m5Tuple.GetMembers("Rest").Single()).Type;
            AssertTupleTypeEquality(m5TupleRestTuple);

            AssertTestDisplayString(m5TupleRestTuple.GetMembers(),
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item1",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item2",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item3",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item4",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item5",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item6",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item7",
"(System.Int32, System.Int32) (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Rest",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor()",
"(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2, System.Int32 item3, System.Int32 item4, System.Int32 item5, System.Int32 item6, System.Int32 item7, (System.Int32, System.Int32) rest)",
"System.Boolean (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals(System.Object obj)",
"System.Boolean (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Equals((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
"System.Boolean (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.IComparable.CompareTo(System.Object other)",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).CompareTo((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).GetHashCode()",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
"System.String (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).ToString()",
"System.String (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.ToStringEnd()",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.Size.get",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).System.ITupleInternal.Size { get; }",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item8",
"System.Int32 (System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32).Item9");

            foreach (var m in m5TupleRestTuple.GetMembers().OfType<FieldSymbol>())
            {
                if (m.Name != "Rest")
                {
                    Assert.True(m.Locations.IsEmpty);
                }
                else
                {
                    Assert.Equal("Rest", m.Name);
                }
            }

            var m5TupleRestTupleRestTuple = ((FieldSymbol)m5TupleRestTuple.GetMembers("Rest").Single()).Type;
            AssertTupleTypeEquality(m5TupleRestTupleRestTuple);

            AssertTestDisplayString(m5TupleRestTupleRestTuple.GetMembers(),
"System.Int32 (System.Int32, System.Int32).Item1",
"System.Int32 (System.Int32, System.Int32).Item2",
"(System.Int32, System.Int32)..ctor()",
"(System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2)",
"System.Boolean (System.Int32, System.Int32).Equals(System.Object obj)",
"System.Boolean (System.Int32, System.Int32).Equals((System.Int32, System.Int32) other)",
"System.Boolean (System.Int32, System.Int32).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
"System.Int32 (System.Int32, System.Int32).System.IComparable.CompareTo(System.Object other)",
"System.Int32 (System.Int32, System.Int32).CompareTo((System.Int32, System.Int32) other)",
"System.Int32 (System.Int32, System.Int32).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
"System.Int32 (System.Int32, System.Int32).GetHashCode()",
"System.Int32 (System.Int32, System.Int32).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
"System.Int32 (System.Int32, System.Int32).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
"System.String (System.Int32, System.Int32).ToString()",
"System.String (System.Int32, System.Int32).System.ITupleInternal.ToStringEnd()",
"System.Int32 (System.Int32, System.Int32).System.ITupleInternal.Size.get",
"System.Int32 (System.Int32, System.Int32).System.ITupleInternal.Size { get; }");

            foreach (var m in m5TupleRestTupleRestTuple.GetMembers().OfType<FieldSymbol>())
            {
                Assert.True(m.Locations.IsEmpty);
            }
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_06()
        {
            var source = @"

class C
{
    static void Main()
    {
        var v5 = M5();
        System.Console.WriteLine(v5.Rest.Item10);
        System.Console.WriteLine(v5.Rest.Item11);
        System.Console.WriteLine(v5.Rest.Item12);
        System.Console.WriteLine(v5.Rest.Item13);
        System.Console.WriteLine(v5.Rest.Item14);
        System.Console.WriteLine(v5.Rest.Item15);
        System.Console.WriteLine(v5.Rest.Item16);

        System.Console.WriteLine(v5.Rest.Rest.Item3);
        System.Console.WriteLine(v5.Rest.Rest.Item4);
        System.Console.WriteLine(v5.Rest.Rest.Item5);
        System.Console.WriteLine(v5.Rest.Rest.Item6);
        System.Console.WriteLine(v5.Rest.Rest.Item7);
        System.Console.WriteLine(v5.Rest.Rest.Item8);
        System.Console.WriteLine(v5.Rest.Rest.Item9);
        System.Console.WriteLine(v5.Rest.Rest.Item10);
        System.Console.WriteLine(v5.Rest.Rest.Item11);
        System.Console.WriteLine(v5.Rest.Rest.Item12);
        System.Console.WriteLine(v5.Rest.Rest.Item13);
        System.Console.WriteLine(v5.Rest.Rest.Item14);
        System.Console.WriteLine(v5.Rest.Rest.Item15);
        System.Console.WriteLine(v5.Rest.Rest.Item16);
    }

    static (int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8, 
            int Item9, int Item10, int Item11, int Item12, int Item13, int Item14, int Item15, int Item16) M5()
    {
        return (501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, 
                references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (8,42): error CS1061: '(int, int, int, int, int, int, int, int, int)' does not contain a definition for 'Item10' and no extension method 'Item10' accepting a first argument of type '(int, int, int, int, int, int, int, int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Item10);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item10").WithArguments("(int, int, int, int, int, int, int, int, int)", "Item10").WithLocation(8, 42),
                // (9,42): error CS1061: '(int, int, int, int, int, int, int, int, int)' does not contain a definition for 'Item11' and no extension method 'Item11' accepting a first argument of type '(int, int, int, int, int, int, int, int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Item11);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item11").WithArguments("(int, int, int, int, int, int, int, int, int)", "Item11").WithLocation(9, 42),
                // (10,42): error CS1061: '(int, int, int, int, int, int, int, int, int)' does not contain a definition for 'Item12' and no extension method 'Item12' accepting a first argument of type '(int, int, int, int, int, int, int, int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Item12);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item12").WithArguments("(int, int, int, int, int, int, int, int, int)", "Item12").WithLocation(10, 42),
                // (11,42): error CS1061: '(int, int, int, int, int, int, int, int, int)' does not contain a definition for 'Item13' and no extension method 'Item13' accepting a first argument of type '(int, int, int, int, int, int, int, int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Item13);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item13").WithArguments("(int, int, int, int, int, int, int, int, int)", "Item13").WithLocation(11, 42),
                // (12,42): error CS1061: '(int, int, int, int, int, int, int, int, int)' does not contain a definition for 'Item14' and no extension method 'Item14' accepting a first argument of type '(int, int, int, int, int, int, int, int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Item14);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item14").WithArguments("(int, int, int, int, int, int, int, int, int)", "Item14").WithLocation(12, 42),
                // (13,42): error CS1061: '(int, int, int, int, int, int, int, int, int)' does not contain a definition for 'Item15' and no extension method 'Item15' accepting a first argument of type '(int, int, int, int, int, int, int, int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Item15);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item15").WithArguments("(int, int, int, int, int, int, int, int, int)", "Item15").WithLocation(13, 42),
                // (14,42): error CS1061: '(int, int, int, int, int, int, int, int, int)' does not contain a definition for 'Item16' and no extension method 'Item16' accepting a first argument of type '(int, int, int, int, int, int, int, int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Item16);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item16").WithArguments("(int, int, int, int, int, int, int, int, int)", "Item16").WithLocation(14, 42),
                // (16,47): error CS1061: '(int, int)' does not contain a definition for 'Item3' and no extension method 'Item3' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item3);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item3").WithArguments("(int, int)", "Item3").WithLocation(16, 47),
                // (17,47): error CS1061: '(int, int)' does not contain a definition for 'Item4' and no extension method 'Item4' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item4);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item4").WithArguments("(int, int)", "Item4").WithLocation(17, 47),
                // (18,47): error CS1061: '(int, int)' does not contain a definition for 'Item5' and no extension method 'Item5' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item5);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item5").WithArguments("(int, int)", "Item5").WithLocation(18, 47),
                // (19,47): error CS1061: '(int, int)' does not contain a definition for 'Item6' and no extension method 'Item6' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item6);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item6").WithArguments("(int, int)", "Item6").WithLocation(19, 47),
                // (20,47): error CS1061: '(int, int)' does not contain a definition for 'Item7' and no extension method 'Item7' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item7);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item7").WithArguments("(int, int)", "Item7").WithLocation(20, 47),
                // (21,47): error CS1061: '(int, int)' does not contain a definition for 'Item8' and no extension method 'Item8' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item8);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item8").WithArguments("(int, int)", "Item8").WithLocation(21, 47),
                // (22,47): error CS1061: '(int, int)' does not contain a definition for 'Item9' and no extension method 'Item9' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item9);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item9").WithArguments("(int, int)", "Item9").WithLocation(22, 47),
                // (23,47): error CS1061: '(int, int)' does not contain a definition for 'Item10' and no extension method 'Item10' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item10);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item10").WithArguments("(int, int)", "Item10").WithLocation(23, 47),
                // (24,47): error CS1061: '(int, int)' does not contain a definition for 'Item11' and no extension method 'Item11' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item11);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item11").WithArguments("(int, int)", "Item11").WithLocation(24, 47),
                // (25,47): error CS1061: '(int, int)' does not contain a definition for 'Item12' and no extension method 'Item12' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item12);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item12").WithArguments("(int, int)", "Item12").WithLocation(25, 47),
                // (26,47): error CS1061: '(int, int)' does not contain a definition for 'Item13' and no extension method 'Item13' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item13);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item13").WithArguments("(int, int)", "Item13").WithLocation(26, 47),
                // (27,47): error CS1061: '(int, int)' does not contain a definition for 'Item14' and no extension method 'Item14' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item14);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item14").WithArguments("(int, int)", "Item14").WithLocation(27, 47),
                // (28,47): error CS1061: '(int, int)' does not contain a definition for 'Item15' and no extension method 'Item15' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item15);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item15").WithArguments("(int, int)", "Item15").WithLocation(28, 47),
                // (29,47): error CS1061: '(int, int)' does not contain a definition for 'Item16' and no extension method 'Item16' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v5.Rest.Rest.Item16);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item16").WithArguments("(int, int)", "Item16").WithLocation(29, 47)
                );
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_07()
        {
            var source = @"

class C
{
    static void Main()
    {
    }

    static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
    {
        return (701, 702, 703, 704, 705, 706, 707, 708, 709);
    }
}
" + trivial2uple + trivial3uple + trivialRemainingTuples + tupleattributes_cs;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (9,17): error CS8201: Tuple member name 'Item9' is only allowed at position 9.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item9").WithArguments("Item9", "9").WithLocation(9, 17),
                // (9,28): error CS8201: Tuple member name 'Item1' is only allowed at position 1.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "1").WithLocation(9, 28),
                // (9,39): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(9, 39),
                // (9,50): error CS8201: Tuple member name 'Item3' is only allowed at position 3.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item3").WithArguments("Item3", "3").WithLocation(9, 50),
                // (9,61): error CS8201: Tuple member name 'Item4' is only allowed at position 4.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item4").WithArguments("Item4", "4").WithLocation(9, 61),
                // (9,72): error CS8201: Tuple member name 'Item5' is only allowed at position 5.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item5").WithArguments("Item5", "5").WithLocation(9, 72),
                // (9,83): error CS8201: Tuple member name 'Item6' is only allowed at position 6.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item6").WithArguments("Item6", "6").WithLocation(9, 83),
                // (9,94): error CS8201: Tuple member name 'Item7' is only allowed at position 7.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item7").WithArguments("Item7", "7").WithLocation(9, 94),
                // (9,105): error CS8201: Tuple member name 'Item8' is only allowed at position 8.
                //     static (int Item9, int Item1, int Item2, int Item3, int Item4, int Item5, int Item6, int Item7, int Item8) M7()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item8").WithArguments("Item8", "8").WithLocation(9, 105)
                );

            var c = comp.GetTypeByMetadataName("C");

            var m7Tuple = c.GetMember<MethodSymbol>("M7").ReturnType;
            AssertTupleTypeEquality(m7Tuple);

            AssertTestDisplayString(m7Tuple.GetMembers(),
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item1",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item9",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item2",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item1",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item3",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item2",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item4",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item3",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item5",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item4",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item6",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item5",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item7",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item6",
                "(System.Int32, System.Int32) (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Rest",
                "(System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    "..ctor" +
                        "(System.Int32 item1, System.Int32 item2, System.Int32 item3, System.Int32 item4, System.Int32 item5, System.Int32 item6, System.Int32 item7, (System.Int32, System.Int32) rest)",
                "System.String (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".ToString()",
                "(System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    "..ctor()",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item8",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item7",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item9",
                "System.Int32 (System.Int32 Item9, System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8)" +
                    ".Item8"
                );
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_08()
        {
            var source = @"

class C
{
    static void Main()
    {
    }

    static (int a1, int a2, int a3, int a4, int a5, int a6, int a7, int Item1) M8()
    {
        return (801, 802, 803, 804, 805, 806, 807, 808);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (9,73): error CS8201: Tuple member name 'Item1' is only allowed at position 1.
                //     static (int a1, int a2, int a3, int a4, int a5, int a6, int a7, int Item1) M8()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item1").WithArguments("Item1", "1").WithLocation(9, 73)
                );

            var c = comp.GetTypeByMetadataName("C");

            var m8Tuple = c.GetMember<MethodSymbol>("M8").ReturnType;
            AssertTupleTypeEquality(m8Tuple);

            AssertTestDisplayString(m8Tuple.GetMembers(),
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item1",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).a1",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item2",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).a2",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item3",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).a3",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item4",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).a4",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item5",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).a5",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item6",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).a6",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item7",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).a7",
                "(System.Int32) (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Rest",
                "(System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1)..ctor()",
                "(System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1)..ctor(System.Int32 item1, System.Int32 item2, System.Int32 item3, System.Int32 item4, System.Int32 item5, System.Int32 item6, System.Int32 item7, (System.Int32) rest)",
                "System.Boolean (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Equals(System.Object obj)",
                "System.Boolean (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Equals((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
                "System.Boolean (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).CompareTo((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).GetHashCode()",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).ToString()",
                "System.String (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).System.ITupleInternal.Size { get; }",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item8",
                "System.Int32 (System.Int32 a1, System.Int32 a2, System.Int32 a3, System.Int32 a4, System.Int32 a5, System.Int32 a6, System.Int32 a7, System.Int32 Item1).Item1");

            var m8Item8 = (FieldSymbol)m8Tuple.GetMembers("Item8").Single();

            Assert.IsType<TupleRenamedElementFieldSymbol>(m8Item8);

            Assert.True(m8Item8.IsTupleField);
            Assert.Same(m8Item8, m8Item8.OriginalDefinition);
            Assert.True(m8Item8.Equals(m8Item8));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32>.Item1",
                         m8Item8.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m8Item8.AssociatedSymbol);
            Assert.Same(m8Tuple, m8Item8.ContainingSymbol);
            Assert.NotEqual(m8Tuple.TupleUnderlyingType, m8Item8.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m8Item8.CustomModifiers.IsEmpty);
            Assert.True(m8Item8.GetAttributes().IsEmpty);
            Assert.Null(m8Item8.GetUseSiteDiagnostic());
            Assert.True(m8Item8.Locations.IsDefaultOrEmpty);
            Assert.Equal("Item1", m8Item8.TupleUnderlyingField.Name);
            Assert.False(m8Item8.IsImplicitlyDeclared);
            Assert.Null(m8Item8.TypeLayoutOffset);

            var m8Item1 = (FieldSymbol)m8Tuple.GetMembers("Item1").Last();

            Assert.IsType<TupleElementFieldSymbol>(m8Item1);

            Assert.True(m8Item1.IsTupleField);
            Assert.Same(m8Item1, m8Item1.OriginalDefinition);
            Assert.True(m8Item1.Equals(m8Item1));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32>.Item1",
                         m8Item1.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m8Item1.AssociatedSymbol);
            Assert.Same(m8Tuple, m8Item1.ContainingSymbol);
            Assert.NotEqual(m8Tuple.TupleUnderlyingType, m8Item1.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m8Item1.CustomModifiers.IsEmpty);
            Assert.True(m8Item1.GetAttributes().IsEmpty);
            Assert.Null(m8Item1.GetUseSiteDiagnostic());
            Assert.False(m8Item1.Locations.IsDefaultOrEmpty);
            Assert.Equal("Item1", m8Item1.Name);
            Assert.Equal("Item1", m8Item1.TupleUnderlyingField.Name);
            Assert.NotEqual(m8Item1.Locations.Single(), m8Item1.TupleUnderlyingField.Locations.Single());
            Assert.False(m8Item1.IsImplicitlyDeclared);
            Assert.Null(m8Item1.TypeLayoutOffset);

            var m8TupleRestTuple = ((FieldSymbol)m8Tuple.GetMembers("Rest").Single()).Type;
            AssertTupleTypeEquality(m8TupleRestTuple);

            AssertTestDisplayString(m8TupleRestTuple.GetMembers(),
                "System.Int32 (System.Int32).Item1",
                "(System.Int32)..ctor()",
                "(System.Int32)..ctor(System.Int32 item1)",
                "System.Boolean (System.Int32).Equals(System.Object obj)",
                "System.Boolean (System.Int32).Equals((System.Int32) other)",
                "System.Boolean (System.Int32).System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32).System.IComparable.CompareTo(System.Object other)",
                "System.Int32 (System.Int32).CompareTo((System.Int32) other)",
                "System.Int32 (System.Int32).System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                "System.Int32 (System.Int32).GetHashCode()",
                "System.Int32 (System.Int32).System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.Int32 (System.Int32).System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                "System.String (System.Int32).ToString()",
                "System.String (System.Int32).System.ITupleInternal.ToStringEnd()",
                "System.Int32 (System.Int32).System.ITupleInternal.Size.get",
                "System.Int32 (System.Int32).System.ITupleInternal.Size { get; }");
        }

        [Fact]
        public void DefaultAndFriendlyElementNames_09()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var v1 = (1, 11);
        System.Console.WriteLine(v1.Item1);
        System.Console.WriteLine(v1.Item2);

        var v2 =(a2: 2, b2: 22);
        System.Console.WriteLine(v2.Item1);
        System.Console.WriteLine(v2.Item2);
        System.Console.WriteLine(v2.a2);
        System.Console.WriteLine(v2.b2);

        var v6 = (item1: 6, item2: 66);
        System.Console.WriteLine(v6.Item1);
        System.Console.WriteLine(v6.Item2);
        System.Console.WriteLine(v6.item1);
        System.Console.WriteLine(v6.item2);

        System.Console.WriteLine(v1.ToString());
        System.Console.WriteLine(v2.ToString());
        System.Console.WriteLine(v6.ToString());
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"1
11
2
22
2
22
6
66
6
66
{1, 11}
{2, 22}
{6, 66}
");

            var c = (CSharpCompilation)comp.Compilation;
            var tree = c.SyntaxTrees.Single();
            var model = c.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<TupleExpressionSyntax>().First();

            var m1Tuple = (NamedTypeSymbol)model.LookupSymbols(node.SpanStart, name: "v1").OfType<LocalSymbol>().Single().Type;
            var m2Tuple = (NamedTypeSymbol)model.LookupSymbols(node.SpanStart, name: "v2").OfType<LocalSymbol>().Single().Type;
            var m6Tuple = (NamedTypeSymbol)model.LookupSymbols(node.SpanStart, name: "v6").OfType<LocalSymbol>().Single().Type;

            AssertTestDisplayString(m1Tuple.GetMembers(),
                "System.Int32 (System.Int32, System.Int32).Item1",
                "System.Int32 (System.Int32, System.Int32).Item2",
                "(System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.String (System.Int32, System.Int32).ToString()",
                "(System.Int32, System.Int32)..ctor()");

            AssertTestDisplayString(m2Tuple.GetMembers(),
                "System.Int32 (System.Int32 a2, System.Int32 b2).Item1",
                "System.Int32 (System.Int32 a2, System.Int32 b2).a2",
                "System.Int32 (System.Int32 a2, System.Int32 b2).Item2",
                "System.Int32 (System.Int32 a2, System.Int32 b2).b2",
                "(System.Int32 a2, System.Int32 b2)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.String (System.Int32 a2, System.Int32 b2).ToString()",
                "(System.Int32 a2, System.Int32 b2)..ctor()");

            AssertTestDisplayString(m6Tuple.GetMembers(),
                "System.Int32 (System.Int32 item1, System.Int32 item2).Item1",
                "System.Int32 (System.Int32 item1, System.Int32 item2).item1",
                "System.Int32 (System.Int32 item1, System.Int32 item2).Item2",
                "System.Int32 (System.Int32 item1, System.Int32 item2).item2",
                "(System.Int32 item1, System.Int32 item2)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.String (System.Int32 item1, System.Int32 item2).ToString()",
                "(System.Int32 item1, System.Int32 item2)..ctor()"
                );

            Assert.Equal("", m1Tuple.Name);
            Assert.Equal(SymbolKind.NamedType, m1Tuple.Kind);
            Assert.Equal(TypeKind.Struct, m1Tuple.TypeKind);
            Assert.False(m1Tuple.IsImplicitlyDeclared);
            Assert.True(m1Tuple.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32>", m1Tuple.TupleUnderlyingType.ToTestDisplayString());
            Assert.Same(m1Tuple, m1Tuple.ConstructedFrom);
            Assert.Same(m1Tuple, m1Tuple.OriginalDefinition);
            AssertTupleTypeEquality(m1Tuple);
            Assert.Same(m1Tuple.TupleUnderlyingType.ContainingSymbol, m1Tuple.ContainingSymbol);
            Assert.Null(m1Tuple.GetUseSiteDiagnostic());
            Assert.Null(m1Tuple.EnumUnderlyingType);
            Assert.Equal(new string[] { "Item1", "Item2", ".ctor", "ToString" },
                         m1Tuple.MemberNames.ToArray());
            Assert.Equal(new string[] { "Item1", "a2", "Item2", "b2", ".ctor", "ToString" },
                         m2Tuple.MemberNames.ToArray());
            Assert.Equal(0, m1Tuple.Arity);
            Assert.True(m1Tuple.TypeParameters.IsEmpty);
            Assert.Equal("System.ValueType", m1Tuple.BaseType.ToTestDisplayString());
            Assert.Null(m1Tuple.ComImportCoClass);
            Assert.False(m1Tuple.HasTypeArgumentsCustomModifiers);
            Assert.False(m1Tuple.IsComImport);
            Assert.True(m1Tuple.TypeArgumentsCustomModifiers.IsEmpty);
            Assert.True(m1Tuple.TypeArgumentsNoUseSiteDiagnostics.IsEmpty);
            Assert.True(m1Tuple.GetAttributes().IsEmpty);
            Assert.Equal("System.Int32 (System.Int32 a2, System.Int32 b2).Item1", m2Tuple.GetMembers("Item1").Single().ToTestDisplayString());
            Assert.Equal("System.Int32 (System.Int32 a2, System.Int32 b2).a2", m2Tuple.GetMembers("a2").Single().ToTestDisplayString());
            Assert.True(m1Tuple.GetTypeMembers().IsEmpty);
            Assert.True(m1Tuple.GetTypeMembers("C9").IsEmpty);
            Assert.True(m1Tuple.GetTypeMembers("C9", 0).IsEmpty);
            Assert.True(m1Tuple.Interfaces.IsEmpty);
            Assert.Equal(m1Tuple.TupleUnderlyingType.GetEarlyAttributeDecodingMembers().Select(m => m.Name).ToArray(),
                         m1Tuple.GetEarlyAttributeDecodingMembers().Select(m => m.Name).ToArray());
            Assert.Equal("System.Int32 (System.Int32, System.Int32).Item1", m1Tuple.GetEarlyAttributeDecodingMembers("Item1").Single().ToTestDisplayString());
            Assert.True(m1Tuple.GetTypeMembersUnordered().IsEmpty);
            Assert.Equal(1, m1Tuple.Locations.Length);
            Assert.Equal("(1, 11)", m1Tuple.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("(a2: 2, b2: 22)", m2Tuple.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("public struct ValueTuple<T1, T2>", m1Tuple.TupleUnderlyingType.DeclaringSyntaxReferences.Single().GetSyntax().ToString().Substring(0, 32));

            AssertTupleTypeEquality(m2Tuple);
            AssertTupleTypeEquality(m6Tuple);

            Assert.False(m1Tuple.Equals(m2Tuple));
            Assert.False(m1Tuple.Equals(m6Tuple));
            Assert.False(m6Tuple.Equals(m2Tuple));
            AssertTupleTypeMembersEquality(m1Tuple, m2Tuple);
            AssertTupleTypeMembersEquality(m1Tuple, m6Tuple);
            AssertTupleTypeMembersEquality(m2Tuple, m6Tuple);

            var m1Item1 = (FieldSymbol)m1Tuple.GetMembers()[0];
            var m2Item1 = (FieldSymbol)m2Tuple.GetMembers()[0];
            var m2a2 = (FieldSymbol)m2Tuple.GetMembers()[1];

            Assert.IsType<TupleElementFieldSymbol>(m1Item1);
            Assert.IsType<TupleFieldSymbol>(m2Item1);
            Assert.IsType<TupleRenamedElementFieldSymbol>(m2a2);

            Assert.True(m1Item1.IsTupleField);
            Assert.Same(m1Item1, m1Item1.OriginalDefinition);
            Assert.True(m1Item1.Equals(m1Item1));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m1Item1.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m1Item1.AssociatedSymbol);
            Assert.Same(m1Tuple, m1Item1.ContainingSymbol);
            Assert.Same(m1Tuple.TupleUnderlyingType, m1Item1.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m1Item1.CustomModifiers.IsEmpty);
            Assert.True(m1Item1.GetAttributes().IsEmpty);
            Assert.Null(m1Item1.GetUseSiteDiagnostic());
            Assert.False(m1Item1.Locations.IsDefaultOrEmpty);
            Assert.Equal("1", m1Item1.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("Item1", m1Item1.TupleUnderlyingField.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.False(m1Item1.IsImplicitlyDeclared);
            Assert.Null(m1Item1.TypeLayoutOffset);

            Assert.True(m2Item1.IsTupleField);
            Assert.Same(m2Item1, m2Item1.OriginalDefinition);
            Assert.True(m2Item1.Equals(m2Item1));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m2Item1.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m2Item1.AssociatedSymbol);
            Assert.Same(m2Tuple, m2Item1.ContainingSymbol);
            Assert.Same(m2Tuple.TupleUnderlyingType, m2Item1.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m2Item1.CustomModifiers.IsEmpty);
            Assert.True(m2Item1.GetAttributes().IsEmpty);
            Assert.Null(m2Item1.GetUseSiteDiagnostic());
            Assert.False(m2Item1.Locations.IsDefaultOrEmpty);
            Assert.Equal("Item1", m2Item1.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("Item1", m2Item1.TupleUnderlyingField.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal(m2Item1.Locations.Single(), m2Item1.TupleUnderlyingField.Locations.Single());
            Assert.False(m2Item1.IsImplicitlyDeclared);
            Assert.Null(m2Item1.TypeLayoutOffset);

            Assert.True(m2a2.IsTupleField);
            Assert.Same(m2a2, m2a2.OriginalDefinition);
            Assert.True(m2a2.Equals(m2a2));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1", m2a2.TupleUnderlyingField.ToTestDisplayString());
            Assert.Null(m2a2.AssociatedSymbol);
            Assert.Same(m2Tuple, m2a2.ContainingSymbol);
            Assert.Same(m2Tuple.TupleUnderlyingType, m2a2.TupleUnderlyingField.ContainingSymbol);
            Assert.True(m2a2.CustomModifiers.IsEmpty);
            Assert.True(m2a2.GetAttributes().IsEmpty);
            Assert.Null(m2a2.GetUseSiteDiagnostic());
            Assert.False(m2a2.Locations.IsDefaultOrEmpty);
            Assert.Equal("a2", m2a2.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.Equal("Item1", m2a2.TupleUnderlyingField.DeclaringSyntaxReferences.Single().GetSyntax().ToString());
            Assert.False(m2a2.IsImplicitlyDeclared);
            Assert.Null(m2a2.TypeLayoutOffset);

            var m1ToString = m1Tuple.GetMember<MethodSymbol>("ToString");

            Assert.True(m1ToString.IsTupleMethod);
            Assert.Same(m1ToString, m1ToString.OriginalDefinition);
            Assert.Same(m1ToString, m1ToString.ConstructedFrom);
            Assert.Equal("System.String System.ValueTuple<System.Int32, System.Int32>.ToString()", 
                         m1ToString.TupleUnderlyingMethod.ToTestDisplayString());
            Assert.Same(m1ToString.TupleUnderlyingMethod, m1ToString.TupleUnderlyingMethod.ConstructedFrom);
            Assert.Same(m1Tuple, m1ToString.ContainingSymbol);
            Assert.Same(m1Tuple.TupleUnderlyingType, m1ToString.TupleUnderlyingMethod.ContainingType);
            Assert.Null(m1ToString.AssociatedSymbol);
            Assert.False(m1ToString.IsExplicitInterfaceImplementation);
            Assert.True(m1ToString.ExplicitInterfaceImplementations.IsEmpty);
            Assert.False(m1ToString.ReturnsVoid);
            Assert.True(m1ToString.TypeArguments.IsEmpty);
            Assert.True(m1ToString.TypeParameters.IsEmpty);
            Assert.True(m1ToString.GetAttributes().IsEmpty);
            Assert.Null(m1ToString.GetUseSiteDiagnostic());
            Assert.Equal("System.String System.ValueType.ToString()",
                         m1ToString.OverriddenMethod.ToTestDisplayString());
            Assert.False(m1ToString.Locations.IsDefaultOrEmpty);
            Assert.Equal("public override string ToString()", m1ToString.DeclaringSyntaxReferences.Single().GetSyntax().ToString().Substring(0, 33));
            Assert.Equal(m1ToString.Locations.Single(), m1ToString.TupleUnderlyingMethod.Locations.Single());
        }

        [Fact]
        public void CustomValueTupleWithStrangeThings_01()
        {
            var source = @"

class C
{
    static void Main()
    {
        var x1 = M9().Item1;
        var x2 = M9().Item2;

        var y = (int, int).C9;

        System.ValueTuple<int, int>.C9 z = null; 
        System.Console.WriteLine(z);       
    }

    static (int, int) M9()
    {
        return (901, 902);
    }
}

namespace System
{
    
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public class C9{}
    }
}
" + tupleattributes_cs;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (10,18): error CS1525: Invalid expression term 'int'
                //         var y = (int, int).C9;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(10, 18),
                // (10,23): error CS1525: Invalid expression term 'int'
                //         var y = (int, int).C9;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(10, 23),
                // (12,37): error CS0426: The type name 'C9' does not exist in the type '(int, int)'
                //         System.ValueTuple<int, int>.C9 z = null; 
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "C9").WithArguments("C9", "(int, int)").WithLocation(12, 37)
                );

            var c = comp.GetTypeByMetadataName("C");

            var m9Tuple = c.GetMember<MethodSymbol>("M9").ReturnType;
            AssertTupleTypeEquality(m9Tuple);

            AssertTestDisplayString(m9Tuple.GetMembers(),
                "System.Int32 (System.Int32, System.Int32).Item1",
                "System.Int32 (System.Int32, System.Int32).Item2",
                "(System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.String (System.Int32, System.Int32).ToString()",
                "(System.Int32, System.Int32)..ctor()");

            AssertTestDisplayString(m9Tuple.TupleUnderlyingType.GetMembers(),
                "System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item1",
                "System.Int32 System.ValueTuple<System.Int32, System.Int32>.Item2",
                "System.ValueTuple<System.Int32, System.Int32>..ctor(System.Int32 item1, System.Int32 item2)",
                "System.String System.ValueTuple<System.Int32, System.Int32>.ToString()",
                "System.ValueTuple<System.Int32, System.Int32>.C9",
                "System.ValueTuple<System.Int32, System.Int32>..ctor()");

            Assert.True(m9Tuple.GetTypeMembers().IsEmpty);
            Assert.True(m9Tuple.GetTypeMembers("C9").IsEmpty);
            Assert.True(m9Tuple.GetTypeMembers("C9", 0).IsEmpty);
            Assert.True(m9Tuple.GetTypeMembersUnordered().IsEmpty);
        }

        [Fact]
        public void CustomValueTupleWithStrangeThings_02()
        {
            var source = @"
partial class C
{
    static void Main()
    {
    }

    public static (int, int) M10()
    {
        return (101, 102);
    }

    static (int, int, int, int, int, int, int, int, int) M101()
    {
        return (1, 1, 1, 1, 1, 1, 1, 1, 1);
    }

    I1 Test01()
    {
        return M10();
    }

    static (int a, int b) M102()
    {
        return (1, 1);
    }

    void Test02()
    {
        System.Console.WriteLine(M10().Item1);
        System.Console.WriteLine(M10().Item20);
        System.Console.WriteLine(M102().a);
    }

    static (int a, int b, int c, int d, int e, int f, int g, int h, int Item2) M103()
    {
        return (1, 1, 1, 1, 1, 1, 1, 1, 1);
    }
}

interface I1
{
    void M1();
    int P1 { get; set; }
    event System.Action E1;
}
namespace System
{
    [Obsolete]
    public struct ValueTuple<T1, T2> : I1
    {
        [Obsolete]
        public T1 Item1;

        [System.Runtime.InteropServices.FieldOffsetAttribute(20)]
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item20 = 0;
            this.Item21 = 0;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public class C9{}

        void I1.M1(){}

        [Obsolete]
        public byte Item20;

        [System.Runtime.InteropServices.FieldOffsetAttribute(21)]
        public byte Item21;

        [Obsolete]
        public void M2() {}

        int I1.P1 { get; set; }
        [Obsolete]
        public int P2 { get; set; } 

        event System.Action I1.E1 {add{} remove{}}
        [Obsolete]
        public event System.Action E2;
    }
}

partial class C
{
    static void Test03()
    {
        M10().M2();
        var x = M10().P2;
        M10().E2 += null;
    }
}
" + trivialRemainingTuples + tupleattributes_cs;

            var comp = CreateCompilationWithMscorlib(source);

            var c = comp.GetTypeByMetadataName("C");
            comp.VerifyDiagnostics(
                // (8,19): warning CS0612: '(int, int)' is obsolete
                //     public static (int, int) M10()
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "(int, int)").WithArguments("(int, int)").WithLocation(8, 19),
                // (23,12): warning CS0612: '(int a, int b)' is obsolete
                //     static (int a, int b) M102()
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "(int a, int b)").WithArguments("(int a, int b)").WithLocation(23, 12),
                // (35,73): error CS8201: Tuple member name 'Item2' is only allowed at position 2.
                //     static (int a, int b, int c, int d, int e, int f, int g, int h, int Item2) M103()
                Diagnostic(ErrorCode.ERR_TupleReservedMemberName, "Item2").WithArguments("Item2", "2").WithLocation(35, 73),
                // (55,10): error CS0636: The FieldOffset attribute can only be placed on members of types marked with the StructLayout(LayoutKind.Explicit)
                //         [System.Runtime.InteropServices.FieldOffsetAttribute(20)]
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadStruct, "System.Runtime.InteropServices.FieldOffsetAttribute").WithLocation(55, 10),
                // (78,10): error CS0636: The FieldOffset attribute can only be placed on members of types marked with the StructLayout(LayoutKind.Explicit)
                //         [System.Runtime.InteropServices.FieldOffsetAttribute(21)]
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadStruct, "System.Runtime.InteropServices.FieldOffsetAttribute").WithLocation(78, 10),
                // (58,16): error CS0843: Auto-implemented property 'ValueTuple<T1, T2>.I1.P1' must be fully assigned before control is returned to the caller.
                //         public ValueTuple(T1 item1, T2 item2)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "ValueTuple").WithArguments("System.ValueTuple<T1, T2>.I1.P1").WithLocation(58, 16),
                // (58,16): error CS0843: Auto-implemented property 'ValueTuple<T1, T2>.P2' must be fully assigned before control is returned to the caller.
                //         public ValueTuple(T1 item1, T2 item2)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "ValueTuple").WithArguments("System.ValueTuple<T1, T2>.P2").WithLocation(58, 16),
                // (58,16): error CS0171: Field 'ValueTuple<T1, T2>.E2' must be fully assigned before control is returned to the caller
                //         public ValueTuple(T1 item1, T2 item2)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "ValueTuple").WithArguments("System.ValueTuple<T1, T2>.E2").WithLocation(58, 16),
                // (30,34): warning CS0612: '(int, int).Item1' is obsolete
                //         System.Console.WriteLine(M10().Item1);
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "M10().Item1").WithArguments("(int, int).Item1").WithLocation(30, 34),
                // (31,34): warning CS0612: '(int, int).Item20' is obsolete
                //         System.Console.WriteLine(M10().Item20);
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "M10().Item20").WithArguments("(int, int).Item20").WithLocation(31, 34),
                // (32,34): warning CS0612: '(int a, int b).a' is obsolete
                //         System.Console.WriteLine(M102().a);
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "M102().a").WithArguments("(int a, int b).a").WithLocation(32, 34),
                // (98,9): warning CS0612: '(int, int).M2()' is obsolete
                //         M10().M2();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "M10().M2()").WithArguments("(int, int).M2()").WithLocation(98, 9),
                // (99,17): warning CS0612: '(int, int).P2' is obsolete
                //         var x = M10().P2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "M10().P2").WithArguments("(int, int).P2").WithLocation(99, 17),
                // (100,9): warning CS0612: '(int, int).E2' is obsolete
                //         M10().E2 += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "M10().E2").WithArguments("(int, int).E2").WithLocation(100, 9),
                // (90,36): warning CS0067: The event 'ValueTuple<T1, T2>.E2' is never used
                //         public event System.Action E2;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E2").WithArguments("System.ValueTuple<T1, T2>.E2").WithLocation(90, 36)
                );

            var m10Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M10").ReturnType;
            AssertTupleTypeEquality(m10Tuple);

            Assert.Equal("System.ObsoleteAttribute", m10Tuple.GetAttributes().Single().ToString());
            Assert.Equal("I1", m10Tuple.Interfaces.Single().ToTestDisplayString());

            var m102Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M102").ReturnType;
            AssertTupleTypeEquality(m102Tuple);

            var m10Item1 = (FieldSymbol)m10Tuple.GetMembers("Item1").Single();
            var m102Item20 = (FieldSymbol)m102Tuple.GetMembers("Item20").Single();
            var m102a = (FieldSymbol)m102Tuple.GetMembers("a").Single();

            Assert.IsType<TupleElementFieldSymbol>(m10Item1);
            Assert.IsType<TupleFieldSymbol>(m102Item20);
            Assert.IsType<TupleRenamedElementFieldSymbol>(m102a);

            Assert.Equal("System.ObsoleteAttribute", m10Item1.GetAttributes().Single().ToString());
            Assert.Equal("System.ObsoleteAttribute", m102Item20.GetAttributes().Single().ToString());
            Assert.Equal("System.ObsoleteAttribute", m102a.GetAttributes().Single().ToString());

            var m10Item2 = (FieldSymbol)m10Tuple.GetMembers("Item2").Single();
            var m102Item21 = (FieldSymbol)m102Tuple.GetMembers("Item21").Single();
            var m102Item2 = (FieldSymbol)m102Tuple.GetMembers("Item2").Single();
            var m102b = (FieldSymbol)m102Tuple.GetMembers("b").Single();

            Assert.IsType<TupleElementFieldSymbol>(m10Item2);
            Assert.IsType<TupleFieldSymbol>(m102Item2);
            Assert.IsType<TupleFieldSymbol>(m102Item21);
            Assert.IsType<TupleRenamedElementFieldSymbol>(m102b);

            Assert.Equal(20, m10Item2.TypeLayoutOffset);
            Assert.Equal(20, m102Item2.TypeLayoutOffset);
            Assert.Equal(21, m102Item21.TypeLayoutOffset);
            Assert.Null(m102b.TypeLayoutOffset);
            Assert.Equal(20, m102b.TupleUnderlyingField.TypeLayoutOffset);

            var m103Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M103").ReturnType;
            AssertTupleTypeEquality(m103Tuple);

            var m103Item2 = (FieldSymbol)m103Tuple.GetMembers("Item2").Last();
            var m103Item9 = (FieldSymbol)m103Tuple.GetMembers("Item9").Single();

            Assert.IsType<TupleElementFieldSymbol>(m103Item2);
            Assert.IsType<TupleRenamedElementFieldSymbol>(m103Item9);
            Assert.Null(m103Item2.TypeLayoutOffset);
            Assert.Equal(20, m103Item2.TupleUnderlyingField.TypeLayoutOffset);
            Assert.Null(m103Item9.TypeLayoutOffset);
            Assert.Equal(20, m103Item9.TupleUnderlyingField.TypeLayoutOffset);

            var m10I1M1 = m10Tuple.GetMember<MethodSymbol>("I1.M1");

            Assert.True(m10I1M1.IsExplicitInterfaceImplementation);
            Assert.Equal("void I1.M1()", m10I1M1.ExplicitInterfaceImplementations.Single().ToTestDisplayString());

            var m10M2 = m10Tuple.GetMember<MethodSymbol>("M2");
            Assert.Equal("System.ObsoleteAttribute", m10M2.GetAttributes().Single().ToString());

            var m10I1P1 = m10Tuple.GetMember<PropertySymbol>("I1.P1");

            Assert.True(m10I1P1.IsExplicitInterfaceImplementation);
            Assert.Equal("System.Int32 I1.P1 { get; set; }", m10I1P1.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            Assert.True(m10I1P1.GetMethod.IsExplicitInterfaceImplementation);
            Assert.Equal("System.Int32 I1.P1.get", m10I1P1.GetMethod.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            Assert.True(m10I1P1.SetMethod.IsExplicitInterfaceImplementation);
            Assert.Equal("void I1.P1.set", m10I1P1.SetMethod.ExplicitInterfaceImplementations.Single().ToTestDisplayString());

            var m10P2 = m10Tuple.GetMember<PropertySymbol>("P2");
            Assert.Equal("System.ObsoleteAttribute", m10P2.GetAttributes().Single().ToString());

            var m10I1E1 = m10Tuple.GetMember<EventSymbol>("I1.E1");

            Assert.True(m10I1E1.IsExplicitInterfaceImplementation);
            Assert.Equal("event System.Action I1.E1", m10I1E1.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            Assert.True(m10I1E1.AddMethod.IsExplicitInterfaceImplementation);
            Assert.Equal("void I1.E1.add", m10I1E1.AddMethod.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            Assert.True(m10I1E1.RemoveMethod.IsExplicitInterfaceImplementation);
            Assert.Equal("void I1.E1.remove", m10I1E1.RemoveMethod.ExplicitInterfaceImplementations.Single().ToTestDisplayString());

            var m10E2 = m10Tuple.GetMember<EventSymbol>("E2");
            Assert.Equal("System.ObsoleteAttribute", m10E2.GetAttributes().Single().ToString());
        }

        [Fact]
        public void CustomValueTupleWithGenericMethod()
        {
            var source = @"

class C
{
    static void Main()
    {
        M9().Test(""Yes"");
    }

    static (int, int) M9()
    {
        return (901, 902);
    }
}

namespace System
{
    
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public void Test<U>(U val)
        {
            System.Console.WriteLine(typeof(U));
            System.Console.WriteLine(val);
        }
    }
}
" + tupleattributes_cs;

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"System.String
Yes");

            var c = comp.GetTypeByMetadataName("C");

            var m9Tuple = c.GetMember<MethodSymbol>("M9").ReturnType;
            AssertTupleTypeEquality(m9Tuple);

            var m9Test = m9Tuple.GetMember<MethodSymbol>("Test");
            Assert.True(m9Test.IsTupleMethod);
            Assert.Same(m9Test, m9Test.OriginalDefinition);
            Assert.Same(m9Test, m9Test.ConstructedFrom);
            Assert.Equal("void System.ValueTuple<System.Int32, System.Int32>.Test<U>(U val)",
                         m9Test.TupleUnderlyingMethod.ToTestDisplayString());
            Assert.Same(m9Test.TupleUnderlyingMethod, m9Test.TupleUnderlyingMethod.ConstructedFrom);
            Assert.Same(m9Tuple.TupleUnderlyingType, m9Test.TupleUnderlyingMethod.ContainingType);
            Assert.Same(m9Test.TypeParameters.Single(), m9Test.TypeParameters.Single().OriginalDefinition);
            Assert.Same(m9Test, m9Test.TypeParameters.Single().ContainingSymbol);
            Assert.Same(m9Test, m9Test.Parameters.Single().ContainingSymbol);
            Assert.Equal(0, m9Test.TypeParameters.Single().Ordinal);
            Assert.Equal(1, m9Test.Arity);
        }

        [Fact]
        public void CreationOfTupleSymbols_01()
        {
            var source = @"
class C
{
    static void Main()
    {
    }

    static (int, int) M1()
    {
        return (101, 102);
    }

    static (int, int, int, int, int, int, int, int, int) M2()
    {
        return (1, 1, 1, 1, 1, 1, 1, 1, 1);
    }

    static (int, int, int) M3()
    {
        return (101, 102, 103);
    }
}

namespace System
{
    public struct ValueTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs);

            var c = comp.GetTypeByMetadataName("C");
            comp.VerifyDiagnostics();

            var m1Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M1").ReturnType;
            {
                var t1 = TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType);
                var t2 = TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType);

                Assert.True(t1.Equals(t2));
                AssertTupleTypeMembersEquality(t1, t2);

                var t3 = TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType, ImmutableArray.Create("a", "b"));
                var t4 = TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType, ImmutableArray.Create("a", "b"));
                var t5 = TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType, ImmutableArray.Create("b", "a"));

                Assert.False(t1.Equals(t3));
                Assert.True(t1.Equals(t3, false, true));
                Assert.True(t3.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t3);

                Assert.True(t3.Equals(t4));
                AssertTupleTypeMembersEquality(t3, t4);

                Assert.False(t5.Equals(t3));
                Assert.True(t5.Equals(t3, false, true));
                Assert.True(t3.Equals(t5, false, true));
                AssertTupleTypeMembersEquality(t5, t3);

                var t6 = TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType, ImmutableArray.Create("Item1", "Item2"));
                var t7 = TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType, ImmutableArray.Create("Item1", "Item2"));

                Assert.True(t6.Equals(t7));
                AssertTupleTypeMembersEquality(t6, t7);

                Assert.False(t1.Equals(t6));
                Assert.True(t1.Equals(t6, false, true));
                Assert.True(t6.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t6);

                var t8 = TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType, ImmutableArray.Create("Item2", "Item1"));

                Assert.False(t1.Equals(t8));
                Assert.True(t1.Equals(t8, false, true));
                Assert.True(t8.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t8);

                Assert.False(t6.Equals(t8));
                Assert.True(t6.Equals(t8, false, true));
                Assert.True(t8.Equals(t6, false, true));
                AssertTupleTypeMembersEquality(t6, t8);
            }

            var m2Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M2").ReturnType;
            {
                var t1 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType, default(ImmutableArray<string>));
                var t2 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType, default(ImmutableArray<string>));

                Assert.True(t1.Equals(t2));
                AssertTupleTypeMembersEquality(t1, t2);

                var t3 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType,
                                                  ImmutableArray.Create("a", "b", "c", "d", "e", "f", "g", "h", "i"));
                var t4 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType,
                                                  ImmutableArray.Create("a", "b", "c", "d", "e", "f", "g", "h", "i"));
                var t5 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType,
                                                  ImmutableArray.Create("a", "b", "c", "d", "e", "f", "g", "i", "h"));

                Assert.False(t1.Equals(t3));
                Assert.True(t1.Equals(t3, false, true));
                Assert.True(t3.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t3);

                Assert.True(t3.Equals(t4));
                AssertTupleTypeMembersEquality(t3, t4);

                Assert.False(t5.Equals(t3));
                Assert.True(t5.Equals(t3, false, true));
                Assert.True(t3.Equals(t5, false, true));
                AssertTupleTypeMembersEquality(t5, t3);

                var t6 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType,
                                    ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9"));
                var t7 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType,
                                    ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9"));

                Assert.True(t6.Equals(t7));
                AssertTupleTypeMembersEquality(t6, t7);

                Assert.False(t1.Equals(t6));
                Assert.True(t1.Equals(t6, false, true));
                Assert.True(t6.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t6);

                var t8 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType,
                                    ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item9", "Item8"));

                Assert.False(t1.Equals(t8));
                Assert.True(t1.Equals(t8, false, true));
                Assert.True(t8.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t8);

                Assert.False(t6.Equals(t8));
                Assert.True(t6.Equals(t8, false, true));
                Assert.True(t8.Equals(t6, false, true));
                AssertTupleTypeMembersEquality(t6, t8);

                var t9 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType,
                                                  ImmutableArray.Create("a", "b", "c", "d", "e", "f", "g", "Item1", "Item2"));
                var t10 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType,
                                                  ImmutableArray.Create("a", "b", "c", "d", "e", "f", "g", "Item1", "Item2"));

                Assert.True(t9.Equals(t10));
                AssertTupleTypeMembersEquality(t9, t10);

                var t11 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType.OriginalDefinition.Construct(
                                                                    m2Tuple.TupleUnderlyingType.TypeArgumentsNoUseSiteDiagnostics.RemoveAt(7).
                                                                    Add(TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType, 
                                                                                ImmutableArray.Create("a", "b")))
                                                                    ));

                Assert.False(t1.Equals(t11));
                AssertTupleTypeMembersEquality(t1, t11);
                Assert.True(t1.Equals(t11, false, true));
                Assert.True(t11.Equals(t1, false, true));
                Assert.False(t1.TupleUnderlyingType.Equals(t11.TupleUnderlyingType));
                Assert.True(t1.TupleUnderlyingType.Equals(t11.TupleUnderlyingType, false, true));
                Assert.False(t11.TupleUnderlyingType.Equals(t1.TupleUnderlyingType));
                Assert.True(t11.TupleUnderlyingType.Equals(t1.TupleUnderlyingType, false, true));

                AssertTestDisplayString(t11.GetMembers(),
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item1",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item2",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item3",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item4",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item5",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item6",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item7",
                    "(System.Int32 a, System.Int32 b) ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Rest",
                    "ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>..ctor()",
                    "ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>..ctor" +
                            "(System.Int32 item1, System.Int32 item2, System.Int32 item3, System.Int32 item4, System.Int32 item5, System.Int32 item6, System.Int32 item7, " +
                                    "(System.Int32 a, System.Int32 b) rest)",
                    "System.Boolean ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Equals(System.Object obj)",
                    "System.Boolean ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".Equals(ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)> other)",
                    "System.Boolean ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".System.IComparable.CompareTo(System.Object other)",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".CompareTo(ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)> other)",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".GetHashCode()",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                    "System.String ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.ToString()",
                    "System.String ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".System.ITupleInternal.ToStringEnd()",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".System.ITupleInternal.Size.get",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>" +
                        ".System.ITupleInternal.Size { get; }",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item8",
                    "System.Int32 ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>.Item9"
                    );

                var t12 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType.OriginalDefinition.Construct(
                                                                    m2Tuple.TupleUnderlyingType.TypeArgumentsNoUseSiteDiagnostics.RemoveAt(7).
                                                                    Add(TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType,
                                                                                ImmutableArray.Create("Item1", "Item2")))
                                                                    ),
                                                          ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9"));

                Assert.False(t1.Equals(t12));
                AssertTupleTypeMembersEquality(t1, t12);
                Assert.True(t1.Equals(t12, false, true));
                Assert.True(t12.Equals(t1, false, true));
                Assert.False(t1.TupleUnderlyingType.Equals(t12.TupleUnderlyingType));
                Assert.True(t1.TupleUnderlyingType.Equals(t12.TupleUnderlyingType, false, true));
                Assert.False(t12.TupleUnderlyingType.Equals(t1.TupleUnderlyingType));
                Assert.True(t12.TupleUnderlyingType.Equals(t1.TupleUnderlyingType, false, true));

                AssertTestDisplayString(t12.GetMembers(),
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item1",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item2",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item3",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item4",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item5",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item6",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item7",
                    "(System.Int32 Item1, System.Int32 Item2) (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Rest",
                    "(System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)..ctor()",
                    "(System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        "..ctor(System.Int32 item1, System.Int32 item2, System.Int32 item3, System.Int32 item4, System.Int32 item5, System.Int32 item6, System.Int32 item7, (System.Int32 Item1, System.Int32 Item2) rest)",
                    "System.Boolean (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Equals(System.Object obj)",
                    "System.Boolean (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".Equals((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
                    "System.Boolean (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".System.Collections.IStructuralEquatable.Equals(System.Object other, System.Collections.IEqualityComparer comparer)",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".System.IComparable.CompareTo(System.Object other)",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".CompareTo((System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32) other)",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".System.Collections.IStructuralComparable.CompareTo(System.Object other, System.Collections.IComparer comparer)",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".GetHashCode()",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".System.ITupleInternal.GetHashCode(System.Collections.IEqualityComparer comparer)",
                    "System.String (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).ToString()",
                    "System.String (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".System.ITupleInternal.ToStringEnd()",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".System.ITupleInternal.Size.get",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9)" +
                        ".System.ITupleInternal.Size { get; }",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item8",
                    "System.Int32 (System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 Item9).Item9"
                    );

                var t13 = TupleTypeSymbol.Create(m2Tuple.TupleUnderlyingType.OriginalDefinition.Construct(
                                                                    m2Tuple.TupleUnderlyingType.TypeArgumentsNoUseSiteDiagnostics.RemoveAt(7).
                                                                    Add(TupleTypeSymbol.Create(m1Tuple.TupleUnderlyingType,
                                                                                ImmutableArray.Create("a", "b")))
                                                                    ),
                                                          ImmutableArray.Create("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "item9"));

                Assert.Equal("(System.Int32 Item1, System.Int32 Item2, System.Int32 Item3, System.Int32 Item4, System.Int32 Item5, System.Int32 Item6, System.Int32 Item7, System.Int32 Item8, System.Int32 item9)", 
                             t13.ToTestDisplayString());
            }

            var m3Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M3").ReturnType;
            {
                var t1 = TupleTypeSymbol.Create(m3Tuple.TupleUnderlyingType);
                var t2 = TupleTypeSymbol.Create(m3Tuple.TupleUnderlyingType);

                Assert.True(t1.Equals(t2));
                AssertTupleTypeMembersEquality(t1, t2);

                var t3 = TupleTypeSymbol.Create(m3Tuple.TupleUnderlyingType, ImmutableArray.Create("a", "b", "c"));
                var t4 = TupleTypeSymbol.Create(m3Tuple.TupleUnderlyingType, ImmutableArray.Create("a", "b", "c"));
                var t5 = TupleTypeSymbol.Create(m3Tuple.TupleUnderlyingType, ImmutableArray.Create("c", "b", "a"));

                Assert.False(t1.Equals(t3));
                Assert.True(t1.Equals(t3, false, true));
                Assert.True(t3.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t3);

                Assert.True(t3.Equals(t4));
                AssertTupleTypeMembersEquality(t3, t4);

                Assert.False(t5.Equals(t3));
                Assert.True(t5.Equals(t3, false, true));
                Assert.True(t3.Equals(t5, false, true));
                AssertTupleTypeMembersEquality(t5, t3);

                var t6 = TupleTypeSymbol.Create(m3Tuple.TupleUnderlyingType, ImmutableArray.Create("Item1", "Item2", "Item3"));
                var t7 = TupleTypeSymbol.Create(m3Tuple.TupleUnderlyingType, ImmutableArray.Create("Item1", "Item2", "Item3"));

                Assert.True(t6.Equals(t7));
                AssertTupleTypeMembersEquality(t6, t7);

                Assert.False(t1.Equals(t6));
                Assert.True(t1.Equals(t6, false, true));
                Assert.True(t6.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t6);

                var t8 = TupleTypeSymbol.Create(m3Tuple.TupleUnderlyingType, ImmutableArray.Create("Item2", "Item3", "Item1"));

                Assert.False(t1.Equals(t8));
                Assert.True(t1.Equals(t8, false, true));
                Assert.True(t8.Equals(t1, false, true));
                AssertTupleTypeMembersEquality(t1, t8);

                Assert.False(t6.Equals(t8));
                Assert.True(t6.Equals(t8, false, true));
                Assert.True(t8.Equals(t6, false, true));
                AssertTupleTypeMembersEquality(t6, t8);
            }
        }

        private static void AssertTestDisplayString(ImmutableArray<Symbol> symbols, params string[] baseLine)
        {
            int common = Math.Min(symbols.Length, baseLine.Length);
            for (int i = 0; i < common; i++)
            {
                var actual = symbols[i].ToTestDisplayString();
                Assert.Equal(baseLine[i], actual);
            }

            Assert.Equal(new string[] { }, symbols.Skip(common).Select(s => s.ToTestDisplayString()).ToArray());
            Assert.Equal(baseLine.Length, symbols.Length);
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_01()
        {
            var source1 = @"
class C
{
    static void Main()
    {
        var v1 = Test.M1();
        System.Console.WriteLine(v1.Item8);
        System.Console.WriteLine(v1.Item9);
    }
}
";

            var source2 = @"
using System;
public class Test
{
    public static ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>> M1()
    {
        return (1, 2, 3, 4, 5, 6, 7, 8, 9);
    }
}
";

            var comp1 = CreateCompilationWithMscorlib(source2 + source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                     options: TestOptions.ReleaseExe);

            UnifyUnderlyingWithTuple_01_AssertCompilation(comp1);

            var comp2 = CreateCompilationWithMscorlib(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                     options: TestOptions.ReleaseDll);

            var comp2CompilationRef = comp2.ToMetadataReference();
            var comp3 = CreateCompilationWithMscorlib45(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, comp2CompilationRef },
                                                     options: TestOptions.ReleaseExe);

            Assert.NotSame(comp2.Assembly, (AssemblySymbol)comp3.GetAssemblyOrModuleSymbol(comp2CompilationRef)); // We are interested in retargeting scenario
            UnifyUnderlyingWithTuple_01_AssertCompilation(comp3);

            var comp4 = CreateCompilationWithMscorlib(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, comp2.EmitToImageReference() },
                                                     options: TestOptions.ReleaseExe);
            UnifyUnderlyingWithTuple_01_AssertCompilation(comp4);
        }

        private void UnifyUnderlyingWithTuple_01_AssertCompilation(CSharpCompilation comp)
        {
            CompileAndVerify(comp, expectedOutput:
@"8
9
");
            var test = comp.GetTypeByMetadataName("Test");

            var m1Tuple = (NamedTypeSymbol)test.GetMember<MethodSymbol>("M1").ReturnType;
            Assert.True(m1Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         m1Tuple.ToTestDisplayString());
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        System.Console.WriteLine(nameof(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>));
        System.Console.WriteLine(typeof(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>>));
        System.Console.WriteLine(typeof((int, int, int, int, int, int, int, int, int)));
        System.Console.WriteLine(typeof((int a, int b, int c, int d, int e, int f, int g, int h, int i)));
        System.Console.WriteLine(typeof(ValueTuple<,>));
        System.Console.WriteLine(typeof(ValueTuple<,,,,,,,>));
    }
}
";

            var comp = CompileAndVerify(source,
                                        additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                        expectedOutput:
@"ValueTuple
System.ValueTuple`8[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.ValueTuple`2[System.Int32,System.Int32]]
System.ValueTuple`8[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.ValueTuple`2[System.Int32,System.Int32]]
System.ValueTuple`8[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.ValueTuple`2[System.Int32,System.Int32]]
System.ValueTuple`2[T1,T2]
System.ValueTuple`8[T1,T2,T3,T4,T5,T6,T7,TRest]
");

            var c = (CSharpCompilation)comp.Compilation;
            var tree = c.SyntaxTrees.Single();
            var model = c.GetSemanticModel(tree);

            var nameofNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "nameof").Single();
            var nameofArg = ((InvocationExpressionSyntax)nameofNode.Parent).ArgumentList.Arguments.Single().Expression;
            var nameofArgSymbolInfo = model.GetSymbolInfo(nameofArg);
            Assert.True(((TypeSymbol)nameofArgSymbolInfo.Symbol).IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)", 
                         nameofArgSymbolInfo.Symbol.ToTestDisplayString());

            var typeofNodes = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().ToArray();
            Assert.Equal(5, typeofNodes.Length);
            for (int i = 0; i < typeofNodes.Length; i++)
            {
                var t = typeofNodes[i];
                var typeInfo = model.GetTypeInfo(t.Type);
                var symbolInfo = model.GetSymbolInfo(t.Type);
                Assert.Same(typeInfo.Type, symbolInfo.Symbol);

                switch (i)
                {
                    case 0:
                    case 1:
                        Assert.True(typeInfo.Type.IsTupleType);
                        Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                                     typeInfo.Type.ToTestDisplayString());
                        break;
                    case 2:
                        Assert.True(typeInfo.Type.IsTupleType);
                        Assert.Equal("(System.Int32 a, System.Int32 b, System.Int32 c, System.Int32 d, System.Int32 e, System.Int32 f, System.Int32 g, System.Int32 h, System.Int32 i)",
                                     typeInfo.Type.ToTestDisplayString());
                        break;

                    default:
                        Assert.False(typeInfo.Type.IsTupleType);
                        Assert.True(((NamedTypeSymbol)typeInfo.Type).IsUnboundGenericType);
                        break;
                }
            }
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_03()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(nameof((int, int)));
        System.Console.WriteLine(nameof((int a, int b)));
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,42): error CS1525: Invalid expression term 'int'
                //         System.Console.WriteLine(nameof((int, int)));
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 42),
                // (6,47): error CS1525: Invalid expression term 'int'
                //         System.Console.WriteLine(nameof((int, int)));
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 47),
                // (7,55): error CS1003: Syntax error, '=>' expected
                //         System.Console.WriteLine(nameof((int a, int b)));
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("=>", ")").WithLocation(7, 55),
                // (7,55): error CS1525: Invalid expression term ')'
                //         System.Console.WriteLine(nameof((int a, int b)));
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(7, 55)
                );
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_04()
        {
            var source1 = @"
class C
{
    static void Main()
    {
        var v1 = Test.M1();
        System.Console.WriteLine(v1.Rest.a);
        System.Console.WriteLine(v1.Rest.b);
    }
}
";

            var source2 = @"
using System;
public class Test
{
    public static ValueTuple<int, int, int, int, int, int, int, (int a, int b)> M1()
    {
        return (1, 2, 3, 4, 5, 6, 7, 8, 9);
    }
}
";

            var comp1 = CreateCompilationWithMscorlib(source2 + source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                     options: TestOptions.ReleaseExe);

            UnifyUnderlyingWithTuple_04_AssertCompilation(comp1);

            var comp2 = CreateCompilationWithMscorlib(source2, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                     options: TestOptions.ReleaseDll);

            var comp2CompilationRef = comp2.ToMetadataReference();
            var comp3 = CreateCompilationWithMscorlib45(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, comp2CompilationRef },
                                                     options: TestOptions.ReleaseExe);

            Assert.NotSame(comp2.Assembly, (AssemblySymbol)comp3.GetAssemblyOrModuleSymbol(comp2CompilationRef)); // We are interested in retargeting scenario
            UnifyUnderlyingWithTuple_04_AssertCompilation(comp3);

            var comp4 = CreateCompilationWithMscorlib(source1, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef, comp2.EmitToImageReference() },
                                                     options: TestOptions.ReleaseExe);
            UnifyUnderlyingWithTuple_04_AssertCompilation(comp4);
        }

        private void UnifyUnderlyingWithTuple_04_AssertCompilation(CSharpCompilation comp)
        {
            CompileAndVerify(comp, expectedOutput:
@"8
9
");
            var test = comp.GetTypeByMetadataName("Test");

            var m1Tuple = (NamedTypeSymbol)test.GetMember<MethodSymbol>("M1").ReturnType;
            Assert.True(m1Tuple.IsTupleType);
            Assert.Equal("ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>",
                         m1Tuple.ToTestDisplayString());
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_05()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        var v1 = Test<ValueTuple<int, int>>.M1((1, 2, 3, 4, 5, 6, 7, 8, 9));
        System.Console.WriteLine(v1.Item8);
        System.Console.WriteLine(v1.Item9);

        var v2 = Test<(int a, int b)>.M2((1, 2, 3, 4, 5, 6, 7, 10, 11));
        System.Console.WriteLine(v2.Item8);
        System.Console.WriteLine(v2.Item9);
        System.Console.WriteLine(v2.Rest.a);
        System.Console.WriteLine(v2.Rest.b);

        Test<ValueTuple<int, int>>.F1 = (1, 2, 3, 4, 5, 6, 7, 12, 13);
        var v3 = Test<ValueTuple<int, int>>.F1;
        System.Console.WriteLine(v3.Item8);
        System.Console.WriteLine(v3.Item9);

        Test<ValueTuple<int, int>>.P1 = (1, 2, 3, 4, 5, 6, 7, 14, 15);
        var v4 = Test<ValueTuple<int, int>>.P1;
        System.Console.WriteLine(v4.Item8);
        System.Console.WriteLine(v4.Item9);

        var v5 = Test<ValueTuple<int, int>>.M3((1, 2, 3, 4, 5, 6, 7, 16, 17));
        System.Console.WriteLine(v5[0].Item8);
        System.Console.WriteLine(v5[0].Item9);

        var v6 = Test<ValueTuple<int, int>>.M4((1, 2, 3, 4, 5, 6, 7, 18, 19));
        System.Console.WriteLine(v6[0].Item8);
        System.Console.WriteLine(v6[0].Item9);

        var v7 = (new Test33()).M5((1, 2, 3, 4, 5, 6, 7, 20, 21));
        System.Console.WriteLine(v7.Item8);
        System.Console.WriteLine(v7.Item9);

        var v8 = (1, 2).M6((1, 2, 3, 4, 5, 6, 7, 22, 23));
        System.Console.WriteLine(v8.Item8);
        System.Console.WriteLine(v8.Item9);
    }
}

class Test<T> where T : struct
{
    public static ValueTuple<int, int, int, int, int, int, int, T> M1(ValueTuple<int, int, int, int, int, int, int, T> val)
    {
        return val;
    }

    public static ValueTuple<int, int, int, int, int, int, int, T> M2(ValueTuple<int, int, int, int, int, int, int, T> val)
    {
        return val;
    }

    public static ValueTuple<int, int, int, int, int, int, int, T> F1;

    public static ValueTuple<int, int, int, int, int, int, int, T> P1 {get; set;}

    public static ValueTuple<int, int, int, int, int, int, int, T>[] M3(ValueTuple<int, int, int, int, int, int, int, T> val)
    {
        return new [] {val};
    }

    public static List<ValueTuple<int, int, int, int, int, int, int, T>> M4(ValueTuple<int, int, int, int, int, int, int, T> val)
    {
        return new List<ValueTuple<int, int, int, int, int, int, int, T>>() {val};
    }
}

abstract class Test31<T>
{
    public abstract U M5<U>(U val) where U : T;
}

abstract class Test32<T> : Test31<ValueTuple<int, int, int, int, int, int, int, T>>  where T : struct { }

class Test33 : Test32<ValueTuple<int, int>>
{
    public override U M5<U>(U val)
    {
        return val;
    }
}

static class Test4
{
    public static ValueTuple<int, int, int, int, int, int, int, T> M6<T>(this T target, ValueTuple<int, int, int, int, int, int, int, T> val) where T : struct
    {
        return val;
    }
}
";

            var comp = CompileAndVerify(source,
                                        additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef, SystemCoreRef },
                                        options: TestOptions.ReleaseExe.WithAllowUnsafe(true),
                                        expectedOutput:
@"8
9
10
11
10
11
12
13
14
15
16
17
18
19
20
21
22
23
");

            var test = ((CSharpCompilation)comp.Compilation).GetTypeByMetadataName("Test`1");

            var m1Tuple = (NamedTypeSymbol)test.GetMember<MethodSymbol>("M1").ReturnType;
            Assert.False(m1Tuple.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>",
                         m1Tuple.ToTestDisplayString());

            m1Tuple = (NamedTypeSymbol)test.GetMember<MethodSymbol>("M1").Parameters[0].Type;
            Assert.False(m1Tuple.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>",
                         m1Tuple.ToTestDisplayString());

            var c = (CSharpCompilation)comp.Compilation;
            var tree = c.SyntaxTrees.Single();
            var model = c.GetSemanticModel(tree);

            var m1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "M1").Single();
            var symbolInfo = model.GetSymbolInfo(m1);
            m1Tuple = (NamedTypeSymbol)((MethodSymbol)symbolInfo.Symbol).ReturnType;
            Assert.True(m1Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         m1Tuple.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)",
                         m1Tuple.GetMember<FieldSymbol>("Rest").Type.ToTestDisplayString());

            m1Tuple = (NamedTypeSymbol)((MethodSymbol)symbolInfo.Symbol).Parameters[0].Type;
            Assert.True(m1Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         m1Tuple.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)",
                         m1Tuple.GetMember<FieldSymbol>("Rest").Type.ToTestDisplayString());

            var m2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "M2").Single();
            symbolInfo = model.GetSymbolInfo(m2);
            var m2Tuple = (NamedTypeSymbol)((MethodSymbol)symbolInfo.Symbol).ReturnType;
            Assert.True(m2Tuple.IsTupleType);
            Assert.Equal("ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, (System.Int32 a, System.Int32 b)>",
                         m2Tuple.ToTestDisplayString());
            Assert.Equal("(System.Int32 a, System.Int32 b)",
                         m2Tuple.GetMember<FieldSymbol>("Rest").Type.ToTestDisplayString());

            var f1Tuple = (NamedTypeSymbol)test.GetMember<FieldSymbol>("F1").Type;
            Assert.False(f1Tuple.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>",
                         f1Tuple.ToTestDisplayString());

            var f1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "F1").First();
            symbolInfo = model.GetSymbolInfo(f1);
            f1Tuple = (NamedTypeSymbol)((FieldSymbol)symbolInfo.Symbol).Type;
            Assert.True(f1Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         f1Tuple.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)",
                         f1Tuple.GetMember<FieldSymbol>("Rest").Type.ToTestDisplayString());

            var p1Tuple = (NamedTypeSymbol)test.GetMember<PropertySymbol>("P1").Type;
            Assert.False(p1Tuple.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>",
                         p1Tuple.ToTestDisplayString());

            var p1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "P1").First();
            symbolInfo = model.GetSymbolInfo(p1);
            p1Tuple = (NamedTypeSymbol)((PropertySymbol)symbolInfo.Symbol).Type;
            Assert.True(p1Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         p1Tuple.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)",
                         p1Tuple.GetMember<FieldSymbol>("Rest").Type.ToTestDisplayString());

            var m3TupleArray = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("M3").ReturnType;
            Assert.False(m3TupleArray.ElementType.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>[]",
                         m3TupleArray.ToTestDisplayString());

            Assert.Equal("System.Collections.Generic.IList<System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>>",
                         m3TupleArray.Interfaces[0].ToTestDisplayString());

            var m3 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "M3").Single();
            symbolInfo = model.GetSymbolInfo(m3);
            m3TupleArray = (ArrayTypeSymbol)((MethodSymbol)symbolInfo.Symbol).ReturnType;
            Assert.True(m3TupleArray.ElementType.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)[]",
                         m3TupleArray.ToTestDisplayString());

            Assert.Equal("System.Collections.Generic.IList<(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)>",
                         m3TupleArray.Interfaces[0].ToTestDisplayString());

            var m4TupleList = (NamedTypeSymbol)test.GetMember<MethodSymbol>("M4").ReturnType;
            Assert.False(m4TupleList.TypeArguments[0].IsTupleType);
            Assert.Equal("System.Collections.Generic.List<System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>>",
                         m4TupleList.ToTestDisplayString());

            Assert.Equal("System.Collections.Generic.IList<System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>>",
                         m4TupleList.Interfaces[0].ToTestDisplayString());

            var m4 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "M4").Single();
            symbolInfo = model.GetSymbolInfo(m4);
            m4TupleList = (NamedTypeSymbol)((MethodSymbol)symbolInfo.Symbol).ReturnType;
            Assert.True(m4TupleList.TypeArguments[0].IsTupleType);
            Assert.Equal("System.Collections.Generic.List<(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)>",
                         m4TupleList.ToTestDisplayString());

            var m5 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "M5").Single();
            symbolInfo = model.GetSymbolInfo(m5);
            var m5Tuple = ((MethodSymbol)symbolInfo.Symbol).TypeParameters[0].ConstraintTypes.Single();
            Assert.True(m5Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         m5Tuple.ToTestDisplayString());

            var m6 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "M6").Single();
            symbolInfo = model.GetSymbolInfo(m6);
            var m6Method = (MethodSymbol)symbolInfo.Symbol;
            Assert.Equal(MethodKind.ReducedExtension, m6Method.MethodKind);

            var m6Tuple = m6Method.ReturnType;
            Assert.True(m6Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         m6Tuple.ToTestDisplayString());

            m6Tuple = m6Method.Parameters.Last().Type;
            Assert.True(m6Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         m6Tuple.ToTestDisplayString());
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_06()
        {
            var source = @"
using System;
unsafe class C
{
    static void Main()
    {
        var x = Test<ValueTuple<int, int>>.E1;

        var v7 = Test<ValueTuple<int, int>>.M5();
        System.Console.WriteLine(v7->Item8);
        System.Console.WriteLine(v7->Item9);

        Test1<ValueTuple<int, int>> v1 = null;
        System.Console.WriteLine(v1.Item8);

        ITest2<ValueTuple<int, int>> v2 = null;
        System.Console.WriteLine(v2.Item8);
    }
}

unsafe class Test<T> where T : struct
{
    public static event ValueTuple<int, int, int, int, int, int, int, T> E1;

    public static ValueTuple<int, int, int, int, int, int, int, T>* M5()
    {
        return null;
    }
}

class Test1<T> : ValueTuple<int, int, int, int, int, int, int, T> where T : struct
{
}

interface ITest2<T> : ValueTuple<int, int, int, int, int, int, int, T> where T : struct
{
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                     options: TestOptions.ReleaseExe.WithAllowUnsafe(true));

            comp.VerifyDiagnostics(
                // (31,7): error CS0509: 'Test1<T>': cannot derive from sealed type 'ValueTuple<int, int, int, int, int, int, int, T>'
                // class Test1<T> : ValueTuple<int, int, int, int, int, int, int, T>
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "Test1").WithArguments("Test1<T>", "System.ValueTuple<int, int, int, int, int, int, int, T>").WithLocation(31, 7),
                // (35,23): error CS0527: Type 'ValueTuple<int, int, int, int, int, int, int, T>' in interface list is not an interface
                // interface ITest2<T> : ValueTuple<int, int, int, int, int, int, int, T>
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "ValueTuple<int, int, int, int, int, int, int, T>").WithArguments("System.ValueTuple<int, int, int, int, int, int, int, T>").WithLocation(35, 23),
                // (25,19): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('ValueTuple<int, int, int, int, int, int, int, T>')
                //     public static ValueTuple<int, int, int, int, int, int, int, T>* M5()
                Diagnostic(ErrorCode.ERR_ManagedAddr, "ValueTuple<int, int, int, int, int, int, int, T>*").WithArguments("System.ValueTuple<int, int, int, int, int, int, int, T>").WithLocation(25, 19),
                // (23,74): error CS0066: 'Test<T>.E1': event must be of a delegate type
                //     public static event ValueTuple<int, int, int, int, int, int, int, T> E1;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "E1").WithArguments("Test<T>.E1").WithLocation(23, 74),
                // (7,44): error CS0070: The event 'Test<(int, int)>.E1' can only appear on the left hand side of += or -= (except when used from within the type 'Test<(int, int)>')
                //         var x = Test<ValueTuple<int, int>>.E1;
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E1").WithArguments("Test<(int, int)>.E1", "Test<(int, int)>").WithLocation(7, 44),
                // (14,37): error CS1061: 'Test1<(int, int)>' does not contain a definition for 'Item8' and no extension method 'Item8' accepting a first argument of type 'Test1<(int, int)>' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v1.Item8);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item8").WithArguments("Test1<(int, int)>", "Item8").WithLocation(14, 37),
                // (17,37): error CS1061: 'ITest2<(int, int)>' does not contain a definition for 'Item8' and no extension method 'Item8' accepting a first argument of type 'ITest2<(int, int)>' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v2.Item8);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item8").WithArguments("ITest2<(int, int)>", "Item8").WithLocation(17, 37)
                );

            var test = comp.GetTypeByMetadataName("Test`1");

            var e1Tuple = (NamedTypeSymbol)test.GetMember<EventSymbol>("E1").Type;
            Assert.False(e1Tuple.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>",
                         e1Tuple.ToTestDisplayString());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var e1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "E1").Single();
            var symbolInfo = model.GetSymbolInfo(e1);
            e1Tuple = (NamedTypeSymbol)((EventSymbol)symbolInfo.CandidateSymbols.Single()).Type;
            Assert.True(e1Tuple.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)",
                         e1Tuple.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.Int32)",
                         e1Tuple.GetMember<FieldSymbol>("Rest").Type.ToTestDisplayString());

            var m5TuplePointer = (PointerTypeSymbol)test.GetMember<MethodSymbol>("M5").ReturnType;
            Assert.False(m5TuplePointer.PointedAtType.IsTupleType);
            Assert.Equal("System.ValueTuple<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, T>*",
                         m5TuplePointer.ToTestDisplayString());

            var m5 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "M5").Single();
            symbolInfo = model.GetSymbolInfo(m5);
            m5TuplePointer = (PointerTypeSymbol)((MethodSymbol)symbolInfo.Symbol).ReturnType;
            Assert.True(m5TuplePointer.PointedAtType.IsTupleType);
            Assert.Equal("(System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32)*",
                         m5TuplePointer.ToTestDisplayString());

            var v1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "v1").Single();
            symbolInfo = model.GetSymbolInfo(v1);
            var v1Type = ((LocalSymbol)symbolInfo.Symbol).Type;
            Assert.Equal("Test1<(System.Int32, System.Int32)>", v1Type.ToTestDisplayString());

            var v1Tuple = v1Type.BaseType;
            Assert.False(v1Tuple.IsTupleType);
            Assert.Equal("System.Object",
                         v1Tuple.ToTestDisplayString());

            var v2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "v2").Single();
            symbolInfo = model.GetSymbolInfo(v2);
            var v2Type = ((LocalSymbol)symbolInfo.Symbol).Type;
            Assert.Equal("ITest2<(System.Int32, System.Int32)>", v2Type.ToTestDisplayString());
            Assert.True(v2Type.Interfaces.IsEmpty);
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_07()
        {
            var source1 = @"
using System;
public class Test<T>
{
    public static ValueTuple<int, int, int, int, int, int, int, T> M1()
    {
       throw new NotImplementedException();
    }
}
" + trivialRemainingTuples;

            var source2 = @"
class C
{
    static void Main()
    {
        var v1 = Test<(int, int, int, int, int, int, int, int, int)>.M1();
        System.Console.WriteLine(v1.Item8);
        System.Console.WriteLine(v1.Item9);
        System.Console.WriteLine(v1.Rest.Item8);
        System.Console.WriteLine(v1.Rest.Item9);
    }
}
" + trivial2uple + trivialRemainingTuples;

            var comp1 = CreateCompilationWithMscorlib(source1, 
                                                     options: TestOptions.ReleaseDll);

            comp1.VerifyDiagnostics();

            var comp2 = CreateCompilationWithMscorlib(source2, references: new[] { comp1.ToMetadataReference() },
                                                     options: TestOptions.ReleaseExe);

            comp2.VerifyDiagnostics(
                // (7,37): error CS1061: 'ValueTuple<int, int, int, int, int, int, int, (int, int, int, int, int, int, int, int, int)>' does not contain a definition for 'Item8' and no extension method 'Item8' accepting a first argument of type 'ValueTuple<int, int, int, int, int, int, int, (int, int, int, int, int, int, int, int, int)>' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v1.Item8);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item8").WithArguments("System.ValueTuple<int, int, int, int, int, int, int, (int, int, int, int, int, int, int, int, int)>", "Item8").WithLocation(7, 37),
                // (8,37): error CS1061: 'ValueTuple<int, int, int, int, int, int, int, (int, int, int, int, int, int, int, int, int)>' does not contain a definition for 'Item9' and no extension method 'Item9' accepting a first argument of type 'ValueTuple<int, int, int, int, int, int, int, (int, int, int, int, int, int, int, int, int)>' could be found (are you missing a using directive or an assembly reference?)
                //         System.Console.WriteLine(v1.Item9);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item9").WithArguments("System.ValueTuple<int, int, int, int, int, int, int, (int, int, int, int, int, int, int, int, int)>", "Item9").WithLocation(8, 37)
                );
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_08()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        var x = (1, 3);
        string s = x;
        System.Console.WriteLine(s);
        System.Console.WriteLine((long)x);

        (int, string) y = new KeyValuePair<int, string>(2, ""4"");
        System.Console.WriteLine(y);
        System.Console.WriteLine((ValueTuple<string, string>)""5"");

        System.Console.WriteLine(+x);
        System.Console.WriteLine(-x);
        System.Console.WriteLine(!x);
        System.Console.WriteLine(~x);
        System.Console.WriteLine(++x);
        System.Console.WriteLine(--x);
        System.Console.WriteLine(x ? true : false);
        System.Console.WriteLine(!x ? true : false);

        System.Console.WriteLine(x + 1);
        System.Console.WriteLine(x - 1);
        System.Console.WriteLine(x * 3);
        System.Console.WriteLine(x / 2);
        System.Console.WriteLine(x % 3);
        System.Console.WriteLine(x & 3);
        System.Console.WriteLine(x | 15);
        System.Console.WriteLine(x ^ 4);
        System.Console.WriteLine(x << 1);
        System.Console.WriteLine(x >> 1);
        System.Console.WriteLine(x == 1);
        System.Console.WriteLine(x != 1);
        System.Console.WriteLine(x > 1);
        System.Console.WriteLine(x < 1);
        System.Console.WriteLine(x >= 1);
        System.Console.WriteLine(x <= 1);
    }
}


namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public static implicit operator string(ValueTuple<T1, T2> arg)
        {
            return arg.ToString();
        }

        public static explicit operator long(ValueTuple<T1, T2> arg)
        {
            return ((long)(int)(object)arg.Item1 + (long)(int)(object)arg.Item2);
        }

        public static implicit operator ValueTuple<T1, T2>(System.Collections.Generic.KeyValuePair<T1, T2> arg)
        {
            return new ValueTuple<T1, T2>(arg.Key, arg.Value);
        }

        public static explicit operator ValueTuple<T1, T2>(string arg)
        {
            return new ValueTuple<T1, T2>((T1)(object)arg, (T2)(object)arg);
        }


        public static ValueTuple<T1, T2> operator +(ValueTuple<T1, T2> arg)
        {
            return arg;
        }

        public static long operator -(ValueTuple<T1, T2> arg)
        {
            return -(long)arg;
        }

        public static bool operator !(ValueTuple<T1, T2> arg)
        {
            return (long)arg == 0;
        }

        public static long operator ~(ValueTuple<T1, T2> arg)
        {
            return -(long)arg;
        }

        public static ValueTuple<T1, T2> operator ++(ValueTuple<T1, T2> arg)
        {
            return new ValueTuple<T1, T2>((T1)(object)((int)(object)arg.Item1+1), (T2)(object)((int)(object)arg.Item2+1));
        }

        public static ValueTuple<T1, T2> operator --(ValueTuple<T1, T2> arg)
        {
            return new ValueTuple<T1, T2>((T1)(object)((int)(object)arg.Item1 - 1), (T2)(object)((int)(object)arg.Item2 - 1));
        }

        public static bool operator true(ValueTuple<T1, T2> arg)
        {
            return (long)arg != 0;
        }

        public static bool operator false(ValueTuple<T1, T2> arg)
        {
            return (long)arg == 0;
        }

        public static long operator + (ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 + arg2;
        }

        public static long operator -(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 - arg2;
        }

        public static long operator *(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 * arg2;
        }

        public static long operator /(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 / arg2;
        }

        public static long operator %(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 % arg2;
        }

        public static long operator &(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 & arg2;
        }

        public static long operator |(ValueTuple<T1, T2> arg1, long arg2)
        {
            return (long)arg1 | arg2;
        }

        public static long operator ^(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 ^ arg2;
        }
        public static long operator <<(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 << arg2;
        }

        public static long operator >>(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 >> arg2;
        }

        public static bool operator ==(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 == arg2;
        }

        public static bool operator !=(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 != arg2;
        }

        public static bool operator >(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 > arg2;
        }

        public static bool operator <(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 < arg2;
        }

        public static bool operator >=(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 >= arg2;
        }

        public static bool operator <=(ValueTuple<T1, T2> arg1, int arg2)
        {
            return (long)arg1 <= arg2;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular,
                options: TestOptions.ReleaseExe.WithAllowUnsafe(true),
                expectedOutput:
@"{1, 3}
4
{2, 4}
{5, 5}
{1, 3}
-4
False
-4
{2, 4}
{1, 3}
True
False
5
3
12
2
1
0
15
0
8
2
False
True
True
False
True
False
");
        }

        [Fact]
        public void UnifyUnderlyingWithTuple_09()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        var x = (1, 3);
        string s = x;
        System.Console.WriteLine(s);
        System.Console.WriteLine((long)x);

        (int, string) y = new KeyValuePair<int, string>(2, ""4"");
        System.Console.WriteLine(y);
        System.Console.WriteLine((ValueTuple<string, string>)""5"");

        System.Console.WriteLine(+x);
        System.Console.WriteLine(-x);
        System.Console.WriteLine(!x);
        System.Console.WriteLine(~x);
        System.Console.WriteLine(++x);
        System.Console.WriteLine(--x);
        System.Console.WriteLine(x ? true : false);
        System.Console.WriteLine(!x ? true : false);

        System.Console.WriteLine(x + 1);
        System.Console.WriteLine(x - 1);
        System.Console.WriteLine(x * 3);
        System.Console.WriteLine(x / 2);
        System.Console.WriteLine(x % 3);
        System.Console.WriteLine(x & 3);
        System.Console.WriteLine(x | 15);
        System.Console.WriteLine(x ^ 4);
        System.Console.WriteLine(x << 1);
        System.Console.WriteLine(x >> 1);
        System.Console.WriteLine(x == 1);
        System.Console.WriteLine(x != 1);
        System.Console.WriteLine(x > 1);
        System.Console.WriteLine(x < 1);
        System.Console.WriteLine(x >= 1);
        System.Console.WriteLine(x <= 1);
    }
}


namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public static implicit operator string((T1, T2) arg)
        {
            return arg.ToString();
        }

        public static explicit operator long((T1, T2) arg)
        {
            return ((long)(int)(object)arg.Item1 + (long)(int)(object)arg.Item2);
        }

        public static implicit operator (T1, T2)(System.Collections.Generic.KeyValuePair<T1, T2> arg)
        {
            return new (T1, T2)(arg.Key, arg.Value);
        }

        public static explicit operator (T1, T2)(string arg)
        {
            return new (T1, T2)((T1)(object)arg, (T2)(object)arg);
        }


        public static (T1, T2) operator +((T1, T2) arg)
        {
            return arg;
        }

        public static long operator -((T1, T2) arg)
        {
            return -(long)arg;
        }

        public static bool operator !((T1, T2) arg)
        {
            return (long)arg == 0;
        }

        public static long operator ~((T1, T2) arg)
        {
            return -(long)arg;
        }

        public static (T1, T2) operator ++((T1, T2) arg)
        {
            return new ValueTuple<T1, T2>((T1)(object)((int)(object)arg.Item1+1), (T2)(object)((int)(object)arg.Item2+1));
        }

        public static (T1, T2) operator --((T1, T2) arg)
        {
            return new ValueTuple<T1, T2>((T1)(object)((int)(object)arg.Item1 - 1), (T2)(object)((int)(object)arg.Item2 - 1));
        }

        public static bool operator true((T1, T2) arg)
        {
            return (long)arg != 0;
        }

        public static bool operator false((T1, T2) arg)
        {
            return (long)arg == 0;
        }

        public static long operator + ((T1, T2) arg1, int arg2)
        {
            return (long)arg1 + arg2;
        }

        public static long operator -((T1, T2) arg1, int arg2)
        {
            return (long)arg1 - arg2;
        }

        public static long operator *((T1, T2) arg1, int arg2)
        {
            return (long)arg1 * arg2;
        }

        public static long operator /((T1, T2) arg1, int arg2)
        {
            return (long)arg1 / arg2;
        }

        public static long operator %((T1, T2) arg1, int arg2)
        {
            return (long)arg1 % arg2;
        }

        public static long operator &((T1, T2) arg1, int arg2)
        {
            return (long)arg1 & arg2;
        }

        public static long operator |((T1, T2) arg1, long arg2)
        {
            return (long)arg1 | arg2;
        }

        public static long operator ^((T1, T2) arg1, int arg2)
        {
            return (long)arg1 ^ arg2;
        }
        public static long operator <<((T1, T2) arg1, int arg2)
        {
            return (long)arg1 << arg2;
        }

        public static long operator >>((T1, T2) arg1, int arg2)
        {
            return (long)arg1 >> arg2;
        }

        public static bool operator ==((T1, T2) arg1, int arg2)
        {
            return (long)arg1 == arg2;
        }

        public static bool operator !=((T1, T2) arg1, int arg2)
        {
            return (long)arg1 != arg2;
        }

        public static bool operator >((T1, T2) arg1, int arg2)
        {
            return (long)arg1 > arg2;
        }

        public static bool operator <((T1, T2) arg1, int arg2)
        {
            return (long)arg1 < arg2;
        }

        public static bool operator >=((T1, T2) arg1, int arg2)
        {
            return (long)arg1 >= arg2;
        }

        public static bool operator <=((T1, T2) arg1, int arg2)
        {
            return (long)arg1 <= arg2;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular,
                options: TestOptions.ReleaseExe.WithAllowUnsafe(true),
                expectedOutput:
@"{1, 3}
4
{2, 4}
{5, 5}
{1, 3}
-4
False
-4
{2, 4}
{1, 3}
True
False
5
3
12
2
1
0
15
0
8
2
False
True
True
False
True
False
");
        }

        [Fact]
        public void ConstructorInvocation_01()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        var v1 = new ValueTuple<int, int>(1, 2);
        System.Console.WriteLine(v1.ToString());

        var v2 = new ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int>> (10, 20, 30, 40, 50, 60, 70, new ValueTuple<int, int>(80, 90));
        System.Console.WriteLine(v2.ToString());

        var v3 = new ValueTuple<int, int, int, int, int, int, int, (int, int)> (100, 200, 300, 400, 500, 600, 700, (800, 900));
        System.Console.WriteLine(v3.ToString());
    }
}
";

            var comp = CompileAndVerify(source,
                                        additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                        options: TestOptions.ReleaseExe,
                                        expectedOutput:
@"(1, 2)
(10, 20, 30, 40, 50, 60, 70, 80, 90)
(100, 200, 300, 400, 500, 600, 700, 800, 900)
");
        }

        [Fact]
        public void PropertiesAsMembers_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var v1 = M1();
        v1.P1 = 33;
        System.Console.WriteLine(v1.P1);
        System.Console.WriteLine(v1[-1, -2]);
    }

    static (int, int) M1()
    {
        return (1, 11);
    }

    static (int a2, int b2) M2()
    {
        return (2, 22);
    }

    static (int, int) M3()
    {
        return (6, 66);
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.P1 = 0;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }

        public int P1 {get; set;}

        public int this[int a, int b]
        {
            get
            {
                return 44;
            }
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: 
@"33
44");

            var c = ((CSharpCompilation)comp.Compilation).GetTypeByMetadataName("C");

            var m1Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M1").ReturnType;

            AssertTestDisplayString(m1Tuple.GetMembers(),
                "System.Int32 (System.Int32, System.Int32).Item1",
                "System.Int32 (System.Int32, System.Int32).Item2",
                "(System.Int32, System.Int32)..ctor(System.Int32 item1, System.Int32 item2)",
                "System.String (System.Int32, System.Int32).ToString()",
                "System.Int32 (System.Int32, System.Int32).<P1>k__BackingField",
                "System.Int32 (System.Int32, System.Int32).P1 { get; set; }",
                "System.Int32 (System.Int32, System.Int32).P1.get",
                "void (System.Int32, System.Int32).P1.set",
                "System.Int32 (System.Int32, System.Int32).this[System.Int32 a, System.Int32 b] { get; }",
                "System.Int32 (System.Int32, System.Int32).this[System.Int32 a, System.Int32 b].get",
                "(System.Int32, System.Int32)..ctor()");

            AssertTupleTypeEquality(m1Tuple);
            Assert.Equal(new string[] { "Item1", "Item2", ".ctor", "ToString",
                                        "<P1>k__BackingField", "P1", "get_P1", "set_P1",
                                        "this[]", "get_Item"},
                         m1Tuple.MemberNames.ToArray());
            Assert.Equal(m1Tuple.TupleUnderlyingType.GetEarlyAttributeDecodingMembers().Select(m => m.Name).ToArray(),
                         m1Tuple.GetEarlyAttributeDecodingMembers().Select(m => m.Name).ToArray());
            Assert.Equal("System.Int32 (System.Int32, System.Int32).P1 { get; set; }", m1Tuple.GetEarlyAttributeDecodingMembers("P1").Single().ToTestDisplayString());

            var m2Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M2").ReturnType;
            var m3Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M3").ReturnType;

            AssertTupleTypeMembersEquality(m1Tuple, m2Tuple);
            AssertTupleTypeMembersEquality(m1Tuple, m3Tuple);

            var m1P1 = m1Tuple.GetMember<PropertySymbol>("P1");
            var m1P1Get = m1Tuple.GetMember<MethodSymbol>("get_P1");
            var m1P1Set = m1Tuple.GetMember<MethodSymbol>("set_P1");

            Assert.True(m1P1.IsTupleProperty);
            Assert.Equal(SymbolKind.Property, m1P1.Kind);
            Assert.Same(m1P1, m1P1.OriginalDefinition);
            Assert.True(m1P1.Equals(m1P1));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.P1 { get; set; }", m1P1.TupleUnderlyingProperty.ToTestDisplayString());
            Assert.Same(m1Tuple, m1P1.ContainingSymbol);
            Assert.Same(m1Tuple.TupleUnderlyingType, m1P1.TupleUnderlyingProperty.ContainingSymbol);
            Assert.True(m1P1.TypeCustomModifiers.IsEmpty);
            Assert.True(m1P1.GetAttributes().IsEmpty);
            Assert.Null(m1P1.GetUseSiteDiagnostic());
            Assert.True(m1P1.TupleUnderlyingProperty.Locations.SequenceEqual(m1P1.Locations));
            Assert.True(m1P1.TupleUnderlyingProperty.DeclaringSyntaxReferences.SequenceEqual(m1P1.DeclaringSyntaxReferences, SyntaxReferenceEqualityComparer.Instance));
            Assert.False(m1P1.IsImplicitlyDeclared);
            Assert.True(m1P1.Parameters.IsEmpty);
            Assert.Same(m1P1Get, m1P1.GetMethod);
            Assert.Same(m1P1Set, m1P1.SetMethod);
            Assert.False(m1P1.IsExplicitInterfaceImplementation);
            Assert.True(m1P1.ExplicitInterfaceImplementations.IsEmpty);
            Assert.Equal(m1P1.TupleUnderlyingProperty.MustCallMethodsDirectly, m1P1.MustCallMethodsDirectly);
            Assert.False(m1P1.IsIndexer);
            Assert.Null(m1P1.OverriddenProperty);

            Assert.Equal(m1P1Get.TupleUnderlyingMethod.MethodKind, m1P1Get.MethodKind);
            Assert.Same(m1P1, m1P1Get.AssociatedSymbol);
            Assert.False(m1P1Get.IsImplicitlyDeclared);
            Assert.False(m1P1Get.TupleUnderlyingMethod.IsImplicitlyDeclared);

            Assert.Equal(m1P1Set.TupleUnderlyingMethod.MethodKind, m1P1Set.MethodKind);
            Assert.Same(m1P1, m1P1Set.AssociatedSymbol);
            Assert.False(m1P1Set.IsImplicitlyDeclared);
            Assert.False(m1P1Set.TupleUnderlyingMethod.IsImplicitlyDeclared);

            var m1P1BackingField = m1Tuple.GetMember<FieldSymbol>("<P1>k__BackingField");
            Assert.Same(m1P1, m1P1BackingField.AssociatedSymbol);
            Assert.Equal(m1P1.TupleUnderlyingProperty, m1P1BackingField.TupleUnderlyingField.AssociatedSymbol);
            Assert.True(m1P1BackingField.IsImplicitlyDeclared);
            Assert.True(m1P1BackingField.TupleUnderlyingField.IsImplicitlyDeclared);

            var m1this = m1Tuple.GetMember<PropertySymbol>("this[]");
            var m1thisGet = m1Tuple.GetMember<MethodSymbol>("get_Item");

            Assert.True(m1this.IsTupleProperty);
            Assert.Equal(SymbolKind.Property, m1this.Kind);
            Assert.Same(m1this, m1this.OriginalDefinition);
            Assert.True(m1this.Equals(m1this));
            Assert.Equal("System.Int32 System.ValueTuple<System.Int32, System.Int32>.this[System.Int32 a, System.Int32 b] { get; }", m1this.TupleUnderlyingProperty.ToTestDisplayString());
            Assert.Same(m1Tuple, m1this.ContainingSymbol);
            Assert.Same(m1Tuple.TupleUnderlyingType, m1this.TupleUnderlyingProperty.ContainingSymbol);
            Assert.True(m1this.TypeCustomModifiers.IsEmpty);
            Assert.True(m1this.GetAttributes().IsEmpty);
            Assert.Null(m1this.GetUseSiteDiagnostic());
            Assert.True(m1this.TupleUnderlyingProperty.Locations.SequenceEqual(m1this.Locations));
            Assert.True(m1this.TupleUnderlyingProperty.DeclaringSyntaxReferences.SequenceEqual(m1this.DeclaringSyntaxReferences, SyntaxReferenceEqualityComparer.Instance));
            Assert.False(m1this.IsImplicitlyDeclared);
            Assert.Equal(2, m1this.Parameters.Length);
            Assert.Same(m1thisGet, m1this.GetMethod);
            Assert.Null(m1this.SetMethod);
            Assert.False(m1this.IsExplicitInterfaceImplementation);
            Assert.True(m1this.ExplicitInterfaceImplementations.IsEmpty);
            Assert.Equal(m1this.TupleUnderlyingProperty.MustCallMethodsDirectly, m1this.MustCallMethodsDirectly);
            Assert.True(m1this.IsIndexer);
            Assert.Null(m1this.OverriddenProperty);

            var m1thisParam1 = (TupleParameterSymbol)m1this.Parameters[0];
            Assert.True(m1thisParam1.UnderlyingParameter.Locations.SequenceEqual(m1thisParam1.Locations));
            Assert.True(m1thisParam1.UnderlyingParameter.DeclaringSyntaxReferences.SequenceEqual(m1thisParam1.DeclaringSyntaxReferences, SyntaxReferenceEqualityComparer.Instance));

            var m1thisGetParam1 = (TupleParameterSymbol)m1thisGet.Parameters[0];
            Assert.True(m1thisGetParam1.UnderlyingParameter.Locations.SequenceEqual(m1thisGetParam1.Locations));
            Assert.True(m1thisGetParam1.UnderlyingParameter.DeclaringSyntaxReferences.SequenceEqual(m1thisGetParam1.DeclaringSyntaxReferences, SyntaxReferenceEqualityComparer.Instance));

            Assert.Equal(m1thisGet.TupleUnderlyingMethod.MethodKind, m1thisGet.MethodKind);
            Assert.Same(m1this, m1thisGet.AssociatedSymbol);
            Assert.False(m1thisGet.IsImplicitlyDeclared);
        }

        private class SyntaxReferenceEqualityComparer : IEqualityComparer<SyntaxReference>
        {
            public static readonly SyntaxReferenceEqualityComparer Instance = new SyntaxReferenceEqualityComparer();
            private SyntaxReferenceEqualityComparer() { }

            public bool Equals(SyntaxReference x, SyntaxReference y)
            {
                return x.GetSyntax().Equals(y.GetSyntax());
            }

            public int GetHashCode(SyntaxReference obj)
            {
                return obj.GetHashCode();
            }
        }

        [Fact]
        public void EventsAsMembers_01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var v1 = M1();
        System.Action<int> d1 = (int i1) => System.Console.WriteLine(i1); 
        v1.Test(v1, d1);
        System.Action<long> d2 = (long i2) => System.Console.WriteLine(i2); 
        v1.E1 += d1;
        v1.RaiseE1();
        v1.E1 -= d1;
        v1.RaiseE1();
        v1.E2 += d2;
        v1.RaiseE2();
        v1.E2 -= d2;
        v1.RaiseE2();
    }

    static (int, long) M1()
    {
        return (1, 11);
    }

    static (int a2, long b2) M2()
    {
        return (2, 22);
    }

    static (int, long) M3()
    {
        return (6, 66);
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.E1 = null;
            this._e2 = null;
        }

        public event System.Action<T1> E1;

        private System.Action<T2> _e2;
        public event System.Action<T2> E2
        {
            add
            {
                _e2 += value;
            }
            remove
            {
                _e2 -= value;
            }
        }

        public void RaiseE1()
        {
            System.Console.WriteLine(""-"");
            if (E1 == null)
            {
                System.Console.WriteLine(""null"");
            }
            else
            {
                E1(Item1);
            }
            System.Console.WriteLine(""-"");
        }

        public void RaiseE2()
        {
            System.Console.WriteLine(""--"");
            if (_e2 == null)
            {
                System.Console.WriteLine(""null"");
            }
            else
            {
                _e2(Item2);
            }
            System.Console.WriteLine(""--"");
        }

        public void Test((T1, T2) val, System.Action<T1> d)
        {
            val.E1 += d;
            System.Console.WriteLine(val.E1);
            val.E1(val.Item1);
            val.E1 -= d;
            System.Console.WriteLine(val.E1 == null);
        }
    }
}
" + tupleattributes_cs;

            var comp = CompileAndVerify(source, expectedOutput:
@"System.Action`1[System.Int32]
1
True
-
1
-
-
null
-
--
11
--
--
null
--");

            var c = ((CSharpCompilation)comp.Compilation).GetTypeByMetadataName("C");

            var m1Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M1").ReturnType;

            AssertTestDisplayString(m1Tuple.GetMembers(),
                "System.Int32 (System.Int32, System.Int64).Item1",
                "System.Int64 (System.Int32, System.Int64).Item2",
                "(System.Int32, System.Int64)..ctor(System.Int32 item1, System.Int64 item2)",
                "void (System.Int32, System.Int64).E1.add",
                "void (System.Int32, System.Int64).E1.remove",
                "event System.Action<System.Int32> (System.Int32, System.Int64).E1",
                "System.Action<System.Int64> (System.Int32, System.Int64)._e2",
                "event System.Action<System.Int64> (System.Int32, System.Int64).E2",
                "void (System.Int32, System.Int64).E2.add",
                "void (System.Int32, System.Int64).E2.remove",
                "void (System.Int32, System.Int64).RaiseE1()",
                "void (System.Int32, System.Int64).RaiseE2()",
                "void (System.Int32, System.Int64).Test((System.Int32, System.Int64) val, System.Action<System.Int32> d)",
                "(System.Int32, System.Int64)..ctor()");

            AssertTupleTypeEquality(m1Tuple);
            Assert.Equal(new string[] { "Item1", "Item2", ".ctor", "add_E1", "remove_E1", "E1", "_e2", "E2", "add_E2", "remove_E2", "RaiseE1", "RaiseE2", "Test" },
                         m1Tuple.MemberNames.ToArray());
            Assert.Equal(m1Tuple.TupleUnderlyingType.GetEarlyAttributeDecodingMembers().Select(m => m.Name).ToArray(),
                         m1Tuple.GetEarlyAttributeDecodingMembers().Select(m => m.Name).ToArray());
            Assert.Equal("event System.Action<System.Int32> (System.Int32, System.Int64).E1", m1Tuple.GetEarlyAttributeDecodingMembers("E1").Single().ToTestDisplayString());
            Assert.Equal("event System.Action<System.Int64> (System.Int32, System.Int64).E2", m1Tuple.GetEarlyAttributeDecodingMembers("E2").Single().ToTestDisplayString());

            var m2Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M2").ReturnType;
            var m3Tuple = (NamedTypeSymbol)c.GetMember<MethodSymbol>("M3").ReturnType;

            AssertTupleTypeMembersEquality(m1Tuple, m2Tuple);
            AssertTupleTypeMembersEquality(m1Tuple, m3Tuple);

            var m1E1 = m1Tuple.GetMember<EventSymbol>("E1");
            var m1E1Add = m1Tuple.GetMember<MethodSymbol>("add_E1");
            var m1E1Remove = m1Tuple.GetMember<MethodSymbol>("remove_E1");

            Assert.True(m1E1.IsTupleEvent);
            Assert.Equal(SymbolKind.Event, m1E1.Kind);
            Assert.Same(m1E1, m1E1.OriginalDefinition);
            Assert.True(m1E1.Equals(m1E1));
            Assert.Equal("event System.Action<System.Int32> System.ValueTuple<System.Int32, System.Int64>.E1", m1E1.TupleUnderlyingEvent.ToTestDisplayString());
            Assert.Same(m1Tuple, m1E1.ContainingSymbol);
            Assert.Same(m1Tuple.TupleUnderlyingType, m1E1.TupleUnderlyingEvent.ContainingSymbol);
            Assert.True(m1E1.GetAttributes().IsEmpty);
            Assert.Null(m1E1.GetUseSiteDiagnostic());
            Assert.True(m1E1.TupleUnderlyingEvent.Locations.SequenceEqual(m1E1.Locations));
            Assert.True(m1E1.TupleUnderlyingEvent.DeclaringSyntaxReferences.SequenceEqual(m1E1.DeclaringSyntaxReferences, SyntaxReferenceEqualityComparer.Instance));
            Assert.False(m1E1.IsImplicitlyDeclared);
            Assert.Same(m1E1Add, m1E1.AddMethod);
            Assert.Same(m1E1Remove, m1E1.RemoveMethod);
            Assert.False(m1E1.IsExplicitInterfaceImplementation);
            Assert.True(m1E1.ExplicitInterfaceImplementations.IsEmpty);
            Assert.Equal(m1E1.TupleUnderlyingEvent.MustCallMethodsDirectly, m1E1.MustCallMethodsDirectly);
            Assert.Null(m1E1.OverriddenEvent);

            Assert.Equal(m1E1Add.TupleUnderlyingMethod.MethodKind, m1E1Add.MethodKind);
            Assert.Same(m1E1, m1E1Add.AssociatedSymbol);
            Assert.True(m1E1Add.IsImplicitlyDeclared);
            Assert.True(m1E1Add.TupleUnderlyingMethod.IsImplicitlyDeclared);

            Assert.Equal(m1E1Remove.TupleUnderlyingMethod.MethodKind, m1E1Remove.MethodKind);
            Assert.Same(m1E1, m1E1Remove.AssociatedSymbol);
            Assert.True(m1E1Remove.IsImplicitlyDeclared);
            Assert.True(m1E1Remove.TupleUnderlyingMethod.IsImplicitlyDeclared);

            var m1E1BackingField = m1E1.AssociatedField;
            Assert.Equal("System.Action<System.Int32> (System.Int32, System.Int64).E1", m1E1BackingField.ToTestDisplayString());
            Assert.Same(m1Tuple, m1E1BackingField.ContainingSymbol);
            Assert.Same(m1E1, m1E1BackingField.AssociatedSymbol);
            Assert.True(m1E1BackingField.IsImplicitlyDeclared);
            Assert.True(m1E1BackingField.TupleUnderlyingField.IsImplicitlyDeclared);
            Assert.Equal(m1E1.TupleUnderlyingEvent, m1E1BackingField.TupleUnderlyingField.AssociatedSymbol);

            var m1E2 = m1Tuple.GetMember<EventSymbol>("E2");
            var m1E2Add = m1Tuple.GetMember<MethodSymbol>("add_E2");
            var m1E2Remove = m1Tuple.GetMember<MethodSymbol>("remove_E2");

            Assert.True(m1E2.IsTupleEvent);
            Assert.Equal(SymbolKind.Event, m1E2.Kind);
            Assert.Same(m1E2, m1E2.OriginalDefinition);
            Assert.True(m1E2.Equals(m1E2));
            Assert.Equal("event System.Action<System.Int64> System.ValueTuple<System.Int32, System.Int64>.E2", m1E2.TupleUnderlyingEvent.ToTestDisplayString());
            Assert.Same(m1Tuple, m1E2.ContainingSymbol);
            Assert.Same(m1Tuple.TupleUnderlyingType, m1E2.TupleUnderlyingEvent.ContainingSymbol);
            Assert.True(m1E2.GetAttributes().IsEmpty);
            Assert.Null(m1E2.GetUseSiteDiagnostic());
            Assert.True(m1E2.TupleUnderlyingEvent.Locations.SequenceEqual(m1E2.Locations));
            Assert.True(m1E2.TupleUnderlyingEvent.DeclaringSyntaxReferences.SequenceEqual(m1E2.DeclaringSyntaxReferences, SyntaxReferenceEqualityComparer.Instance));
            Assert.False(m1E2.IsImplicitlyDeclared);
            Assert.Same(m1E2Add, m1E2.AddMethod);
            Assert.Same(m1E2Remove, m1E2.RemoveMethod);
            Assert.False(m1E2.IsExplicitInterfaceImplementation);
            Assert.True(m1E2.ExplicitInterfaceImplementations.IsEmpty);
            Assert.Equal(m1E2.TupleUnderlyingEvent.MustCallMethodsDirectly, m1E2.MustCallMethodsDirectly);
            Assert.Null(m1E2.OverriddenEvent);
            Assert.Null(m1E2.AssociatedField);

            Assert.Equal(m1E2Add.TupleUnderlyingMethod.MethodKind, m1E2Add.MethodKind);
            Assert.Same(m1E2, m1E2Add.AssociatedSymbol);
            Assert.False(m1E2Add.IsImplicitlyDeclared);
            Assert.False(m1E2Add.TupleUnderlyingMethod.IsImplicitlyDeclared);

            Assert.Equal(m1E2Remove.TupleUnderlyingMethod.MethodKind, m1E2Remove.MethodKind);
            Assert.Same(m1E2, m1E2Remove.AssociatedSymbol);
            Assert.False(m1E2Remove.IsImplicitlyDeclared);
            Assert.False(m1E2Remove.TupleUnderlyingMethod.IsImplicitlyDeclared);
        }

        [Fact]
        public void EventsAsMembers_02()
        {
            var source = @"
class C
{
    static void Main()
    {
        var v1 = M1();
        v1.E1();
    }

    static (int, long) M1()
    {
        return (1, 11);
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.E1 = null;
            this.E1();
        }

        public event System.Action E1;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (7,12): error CS0070: The event '(int, long).E1' can only appear on the left hand side of += or -= (except when used from within the type '(int, long)')
                //         v1.E1();
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E1").WithArguments("(int, long).E1", "(int, long)").WithLocation(7, 12)
                );
        }

        [Fact]
        public void TupleTypeWithPatternIs()
        {
            var source = @"
class C
{
    void Match(object o)
    {
        if (o is (int, int) t) { }
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,19): error CS1525: Invalid expression term 'int'
                //         if (o is (int, int) t) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 19),
                // (6,24): error CS1525: Invalid expression term 'int'
                //         if (o is (int, int) t) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 24),
                // (6,29): error CS1026: ) expected
                //         if (o is (int, int) t) { }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "t").WithLocation(6, 29),
                // (6,30): error CS1002: ; expected
                //         if (o is (int, int) t) { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 30),
                // (6,30): error CS1513: } expected
                //         if (o is (int, int) t) { }
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 30),
                // (6,18): error CS0150: A constant value is expected
                //         if (o is (int, int) t) { }
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int, int)").WithLocation(6, 18),
                // (6,29): error CS0103: The name 't' does not exist in the current context
                //         if (o is (int, int) t) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "t").WithArguments("t").WithLocation(6, 29)
                );
        }

        [Fact]
        public void TupleAsStatement()
        {
            var source = @"
class C
{
    void M(int x)
    {
        (x, x);
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         (x, x);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(x, x)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void TupleTypeWithPatternIs2()
        {
            var source = @"
class C
{
    void Match(object o)
    {
        if (o is (int a, int b)) { }
    }
}
" + trivial2uple;


            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,32): error CS1003: Syntax error, '=>' expected
                //         if (o is (int a, int b)) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("=>", ")").WithLocation(6, 32),
                // (6,32): error CS1525: Invalid expression term ')'
                //         if (o is (int a, int b)) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 32),
                // (6,18): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         if (o is (int a, int b)) { }
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "(int a, int b)").WithArguments("lambda expression", "object").WithLocation(6, 18)
                );
        }

        [Fact]
        public void TupleDeclarationWithPatternIs()
        {
            var source = @"
class C
{
    void Match(object o)
    {
        if (o is (1, 2)) { }
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,18): error CS0150: A constant value is expected
                //         if (o is (1, 2))
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(1, 2)").WithLocation(6, 18)
                );
        }

        [Fact]
        public void TupleTypeWithPatternSwitch()
        {
            var source = @"
class C
{
    void Match(object o)
    {
        switch(o) {
            case (int, int) tuple: return;
        }
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.RegularWithPatterns);
            comp.VerifyDiagnostics(
                // (7,19): error CS1525: Invalid expression term 'int'
                //             case (int, int) tuple: return;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 19),
                // (7,24): error CS1525: Invalid expression term 'int'
                //             case (int, int) tuple: return;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 24),
                // (7,29): error CS1003: Syntax error, ':' expected
                //             case (int, int) tuple: return;
                Diagnostic(ErrorCode.ERR_SyntaxError, "tuple").WithArguments(":", "").WithLocation(7, 29),
                // (7,18): error CS0150: A constant value is expected
                //             case (int, int) tuple: return;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int, int)").WithLocation(7, 18),
                // (7,29): warning CS0164: This label has not been referenced
                //             case (int, int) tuple: return;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "tuple").WithLocation(7, 29)
               );
        }

        [Fact]
        public void TupleLiteralWithPatternSwitch()
        {
            var source = @"
class C
{
    void Match(object o)
    {
        switch(o) {
            case (1, 1): return;
        }
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.RegularWithPatterns);
            comp.VerifyDiagnostics(
                // (7,18): error CS0150: A constant value is expected
                //             case (1, 1): return;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(1, 1)").WithLocation(7, 18)
               );
        }

        [Fact]
        public void TupleLiteralWithPatternSwitch2()
        {
            var source = @"
class C
{
    void Match(object o)
    {
        switch(o) {
            case (1, 1) t: return;
        }
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.RegularWithPatterns);
            comp.VerifyDiagnostics(
                // (7,25): error CS1003: Syntax error, ':' expected
                //             case (1, 1) t: return;
                Diagnostic(ErrorCode.ERR_SyntaxError, "t").WithArguments(":", "").WithLocation(7, 25),
                // (7,18): error CS0150: A constant value is expected
                //             case (1, 1) t: return;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(1, 1)").WithLocation(7, 18),
                // (7,25): warning CS0164: This label has not been referenced
                //             case (1, 1) t: return;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "t").WithLocation(7, 25)
               );
        }

        [Fact]
        public void TupleAsStatement2()
        {
            var source = @"
class C
{
    void M()
    {
        (1, 1);
    }
}
" + trivial2uple;
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         (1, 1);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(1, 1)").WithLocation(6, 9)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/11282 + https://github.com/dotnet/roslyn/issues/11291")]
        [WorkItem(11282, "https://github.com/dotnet/roslyn/issues/11282")]
        [WorkItem(11291, "https://github.com/dotnet/roslyn/issues/11291")]
        public void TargetTyping_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x0 = new {tuple = (null, null)};
        System.Console.WriteLine(x0.ToString());
        System.ValueType x1 = (null, ""1"");
    }

    [TestAttribute((null, ""2""))]
    [TestAttribute((3, ""3""))]
    [TestAttribute(Val = (null, ""4""))]
    [TestAttribute(Val = (5, ""5""))]
    static void AttributeTarget1(){}

    async System.Threading.Tasks.Task AsyncTest()
    {
        await (null, ""6"");
        await (7, ""7"");
    }

    static void Default((int, string) val1 = (8, null),
                        (int, string) val2 = (9, ""9""),
                        (int, string) val3 = (""10"", ""10""))
    {}

    [TestAttribute((""11"", ""11""))]
    [TestAttribute(Val = (""12"", ""12""))]
    static void AttributeTarget2(){}

    enum TestEnum
    {
        V1 = (13, null),
        V2 = (14, ""14""),
        V3 = (""15"", ""15""),
    }

    static void Test2()
    {
        var x = (16, (16 , null));
        System.Console.WriteLine(x);

        var u = __refvalue((17, null), (int, string));
        var v = __refvalue((18, ""18""), (int, string));
        var w = __refvalue((""19"", ""19""), (int, string));
    }

    
}

class TestAttribute : System.Attribute
{
    public TestAttribute()
    {}
    public TestAttribute((int, string) val)
    {}

    public (int, string) Val {get; set;}
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, references: new[] { SystemRef });
            comp.VerifyDiagnostics(
                // (6,32): error CS8206: Type of the tuple element cannot be infered from '<null>'.
                //         var x0 = new {tuple = (null, null)};
                Diagnostic(ErrorCode.Unknown, "null").WithArguments("<null>").WithLocation(6, 32),
                // (6,38): error CS8206: Type of the tuple element cannot be infered from '<null>'.
                //         var x0 = new {tuple = (null, null)};
                Diagnostic(ErrorCode.Unknown, "null").WithArguments("<null>").WithLocation(6, 38),
                // (8,31): error CS8207: Cannot implicitly convert tuple expression to 'ValueType'
                //         System.ValueType x1 = (null, "1");
                Diagnostic(ErrorCode.Unknown, @"(null, ""1"")").WithArguments("System.ValueType").WithLocation(8, 31),
                // (11,20): error CS1503: Argument 1: cannot convert from '<tuple>' to '(int, string)'
                //     [TestAttribute((null, "2"))]
                Diagnostic(ErrorCode.ERR_BadArgType, @"(null, ""2"")").WithArguments("1", "<tuple>", "(int, string)").WithLocation(11, 20),
                // (12,6): error CS0181: Attribute constructor parameter 'val' has type '(int, string)', which is not a valid attribute parameter type
                //     [TestAttribute((3, "3"))]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "TestAttribute").WithArguments("val", "(int, string)").WithLocation(12, 6),
                // (13,20): error CS0655: 'Val' is not a valid named attribute argument because it is not a valid attribute parameter type
                //     [TestAttribute(Val = (null, "4"))]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "Val").WithArguments("Val").WithLocation(13, 20),
                // (14,20): error CS0655: 'Val' is not a valid named attribute argument because it is not a valid attribute parameter type
                //     [TestAttribute(Val = (5, "5"))]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "Val").WithArguments("Val").WithLocation(14, 20),
                // (19,16): error CS8206: Type of the tuple element cannot be infered from '<null>'.
                //         await (null, "6");
                Diagnostic(ErrorCode.Unknown, "null").WithArguments("<null>").WithLocation(19, 16),
                // (20,9): error CS1061: '(int, string)' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type '(int, string)' could be found (are you missing a using directive or an assembly reference?)
                //         await (7, "7");
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"await (7, ""7"")").WithArguments("(int, string)", "GetAwaiter").WithLocation(20, 9),
                // (23,46): error CS1736: Default parameter value for 'val1' must be a compile-time constant
                //     static void Default((int, string) val1 = (8, null),
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "(8, null)").WithArguments("val1").WithLocation(23, 46),
                // (24,46): error CS1736: Default parameter value for 'val2' must be a compile-time constant
                //                         (int, string) val2 = (9, "9"))
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"(9, ""9"")").WithArguments("val2").WithLocation(24, 46),
                // (25,46): error CS1736: Default parameter value for 'val3' must be a compile-time constant
                //                         (int, string) val3 = ("10", "10"))
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, @"(""10"", ""10"")").WithArguments("val3").WithLocation(25, 46),
                // (28,20): error CS1503: Argument 1: cannot convert from '<tuple>' to '(int, string)'
                //     [TestAttribute(("11", "11"))]
                Diagnostic(ErrorCode.ERR_BadArgType, @"(""11"", ""11"")").WithArguments("1", "<tuple>", "(int, string)").WithLocation(28, 20),
                // (29,20): error CS0655: 'Val' is not a valid named attribute argument because it is not a valid attribute parameter type
                //     [TestAttribute(Val = ("12", "12"))]
                Diagnostic(ErrorCode.ERR_BadNamedAttributeArgumentType, "Val").WithArguments("Val").WithLocation(29, 20),
                // (34,14): error CS8207: Cannot implicitly convert tuple expression to 'int'
                //         V1 = (13, null),
                Diagnostic(ErrorCode.Unknown, "(13, null)").WithArguments("int").WithLocation(34, 14),
                // (35,14): error CS8207: Cannot implicitly convert tuple expression to 'int'
                //         V2 = (14, "14"),
                Diagnostic(ErrorCode.Unknown, @"(14, ""14"")").WithArguments("int").WithLocation(35, 14),
                // (36,14): error CS8207: Cannot implicitly convert tuple expression to 'int'
                //         V3 = ("15", "15"),
                Diagnostic(ErrorCode.Unknown, @"(""15"", ""15"")").WithArguments("int").WithLocation(36, 14),
                // (41,28): error CS8206: Type of the tuple element cannot be infered from '<null>'.
                //         var x = (16, (16 , null));
                Diagnostic(ErrorCode.Unknown, "null").WithArguments("<null>").WithLocation(41, 28),
                // (44,17): error CS8207: Cannot implicitly convert tuple expression to 'TypedReference'
                //         var u = __refvalue((17, null), (int, string));
                Diagnostic(ErrorCode.Unknown, "__refvalue((17, null), (int, string))").WithArguments("System.TypedReference").WithLocation(44, 17),
                // (45,17): error CS8207: Cannot implicitly convert tuple expression to 'TypedReference'
                //         var v = __refvalue((18, "18"), (int, string));
                Diagnostic(ErrorCode.Unknown, @"__refvalue((18, ""18""), (int, string))").WithArguments("System.TypedReference").WithLocation(45, 17),
                // (46,17): error CS8207: Cannot implicitly convert tuple expression to 'TypedReference'
                //         var w = __refvalue(("19", "19"), (int, string));
                Diagnostic(ErrorCode.Unknown, @"__refvalue((""19"", ""19""), (int, string))").WithArguments("System.TypedReference").WithLocation(46, 17)
                );
        }

        [Fact]
        public void TargetTyping_02()
        {
            var source = @"
class C
{
    static void Print<T>(T val)
    {
        System.Console.WriteLine($""{typeof(T)} - {val.ToString()}"");
    }

    static void Main()
    {
        var x0 = new {tuple = (1, ""s"")};
        Print(x0.tuple);
        System.ValueType x1 = (2, ""s"");
        Print(x1);
        (int, (int, string)) x2 = (3, (3 , null));
        Print(x2);
    }
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"System.ValueTuple`2[System.Int32,System.String] - {1, s}
System.ValueType - {2, s}
System.ValueTuple`2[System.Int32,System.ValueTuple`2[System.Int32,System.String]] - {3, {3, }}
");
        }

        [Fact]
        public void TargetTyping_03()
        {
            var source = @"
class Class { void Method() { tuple = (1, ""hello""); } }
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, references: new[] { SystemRef });
            comp.VerifyDiagnostics(
                // (2,31): error CS0103: The name 'tuple' does not exist in the current context
                // class Class { void Method() { tuple = (1, "hello"); } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "tuple").WithArguments("tuple").WithLocation(2, 31)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var node = nodes.OfType<TupleExpressionSyntax>().Single();

            Assert.Equal(@"(1, ""hello"")", node.ToString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("(System.Int32, System.String)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.String)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, model.GetConversion(node).Kind);
        }

        [Fact]
        public void MissingUnderlyingType()
        {
            string source = @"
class C
{
    void M()
    {
        (int, int) x = (1, 1);
    }
}
";

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var x = nodes.OfType<VariableDeclaratorSyntax>().First();
            var xSymbol = (TupleTypeSymbol)((SourceLocalSymbol)model.GetDeclaredSymbol(x)).Type;
            Assert.Equal("(System.Int32, System.Int32)", xSymbol.ToTestDisplayString());

            Assert.True(xSymbol.TupleUnderlyingType.IsErrorType());
            Assert.False(xSymbol.IsReferenceType); // if no underlying type, then defaults to struct
        }

        [Fact, CompilerTrait(CompilerFeature.LocalFunctions)]
        public void LocalFunction()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, int) Local((int, int) x) { return x; }
        System.Console.WriteLine(Local((1, 2)));
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: "(1, 2)");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LocalFunctionWithNamedTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int a, int b) Local((int c, int d) x) { return x; }
        System.Console.WriteLine(Local((d: 1, c: 2)).b);
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                expectedOutput: "2");
            comp.VerifyDiagnostics();
        }

        [Fact, CompilerTrait(CompilerFeature.LocalFunctions)]
        public void LocalFunctionWithLongTuple()
        {
            var source = @"
class C
{
    static void Main()
    {
        (int, string, int, string, int, string, int, string) Local((int, string, int, string, int, string, int, string) x) { return x; }
        System.Console.WriteLine(Local((1, ""Alice"", 2, ""Brenda"", 3, ""Chloe"", 4, ""Dylan"")));
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "(1, Alice, 2, Brenda, 3, Chloe, 4, Dylan)", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TupleMembersInLambda()
        {
            var source = @"
using System;
class C
{
    static Action action;

    static void Main()
    {
        var tuple = (a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8);
        action += () => { Console.Write(tuple.Item1 + "" "" + tuple.a + "" "" + tuple.Rest + "" "" + tuple.Item8 + "" "" + tuple.h); };
        action();
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "1 1 (8) 8 8", additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LongTupleConstructor()
        {
            var source = @"
class C
{
    void M()
    {
        var x = new (int, int, int, int, int, int, int, int)(1, 2, 3, 4, 5, 6, 7, 8);
        var y = new (int, int, int, int, int, int, int, int, int)(1, 2, 3, 4, 5, 6, 7, 8, 9);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,83): error CS1503: Argument 8: cannot convert from 'int' to '(int)'
                //         var x = new (int, int, int, int, int, int, int, int)(1, 2, 3, 4, 5, 6, 7, 8);
                Diagnostic(ErrorCode.ERR_BadArgType, "8").WithArguments("8", "int", "(int)").WithLocation(6, 83),
                // (7,21): error CS1729: '(int, int, int, int, int, int, int, int, int)' does not contain a constructor that takes 9 arguments
                //         var y = new (int, int, int, int, int, int, int, int, int)(1, 2, 3, 4, 5, 6, 7, 8, 9);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(int, int, int, int, int, int, int, int, int)").WithArguments("(int, int, int, int, int, int, int, int, int)", "9").WithLocation(7, 21)
                );
        }

        [Fact]
        [WorkItem(11322, "https://github.com/dotnet/roslyn/issues/11322")]
        public void LiteralsAndAmbiguousVT_01()
        {
            var source = @"
class C3
{
    public static void Main()
    {
        var x = (1, 1);
        System.Console.WriteLine(x.GetType().Assembly);
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(trivial2uple, assemblyName: "comp1");
            var comp2 = CreateCompilationWithMscorlib(trivial2uple, assemblyName: "comp2");

            var comp = CreateCompilationWithMscorlib(source, 
                references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference() }, 
                parseOptions: TestOptions.Regular, 
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (6,17): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         var x = (1, 1);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(1, 1)").WithArguments("System.ValueTuple`2").WithLocation(6, 17)
                );

            comp = CreateCompilationWithMscorlib(source,
                references: new[] { comp2.ToMetadataReference(), comp1.ToMetadataReference() },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (6,17): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         var x = (1, 1);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(1, 1)").WithArguments("System.ValueTuple`2").WithLocation(6, 17)
                );

            comp = CreateCompilationWithMscorlib(source,
                references: new[] { comp2.ToMetadataReference(ImmutableArray.Create("alias")), comp1.ToMetadataReference() },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        }

        [Fact]
        [WorkItem(11322, "https://github.com/dotnet/roslyn/issues/11322")]
        public void LiteralsAndAmbiguousVT_02()
        {
            var source1 = trivial2uple;

            var source2 = @"
public static class C2
{
    public static void M1(this string x, (int, string) y)
    {
        System.Console.WriteLine(""C2.M1"");
    }
}
" + trivial2uple + tupleattributes_cs;

            var comp1 = CreateCompilationWithMscorlibAndSystemCore(source1, assemblyName: "comp1");
            comp1.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlibAndSystemCore(source2, assemblyName: "comp2");
            comp2.VerifyDiagnostics();

            var source3 = @"
class C3
{
    public static void Main()
    {
        ""x"".M1((1, null));
    }
}
";

            var comp = CreateCompilationWithMscorlib(source3,
                references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference() },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (6,16): error CS0518: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         "x".M1((1, null));
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(1, null)").WithArguments("System.ValueTuple`2").WithLocation(6, 16)
                );

            comp = CreateCompilationWithMscorlib(source3,
                            references: new[] { comp1.ToMetadataReference().WithAliases(ImmutableArray.Create("alias")), comp2.ToMetadataReference() },
                            parseOptions: TestOptions.Regular,
                            options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C2.M1");

            var source4 = @"
extern alias alias1;
using alias1;

class C3
{
    public static void Main()
    {
        ""x"".M1((1, null));
    }
}
";

            comp = CreateCompilationWithMscorlib(source4,
                            references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference().WithAliases(ImmutableArray.Create("alias1")) },
                            parseOptions: TestOptions.Regular,
                            options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C2.M1");
        }

        [Fact]
        [WorkItem(11322, "https://github.com/dotnet/roslyn/issues/11322")]
        public void LongLiteralsAndAmbiguousVT_02()
        {
            var source1 = trivial2uple + trivial3uple + trivialRemainingTuples;

            var source2 = @"
public static class C2
{
    public static void M1(this string x, (int, string, int, string, int, string, int, string, int, string) y)
    {
        System.Console.WriteLine(""C2.M1"");
    }
}
" + trivial2uple + trivial3uple + trivialRemainingTuples + tupleattributes_cs;

            var comp1 = CreateCompilationWithMscorlibAndSystemCore(source1, assemblyName: "comp1");
            comp1.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlibAndSystemCore(source2, assemblyName: "comp2");
            comp2.VerifyDiagnostics();

            var source3 = @"
class C3
{
    public static void Main()
    {
        ""x"".M1((1, null, 1, null, 1, null, 1, null, 1, null));
    }
}
";

            var comp = CreateCompilationWithMscorlib(source3,
                references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference() },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (6,16): error CS0518: Predefined type 'System.ValueTuple`3' is not defined or imported
                //         "x".M1((1, null, 1, null, 1, null, 1, null, 1, null));
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(1, null, 1, null, 1, null, 1, null, 1, null)").WithArguments("System.ValueTuple`3").WithLocation(6, 16),
                // (6,16): error CS0518: Predefined type 'System.ValueTuple`8' is not defined or imported
                //         "x".M1((1, null, 1, null, 1, null, 1, null, 1, null));
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "(1, null, 1, null, 1, null, 1, null, 1, null)").WithArguments("System.ValueTuple`8").WithLocation(6, 16)
                );

            comp = CreateCompilationWithMscorlib(source3,
                            references: new[] { comp1.ToMetadataReference().WithAliases(ImmutableArray.Create("alias")), comp2.ToMetadataReference() },
                            parseOptions: TestOptions.Regular,
                            options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C2.M1");

            var source4 = @"
extern alias alias1;
using alias1;

class C3
{
    public static void Main()
    {
        ""x"".M1((1, null, 1, null, 1, null, 1, null, 1, null));
    }
}
";

            comp = CreateCompilationWithMscorlib(source4,
                            references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference().WithAliases(ImmutableArray.Create("alias1")) },
                            parseOptions: TestOptions.Regular,
                            options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C2.M1");
        }

        [Fact]
        [WorkItem(11323, "https://github.com/dotnet/roslyn/issues/11323")]
        public void ExactlyMatchingExpression()
        {
            var source1 = @"
namespace NS1
{
    public static class C1
    {
        public static void M1(this string x, (int, int) y)
        {
            System.Console.WriteLine(""C1.M1"");
        }
    }
}
";

            var source2 = @"
namespace NS2
{
    public static class C2
    {
        public static void M1(this string x, (int, int) y)
        {
            System.Console.WriteLine(""C2.M1"");
        }
    }
}
";

            var comp1 = CreateCompilationWithMscorlibAndSystemCore(source1,
                references: s_valueTupleRefs,
                assemblyName: "comp1");
            comp1.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlibAndSystemCore(source2,
                references: s_valueTupleRefs,
                assemblyName: "comp2");
            comp2.VerifyDiagnostics();

            var source3 = @"
extern alias alias1;
using alias1::NS2;
using NS1;

class C3
{
    public static void Main()
    {
        ""x"".M1((1, 1));
    }
}
";

            var comp = CreateCompilationWithMscorlib(source3,
                references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference().WithAliases(ImmutableArray.Create("alias1")),
                    ValueTupleRef },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (10,13): error CS0121: The call is ambiguous between the following methods or properties: 'NS2.C2.M1(string, (int, int))' and 'NS1.C1.M1(string, (int, int))'
                //         "x".M1((1, 1));
                Diagnostic(ErrorCode.ERR_AmbigCall, "M1").WithArguments("NS2.C2.M1(string, (int, int))", "NS1.C1.M1(string, (int, int))").WithLocation(10, 13)
                                );

            var source4 = @"
using NS1;

class C3
{
    public static void Main()
    {
        ""x"".M1((1, 1));
    }
}
";

            comp = CreateCompilationWithMscorlib(source4,
                references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference().WithAliases(ImmutableArray.Create("alias1")),
                    ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "C1.M1");

            var source5 = @"
extern alias alias1;
using alias1::NS2;

class C3
{
    public static void Main()
    {
        ""x"".M1((1, 1));
    }
}
";

            comp = CreateCompilationWithMscorlib(source5,
                references: new[] {
                    comp1.ToMetadataReference(),
                    comp2.ToMetadataReference().WithAliases(ImmutableArray.Create("alias1")),
                    ValueTupleRef,
                    SystemRuntimeFacadeRef
                },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "C2.M1");
        }

        [Fact]
        public void MultipleVT_01()
        {
            var source1 = @"
public class C1
{
    public void M1((int, int) y)
    {
        System.Console.WriteLine(""C1.M1"");
    }
}
" + trivial2uple + tupleattributes_cs;

            var source2 = @"
public class C2
{
    public void M1((int, int) y)
    {
        System.Console.WriteLine(""C2.M1"");
    }
}
" + trivial2uple + tupleattributes_cs;

            var comp1 = CreateCompilationWithMscorlibAndSystemCore(source1, assemblyName: "comp1");
            comp1.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlibAndSystemCore(source2, assemblyName: "comp2");
            comp2.VerifyDiagnostics();


            var source = @"
extern alias alias1;

class C3
{
    public static void Main()
    {
        new C1().M1((1, 1));
        new alias1::C2().M1((1, 1));
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference().WithAliases(ImmutableArray.Create("alias1")) },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput:
@"C1.M1
C2.M1");
        }

        [Fact]
        [WorkItem(11325, "https://github.com/dotnet/roslyn/issues/11325")]
        public void MultipleVT_02()
        {
            var source1 = @"
public class C1
{
    public (int, int) M2()
    {
        System.Console.WriteLine(""C1.M2"");
        return (1, 1);
    }
}
" + trivial2uple + tupleattributes_cs;

            var source2 = @"
public class C2
{
    public void M3((int, int) y)
    {
        System.Console.WriteLine(""C2.M3"");
    }
}
" + trivial2uple + tupleattributes_cs;

            var comp1 = CreateCompilationWithMscorlibAndSystemCore(source1, assemblyName: "comp1");
            comp1.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlibAndSystemCore(source2, assemblyName: "comp2");
            comp2.VerifyDiagnostics();


            var source = @"
extern alias alias1;

class C3
{
    public static void Main()
    {
        new alias1::C2().M3(new C1().M2());
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: new[] { comp1.ToMetadataReference(), comp2.ToMetadataReference().WithAliases(ImmutableArray.Create("alias1")) },
                parseOptions: TestOptions.Regular,
                options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput:
@"C1.M2
C2.M3");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/11326")]
        [WorkItem(11326, "https://github.com/dotnet/roslyn/issues/11326")]
        public void GetTypeInfo_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x1 = ((short, string))(1, ""hello"");
        var x2 = ((short, string))(2, null);
        var x3 = (short)11;
        var x4 = (string)null;
        var x5 = (System.Func<int>)(() => 12);
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);
            //comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var n1 = nodes.OfType<TupleExpressionSyntax>().ElementAt(0);

            Assert.Equal(@"(1, ""hello"")", n1.ToString());
            Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(n1).Type.ToTestDisplayString());
            Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(n1).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(n1));

            var n2 = nodes.OfType<TupleExpressionSyntax>().ElementAt(1);

            Assert.Equal(@"(2, null)", n2.ToString());
            Assert.Null(model.GetTypeInfo(n2).Type);
            Assert.Null(model.GetTypeInfo(n2).ConvertedType);
            Assert.Equal(Conversion.Identity, model.GetConversion(n2));

            var n3 = nodes.OfType<LiteralExpressionSyntax>().ElementAt(4);

            Assert.Equal(@"11", n3.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(n3).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(n3).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.Identity, model.GetConversion(n3));

            var n4 = nodes.OfType<LiteralExpressionSyntax>().ElementAt(5);

            Assert.Equal(@"null", n4.ToString());
            Assert.Null(model.GetTypeInfo(n4).Type);
            Assert.Null(model.GetTypeInfo(n4).ConvertedType);
            Assert.Equal(Conversion.Identity, model.GetConversion(n4));

            var n5 = nodes.OfType<LambdaExpressionSyntax>().Single();

            Assert.Equal(@"() => 12", n5.ToString());
            Assert.Null(model.GetTypeInfo(n5).Type);
            Assert.Equal("System.Func<System.Int32>", model.GetTypeInfo(n5).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.AnonymousFunction, model.GetConversion(n5).Kind);
        }

        [Fact]
        [WorkItem(11326, "https://github.com/dotnet/roslyn/issues/11326")]
        public void GetTypeInfo_02()
        {
            var source = @"
class C
{
    static void Main()
    {
        (short, string) x1 = (1, ""hello"");
        (short, string) x2 = (2, null);
        short x3 = 11;
        string x4 = null;
        System.Func<int> x5 = () => 12;
    }
}
" + trivial2uple + trivial3uple;

            var tree = Parse(source, options: TestOptions.Regular);
            var comp = CreateCompilationWithMscorlib(tree);
            //comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var n1 = nodes.OfType<TupleExpressionSyntax>().ElementAt(0);

            Assert.Equal(@"(1, ""hello"")", n1.ToString());
            Assert.Equal("(System.Int32, System.String)", model.GetTypeInfo(n1).Type.ToTestDisplayString());
            Assert.Equal("(System.Int16, System.String)", model.GetTypeInfo(n1).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(n1).Kind);

            var n2 = nodes.OfType<TupleExpressionSyntax>().ElementAt(1);

            Assert.Equal(@"(2, null)", n2.ToString());
            Assert.Null(model.GetTypeInfo(n2).Type);
            Assert.Equal("(System.Int16, System.String)", model.GetTypeInfo(n2).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitTupleLiteral, model.GetConversion(n2).Kind);

            var n3 = nodes.OfType<LiteralExpressionSyntax>().ElementAt(4);

            Assert.Equal(@"11", n3.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(n3).Type.ToTestDisplayString());
            Assert.Equal("System.Int16", model.GetTypeInfo(n3).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.ImplicitConstant, model.GetConversion(n3).Kind);

            var n4 = nodes.OfType<LiteralExpressionSyntax>().ElementAt(5);

            Assert.Equal(@"null", n4.ToString());
            Assert.Null(model.GetTypeInfo(n4).Type);
            Assert.Equal("System.String", model.GetTypeInfo(n4).ConvertedType.ToTestDisplayString());
            Assert.Equal(Conversion.ImplicitReference, model.GetConversion(n4));

            var n5 = nodes.OfType<LambdaExpressionSyntax>().Single();

            Assert.Equal(@"() => 12", n5.ToString());
            Assert.Null(model.GetTypeInfo(n5).Type);
            Assert.Equal("System.Func<System.Int32>", model.GetTypeInfo(n5).ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.AnonymousFunction, model.GetConversion(n5).Kind);
        }

        [Fact]
        public void CompileTupleLib()
        {
            string additionalSource = @"

namespace System
{
    public class SR
    {
        public static string ArgumentException_ValueTupleIncorrectType { get { return """"; } }
        public static string ArgumentException_ValueTupleLastArgumentNotAValueTuple { get { return """"; } }
    }
}
namespace System.Diagnostics
{
    public static class Debug
    {
        public static void Assert(bool condition) { }
        public static void Assert(bool condition, string message) { }
    }
}
";
            var tupleComp = CreateCompilationWithMscorlib(
                tuplelib_cs + tupleattributes_cs + additionalSource);
            tupleComp.VerifyDiagnostics();

            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 2);
        System.Console.WriteLine(x.ToString());
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "(1, 2)", additionalRefs: new[] { tupleComp.ToMetadataReference() });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ImplicitConversions01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        // long tuple
        var x1 = (1,2,3,4,5,6,7,8,9,10,11,12);
        (long, long, long, long, long, long, long, long,long, long, long, long) y1 = x1;
        System.Console.WriteLine(y1);

        // long nested tuple
        var x2 = (1,2,3,4,5,6,7,8,9,10,11,(1,2,3,4,5,6,7,8,9,10,11,12));
        (long, long, long, long, long, long, long, long,long, long, long, (long, long, long, long, long, long, long, long,long, long, long, long)) y2 = x2;
        System.Console.WriteLine(y2);

        // user defined conversion
        var x3 = (1,1);
        C1 y3 = x3;        
        x3 = y3;
        System.Console.WriteLine(x3);
    }

    class C1
    {
        static public implicit operator C1((long, long) arg)
        {
            return new C1();
        }

        static public implicit operator (byte, byte)(C1 arg)
        {
            return (2, 2);
        }
    }
}
" ;

            var comp = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, expectedOutput: @"
(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)
(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12))
(2, 2)
");
        }

        [Fact]
        public void ExplicitConversions01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        // long tuple
        var x1 = (1,2,3,4,5,6,7,8,9,10,11,12);
        (byte, byte, byte, byte, byte, byte, byte, byte,byte, byte, byte, byte) y1 = ((byte, byte, byte, byte, byte, byte, byte, byte,byte, byte, byte, byte))x1;
        System.Console.WriteLine(y1);

        // long nested tuple
        var x2 = (1,2,3,4,5,6,7,8,9,10,11,(1,2,3,4,5,6,7,8,9,10,11,12));
        (byte, byte, byte, byte, byte, byte, byte, byte,byte, byte, byte, (byte, byte, byte, byte, byte, byte, byte, byte,byte, byte, byte, byte)) y2 = 
                            ((byte, byte, byte, byte, byte, byte, byte, byte,byte, byte, byte, (byte, byte, byte, byte, byte, byte, byte, byte,byte, byte, byte, byte)))x2;
        System.Console.WriteLine(y2);

        // user defined conversion
        var x3 = (1,1);
        C1 y3 = (C1)x3;        
        x3 = ((int, int))y3;
        System.Console.WriteLine(x3);
    }

    class C1
    {
        static public explicit operator C1((long, long) arg)
        {
            return new C1();
        }

        static public explicit operator (byte, byte)(C1 arg)
        {
            return (2, 2);
        }
    }
}
";

            var comp = CompileAndVerify(source, additionalRefs: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, expectedOutput: @"
(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)
(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12))
(2, 2)
");
        }

        [Fact]
        public void ImplicitConversions02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (a:1, b:1);
        C1 y = x;        
        x = y;
        System.Console.WriteLine(x);

        x = ((int, int))(C1)x;
        System.Console.WriteLine(x);
    }

    class C1
    {
        static public implicit operator C1((long, long) arg)
        {
            return new C1();
        }

        static public implicit operator (byte c, byte d)(C1 arg)
        {
            return (2, 2);
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(2, 2)
(2, 2)");
            
            comp.VerifyIL("C.Main", @"
{
  // Code size      125 (0x7d)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0,
                System.ValueTuple<byte, byte> V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000e:  conv.i8
  IL_000f:  ldloc.0
  IL_0010:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0015:  conv.i8
  IL_0016:  newobj     ""System.ValueTuple<long, long>..ctor(long, long)""
  IL_001b:  call       ""C.C1 C.C1.op_Implicit((long, long))""
  IL_0020:  call       ""(byte c, byte d) C.C1.op_Implicit(C.C1)""
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldfld      ""byte System.ValueTuple<byte, byte>.Item1""
  IL_002c:  ldloc.1
  IL_002d:  ldfld      ""byte System.ValueTuple<byte, byte>.Item2""
  IL_0032:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0037:  dup
  IL_0038:  box        ""System.ValueTuple<int, int>""
  IL_003d:  call       ""void System.Console.WriteLine(object)""
  IL_0042:  stloc.0
  IL_0043:  ldloc.0
  IL_0044:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0049:  conv.i8
  IL_004a:  ldloc.0
  IL_004b:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0050:  conv.i8
  IL_0051:  newobj     ""System.ValueTuple<long, long>..ctor(long, long)""
  IL_0056:  call       ""C.C1 C.C1.op_Implicit((long, long))""
  IL_005b:  call       ""(byte c, byte d) C.C1.op_Implicit(C.C1)""
  IL_0060:  stloc.1
  IL_0061:  ldloc.1
  IL_0062:  ldfld      ""byte System.ValueTuple<byte, byte>.Item1""
  IL_0067:  ldloc.1
  IL_0068:  ldfld      ""byte System.ValueTuple<byte, byte>.Item2""
  IL_006d:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0072:  box        ""System.ValueTuple<int, int>""
  IL_0077:  call       ""void System.Console.WriteLine(object)""
  IL_007c:  ret
}
"); ;
        }

        [Fact]
        public void ExplicitConversions02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (a:1, b:1);
        C1 y = (C1)x;        
        x = ((int a, int b))y;
        System.Console.WriteLine(x);

        x = ((int, int))(C1)x;
        System.Console.WriteLine(x);
    }

    class C1
    {
        static public explicit operator C1((long, long) arg)
        {
            return new C1();
        }

        static public explicit operator (byte c, byte d)(C1 arg)
        {
            return (2, 2);
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(2, 2)
(2, 2)");

            comp.VerifyIL("C.Main", @"
{
  // Code size      125 (0x7d)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0,
                System.ValueTuple<byte, byte> V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000e:  conv.i8
  IL_000f:  ldloc.0
  IL_0010:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0015:  conv.i8
  IL_0016:  newobj     ""System.ValueTuple<long, long>..ctor(long, long)""
  IL_001b:  call       ""C.C1 C.C1.op_Explicit((long, long))""
  IL_0020:  call       ""(byte c, byte d) C.C1.op_Explicit(C.C1)""
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldfld      ""byte System.ValueTuple<byte, byte>.Item1""
  IL_002c:  ldloc.1
  IL_002d:  ldfld      ""byte System.ValueTuple<byte, byte>.Item2""
  IL_0032:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0037:  dup
  IL_0038:  box        ""System.ValueTuple<int, int>""
  IL_003d:  call       ""void System.Console.WriteLine(object)""
  IL_0042:  stloc.0
  IL_0043:  ldloc.0
  IL_0044:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0049:  conv.i8
  IL_004a:  ldloc.0
  IL_004b:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0050:  conv.i8
  IL_0051:  newobj     ""System.ValueTuple<long, long>..ctor(long, long)""
  IL_0056:  call       ""C.C1 C.C1.op_Explicit((long, long))""
  IL_005b:  call       ""(byte c, byte d) C.C1.op_Explicit(C.C1)""
  IL_0060:  stloc.1
  IL_0061:  ldloc.1
  IL_0062:  ldfld      ""byte System.ValueTuple<byte, byte>.Item1""
  IL_0067:  ldloc.1
  IL_0068:  ldfld      ""byte System.ValueTuple<byte, byte>.Item2""
  IL_006d:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0072:  box        ""System.ValueTuple<int, int>""
  IL_0077:  call       ""void System.Console.WriteLine(object)""
  IL_007c:  ret
}
"); 
        }

        [Fact]
        public void ImplicitConversions03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (a:1, b:1);
        C1 y = x;        
        (int, int)? x1 = y;
        System.Console.WriteLine(x1);

        x1 = ((int, int)?)(C1)x;
        System.Console.WriteLine(x1);
    }

    class C1
    {
        static public implicit operator C1((long, long)? arg)
        {
            return new C1();
        }

        static public implicit operator (byte c, byte d)(C1 arg)
        {
            return (2, 2);
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(2, 2)
(2, 2)
") ;
        }

        [Fact]
        public void ExplicitConversions03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (a:1, b:1);
        C1 y = (C1)x;        
        (int, int)? x1 = ((int, int)?)y;
        System.Console.WriteLine(x1);

        x1 = ((int, int)?)(C1)x;
        System.Console.WriteLine(x1);
    }

    class C1
    {
        static public explicit operator C1((long, long)? arg)
        {
            return new C1();
        }

        static public explicit operator (byte c, byte d)(C1 arg)
        {
            return (2, 2);
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(2, 2)
(2, 2)
");
        }

        [Fact]
        public void ImplicitConversions04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (1, (1, (1, (1, (1, (1, 1))))));
        C1 y = x;   

        (int, int) x2 = y;
        System.Console.WriteLine(x2);

        (int, (int, int)) x3 = y;
        System.Console.WriteLine(x3);
 
        (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, int))))))))))) x12 = y;
        System.Console.WriteLine(x12);
    }

    class C1
    {
        private byte x;

        static public implicit operator C1((long, C1) arg)
        {
            var result = new C1();
            result.x = arg.Item2.x;
            return result;
        }

        static public implicit operator C1((long, long) arg)
        {
            var result = new C1();
            result.x = (byte)(arg.Item2);
            return result;
        }

        static public implicit operator (byte, C1)(C1 arg)
        {
            return ((byte)(arg.x++), arg);
        }

        static public implicit operator (byte c, byte d)(C1 arg)
        {
            return ((byte)(arg.x++), (byte)(arg.x++));
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(1, 2)
(3, (4, 5))
(6, (7, (8, (9, (10, (11, (12, (13, (14, (15, (16, 17)))))))))))");
        }

        [Fact]
        public void ExplicitConversions04Err()
        {

            // explicit conversion case similar to ImplicitConversions04
            // is a compiler error since explicit tuple conversions do not stack up like
            // implicit tuple conversions which are in standard implicit set.

            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (1, (1, (1, (1, (1, (1, 1))))));
        C1 y = (C1)x;   

        (int, int) x2 = ((int, int))y;
        System.Console.WriteLine(x2);

        (int, (int, int)) x3 = ((int, (int, int)))y;
        System.Console.WriteLine(x3);
 
        (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, int))))))))))) x12 = ((int, (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, int))))))))))))y;
        System.Console.WriteLine(x12);
    }

    class C1
    {
        private byte x;

        static public explicit operator C1((long, C1) arg)
        {
            var result = new C1();
            result.x = arg.Item2.x;
            return result;
        }

        static public explicit operator C1((long, long) arg)
        {
            var result = new C1();
            result.x = (byte)(arg.Item2);
            return result;
        }

        static public explicit operator (byte, C1)(C1 arg)
        {
            return ((byte)(arg.x++), arg);
        }

        static public explicit operator (byte c, byte d)(C1 arg)
        {
            return ((byte)(arg.x++), (byte)(arg.x++));
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, assemblyName: "ImplicitConversions06Err");
            comp.VerifyEmitDiagnostics(
                // (8,16): error CS0030: Cannot convert type '(int, (int, (int, (int, (int, (int, int))))))' to 'C.C1'
                //         C1 y = (C1)x;   
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C1)x").WithArguments("(int, (int, (int, (int, (int, (int, int))))))", "C.C1").WithLocation(8, 16),
                // (13,32): error CS0030: Cannot convert type 'C.C1' to '(int, (int, int))'
                //         (int, (int, int)) x3 = ((int, (int, int)))y;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "((int, (int, int)))y").WithArguments("C.C1", "(int, (int, int))").WithLocation(13, 32),
                // (16,96): error CS0030: Cannot convert type 'C.C1' to '(int, (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, int)))))))))))'
                //         (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, int))))))))))) x12 = ((int, (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, int))))))))))))y;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "((int, (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, int))))))))))))y").WithArguments("C.C1", "(int, (int, (int, (int, (int, (int, (int, (int, (int, (int, (int, int)))))))))))").WithLocation(16, 96)
            );
        }

        [Fact]
        public void ImplicitConversions05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (1, (1, (1, (1, (1, 1)))));
        C1 y = x;   
 
        (int, (object, (byte, (int?, (long, IComparable)?))?))? x1 = y;
        System.Console.WriteLine(x1);
    }

    class C1
    {
        private byte x;

        static public implicit operator C1((long, C1) arg)
        {
            var result = new C1();
            result.x = arg.Item2.x;
            return result;
        }

        static public implicit operator C1((long, long) arg)
        {
            var result = new C1();
            result.x = (byte)(arg.Item2);
            return result;
        }

        static public implicit operator (byte, C1)(C1 arg)
        {
            return ((byte)(arg.x++), arg);
        }

        static public implicit operator (byte c, byte d)(C1 arg)
        {
            return ((byte)(arg.x++), (byte)(arg.x++));
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(1, (2, (3, (4, (5, 6)))))
");
        }

        [Fact]
        public void ExplicitConversions05()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (1, (1, (1, (1, (1, 1)))));
        C1 y = (C1)((int, C1))((int, (int, C1)))((int, (int, (int, C1))))((int, (int, (int, (int, C1)))))x;   
 
        (int, (object, (byte, (int?, (long, IComparable)?))?))? x1 = ((int, (object, (byte, (int?, (long, IComparable)?))?))?)
                                                                     ((int, (object, (byte, (int?, C1))?))?)
                                                                     ((int, (object, (byte, C1)?))?)
                                                                     ((int, (object, C1))?)
                                                                     ((int, C1)?)
                                                                     (C1)y;
        System.Console.WriteLine(x1);
    }

    class C1
    {
        private byte x;

        static public explicit operator C1((long, C1) arg)
        {
            var result = new C1();
            result.x = arg.Item2.x;
            return result;
        }

        static public explicit operator C1((long, long) arg)
        {
            var result = new C1();
            result.x = (byte)(arg.Item2);
            return result;
        }

        static public explicit operator (byte, C1)(C1 arg)
        {
            return ((byte)(arg.x++), arg);
        }

        static public explicit operator (byte c, byte d)(C1 arg)
        {
            return ((byte)(arg.x++), (byte)(arg.x++));
        }
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(1, (2, (3, (4, (5, 6)))))
");
        }

        [Fact]
        public void ImplicitConversions05Err()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (1, (1, (1, (1, (1, 1)))));
        C1 y = x;   
 
        (int, (object, (byte, (int?, (long, IComparable)?))?))? x1 = y;
        System.Console.WriteLine(x1);
    }

    class C1
    {
        private byte x;

        static public implicit operator C1((long, C1) arg)
        {
            var result = new C1();
            result.x = arg.Item2.x;
            return result;
        }

        static public implicit operator C1((long, long) arg)
        {
            var result = new C1();
            result.x = (byte)(arg.Item2);
            return result;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source,
                references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (10,70): error CS0029: Cannot implicitly convert type 'C.C1' to '(int, (object, (byte, (int?, (long, System.IComparable)?))?))?'
                //         (int, (object, (byte, (int?, (long, IComparable)?))?))? x1 = y;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("C.C1", "(int, (object, (byte, (int?, (long, System.IComparable)?))?))?").WithLocation(10, 70)
                );

        }

        [Fact]
        public void ExplicitConversions05Err()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
                var x = (1, (1, (1, (1, (1, 1)))));
        C1 y = (C1)((int, C1))((int, (int, C1)))((int, (int, (int, C1))))((int, (int, (int, (int, C1)))))x;   
 
        (int, (object, (byte, (int?, (long, IComparable)?))?))? x1 = ((int, (object, (byte, (int?, (long, IComparable)?))?))?)
                                                                     ((int, (object, (byte, (int?, C1))?))?)
                                                                     ((int, (object, (byte, C1)?))?)
                                                                     ((int, (object, C1))?)
                                                                     ((int, C1)?)
                                                                     (C1)y;
        System.Console.WriteLine(x1);
    }

    class C1
    {
        private byte x;

        static public explicit operator C1((long, C1) arg)
        {
            var result = new C1();
            result.x = arg.Item2.x;
            return result;
        }

        static public explicit operator C1((long, long) arg)
        {
            var result = new C1();
            result.x = (byte)(arg.Item2);
            return result;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, references: s_valueTupleRefs);
            comp.VerifyDiagnostics(
                // (14,70): error CS0030: Cannot convert type 'C.C1' to '(int, C.C1)?'
                //                                                                      ((int, C1)?)
                Diagnostic(ErrorCode.ERR_NoExplicitConv, @"((int, C1)?)
                                                                     (C1)y").WithArguments("C.C1", "(int, C.C1)?").WithLocation(14, 70)
                );

        }

        [Fact]
        public void ImplicitConversions06Err()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 1);
        (long, long) y = x;   
 
        System.Console.WriteLine(y);
    }

}

namespace System
{
    // struct with two values (missing a field)
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, assemblyName: "ImplicitConversions06Err");
            comp.VerifyEmitDiagnostics(
                // (7,26): error CS8205: Member 'Item2' was not found on type 'ValueTuple<T1, T2>' from assembly 'ImplicitConversions06Err, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (long, long) y = x;   
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "x").WithArguments("Item2", "System.ValueTuple<T1, T2>", "ImplicitConversions06Err, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 26)
            );

        }

        [Fact]
        public void ExplicitConversions06Err()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (1, 1);
        (byte, byte) y = ((byte, byte))x;   
 
        System.Console.WriteLine(y);
    }

}

namespace System
{
    // struct with two values (missing a field)
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, assemblyName: "ImplicitConversions06Err");
            comp.VerifyEmitDiagnostics(
                // (7,26): error CS8205: Member 'Item2' was not found on type 'ValueTuple<T1, T2>' from assembly 'ImplicitConversions06Err, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (byte, byte) y = ((byte, byte))x;   
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "((byte, byte))x").WithArguments("Item2", "System.ValueTuple<T1, T2>", "ImplicitConversions06Err, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 26)
            );

        }

        [Fact]
        public void ImplicitConversions07()
        {
            var source = @"
using System;

    internal class Program
    {
        static void Main(string[] args)
        {
            var t = (1, 1);
            (C2, C2) aa = t;
            System.Console.WriteLine(aa);
        }

        // accessibility of operators. Notice Private
        private class C2
        {
            public static implicit operator C2(int arg)
            {
                return new C2();
            }
        }
    }

" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
{Program+C2, Program+C2}
");
        }

        [Fact]
        public void ExplicitConversions07()
        {
            var source = @"
using System;

    internal class Program
    {
        static void Main(string[] args)
        {
            var t = (1, 1);
            (C2, C2) aa = ((C2, C2))t;
            System.Console.WriteLine(aa);
        }

        // accessibility of operators. Notice Private
        private class C2
        {
            public static explicit operator C2(int arg)
            {
                return new C2();
            }
        }
    }

" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
{Program+C2, Program+C2}
");
        }

        [Fact]
        public void ExplicitConversionsSimple01()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (a:1, b:2);
        var y = ((byte, byte))x;        
        System.Console.WriteLine(y);

        var z = ((int, int))((long)3, (object)4);
        System.Console.WriteLine(z);
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
{1, 2}
{3, 4}");

            comp.VerifyIL("C.Main", @"
{
  // Code size       65 (0x41)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000e:  conv.u1
  IL_000f:  ldloc.0
  IL_0010:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0015:  conv.u1
  IL_0016:  newobj     ""System.ValueTuple<byte, byte>..ctor(byte, byte)""
  IL_001b:  box        ""System.ValueTuple<byte, byte>""
  IL_0020:  call       ""void System.Console.WriteLine(object)""
  IL_0025:  ldc.i4.3
  IL_0026:  ldc.i4.4
  IL_0027:  box        ""int""
  IL_002c:  unbox.any  ""int""
  IL_0031:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0036:  box        ""System.ValueTuple<int, int>""
  IL_003b:  call       ""void System.Console.WriteLine(object)""
  IL_0040:  ret
}
"); ;
        }

        [Fact]
        public void ExplicitConversionsSimple02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (a:1, b:2);
        var y = ((C2, C2))x;        
        System.Console.WriteLine(y);

        var z = ((C2, C2))((object)null, 4);
        System.Console.WriteLine(z);
    }

    private class C2
    {
        private int x;

        public static explicit operator C2(int arg)
        {
            var result = new C2();
            result.x = arg + 1;
            return result;
        }

        public override string ToString()
        {
            return x.ToString();
        }
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
{2, 3}
{, 5}");

            comp.VerifyIL("C.Main", @"
{
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000e:  call       ""C.C2 C.C2.op_Explicit(int)""
  IL_0013:  ldloc.0
  IL_0014:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0019:  call       ""C.C2 C.C2.op_Explicit(int)""
  IL_001e:  newobj     ""System.ValueTuple<C.C2, C.C2>..ctor(C.C2, C.C2)""
  IL_0023:  box        ""System.ValueTuple<C.C2, C.C2>""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ldnull
  IL_002e:  ldc.i4.4
  IL_002f:  call       ""C.C2 C.C2.op_Explicit(int)""
  IL_0034:  newobj     ""System.ValueTuple<C.C2, C.C2>..ctor(C.C2, C.C2)""
  IL_0039:  box        ""System.ValueTuple<C.C2, C.C2>""
  IL_003e:  call       ""void System.Console.WriteLine(object)""
  IL_0043:  ret
}
"); ;
        }

        [Fact]
        [WorkItem(11804, "https://github.com/dotnet/roslyn/issues/11804")]
        public void ExplicitConversionsSimple02Expr()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x = (a:1, b:2);
        var y = ((C2, C2))x;        
        System.Console.WriteLine(y);

        var z = ((C2, C2))(null, 4); 

        System.Console.WriteLine(z);
    }

    private class C2
    {
        private int x;

        public static explicit operator C2(int arg)
        {
            var result = new C2();
            result.x = arg + 1;
            return result;
        }

        public override string ToString()
        {
            return x.ToString();
        }
    }
}
" + trivial2uple + tupleattributes_cs;

            var comp = CompileAndVerify(source, parseOptions: TestOptions.Regular, expectedOutput: @"
{2, 3}
{, 5}");

            comp.VerifyIL("C.Main", @"
{
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000e:  call       ""C.C2 C.C2.op_Explicit(int)""
  IL_0013:  ldloc.0
  IL_0014:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0019:  call       ""C.C2 C.C2.op_Explicit(int)""
  IL_001e:  newobj     ""System.ValueTuple<C.C2, C.C2>..ctor(C.C2, C.C2)""
  IL_0023:  box        ""System.ValueTuple<C.C2, C.C2>""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ldnull
  IL_002e:  ldc.i4.4
  IL_002f:  call       ""C.C2 C.C2.op_Explicit(int)""
  IL_0034:  newobj     ""System.ValueTuple<C.C2, C.C2>..ctor(C.C2, C.C2)""
  IL_0039:  box        ""System.ValueTuple<C.C2, C.C2>""
  IL_003e:  call       ""void System.Console.WriteLine(object)""
  IL_0043:  ret
}
");

        }

        [WorkItem(12064, "https://github.com/dotnet/roslyn/issues/12064")]
        [Fact]
        public void ExplicitConversionsPreference01()
        {
            var source = @"

using System;

class B
{
    static void Main()
    {
        C<int> x = new D<int>(""original"");

        D<int> y = (D<int>)x;           // explicit builtin downcast is preferred
        Console.WriteLine(""explicit"");
        Console.WriteLine(y);
        Console.WriteLine();

        y = x;                          // implicit user defined implicit conversion
        Console.WriteLine(""implicit"");
        Console.WriteLine(y);

        Console.WriteLine();
        Console.WriteLine();

        (C<int>, C<int>) xt = (new D<int>(""original1""), new D<int>(""original2""));
        var xtxt = (xt, xt);

        Console.WriteLine(""explicit"");
        (D<int>, D<int>) yt = ((D<int>, D<int>))xt;           // explicit builtin downcast is preferred
        Console.WriteLine(yt);

        ((D<int>, D<int>),(D<int>, D<int>)) ytyt = (((D<int>, D<int>),(D<int>, D<int>)))(xt, xt);   // explicit builtin downcast is preferred
        Console.WriteLine(ytyt);

        ytyt = (((D<int>, D<int>),(D<int>, D<int>)))xtxt;   // explicit builtin downcast is preferred
        Console.WriteLine(ytyt);

        (D<int>, D<int>)? ytn = ((D<int>, D<int>)?)xt;           // explicit builtin downcast is preferred (nullable)
        Console.WriteLine(ytn);
        Console.WriteLine();


        Console.WriteLine(""implicit"");
        yt = xt;                          // implicit user defined implicit conversion
        Console.WriteLine(yt);

        ytyt = xtxt;                     // implicit user defined implicit conversion
        Console.WriteLine(ytyt);

        ytyt = (xt, xt);                  // implicit user defined implicit conversion
        Console.WriteLine(ytyt);

        ytn = xt;                         // implicit user defined implicit conversion
        Console.WriteLine(ytn);
    }
}

class C<T> { }
class D<T> : C<T>
{
    private readonly string val;

    public D(string val)
    {
        this.val = val;
    }

    public override string ToString()
    {
        return val;
    }

    public static implicit operator D<T>(C<int> x)
    {
        return new D<T>(""converted"");
    }
}
" + trivial2uple;

            var comp = CompileAndVerify(source, expectedOutput: @"
explicit
original

implicit
converted


explicit
{original1, original2}
{{original1, original2}, {original1, original2}}
{{original1, original2}, {original1, original2}}
{original1, original2}

implicit
{converted, converted}
{{converted, converted}, {converted, converted}}
{{converted, converted}, {converted, converted}}
{converted, converted}
");
                        
        }

        [Fact]
        public void ExplicitConversionsPreference02()
        {
            var source = @"

using System;

class A
{
    public static implicit operator A(int d) { return null; }
}

class B : A
{
    public static implicit operator B(long d) { return null; }
}

class C
{
    static void Main()
    {
        var x = 1;
        var xt = (x, x);

        // implicit conversions are unambiguous
        B b = x;
        (B, B) bt = xt;

        // the following is an error for compat reasons 
        // explicit conversion is ambiguous and implicit conversion exists, but ignored
        b = (B)x;

        // SHOULD THIS BE AN ERROR TOO?
        bt = ((B, B))xt;

        // SHOULD THIS BE AN ERROR TOO?
        bt = ((B, B)?)xt;
}
}
" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source, assemblyName: "ImplicitConversions06Err");
            comp.VerifyEmitDiagnostics(
                // (28,13): error CS0457: Ambiguous user defined conversions 'B.implicit operator B(long)' and 'A.implicit operator A(int)' when converting from 'int' to 'B'
                //         b = (B)x;
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(B)x").WithArguments("B.implicit operator B(long)", "A.implicit operator A(int)", "int", "B").WithLocation(28, 13),
                // (31,14): error CS0030: Cannot convert type '(int, int)' to '(B, B)'
                //         bt = ((B, B))xt;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "((B, B))xt").WithArguments("(int, int)", "(B, B)").WithLocation(31, 14),
                // (34,14): error CS0030: Cannot convert type '(int, int)' to '(B, B)?'
                //         bt = ((B, B)?)xt;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "((B, B)?)xt").WithArguments("(int, int)", "(B, B)?").WithLocation(34, 14)
            );
        }

        [Fact]
        public void ConversionsOverload02()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        (long, long) x = (1,1);
        Test(x);

        (int, int) y = (1,1);      
        Test(y);

        Test((1, 1));
    }

    static void Test((long, long) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((long?, long?) x)
    {
        System.Console.WriteLine(""second"");
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
first
first
first
");
        }

        [Fact]
        public void ConversionsOverload03()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((1, 1));

        (int, int)? y = (1,1);      
        Test(y);

        (long, long)? x = (1,1);
        Test(x);
    }

    static void Test((long?, long?)? x)
    {
        System.Console.WriteLine(""second"");
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
second
second
second
");
        }

        [Fact]
        public void ConversionsOverload04()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        Test((1, 1));

        (int, int)? y = (1,1);      
        Test(y);

        (long, long)? x = (1,1);
        Test(x);
    }

    static void Test((long, long) x)
    {
        System.Console.WriteLine(""first"");
    }

    static void Test((long?, long?)? x)
    {
        System.Console.WriteLine(""second"");
    }
}
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
first
second
second
");
        }

        [Fact]
        public void ConversionsOverload05()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Action<IEnumerable> x = null;
        Action<IEnumerable<int>> x1 = null;

        // valid and T is IEnumerable<int> 
        // Action<in T> is introducing upper bound on T 
        Test(x, x1);

        (Action<IEnumerable> x, Action<IEnumerable> y) z = (null, null);
        (Action<IEnumerable<int>> x, Action<IEnumerable<int>> y) z1 = (null, null);

        // inference fails
        // (T, T) is introducing exact bound on T even in an 'in' param position
        Test(z, z1);

        // evdently, this is an error. 
        Test<(IEnumerable<int>, IEnumerable <int>)> (z, z1);
    }

    public static void Test<T>(Action<T> x, Action<T> y)
    {
        System.Console.WriteLine(typeof(T));
    }
}

" + trivial2uple;

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (22,9): error CS0411: The type arguments for method 'Program.Test<T>(Action<T>, Action<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Test(z, z1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Program.Test<T>(System.Action<T>, System.Action<T>)").WithLocation(22, 9),
                // (25,54): error CS1503: Argument 1: cannot convert from '(System.Action<System.Collections.IEnumerable> x, System.Action<System.Collections.IEnumerable> y)' to 'System.Action<(System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>)>'
                //         Test<(IEnumerable<int>, IEnumerable <int>)> (z, z1);
                Diagnostic(ErrorCode.ERR_BadArgType, "z").WithArguments("1", "(System.Action<System.Collections.IEnumerable> x, System.Action<System.Collections.IEnumerable> y)", "System.Action<(System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>)>").WithLocation(25, 54),
                // (25,57): error CS1503: Argument 2: cannot convert from '(System.Action<System.Collections.Generic.IEnumerable<int>> x, System.Action<System.Collections.Generic.IEnumerable<int>> y)' to 'System.Action<(System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>)>'
                //         Test<(IEnumerable<int>, IEnumerable <int>)> (z, z1);
                Diagnostic(ErrorCode.ERR_BadArgType, "z1").WithArguments("2", "(System.Action<System.Collections.Generic.IEnumerable<int>> x, System.Action<System.Collections.Generic.IEnumerable<int>> y)", "System.Action<(System.Collections.Generic.IEnumerable<int>, System.Collections.Generic.IEnumerable<int>)>").WithLocation(25, 57)
                );
        }

        [Fact]
        public void ClassifyConversionIdentity01()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var int_string1 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));
            var int_string2 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));
            var int_stringNamed = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), ImmutableArray.Create("a", "b"));

            Assert.Equal(ConversionKind.Identity, comp.ClassifyConversion(int_string1, int_string1).Kind);
            Assert.Equal(ConversionKind.Identity, comp.ClassifyConversion(int_string1, int_string2).Kind);
            Assert.Equal(ConversionKind.Identity, comp.ClassifyConversion(int_string1, int_stringNamed).Kind);
        }

        [Fact]
        public void ClassifyConversionIdentity02()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var int_string1 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct((TypeSymbol)intType, (TypeSymbol)stringType);
            var int_string2 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));
            var int_stringNamed = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType), ImmutableArray.Create("a", "b"));

            Assert.Equal(ConversionKind.Identity, comp.ClassifyConversion(int_string1, int_string1).Kind);
            Assert.Equal(ConversionKind.Identity, comp.ClassifyConversion(int_string1, int_string2).Kind);
            Assert.Equal(ConversionKind.Identity, comp.ClassifyConversion(int_string1, int_stringNamed).Kind);
            Assert.Equal(ConversionKind.Identity, comp.ClassifyConversion(int_string2, int_string1).Kind);
            Assert.Equal(ConversionKind.Identity, comp.ClassifyConversion(int_stringNamed, int_string1).Kind);
        }

        [Fact]
        public void ClassifyConversionNone01()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);

            var int_int = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType));
            var int_int_int = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, intType, intType));
            var string_string = comp.CreateTupleTypeSymbol(ImmutableArray.Create(stringType, stringType));

            var int_int1 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct((TypeSymbol)intType, (TypeSymbol)intType);

            Assert.Equal(ConversionKind.NoConversion, comp.ClassifyConversion(int_int, int_int_int).Kind);
            Assert.Equal(ConversionKind.NoConversion, comp.ClassifyConversion(int_int, string_string).Kind);
            Assert.Equal(ConversionKind.NoConversion, comp.ClassifyConversion(int_int1, string_string).Kind);
            Assert.Equal(ConversionKind.NoConversion, comp.ClassifyConversion(int_int1, int_int_int).Kind);
            Assert.Equal(ConversionKind.NoConversion, comp.ClassifyConversion(string_string, int_int1).Kind);
            Assert.Equal(ConversionKind.NoConversion, comp.ClassifyConversion(int_int_int, int_int1).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit01()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));
            var int_object = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType));

            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string, int_object).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit02()
        {
            var tupleComp = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));
            var int_object1 = comp.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType));

            var int_string2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct((TypeSymbol)intType, (TypeSymbol)stringType);
            var int_object2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct((TypeSymbol)intType, (TypeSymbol)objectType);


            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_object2).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_object1).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_object2).Kind);

            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object2, int_string1).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object1, int_string2).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object2, int_string2).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit03()
        {
            var tupleComp1 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var tupleComp2 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp1.ToMetadataReference(), tupleComp2.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));
            var int_string2 = tupleComp2.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));

            var int_object = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType));

            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_string2).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_string1).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_object).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_object).Kind);

            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string1).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string2).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit03u()
        {
            var tupleComp1 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var tupleComp2 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp1.ToMetadataReference(), tupleComp2.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType)).TupleUnderlyingType;
            var int_string2 = tupleComp2.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));

            var int_object = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType));

            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_string2).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_string1).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_object).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_object).Kind);

            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string1).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string2).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit03uu()
        {
            var tupleComp1 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var tupleComp2 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp1.ToMetadataReference(), tupleComp2.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType)).TupleUnderlyingType;
            var int_string2 = tupleComp2.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType)).TupleUnderlyingType;

            var int_object = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType));

            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_string2).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_string1).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_object).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_object).Kind);

            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string1).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string2).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit03uuu()
        {
            var tupleComp1 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var tupleComp2 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);
            var comp = CSharpCompilation.Create("test", references: new[] { MscorlibRef, tupleComp1.ToMetadataReference(), tupleComp2.ToMetadataReference() });

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType)).TupleUnderlyingType;
            var int_string2 = tupleComp2.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType)).TupleUnderlyingType;

            var int_object = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType)).TupleUnderlyingType;

            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_string2).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_string1).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string1, int_object).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, comp.ClassifyConversion(int_string2, int_object).Kind);

            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string1).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, comp.ClassifyConversion(int_object, int_string2).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit04()
        {
            var text = @"
class C {
    public static implicit operator int(C c) { return 0; }

    public C() 
    {
        var x = (1, ""qq"");
        var i = /*<bind0>*/x/*</bind0>*/;
    }
}";
            var tupleComp1 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);

            var tree = Parse(text);
            var comp = CSharpCompilation.Create("test", syntaxTrees: new[] { tree }, references: new[] { MscorlibRef, tupleComp1.ToMetadataReference()});

            var model = comp.GetSemanticModel(tree);
            var exprs = GetBindingNodes<ExpressionSyntax>(comp);
            var expr1 = exprs.First();

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));
            var int_object = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType));
            var int_object_object = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType, objectType));

            Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(expr1, int_string1).Kind);
            Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(expr1, int_string1, isExplicitInSource: true).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, model.ClassifyConversion(expr1, int_object).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, model.ClassifyConversion(expr1, int_object, isExplicitInSource: true).Kind);

            Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(expr1, int_object_object).Kind);
            Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(expr1, int_object_object, isExplicitInSource: true).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit05()
        {
            var text = @"
class C {
    public static implicit operator int(C c) { return 0; }

    public C() 
    {
        var x = (1, (object)""qq"");
        var i = /*<bind0>*/x/*</bind0>*/;
    }
}";
            var tupleComp1 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);

            var tree = Parse(text);
            var comp = CSharpCompilation.Create("test", syntaxTrees: new[] { tree }, references: new[] { MscorlibRef, tupleComp1.ToMetadataReference() });

            var model = comp.GetSemanticModel(tree);
            var exprs = GetBindingNodes<ExpressionSyntax>(comp);
            var expr1 = exprs.First();

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, stringType));
            var int_object = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType));
            var int_object_object = tupleComp1.CreateTupleTypeSymbol(ImmutableArray.Create(intType, objectType, objectType));

            Assert.Equal(ConversionKind.ExplicitTuple, model.ClassifyConversion(expr1, int_string1).Kind);
            Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(expr1, int_object).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, model.ClassifyConversion(expr1, int_string1, isExplicitInSource: true).Kind);
            Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(expr1, int_object, isExplicitInSource: true).Kind);

            Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(expr1, int_object_object).Kind);
            Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(expr1, int_object_object, isExplicitInSource: true).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit06()
        {
            var text = @"
class C {
    public static implicit operator int(C c) { return 0; }

    public C() 
    {
        var x = (1, ""qq"");
        var i = /*<bind0>*/x/*</bind0>*/;
    }
}";
            var tupleComp1 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);

            var tree = Parse(text);
            var comp = CSharpCompilation.Create("test", syntaxTrees: new[] { tree }, references: new[] { MscorlibRef, tupleComp1.ToMetadataReference() });

            var model = comp.GetSemanticModel(tree);
            var exprs = GetBindingNodes<ExpressionSyntax>(comp);
            var expr1 = exprs.First();

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct((TypeSymbol)intType, (TypeSymbol)stringType);
            var int_object = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct((TypeSymbol)intType, (TypeSymbol)objectType);
            var int_object_object = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T3).Construct((TypeSymbol)intType, (TypeSymbol)objectType, (TypeSymbol)objectType);

            Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(expr1, int_string1).Kind);
            Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(expr1, int_string1, isExplicitInSource: true).Kind);
            Assert.Equal(ConversionKind.ImplicitTuple, model.ClassifyConversion(expr1, int_object).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, model.ClassifyConversion(expr1, int_object, isExplicitInSource: true).Kind);

            Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(expr1, int_object_object).Kind);
            Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(expr1, int_object_object, isExplicitInSource: true).Kind);
        }

        [Fact]
        public void ClassifyConversionImplicit07()
        {
            var text = @"
class C {
    public static implicit operator int(C c) { return 0; }

    public C() 
    {
        var x = (1, (object)""qq"");
        var i = /*<bind0>*/x/*</bind0>*/;
    }
}";
            var tupleComp1 = CreateCompilationWithMscorlib(trivial2uple + trivial3uple + trivialRemainingTuples);

            var tree = Parse(text);
            var comp = CSharpCompilation.Create("test", syntaxTrees: new[] { tree }, references: new[] { MscorlibRef, tupleComp1.ToMetadataReference() });

            var model = comp.GetSemanticModel(tree);
            var exprs = GetBindingNodes<ExpressionSyntax>(comp);
            var expr1 = exprs.First();

            ITypeSymbol intType = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            ITypeSymbol objectType = comp.GetSpecialType(SpecialType.System_Object);

            var int_string1 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct((TypeSymbol)intType, (TypeSymbol)stringType);
            var int_object = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct((TypeSymbol)intType, (TypeSymbol)objectType);
            var int_object_object = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T3).Construct((TypeSymbol)intType, (TypeSymbol)objectType, (TypeSymbol)objectType);

            Assert.Equal(ConversionKind.ExplicitTuple, model.ClassifyConversion(expr1, int_string1).Kind);
            Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(expr1, int_object).Kind);
            Assert.Equal(ConversionKind.ExplicitTuple, model.ClassifyConversion(expr1, int_string1, isExplicitInSource: true).Kind);
            Assert.Equal(ConversionKind.Identity, model.ClassifyConversion(expr1, int_object, isExplicitInSource: true).Kind);

            Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(expr1, int_object_object).Kind);
            Assert.Equal(ConversionKind.NoConversion, model.ClassifyConversion(expr1, int_object_object, isExplicitInSource: true).Kind);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/12267")]
        [WorkItem(12267, "https://github.com/dotnet/roslyn/issues/12267")]
        public void ConstraintsAndNames()
        {
            var source1 = @"
using System.Collections.Generic;

public abstract class Base
{
    public abstract void M<T>(T x) where T : IEnumerable<(int a, int b)>;
}
";

            var source2 = @"
class Derived : Base
{
    public override void M<T>(T x)
    {
        foreach (var y in x)
        {
            System.Console.WriteLine(y.a);
        }
    }
}";

            var comp1 = CreateCompilationWithMscorlib(source1 + trivial2uple + source2);
            comp1.VerifyDiagnostics();

            var comp2 = CreateCompilationWithMscorlib45(source1 + trivial2uple);
            comp2.VerifyDiagnostics();

            // Retargeting (different version of mscorlib)
            var comp3 = CreateCompilationWithMscorlib46(source2, references: new[] { new CSharpCompilationReference(comp2)});
            comp3.VerifyDiagnostics();

            // Metadata
            var comp4 = CreateCompilationWithMscorlib45(source2, references: new[] { comp2.EmitToImageReference() });

            comp4.VerifyDiagnostics();
        }
    }
}
