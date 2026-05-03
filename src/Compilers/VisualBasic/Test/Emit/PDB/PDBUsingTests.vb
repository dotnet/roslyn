' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBUsingTests
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub UsingNested()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Inside Dispose.")
    End Sub
End Class

Class C1
    Public Shared Sub Main()

        Using goo1 As New MyDisposable(), goo2 As New MyDisposable(), goo3 As MyDisposable = Nothing
            Console.WriteLine("Inside Using.")
        End Using
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
                    <slot kind="0" offset="6"/>
                    <slot kind="0" offset="34"/>
                    <slot kind="0" offset="62"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="5" endLine="16" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="18" startColumn="9" endLine="18" endColumn="101" document="1"/>
                <entry offset="0x2" startLine="18" startColumn="15" endLine="18" endColumn="41" document="1"/>
                <entry offset="0x8" startLine="18" startColumn="43" endLine="18" endColumn="69" document="1"/>
                <entry offset="0xe" startLine="18" startColumn="71" endLine="18" endColumn="101" document="1"/>
                <entry offset="0x10" startLine="19" startColumn="13" endLine="19" endColumn="47" document="1"/>
                <entry offset="0x1b" hidden="true" document="1"/>
                <entry offset="0x1d" startLine="20" startColumn="9" endLine="20" endColumn="18" document="1"/>
                <entry offset="0x29" hidden="true" document="1"/>
                <entry offset="0x2b" startLine="20" startColumn="9" endLine="20" endColumn="18" document="1"/>
                <entry offset="0x37" hidden="true" document="1"/>
                <entry offset="0x39" startLine="20" startColumn="9" endLine="20" endColumn="18" document="1"/>
                <entry offset="0x45" startLine="21" startColumn="5" endLine="21" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <importsforward declaringType="MyDisposable" methodName="Dispose"/>
                <scope startOffset="0x2" endOffset="0x44">
                    <local name="goo1" il_index="0" il_start="0x2" il_end="0x44" attributes="0"/>
                    <local name="goo2" il_index="1" il_start="0x2" il_end="0x44" attributes="0"/>
                    <local name="goo3" il_index="2" il_start="0x2" il_end="0x44" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub UsingExpression()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using (New DisposableObject())
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry startLine="4" startColumn="5" endLine="4" endColumn="15"/>
                               <entry startLine="5" startColumn="9" endLine="5" endColumn="39"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry startLine="7" startColumn="5" endLine="7" endColumn="12"/>
                           </sequencePoints>

            AssertXml.Equal(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub UsingVariableDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using v As New DisposableObject()
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry startLine="4" startColumn="5" endLine="4" endColumn="15"/>
                               <entry startLine="5" startColumn="9" endLine="5" endColumn="42"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry startLine="7" startColumn="5" endLine="7" endColumn="12"/>
                           </sequencePoints>

            AssertXml.Equal(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub UsingMultipleVariableDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using v1 As New DisposableObject(), v2 As New DisposableObject()
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry startLine="4" startColumn="5" endLine="4" endColumn="15"/>
                               <entry startLine="5" startColumn="9" endLine="5" endColumn="73"/>
                               <entry startLine="5" startColumn="15" endLine="5" endColumn="43"/>
                               <entry startLine="5" startColumn="45" endLine="5" endColumn="73"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry startLine="7" startColumn="5" endLine="7" endColumn="12"/>
                           </sequencePoints>

            AssertXml.Equal(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub UsingVariableAsNewDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using v1, v2 As New DisposableObject()
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry startLine="4" startColumn="5" endLine="4" endColumn="15"/>
                               <entry startLine="5" startColumn="9" endLine="5" endColumn="47"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry startLine="7" startColumn="5" endLine="7" endColumn="12"/>
                           </sequencePoints>

            AssertXml.Equal(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub UsingMultipleVariableAsNewDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using v1, v2 As New DisposableObject(), v3, v4 As New DisposableObject()
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class 
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry startLine="4" startColumn="5" endLine="4" endColumn="15"/>
                               <entry startLine="5" startColumn="9" endLine="5" endColumn="81"/>
                               <entry startLine="5" startColumn="15" endLine="5" endColumn="47"/>
                               <entry startLine="5" startColumn="15" endLine="5" endColumn="47"/>
                               <entry startLine="5" startColumn="49" endLine="5" endColumn="81"/>
                               <entry startLine="5" startColumn="49" endLine="5" endColumn="81"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry/>
                               <entry startLine="6" startColumn="9" endLine="6" endColumn="18"/>
                               <entry startLine="7" startColumn="5" endLine="7" endColumn="12"/>
                           </sequencePoints>

            AssertXml.Equal(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub NoPia()
            Dim piaSource = "
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssembly(0, 0)>
<assembly: Guid(""863D5BC0-46A1-49AC-97AA-A5F0D441A9DA"")>

<ComImport>
<Guid(""863D5BC0-46A1-49AD-97AA-A5F0D441A9D9"")>
Public Interface I
    Function F() As Object
End Interface
"
            Dim piaComp = CreateCompilationWithMscorlib40({piaSource}, options:=TestOptions.DebugDll, assemblyName:="PIA")
            AssertNoErrors(piaComp)
            Dim piaRef = piaComp.EmitToImageReference(embedInteropTypes:=True)

            Dim source = "
Namespace N1
    Class C
        Shared Sub M()
            Dim o As I = Nothing
        End Sub
    End Class
End Namespace

Namespace N2
    Class D
        Shared Sub M()
        End Sub
    End Class
End Namespace
"

            Dim comp = CreateCompilationWithMscorlib40({source}, {piaRef}, TestOptions.DebugDll)
            Dim v = CompileAndVerify(comp)

            v.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="N1.C" name="M">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="9" endLine="4" endColumn="23" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="17" endLine="5" endColumn="33" document="1"/>
                <entry offset="0x3" startLine="6" startColumn="9" endLine="6" endColumn="16" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4">
                <defunct name="&amp;PIA"/>
                <currentnamespace name="N1"/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x4" attributes="0"/>
            </scope>
        </method>
        <method containingType="N2.D" name="M">
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="9" endLine="12" endColumn="23" document="1"/>
                <entry offset="0x1" startLine="13" startColumn="9" endLine="13" endColumn="16" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <defunct name="&amp;PIA"/>
                <currentnamespace name="N2"/>
            </scope>
        </method>
    </methods>
</symbols>
            )
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub UnusedImports()
            Dim source = "
Imports X = System.Linq.Enumerable
Imports Y = System.Linq

Class C
    Sub Main() 
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib40(
                {source},
                {SystemCoreRef, SystemDataRef},
                options:=TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse("System.Data.DataColumn")))

            CompileAndVerify(comp, validator:=
                Sub(peAssembly)
                    Dim reader = peAssembly.ManifestModule.MetadataReader

                    Assert.Equal(
                        {"mscorlib",
                         "System.Core",
                         "System.Data"},
                        peAssembly.AssemblyReferences.Select(Function(ai) ai.Name))

                    Assert.Equal(
                        {"CompilationRelaxationsAttribute",
                         "RuntimeCompatibilityAttribute",
                         "DebuggableAttribute",
                         "DebuggingModes",
                         "Object",
                         "Enumerable",
                         "DataColumn"},
                         reader.TypeReferences.Select(Function(h) reader.GetString(reader.GetTypeReference(h).Name)))
                End Sub)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub UnusedImports_Nonexisting()
            Dim source = "
Imports A.B
Imports Z = C.D

Class Program
    Sub Main() 
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib40(
                {source},
                {SystemCoreRef, SystemDataRef},
                options:=TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse("E.F"), GlobalImport.Parse("Q = G.H")))

            ' only warnings are reported (unlike C# which reports errors):
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UndefinedOrEmptyProjectNamespaceOrClass1).WithArguments("E.F"),
                Diagnostic(ERRID.WRN_UndefinedOrEmptyProjectNamespaceOrClass1).WithArguments("Q = G.H"),
                Diagnostic(ERRID.WRN_UndefinedOrEmptyNamespaceOrClass1, "A.B").WithArguments("A.B"),
                Diagnostic(ERRID.WRN_UndefinedOrEmptyNamespaceOrClass1, "C.D").WithArguments("C.D"),
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports A.B"),
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports Z = C.D"))

            CompileAndVerify(comp)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub BadGlobalImports()
            Dim source1 = "
Namespace N
    Friend Class A
    End Class
End Namespace
"
            Dim source2 = "
Class C
    Sub Main() 
        Console.WriteLine()
    End Sub
End Class
"
            Dim comp1 = CreateCompilationWithMscorlib40({source1}, options:=TestOptions.ReleaseDll)
            Dim ref1 = comp1.EmitToImageReference()

            Dim comp2 = CreateCompilationWithMscorlib40(
                {source2}, {ref1},
                options:=TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse("X=N.A"), GlobalImport.Parse("System")))

            comp2.VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_InaccessibleSymbol2))
        End Sub
    End Class
End Namespace
