' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBForEachTests
        Inherits BasicTestBase

#Region "For Each Loop"

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForEachLookAtTheStartOfMethodBody()
            Dim source =
<compilation>
    <file>
Class C
    Private F As Object
    Shared Function M(c As Object()) As Boolean
        For Each o in c
            If o IsNot Nothing Then Return True
        Next
        Return False
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="M" parameterNames="c">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="6" offset="0"/>
                    <slot kind="8" offset="0"/>
                    <slot kind="0" offset="0"/>
                    <slot kind="1" offset="29"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="48" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="9" endLine="4" endColumn="24" document="1"/>
                <entry offset="0x5" hidden="true" document="1"/>
                <entry offset="0x10" startLine="5" startColumn="13" endLine="5" endColumn="36" document="1"/>
                <entry offset="0x16" hidden="true" document="1"/>
                <entry offset="0x1a" startLine="5" startColumn="37" endLine="5" endColumn="48" document="1"/>
                <entry offset="0x1e" startLine="6" startColumn="9" endLine="6" endColumn="13" document="1"/>
                <entry offset="0x1f" hidden="true" document="1"/>
                <entry offset="0x23" hidden="true" document="1"/>
                <entry offset="0x2b" hidden="true" document="1"/>
                <entry offset="0x2f" startLine="7" startColumn="9" endLine="7" endColumn="21" document="1"/>
                <entry offset="0x33" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x35">
                <currentnamespace name=""/>
                <local name="M" il_index="0" il_start="0x0" il_end="0x35" attributes="0"/>
                <scope startOffset="0x7" endOffset="0x22">
                    <local name="o" il_index="3" il_start="0x7" il_end="0x22" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForEachOverOneDimensionalArray()
            Dim source =
<compilation>
    <file>
Option Strict On

Imports System

        Class C1
            Public Shared Sub Main()
                Dim arr As Integer() = New Integer(1) {}
                arr(0) = 23
                arr(1) = 42

                For Each element As Integer In arr
                    Console.WriteLine(element)
                Next
            End Sub
        End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="6" offset="118"/>
                    <slot kind="8" offset="118"/>
                    <slot kind="0" offset="127"/>
                    <slot kind="1" offset="118"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="13" endLine="6" endColumn="37" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="21" endLine="7" endColumn="57" document="1"/>
                <entry offset="0x8" startLine="8" startColumn="17" endLine="8" endColumn="28" document="1"/>
                <entry offset="0xd" startLine="9" startColumn="17" endLine="9" endColumn="28" document="1"/>
                <entry offset="0x12" startLine="11" startColumn="17" endLine="11" endColumn="51" document="1"/>
                <entry offset="0x16" hidden="true" document="1"/>
                <entry offset="0x1c" startLine="12" startColumn="21" endLine="12" endColumn="47" document="1"/>
                <entry offset="0x23" startLine="13" startColumn="17" endLine="13" endColumn="21" document="1"/>
                <entry offset="0x24" hidden="true" document="1"/>
                <entry offset="0x28" hidden="true" document="1"/>
                <entry offset="0x30" hidden="true" document="1"/>
                <entry offset="0x34" startLine="14" startColumn="13" endLine="14" endColumn="20" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x35">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="arr" il_index="0" il_start="0x0" il_end="0x35" attributes="0"/>
                <scope startOffset="0x18" endOffset="0x27">
                    <local name="element" il_index="3" il_start="0x18" il_end="0x27" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForEachOverString()
            Dim source =
