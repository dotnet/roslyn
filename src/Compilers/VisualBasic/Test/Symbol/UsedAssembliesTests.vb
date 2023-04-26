' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class UsedAssembliesTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub NoReferences_01()
            Dim source =
    <compilation>
        <file>
interface I1
    Function M() as I1
End Interface
        </file>
    </compilation>

            Dim comp1 = CreateEmptyCompilation(source)
            ' ILVerify: Failed to load type 'System.String' from assembly ...
            CompileAndVerify(comp1, verify:=Verification.FailsILVerify)

            Assert.Empty(comp1.GetUsedAssemblyReferences())

            Dim comp2 = CreateCompilation(source)
            CompileAndVerify(comp2)

            AssertUsedAssemblyReferences(comp2)
        End Sub

        <Fact()>
        Public Sub NoReferences_02()
            Dim source =
    <compilation>
        <file>
Public interface I1
    Function M() as I1
End Interface
        </file>
    </compilation>

            Dim comp1 = CreateEmptyCompilation(source)
            CompileAndVerify(comp1, verify:=Verification.FailsILVerify)

            Dim source2 =
    <compilation>
        <file>
public class C2
    public Shared Sub Main(x As I1)
        x.M()
    End Sub
End Class
        </file>
    </compilation>

            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source2, comp1.EmitToImageReference())
            VerifyUsedAssemblyReferences(Of RetargetingAssemblySymbol)(source2, comp1.ToMetadataReference())
            Assert.Empty(comp1.GetUsedAssemblyReferences())
        End Sub

        Private Sub VerifyUsedAssemblyReferences(Of TAssemblySymbol As AssemblySymbol)(source2 As BasicTestSource, reference As MetadataReference)
            Dim comp2 As Compilation = AssertUsedAssemblyReferences(source2, reference)
            Assert.IsType(Of TAssemblySymbol)(DirectCast(comp2, VisualBasicCompilation).GetAssemblyOrModuleSymbol(reference))
        End Sub

        Private Sub VerifyUsedAssemblyReferences(Of TAssemblySymbol As AssemblySymbol)(source2 As BasicTestSource, reference0 As MetadataReference, reference1 As MetadataReference)
            Dim comp2 As Compilation = AssertUsedAssemblyReferences(source2, {reference0, reference1}, reference1)
            Assert.IsType(Of TAssemblySymbol)(DirectCast(comp2, VisualBasicCompilation).GetAssemblyOrModuleSymbol(reference1))
        End Sub

        Private Sub AssertUsedAssemblyReferences(comp As Compilation, expected As MetadataReference(), before As XElement, after As XElement, specificReferencesToAssert As MetadataReference())
            comp.AssertTheseDiagnostics(before, suppressInfos:=False)

            Dim hasCoreLibraryRef As Boolean = comp.ObjectType.Kind = SymbolKind.NamedType
            Dim used = comp.GetUsedAssemblyReferences()

            If hasCoreLibraryRef Then
                Assert.Same(comp.ObjectType.ContainingAssembly, comp.GetAssemblyOrModuleSymbol(used(0)))
                AssertEx.Equal(expected, used.Skip(1))
            Else
                AssertEx.Equal(expected, used)
            End If

            Assert.Empty(used.Where(Function(r) r.Properties.Kind = MetadataImageKind.Module))

            Dim comp2 = comp.RemoveAllReferences().AddReferences(used.Concat(comp.References.Where(Function(r) r.Properties.Kind = MetadataImageKind.Module)))

            CompileAndVerify(comp2, verify:=Verification.Skipped).Diagnostics.AssertTheseDiagnostics(after, suppressInfos:=False)

            If specificReferencesToAssert IsNot Nothing Then
                Dim tryRemove = specificReferencesToAssert.Where(Function(reference) reference.Properties.Kind = MetadataImageKind.Assembly AndAlso Not used.Contains(reference))
                If tryRemove.Count() > 1 Then
                    For Each reference In tryRemove
                        Dim comp3 = comp.RemoveReferences(reference)
                        CompileAndVerify(comp3, verify:=Verification.Skipped).Diagnostics.AssertTheseDiagnostics(after, suppressInfos:=False)
                    Next
                End If
            End If
        End Sub

        Private Sub AssertUsedAssemblyReferences(comp As Compilation, ParamArray expected As MetadataReference())
            AssertUsedAssemblyReferences(comp, expected, Nothing, Nothing, specificReferencesToAssert:=Nothing)
        End Sub

        Private Sub AssertUsedAssemblyReferences(comp As Compilation, expected As MetadataReference(), specificReferencesToAssert As MetadataReference())
            AssertUsedAssemblyReferences(comp, expected, Nothing, Nothing, specificReferencesToAssert)
        End Sub

        Private Function AssertUsedAssemblyReferences(source As BasicTestSource, references As MetadataReference(), ParamArray expected As MetadataReference()) As Compilation
            Dim comp As Compilation = CreateCompilation(source, references:=references)
            AssertUsedAssemblyReferences(comp, expected, references)
            Return comp
        End Function

        Private Function AssertUsedAssemblyReferences(source As BasicTestSource, ParamArray references As MetadataReference()) As Compilation
            Return AssertUsedAssemblyReferences(source, references, references)
        End Function

        Private Shared Sub AssertUsedAssemblyReferences(source As BasicTestSource, references As MetadataReference(), expected As XElement)
            Dim comp As Compilation = CreateCompilation(source, references:=references)
            Dim diagnostics = comp.GetDiagnostics()
            diagnostics.AssertTheseDiagnostics(expected)

            Assert.True(diagnostics.Any(Function(d) d.DefaultSeverity = DiagnosticSeverity.Error))
            AssertEx.Equal(comp.References.Where(Function(r) r.Properties.Kind = MetadataImageKind.Assembly), comp.GetUsedAssemblyReferences())
        End Sub

        Private Function CompileWithUsedAssemblyReferences(source As BasicTestSource, targetFramework As TargetFramework, ParamArray references As MetadataReference()) As ImmutableArray(Of MetadataReference)
            Return CompileWithUsedAssemblyReferences(source, targetFramework, options:=Nothing, references)
        End Function

        Private Function CompileWithUsedAssemblyReferences(source As BasicTestSource, targetFramework As TargetFramework, options As VisualBasicCompilationOptions, ParamArray references As MetadataReference()) As ImmutableArray(Of MetadataReference)
            Dim comp As Compilation = CreateEmptyCompilation(source,
                                                      references:=TargetFrameworkUtil.GetReferences(targetFramework).Concat(references),
                                                      options:=options,
                                                      parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose))
            Return CompileWithUsedAssemblyReferences(comp, specificReferencesToAssert:=references)
        End Function

        Private Function CompileWithUsedAssemblyReferences(comp As Compilation, Optional expectedOutput As String = Nothing, Optional specificReferencesToAssert As MetadataReference() = Nothing) As ImmutableArray(Of MetadataReference)
            Dim used = comp.GetUsedAssemblyReferences()
            CompileAndVerify(comp, verify:=Verification.Skipped, expectedOutput:=expectedOutput).VerifyDiagnostics()

            Assert.Empty(used.Where(Function(r) r.Properties.Kind = MetadataImageKind.Module))

            If specificReferencesToAssert IsNot Nothing Then
                Dim tryRemove = specificReferencesToAssert.Where(Function(reference) reference.Properties.Kind = MetadataImageKind.Assembly AndAlso Not used.Contains(reference))
                If tryRemove.Count() > 1 Then
                    For Each reference In tryRemove
                        Dim comp3 = comp.RemoveReferences(reference)
                        comp3.VerifyDiagnostics()
                        CompileAndVerify(comp3, verify:=Verification.Skipped, expectedOutput:=expectedOutput).VerifyDiagnostics()
                    Next
                End If
            End If

            Dim comp2 = comp.RemoveAllReferences().AddReferences(used.Concat(comp.References.Where(Function(r) r.Properties.Kind = MetadataImageKind.Module)))
            comp2.VerifyDiagnostics()
            CompileAndVerify(comp2, verify:=Verification.Skipped, expectedOutput:=expectedOutput).VerifyDiagnostics()

            Return used
        End Function

        Private Function CompileWithUsedAssemblyReferences(source As BasicTestSource, ParamArray references As MetadataReference()) As ImmutableArray(Of MetadataReference)
            Return CompileWithUsedAssemblyReferences(source, TargetFramework.Standard, references)
        End Function

        Private Function CompileWithUsedAssemblyReferences(source As BasicTestSource, options As VisualBasicCompilationOptions, ParamArray references As MetadataReference()) As ImmutableArray(Of MetadataReference)
            Return CompileWithUsedAssemblyReferences(source, TargetFramework.Standard, options:=options, references)
        End Function

        <Fact()>
        Public Sub NoReferences_03()
            Dim source =
    <compilation>
        <file>
namespace System
    public class [Object]
    End Class
    public class ValueType
    End Class
    public structure Void
    End Structure
End Namespace

Public interface I1
    Function M() as I1
End Interface
        </file>
    </compilation>

            Dim comp1 = CreateEmptyCompilation(source)
            comp1.AssertTheseEmitDiagnostics()

            Dim source2 =
    <compilation>
        <file>
public class C2
    public shared Function Main(x as I1) As Object
        x.M()
        return Nothing
    End Function
End Class
        </file>
    </compilation>

            Verify_NoReferences_03(Of PEAssemblySymbol)(source2, comp1.EmitToImageReference())
            Verify_NoReferences_03(Of SourceAssemblySymbol)(source2, comp1.ToMetadataReference())
            Assert.Empty(comp1.GetUsedAssemblyReferences())
        End Sub

        Private Sub Verify_NoReferences_03(Of TAssemblySymbol As AssemblySymbol)(source2 As BasicTestSource, reference As MetadataReference)
            Dim comp2 As Compilation = CreateEmptyCompilation(source2, references:={reference, SystemCoreRef, SystemDrawingRef})
            AssertUsedAssemblyReferences(comp2)
            Assert.IsType(Of TAssemblySymbol)(DirectCast(comp2, VisualBasicCompilation).GetAssemblyOrModuleSymbol(reference))
        End Sub

        <Fact()>
        Public Sub NoReferences_04()
            Dim source =
    <compilation>
        <file>
Public interface I1
    Function M1() as I1
End Interface
        </file>
    </compilation>

            Dim comp1 = CreateEmptyCompilation(source)
            CompileAndVerify(comp1, verify:=Verification.FailsILVerify)

            Dim source2 =
    <compilation>
        <file>
Public interface I2
    Function M2() as I1
End Interface
        </file>
    </compilation>

            Verify_NoReferences_04(Of PEAssemblySymbol)(source2, comp1.EmitToImageReference())
            Verify_NoReferences_04(Of RetargetingAssemblySymbol)(source2, comp1.ToMetadataReference())
            Assert.Empty(comp1.GetUsedAssemblyReferences())
        End Sub

        Private Sub Verify_NoReferences_04(Of TAssemblySymbol As AssemblySymbol)(source2 As BasicTestSource, reference As MetadataReference)
            Dim comp2 As Compilation = CreateEmptyCompilation(source2, references:={reference, SystemCoreRef, SystemDrawingRef})
            AssertUsedAssemblyReferences(comp2, reference)
            Assert.IsType(Of TAssemblySymbol)(DirectCast(comp2, VisualBasicCompilation).GetAssemblyOrModuleSymbol(reference))
        End Sub

        <Fact()>
        Public Sub FieldReference_01()
            Dim source1 =
    <compilation>
        <file>
