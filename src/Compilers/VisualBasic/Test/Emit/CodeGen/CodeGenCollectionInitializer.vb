' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenCollectionInitializer
        Inherits BasicTestBase

        <Fact()>
        Public Sub CollectionInitializerAsRefTypeEqualsNew()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim x As List(Of String) = New List(Of String)() From {"Hello ", "World!"}
        Console.Write(x.Item(0))
        Console.Write(x.Item(1))
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Hello World!
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  3
  IL_0000:  newobj     "Sub System.Collections.Generic.List(Of String)..ctor()"
  IL_0005:  dup
  IL_0006:  ldstr      "Hello "
  IL_000b:  callvirt   "Sub System.Collections.Generic.List(Of String).Add(String)"
  IL_0010:  dup
  IL_0011:  ldstr      "World!"
  IL_0016:  callvirt   "Sub System.Collections.Generic.List(Of String).Add(String)"
  IL_001b:  dup
  IL_001c:  ldc.i4.0
  IL_001d:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_0022:  call       "Sub System.Console.Write(String)"
  IL_0027:  ldc.i4.1
  IL_0028:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_002d:  call       "Sub System.Console.Write(String)"
  IL_0032:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerAsNewRefType()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim x As New List(Of String)() From {"Hello ", "World!"}
        Console.Write(x.Item(0))
        Console.Write(x.Item(1))
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Hello World!
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  3
  IL_0000:  newobj     "Sub System.Collections.Generic.List(Of String)..ctor()"
  IL_0005:  dup
  IL_0006:  ldstr      "Hello "
  IL_000b:  callvirt   "Sub System.Collections.Generic.List(Of String).Add(String)"
  IL_0010:  dup
  IL_0011:  ldstr      "World!"
  IL_0016:  callvirt   "Sub System.Collections.Generic.List(Of String).Add(String)"
  IL_001b:  dup
  IL_001c:  ldc.i4.0
  IL_001d:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_0022:  call       "Sub System.Console.Write(String)"
  IL_0027:  ldc.i4.1
  IL_0028:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_002d:  call       "Sub System.Console.Write(String)"
  IL_0032:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerAsValueTypeEqualsNewNoParamConstructor()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Structure Custom
    Public shared list As New List(Of String)()

    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(p As String)
        list.Add(p)
    End Sub

    Public Structure CustomEnumerator
        Private list As list(Of String)
        Private shared index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If index &lt; Me.list.Count - 1 Then
                index = index + 1
            Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Structure