<compilation>
    <file>
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim str As String = "Hello"

        For Each element As Char In str
            Console.WriteLine(element)
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="6" offset="39"/>
                    <slot kind="8" offset="39"/>
                    <slot kind="0" offset="48"/>
                    <slot kind="1" offset="39"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="13" endLine="7" endColumn="36" document="1"/>
                <entry offset="0x7" startLine="9" startColumn="9" endLine="9" endColumn="40" document="1"/>
                <entry offset="0xb" hidden="true" document="1"/>
                <entry offset="0x15" startLine="10" startColumn="13" endLine="10" endColumn="39" document="1"/>
                <entry offset="0x1c" startLine="11" startColumn="9" endLine="11" endColumn="13" document="1"/>
                <entry offset="0x1d" hidden="true" document="1"/>
                <entry offset="0x21" hidden="true" document="1"/>
                <entry offset="0x2c" hidden="true" document="1"/>
                <entry offset="0x30" startLine="12" startColumn="5" endLine="12" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x31">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x31" attributes="0"/>
                <scope startOffset="0xd" endOffset="0x20">
                    <local name="element" il_index="3" il_start="0xd" il_end="0x20" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForEachIEnumerableWithNoTryCatch()
            Dim source =
<compilation>
    <file>
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class
Structure Enumerator
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
    Public Sub Dispose()
    End Sub
End Structure
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="C" methodName="Main"/>
    <methods>
        <method containingType="C" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="5" offset="0"/>
                    <slot kind="0" offset="0"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="39" document="1"/>
                <entry offset="0xc" hidden="true" document="1"/>
                <entry offset="0x16" startLine="6" startColumn="13" endLine="6" endColumn="40" document="1"/>
                <entry offset="0x1d" startLine="7" startColumn="9" endLine="7" endColumn="13" document="1"/>
                <entry offset="0x1e" hidden="true" document="1"/>
                <entry offset="0x26" hidden="true" document="1"/>
                <entry offset="0x29" startLine="8" startColumn="5" endLine="8" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2a">
                <currentnamespace name=""/>
                <scope startOffset="0xe" endOffset="0x1d">
                    <local name="x" il_index="1" il_start="0xe" il_end="0x1d" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForEachIEnumerableWithTryCatchImplementIDisposable()
            Dim source =
<compilation>
    <file>
Option Infer On

Imports System.Collections.Generic
Imports System

Class C
    Public Shared Sub Main()
        For Each j In New Gen(Of Integer)(12, 42, 23)
            console.writeline(j)
        Next
    End Sub
End Class

Public Class Gen(Of T As New)
    Dim list As New List(Of T)

    Public Sub New(ParamArray elem() As T)
        For Each el In elem
            list.add(el)
        Next
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of T)
        Return list.GetEnumerator
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="C" methodName="Main"/>
    <methods>
        <method containingType="C" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="5" offset="0"/>
                    <slot kind="0" offset="0"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="7" startColumn="5" endLine="7" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="8" startColumn="9" endLine="8" endColumn="54" document="1"/>
                <entry offset="0x1d" hidden="true" document="1"/>
                <entry offset="0x26" startLine="9" startColumn="13" endLine="9" endColumn="33" document="1"/>
                <entry offset="0x2d" startLine="10" startColumn="9" endLine="10" endColumn="13" document="1"/>
                <entry offset="0x2e" hidden="true" document="1"/>
                <entry offset="0x35" hidden="true" document="1"/>
                <entry offset="0x3a" hidden="true" document="1"/>
                <entry offset="0x45" startLine="11" startColumn="5" endLine="11" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0x1f" endOffset="0x2d">
                    <local name="j" il_index="1" il_start="0x1f" il_end="0x2d" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForEachIEnumerableWithTryCatchPossiblyImplementIDisposable()
            Dim source =
<compilation>
    <file>
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Implements System.Collections.IEnumerable
    ' Explicit implementation won't match pattern.
    Private Function System_Collections_IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Dim list As New System.Collections.Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="C" methodName="Main"/>
    <methods>
        <method containingType="C" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="5" offset="0"/>
                    <slot kind="0" offset="0"/>
                    <slot kind="1" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="39" document="1"/>
                <entry offset="0xc" hidden="true" document="1"/>
                <entry offset="0x1a" startLine="6" startColumn="13" endLine="6" endColumn="40" document="1"/>
                <entry offset="0x26" startLine="7" startColumn="9" endLine="7" endColumn="13" document="1"/>
                <entry offset="0x27" hidden="true" document="1"/>
                <entry offset="0x2e" hidden="true" document="1"/>
                <entry offset="0x33" hidden="true" document="1"/>
                <entry offset="0x48" startLine="8" startColumn="5" endLine="8" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x49">
                <currentnamespace name=""/>
                <scope startOffset="0xe" endOffset="0x26">
                    <local name="x" il_index="1" il_start="0xe" il_end="0x26" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