public class C1
    public shared F1 As Integer = 0
    public F2 As Integer = 0
End class
        </file>
    </compilation>

            Dim comp1 = CreateCompilation(source1)
            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
    <compilation>
        <file>
public class C2
    public Shared Sub Main()
        Dim __ = C1.F1
    End Sub
End Class
        </file>
    </compilation>

            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source2, comp1ImageRef)
            VerifyUsedAssemblyReferences(Of SourceAssemblySymbol)(source2, comp1Ref)

            Dim source3 =
    <compilation>
        <file>
public class C2
    public Shared Sub Main()
        Dim x as C1 = Nothing
        Dim __ = x.F2
    End Sub
End Class
        </file>
    </compilation>

            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source3, comp1ImageRef)
            VerifyUsedAssemblyReferences(Of SourceAssemblySymbol)(source3, comp1Ref)

        End Sub

        <Fact()>
        Public Sub FieldReference_02()
            Dim source0 =
    <compilation>
        <file>
public class C0
    public shared F0 As C0 = new C0()
End class
        </file>
    </compilation>

            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference()
            Dim comp0ImageRef = comp0.EmitToImageReference()

            Dim source1 =
    <compilation>
        <file>
public class C1
    public shared F0 As C0 = C0.F0
    public shared F1 As Integer = 0
End Class
        </file>
    </compilation>

            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            comp1.VerifyDiagnostics()

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
    <compilation>
        <file>
public class C2
    public Shared Sub Main()
        Dim __ = C1.F1
    End Sub
End Class
        </file>
    </compilation>

            Verify_FieldReference_02(Of PEAssemblySymbol)(source2, comp0ImageRef, comp1ImageRef)
            Verify_FieldReference_02(Of PEAssemblySymbol)(source2, comp0Ref, comp1ImageRef)
            Verify_FieldReference_02(Of SourceAssemblySymbol)(source2, comp0Ref, comp1Ref)
            Verify_FieldReference_02(Of RetargetingAssemblySymbol)(source2, comp0ImageRef, comp1Ref)

            Dim source3 =
    <compilation>
        <file><![CDATA[
Imports C1

public class C2
    public shared Sub Main()
        Assert(F1)
    End Sub

    <System.Diagnostics.Conditional("Always")>
    public shared Sub Assert(condition As Integer)
    End Sub
End Class
        ]]></file>
    </compilation>

            Verify_FieldReference_02(Of SourceAssemblySymbol)(source3, comp0Ref, comp1Ref)

        End Sub

        Private Sub Verify_FieldReference_02(Of TAssemblySymbol As AssemblySymbol)(source2 As BasicTestSource, reference0 As MetadataReference, reference1 As MetadataReference)
            Dim comp2 As Compilation = AssertUsedAssemblyReferences(source2, reference0, reference1)
            Assert.IsType(Of TAssemblySymbol)(DirectCast(comp2, VisualBasicCompilation).GetAssemblyOrModuleSymbol(reference1))
        End Sub

        <Fact()>
        Public Sub FieldReference_03()
            Dim source0 =
    <compilation>
        <file>
public class C0
End class
        </file>
    </compilation>

            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference()
            Dim comp0ImageRef = comp0.EmitToImageReference()

            Dim source1 =
    <compilation>
        <file>
Friend class C1
    Private Shared F0 As C0 = new C0()
    public Shared F1 as C1 = new C1()
End class
        </file>
    </compilation>

            Dim comp1 = CreateCompilation(source1, references:={comp0Ref}, options:=TestOptions.ReleaseModule)
            comp1.VerifyDiagnostics()

            Dim comp1Ref = comp1.EmitToImageReference()

            Dim source2 =
    <compilation>
        <file>
public class C2
    Private Shared F1 As C1 = C1.F1
    public Shared F2 As Integer = 0
End class
        </file>
    </compilation>

            Dim comp2 = Verify2_FieldReference_03(Of SourceAssemblySymbol)(source2, comp0Ref, comp1Ref)

            Dim comp2Ref = comp2.ToMetadataReference()
            Dim comp2ImageRef = comp2.EmitToImageReference()

            Dim source3 =
    <compilation>
        <file>
public class C3
    public shared sub Main()
        Dim __ = C2.F2
    End Sub
End class
        </file>
    </compilation>

            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp0ImageRef, comp2ImageRef)
            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp0Ref, comp2ImageRef)
            Verify3_FieldReference_03(Of SourceAssemblySymbol)(source3, comp0Ref, comp2Ref)
            Verify3_FieldReference_03(Of RetargetingAssemblySymbol)(source3, comp0ImageRef, comp2Ref)
            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp2ImageRef)
            Verify3_FieldReference_03(Of RetargetingAssemblySymbol)(source3, comp2Ref)

            comp2 = Verify2_FieldReference_03(Of PEAssemblySymbol)(source2, comp0ImageRef, comp1Ref)
            comp2Ref = comp2.ToMetadataReference()
            comp2ImageRef = comp2.EmitToImageReference()

            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp0ImageRef, comp2ImageRef)
            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp0Ref, comp2ImageRef)
            Verify3_FieldReference_03(Of RetargetingAssemblySymbol)(source3, comp0Ref, comp2Ref)
            Verify3_FieldReference_03(Of SourceAssemblySymbol)(source3, comp0ImageRef, comp2Ref)
            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp2ImageRef)
            Verify3_FieldReference_03(Of RetargetingAssemblySymbol)(source3, comp2Ref)

            comp2 = CreateCompilation(source2, references:={comp1Ref})
            comp2.VerifyDiagnostics()

            Assert.True(comp2.References.Count() > 1)

            Dim used = comp2.GetUsedAssemblyReferences()

            Assert.Equal(1, used.Length)
            Assert.Same(comp2.ObjectType.ContainingAssembly, comp2.GetAssemblyOrModuleSymbol(used(0)))

            comp2Ref = comp2.ToMetadataReference()
            comp2ImageRef = comp2.EmitToImageReference()

            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp0ImageRef, comp2ImageRef)
            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp0Ref, comp2ImageRef)
            Verify3_FieldReference_03(Of RetargetingAssemblySymbol)(source3, comp0Ref, comp2Ref)
            Verify3_FieldReference_03(Of RetargetingAssemblySymbol)(source3, comp0ImageRef, comp2Ref)
            Verify3_FieldReference_03(Of PEAssemblySymbol)(source3, comp2ImageRef)
            Verify3_FieldReference_03(Of SourceAssemblySymbol)(source3, comp2Ref)
        End Sub

        Private Function Verify2_FieldReference_03(Of TAssemblySymbol As AssemblySymbol)(source2 As BasicTestSource, reference0 As MetadataReference, reference1 As MetadataReference) As Compilation
            Dim comp2 As Compilation = AssertUsedAssemblyReferences(source2, {reference0, reference1}, reference0)
            Assert.IsType(Of TAssemblySymbol)(DirectCast(comp2, VisualBasicCompilation).GetAssemblyOrModuleSymbol(reference0))
            Return comp2
        End Function

        Private Sub Verify3_FieldReference_03(Of TAssemblySymbol As AssemblySymbol)(source3 As BasicTestSource, ParamArray references As MetadataReference())
            Dim comp3 As Compilation = AssertUsedAssemblyReferences(source3, references)
            Assert.IsType(Of TAssemblySymbol)(DirectCast(comp3, VisualBasicCompilation).GetAssemblyOrModuleSymbol(references.Last()))
        End Sub

        <Fact()>
        Public Sub FieldReference_04()
            Dim source1 =
    <compilation>
        <file>
namespace N1
    public enum E1
        F1 = 0
    End Enum
End Namespace
        </file>
    </compilation>

            Dim comp1 = CreateCompilation(source1)

            Dim comp1Ref = comp1.ToMetadataReference()

            VerifyUsedAssemblyReferences(comp1Ref,
    <compilation>
        <file>
public class C2
    public shared Sub Main()
        Dim __ = N1.E1.F1 + 1
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp1Ref,
    <compilation>
        <file>
Imports N1
public class C2
    public shared Sub Main()
        Dim __ = E1.F1 + 1
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp1Ref,
    <compilation>
        <file>
Imports N1.E1
public class C2
    public shared Sub Main()
        Dim __ = F1 + 1
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp1Ref,
    <compilation>
        <file>
Imports [alias] = N1.E1
public class C2
    public shared Sub Main()
        Dim __ = [alias].F1 + 1
    End Sub
End Class
        </file>
    </compilation>)
        End Sub

        Private Sub VerifyUsedAssemblyReferences(reference As MetadataReference, source As BasicTestSource)
            AssertUsedAssemblyReferences(source, reference)
        End Sub

        <Fact()>
        Public Sub FieldReference_05()
            Dim source0 =
    <compilation>
        <file>
public class C0
End Class
        </file>
    </compilation>

            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
    <compilation>
        <file>
public class C1(Of T)
    public enum E1
        F1 = 0
    end enum

    public class C3
        public F3 As Integer = 0
    End Class
End Class
        </file>
    </compilation>

            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
public class C2
    public shared Sub Main()
        Dim __ = C1(Of C0).E1.F1 + 1
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports C1(Of C0)
public class C2
    public shared Sub Main()
        Dim __ = E1.F1 + 1
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports C1(Of C0).E1
public class C2
    public shared Sub Main()
        Dim __ = F1 + 1
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports [alias] = C1(Of C0).E1
public class C2
    public shared Sub Main()
        Dim __ = [alias].F1 + 1
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports [alias] = C1(Of C0)
public class C2
    public shared Sub Main()
        Dim __ = [alias].E1.F1 + 1
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
public class C2
    public shared Sub Main()
        Dim __ = nameof(C1(Of C0).E1.F1)
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports C1(Of C0)
public class C2
    public shared Sub Main()
        Dim __ = nameof(E1.F1)
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports C1(Of C0).E1
public class C2
    public shared Sub Main()
        Dim __ = nameof(F1)
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports [alias] = C1(Of C0).E1
public class C2
    public shared Sub Main()
        Dim __ = nameof([alias].F1)
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports [alias] = C1(Of C0)
public class C2
    public shared Sub Main()
        Dim __ = nameof([alias].E1.F1)
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
public class C2
    public shared Sub Main()
        Dim __ = nameof(C1(Of C0).C3.F3)
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports C1(Of C0)
public class C2
    public shared Sub Main()
        Dim __ = nameof(C3.F3)
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports [alias] = C1(Of C0).C3
public class C2
    public shared Sub Main()
        Dim __ = nameof([alias].F3)
    End Sub
End Class
        </file>
    </compilation>)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
    <compilation>
        <file>
Imports [alias] = C1(Of C0)
public class C2
    public shared Sub Main()
        Dim __ = nameof([alias].C3.F3)
    End Sub
End Class
        </file>
    </compilation>)
        End Sub

        Private Sub VerifyUsedAssemblyReferences(reference0 As MetadataReference, reference1 As MetadataReference, source As BasicTestSource)
            AssertUsedAssemblyReferences(source, reference0, reference1)
        End Sub

        <Fact()>
        Public Sub FieldReference_06()
            Dim source0 =
    <compilation>
        <file>
public class C0
End Class
        </file>
    </compilation>

            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
    <compilation>
        <file>
public class C1(Of T)
    public enum E1
        F1 = 0
    end enum

    public class C3
        public F3 As Integer = 0
    End Class
End Class
        </file>
    </compilation>

            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