End Structure

        Class C1
            Public Shared Sub Main()
                Dim x As Custom = New Custom() From {"Hello ", "World!"}
                Console.Write(Custom.list.Item(0))
                Console.Write(Custom.list.Item(1))
            End Sub
        End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World!
]]>).VerifyIL("C1.Main",
            <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  2
  .locals init (Custom V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Custom"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldstr      "Hello "
  IL_000f:  call       "Sub Custom.add(String)"
  IL_0014:  ldloca.s   V_0
  IL_0016:  ldstr      "World!"
  IL_001b:  call       "Sub Custom.add(String)"
  IL_0020:  ldsfld     "Custom.list As System.Collections.Generic.List(Of String)"
  IL_0025:  ldc.i4.0
  IL_0026:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_002b:  call       "Sub System.Console.Write(String)"
  IL_0030:  ldsfld     "Custom.list As System.Collections.Generic.List(Of String)"
  IL_0035:  ldc.i4.1
  IL_0036:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_003b:  call       "Sub System.Console.Write(String)"
  IL_0040:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerAsValueTypeEqualsNewOneParamConstructor()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Structure Custom
    Public readonly list As List(Of String)

    Public Sub New(list as List(Of String))
        me.list = list
    End Sub


    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(p As String)
        list.Add(p)
    End Sub

    Public Structure CustomEnumerator
        Private list As list(Of String)
        Private shared index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If index &lt; Me.list.Count - 1 Then
                index = index + 1
            Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Structure
End Structure

        Class C1
            Public Shared Sub Main()
                Dim x As Custom = New Custom(new List(of string)()) From {"Hello ", "World!"}
                Console.Write(x.list.Item(0))
                Console.Write(x.list.Item(1))
            End Sub
        End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World!
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       71 (0x47)
  .maxstack  3
  .locals init (Custom V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  newobj     "Sub System.Collections.Generic.List(Of String)..ctor()"
  IL_0007:  call       "Sub Custom..ctor(System.Collections.Generic.List(Of String))"
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldstr      "Hello "
  IL_0013:  call       "Sub Custom.add(String)"
  IL_0018:  ldloca.s   V_0
  IL_001a:  ldstr      "World!"
  IL_001f:  call       "Sub Custom.add(String)"
  IL_0024:  ldloc.0
  IL_0025:  dup
  IL_0026:  ldfld      "Custom.list As System.Collections.Generic.List(Of String)"
  IL_002b:  ldc.i4.0
  IL_002c:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_0031:  call       "Sub System.Console.Write(String)"
  IL_0036:  ldfld      "Custom.list As System.Collections.Generic.List(Of String)"
  IL_003b:  ldc.i4.1
  IL_003c:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_0041:  call       "Sub System.Console.Write(String)"
  IL_0046:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerAsNewValueTypeOneParamConstructor()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Structure Custom
    Public readonly list As List(Of String)

    Public Sub New(list as List(Of String))
        me.list = list
    End Sub

    Public Function GetEnumerator() As CustomEnumerator
        Return New CustomEnumerator(list)
    End Function

    Public Sub add(p As String)
        list.Add(p)
    End Sub

    Public Structure CustomEnumerator
        Private list As list(Of String)
        Private shared index As Integer = -1

        Public Sub New(list As List(Of String))
            Me.list = list
        End Sub

        Public Function MoveNext() As Boolean
            If index &lt; Me.list.Count - 1 Then
                index = index + 1
            Return True
            End If

            Return False
        End function

        Public ReadOnly Property Current As String
            Get
                Return Me.list(index)
            End Get
        End Property
    End Structure
End Structure

        Class C1
            Public Shared Sub Main()
                Dim x As New Custom(new List(of string)()) From {"Hello ", "World!"}
                Console.Write(x.list.Item(0))
                Console.Write(x.list.Item(1))
            End Sub
        End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World!
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       71 (0x47)
  .maxstack  3
  .locals init (Custom V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  newobj     "Sub System.Collections.Generic.List(Of String)..ctor()"
  IL_0007:  call       "Sub Custom..ctor(System.Collections.Generic.List(Of String))"
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldstr      "Hello "
  IL_0013:  call       "Sub Custom.add(String)"
  IL_0018:  ldloca.s   V_0
  IL_001a:  ldstr      "World!"
  IL_001f:  call       "Sub Custom.add(String)"
  IL_0024:  ldloc.0
  IL_0025:  dup
  IL_0026:  ldfld      "Custom.list As System.Collections.Generic.List(Of String)"
  IL_002b:  ldc.i4.0
  IL_002c:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_0031:  call       "Sub System.Console.Write(String)"
  IL_0036:  ldfld      "Custom.list As System.Collections.Generic.List(Of String)"
  IL_003b:  ldc.i4.1
  IL_003c:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_0041:  call       "Sub System.Console.Write(String)"
  IL_0046:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerForTypeParameter()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Interface IAdd(Of T)
    Sub Add(p As T)
End Interface

Class C1
    Implements IAdd(Of String), ICollection

    private mylist as new list(of String)()

    Public Sub New()
    End Sub

    Public Sub Add1(p As String) Implements IAdd(Of String).Add
        mylist.add(p)
    End Sub

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Return False
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return mylist.getenumerator
    End Function
End Class

Module Program
    Public Sub DoStuff(Of T As {IAdd(Of String), ICollection, New})()
        Dim a As New T() From {"Hello", " ", "World!"}

        for each str as string in a
            Console.Write(str)
        next str
    End Sub

    Public Sub Main()
        DoStuff(Of C1)()
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
Hello World!
]]>).VerifyIL("Program.DoStuff", <![CDATA[
{
  // Code size      125 (0x7d)
  .maxstack  2
  .locals init (T V_0, //a
  T V_1,
  System.Collections.IEnumerator V_2)
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  ldstr      "Hello"
  IL_000d:  constrained. "T"
  IL_0013:  callvirt   "Sub IAdd(Of String).Add(String)"
  IL_0018:  ldloca.s   V_1
  IL_001a:  ldstr      " "
  IL_001f:  constrained. "T"
  IL_0025:  callvirt   "Sub IAdd(Of String).Add(String)"
  IL_002a:  ldloca.s   V_1
  IL_002c:  ldstr      "World!"
  IL_0031:  constrained. "T"
  IL_0037:  callvirt   "Sub IAdd(Of String).Add(String)"
  IL_003c:  ldloc.1
  IL_003d:  stloc.0
  .try
{
  IL_003e:  ldloca.s   V_0
  IL_0040:  constrained. "T"
  IL_0046:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_004b:  stloc.2
  IL_004c:  br.s       IL_005e
  IL_004e:  ldloc.2
  IL_004f:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_0054:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_0059:  call       "Sub System.Console.Write(String)"
  IL_005e:  ldloc.2
  IL_005f:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0064:  brtrue.s   IL_004e
  IL_0066:  leave.s    IL_007c
}
  finally
{
  IL_0068:  ldloc.2
  IL_0069:  isinst     "System.IDisposable"
  IL_006e:  brfalse.s  IL_007b
  IL_0070:  ldloc.2
  IL_0071:  isinst     "System.IDisposable"
  IL_0076:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_007b:  endfinally
}
  IL_007c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerAsRefTypeEqualsNewNested()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim x As List(Of List(Of String)) = New List(Of List(of String))() From {New List(Of String)() From {"Hello", " "}, New List(Of String)() From {"World!"}}
        Console.Write(x.Item(0).Item(0))
        Console.Write(x.Item(0).Item(1))        
        Console.Write(x.Item(1).Item(0))
    End Sub
End Class       
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Hello World!
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size      114 (0x72)
  .maxstack  5
  IL_0000:  newobj     "Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of String))..ctor()"
  IL_0005:  dup
  IL_0006:  newobj     "Sub System.Collections.Generic.List(Of String)..ctor()"
  IL_000b:  dup
  IL_000c:  ldstr      "Hello"
  IL_0011:  callvirt   "Sub System.Collections.Generic.List(Of String).Add(String)"
  IL_0016:  dup
  IL_0017:  ldstr      " "
  IL_001c:  callvirt   "Sub System.Collections.Generic.List(Of String).Add(String)"
  IL_0021:  callvirt   "Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of String)).Add(System.Collections.Generic.List(Of String))"
  IL_0026:  dup
  IL_0027:  newobj     "Sub System.Collections.Generic.List(Of String)..ctor()"
  IL_002c:  dup
  IL_002d:  ldstr      "World!"
  IL_0032:  callvirt   "Sub System.Collections.Generic.List(Of String).Add(String)"
  IL_0037:  callvirt   "Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of String)).Add(System.Collections.Generic.List(Of String))"
  IL_003c:  dup
  IL_003d:  ldc.i4.0
  IL_003e:  callvirt   "Function System.Collections.Generic.List(Of System.Collections.Generic.List(Of String)).get_Item(Integer) As System.Collections.Generic.List(Of String)"
  IL_0043:  ldc.i4.0
  IL_0044:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_0049:  call       "Sub System.Console.Write(String)"
  IL_004e:  dup
  IL_004f:  ldc.i4.0
  IL_0050:  callvirt   "Function System.Collections.Generic.List(Of System.Collections.Generic.List(Of String)).get_Item(Integer) As System.Collections.Generic.List(Of String)"
  IL_0055:  ldc.i4.1
  IL_0056:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_005b:  call       "Sub System.Console.Write(String)"
  IL_0060:  ldc.i4.1
  IL_0061:  callvirt   "Function System.Collections.Generic.List(Of System.Collections.Generic.List(Of String)).get_Item(Integer) As System.Collections.Generic.List(Of String)"
  IL_0066:  ldc.i4.0
  IL_0067:  callvirt   "Function System.Collections.Generic.List(Of String).get_Item(Integer) As String"
  IL_006c:  call       "Sub System.Console.Write(String)"
  IL_0071:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerWithLambdasAndLifting()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim hello as String = "Hello "
        Const world as String = "World!"
    
        Dim x As List(Of Action) = new List(Of Action) From {
          Sub() 
            Console.Write(hello)
          End Sub,
          Sub() Console.Write(world)
        }
        
        x.Item(0).Invoke()
        x.Item(1).Invoke()        
    End Sub
End Class         
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Hello World!
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size      106 (0x6a)
  .maxstack  4
  .locals init (C1._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub C1._Closure$__1-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      "Hello "
  IL_000c:  stfld      "C1._Closure$__1-0.$VB$Local_hello As String"
  IL_0011:  newobj     "Sub System.Collections.Generic.List(Of System.Action)..ctor()"
  IL_0016:  dup
  IL_0017:  ldloc.0
  IL_0018:  ldftn      "Sub C1._Closure$__1-0._Lambda$__0()"
  IL_001e:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0023:  callvirt   "Sub System.Collections.Generic.List(Of System.Action).Add(System.Action)"
  IL_0028:  dup
  IL_0029:  ldsfld     "C1._Closure$__.$I1-1 As System.Action"
  IL_002e:  brfalse.s  IL_0037
  IL_0030:  ldsfld     "C1._Closure$__.$I1-1 As System.Action"
  IL_0035:  br.s       IL_004d
  IL_0037:  ldsfld     "C1._Closure$__.$I As C1._Closure$__"
  IL_003c:  ldftn      "Sub C1._Closure$__._Lambda$__1-1()"
  IL_0042:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0047:  dup
  IL_0048:  stsfld     "C1._Closure$__.$I1-1 As System.Action"
  IL_004d:  callvirt   "Sub System.Collections.Generic.List(Of System.Action).Add(System.Action)"
  IL_0052:  dup
  IL_0053:  ldc.i4.0
  IL_0054:  callvirt   "Function System.Collections.Generic.List(Of System.Action).get_Item(Integer) As System.Action"
  IL_0059:  callvirt   "Sub System.Action.Invoke()"
  IL_005e:  ldc.i4.1
  IL_005f:  callvirt   "Function System.Collections.Generic.List(Of System.Action).get_Item(Integer) As System.Action"
  IL_0064:  callvirt   "Sub System.Action.Invoke()"
  IL_0069:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerWithLambdasAndLifting_2()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
   
        Dim x As List(Of Action) = new List(Of Action) From {
          Sub() 
            Console.Write(if(x is nothing, "Nothing", x.Count.ToString()))
          End Sub
        }
        
        x.Item(0).Invoke()
    End Sub
End Class         
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
1
]]>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  5
  .locals init (C1._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub C1._Closure$__1-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     "Sub System.Collections.Generic.List(Of System.Action)..ctor()"
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldftn      "Sub C1._Closure$__1-0._Lambda$__0()"
  IL_0014:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0019:  callvirt   "Sub System.Collections.Generic.List(Of System.Action).Add(System.Action)"
  IL_001e:  stfld      "C1._Closure$__1-0.$VB$Local_x As System.Collections.Generic.List(Of System.Action)"
  IL_0023:  ldloc.0
  IL_0024:  ldfld      "C1._Closure$__1-0.$VB$Local_x As System.Collections.Generic.List(Of System.Action)"
  IL_0029:  ldc.i4.0
  IL_002a:  callvirt   "Function System.Collections.Generic.List(Of System.Action).get_Item(Integer) As System.Action"
  IL_002f:  callvirt   "Sub System.Action.Invoke()"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerWithByRefExtensionMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module Program
    Sub Main(args As String())
        Dim y = New LinkedList(Of Integer)() From {1, 2, 3}

        Console.WriteLine(y.Count)
        Console.WriteLine(y.First.Value)
    End Sub

    &lt;Extension()&gt;
    Public Sub Add(ByRef this As LinkedList(Of Integer), p As Integer)
        this = New LinkedList(Of Integer)()
        this.AddFirst(23)
    End Sub
End Module

Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
1
23
]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (System.Collections.Generic.LinkedList(Of Integer) V_0)
  IL_0000:  newobj     "Sub System.Collections.Generic.LinkedList(Of Integer)..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.1
  IL_0009:  call       "Sub Program.Add(ByRef System.Collections.Generic.LinkedList(Of Integer), Integer)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.2
  IL_0011:  call       "Sub Program.Add(ByRef System.Collections.Generic.LinkedList(Of Integer), Integer)"
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldc.i4.3
  IL_0019:  call       "Sub Program.Add(ByRef System.Collections.Generic.LinkedList(Of Integer), Integer)"
  IL_001e:  ldloc.0
  IL_001f:  dup
  IL_0020:  callvirt   "Function System.Collections.Generic.LinkedList(Of Integer).get_Count() As Integer"
  IL_0025:  call       "Sub System.Console.WriteLine(Integer)"
  IL_002a:  callvirt   "Function System.Collections.Generic.LinkedList(Of Integer).get_First() As System.Collections.Generic.LinkedListNode(Of Integer)"
  IL_002f:  callvirt   "Function System.Collections.Generic.LinkedListNode(Of Integer).get_Value() As Integer"
  IL_0034:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0039:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerPropertyInitializer()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C2
    Public Shared Property MyProperty1 As New List(Of Integer) From {1}
    Public Shared Property MyProperty2 As List(Of Integer) = New List(Of Integer) From {2}

    Public Shared Sub Main()
        Console.WriteLine(MyProperty1.Item(0))
        Console.WriteLine(MyProperty2.Item(0))
    End Sub
End Class          
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
1
2
]]>).VerifyIL("C2..cctor", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  newobj     "Sub System.Collections.Generic.List(Of Integer)..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_000c:  call       "Sub C2.set_MyProperty1(System.Collections.Generic.List(Of Integer))"
  IL_0011:  newobj     "Sub System.Collections.Generic.List(Of Integer)..ctor()"
  IL_0016:  dup
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_001d:  call       "Sub C2.set_MyProperty2(System.Collections.Generic.List(Of Integer))"
  IL_0022:  ret
}
]]>)
        End Sub

        <WorkItem(544125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544125")>
        <Fact()>
        Public Sub CollectionInitializerFieldInitializer()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Class C2
    Public Shared MyField1 As New List(Of Integer) From {1}
    Public Shared MyField2, MyField3 As New List(Of Integer) From {2}
    Public Shared MyField4 As List(Of Integer) = New List(Of Integer) From {3}

    Public Shared Sub Main()
        Console.WriteLine(MyField1.Item(0))
        Console.WriteLine(MyField2.Item(0))
        Console.WriteLine(MyField3.Item(0))
        Console.WriteLine(MyField4.Item(0))
    End Sub
End Class    
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
1
2
2
3
]]>).VerifyIL("C2..cctor", <![CDATA[
{
  // Code size       69 (0x45)
  .maxstack  3
  IL_0000:  newobj     "Sub System.Collections.Generic.List(Of Integer)..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_000c:  stsfld     "C2.MyField1 As System.Collections.Generic.List(Of Integer)"
  IL_0011:  newobj     "Sub System.Collections.Generic.List(Of Integer)..ctor()"
  IL_0016:  dup
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_001d:  stsfld     "C2.MyField2 As System.Collections.Generic.List(Of Integer)"
  IL_0022:  newobj     "Sub System.Collections.Generic.List(Of Integer)..ctor()"
  IL_0027:  dup
  IL_0028:  ldc.i4.2
  IL_0029:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_002e:  stsfld     "C2.MyField3 As System.Collections.Generic.List(Of Integer)"
  IL_0033:  newobj     "Sub System.Collections.Generic.List(Of Integer)..ctor()"
  IL_0038:  dup
  IL_0039:  ldc.i4.3
  IL_003a:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_003f:  stsfld     "C2.MyField4 As System.Collections.Generic.List(Of Integer)"
  IL_0044:  ret
}
]]>)
        End Sub
    End Class
End Namespace