#End Region

#Region "For Loop"

        <WorkItem(529183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529183")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForLoop01()
            Dim source =
<compilation>
    <file>
Option Strict On
Imports System

Module M1
    Sub Main()
        Dim myFArr(3) As Short
        Dim i As Short
        For i = 1 To 3
            myFArr(i) = i
        Next i
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            ' Note: the scope of the loop variable is intentionally different from Dev11. 
            ' It's now the scope of the complete loop and not just the body
            compilation.VerifyPdb("M1.Main",
 <symbols>
     <files>
         <file id="1" name="" language="VB"/>
     </files>
     <entryPoint declaringType="M1" methodName="Main"/>
     <methods>
         <method containingType="M1" name="Main">
             <customDebugInfo>
                 <encLocalSlotMap>
                     <slot kind="0" offset="4"/>
                     <slot kind="0" offset="36"/>
                 </encLocalSlotMap>
             </customDebugInfo>
             <sequencePoints>
                 <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="15" document="1"/>
                 <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="22" document="1"/>
                 <entry offset="0x8" startLine="8" startColumn="9" endLine="8" endColumn="23" document="1"/>
                 <entry offset="0xa" startLine="9" startColumn="13" endLine="9" endColumn="26" document="1"/>
                 <entry offset="0xe" startLine="10" startColumn="9" endLine="10" endColumn="15" document="1"/>
                 <entry offset="0x13" hidden="true" document="1"/>
                 <entry offset="0x17" startLine="11" startColumn="5" endLine="11" endColumn="12" document="1"/>
             </sequencePoints>
             <scope startOffset="0x0" endOffset="0x18">
                 <namespace name="System" importlevel="file"/>
                 <currentnamespace name=""/>
                 <local name="myFArr" il_index="0" il_start="0x0" il_end="0x18" attributes="0"/>
                 <local name="i" il_index="1" il_start="0x0" il_end="0x18" attributes="0"/>
             </scope>
         </method>
     </methods>
 </symbols>)
        End Sub

        <WorkItem(529183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529183")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ForLoop02()
            Dim source =
<compilation>
    <file>
Option Strict On
Imports System

Module M1
  Sub Main()
    For i as Object = 3 To 6 step 2
    Console.Writeline("Hello")        
    Next
   End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            ' Note: the scope of the loop variable is intentionally different from Dev11. 
            ' It 's now the scope of the complete loop and not just the body            
            compilation.VerifyPdb("M1.Main",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="M1" methodName="Main"/>
    <methods>
        <method containingType="M1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="13" offset="0"/>
                    <slot kind="0" offset="4"/>
                    <slot kind="1" offset="0"/>
                    <slot kind="1" offset="0" ordinal="1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="3" endLine="5" endColumn="13" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="5" endLine="6" endColumn="36" document="1"/>
                <entry offset="0x1e" hidden="true" document="1"/>
                <entry offset="0x21" startLine="7" startColumn="5" endLine="7" endColumn="31" document="1"/>
                <entry offset="0x2c" startLine="8" startColumn="5" endLine="8" endColumn="9" document="1"/>
                <entry offset="0x36" hidden="true" document="1"/>
                <entry offset="0x39" startLine="9" startColumn="4" endLine="9" endColumn="11" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3a">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0x1" endOffset="0x38">
                    <local name="i" il_index="1" il_start="0x1" il_end="0x38" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

#End Region

    End Class
End Namespace