public class C2
    ''' <summary>
    ''' <see cref="C1(Of C0).E1.F1"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>,
                    hasTypeReferencesInImports:=False)

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
Imports C1(Of C0)
public class C2
    ''' <summary>
    ''' <see cref="E1.F1"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>)

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
Imports C1(Of C0).E1
public class C2
    ''' <summary>
    ''' <see cref="F1"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>)

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
Imports alias1 = C1(Of C0).E1
public class C2
    ''' <summary>
    ''' <see cref="alias1.F1"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>)

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
Imports alias1 = C1(Of C0)
public class C2
    ''' <summary>
    ''' <see cref="alias1.E1.F1"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>)

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
public class C2
    ''' <summary>
    ''' <see cref="C1(Of C0).C3.F3"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>,
                    hasTypeReferencesInImports:=False)

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
Imports C1(Of C0)
public class C2
    ''' <summary>
    ''' <see cref="C3.F3"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>)

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
Imports alias1 = C1(Of C0).C3
public class C2
    ''' <summary>
    ''' <see cref="alias1.F3"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>)

            VerifyCrefReferences(comp0Ref, comp1Ref,
    <compilation>
        <file><![CDATA[
Imports alias1 = C1(Of C0)
public class C2
    ''' <summary>
    ''' <see cref="alias1.C3.F3"/>
    ''' </summary>
    public shared Sub Main()
    End Sub
End Class
        ]]></file>
    </compilation>)
        End Sub

        Private Sub VerifyCrefReferences(
            reference0 As MetadataReference,
            reference1 As MetadataReference,
            source As BasicTestSource,
            Optional hasTypeReferencesInImports As Boolean = True
        )
            Dim references = {reference0, reference1}
            Dim comp2 As Compilation = CreateCompilation(source, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None))
            AssertUsedAssemblyReferences(comp2, If(hasTypeReferencesInImports, references, {}), references)

            Dim expected = If(hasTypeReferencesInImports, references, {reference1})

            Dim comp3 As Compilation = CreateCompilation(source, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse))
            AssertUsedAssemblyReferences(comp3, expected)

            Dim comp4 = CreateCompilation(source, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose))
            AssertUsedAssemblyReferences(comp4, expected)
        End Sub

        <Fact()>
        Public Sub FieldReference_07()
            Dim source0 =
    <compilation>
        <file>
public class C0
End Class
        </file>
    </compilation>

            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
    <compilation>
        <file>
public class C1(Of T)
    public enum E1
        F1 = 0
    end enum
End Class
        </file>
    </compilation>

            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim attribute =
"
class TestAttribute
    Inherits System.Attribute
    public Sub New()
    End Sub
    public Sub New(value As Integer)
    End Sub
    public Value As Integer = 0
End Class
"

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
public class C2
    <Test(C1(Of C0).E1.F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0)
public class C2
    <Test(E1.F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0).E1
public class C2
    <Test(F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0).E1
public class C2
    <Test(alias1.F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0)
public class C2
    <Test(alias1.E1.F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
public class C2
    <Test(Value:=C1(Of C0).E1.F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0)
public class C2
    <Test(Value:=E1.F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0).E1
public class C2
    <Test(Value:=F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0).E1
public class C2
    <Test(Value:=alias1.F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0)
public class C2
    <Test(Value:=alias1.E1.F1 + 1)>
    public shared Sub Main()
    End Sub
End Class
" + attribute)
        End Sub

        <Fact()>
        Public Sub FieldReference_08()
            Dim source0 =
    <compilation>
        <file>
public class C0
End Class
        </file>
    </compilation>

            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
    <compilation>
        <file>
public class C1(Of T)
    public enum E1
        F1 = 0
    end enum
End Class
        </file>
    </compilation>

            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
public class C2
    public shared Sub Main(Optional p As Integer = C1(Of C0).E1.F1 + 1)
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0)
public class C2
    public shared Sub Main(Optional p As Integer = E1.F1 + 1)
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0).E1
public class C2
    public shared Sub Main(Optional p As Integer = F1 + 1)
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0).E1
public class C2
    public shared Sub Main(Optional p As Integer = alias1.F1 + 1)
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0)
public class C2
    public shared Sub Main(Optional p As Integer = alias1.E1.F1 + 1)
    End Sub
End Class
")
        End Sub

        <Fact()>
        Public Sub FieldReference_09()
            Dim source0 =
"
Public Module Module01
    Public FF1 As Integer
End Module"

            Dim comp0 = CreateCompilation(source0, targetFramework:=TargetFramework.StandardAndVBRuntime)
            Dim comp0Ref = comp0.ToMetadataReference()
            Dim comp0ImageRef = comp0.EmitToImageReference()

            Dim source1 =
"
public class C2
    public shared Function Main() As String
        return Nameof(FF1)
    End Function
End Class
"

            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0Ref)
            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0ImageRef)
        End Sub

        <Fact()>
        Public Sub MethodReference_01()
            Dim source1 =
"
public class C1
    Public Shared Sub M1()
    End Sub
End Class
"
            Dim comp1 = CreateCompilation(source1)

            Dim source2 =
"
public class C2
    public shared Sub Main()
        C1.M1()
    end Sub
End Class
"
            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source2, comp1.EmitToImageReference())
            VerifyUsedAssemblyReferences(Of SourceAssemblySymbol)(source2, comp1.ToMetadataReference())
        End Sub

        <Fact()>
        Public Sub MethodReference_02()
            Dim source0 =
"
public class C0
End Class"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim source1 =
"
public class C1
    public shared Sub M1(Of T)()
    End Sub
End Class

public class C2(Of T)
End Class

public class C3(Of T)
    public class C4
    End Class
End Class
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()

            Dim reference0 = comp0.ToMetadataReference()
            Dim reference1 = comp1.ToMetadataReference()

            VerifyUsedAssemblyReferences(reference0, reference1,
"
public class C5
    public shared Sub Main()
        C1.M1(Of C0)()
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(reference0, reference1,
"
public class C5
    public shared Sub Main()
        C1.M1(Of C2(Of C0))()
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(reference1, reference0,
"
public class C5
    public shared Sub Main()
        C1.M1(Of C3(Of C0).C4)()
    End Sub
End Class
")
        End Sub

        Shared ReadOnly ExtensionAttributeSource As String =
"
Namespace Microsoft.VisualBasic.CompilerServices
    Public Class StandardModuleAttribute : Inherits System.Attribute
    End Class
End Namespace

Namespace System.Runtime.CompilerServices
    Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace
"

        <Fact()>
        Public Sub MethodReference_03()
            Dim source0 =
"
public Module C0
    <System.Runtime.CompilerServices.Extension>
    public Sub M1(this As string, y As integer)
    End Sub
End Module
" + ExtensionAttributeSource

            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()
            comp0.AssertTheseDiagnostics()

            Dim source1 =
"
public Module C1
    <System.Runtime.CompilerServices.Extension>
    public Sub M1(this As string, y as string)
    End Sub
End Module
" + ExtensionAttributeSource

            Dim comp1 = CreateCompilation(source1)
            Dim comp1Ref = comp1.ToMetadataReference()
            comp1.AssertTheseDiagnostics()

            Dim source2 =
"
public class C2
    public shared Sub Main()
        Dim x = ""a""
        x.M1(""b"")
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source2, references:={comp0Ref, comp1Ref}, comp0Ref, comp1Ref)
        End Sub

        <Fact()>
        Public Sub MethodReference_04()
            Dim source0 =
"
public Module C0
    <System.Runtime.CompilerServices.Extension>
    public Sub M1(this As string, y As string)
    End Sub
End Module
" + ExtensionAttributeSource

            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()
            comp0.AssertTheseDiagnostics()

            Dim source1 =
"
public Module C1
    <System.Runtime.CompilerServices.Extension>
    public Sub M1(this As string, y as string)
    End Sub
End Module
" + ExtensionAttributeSource

            Dim comp1 = CreateCompilation(source1)
            Dim comp1Ref = comp1.ToMetadataReference()
            comp1.AssertTheseDiagnostics()

            Dim source2 =
"
public class C2
    public shared Sub Main()
        Dim x = ""a""
        x.M1(""b"")
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source2, references:={comp0Ref, comp1Ref},
<expected>
BC30521: Overload resolution failed because no accessible 'M1' is most specific for these arguments:
    Extension method 'Public Sub M1(y As String)' defined in 'C0': Not most specific.
    Extension method 'Public Sub M1(y As String)' defined in 'C1': Not most specific.
        x.M1("b")
          ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MethodReference_05()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0, assemblyName:="MethodReference_05_0")
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public Module C1
    <System.Runtime.CompilerServices.Extension>
    public Sub M1(this As string, y as C0)
    End Sub
End Module

public interface I1
End Interface
" + ExtensionAttributeSource

            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
public Module C1
    <System.Runtime.CompilerServices.Extension>
    public Sub M1(this As string, y as string)
    End Sub
End Module
" + ExtensionAttributeSource

            Dim comp2 = CreateCompilation(source2)
            Dim comp2Ref = comp2.ToMetadataReference()

            Dim source3 =
"
public class C3
    public shared Sub Main()
        Dim x = ""a""
        x.M1(""b"")
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source3, references:={comp0Ref, comp1Ref, comp2Ref}, comp0Ref, comp1Ref, comp2Ref)

            Dim expected1 =
<expected>
BC30652: Reference required to assembly 'MethodReference_05_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C0'. Add one to your project.
        x.M1("b")
        ~~~~~~~~~
</expected>

            AssertUsedAssemblyReferences(source3, references:={comp1Ref, comp2Ref}, expected1)

            Dim source4 =
"
public class C3
    public shared Sub Main()
        Dim x = ""a""
        x.M1(""b"")
    End Sub

    Sub M1(x as I1)
    End Sub
End Class
"
            AssertUsedAssemblyReferences(source4, references:={comp0Ref, comp1Ref, comp2Ref}, comp0Ref, comp1Ref, comp2Ref)

            AssertUsedAssemblyReferences(source4, references:={comp1Ref, comp2Ref}, expected1)

            Dim source5 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface C0
End Interface
"

            Dim comp5 = CreateCompilation(source5)
            comp5.VerifyDiagnostics()

            Dim comp5Ref = comp5.ToMetadataReference(embedInteropTypes:=True)

            Dim comp6 = CreateCompilation(source1, references:={comp5Ref})
            Dim comp6Ref = comp6.ToMetadataReference()
            Dim comp6ImageRef = comp6.EmitToImageReference()

            Dim comp7 = CreateCompilation(source5)
            Dim comp7Ref = comp7.ToMetadataReference(embedInteropTypes:=False)
            Dim comp7ImageRef = comp7.EmitToImageReference(embedInteropTypes:=False)

            AssertUsedAssemblyReferences(source3, references:={comp7Ref, comp6Ref, comp2Ref}, comp7Ref, comp6Ref, comp2Ref)
            AssertUsedAssemblyReferences(source3, references:={comp7ImageRef, comp6ImageRef, comp2Ref}, comp7ImageRef, comp6ImageRef, comp2Ref)

            Dim expected2 =
<expected>
BC31539: Cannot find the interop type that matches the embedded type 'C0'. Are you missing an assembly reference?
        x.M1("b")
        ~~~~~~~~~
</expected>

            AssertUsedAssemblyReferences(source3, references:={comp6Ref, comp2Ref}, expected2)
            AssertUsedAssemblyReferences(source3, references:={comp6ImageRef, comp2Ref}, expected2)

            AssertUsedAssemblyReferences(source4, references:={comp7Ref, comp6Ref, comp2Ref}, comp7Ref, comp6Ref, comp2Ref)
            AssertUsedAssemblyReferences(source4, references:={comp7ImageRef, comp6ImageRef, comp2Ref}, comp7ImageRef, comp6ImageRef, comp2Ref)

            AssertUsedAssemblyReferences(source4, references:={comp6Ref, comp2Ref}, expected2)
            AssertUsedAssemblyReferences(source4, references:={comp6ImageRef, comp2Ref}, expected2)

            Dim source8 =
"
public class C3
    public shared Sub Main()
        Dim x = ""a""
        Dim __ = nameof(x.M1)
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source8, comp0Ref, comp1Ref, comp2Ref)
            AssertUsedAssemblyReferences(source8, comp1Ref, comp2Ref)
            AssertUsedAssemblyReferences(source8, comp1Ref)
            AssertUsedAssemblyReferences(source8, references:={comp7Ref, comp6Ref, comp2Ref}, comp6Ref, comp2Ref)
            AssertUsedAssemblyReferences(source8, comp6Ref, comp2Ref)
            AssertUsedAssemblyReferences(source8, references:={comp7ImageRef, comp6ImageRef, comp2Ref}, comp6ImageRef, comp2Ref)
            AssertUsedAssemblyReferences(source8, comp6ImageRef, comp2Ref)
        End Sub

        <Fact()>
        Public Sub MethodReference_06()
            Dim source0 =
"
public class C0
End class"
            Dim comp0 = CreateCompilation(source0, assemblyName:="MethodReference_06_0")
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1
    public Sub M1(y as C0)
    End Sub
End Class
"
            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
"
public class C2
    Inherits C1
    public overloads Sub M1(y as String)
    End Sub
End Class
"
            Dim comp2 = CreateCompilation(source2, references:={comp1Ref})
            Dim comp2Ref = comp2.ToMetadataReference()
            Dim comp2ImageRef = comp2.EmitToImageReference()

            Dim source3 =
"
public class C3
    public shared Sub Main()
        Dim x = ""a""
        Call new C2().M1(x)
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source3, references:={comp0Ref, comp1Ref, comp2Ref}, comp0Ref, comp1Ref, comp2Ref)
            AssertUsedAssemblyReferences(source3, references:={comp0Ref, comp1ImageRef, comp2ImageRef}, comp0Ref, comp1ImageRef, comp2ImageRef)

            Dim expected =
<expected>
BC30652: Reference required to assembly 'MethodReference_06_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C0'. Add one to your project.
        Call new C2().M1(x)
             ~~~~~~~~~~~~~~
</expected>

            AssertUsedAssemblyReferences(source3, references:={comp1Ref, comp2Ref}, expected)
            AssertUsedAssemblyReferences(source3, references:={comp1ImageRef, comp2Ref}, expected)
            AssertUsedAssemblyReferences(source3, references:={comp1Ref, comp2ImageRef}, expected)
            AssertUsedAssemblyReferences(source3, references:={comp1ImageRef, comp2ImageRef}, expected)
        End Sub

        <Fact()>
        Public Sub MethodReference_07()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0, assemblyName:="MethodReference_07_0")
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1
    public Sub M1(y As String)
    End Sub
End Class
"
            Dim comp1 = CreateCompilation(source1)
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
public class C2
    Inherits C1
    public overloads Sub M1(y As C0)
    End Sub
End Class
"
            Dim comp2 = CreateCompilation(source2, references:={comp0Ref, comp1Ref})
            Dim comp2Ref = comp2.ToMetadataReference()

            Dim source3 =
"
public class C3
    public shared Sub Main()
        Dim x = ""a""
        Call new C2().M1(x)
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source3, references:={comp0Ref, comp1Ref, comp2Ref}, comp0Ref, comp1Ref, comp2Ref)

            AssertUsedAssemblyReferences(source3, references:={comp1Ref, comp2Ref},
<expected>
BC30652: Reference required to assembly 'MethodReference_07_0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C0'. Add one to your project.
        Call new C2().M1(x)
             ~~~~~~~~~~~~~~
</expected>
            )
        End Sub

        <Fact()>
        Public Sub MethodReference_08()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1
    public Function M1() As C0
        return Nothing
    End Function
End Class
"
            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            comp1.VerifyDiagnostics()

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source3 =
"
public class C3
    public shared Sub Main()
        Dim __ = nameof(C1.M1)
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source3, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source3, comp0Ref, comp1ImageRef)

            AssertUsedAssemblyReferences(source3, comp1Ref)
            AssertUsedAssemblyReferences(source3, comp1ImageRef)

            Dim source5 =
"
Imports C1

public class C3
    public shared Sub Main()
        Dim __ = nameof(M1)
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source5, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source5, comp1Ref)

            Dim source6 =
"
public class C3
    public shared Sub Main()
        Dim x = new C1()
        Dim __ = nameof(x.M1)
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source6, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source6, comp0Ref, comp1ImageRef)
            AssertUsedAssemblyReferences(source6, comp1Ref)
            AssertUsedAssemblyReferences(source6, comp1ImageRef)
        End Sub

        <Fact()>
        Public Sub MethodReference_10()
            Dim source1 =
"
public class C1
    <System.Diagnostics.Conditional(""Always"")>
    public shared Sub M1()
    End Sub
End Class
"
            Dim comp1 = CreateCompilation(source1)
            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
"
public class C2
    public Shared Sub Main()
        C1.M1()
    End Sub
End Class
"

            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source2, comp1ImageRef)
            VerifyUsedAssemblyReferences(Of SourceAssemblySymbol)(source2, comp1Ref)

            Dim source3 =
"
Imports C1

public class C2
    public Shared Sub Main()
        M1()
    End Sub
End Class
"

            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source3, comp1ImageRef)
            VerifyUsedAssemblyReferences(Of SourceAssemblySymbol)(source3, comp1Ref)
        End Sub

        <Fact()>
        Public Sub MethodReference_11()
            Dim source0 =
"
public class C0 
    Implements System.Collections.IEnumerable

    Function GetEnumerator() as System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        throw new System.NotImplementedException()
    End Function
End Class
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public Module C1
    <System.Runtime.CompilerServices.Extension>
    public Sub Add(this As C0, y As integer)
    End Sub
End Module
" + ExtensionAttributeSource

            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
public class C2
    public Shared Sub Main()
        Dim __ = new C0() From { 1 }
    End Sub
End Class
"

            CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
        End Sub

        <Fact()>
        Public Sub MethodReference_12()
            Dim source0 =
"
public class C0 
    Implements System.Collections.IEnumerable

    Function GetEnumerator() as System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        throw new System.NotImplementedException()
    End Function
End Class
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public Module C1
    <System.Runtime.CompilerServices.Extension>
    <System.Diagnostics.Conditional(""Always"")>
    public Sub Add(this As C0, y As integer)
    End Sub
End Module
" + ExtensionAttributeSource

            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
public class C2
    public Shared Sub Main()
        Dim __ = new C0() From { 1 }
    End Sub
End Class
"

            CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
        End Sub

        <Fact()>
        Public Sub MethodReference_13()
            Dim source0 =
"
Public Module Module01
    Public Sub MM1(x As Integer)
    End Sub
End Module"

            Dim comp0 = CreateCompilation(source0, targetFramework:=TargetFramework.StandardAndVBRuntime)
            Dim comp0Ref = comp0.ToMetadataReference()
            Dim comp0ImageRef = comp0.EmitToImageReference()

            Dim source1 =
"
public class C2
    public shared Function Main() As String
        return Nameof(MM1)
    End Function
End Class
"

            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0Ref)
            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0ImageRef)
        End Sub

        <Fact()>
        Public Sub MethodReference_14()
            Dim source0 =
"
Public Module Module01
    Public Sub MM1(x As Integer)
    End Sub
    Public Sub MM1(x As Long)
    End Sub
End Module"

            Dim comp0 = CreateCompilation(source0, targetFramework:=TargetFramework.StandardAndVBRuntime)
            Dim comp0Ref = comp0.ToMetadataReference()
            Dim comp0ImageRef = comp0.EmitToImageReference()

            Dim source1 =
"
public class C2
    public shared Function Main() As String
        return Nameof(MM1)
    End Function
End Class
"

            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0Ref)
            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0ImageRef)
        End Sub

        <Fact()>
        Public Sub FieldDeclaration_01()
            Dim source1 =
"
namespace N1
    public class C1
        public class C11
        End Class
    End Class
End Namespace
"
            Dim comp1 = CreateCompilation(source1)

            Dim comp1Ref = comp1.ToMetadataReference()
            VerifyUsedAssemblyReferences(comp1Ref,
"
public class C2
    public Shared F1 As N1.C1.C11 = Nothing
End Class
")
            VerifyUsedAssemblyReferences(comp1Ref,
"
Imports N2 = N1
public class C2
    public Shared F1 As N2.C1.C11 = Nothing
End Class
")
            VerifyUsedAssemblyReferences(comp1Ref,
"
Imports N1
public class C2
    public Shared F1 As C1.C11 = Nothing
End Class
")
            VerifyUsedAssemblyReferences(comp1Ref,
"
Imports N1.C1
public class C2
    public Shared F1 As C11 = Nothing
End Class
")
            VerifyUsedAssemblyReferences(comp1Ref,
"
Imports C111 = N1.C1.C11
public class C2
    public Shared F1 As C111 = Nothing
End Class
")
        End Sub

        <Fact()>
        Public Sub UnusedImports_01()
            Dim source1 =
"
namespace N1
    public class C1
    End Class
End Namespace
"
            Dim comp1 = CreateCompilation(source1)
            Dim comp1Ref = comp1.ToMetadataReference()

            Verify_UnusedImports_01(comp1Ref,
"
Imports N1

public class C2
End Class
",
<expected>
BC50001: Unused import statement.
Imports N1
~~~~~~~~~~
</expected>)

            Verify_UnusedImports_01(comp1Ref,
"
Imports N1.C1

public class C2
End Class
",
<expected>
BC50001: Unused import statement.
Imports N1.C1
~~~~~~~~~~~~~
</expected>)

            Verify_UnusedImports_01(comp1Ref,
"
Imports <xmlns:db=""http://example.org/database"">

public class C2
End Class
",
<expected><![CDATA[
BC50001: Unused import statement.
Imports <xmlns:db="http://example.org/database">
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

            Verify_UnusedImports_01(comp1Ref,
"
Imports alias1 = N1.C1

public class C2
End Class
",
<expected>
BC50001: Unused import statement.
Imports alias1 = N1.C1
~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Verify_UnusedImports_01(comp1Ref,
"
Imports alias1 = N1

public class C2
End Class
",
<expected>
BC50001: Unused import statement.
Imports alias1 = N1
~~~~~~~~~~~~~~~~~~~
</expected>)

            Verify_UnusedImports_01(comp1Ref,
"
public class C2
End Class
",
<expected/>,
                GlobalImport.Parse({"N1"}),
<expected>
BC40057: Namespace or type specified in the project-level Imports 'N1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
</expected>)

            Verify_UnusedImports_01(comp1Ref,
"
public class C2
End Class
",
<expected/>,
                GlobalImport.Parse({"N1.C1"}),
<expected>
BC40057: Namespace or type specified in the project-level Imports 'N1.C1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
</expected>)

            Verify_UnusedImports_01(comp1Ref,
"
public class C2
End Class
",
<expected/>,
                GlobalImport.Parse({"<xmlns:db=""http://example.org/database"">"}))

            Verify_UnusedImports_01(comp1Ref,
"
public class C2
End Class
",
<expected/>,
                GlobalImport.Parse({"alias1 = N1.C1"}),
<expected>
BC40057: Namespace or type specified in the project-level Imports 'alias1 = N1.C1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
</expected>)

            Verify_UnusedImports_01(comp1Ref,
"
public class C2
End Class
",
<expected/>,
                GlobalImport.Parse({"alias1 = N1"}),
<expected>
BC40057: Namespace or type specified in the project-level Imports 'alias1 = N1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
</expected>)

        End Sub

        Private Shared Sub Verify_UnusedImports_01(
            reference As MetadataReference,
            source As String,
            expected1 As XElement,
            Optional globalImports As IEnumerable(Of GlobalImport) = Nothing,
            Optional expected2 As XElement = Nothing
        )
            Dim comp As Compilation = CreateCompilation(source, references:={reference}, options:=TestOptions.DebugDll.WithGlobalImports(globalImports))
            comp.AssertTheseDiagnostics(expected1, suppressInfos:=False)

            Assert.True(comp.References.Count() > 1)

            Dim used = comp.GetUsedAssemblyReferences()

            If globalImports Is Nothing OrElse expected2 Is Nothing Then
                Assert.Equal(1, used.Length)
            Else
                Dim comp2 = comp.RemoveReferences(reference)
                comp2.AssertTheseDiagnostics(expected2, suppressInfos:=False)

                Assert.Equal(2, used.Length)
                Assert.Same(reference, used(1))
            End If

            Assert.Same(comp.ObjectType.ContainingAssembly, comp.GetAssemblyOrModuleSymbol(used(0)))
        End Sub

        <Fact()>
        Public Sub MethodDeclaration_01()
            Dim source1 =
"
namespace N1
    public class C1
        public class C11
        End Class
    End Class
End Namespace
"
            Dim comp1 = CreateCompilation(source1)

            Dim comp1Ref = comp1.ToMetadataReference()
            VerifyUsedAssemblyReferences(comp1Ref,
"
public class C2
    public shared Function M1() As N1.C1.C11
        Return Nothing
    End Function
End Class
")
            VerifyUsedAssemblyReferences(comp1Ref,
"
Imports N2 = N1
public class C2
    public shared Function M1() As N2.C1.C11
        Return Nothing
    End Function
End Class
")
            VerifyUsedAssemblyReferences(comp1Ref,
"
Imports N1
public class C2
    public shared Function M1() As C1.C11
        Return Nothing
    End Function
End Class
")
            VerifyUsedAssemblyReferences(comp1Ref,
"
Imports N1.C1
public class C2
    public shared Function M1() As C11
        Return Nothing
    End Function
End Class
")
            VerifyUsedAssemblyReferences(comp1Ref,
"
Imports C111 = N1.C1.C11
public class C2
    public shared Function M1() As C111
        Return Nothing
    End Function
End Class
")
        End Sub

        <Fact()>
        Public Sub NoPia_01()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface ITest33
End Interface
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1
    public Shared F0 As ITest33 = Nothing
    public Shared F1 As Integer = 0
End Class
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
"
public class C2
    public Shared Sub Main()
        Dim __ = C1.F1
    End Sub
End Class
"

            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source2, comp0ImageRef, comp1ImageRef)
            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source2, comp0Ref, comp1ImageRef)
            VerifyUsedAssemblyReferences(Of RetargetingAssemblySymbol)(source2, comp0Ref, comp1Ref)
            VerifyUsedAssemblyReferences(Of RetargetingAssemblySymbol)(source2, comp0ImageRef, comp1Ref)

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source2, comp3ImageRef, comp1ImageRef)
            VerifyUsedAssemblyReferences(Of PEAssemblySymbol)(source2, comp3Ref, comp1ImageRef)
            VerifyUsedAssemblyReferences(Of RetargetingAssemblySymbol)(source2, comp3Ref, comp1Ref)
            VerifyUsedAssemblyReferences(Of RetargetingAssemblySymbol)(source2, comp3ImageRef, comp1Ref)

        End Sub

        <Fact()>
        Public Sub NoPia_02()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface ITest33
End Interface
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1
    public shared F0 As ITest33 = Nothing
End Class
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            Dim verify =
                Sub(source2 As String)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1Ref)

                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1Ref)
                End Sub

            verify(
"
public class C2
    public Shared Sub Main()
        Dim __ = C1.F0
    End Sub
End Class
")
            verify(
"
public class C2
    public Shared Sub Main()
        Dim __ = nameof(C1.F0)
    End Sub
End Class
")
            verify(
"
public class C2
    ''' <summary>
    ''' <see cref=""C1.F0""/>
    ''' </summary>
    public Shared Sub Main()
    End Sub
End Class
")
        End Sub

        <Fact()>
        Public Sub NoPia_03()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface ITest33
End Interface
"

            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1
    public shared Function M0() as ITest33
        Return Nothing
    End Function
End Class
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            Dim verify =
                Sub(source2 As String)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1Ref)

                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1Ref)
                End Sub

            verify(
"
public class C2
    public shared sub Main()
        C1.M0()
    End Sub
End Class
")

            verify(
"
public class C2
    public shared sub Main()
        Dim __ = nameof(C1.M0)
    End Sub
End Class
")

            verify(
"
public class C2
    ''' <summary>
    ''' <see cref=""C1.M0""/>
    ''' </summary>
    public shared sub Main()
    End Sub
End Class
")

        End Sub

        <Fact()>
        Public Sub NoPia_04()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface ITest33
End Interface
"

            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1
    public shared Function M0(x as ITest33) as object
        Return Nothing
    End Function
End Class
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            Dim verify =
                Sub(source2 As String)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1Ref)

                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1Ref)
                End Sub

            verify(
"
public class C2
    public shared sub Main()
        C1.M0(Nothing)
    End Sub
End Class
")

            verify(
"
public class C2
    public shared sub Main()
        Dim __ = nameof(C1.M0)
    End Sub
End Class
")

            verify(
"
public class C2
    ''' <summary>
    ''' <see cref=""C1.M0""/>
    ''' </summary>
    public shared sub Main()
    End Sub
End Class
")

        End Sub

        <Fact()>
        Public Sub NoPia_05()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

Public Delegate Sub DTest33()
"

            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1
    public shared event E0 as DTest33
End Class
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            Dim verify =
                Sub(source2 As String)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1Ref)

                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1Ref)
                End Sub

            verify(
"
public class C2
    public shared sub Main()
        AddHandler C1.E0, AddressOf Main
    End Sub
End Class
")

            verify(
"
public class C2
    public shared sub Main()
        Dim __ = nameof(C1.E0)
    End Sub
End Class
")

            verify(
"
public class C2
    ''' <summary>
    ''' <see cref=""C1.E0""/>
    ''' </summary>
    public shared sub Main()
    End Sub
End Class
")

        End Sub

        <Fact()>
        Public Sub NoPia_06()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface ITest33
End Interface
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1
    public shared Property P0 As ITest33
End Class
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            Dim verify =
                Sub(source2 As String)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1Ref)

                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1Ref)
                End Sub

            verify(
"
public class C2
    public Shared Sub Main()
        Dim __ = C1.P0
    End Sub
End Class
")

            verify(
"
public class C2
    public Shared Sub Main()
        C1.P0 = Nothing
    End Sub
End Class
")
            verify(
"
public class C2
    public Shared Sub Main()
        Dim __ = nameof(C1.P0)
    End Sub
End Class
")
            verify(
"
public class C2
    ''' <summary>
    ''' <see cref=""C1.P0""/>
    ''' </summary>
    public Shared Sub Main()
    End Sub
End Class
")
        End Sub

        <Fact()>
        Public Sub NoPia_07()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface ITest33
End Interface
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1
    public Default Property P0(x As ITest33) as Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            Dim verify =
                Sub(source2 As String)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1Ref)

                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1Ref)
                End Sub

            verify(
"
public class C2
    public Shared Sub Main()
        Dim __ = new C1().P0(Nothing)
    End Sub
End Class
")

            verify(
"
public class C2
    public Shared Sub Main()
        Dim __ = (new C1())(Nothing)
    End Sub
End Class
")

            verify(
"
public class C2
    public Shared Sub Main()
        Dim x as New C1()
        x.P0(Nothing) = Nothing
    End Sub
End Class
")

            verify(
"
public class C2
    public Shared Sub Main()
        Dim x as New C1()
        x(Nothing) = Nothing
    End Sub
End Class
")
            verify(
"
public class C2
    public Shared Sub Main()
        Dim __ = nameof(C1.P0)
    End Sub
End Class
")
            verify(
"
public class C2
    public Shared Sub Main()
        Dim x as New C1()
        Dim __ = nameof(x.P0)
    End Sub
End Class
")
            verify(
"
public class C2
    ''' <summary>
    ''' <see cref=""C1.P0""/>
    ''' </summary>
    public Shared Sub Main()
    End Sub
End Class
")
        End Sub

        <Fact()>
        Public Sub NoPia_08()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface ITest33
End Interface
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1 
    Implements ITest33, I1
End Class

public interface I1
End Interface

public class C2 
    Implements I2(Of ITest33), I1
End Class

public class C3 
    Inherits C2
End Class

public interface I2(Of Out T)
End Interface

public interface I3 
    Inherits ITest33, I1
End Interface

public interface I4 
    Inherits I3
End Interface

public structure S1 
    Implements ITest33, I1
End Structure
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            Dim verify =
                Sub(source2 As String)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp0ImageRef, comp1Ref)

                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, comp3Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, comp3ImageRef, comp1Ref)
                End Sub

            verify(
"
public class C
    public shared Sub Main()
        Dim __ = DirectCast(new C2(), I2(Of object))
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim __ = DirectCast(new C2(), I1)
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim __ = DirectCast(new C1(), I1)
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x as I2(Of object) = new C2()
        Dim __ = x
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I1 = new C2()
        Dim __ = x
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I1 = new C1()
        Dim __ = x
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim __ = DirectCast(new C3(), I2(Of object))
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I2(Of object) = new C3()
        Dim __ = x
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I3 = Nothing
        Dim y As I1 = x
        Dim __ = y
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I4 = Nothing
        Dim y As I1 = x
        Dim __ = y
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I(Of C1()) = Nothing
        Dim y As I(Of I1()) = x
        Dim __ = y
    End Sub
End Class

interface I(Of Out T)
End Interface
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I(Of C1) = Nothing
        Dim y As I(Of I1) = x
        Dim __ = y
    End Sub
End Class

interface I(Of out T)
End Interface
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I(Of I1)()= new I(Of C1)(10) {}
        Dim __ = x
    End Sub
End Class

interface I(Of out T)
End Interface
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I1() = new C1(10) {}
        Dim __ = x
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As S1? = Nothing
        Dim y As I1 = x
        Dim __ = y
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main(Of T As C1)()
        Dim x As T = Nothing
        Dim y As I1 = x
        Dim __ = y
    End Sub
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As I1 = new A()
        Dim __ = x
    End Sub
End Class

class A 
    Inherits C1
End Class
")

            verify(
"
public class C
    public shared Sub Main()
        Dim x As IA = Nothing
        Dim y As I1 = x
        Dim __ = y
    End Sub
End Class

interface IA 
    Inherits I3
End Interface
")
        End Sub

        <Fact()>
        Public Sub NoPia_09()
            Dim source0 =
"
Imports System.Runtime.InteropServices

<assembly: PrimaryInteropAssemblyAttribute(1,1)>
<assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")>

<ComImport()>
<Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")>
public interface ITest33
End Interface
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()

            Dim comp0Ref = comp0.ToMetadataReference(embedInteropTypes:=True)
            Dim comp0ImageRef = comp0.EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
"
public class C1 
    Implements ITest33, I1
End Class

public interface I1
End Interface

public interface I3 
    Inherits ITest33, I1
End Interface

public interface I4
    Inherits I3
End Interface

public class C2
    public shared Sub M1(Of T As ITest33)
    End Sub
    public shared Sub M2(Of T As I3)
    End Sub
    public shared Sub M3(Of T As C1)
    End Sub
    public shared Sub M4(Of T As I1)
    End Sub
End Class

public class C3(Of T As I1)
End Class

public class C4(Of T As I1)
    public Shared Sub M5()
    End Sub
End Class
"
            Dim comp1 = AssertUsedAssemblyReferences(source1, {comp0Ref})

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim comp3 = CreateCompilation(source0)
            Dim comp3Ref = comp3.ToMetadataReference(embedInteropTypes:=False)
            Dim comp3ImageRef = comp3.EmitToImageReference(embedInteropTypes:=False)

            Dim verifyUsedAssemblyReferences =
                Sub(source2 As String, globalImports As IEnumerable(Of GlobalImport))
                    Dim options = TestOptions.DebugDll.WithGlobalImports(globalImports)
                    CompileWithUsedAssemblyReferences(source2, options, comp0ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, options, comp0Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, options, comp0Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, options, comp0ImageRef, comp1Ref)

                    CompileWithUsedAssemblyReferences(source2, options, comp3ImageRef, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, options, comp3Ref, comp1ImageRef)
                    CompileWithUsedAssemblyReferences(source2, options, comp3Ref, comp1Ref)
                    CompileWithUsedAssemblyReferences(source2, options, comp3ImageRef, comp1Ref)
                End Sub

            Dim verifyNotUsedHelper =
                Sub(source2 As String, ref0 As MetadataReference, ref1 As MetadataReference)
                    Dim used = CreateCompilation(source2, references:={ref0, ref1}).GetUsedAssemblyReferences()
                    Assert.DoesNotContain(ref0, used)
                    Assert.DoesNotContain(ref1, used)
                End Sub

            Dim verifyNotUsed =
                Sub(source2 As String)
                    verifyNotUsedHelper(source2, comp0ImageRef, comp1ImageRef)
                    verifyNotUsedHelper(source2, comp0Ref, comp1ImageRef)
                    verifyNotUsedHelper(source2, comp0Ref, comp1Ref)
                    verifyNotUsedHelper(source2, comp0ImageRef, comp1Ref)

                    verifyNotUsedHelper(source2, comp3ImageRef, comp1ImageRef)
                    verifyNotUsedHelper(source2, comp3Ref, comp1ImageRef)
                    verifyNotUsedHelper(source2, comp3Ref, comp1Ref)
                    verifyNotUsedHelper(source2, comp3ImageRef, comp1Ref)
                End Sub

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
        C2.M4(Of I3)()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
        C2.M4(Of C1)()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
        C2.M3(Of C1)()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
        C2.M2(Of I4)()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
        C2.M2(Of I3)()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
        C2.M1(Of I4)()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
        C2.M1(Of I3)()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
        C2.M1(Of C1)()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C 
    Implements I3
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C 
    Inherits C1
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
interface IA 
    Inherits I3
End Interface
",
                Nothing)

            verifyUsedAssemblyReferences(
"
Imports C4(Of I3)

public class C
    shared Sub Main()
        M5()
    End Sub
End Class
",
                Nothing)

            verifyNotUsed(
"
Imports C4(Of I3)

public class C
    shared Sub Main()
    End Sub
End Class
")

            verifyNotUsed(
"
Imports alias1 = C3(Of I3)

public class C
    shared Sub Main()
    End Sub
End Class
")

            verifyUsedAssemblyReferences(
"
Imports alias1 = C3(Of I3)

public class C
    shared Sub Main()
        Dim __ = new alias1()
    End Sub
End Class
",
                Nothing)

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
    End Sub
End Class
",
                GlobalImport.Parse({"C4(Of I3)"}))

            verifyUsedAssemblyReferences(
"
public class C
    shared Sub Main()
    End Sub
End Class
",
                GlobalImport.Parse({"alias1 = C3(Of I3)"}))
        End Sub

        <Fact()>
        Public Sub Arrays_01()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1(Of T)
    public enum E1
        F1 = 0
    End Enum
End Class

public structure S(Of T)
End Structure
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
public class C2
    public Shared Sub Main()
        Dim __ = C1(Of S(Of C0)()).E1.F1 + 1
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(OF S(Of C0)())
public class C2
    public Shared Sub Main()
        Dim __ = E1.F1 + 1
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(Of S(Of C0)()).E1
public class C2
    public Shared Sub Main()
        Dim __ = F1 + 1
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of S(Of C0)()).E1
public class C2
    public Shared Sub Main()
        Dim __ = alias1.F1 + 1
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of S(Of C0)())
public class C2
    public Shared Sub Main()
        Dim __ = alias1.E1.F1 + 1
    End Sub
End Class
")

        End Sub

        <Fact()>
        Public Sub TypeReference_01()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1(Of T)
    public enum E1
        F1 = 0
    End Enum
End Class
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
public class C2
    public Shared Sub Main()
        Dim __ = nameof(C1(Of C0).E1)
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0)
public class C2
    public Shared Sub Main()
        Dim __ = nameof(E1)
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0).E1
public class C2
    public Shared Sub Main()
        Dim __ = nameof(alias1)
    End Sub
End Class
")

            VerifyUsedAssemblyReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0)
public class C2
    public Shared Sub Main()
        Dim __ = nameof(alias1.E1)
    End Sub
End Class
")

        End Sub

        <Fact()>
        Public Sub TypeReference_02()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1(Of T)
    public enum E1
        F1 = 0
    End Enum
End Class
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            VerifyCrefReferences(comp0Ref, comp1Ref,
"
class C2
    ''' <summary>
    ''' <see cref=""C1(Of C0).E1""/>
    ''' </summary>
    shared Sub Main()
    End Sub
End Class
",
                hasTypeReferencesInImports:=False)

            VerifyCrefReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0)
class C2
    ''' <summary>
    ''' <see cref=""E1""/>
    ''' </summary>
    shared Sub Main()
    End Sub
End Class
")

            VerifyCrefReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0).E1
class C2
    ''' <summary>
    ''' <see cref=""alias1""/>
    ''' </summary>
    shared Sub Main()
    End Sub
End Class
")

            VerifyCrefReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0)
class C2
    ''' <summary>
    ''' <see cref=""alias1.E1""/>
    ''' </summary>
    shared Sub Main()
    End Sub
End Class
")

        End Sub

        <Fact()>
        Public Sub TypeReference_03()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1(Of T)
    public enum E1
        F1 = 0
    End Enum
End Class
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim verifyCrefReferences =
                Sub(reference0 As MetadataReference, reference1 As MetadataReference, source As String, hasTypeReferencesInImports As Boolean)
                    Dim references = {reference0, reference1}
                    Dim comp2 As Compilation = CreateCompilation(source, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None))
                    AssertUsedAssemblyReferences(comp2, If(hasTypeReferencesInImports, references, {}), references)

                    Dim comp3 As Compilation = CreateCompilation(source, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse))
                    AssertUsedAssemblyReferences(comp3, references)
                End Sub

            verifyCrefReferences(comp0Ref, comp1Ref,
"
class C2
    ''' <summary>
    ''' <see cref=""M(C1(Of C0).E1)""/>
    ''' </summary>
    shared Sub Main()
    End Sub

    Sub M(x as Integer)
    End Sub
End Class
",
                hasTypeReferencesInImports:=False)

            verifyCrefReferences(comp0Ref, comp1Ref,
"
Imports C1(Of C0)
class C2
    ''' <summary>
    ''' <see cref=""M(E1)""/>
    ''' </summary>
    shared Sub Main()
    End Sub

    Sub M(x as Integer)
    End Sub
End Class
",
                hasTypeReferencesInImports:=True)

            verifyCrefReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0).E1
class C2
    ''' <summary>
    ''' <see cref=""M(alias1)""/>
    ''' </summary>
    shared Sub Main()
    End Sub

    Sub M(x as Integer)
    End Sub
End Class
",
                hasTypeReferencesInImports:=True)

            verifyCrefReferences(comp0Ref, comp1Ref,
"
Imports alias1 = C1(Of C0)
class C2
    ''' <summary>
    ''' <see cref=""M(alias1.E1)""/>
    ''' </summary>
    shared Sub Main()
    End Sub

    Sub M(x as Integer)
    End Sub
End Class
",
                hasTypeReferencesInImports:=True)

        End Sub

        <Fact()>
        Public Sub TypeReference_04()
            Dim source0 =
"
public class C0
    public class C1
        public Shared Sub M1()
        End Sub
    End Class
End Class
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C2 
    Inherits C0
End Class
"
            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            Dim comp1Ref = comp1.ToMetadataReference()

            CompileWithUsedAssemblyReferences("
public class C3
    public Shared Sub Main()
        Dim __ = new C2.C1()
    End Sub
End Class
", comp0Ref, comp1Ref)

            CompileWithUsedAssemblyReferences("
public class C3
    Inherits C2.C1
    public Shared Sub Main()
    End Sub
End Class
", comp0Ref, comp1Ref)

            Dim used = CompileWithUsedAssemblyReferences("
public class C3
    Inherits C0
    public Shared Sub Main()
    End Sub
End Class
", comp0Ref, comp1Ref)

            Assert.DoesNotContain(comp1Ref, used)

            used = CreateCompilation("
Imports C2.C1

public class C3
    public Shared Sub Main()
    End Sub
End Class
", references:={comp0Ref, comp1Ref}).GetUsedAssemblyReferences()

            Assert.DoesNotContain(comp0Ref, used)
            Assert.DoesNotContain(comp1Ref, used)

            used = CreateCompilation("
Imports alias1 = C2.C1

public class C3
    public Shared Sub Main()
    End Sub
End Class
", references:={comp0Ref, comp1Ref}).GetUsedAssemblyReferences()

            Assert.DoesNotContain(comp0Ref, used)
            Assert.DoesNotContain(comp1Ref, used)

            CreateCompilation("
Imports C2.C1

public class C3
    public Shared Sub Main()
        M1()
    End Sub
End Class
", references:={comp0Ref, comp1Ref}).AssertTheseDiagnostics(
<expected>
BC40056: Namespace or type specified in the Imports 'C2.C1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports C2.C1
        ~~~~~
BC30451: 'M1' is not declared. It may be inaccessible due to its protection level.
        M1()
        ~~
</expected>)

            CreateCompilation("
Imports alias1 = C2.C1

public class C3
    public Shared Sub Main()
        Dim __ = new alias1()
    End Sub
End Class
", references:={comp0Ref, comp1Ref}).AssertTheseDiagnostics(
<expected>
BC40056: Namespace or type specified in the Imports 'C2.C1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports alias1 = C2.C1
                 ~~~~~
BC31208: Type or namespace 'C2.C1' is not defined.
        Dim __ = new alias1()
                     ~~~~~~
</expected>)

            used = CompileWithUsedAssemblyReferences("
public class C3
    public Shared Sub Main()
    End Sub
End Class
",
                        TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse({"C2.C1"})).
                            WithSpecificDiagnosticOptions({KeyValuePairUtil.Create("BC40057", ReportDiagnostic.Suppress)}),
                        comp0Ref, comp1Ref)
            Assert.DoesNotContain(comp0Ref, used)
            Assert.DoesNotContain(comp1Ref, used)

            used = CompileWithUsedAssemblyReferences("
public class C3
    public Shared Sub Main()
    End Sub
End Class
",
                       TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse({"alias1 = C2.C1"})).
                            WithSpecificDiagnosticOptions({KeyValuePairUtil.Create("BC40057", ReportDiagnostic.Suppress)}),
                       comp0Ref, comp1Ref)
            Assert.DoesNotContain(comp0Ref, used)
            Assert.DoesNotContain(comp1Ref, used)
        End Sub

        <Fact()>
        Public Sub NamespaceReference_01()
            Dim source0 =
"
namespace N1.N2
    public enum E0
        None
    End Enum
End Namespace
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
namespace N1.N2
    public enum E1
        None
    End Enum
End Namespace
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
namespace N1
    public enum E2
        None
    End Enum
End Namespace
"
            Dim comp2 = CreateCompilation(source2)
            comp2.VerifyDiagnostics()
            Dim comp2Ref = comp2.ToMetadataReference()

            Dim verify =
                Sub(reference0 As MetadataReference,
                    reference1 As MetadataReference,
                    reference2 As MetadataReference,
                    source As String
                )
                    AssertUsedAssemblyReferences(source, {reference0, reference1, reference2}, reference0, reference1)
                End Sub

            verify(comp0Ref, comp1Ref, comp2Ref,
"
public class C2
    public Shared Sub Main()
        Dim __ = nameof(N1.N2)
    End Sub
End Class
")

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1.N2
public class C2
    public Shared Sub Main()
        Dim __ = nameof(alias1)
    End Sub
End Class
")

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1
public class C2
    public Shared Sub Main()
        Dim __ = nameof(alias1.N2)
    End Sub
End Class
")
        End Sub

        <Fact()>
        Public Sub NamespaceReference_02()
            Dim source0 =
"
namespace N1.N2
    public enum E0
        F0
    End Enum
End Namespace
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
namespace N1.N2
    public enum E1
        None
    End Enum
End Namespace
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
namespace N1
    public enum E2
        None
    End Enum
End Namespace
"
            Dim comp2 = CreateCompilation(source2)
            comp2.VerifyDiagnostics()
            Dim comp2Ref = comp2.ToMetadataReference()

            Dim verify =
                Sub(reference0 As MetadataReference,
                    reference1 As MetadataReference,
                    reference2 As MetadataReference,
                    source As String
                )
                    AssertUsedAssemblyReferences(source, {reference0, reference1, reference2}, reference0)
                End Sub

            verify(comp0Ref, comp1Ref, comp2Ref,
"
public class C2
    public Shared Sub Main()
        Dim __ = N1.N2.E0.F0
    End Sub
End Class
")

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1.N2.E0
public class C2
    public Shared Sub Main()
        Dim __ = alias1.F0
    End Sub
End Class
")

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports N1.N2.E0
public class C2
    public Shared Sub Main()
        Dim __ = F0
    End Sub
End Class
")

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1.N2
public class C2
    public Shared Sub Main()
        Dim __ = alias1.E0.F0
    End Sub
End Class
")

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports N1.N2
public class C2
    public Shared Sub Main()
        Dim __ = E0.F0
    End Sub
End Class
")

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1
public class C2
    public Shared Sub Main()
        Dim __ = alias1.N2.E0.F0
    End Sub
End Class
")
        End Sub

        <Fact()>
        Public Sub NamespaceReference_03()
            Dim source0 =
"
namespace N1.N2
    public enum E0
        None
    End Enum
End Namespace
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
namespace N1.N2
    public enum E1
        None
    End Enum
End Namespace
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
namespace N1
    public enum E2
        None
    End Enum
End Namespace
"
            Dim comp2 = CreateCompilation(source2)
            comp2.VerifyDiagnostics()
            Dim comp2Ref = comp2.ToMetadataReference()

            Dim verify =
                Sub(reference0 As MetadataReference, reference1 As MetadataReference, reference2 As MetadataReference, source As String, namespaceOrdinalReferencedInImports As Integer)
                    Dim useReferences = {reference0, reference1, reference2}
                    Dim expected = {reference0, reference1}
                    Dim comp As Compilation = CreateCompilation(source, references:=useReferences, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None))
                    Dim compExpected As MetadataReference()
                    Select Case namespaceOrdinalReferencedInImports
                        Case 1
                            compExpected = useReferences
                        Case 2
                            compExpected = expected
                        Case Else
                            compExpected = {}
                    End Select
                    AssertUsedAssemblyReferences(comp, compExpected, useReferences)

                    Dim comp3 As Compilation = CreateCompilation(source, references:=useReferences, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse))
                    AssertUsedAssemblyReferences(comp3, expected)

                    Dim comp4 As Compilation = CreateCompilation(source, references:=useReferences, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose))
                    AssertUsedAssemblyReferences(comp4, expected)
                End Sub

            verify(comp0Ref, comp1Ref, comp2Ref,
"
class C2
    ''' <summary>
    ''' <see cref=""N1.N2""/>
    ''' </summary>
    Shared Sub Main()
    End Sub
End Class
",
                    namespaceOrdinalReferencedInImports:=0)

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1.N2
class C2
    ''' <summary>
    ''' <see cref=""alias1""/>
    ''' </summary>
    Shared Sub Main()
    End Sub
End Class
",
                namespaceOrdinalReferencedInImports:=2
                )

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1
class C2
    ''' <summary>
    ''' <see cref=""alias1.N2""/>
    ''' </summary>
    Shared Sub Main()
    End Sub
End Class
",
                namespaceOrdinalReferencedInImports:=1
                )

            Dim source3 =
"
Imports N1.N2
class C2
    Shared Sub Main()
    End Sub
End Class
"

            Dim references = {comp0Ref, comp1Ref, comp2Ref}
            AssertUsedAssemblyReferences(CreateCompilation(source3, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)),
                                         comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(CreateCompilation(source3, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse)),
                                         {},
<expected>
BC50001: Unused import statement.
Imports N1.N2
~~~~~~~~~~~~~
</expected>,
<expected>
BC50001: Unused import statement.
Imports N1.N2
~~~~~~~~~~~~~
BC40056: Namespace or type specified in the Imports 'N1.N2' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports N1.N2
        ~~~~~
</expected>,
                                         specificReferencesToAssert:=Nothing)

            Dim source4 =
"
Imports N1
class C2
    Shared Sub Main()
    End Sub
End Class
"

            AssertUsedAssemblyReferences(CreateCompilation(source4, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)),
                                         references)
            AssertUsedAssemblyReferences(CreateCompilation(source4, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse)),
                                         {},
<expected>
BC50001: Unused import statement.
Imports N1
~~~~~~~~~~
</expected>,
<expected>
BC50001: Unused import statement.
Imports N1
~~~~~~~~~~
BC40056: Namespace or type specified in the Imports 'N1' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports N1
        ~~
</expected>,
                                         specificReferencesToAssert:=Nothing)

            Dim source5 =
"
class C2
    Shared Sub Main1()
    End Sub
End Class
"

            AssertUsedAssemblyReferences(CreateCompilation(source5, references:=references,
                                                           parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None),
                                                           options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse({"N1.N2"}))),
                                         comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(CreateCompilation(source5, references:=references,
                                                           parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse),
                                                           options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse({"N1.N2"}))),
                                         comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(CreateCompilation(source5, references:=references,
                                                           parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None),
                                                           options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse({"N1"}))),
                                         references)
            AssertUsedAssemblyReferences(CreateCompilation(source5, references:=references,
                                                           parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse),
                                                           options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse({"N1"}))),
                                         references)
        End Sub

        <Fact()>
        Public Sub NamespaceReference_04()
            Dim source0 =
