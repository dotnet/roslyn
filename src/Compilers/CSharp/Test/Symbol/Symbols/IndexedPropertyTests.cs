// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class IndexedPropertyTests : CSharpTestBase
    {
        [ClrOnlyFact]
        public void IndexedProperties()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(index As Integer) As Object
End Interface
Public Class A
    Public Shared Function Create() As IA
        Return New AImpl()
    End Function
    Private NotInheritable Class AImpl
        Implements IA
        Property P(index As Integer) As Object Implements IA.P
            Get
                Console.WriteLine(""P[{0}]"", index)
                Return index * 2
            End Get
            Set(value As Object)
                Console.WriteLine(""P[{0}] = {1}"", index, value)
            End Set
        End Property
    End Class
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void Main()
    {
        var a = A.Create();
        var o = a.P[1];
        a.P[2] = o;
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"P[1]
P[2] = 2
");
            compilation2.VerifyIL("B.Main()",
@"{
  // Code size       21 (0x15)
  .maxstack  3
  .locals init (object V_0) //o
  IL_0000:  call       ""IA A.Create()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""object IA.P[int].get""
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.2
  IL_000e:  ldloc.0
  IL_000f:  callvirt   ""void IA.P[int].set""
  IL_0014:  ret
}");
        }

        [ClrOnlyFact]
        public void OptionalParameters()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(Optional x As Integer = 1, Optional y As Integer = 2) As Object
End Interface
Public Class A
    Public Shared Function Create() As IA
        Return New AImpl()
    End Function
    Private NotInheritable Class AImpl
        Implements IA
        Property P(Optional x As Integer = 1, Optional y As Integer = 2) As Object Implements IA.P
            Get
                Console.WriteLine(""P[{0}, {1}].get"", x, y)
                Return Nothing
            End Get
            Set(value As Object)
                Console.WriteLine(""P[{0}, {1}].set"", x, y)
            End Set
        End Property
    End Class
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void Main()
    {
        var a = A.Create();
        var o = a.P[3, 4];
        a.P[5, 6] = o;
        o = a.P[3];
        a.P[5] = o;
        o = a.P;
        a.P = o;
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"P[3, 4].get
P[5, 6].set
P[3, 2].get
P[5, 2].set
P[1, 2].get
P[1, 2].set
");
            compilation2.VerifyIL("B.Main()",
@"{
  // Code size       59 (0x3b)
  .maxstack  5
  .locals init (object V_0) //o
  IL_0000:  call       ""IA A.Create()""
  IL_0005:  dup
  IL_0006:  ldc.i4.3
  IL_0007:  ldc.i4.4
  IL_0008:  callvirt   ""object IA.P[int, int].get""
  IL_000d:  stloc.0
  IL_000e:  dup
  IL_000f:  ldc.i4.5
  IL_0010:  ldc.i4.6
  IL_0011:  ldloc.0
  IL_0012:  callvirt   ""void IA.P[int, int].set""
  IL_0017:  dup
  IL_0018:  ldc.i4.3
  IL_0019:  ldc.i4.2
  IL_001a:  callvirt   ""object IA.P[int, int].get""
  IL_001f:  stloc.0
  IL_0020:  dup
  IL_0021:  ldc.i4.5
  IL_0022:  ldc.i4.2
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void IA.P[int, int].set""
  IL_0029:  dup
  IL_002a:  ldc.i4.1
  IL_002b:  ldc.i4.2
  IL_002c:  callvirt   ""object IA.P[int, int].get""
  IL_0031:  stloc.0
  IL_0032:  ldc.i4.1
  IL_0033:  ldc.i4.2
  IL_0034:  ldloc.0
  IL_0035:  callvirt   ""void IA.P[int, int].set""
  IL_003a:  ret
}");
        }

        [ClrOnlyFact]
        public void ParamsArrayParameters()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Property P(ParamArray args As Integer()) As Object
End Interface
Public Class A
    Implements IA
    Private Property P(ParamArray args As Integer()) As Object Implements IA.P
        Get
            Report(args)
            Return Nothing
        End Get
        Set(value As Object)
            Report(args)
        End Set
    End Property
    Private Shared Sub Report(args As Integer())
        Dim n = args.Length
        If n = 0 Then
            Console.WriteLine(""-"")
        Else
            For i = 0 To n - 1
                Console.WriteLine(""{0}: {1}"", i, args(i))
            Next
        End If
    End Sub
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void Main()
    {
        var a = new IA();
        object o;
        o = a.P[0];
        o = a.P[1, 2];
        o = a.P[new[] { 3, 4 }];
        a.P[5] = o;
        a.P[6, 7] = o;
        a.P[new[] { 8, 9 }] = o;
        a.P = o;
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"0: 0
0: 1
1: 2
0: 3
1: 4
0: 5
0: 6
1: 7
0: 8
1: 9
-
");
        }

        [ClrOnlyFact]
        public void RefParameters()
        {
            var source1 =
@".class interface public abstract import IA
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 01 41 00 00 )
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
  .method public abstract virtual instance int32 get_P(int32& i) { }
  .method public abstract virtual instance void set_P(int32& i, int32 v) { }
  .property instance int32 P(int32&)
  {
    .get instance int32 IA::get_P(int32&)
    .set instance void IA::set_P(int32&, int32)
  }
}
.class public A implements IA
{
  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
    ret
  }
  // i += 1; return 0;
  .method public virtual instance int32 get_P(int32& i)
  {
    ldarg.1
    ldarg.1
    ldind.i4
    ldc.i4.1
    add.ovf
    stind.i4
    ldc.i4.0
    ret
  }
  // i += 2; return;
  .method public virtual instance void set_P(int32& i, int32 v)
  {
    ldarg.1
    ldarg.1
    ldind.i4
    ldc.i4.2
    add.ovf
    stind.i4
    ret
  }
  .property instance int32 P(int32&)
  {
    .get instance int32 A::get_P(int32&)
    .set instance void A::set_P(int32&, int32)
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"using System;
class B
{
    static void GetAndSet(IA a)
    {
        var value = a.P[F()[0]];
        a.P[F()[0]] = value;
    }
    static void GetAndSetByRef(IA a)
    {
        var value = a.P[ref F()[0]];
        a.P[ref F()[0]] = value;
    }
    static void CompoundAssignment(IA a)
    {
        a.P[F()[0]] += 1;
    }
    static void CompoundAssignmentByRef(IA a)
    {
        a.P[ref F()[0]] += 1;
    }
    static void Increment(IA a)
    {
        a.P[F()[0]]++;
    }
    static void IncrementByRef(IA a)
    {
        a.P[ref F()[0]]++;
    }
    static void Main()
    {
        var a = new IA();
        GetAndSet(a);
        ReportAndReset();
        GetAndSetByRef(a);
        ReportAndReset();
        CompoundAssignment(a);
        ReportAndReset();
        CompoundAssignmentByRef(a);
        ReportAndReset();
        Increment(a);
        ReportAndReset();
        IncrementByRef(a);
        ReportAndReset();
    }
    static int[] i = { 0 };
    static int[] F()
    {
        Console.WriteLine(""F()"");
        return i;
    }
    static void ReportAndReset()
    {
        Console.WriteLine(""{0}"", i[0]);
        i = new[] { 0 };
    }
}";
            // Note that Dev11 (incorrectly) calls F() twice in a.P[ref F()[0]]
            // for compound assignment and increment.
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"F()
F()
0
F()
F()
3
F()
0
F()
3
F()
0
F()
3
");
            compilation2.VerifyIL("B.GetAndSet(IA)",
@"{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (int V_0, //value
  int V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int[] B.F()""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelem.i4
  IL_0008:  stloc.1
  IL_0009:  ldloca.s   V_1
  IL_000b:  callvirt   ""int IA.P[ref int].get""
  IL_0010:  stloc.0
  IL_0011:  ldarg.0
  IL_0012:  call       ""int[] B.F()""
  IL_0017:  ldc.i4.0
  IL_0018:  ldelem.i4
  IL_0019:  stloc.1
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldloc.0
  IL_001d:  callvirt   ""void IA.P[ref int].set""
  IL_0022:  ret
}");
            compilation2.VerifyIL("B.GetAndSetByRef(IA)",
@"{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (int V_0) //value
  IL_0000:  ldarg.0
  IL_0001:  call       ""int[] B.F()""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  callvirt   ""int IA.P[ref int].get""
  IL_0011:  stloc.0
  IL_0012:  ldarg.0
  IL_0013:  call       ""int[] B.F()""
  IL_0018:  ldc.i4.0
  IL_0019:  ldelema    ""int""
  IL_001e:  ldloc.0
  IL_001f:  callvirt   ""void IA.P[ref int].set""
  IL_0024:  ret
}");
            compilation2.VerifyIL("B.CompoundAssignment(IA)",
@"{
  // Code size       33 (0x21)
  .maxstack  4
  .locals init (IA V_0,
  int V_1,
  int V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  call       ""int[] B.F()""
  IL_0007:  ldc.i4.0
  IL_0008:  ldelem.i4
  IL_0009:  stloc.2
  IL_000a:  ldloc.0
  IL_000b:  ldloc.2
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  ldloc.0
  IL_0010:  ldloc.2
  IL_0011:  stloc.1
  IL_0012:  ldloca.s   V_1
  IL_0014:  callvirt   ""int IA.P[ref int].get""
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  callvirt   ""void IA.P[ref int].set""
  IL_0020:  ret
}");
            compilation2.VerifyIL("B.CompoundAssignmentByRef(IA)",
@"{
  // Code size       31 (0x1f)
  .maxstack  4
  .locals init (IA V_0,
  int& V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  call       ""int[] B.F()""
  IL_0007:  ldc.i4.0
  IL_0008:  ldelema    ""int""
  IL_000d:  stloc.1
  IL_000e:  ldloc.0
  IL_000f:  ldloc.1
  IL_0010:  ldloc.0
  IL_0011:  ldloc.1
  IL_0012:  callvirt   ""int IA.P[ref int].get""
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  callvirt   ""void IA.P[ref int].set""
  IL_001e:  ret
}");
            compilation2.VerifyIL("B.Increment(IA)",
@"{
  // Code size       33 (0x21)
  .maxstack  4
  .locals init (int V_0,
  int V_1,
  int V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int[] B.F()""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelem.i4
  IL_0008:  stloc.1
  IL_0009:  dup
  IL_000a:  ldloc.1
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  callvirt   ""int IA.P[ref int].get""
  IL_0013:  stloc.2
  IL_0014:  ldloc.1
  IL_0015:  stloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldloc.2
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  callvirt   ""void IA.P[ref int].set""
  IL_0020:  ret
}");
            compilation2.VerifyIL("B.IncrementByRef(IA)",
@"{
  // Code size       31 (0x1f)
  .maxstack  4
  .locals init (int& V_0,
  int V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int[] B.F()""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  stloc.0
  IL_000d:  dup
  IL_000e:  ldloc.0
  IL_000f:  callvirt   ""int IA.P[ref int].get""
  IL_0014:  stloc.1
  IL_0015:  ldloc.0
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  callvirt   ""void IA.P[ref int].set""
  IL_001e:  ret
}");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void RefParametersIndexers()
        {
            var source1 =
@"// ComImport
.class interface public abstract import IA
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 01 41 00 00 )
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public abstract virtual instance int32 get_P(int32& i) { }
  .method public abstract virtual instance void set_P(int32& i, int32 v) { }
  .property instance int32 P(int32&)
  {
    .get instance int32 IA::get_P(int32&)
    .set instance void IA::set_P(int32&, int32)
  }
}
// Not ComImport
.class interface public abstract IB
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public abstract virtual instance int32 get_P(int32& i) { }
  .method public abstract virtual instance void set_P(int32& i, int32 v) { }
  .property instance int32 P(int32&)
  {
    .get instance int32 IB::get_P(int32&)
    .set instance void IB::set_P(int32&, int32)
  }
}
.class public A implements IA
{
  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
    ret
  }
  // i += 1; return 0;
  .method public virtual instance int32 get_P(int32& i)
  {
    ldarg.1
    ldarg.1
    ldind.i4
    ldc.i4.1
    add.ovf
    stind.i4
    ldc.i4.0
    ret
  }
  // i += 2; return;
  .method public virtual instance void set_P(int32& i, int32 v)
  {
    ldarg.1
    ldarg.1
    ldind.i4
    ldc.i4.2
    add.ovf
    stind.i4
    ret
  }
  .property instance int32 P(int32&)
  {
    .get instance int32 A::get_P(int32&)
    .set instance void A::set_P(int32&, int32)
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"class C
{
    static void M(IB b)
    {
        int x = 0;
        int y = 0;
        b[y] = b[x];
        b[ref y] = b[ref x];
        b.set_P(ref y, b.get_P(ref x));
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (7,9): error CS1545: Property, indexer, or event 'IB.this[ref int]' is not supported by the language; try directly calling accessor methods 'IB.get_P(ref int)' or 'IB.set_P(ref int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "b[y]").WithArguments("IB.this[ref int]", "IB.get_P(ref int)", "IB.set_P(ref int, int)").WithLocation(7, 9),
                // (7,16): error CS1545: Property, indexer, or event 'IB.this[ref int]' is not supported by the language; try directly calling accessor methods 'IB.get_P(ref int)' or 'IB.set_P(ref int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "b[x]").WithArguments("IB.this[ref int]", "IB.get_P(ref int)", "IB.set_P(ref int, int)").WithLocation(7, 16),
                // (8,9): error CS1545: Property, indexer, or event 'IB.this[ref int]' is not supported by the language; try directly calling accessor methods 'IB.get_P(ref int)' or 'IB.set_P(ref int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "b[ref y]").WithArguments("IB.this[ref int]", "IB.get_P(ref int)", "IB.set_P(ref int, int)").WithLocation(8, 9),
                // (8,20): error CS1545: Property, indexer, or event 'IB.this[ref int]' is not supported by the language; try directly calling accessor methods 'IB.get_P(ref int)' or 'IB.set_P(ref int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "b[ref x]").WithArguments("IB.this[ref int]", "IB.get_P(ref int)", "IB.set_P(ref int, int)").WithLocation(8, 20));
            var source3 =
@"class C
{
    static void Main()
    {
        var a = new IA();
        int x = 0;
        int y = 0;
        a[y] = a[x];
        Report(x, y);
        a[ref y] = a[ref x];
        Report(x, y);
        a.set_P(ref y, a.get_P(ref x));
        Report(x, y);
    }
    static void Report(int x, int y)
    {
        System.Console.WriteLine(""{0}, {1}"", x, y);
    }
}";
            var compilation3 = CompileAndVerify(source3, references: new[] { reference1 }, expectedOutput:
@"0, 0
1, 2
2, 4
");
        }

        [ClrOnlyFact]
        public void OptionalRefParameters()
        {
            var source1 =
@".class interface public abstract import IA
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 01 41 00 00 )
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
  .method public abstract virtual instance int32 get_P(int32& x, [opt] int32& y)
  {
    .param[2] = int32(0)
  }
  .method public abstract virtual instance void set_P(int32& x, [opt] int32& y, int32 v)
  {
    .param[2] = int32(0)
  }
  .property instance int32 P(int32&, int32&)
  {
    .get instance int32 IA::get_P(int32&, int32&)
    .set instance void IA::set_P(int32&, int32&, int32)
  }
}
.class public A implements IA
{
  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
    ret
  }
  // y += 1; return 0
  .method public virtual instance int32 get_P(int32& x, int32& y)
  {
    ldarg.2
    ldc.i4.0
    ceq
    brtrue L1
    ldarg.2
    ldarg.2
    ldind.i4
    ldc.i4.1
    add.ovf
    stind.i4
  L1:
    ldc.i4.0
    ret
  }
  // y += 2
  .method public virtual instance void set_P(int32& x, int32& y, int32 v)
  {
    ldarg.2
    ldc.i4.0
    ceq
    brtrue L1
    ldarg.2
    ldarg.2
    ldind.i4
    ldc.i4.2
    add.ovf
    stind.i4
  L1:
    ret
  }
  .property instance int32 P(int32&, int32&)
  {
    .get instance int32 A::get_P(int32&, int32&)
    .set instance void A::set_P(int32&, int32&, int32)
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"class C
{
    static void MissingArg(IA a)
    {
        a.P[x]++;
    }
    static void ValueArgs(IA a)
    {
        a.P[x, y]++;
    }
    static void RefArgs(IA a)
    {
        a.P[ref x, ref y]++;
    }
    static void Main()
    {
        var a = new IA();
        MissingArg(a);
        ReportAndReset();
        ValueArgs(a);
        ReportAndReset();
        RefArgs(a);
        ReportAndReset();
    }
    static int x;
    static int y;
    static void ReportAndReset()
    {
        System.Console.WriteLine(""{0}, {1}"", x, y);
        x = 0;
        y = 0;
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"0, 0
0, 0
0, 3");
            compilation2.VerifyIL("C.MissingArg(IA)",
@"{
  // Code size       39 (0x27)
  .maxstack  5
  .locals init (int V_0,
  int V_1,
  int V_2,
  int V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""int C.x""
  IL_0006:  stloc.2
  IL_0007:  dup
  IL_0008:  ldloc.2
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  callvirt   ""int IA.P[ref int, ref int].get""
  IL_0015:  stloc.3
  IL_0016:  ldloc.2
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  ldc.i4.0
  IL_001b:  stloc.1
  IL_001c:  ldloca.s   V_1
  IL_001e:  ldloc.3
  IL_001f:  ldc.i4.1
  IL_0020:  add
  IL_0021:  callvirt   ""void IA.P[ref int, ref int].set""
  IL_0026:  ret
}");
            compilation2.VerifyIL("C.ValueArgs(IA)",
@"{
  // Code size       47 (0x2f)
  .maxstack  5
  .locals init (int V_0,
  int V_1,
  int V_2,
  int V_3,
  int V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""int C.x""
  IL_0006:  stloc.2
  IL_0007:  ldsfld     ""int C.y""
  IL_000c:  stloc.3
  IL_000d:  dup
  IL_000e:  ldloc.2
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldloc.3
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  callvirt   ""int IA.P[ref int, ref int].get""
  IL_001b:  stloc.s    V_4
  IL_001d:  ldloc.2
  IL_001e:  stloc.0
  IL_001f:  ldloca.s   V_0
  IL_0021:  ldloc.3
  IL_0022:  stloc.1
  IL_0023:  ldloca.s   V_1
  IL_0025:  ldloc.s    V_4
  IL_0027:  ldc.i4.1
  IL_0028:  add
  IL_0029:  callvirt   ""void IA.P[ref int, ref int].set""
  IL_002e:  ret
}");
            compilation2.VerifyIL("C.RefArgs(IA)",
@"{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (int& V_0,
  int& V_1,
  int V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldsflda    ""int C.x""
  IL_0006:  stloc.0
  IL_0007:  ldsflda    ""int C.y""
  IL_000c:  stloc.1
  IL_000d:  dup
  IL_000e:  ldloc.0
  IL_000f:  ldloc.1
  IL_0010:  callvirt   ""int IA.P[ref int, ref int].get""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  callvirt   ""void IA.P[ref int, ref int].set""
  IL_0020:  ret
}");
        }

        [ClrOnlyFact]
        public void DefaultProperty()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Default Property P(index As Integer) As Object
End Interface
<ComImport()>
Public Class A
    Implements IA
    Default Property P(index As Integer) As Object Implements IA.P
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void M()
    {
        var a = new A();
        a.P[2] = a.P[1];
        a[4] = a[3];
        a[7, 8] = a[5, 6];
        a.set_P(10, a.get_P(9));
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (6,11): error CS1061: 'A' does not contain a definition for 'P' and no extension method 'P' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("A", "P").WithLocation(6, 11),
                // (6,20): error CS1061: 'A' does not contain a definition for 'P' and no extension method 'P' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("A", "P").WithLocation(6, 20),
                // (8,9): error CS1501: No overload for method 'this' takes 2 arguments
                Diagnostic(ErrorCode.ERR_BadArgCount, "a[7, 8]").WithArguments("this", "2").WithLocation(8, 9),
                // (8,19): error CS1501: No overload for method 'this' takes 2 arguments
                Diagnostic(ErrorCode.ERR_BadArgCount, "a[5, 6]").WithArguments("this", "2").WithLocation(8, 19),
                // (9,11): error CS0571: 'A.this[int].set': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_P").WithArguments("A.this[int].set").WithLocation(9, 11),
                // (9,23): error CS0571: 'A.this[int].get': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_P").WithArguments("A.this[int].get").WithLocation(9, 23));
            var source3 =
@"class C
{
    static void M()
    {
        var a = new A();
        a[2] = a[1];
    }
}";
            var compilation3 = CompileAndVerify(source3, references: new[] { reference1 });
            compilation3.VerifyIL("C.M()",
@"{
  // Code size       21 (0x15)
  .maxstack  4
  .locals init (A V_0) //a
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.2
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  callvirt   ""object A.this[int].get""
  IL_000f:  callvirt   ""void A.this[int].set""
  IL_0014:  ret
}");
        }

        /// <summary>
        /// Allow calling indexed property accessors
        /// directly, for legacy code.
        /// </summary>
        [ClrOnlyFact]
        public void CanBeReferencedByName()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(index As Integer) As Object
End Interface
Public Interface IB
    Inherits IA
    Property Q(index As Integer) As Object
    Property R As Object
End Interface
Public Class B
    Public Shared Function Create() As IB
        Return New AImpl()
    End Function
    Private NotInheritable Class AImpl
        Implements IA, IB
        Property P(index As Integer) As Object Implements IA.P
            Get
                Console.WriteLine(""P[{0}]"", index)
                Return index
            End Get
            Set(value As Object)
                Console.WriteLine(""P[{0}] = {1}"", index, value)
            End Set
        End Property
        Property Q(index As Integer) As Object Implements IB.Q
            Get
                Console.WriteLine(""Q[{0}]"", index)
                Return index
            End Get
            Set(value As Object)
                Console.WriteLine(""Q[{0}] = {1}"", index, value)
            End Set
        End Property
        Property R As Object Implements IB.R
            Get
                Console.WriteLine(""R"")
                Return 0
            End Get
            Set(value As Object)
                Console.WriteLine(""R = {0}"", value)
            End Set
        End Property
    End Class
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"using System;
class C
{
    static void Main()
    {
        var b = B.Create();
        var o = b.get_P(1);
        b.set_P(2, o);
        o = b.get_Q(3);
        b.set_Q(4, o);
        Func<int, object> g = b.get_P;
        o = g(5);
        Action<int, object> s = b.set_Q;
        s(6, o);
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"P[1]
P[2] = 1
Q[3]
Q[4] = 3
P[5]
Q[6] = 5
");

            var @namespace = (NamespaceSymbol)((CSharpCompilation)compilation2.Compilation).GlobalNamespace;
            // Property with parameters from type with [ComImport].
            var property = @namespace.GetMember<NamedTypeSymbol>("IA").GetMember<PropertySymbol>("P");
            Assert.False(property.MustCallMethodsDirectly);
            Assert.True(property.CanCallMethodsDirectly());
            Assert.True(property.GetMethod.CanBeReferencedByName);
            Assert.True(property.GetMethod.CanBeReferencedByNameIgnoringIllegalCharacters);
            Assert.True(property.SetMethod.CanBeReferencedByName);
            Assert.True(property.SetMethod.CanBeReferencedByNameIgnoringIllegalCharacters);
            // Property with parameters from type without [ComImport].
            property = @namespace.GetMember<NamedTypeSymbol>("IB").GetMember<PropertySymbol>("Q");
            Assert.True(property.MustCallMethodsDirectly);
            Assert.True(property.CanCallMethodsDirectly());
            Assert.True(property.GetMethod.CanBeReferencedByName);
            Assert.True(property.GetMethod.CanBeReferencedByNameIgnoringIllegalCharacters);
            Assert.True(property.SetMethod.CanBeReferencedByName);
            Assert.True(property.SetMethod.CanBeReferencedByNameIgnoringIllegalCharacters);
            // Property without parameters.
            property = @namespace.GetMember<NamedTypeSymbol>("IB").GetMember<PropertySymbol>("R");
            Assert.False(property.MustCallMethodsDirectly);
            Assert.False(property.CanCallMethodsDirectly());
            Assert.False(property.GetMethod.CanBeReferencedByName);
            Assert.False(property.GetMethod.CanBeReferencedByNameIgnoringIllegalCharacters);
            Assert.False(property.SetMethod.CanBeReferencedByName);
            Assert.False(property.SetMethod.CanBeReferencedByNameIgnoringIllegalCharacters);

            compilation2.VerifyIL("C.Main()",
@"{
  // Code size       77 (0x4d)
  .maxstack  4
  .locals init (object V_0) //o
  IL_0000:  call       ""IB B.Create()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""object IA.P[int].get""
  IL_000c:  stloc.0
  IL_000d:  dup
  IL_000e:  ldc.i4.2
  IL_000f:  ldloc.0
  IL_0010:  callvirt   ""void IA.P[int].set""
  IL_0015:  dup
  IL_0016:  ldc.i4.3
  IL_0017:  callvirt   ""object IB.get_Q(int)""
  IL_001c:  stloc.0
  IL_001d:  dup
  IL_001e:  ldc.i4.4
  IL_001f:  ldloc.0
  IL_0020:  callvirt   ""void IB.set_Q(int, object)""
  IL_0025:  dup
  IL_0026:  dup
  IL_0027:  ldvirtftn  ""object IA.P[int].get""
  IL_002d:  newobj     ""System.Func<int, object>..ctor(object, System.IntPtr)""
  IL_0032:  ldc.i4.5
  IL_0033:  callvirt   ""object System.Func<int, object>.Invoke(int)""
  IL_0038:  stloc.0
  IL_0039:  dup
  IL_003a:  ldvirtftn  ""void IB.set_Q(int, object)""
  IL_0040:  newobj     ""System.Action<int, object>..ctor(object, System.IntPtr)""
  IL_0045:  ldc.i4.6
  IL_0046:  ldloc.0
  IL_0047:  callvirt   ""void System.Action<int, object>.Invoke(int, object)""
  IL_004c:  ret
}");
        }

        /// <summary>
        /// CanBeReferencedByName should return false if
        /// the accessor name is not a valid identifier.
        /// </summary>
        [ClrOnlyFact]
        public void CanBeReferencedByName_InvalidName()
        {
            // Note: Dev11 treats I.Q as invalid so Q is not recognized from source.
            var source1 =
@".class interface public abstract import I
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
  .method public abstract virtual instance object valid_name(object) { }
  .method public abstract virtual instance object invalid.name(object) { }
  .property instance object P(object)
  {
    .get instance object I::valid_name(object)
  }
  .property instance object Q(object)
  {
    .get instance object I::invalid.name(object)
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"class C
{
    static void M(I i)
    {
        var o = i.P[1];
        o = i.Q[2];
        o = i.valid_name(1);
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, verify: Verification.Passes);

            var @namespace = (NamespaceSymbol)((CSharpCompilation)compilation2.Compilation).GlobalNamespace;
            // Indexed property with valid name.
            var type = @namespace.GetMember<NamedTypeSymbol>("I");
            var property = type.GetMember<PropertySymbol>("P");
            Assert.False(property.MustCallMethodsDirectly);
            Assert.True(property.CanCallMethodsDirectly());
            Assert.True(property.GetMethod.CanBeReferencedByName);
            Assert.True(property.GetMethod.CanBeReferencedByNameIgnoringIllegalCharacters);
            // Indexed property with invalid name.
            property = type.GetMember<PropertySymbol>("Q");
            Assert.False(property.MustCallMethodsDirectly);
            Assert.True(property.CanCallMethodsDirectly());
            Assert.False(property.GetMethod.CanBeReferencedByName);
            Assert.True(property.GetMethod.CanBeReferencedByNameIgnoringIllegalCharacters);

            compilation2.VerifyIL("C.M(I)",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""int""
  IL_0007:  callvirt   ""object I.P[object].get""
  IL_000c:  pop
  IL_000d:  ldarg.0
  IL_000e:  ldc.i4.2
  IL_000f:  box        ""int""
  IL_0014:  callvirt   ""object I.Q[object].get""
  IL_0019:  pop
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.1
  IL_001c:  box        ""int""
  IL_0021:  callvirt   ""object I.P[object].get""
  IL_0026:  pop
  IL_0027:  ret
}");
        }

        [Fact]
        public void NotIndexedProperties()
        {
            var source =
@"using System.ComponentModel;
[DefaultProperty(""R"")]
class A
{
    internal object P { get { return null; } }
    internal object[] Q { get { return null; } }
    internal object this[int index] { get { return null; } }
}
class B
{
    static void M(A a)
    {
        object o;
        o = a.P[1];
        o = a.Q[2];
        o = a.R[3];
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,13): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "a.P[1]").WithArguments("object").WithLocation(14, 13),
                // (16,15): error CS1061: 'A' does not contain a definition for 'R' and no extension method 'R' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "R").WithArguments("A", "R").WithLocation(16, 15));
        }

        [ClrOnlyFact]
        public void BaseProperties()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class A
    Property P(index As Integer) As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"class B : A
{
    void M()
    {
        object o;
        o = P[1];
        P[2] = o;
        o = this.P[3];
        base.P[4] = o;
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics();
        }

        [ClrOnlyFact]
        public void StaticProperties()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class A
    Shared Property P(index As Integer) As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"class B : A
{
    void M()
    {
        object o;
        o = P[0];
        P[1] = o;
        o = this.P[2];
        A.P[3] = o;
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (6,13): error CS1545: Property, indexer, or event 'A.P[int]' is not supported by the language; try directly calling accessor methods 'A.get_P(int)' or 'A.set_P(int, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A.P[int]", "A.get_P(int)", "A.set_P(int, object)").WithLocation(6, 13),
                // (7,9): error CS1545: Property, indexer, or event 'A.P[int]' is not supported by the language; try directly calling accessor methods 'A.get_P(int)' or 'A.set_P(int, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A.P[int]", "A.get_P(int)", "A.set_P(int, object)").WithLocation(7, 9),
                // (8,18): error CS1545: Property, indexer, or event 'A.P[int]' is not supported by the language; try directly calling accessor methods 'A.get_P(int)' or 'A.set_P(int, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A.P[int]", "A.get_P(int)", "A.set_P(int, object)").WithLocation(8, 18),
                // (9,11): error CS1545: Property, indexer, or event 'A.P[int]' is not supported by the language; try directly calling accessor methods 'A.get_P(int)' or 'A.set_P(int, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A.P[int]", "A.get_P(int)", "A.set_P(int, object)").WithLocation(9, 11));
        }

        /// <summary>
        /// Indexed properties are only supported from [ComImport] types.
        /// </summary>
        [ClrOnlyFact]
        public void ComImport()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(index As Object) As Object
    Property Q(index As Object) As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public Interface IB
    Property P(index As Object) As Object
    Property Q(index As Object) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void M(IA a, IB b)
    {
        a.P[null] = a.Q[null];
        b.P[null] = b.Q[null];
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (5,11): error CS1545: Property, indexer, or event 'IA.P[object]' is not supported by the language; try directly calling accessor methods 'IA.get_P(object)' or 'IA.set_P(object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("IA.P[object]", "IA.get_P(object)", "IA.set_P(object, object)").WithLocation(5, 11),
                // (5,23): error CS1545: Property, indexer, or event 'IA.Q[object]' is not supported by the language; try directly calling accessor methods 'IA.get_Q(object)' or 'IA.set_Q(object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Q").WithArguments("IA.Q[object]", "IA.get_Q(object)", "IA.set_Q(object, object)").WithLocation(5, 23));
        }

        [ClrOnlyFact]
        public void PropertyAccesses()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Default Property A(x As Integer) As IA
    Default Property A(x As Integer, y As Integer) As IB
    Default Property A(x As Integer, y As Integer, z As Integer) As IC
    Default Property A(x As Integer, y As Integer, z As Integer, w As Integer) As Integer()
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public Interface IB
    Property A As IA
    Property B As IB
    Property C As IC
    Property D As Integer()
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E213"")>
Public Interface IC
    Property A(Optional x As Integer = 0) As IA
    Property B(Optional x As Integer = 0) As IB
    Property C(Optional x As Integer = 0) As IC
    Property D(Optional x As Integer = 0) As Integer()
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void M(IA a, IB b, IC c)
    {
        int i;
        a = a[0][1];
        b = a[0, 1].B;
        c = a[0, 1, 2].C[0];
        i = a[0, 1, 2, 3][0];
        a = b.A[0];
        b = b.B.B;
        c = b.C.C[0];
        i = b.D[0];
        a = c.A[0][0];
        b = c.B[0].B;
        c = c.C[0].C[0];
        i = c.D[0];
        i = c.D[0][1];
        a = c.A[0, 1].A; // CS1501
        b = c.B.B;
        c = c.C.C[0];
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (17,13): error CS0029: Cannot implicitly convert type 'int[]' to 'int'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c.D[0]").WithArguments("int[]", "int").WithLocation(17, 13),
                // (19,13): error CS1501: No overload for method 'A' takes 2 arguments
                Diagnostic(ErrorCode.ERR_BadArgCount, "c.A[0, 1]").WithArguments("A", "2").WithLocation(19, 13));
        }

        /// <summary>
        /// Cases where a PropertyGroup must be converted to a PropertyAccess.
        /// (resulting from an indexed property expression with no args).
        /// </summary>
        [ClrOnlyFact]
        public void PropertyGroup()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface I
    Property P(Optional x As Integer = 0) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void M(I i)
    {
        object o = i.P;
        i.P = o;
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics();
        }

        /// <summary>
        /// Overload resolution should be supported for indexed properties,
        /// even though COM does not support overloads.
        /// </summary>
        [ClrOnlyFact]
        public void OverloadResolution()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(o As Object) As Object
    Property P(x As Object, y As Object) As Object
    Property Q(o As Integer) As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public Interface IB
    Property Q(x As Object, y As Object) As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E213"")>
Public Interface IC
    Inherits IA, IB
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void M(IC c)
    {
        var o = c.P[1, 2];
        c.Q[1, 2] = o; // Dev11: CS1501: No overload for method 'Q' takes 2 arguments
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics();
        }

        [ClrOnlyFact(Skip = "https://github.com/dotnet/roslyn/issues/39934")]
        [WorkItem(39934, "https://github.com/dotnet/roslyn/issues/39934")]
        public void OverloadResolutionWithSimpleProperty()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P As Object
    Property Q(o As Object) As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public Interface IB
    Property P(o As Object) As Object
    Property Q As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E213"")>
Public Interface IC
    Inherits IA, IB
End Interface
Public Class C
    Public Shared Function Create() As IC
        Return New CImpl()
    End Function
    Private Class CImpl
        Implements IA, IB
        Private Property P_IA As Object Implements IA.P
            Get
                Console.WriteLine(""P_IA.get"")
                Return Nothing
            End Get
            Set(value As Object)
                Console.WriteLine(""P_IA.set"")
            End Set
        End Property
        Private Property P_IB(o As Object) As Object Implements IB.P
            Get
                Console.WriteLine(""P_IB.get"")
                Return Nothing
            End Get
            Set(value As Object)
                Console.WriteLine(""P_IB.set"")
            End Set
        End Property
        Private Property Q_IA(o As Object) As Object Implements IA.Q
            Get
                Console.WriteLine(""Q_IA.get"")
                Return Nothing
            End Get
            Set(value As Object)
                Console.WriteLine(""Q_IA.set"")
            End Set
        End Property
        Private Property Q_IB As Object Implements IB.Q
            Get
                Console.WriteLine(""Q_IB.get"")
                Return Nothing
            End Get
            Set(value As Object)
                Console.WriteLine(""Q_IB.set"")
            End Set
        End Property
    End Class
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class D
{
    static void M(IC c)
    {
        object o;
        o = c.P;
        o = c.P[1];
        c.Q = o;
        c.Q[2] = o;
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2);
        }

        [ClrOnlyFact]
        public void OverridesHidesImplements()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    ReadOnly Property P(o As Object) As Object
    ReadOnly Property Q(o As Object) As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public Class A
    Implements IA
    Public Overridable ReadOnly Property P(o As Object) As Object Implements IA.P
        Get
            Return Nothing
        End Get
    End Property
    Public ReadOnly Property Q(o As Object) As Object Implements IA.Q
        Get
            Return Nothing
        End Get
    End Property
End Class
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E213"")>
Public Class B
    Inherits A
    Public Overrides ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Shadows ReadOnly Property Q(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Skipped);
            var source2 =
@"class C
{
    static void M(B b)
    {
        var o = b.Q[0];
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (5,17): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'B.Q[object, object]'
                //         var o = b.Q[0];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "b.Q[0]").WithArguments("y", "B.Q[object, object]").WithLocation(5, 17));
            var source3 =
@"class C
{
    static void M(B b)
    {
        var o = b.P[1];
        o = b.Q[2, 3];
    }
}";
            var compilation3 = CompileAndVerify(source3, references: new[] { reference1 }, verify: Verification.Skipped);
            compilation3.VerifyIL("C.M(B)",
@"{
  // Code size       33 (0x21)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""int""
  IL_0007:  callvirt   ""object A.P[object].get""
  IL_000c:  pop
  IL_000d:  ldarg.0
  IL_000e:  ldc.i4.2
  IL_000f:  box        ""int""
  IL_0014:  ldc.i4.3
  IL_0015:  box        ""int""
  IL_001a:  callvirt   ""object B.Q[object, object].get""
  IL_001f:  pop
  IL_0020:  ret
}");
        }

        /// <summary>
        /// Should support implementing and overriding indexed properties
        /// from C# if the accessors are implemented/overridden directly.
        /// </summary>
        [WorkItem(545516, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545516")]
        [ClrOnlyFact]
        public void InterfaceImplementation()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface I
    Property P(index As Integer) As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public MustInherit Class A0
    Implements I
    MustOverride Property P(index As Integer) As Object Implements I.P
End Class
Public MustInherit Class A1
    Implements I
    MustOverride Property P(index As Integer) As Object Implements I.P
End Class
Public Class M
    Shared Function get_P(o As I, index As Integer) As Object
        Return o.P(index)
    End Function
    Shared Sub set_P(o As I, index As Integer, value As Object)
        o.P(index) = value
    End Sub
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"using System;
// Implicit implementation.
public class A2 : I
{
    public virtual object get_P(int index)
    {
        Console.WriteLine(""A2.get_P({0})"", index);
        return null;
    }
    public virtual void set_P(int index, object value)
    {
        Console.WriteLine(""A2.set_P({0}, ...)"", index);
    }
}
// Explicit implementation.
public class A3 : I
{
    object I.get_P(int index)
    {
        Console.WriteLine(""A3.get_P({0})"", index);
        return null;
    }
    void I.set_P(int index, object value)
    {
        Console.WriteLine(""A3.set_P({0}, ...)"", index);
    }
}
// Overridden implementation of indexed properties.
// (COMException loading type at runtime however.
// Same exception when compiled with Dev11.)
public class B0 : A0
{
    public override object get_P(int index)
    {
        Console.WriteLine(""B0.get_P({0})"", index);
        return null;
    }
    public override void set_P(int index, object value)
    {
        Console.WriteLine(""B0.set_P({0}, ...)"", index);
    }
}
// Overridden implementation of accessors on imported type.
public class B1 : A1
{
    public override object get_P(int index)
    {
        Console.WriteLine(""B1.get_P({0})"", index);
        return null;
    }
    public override void set_P(int index, object value)
    {
        Console.WriteLine(""B1.set_P({0}, ...)"", index);
    }
}
// Overridden implementation of accessors on source type.
public class B2 : A2
{
    public override object get_P(int index)
    {
        Console.WriteLine(""B2.get_P({0})"", index);
        return null;
    }
    public override void set_P(int index, object value)
    {
        Console.WriteLine(""B2.set_P({0}, ...)"", index);
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 });
            var reference2 = MetadataReference.CreateFromImage(compilation2.EmittedAssemblyData);
            // Can invoke C# implementations by invoking the accessors directly
            // or by casting to the COM interface and invoking the indexed property.
            var source3 =
@"class C
{
    static void Main()
    {
        A2 a2 = new A2();
        M.set_P(a2, 2, M.get_P(a2, 1));
        Invoke(a2);
        A3 a3 = new A3();
        M.set_P(a3, 2, M.get_P(a3, 1));
        Invoke(a3);
        B1 b1 = new B1();
        M.set_P(b1, 2, M.get_P(b1, 1));
        Invoke(b1);
        B2 b2 = new B2();
        M.set_P(b2, 2, M.get_P(b2, 1));
        Invoke(b2);
    }
    static void Invoke(I i)
    {
        M.set_P(i, 4, M.get_P(i, 3));
        i.P[6] = i.P[5];
    }
}";
            var compilation3 = CompileAndVerify(source3, references: new[] { reference1, reference2 }, expectedOutput:
@"A2.get_P(1)
A2.set_P(2, ...)
A2.get_P(3)
A2.set_P(4, ...)
A2.get_P(5)
A2.set_P(6, ...)
A3.get_P(1)
A3.set_P(2, ...)
A3.get_P(3)
A3.set_P(4, ...)
A3.get_P(5)
A3.set_P(6, ...)
B1.get_P(1)
B1.set_P(2, ...)
B1.get_P(3)
B1.set_P(4, ...)
B1.get_P(5)
B1.set_P(6, ...)
B2.get_P(1)
B2.set_P(2, ...)
B2.get_P(3)
B2.set_P(4, ...)
B2.get_P(5)
B2.set_P(6, ...)
");
            // Cannot invoke C# implementations by invoking the indexed property directly.
            var source4 =
@"class C
{
    static void Main()
    {
        A2 a = new A2();
        a.P[2] = a.P[1];
        B2 b = new B2();
        b.P[4] = b.P[3];
    }
}";
            var compilation4 = CreateCompilation(source4, new[] { reference1, reference2 });
            compilation4.VerifyDiagnostics(
                // (6,11): error CS1061: 'A2' does not contain a definition for 'P' and no extension method 'P' accepting a first argument of type 'A2' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("A2", "P").WithLocation(6, 11),
                // (6,20): error CS1061: 'A2' does not contain a definition for 'P' and no extension method 'P' accepting a first argument of type 'A2' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("A2", "P").WithLocation(6, 20),
                // (8,11): error CS1061: 'B2' does not contain a definition for 'P' and no extension method 'P' accepting a first argument of type 'B2' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("B2", "P").WithLocation(8, 11),
                // (8,20): error CS1061: 'B2' does not contain a definition for 'P' and no extension method 'P' accepting a first argument of type 'B2' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P").WithArguments("B2", "P").WithLocation(8, 20));
        }

        /// <summary>
        /// "new" required to hide indexed property accessors, although
        /// property from base class can still be invoked using property syntax.
        /// </summary>
        [ClrOnlyFact]
        public void Hiding()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface I
    Property P(index As Integer) As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public MustInherit Class A0
    Implements I
    Property P(index As Integer) As Object Implements I.P
        Get
            Console.WriteLine(""A0.get_P({0})"", index)
            Return Nothing
        End Get
        Set(value As Object)
            Console.WriteLine(""A0.set_P({0}, ...)"", index)
        End Set
    End Property
End Class
Public MustInherit Class A1
    Implements I
    Property P(index As Integer) As Object Implements I.P
        Get
            Console.WriteLine(""A1.get_P({0})"", index)
            Return Nothing
        End Get
        Set(value As Object)
            Console.WriteLine(""A1.set_P({0}, ...)"", index)
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"using System;
class B1 : A1
{
    internal new object get_P(int index)
    {
        Console.WriteLine(""B1.get_P({0})"", index);
        return null;
    }
}
class B2 : A1
{
    internal new void set_P(int index, object value)
    {
        Console.WriteLine(""B2.set_P({0}, ...)"", index);
    }
}
class C
{
    static void Main()
    {
        var b1 = new B1();
        b1.set_P(1, b1.get_P(0));
        var b2 = new B2();
        b2.set_P(1, b2.get_P(0));
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"B1.get_P(0)
A1.set_P(1, ...)
A1.get_P(0)
B2.set_P(1, ...)
");
            var source3 =
@"class B0 : A0
{
    internal new object get_P(int index) { return null; }
    internal new void set_P(int index, object value) { }
}
class B1 : A1
{
    internal new object get_P(int index) { return null; }
    internal new void set_P(int index, object value) { }
}
class B2 : A1
{
    internal object get_P(int index) { return null; }
    internal void set_P(int index, object value) { }
}
class C
{
    static void Main()
    {
        var b0 = new B0();
        b0.set_P(1, b0.get_P(0));
        b0.P[1] = b0.P[0];
        var b1 = new B1();
        b1.set_P(1, b1.get_P(0));
        b1.P[1] = b1.P[0];
    }
}";
            var compilation3 = CreateCompilation(source3, new[] { reference1 });
            compilation3.VerifyDiagnostics(
                // (13,21): warning CS0108: 'B2.get_P(int)' hides inherited member 'A1.get_P(int)'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "get_P").WithArguments("B2.get_P(int)", "A1.get_P(int)").WithLocation(13, 21),
                // (14,19): warning CS0108: 'B2.set_P(int, object)' hides inherited member 'A1.set_P(int, object)'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "set_P").WithArguments("B2.set_P(int, object)", "A1.set_P(int, object)").WithLocation(14, 19),
                // (25,12): error CS1545: Property, indexer, or event 'A1.P[int]' is not supported by the language; try directly calling accessor methods 'A1.get_P(int)' or 'A1.set_P(int, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A1.P[int]", "A1.get_P(int)", "A1.set_P(int, object)").WithLocation(25, 12),
                // (25,22): error CS1545: Property, indexer, or event 'A1.P[int]' is not supported by the language; try directly calling accessor methods 'A1.get_P(int)' or 'A1.set_P(int, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A1.P[int]", "A1.get_P(int)", "A1.set_P(int, object)").WithLocation(25, 22));
        }

        [ClrOnlyFact]
        public void ReadOnlyWriteOnly()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    ReadOnly Property P(index As Object) As Object
    WriteOnly Property Q(index As Object) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void M(IA a)
    {
        a.P[null] = a.P[null];
        a.Q[null] = a.Q[null];
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (5,9): error CS0200: Property or indexer 'IA.P[object]' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "a.P[null]").WithArguments("IA.P[object]").WithLocation(5, 9),
                // (6,21): error CS0154: The property or indexer 'IA.Q[object]' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "a.Q[null]").WithArguments("IA.Q[object]").WithLocation(6, 21));
        }

        [ClrOnlyFact]
        public void ObjectInitializer()
        {
            var source1 =
@"Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Property P1(Optional index As Integer = 1) As Object
    ReadOnly Property P2(Optional index As Integer = 2) As IA
    ReadOnly Property P3(Optional index As Integer = 3) As List(Of Object)
End Interface
Public Class A
    Implements IA
    Property P1(Optional index As Integer = 1) As Object Implements IA.P1
        Get
            Console.WriteLine(""P1({0}).get"", index)
            Return Nothing
        End Get
        Set(value As Object)
            Console.WriteLine(""P1({0}).set"", index)
        End Set
    End Property
    ReadOnly Property P2(Optional index As Integer = 2) As IA Implements IA.P2
        Get
            Console.WriteLine(""P2({0}).get"", index)
            Return New A()
        End Get
    End Property
    ReadOnly Property P3(Optional index As Integer = 3) As List(Of Object) Implements IA.P3
        Get
            Console.WriteLine(""P3({0}).get"", index)
            Return New List(Of Object)()
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"class B
{
    static void Main()
    {
        IA a;
        a = new IA() { P1 = 4 };
        a = new IA() { P2 = { P1 = 5 } };
        a = new IA() { P3 = { 6, 7 } };
    }
}";
            var compilation2 = CompileAndVerify(source2, new[] { reference1 }, verify: Verification.Passes, expectedOutput:
@"P1(1).set
P2(2).get
P1(1).set
P3(3).get
P3(3).get
");
            compilation2.VerifyDiagnostics();
            compilation2.VerifyIL("B.Main",
@"{
  // Code size       87 (0x57)
  .maxstack  4
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  ldc.i4.4
  IL_0008:  box        ""int""
  IL_000d:  callvirt   ""void IA.P1[int].set""
  IL_0012:  pop
  IL_0013:  newobj     ""A..ctor()""
  IL_0018:  dup
  IL_0019:  ldc.i4.2
  IL_001a:  callvirt   ""IA IA.P2[int].get""
  IL_001f:  ldc.i4.1
  IL_0020:  ldc.i4.5
  IL_0021:  box        ""int""
  IL_0026:  callvirt   ""void IA.P1[int].set""
  IL_002b:  pop
  IL_002c:  newobj     ""A..ctor()""
  IL_0031:  dup
  IL_0032:  ldc.i4.3
  IL_0033:  callvirt   ""System.Collections.Generic.List<object> IA.P3[int].get""
  IL_0038:  ldc.i4.6
  IL_0039:  box        ""int""
  IL_003e:  callvirt   ""void System.Collections.Generic.List<object>.Add(object)""
  IL_0043:  dup
  IL_0044:  ldc.i4.3
  IL_0045:  callvirt   ""System.Collections.Generic.List<object> IA.P3[int].get""
  IL_004a:  ldc.i4.7
  IL_004b:  box        ""int""
  IL_0050:  callvirt   ""void System.Collections.Generic.List<object>.Add(object)""
  IL_0055:  pop
  IL_0056:  ret
}");
        }

        [ClrOnlyFact]
        public void ObjectInitializer_Errors()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
Public Structure S
    Public F As Object
End Structure
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class A
    ReadOnly Property P1(Optional x As Integer = 0, Optional y As Integer = 1) As A
        Get
            Return Nothing
        End Get
    End Property
    ReadOnly Property P2(Optional index As Integer = 0) As S
        Get
            Return Nothing
        End Get
    End Property
    Protected ReadOnly Property P3(Optional index As Integer = 0) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"class B
{
    static void Main()
    {
        A a;
        a = new A() { P1 = 2 };
        a = new A() { P2 = { F = 4 } };
        a = new A() { P3 = 5 };
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (6,23): error CS0200: Property or indexer 'A.P1[int, int]' cannot be assigned to -- it is read only
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P1").WithArguments("A.P1[int, int]").WithLocation(6, 23),
                // (7,23): error CS1918: Members of property 'A.P2[int]' of type 'S' cannot be assigned with an object initializer because it is of a value type
                Diagnostic(ErrorCode.ERR_ValueTypePropertyInObjectInitializer, "P2").WithArguments("A.P2[int]", "S").WithLocation(7, 23),
                // (8,23): error CS0122: 'A.P3[int]' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "P3").WithArguments("A.P3[int]").WithLocation(8, 23));
        }

        [ClrOnlyFact]
        public void Attributes()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(Optional index As Integer = 1) As Integer
End Interface
Public Class A1
    Inherits Attribute
    Implements IA
    Property P(Optional index As Integer = 1) As Integer Implements IA.P
        Get
            Return Nothing
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public Class A2
    Inherits Attribute
    Implements IA
    Property P(Optional index As Integer = 1) As Integer Implements IA.P
        Get
            Return Nothing
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Skipped);
            var source2 =
@"[A1(P = 1)] // Not ComImport
class B
{
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (1,5): error CS1545: Property, indexer, or event 'A1.P[int]' is not supported by the language; try directly calling accessor methods 'A1.get_P(int)' or 'A1.set_P(int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A1.P[int]", "A1.get_P(int)", "A1.set_P(int, int)").WithLocation(1, 5));
            var source3 =
@"[A2(P = 1)] // ComImport
class B
{
}";
            var compilation3 = CompileAndVerify(source3, new[] { reference1 });
        }

        [ClrOnlyFact]
        public void LinqMember()
        {
            var source1 =
@"Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IEnumerableOfA
    ReadOnly Property [Select](f As Func(Of IA, Object)) As IEnumerable(Of Object)
End Interface
Public Interface IA
    ReadOnly Property P As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<object> F(IEnumerableOfA arg)
    {
        return from o in arg select o.P;
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (6,30): error CS1955: Non-invocable member 'IEnumerableOfA.Select[System.Func<IA, object>]' cannot be used like a method.
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "select o.P").WithArguments("IEnumerableOfA.Select[System.Func<IA, object>]"));
        }

        [ClrOnlyFact]
        public void AmbiguityPropertyAndNonProperty()
        {
            var source1 =
@".class interface public abstract IA
{
  .class interface nested public abstract P { }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IB
    ReadOnly Property P(o As Object) As Object
End Interface
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public Interface IC
    Inherits IA, IB
End Interface";
            var reference2 = BasicCompilationUtils.CompileToMetadata(source2, references: new[] { MscorlibRef, reference1 });
            var source3 =
@"class C
{
    static object F(IC c)
    {
        return c.P[null];
    }
}";
            var compilation3 = CreateCompilation(source3, new[] { reference1, reference2 });
            compilation3.VerifyDiagnostics(
                // (5,18): error CS0229: Ambiguity between 'IA.P' and 'IB.P[object]'
                Diagnostic(ErrorCode.ERR_AmbigMember, "P").WithArguments("IA.P", "IB.P[object]").WithLocation(5, 18));
        }

        [ClrOnlyFact]
        public void LambdaWithIndexedProperty()
        {
            var source1 = @"
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Property P1(Optional index As Integer = 1) As Integer
    ReadOnly Property P2(Optional index As Integer = 2) As IA
    ReadOnly Property P3(Optional index As Integer = 3) As List(Of Object)
End Interface
Public Class A
    Implements IA
    Property P1(Optional index As Integer = 1) As Integer Implements IA.P1
        Get
            Console.WriteLine(""P1({0}).get"", index)
            Return index * 2
        End Get
        Set(value As Integer)
            Console.WriteLine(""P1({0}).set"", index)
        End Set
    End Property
    ReadOnly Property P2(Optional index As Integer = 2) As IA Implements IA.P2
        Get
            Console.WriteLine(""P2({0}).get"", index)
            Return New A()
        End Get
    End Property
    ReadOnly Property P3(Optional index As Integer = 3) As List(Of Object) Implements IA.P3
        Get
            Console.WriteLine(""P3({0}).get"", index)
            Return New List(Of Object)()
        End Get
    End Property
End Class
";

            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"
using System;
class B
{

    delegate int @del(int i);
    static void Main(string[] args)
    {
        del myDelegate = x =>
        {
            IA a;
            a = new IA() { P1 = 4 };
            a = new IA() { P2 = { P1 = 5 } };
            a = new IA() { P3 = { 6, 7 } };
            a.P1[x] = 2;
            return a.P1[x];
        };
        int j = myDelegate(5);
        Console.WriteLine(j);
    }
}
";
            var compilation2 = CompileAndVerify(source2, new[] { reference1 }, verify: Verification.Passes, expectedOutput:
@"P1(1).set
P2(2).get
P1(1).set
P3(3).get
P3(3).get
P1(5).set
P1(5).get
10
");

            compilation2.VerifyDiagnostics();

            /*
             * Intentionally not validating IL as it is just going to show the same generated code as 
             * many of the other tests.  the run results are far more  interesting
             */
        }

        [ClrOnlyFact]
        public void QueryStatementIndexedProperty()
        {
            var source1 = @"
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Property P1(Optional index As Integer = 1) As Integer
End Interface
Public Class A
    Implements IA
    Property P1(Optional index As Integer = 1) As Integer Implements IA.P1
        Get
            Console.WriteLine(""P1({0}).get"", index)
            Return index * 2
        End Get
        Set(value As Integer)
            Console.WriteLine(""P1({0}).set"", index)
        End Set
    End Property
End Class
";

            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"
using System;
using System.Linq;

class B
{
    static void Main(string[] args)
    {
        IA a;
        a = new IA();

        int[] arr = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        var query = from val in arr where val > a.P1[2] select val;

        foreach (var val in query) Console.WriteLine(val);
    }
}
";

            var compilation2 = CompileAndVerify(source2, new[] { reference1 }, verify: Verification.Passes, expectedOutput:
@"P1(2).get
P1(2).get
P1(2).get
P1(2).get
P1(2).get
P1(2).get
5
P1(2).get
6
P1(2).get
7
P1(2).get
8
P1(2).get
9
");

            compilation2.VerifyDiagnostics();

            /*
             * Intentionally not validating IL as it is just going to show the same generated code as 
             * many of the other tests.  the run results are far more  interesting
             */
        }

        [ClrOnlyFact]
        public void IncrementersAndIndexedProperties()
        {
            var source1 = @"
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Property P1(Optional index As Integer = 1) As Integer
End Interface
Public Class A
    Implements IA
    Property P1(Optional index As Integer = 1) As Integer Implements IA.P1
        Get
            Console.WriteLine(""P1({0}).get"", index)
            Return index * 2
        End Get
        Set(value As Integer)
            Console.WriteLine(""P1({0}).set"", index)
        End Set
    End Property
End Class
";

            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Passes);
            var source2 =
@"
using System;
class B
{

    static void Main(string[] args)
    {
        IA a;
        a = new IA();
        int ret = a.P1[3]++;
        Console.WriteLine(ret);
        
        a = new IA();
        ret = ++a.P1[4];
        Console.WriteLine(ret);
    }
}
";
            var compilation2 = CompileAndVerify(source2, new[] { reference1 }, verify: Verification.Passes, expectedOutput:
@"P1(3).get
P1(3).set
6
P1(4).get
P1(4).set
9
");

            compilation2.VerifyDiagnostics();
            compilation2.VerifyIL("B.Main",
@"{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.3
  IL_0007:  callvirt   ""int IA.P1[int].get""
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.3
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  add
  IL_0011:  callvirt   ""void IA.P1[int].set""
  IL_0016:  ldloc.0
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  newobj     ""A..ctor()""
  IL_0021:  dup
  IL_0022:  ldc.i4.4
  IL_0023:  callvirt   ""int IA.P1[int].get""
  IL_0028:  ldc.i4.1
  IL_0029:  add
  IL_002a:  stloc.0
  IL_002b:  ldc.i4.4
  IL_002c:  ldloc.0
  IL_002d:  callvirt   ""void IA.P1[int].set""
  IL_0032:  ldloc.0
  IL_0033:  call       ""void System.Console.WriteLine(int)""
  IL_0038:  ret
}");
        }

        [WorkItem(546441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546441")]
        [Fact]
        public void UnimplementedIndexedProperty()
        {
            // From Microsoft.Vbe.Interop, Version=14.0.0.0.
            var il = @"
.class public auto ansi sealed Microsoft.Vbe.Interop.vbext_ProcKind
       extends [mscorlib]System.Enum
{
	.field public specialname rtspecialname int32 value__
	.field public static literal valuetype Microsoft.Vbe.Interop.vbext_ProcKind vbext_pk_Get = int32(0x00000003)
} // end of class Microsoft.Vbe.Interop.vbext_ProcKind


.class interface public abstract auto ansi import Microsoft.Vbe.Interop._CodeModule
{
	.method public hidebysig newslot specialname abstract virtual 
			instance string  marshal( bstr)  get_ProcOfLine([in] int32 Line,
															[out] valuetype Microsoft.Vbe.Interop.vbext_ProcKind& ProcKind) runtime managed internalcall
	{
	  .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 02 60 00 00 )                         // .....`..
	}

	.property string ProcOfLine(int32,
								valuetype Microsoft.Vbe.Interop.vbext_ProcKind&)
	{
	  .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 02 60 00 00 )                         // .....`..
	  .get instance string Microsoft.Vbe.Interop._CodeModule::get_ProcOfLine(int32,
																			 valuetype Microsoft.Vbe.Interop.vbext_ProcKind&)
	}
} // end of class Microsoft.Vbe.Interop._CodeModule

.class interface public abstract auto ansi import Microsoft.Vbe.Interop.CodeModule
       implements Microsoft.Vbe.Interop._CodeModule
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 25 4D 69 63 72 6F 73 6F 66 74 2E 56 62 65   // ..%Microsoft.Vbe
                                                                                                                          2E 49 6E 74 65 72 6F 70 2E 43 6F 64 65 4D 6F 64   // .Interop.CodeMod
                                                                                                                          75 6C 65 43 6C 61 73 73 00 00 )                   // uleClass..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 45 31 36 45 2D 30 30 30 30   // ..$0002E16E-0000
                                                                                                  2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30   // -0000-C000-00000
                                                                                                  30 30 30 30 30 34 36 00 00 )                      // 0000046..
} // end of class Microsoft.Vbe.Interop.CodeModule
";

            var source = @"
using Microsoft.Vbe.Interop;

class C : CodeModule
{
}

class D : CodeModule
{
    public string get_ProcOfLine(int line, out Microsoft.Vbe.Interop.vbext_ProcKind procKind) { throw null; }
}
";
            var comp = CreateCompilationWithILAndMscorlib40(source, il, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (4,7): error CS0535: 'C' does not implement interface member 'Microsoft.Vbe.Interop._CodeModule.ProcOfLine[int, out Microsoft.Vbe.Interop.vbext_ProcKind].get'
                // class C : CodeModule
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "CodeModule").WithArguments("C", "Microsoft.Vbe.Interop._CodeModule.ProcOfLine[int, out Microsoft.Vbe.Interop.vbext_ProcKind].get"));

            var interfaceProperty = comp.GlobalNamespace
                .GetMember<NamespaceSymbol>("Microsoft")
                .GetMember<NamespaceSymbol>("Vbe")
                .GetMember<NamespaceSymbol>("Interop")
                .GetMember<NamedTypeSymbol>("_CodeModule")
                .GetMember<PropertySymbol>("ProcOfLine");
            Assert.True(interfaceProperty.IsIndexedProperty);

            var sourceType1 = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.Null(sourceType1.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Null(sourceType1.FindImplementationForInterfaceMember(interfaceProperty.GetMethod));

            var sourceType2 = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("D");
            Assert.Null(sourceType2.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.NotNull(sourceType2.FindImplementationForInterfaceMember(interfaceProperty.GetMethod));
        }

        [WorkItem(530571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530571")]
        [Fact(Skip = "530571")]
        public void GetAccessorMethodBug16439()
        {
            var il = @"
.class interface public abstract import InterfaceA
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 01 41 00 00 )
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 36 35 46 37 35 32 44 2D 45 39 43 34 2D 34 46 37 45 2D 42 30 44 30 2D 43 44 46 44 37 41 33 36 45 32 31 31 00 00 )
  .property instance int32 P1(int32)
  {
    .get instance int32 InterfaceA::get_P1(int32)
    .set instance void InterfaceA::set_P1(int32, int32)
  }
  .method public abstract virtual instance int32 get_P1(int32 i) { }
  .method public abstract virtual instance void set_P1(int32 i, int32 v) { }
}

.class public A implements InterfaceA
{
  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .property instance int32 P1(int32)
  {
    .get instance int32 InterfaceA::get_P1(int32)
    .set instance void InterfaceA::set_P1(int32, int32)
  }
  .method public virtual instance int32 get_P1(int32 i)
  {
    ldc.i4.1
    call       void [mscorlib]System.Console::WriteLine(int32)
    ldc.i4.0
    ret
  }
  .method public virtual instance void set_P1(int32 i, int32 v)
  {
    ldc.i4.2
    call       void [mscorlib]System.Console::WriteLine(int32)
    ret
  }
}
";

            var source = @"
class Test
{
   public static void Main()
   {
     InterfaceA ia = new A();
     System.Console.WriteLine(ia.P1[10]);
   }
}
";
            string expectedOutput = @"1
0";
            var compilation = CreateCompilationWithILAndMscorlib40(source, il, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [WorkItem(846234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846234")]
        [ClrOnlyFact]
        public void IndexedPropertyColorColor()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(index As Integer) As Object
End Interface
Public Class A
    Public Shared Function Create() As IA
        Return New AImpl()
    End Function
    Private NotInheritable Class AImpl
        Implements IA
        Property P(index As Integer) As Object Implements IA.P
            Get
                Console.WriteLine(""P[{0}]"", index)
                Return index * 2
            End Get
            Set(value As Object)
                Console.WriteLine(""P[{0}] = {1}"", index, value)
            End Set
        End Property
    End Class
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void Main()
    {
        IA IA = A.Create();
        var o = IA.P[1];
        IA.P[2] = o;
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"P[1]
P[2] = 2
");
            compilation2.VerifyIL("B.Main()",
@"{
  // Code size       21 (0x15)
  .maxstack  3
  .locals init (object V_0) //o
  IL_0000:  call       ""IA A.Create()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""object IA.P[int].get""
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.2
  IL_000e:  ldloc.0
  IL_000f:  callvirt   ""void IA.P[int].set""
  IL_0014:  ret
}");
        }

        [WorkItem(853401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853401")]
        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void IndexedPropertyDynamicInvocation()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(index As Integer) As Object
    Property P(index As String) As Object
End Interface
Public Class A
    Public Shared Function Create() As IA
        Return New AImpl()
    End Function
    Private NotInheritable Class AImpl
        Implements IA
        Property P(index As Integer) As Object Implements IA.P
            Get
                Console.WriteLine(""P[{0}]"", index)
                Return index * 2
            End Get
            Set(value As Object)
                Console.WriteLine(""P[{0}] = {1}"", index, value)
            End Set
        End Property
        Property P(index As String) As Object Implements IA.P
            Get
                Console.WriteLine(""P[{0}]"", index)
                Return index + ""2""
            End Get
            Set(value As Object)
                Console.WriteLine(""P[{0}] = {1}"", index, value)
            End Set
        End Property
    End Class
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void Main()
    {
        dynamic d = 1;
        IA i = A.Create();
        var o = i.P[d];
        i.P[d] = o;
    }
}";

            var compilation2 = CreateCompilationWithMscorlib40AndSystemCore(source2, new[] { reference1, CSharpRef }, TestOptions.ReleaseExe);
            CompileAndVerifyException<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(compilation2); // As in dev11.
        }

        [WorkItem(846234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846234")]
        [WorkItem(853401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853401")]
        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        public void IndexedPropertyDynamicColorColorInvocation()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(index As Integer) As Object
    Property P(index As String) As Object
End Interface
Public Class A
    Public Shared Function Create() As IA
        Return New AImpl()
    End Function
    Private NotInheritable Class AImpl
        Implements IA
        Property P(index As Integer) As Object Implements IA.P
            Get
                Console.WriteLine(""P[{0}]"", index)
                Return index * 2
            End Get
            Set(value As Object)
                Console.WriteLine(""P[{0}] = {1}"", index, value)
            End Set
        End Property
        Property P(index As String) As Object Implements IA.P
            Get
                Console.WriteLine(""P[{0}]"", index)
                Return index + ""2""
            End Get
            Set(value As Object)
                Console.WriteLine(""P[{0}] = {1}"", index, value)
            End Set
        End Property
    End Class
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void Main()
    {
        dynamic d = 1;
        IA IA = A.Create();
        var o = IA.P[d];
        IA.P[d] = o;
    }
}";

            var compilation2 = CreateCompilationWithMscorlib40AndSystemCore(source2, new[] { reference1, CSharpRef }, TestOptions.ReleaseExe);
            compilation2.VerifyEmitDiagnostics(); // Used to assert.

            CompileAndVerifyException<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(compilation2); // As in dev11.
        }
    }
}