"
namespace N1.N2
    public enum E0
        F0
    End Enum
End Namespace
"
            Dim comp0 = CreateCompilation(source0)
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
namespace N1.N2
    public enum E1
        None
    End Enum
End Namespace
"
            Dim comp1 = CreateCompilation(source1)
            comp1.VerifyDiagnostics()
            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
namespace N1
    public enum E2
        None
    End Enum
End Namespace
"
            Dim comp2 = CreateCompilation(source2)
            comp2.VerifyDiagnostics()
            Dim comp2Ref = comp2.ToMetadataReference()

            Dim verify =
                Sub(reference0 As MetadataReference, reference1 As MetadataReference, reference2 As MetadataReference, source As String, namespaceOrdinalReferencedInImports As Integer)
                    Dim useReferences = {reference0, reference1, reference2}
                    Dim comp As Compilation = CreateCompilation(source, references:=useReferences, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None))

                    Dim expected1 As MetadataReference()
                    Select Case namespaceOrdinalReferencedInImports
                        Case 1
                            expected1 = useReferences
                        Case 2
                            expected1 = {reference0, reference1}
                        Case 3
                            expected1 = {reference0}
                        Case Else
                            expected1 = {}
                    End Select

                    AssertUsedAssemblyReferences(comp, expected1, useReferences)

                    Dim comp3 As Compilation = CreateCompilation(source, references:=useReferences, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse))
                    AssertUsedAssemblyReferences(comp3, {reference0}, useReferences)

                    Dim comp4 As Compilation = CreateCompilation(source, references:=useReferences, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose))
                    AssertUsedAssemblyReferences(comp4, {reference0}, useReferences)
                End Sub

            verify(comp0Ref, comp1Ref, comp2Ref,
"
class C2
    ''' <summary>
    ''' <see cref=""N1.N2.E0""/>
    ''' </summary>
    Shared Sub Main()
    End Sub
End Class
",
                namespaceOrdinalReferencedInImports:=0
                )

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1.N2
class C2
    ''' <summary>
    ''' <see cref=""alias1.E0""/>
    ''' </summary>
    Shared Sub Main()
    End Sub
End Class
",
                namespaceOrdinalReferencedInImports:=2
                )

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1.N2.E0
class C2
    ''' <summary>
    ''' <see cref=""alias1""/>
    ''' </summary>
    Shared Sub Main()
    End Sub
End Class
",
                namespaceOrdinalReferencedInImports:=3
                )

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports N1.N2.E0
class C2
    ''' <summary>
    ''' <see cref=""F0""/>
    ''' </summary>
    Shared Sub Main()
    End Sub
End Class
",
                namespaceOrdinalReferencedInImports:=3
                )

            verify(comp0Ref, comp1Ref, comp2Ref,
"
Imports alias1 = N1
class C2
    ''' <summary>
    ''' <see cref=""alias1.N2.E0""/>
    ''' </summary>
    Shared Sub Main()
    End Sub
End Class
",
                namespaceOrdinalReferencedInImports:=1
                )

            Dim source3 =
"
Imports N1.N2.E0
class C2
    Shared Sub Main()
    End Sub
End Class
"

            Dim references = {comp0Ref, comp1Ref, comp2Ref}
            AssertUsedAssemblyReferences(CreateCompilation(source3, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)),
                                         {comp0Ref}, references)
            AssertUsedAssemblyReferences(CreateCompilation(source3, references:=references, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse)),
                                         {},
<expected>
BC50001: Unused import statement.
Imports N1.N2.E0
~~~~~~~~~~~~~~~~
</expected>,
<expected>
BC50001: Unused import statement.
Imports N1.N2.E0
~~~~~~~~~~~~~~~~
BC40056: Namespace or type specified in the Imports 'N1.N2.E0' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports N1.N2.E0
        ~~~~~~~~
</expected>,
                                         specificReferencesToAssert:=Nothing)

            Dim source5 =
"
class C2
    Shared Sub Main1()
    End Sub
End Class
"

            AssertUsedAssemblyReferences(CreateCompilation(source5, references:=references,
                                                           parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None),
                                                           options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse({"N1.N2.E0"}))),
                                         {comp0Ref}, references)
            AssertUsedAssemblyReferences(CreateCompilation(source5, references:=references,
                                                           parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse),
                                                           options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse({"N1.N2.E0"}))),
                                         {comp0Ref}, references)
        End Sub

        <Fact()>
        Public Sub NamespaceReference_05()
            Dim source1 =
"
Imports Global
class C2
    Shared Sub Main()
    End Sub
End Class
"

            CreateCompilation(source1, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)).AssertTheseDiagnostics(
<expected>
BC36001: 'Global' not allowed in this context; identifier expected.
Imports Global
        ~~~~~~
</expected>
            )

            Dim source2 =
"
Imports alias1 = Global
class C2
    Shared Sub Main()
    End Sub
End Class
"

            CreateCompilation(source2, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None)).AssertTheseDiagnostics(
<expected>
BC36001: 'Global' not allowed in this context; identifier expected.
Imports alias1 = Global
                 ~~~~~~
</expected>
            )

            Dim source3 =
"
class C2
    Shared Sub Main()
        Dim __ = nameof(global)
    End Sub
End Class
"

            CreateCompilation(source3).AssertTheseDiagnostics(
<expected>
BC36000: 'Global' must be followed by '.' and an identifier.
        Dim __ = nameof(global)
                        ~~~~~~
BC37244: This expression does not have a name.
        Dim __ = nameof(global)
                        ~~~~~~
</expected>
            )

            Assert.Throws(Of System.ArgumentException)(Sub() GlobalImport.Parse({"global"})) ' System.ArgumentException : Error in project-level import 'global' at 'global' : 'Global' not allowed in this context; identifier expected.
        End Sub

        <Fact()>
        Public Sub EventReference_01()
            Dim source0 =
"
public delegate Sub D0()
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1
    public shared event E1 as D0

    Sub Use()
        RaiseEvent E1()
    End Sub
End Class
"
            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            comp1.VerifyDiagnostics()

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
"
public class C2
    public Shared Sub Main()
        AddHandler C1.E1, Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)

            Dim source3 =
"
public class C3
    public Shared Sub Main()
        RemoveHandler C1.E1, Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source3, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source3, comp0Ref, comp1ImageRef)

            Dim source4 =
"
Imports C1

public class C2
    public Shared Sub Main()
        AddHandler E1, Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source4, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source4, comp0Ref, comp1ImageRef)

            Dim source5 =
"
Imports C1

public class C3
    public Shared Sub Main()
        RemoveHandler E1, Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source5, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source5, comp0Ref, comp1ImageRef)
        End Sub

        <Fact()>
        Public Sub EventReference_02()
            Dim source0 =
"
public delegate Sub D0()
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1
    public event E1 as D0

    Sub Use()
        RaiseEvent E1()
    End Sub
End Class
"
            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            comp1.VerifyDiagnostics()

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
"
public class C2
    public Shared Sub Main(x as C1)
        AddHandler x.E1, Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)

            Dim source3 =
"
public class C3
    public Shared Sub Main(x as C1)
        RemoveHandler x.E1, Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source3, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source3, comp0Ref, comp1ImageRef)
        End Sub

        <Fact()>
        Public Sub EventReference_03()
            Dim source0 =
"
Public Module Module01
    Public Event EE1 As System.Action
End Module"

            Dim comp0 = CreateCompilation(source0, targetFramework:=TargetFramework.StandardAndVBRuntime)
            Dim comp0Ref = comp0.ToMetadataReference()
            Dim comp0ImageRef = comp0.EmitToImageReference()

            Dim source1 =
"
public class C2
    public shared Function Main() As String
        return Nameof(EE1)
    End Function
End Class
"

            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0Ref)
            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0ImageRef)
        End Sub

        <Fact()>
        Public Sub PropertyReference_01()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1
    public shared Property P1 As C0
End Class
"
            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            comp1.VerifyDiagnostics()

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
"
public class C2
    public Shared Sub Main()
        C1.P1 = Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)

            Dim source3 =
"
public class C3
    public Shared Sub Main()
        Dim __ = C1.P1
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source3, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source3, comp0Ref, comp1ImageRef)

            Dim source4 =
"
Imports C1

public class C2
    public Shared Sub Main()
        P1 = Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source4, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source4, comp0Ref, comp1ImageRef)

            Dim source5 =
"
Imports C1

public class C3
    public Shared Sub Main()
        Dim __ = P1
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source5, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source5, comp0Ref, comp1ImageRef)
        End Sub

        <Fact()>
        Public Sub PropertyReference_02()
            Dim source0 =
"
public class C0
End Class
"
            Dim comp0 = CreateCompilation(source0)
            comp0.VerifyDiagnostics()
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
public class C1
    public Property P1 As C0
End Class
"
            Dim comp1 = CreateCompilation(source1, references:={comp0Ref})
            comp1.VerifyDiagnostics()

            Dim comp1Ref = comp1.ToMetadataReference()
            Dim comp1ImageRef = comp1.EmitToImageReference()

            Dim source2 =
"
public class C2
    public Shared Sub Main(x as C1)
        x.P1 = Nothing
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source2, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source2, comp0Ref, comp1ImageRef)

            Dim source3 =
"
public class C3
    public Shared Sub Main(x as C1)
        Dim __ = x.P1
    End Sub
End Class
"

            AssertUsedAssemblyReferences(source3, comp0Ref, comp1Ref)
            AssertUsedAssemblyReferences(source3, comp0Ref, comp1ImageRef)
        End Sub

        <Fact()>
        Public Sub PropertyReference_03()
            Dim source0 =
"
Public Module Module01
    Public Property PP1 As Integer
End Module"

            Dim comp0 = CreateCompilation(source0, targetFramework:=TargetFramework.StandardAndVBRuntime)
            Dim comp0Ref = comp0.ToMetadataReference()
            Dim comp0ImageRef = comp0.EmitToImageReference()

            Dim source1 =
"
public class C2
    public shared Function Main() As String
        return Nameof(PP1)
    End Function
End Class
"

            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0Ref)
            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0ImageRef)
        End Sub

        <Fact()>
        Public Sub PropertyReference_04()
            Dim source0 =
"
Public Module Module01
    Public Property PP1(x As Integer) As Long
        Get
            Return 0
        End Get
        Set(value As Long)

        End Set
    End Property

    Public Property PP1(x As Long) As Long
        Get
            Return 0
        End Get
        Set(value As Long)

        End Set
    End Property

End Module"

            Dim comp0 = CreateCompilation(source0, targetFramework:=TargetFramework.StandardAndVBRuntime)
            Dim comp0Ref = comp0.ToMetadataReference()
            Dim comp0ImageRef = comp0.EmitToImageReference()

            Dim source1 =
"
public class C2
    public shared Function Main() As String
        return Nameof(PP1)
    End Function
End Class
"

            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0Ref)
            CompileWithUsedAssemblyReferences(source1, TargetFramework.StandardAndVBRuntime, comp0ImageRef)
        End Sub

        <Fact()>
        Public Sub WellKnownTypeReference_01()
            Dim source0 =
"
namespace System
    public class [Object]
    End Class
    public class ValueType
    End Class
    public structure Void
    End Structure
End Namespace
"
            Dim comp0 = CreateEmptyCompilation(source0)
            comp0.VerifyDiagnostics()
            Dim comp0Ref = comp0.ToMetadataReference()

            Dim source1 =
"
namespace System
    public class Type
        public shared Function GetTypeFromHandle(handle as RuntimeTypeHandle) as Type
            return Nothing
        End Function
    End Class

    public structure RuntimeTypeHandle
    End Structure
End Namespace
"
            Dim comp1 = CreateEmptyCompilation(source1, references:={comp0Ref})
            comp1.VerifyDiagnostics()

            Dim comp1Ref = comp1.ToMetadataReference()

            Dim source2 =
"
public class Type
End Class
"
            Dim comp2 = CreateEmptyCompilation(source2, references:={comp0Ref})
            comp2.VerifyDiagnostics()

            Dim comp2Ref = comp2.ToMetadataReference()

            Dim source3 =
"
public class C2
    public Shared Sub Main()
        Dim __ = GetType(C2)
    End Sub
End Class
"
            Dim references = {comp0Ref, comp1Ref, comp2Ref}
            Dim comp3 = CreateEmptyCompilation(source3, references:=references)

            AssertUsedAssemblyReferences(comp3, {comp1Ref}, references)

            Dim source4 =
"
public class C2
    public Shared Sub Main()
        Dim __ = GetType(Type)
    End Sub
End Class
"

            Dim comp4 = CreateEmptyCompilation(source4, references:={comp0Ref, comp1Ref, comp2Ref})

            AssertUsedAssemblyReferences(comp4, comp1Ref, comp2Ref)
        End Sub

        <Fact()>
        Public Sub WellKnownTypeReference_03()
            Dim source3 =
"
public class C2
    public shared Sub Main()
        Dim x = new With {.a = 1}
        x.ToString()
    End Sub
End Class
"

            CompileWithUsedAssemblyReferences(source3, targetFramework:=TargetFramework.Standard)
        End Sub

        <Fact()>
        Public Sub WellKnownTypeReference_07()
            Dim source3 =
"
public class C2
    public shared Sub Main()
        Dim x As Integer = ""123""
    End Sub
End Class
"

            CompileWithUsedAssemblyReferences(source3, targetFramework:=TargetFramework.StandardAndVBRuntime)
        End Sub

        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        <WorkItem(40033, "https://github.com/dotnet/roslyn/issues/40033")>
        Public Sub SynthesizeTupleElementNamesAttributeBasedOnInterfacesToEmit_IndirectInterfaces(ByVal useImageReferences As Boolean)

            Dim getReference As Func(Of Compilation, MetadataReference) = Function(c) If(useImageReferences, c.EmitToImageReference(), c.ToMetadataReference())

            Dim valueTuple_source = "
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub

        Public Overrides Function ToString() As String
            Return ""{"" + Item1?.ToString() + "", "" + Item2?.ToString() + ""}""
        End Function
    End Structure
End Namespace
"
            Dim valueTuple_comp = CreateCompilationWithMscorlib40(valueTuple_source)

            Dim tupleElementNamesAttribute_comp = CreateCompilationWithMscorlib40(
"
namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Field Or AttributeTargets.Parameter Or AttributeTargets.Property Or AttributeTargets.ReturnValue Or AttributeTargets.Class Or AttributeTargets.Struct )>
    public class TupleElementNamesAttribute : Inherits Attribute
        public Sub New(transformNames As String())
	    End Sub
    End Class
End Namespace
")
            tupleElementNamesAttribute_comp.AssertNoDiagnostics()

            Dim lib1_source = "
Imports System.Threading.Tasks

Public Interface I2(Of T, TResult)
    Function ExecuteAsync(parameter As T) As Task(Of TResult)
End Interface

Public Interface I1(Of T)
    Inherits I2(Of T, (a As Object, b As Object))
End Interface
"
            Dim lib1_refs = {getReference(valueTuple_comp), getReference(tupleElementNamesAttribute_comp)}
            Dim lib1_comp = CreateCompilationWithMscorlib40(lib1_source, references:=lib1_refs)
            lib1_comp.AssertNoDiagnostics()
            CompileWithUsedAssemblyReferences(lib1_comp, specificReferencesToAssert:=lib1_refs)

        End Sub

    End Class
End Namespace
