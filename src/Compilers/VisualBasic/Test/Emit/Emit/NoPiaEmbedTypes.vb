' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities
Imports System.IO
Imports System.Reflection
Imports System.Xml.Linq
Imports Xunit
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Emit
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class NoPiaEmbedTypes
        Inherits BasicTestBase

        ' See C# EmbedClass1 and EmbedClass2 tests.
        <Fact()>
        Public Sub BC31541ERR_CannotLinkClassWithNoPIA1()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
Public Class A
End Class
Public Module M
End Module
Public Structure S
End Structure
Public Delegate Sub D()
Public Enum E
    A
End Enum
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports B = A
Class C(Of T)
    Sub M(o As Object)
        Dim _1 As A = Nothing
        Dim _2 As C(Of A) = Nothing
        Dim _3 = GetType(A)
        Dim _4 = GetType(A())
        Dim _5 = GetType(B)
        Dim _6 = GetType(M)
        Dim _7 As S = Nothing
        Dim _8 As D = Nothing
        Dim _9 As E = Nothing
        M(_1)
        M(_2)
        M(_3)
        M(_4)
        M(_5)
        M(_6)
        M(_7)
        M(_8)
        M(_9)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
        Dim _1 As A = Nothing
                  ~
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
        Dim _2 As C(Of A) = Nothing
                       ~
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
        Dim _3 = GetType(A)
                         ~
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
        Dim _4 = GetType(A())
                         ~
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
        Dim _5 = GetType(B)
                         ~
BC31541: Reference to class 'M' is not allowed when its assembly is configured to embed interop types.
        Dim _6 = GetType(M)
                         ~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntime(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=False)})
            VerifyEmitDiagnostics(compilation2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={AssemblyMetadata.CreateFromImage(compilation1.EmitToArray()).GetReference(embedInteropTypes:=False)})
            VerifyEmitDiagnostics(compilation2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={AssemblyMetadata.CreateFromImage(compilation1.EmitToArray()).GetReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
        End Sub

        ' See C# EmbedClass3 test.
        <Fact()>
        Public Sub BC31541ERR_CannotLinkClassWithNoPIA1_2()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
Public Class A
End Class
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Interface I(Of T)
End Interface
Class C(Of T)
End Class
Class B1
    Inherits A
End Class
Class B2
    Inherits C(Of A)
    Implements I(Of A)
End Class
Class B3
    Inherits C(Of I(Of A))
    Implements I(Of C(Of A))
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
    Inherits A
             ~
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
    Inherits C(Of A)
                  ~
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
    Implements I(Of A)
                    ~
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
    Inherits C(Of I(Of A))
                       ~
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
    Implements I(Of C(Of A))
                         ~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
        End Sub

        <Fact()>
        Public Sub BC31541ERR_CannotLinkClassWithNoPIA1_3()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
Public Class A
End Class
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports COfA = C(Of A)
Class C(Of T)
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31541: Reference to class 'A' is not allowed when its assembly is configured to embed interop types.
Imports COfA = C(Of A)
                    ~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={AssemblyMetadata.CreateFromImage(compilation1.EmitToArray()).GetReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
        End Sub

        <Fact()>
        Public Sub BC31558ERR_InvalidInteropType()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface IA
    Interface IB
    End Interface
    Class C
    End Class
    Structure S
    End Structure
    Delegate Sub D()
    Enum E
        A
    End Enum
End Interface
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Dim _1 As IA.IB
    Dim _2 As IA.C
    Dim _3 As IA.S
    Dim _4 As IA.D
    Dim _5 As IA.E
End Module
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31558: Nested type 'IA.IB' cannot be embedded.
    Dim _1 As IA.IB
              ~~~~~
BC31541: Reference to class 'IA.C' is not allowed when its assembly is configured to embed interop types.
    Dim _2 As IA.C
              ~~~~
BC31558: Nested type 'IA.S' cannot be embedded.
    Dim _3 As IA.S
              ~~~~
BC31558: Nested type 'IA.D' cannot be embedded.
    Dim _4 As IA.D
              ~~~~
BC31558: Nested type 'IA.E' cannot be embedded.
    Dim _5 As IA.E
              ~~~~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={AssemblyMetadata.CreateFromImage(compilation1.EmitToArray()).GetReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
        End Sub

        <Fact()>
        Public Sub EmbedNestedType1()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Function F() As S1.S2
End Interface
Public Structure S1
    Structure S2
    End Structure
End Structure
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Function F1(o As I) As Object
        Dim x = o.F()
        Return x
    End Function
    Function F2(o As I) As Object
        Dim x As S1.S2 = o.F()
        Return x
    End Function
End Module
]]></file>
                           </compilation>
            Dim errors2 = <errors>
BC31558: Nested type 'S1.S2' cannot be embedded.
        Dim x = o.F()
                ~~~~~
BC31558: Nested type 'S1.S2' cannot be embedded.
        Dim x As S1.S2 = o.F()
                 ~~~~~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
        End Sub

        <Fact()>
        Public Sub EmbedNestedType2()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Function F() As S1.S2
End Interface
Public Structure S1
    Structure S2
    End Structure
End Structure
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Function F(o As I) As Object
        Return o.F()
    End Function
End Module
]]></file>
                           </compilation>
            Dim errors2 = <errors>
BC31558: Nested type 'S1.S2' cannot be embedded.
        Return o.F()
               ~~~~~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
        End Sub

        <Fact()>
        Public Sub EmbedNestedType3()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
Public Structure S1
    Structure S2
    End Structure
End Structure
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Sub M(o As S1.S2)
    End Sub
End Module
]]></file>
                           </compilation>
            Dim errors2 = <errors>
BC31558: Nested type 'S1.S2' cannot be embedded.
    Sub M(o As S1.S2)
               ~~~~~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors2)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors2)
        End Sub

        ' See C# EmbedGenericType* tests.
        <Fact()>
        Public Sub BC36923ERR_CannotEmbedInterfaceWithGeneric()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I1(Of T)
    Interface I2
    End Interface
    Class C2
    End Class
    Structure S2
    End Structure
    Enum E2
        A
    End Enum
End Interface
Public Class C1(Of T)
End Class
Public Structure S1(Of T)
End Structure
Public Delegate Sub D1(Of T)()
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Dim _1 As I1(Of Object)
    Dim _2 As I1(Of Object).I2
    Dim _3 As I1(Of Object).C2
    Dim _4 As I1(Of Object).S2
    Dim _5 As I1(Of Object).E2
    Dim _6 As C1(Of Object)
    Dim _7 As S1(Of Object) ' No error from Dev11
    Dim _8 As D1(Of Object) ' No error from Dev11
End Module
]]></file>
                           </compilation>
            Dim errors = <errors>
BC36923: Type 'I1(Of T)' cannot be embedded because it has generic argument. Consider disabling the embedding of interop types.
    Dim _1 As I1(Of Object)
              ~~~~~~~~~~~~~
BC31558: Nested type 'I1(Of T).I2' cannot be embedded.
    Dim _2 As I1(Of Object).I2
              ~~~~~~~~~~~~~~~~
BC31541: Reference to class 'I1(Of T).C2' is not allowed when its assembly is configured to embed interop types.
    Dim _3 As I1(Of Object).C2
              ~~~~~~~~~~~~~~~~
BC31558: Nested type 'I1(Of T).S2' cannot be embedded.
    Dim _4 As I1(Of Object).S2
              ~~~~~~~~~~~~~~~~
BC31558: Nested type 'I1(Of T).E2' cannot be embedded.
    Dim _5 As I1(Of Object).E2
              ~~~~~~~~~~~~~~~~
BC31541: Reference to class 'C1(Of T)' is not allowed when its assembly is configured to embed interop types.
    Dim _6 As C1(Of Object)
              ~~~~~~~~~~~~~
BC36923: Type 'S1(Of T)' cannot be embedded because it has generic argument. Consider disabling the embedding of interop types.
    Dim _7 As S1(Of Object) ' No error from Dev11
              ~~~~~~~~~~~~~
BC36923: Type 'D1(Of T)' cannot be embedded because it has generic argument. Consider disabling the embedding of interop types.
    Dim _8 As D1(Of Object) ' No error from Dev11
              ~~~~~~~~~~~~~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
        End Sub

        Private Shared Sub VerifyEmitDiagnostics(compilation As VisualBasicCompilation, Optional errors As XElement = Nothing)
            If errors Is Nothing Then
                errors = <errors/>
            End If
            Using executableStream As New MemoryStream()
                Dim result = compilation.Emit(executableStream)
                result.Diagnostics.AssertTheseDiagnostics(errors)
            End Using
        End Sub

        Private Shared Sub VerifyEmitMetadataOnlyDiagnostics(compilation As VisualBasicCompilation, Optional errors As XElement = Nothing)
            If errors Is Nothing Then
                errors = <errors/>
            End If
            Using executableStream As New MemoryStream()
                Dim result = compilation.Emit(executableStream, options:=New EmitOptions(metadataOnly:=True))
                result.Diagnostics.AssertTheseDiagnostics(errors)
            End Using
        End Sub

        ' See C# EmbedStructWith* tests.
        <Fact()>
        Public Sub BC31542ERR_InvalidStructMemberNoPIA1()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
' Public field
Public Structure S0
    Public F As Object
End Structure
' Private field
Public Structure S1
    Friend F As Object
End Structure
' Shared field
Public Structure S2
    Public Shared F As Object
End Structure
' Public method
Public Structure S3
    Public Sub F()
    End Sub
End Structure
' Public property
Public Structure S4
    Public Property P As Object
End Structure
' Public event
Public Delegate Sub D()
Public Structure S5
    Public Event E As D
End Structure
' Public type
Public Structure S6
    Public Structure T
    End Structure
End Structure
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Function F0() As Object
        Return DirectCast(Nothing, S0)
    End Function
    Function F1() As Object
        Return DirectCast(Nothing, S1)
    End Function
    Function F2() As Object
        Return DirectCast(Nothing, S2)
    End Function
    Function F3() As Object
        Return DirectCast(Nothing, S3)
    End Function
    Function F4() As Object
        Return DirectCast(Nothing, S4)
    End Function
    Function F5() As Object
        Return DirectCast(Nothing, S5)
    End Function
    Function F6() As Object
        Return DirectCast(Nothing, S6)
    End Function
End Module
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31542: Embedded interop structure 'S1' can contain only public instance fields.
        Return DirectCast(Nothing, S1)
               ~~~~~~~~~~~~~~~~~~~~~~~
BC31542: Embedded interop structure 'S2' can contain only public instance fields.
        Return DirectCast(Nothing, S2)
               ~~~~~~~~~~~~~~~~~~~~~~~
BC31542: Embedded interop structure 'S3' can contain only public instance fields.
        Return DirectCast(Nothing, S3)
               ~~~~~~~~~~~~~~~~~~~~~~~
BC31542: Embedded interop structure 'S4' can contain only public instance fields.
        Return DirectCast(Nothing, S4)
               ~~~~~~~~~~~~~~~~~~~~~~~
BC31542: Embedded interop structure 'S5' can contain only public instance fields.
        Return DirectCast(Nothing, S5)
               ~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
        End Sub

        <Fact()>
        Public Sub BC31561ERR_InteropMethodWithBody1()
            Dim sources1 = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly A
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = {string('_.dll')}
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = {string('f9c2d51d-4f44-45f0-9eda-c9d599b58257')}
}
.class public sealed D extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname instance void .ctor(object o, native int m) runtime { }
  .method public hidebysig instance void Invoke() runtime { }
  .method public hidebysig instance class [mscorlib]System.IAsyncResult BeginInvoke(class [mscorlib]System.AsyncCallback c, object o) runtime { }
  .method public hidebysig instance void EndInvoke(class [mscorlib]System.IAsyncResult r) runtime { }
  .method public static void M1() { ldnull throw }
  .method public static pinvokeimpl("A.dll" winapi) void M2() { }
  .method public instance void M3() { ldnull throw }
}
]]>.Value
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Sub M(o As D)
        D.M1()
        D.M2()
        o.M3()
    End Sub
End Module
]]></file>
                           </compilation>
            Dim reference1 = CompileIL(sources1, appendDefaultHeader:=False, embedInteropTypes:=True)
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(sources2, additionalRefs:={reference1})
            VerifyEmitDiagnostics(compilation2, <errors>
BC31561: Embedded interop method 'Sub D.M1()' contains a body.
        D.M1()
        ~~~~~~
BC31561: Embedded interop method 'Sub D.M3()' contains a body.
        D.M1()
        ~~~~~~
</errors>)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, <errors>
BC31561: Embedded interop method 'Sub D.M1()' contains a body.
BC31561: Embedded interop method 'Sub D.M3()' contains a body.
</errors>)
        End Sub

        <Fact()>
        Public Sub TypeIdentifierIsMissing1()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
Public Structure S
End Structure
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Function F() As Object
        Dim x As S = Nothing
        Return x
    End Function
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.InteropServices.TypeIdentifierAttribute..ctor' is not defined.
        Dim x As S = Nothing
                     ~~~~~~~
</errors>
            Dim compilation0 = CreateCompilationWithReferences(sources0, references:={MscorlibRef_v20})
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, <errors/>)
            Dim compilation1 = CreateCompilationWithReferences(
                sources1,
                references:={MscorlibRef_v20, New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
            compilation1 = CreateCompilationWithReferences(
                sources1,
                references:={MscorlibRef_v20, compilation0.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
        End Sub

        <Fact()>
        Public Sub TypeIdentifierIsMissing2()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Function F() As Object
        Dim y = DirectCast(Nothing, I)
        Return y
    End Function
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.InteropServices.TypeIdentifierAttribute..ctor' is not defined.
        Dim y = DirectCast(Nothing, I)
            ~
</errors>
            Dim compilation0 = CreateCompilationWithReferences(sources0, references:={MscorlibRef_v20})
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim compilation1 = CreateCompilationWithReferences(
                sources1,
                options:=TestOptions.DebugDll,
                references:={MscorlibRef_v20, New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
            compilation1 = CreateCompilationWithReferences(
                sources1,
                options:=TestOptions.DebugDll,
                references:={MscorlibRef_v20, compilation0.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
        End Sub

        <Fact()>
        Public Sub LocalTypeMetadata_Simple()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
Public Interface ITest1
End Interface

Public Structure Test2
    Implements ITest1
End Structure

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58259")>
Public Interface ITest3
    Inherits ITest1
End Interface

Public Interface ITest4
End Interface

<Serializable()>
<StructLayout(LayoutKind.Explicit, CharSet:=CharSet.Unicode, Pack:=16, Size:=64)>
Public Structure Test5
    <FieldOffset(2)>
    Public F5 As Integer
End Structure

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58260")>
Public Interface ITest6
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58261")>
Public Interface ITest7
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58262")>
Public Interface ITest8
End Interface

Public Enum Test9
    F1 = 1
    F2 = 2
End Enum

<Serializable()>
<StructLayout(LayoutKind.Sequential)>
Public Structure Test10
    <NonSerialized()>
    Public F3 As Integer
    <MarshalAs(UnmanagedType.U4)>
    Public F4 As Integer
End Structure

Public Delegate Sub Test11()

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58264")>
Public Interface ITest13
    Sub M13(x As Integer)
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58265")>
Public Interface ITest14
    Sub M14()
    WriteOnly Property P6 As Integer
    Event E4 As Action
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58266")>
Public Interface ITest15
    Inherits ITest14
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58267")>
Public Interface ITest16
    Sub M16()
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58268")>
Public Interface ITest17
    Sub M17()
    Sub _VtblGap()
    Sub M18()
    Sub _VtblGap3_2()
    Sub M19()
    Sub _VtblGap4_2()
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58269")>
Public Interface ITest18
    Sub _VtblGap3_2()
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58270")>
Public Interface ITest19
    Function M20(ByRef x As Integer, ByRef y As Integer, <[In]()> ByRef z As Integer, <[In](), Out()> ByRef u As Integer, <[Optional]()> v As Integer, Optional w As Integer = 34) As String
    Function M21(<MarshalAs(UnmanagedType.U4)> x As Integer) As <MarshalAs(UnmanagedType.LPWStr)> String
End Interface

Public Structure Test20
End Structure

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface ITest21
    <SpecialName()>
    Property P1 As Integer
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58272")>
Public Interface ITest22
    Property P2 As Integer
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58273")>
Public Interface ITest23
    ReadOnly Property P3 As Integer
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58274")>
Public Interface ITest24
    WriteOnly Property P4 As Integer
    Event E3 As Action
    Sub M27()
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58275")>
Public Interface ITest25
    <SpecialName()>
    Event E1 As Action
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58276")>
Public Interface ITest26
    Event E2 As Action
    WriteOnly Property P5 As Integer
    Sub M26()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Imports System

Class UsePia
    Shared Sub Main()
        Dim x As New Test2()
        Dim y As ITest3 = Nothing
        Console.WriteLine(x)
        Console.WriteLine(y)
        Dim x5 As New Test5()
        Console.WriteLine(x5)
    End Sub

    <MyAttribute(GetType(ITest7))>
    Sub M2(x As ITest6)
    End Sub
End Class

Class UsePia1
    Implements ITest8
End Class

Class MyAttribute
    Inherits Attribute
    Public Sub New(type As Type)
    End Sub
End Class

Class UsePia2
    Sub Test(x As Test10, x11 As Test11)
        Console.WriteLine(Test9.F1.ToString())
        Console.WriteLine(x.F4)
        Dim y As ITest17 = Nothing
        y.M17()
        y.M19()
    End Sub
End Class

Class UsePia3
    Implements ITest13
    Sub M13(x As Integer) Implements ITest13.M13
    End Sub
    Sub M14(x As ITest13)
        x.M13(1)
        x.M13(1)
    End Sub
End Class

Interface IUsePia4
    Inherits ITest15, ITest16, ITest18, ITest19
End Interface

Class UsePia4
    Public Function M1(x As ITest21) As Integer
        Return x.P1
    End Function
    Public Sub M2(x As ITest22)
        x.P2 = 1
    End Sub
    Public Function M3(x As ITest23) As Integer
        Return x.P3
    End Function
    Public Sub M4(x As ITest24)
        x.P4 = 1
    End Sub
    Public Sub M5(x As ITest25)
        AddHandler x.E1, Nothing
    End Sub
    Public Sub M6(x As ITest26)
        RemoveHandler x.E2, Nothing
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)

                                                           Dim itest1 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest1")
                                                           Assert.Equal(TypeKind.Interface, itest1.TypeKind)
                                                           Assert.Null(itest1.BaseType)
                                                           Assert.Equal(0, itest1.Interfaces.Length)
                                                           Assert.True(itest1.IsComImport)
                                                           Assert.False(itest1.IsSerializable)
                                                           Assert.False(itest1.IsNotInheritable)
                                                           Assert.Equal(System.Runtime.InteropServices.CharSet.Ansi, itest1.MarshallingCharSet)
                                                           Assert.Equal(System.Runtime.InteropServices.LayoutKind.Auto, itest1.Layout.Kind)
                                                           Assert.Equal(0, itest1.Layout.Alignment)
                                                           Assert.Equal(0, itest1.Layout.Size)

                                                           Dim attributes = itest1.GetAttributes()
                                                           Assert.Equal(3, attributes.Length)
                                                           Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes(0).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.GuidAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58258"")", attributes(1).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute", attributes(2).ToString())

                                                           ' TypDefName: ITest1  (02000018)
                                                           ' Flags     : [Public] [AutoLayout] [Interface] [Abstract] [Import] [AnsiClass]  (000010a1)
                                                           Assert.Equal(TypeAttributes.Public Or TypeAttributes.AutoLayout Or TypeAttributes.Interface Or TypeAttributes.Abstract Or TypeAttributes.Import Or TypeAttributes.AnsiClass, itest1.TypeDefFlags)

                                                           Dim test2 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("Test2")
                                                           Assert.Equal(TypeKind.Structure, test2.TypeKind)
                                                           Assert.Equal(SpecialType.System_ValueType, test2.BaseType.SpecialType)
                                                           Assert.Same(itest1, test2.Interfaces.Single())
                                                           Assert.False(test2.IsComImport)
                                                           Assert.False(test2.IsSerializable)
                                                           Assert.True(test2.IsNotInheritable)
                                                           Assert.Equal(System.Runtime.InteropServices.CharSet.Ansi, test2.MarshallingCharSet)
                                                           Assert.Equal(System.Runtime.InteropServices.LayoutKind.Sequential, test2.Layout.Kind)
                                                           Assert.Equal(0, test2.Layout.Alignment)
                                                           Assert.Equal(1, test2.Layout.Size)

                                                           ' TypDefName: Test2  (02000013)
                                                           ' Flags     : [Public] [SequentialLayout] [Class] [Sealed] [AnsiClass] [BeforeFieldInit]  (00100109)
                                                           Assert.Equal(TypeAttributes.Public Or TypeAttributes.SequentialLayout Or TypeAttributes.Class Or TypeAttributes.Sealed Or TypeAttributes.AnsiClass Or TypeAttributes.BeforeFieldInit, test2.TypeDefFlags)

                                                           attributes = test2.GetAttributes()
                                                           Assert.Equal(2, attributes.Length)
                                                           Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes(0).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""Test2"")", attributes(1).ToString())

                                                           Dim itest3 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("ITest3")
                                                           Assert.Equal(TypeKind.Interface, itest3.TypeKind)
                                                           Assert.Same(itest1, itest3.Interfaces.Single())
                                                           Assert.True(itest3.IsComImport)
                                                           Assert.False(itest3.IsSerializable)
                                                           Assert.False(itest3.IsNotInheritable)
                                                           Assert.Equal(System.Runtime.InteropServices.CharSet.Ansi, itest3.MarshallingCharSet)
                                                           Assert.Equal(System.Runtime.InteropServices.LayoutKind.Auto, itest3.Layout.Kind)
                                                           Assert.Equal(0, itest3.Layout.Alignment)
                                                           Assert.Equal(0, itest3.Layout.Size)

                                                           Assert.Equal(0, [module].GlobalNamespace.GetTypeMembers("ITest4").Length)

                                                           Dim test5 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test5")
                                                           Assert.Equal(TypeKind.Structure, test5.TypeKind)
                                                           Assert.False(test5.IsComImport)
                                                           Assert.True(test5.IsSerializable)
                                                           Assert.True(test5.IsNotInheritable)
                                                           Assert.Equal(System.Runtime.InteropServices.CharSet.Unicode, test5.MarshallingCharSet)
                                                           Assert.Equal(System.Runtime.InteropServices.LayoutKind.Explicit, test5.Layout.Kind)
                                                           Assert.Equal(16, test5.Layout.Alignment)
                                                           Assert.Equal(64, test5.Layout.Size)

                                                           Dim f5 = DirectCast(test5.GetMembers()(0), PEFieldSymbol)
                                                           Assert.Equal("Test5.F5 As System.Int32", f5.ToTestDisplayString())
                                                           Assert.Equal(2, f5.TypeLayoutOffset.Value)

                                                           ' Field Name: F5 (04000003)
                                                           ' Flags     : [Public]  (00000006)
                                                           Assert.Equal(FieldAttributes.Public, f5.FieldFlags)

                                                           Dim itest6 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("ITest6")
                                                           Assert.Equal(TypeKind.Interface, itest6.TypeKind)

                                                           Dim itest7 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("ITest7")
                                                           Assert.Equal(TypeKind.Interface, itest7.TypeKind)

                                                           Dim itest8 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("ITest8")
                                                           Assert.Equal(TypeKind.Interface, itest8.TypeKind)
                                                           Assert.Same(itest8, [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("UsePia1").Interfaces.Single())

                                                           Dim test9 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("Test9")
                                                           Assert.Equal(TypeKind.Enum, test9.TypeKind)
                                                           Assert.False(test9.IsComImport)
                                                           Assert.False(test9.IsSerializable)
                                                           Assert.True(test9.IsNotInheritable)
                                                           Assert.Equal(System.Runtime.InteropServices.CharSet.Ansi, test9.MarshallingCharSet)
                                                           Assert.Equal(System.Runtime.InteropServices.LayoutKind.Auto, test9.Layout.Kind)

                                                           Assert.Equal(SpecialType.System_Int32, test9.EnumUnderlyingType.SpecialType)

                                                           ' TypDefName: Test9  (02000016)
                                                           ' Flags     : [Public] [AutoLayout] [Class] [Sealed] [AnsiClass]  (00000101)
                                                           Assert.Equal(TypeAttributes.Public Or TypeAttributes.AutoLayout Or TypeAttributes.Class Or TypeAttributes.Sealed Or TypeAttributes.AnsiClass, test9.TypeDefFlags)

                                                           attributes = test9.GetAttributes()
                                                           Assert.Equal(2, attributes.Length)
                                                           Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes(0).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""Test9"")", attributes(1).ToString())

                                                           Dim fieldToEmit = test9.GetFieldsToEmit().ToArray().AsImmutableOrNull()
                                                           Assert.Equal(3, fieldToEmit.Length)

                                                           Dim value__ = DirectCast(fieldToEmit(0), PEFieldSymbol)
                                                           Assert.Equal(Accessibility.Public, value__.DeclaredAccessibility)
                                                           Assert.Equal("Test9.value__ As System.Int32", value__.ToTestDisplayString())
                                                           Assert.False(value__.IsShared)
                                                           Assert.True(value__.HasSpecialName)
                                                           Assert.True(value__.HasRuntimeSpecialName)
                                                           Assert.Null(value__.ConstantValue)

                                                           ' Field Name: value__ (04000004)
                                                           ' Flags     : [Public] [SpecialName] [RTSpecialName]  (00000606)
                                                           Assert.Equal(FieldAttributes.Public Or FieldAttributes.SpecialName Or FieldAttributes.RTSpecialName, value__.FieldFlags)

                                                           Dim f1 = DirectCast(fieldToEmit(1), PEFieldSymbol)
                                                           Assert.Equal(Accessibility.Public, f1.DeclaredAccessibility)
                                                           Assert.Equal("Test9.F1", f1.ToTestDisplayString())
                                                           Assert.True(f1.IsShared)
                                                           Assert.False(f1.HasSpecialName)
                                                           Assert.False(f1.HasRuntimeSpecialName)
                                                           Assert.Equal(1, f1.ConstantValue)

                                                           ' Field Name: F1 (04000005)
                                                           ' Flags     : [Public] [Static] [Literal] [HasDefault]  (00008056)
                                                           Assert.Equal(FieldAttributes.Public Or FieldAttributes.Static Or FieldAttributes.Literal Or FieldAttributes.HasDefault, f1.FieldFlags)

                                                           Dim f2 = DirectCast(fieldToEmit(2), PEFieldSymbol)
                                                           Assert.Equal("Test9.F2", f2.ToTestDisplayString())
                                                           Assert.Equal(2, f2.ConstantValue)

                                                           Assert.Equal(4, test9.GetMembers().Length)
                                                           Assert.Equal("Test9.value__ As System.Int32", test9.GetMembers()(0).ToTestDisplayString())
                                                           Assert.Same(f1, test9.GetMembers()(1))
                                                           Assert.Same(f2, test9.GetMembers()(2))
                                                           Assert.True(DirectCast(test9.GetMembers()(3), MethodSymbol).IsDefaultValueTypeConstructor())

                                                           Dim test10 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test10")
                                                           Assert.Equal(TypeKind.Structure, test10.TypeKind)
                                                           Assert.Equal(System.Runtime.InteropServices.LayoutKind.Sequential, test10.Layout.Kind)

                                                           Assert.Equal(3, test10.GetMembers().Length)

                                                           Dim f3 = DirectCast(test10.GetMembers()(0), FieldSymbol)
                                                           Assert.Equal(Accessibility.Public, f3.DeclaredAccessibility)
                                                           Assert.Equal("Test10.F3 As System.Int32", f3.ToTestDisplayString())
                                                           Assert.False(f3.IsShared)
                                                           Assert.False(f3.HasSpecialName)
                                                           Assert.False(f3.HasRuntimeSpecialName)
                                                           Assert.Null(f3.ConstantValue)
                                                           Assert.Equal(0, f3.MarshallingType)
                                                           Assert.False(f3.TypeLayoutOffset.HasValue)
                                                           Assert.True(f3.IsNotSerialized)

                                                           Dim f4 = DirectCast(test10.GetMembers()(1), FieldSymbol)
                                                           Assert.Equal("Test10.F4 As System.Int32", f4.ToTestDisplayString())
                                                           Assert.Equal(System.Runtime.InteropServices.UnmanagedType.U4, f4.MarshallingType)
                                                           Assert.False(f4.IsNotSerialized)

                                                           Assert.True(DirectCast(test10.GetMembers()(2), MethodSymbol).IsDefaultValueTypeConstructor())

                                                           Dim test11 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("Test11")
                                                           Assert.Equal(TypeKind.Delegate, test11.TypeKind)
                                                           Assert.Equal(SpecialType.System_MulticastDelegate, test11.BaseType.SpecialType)

                                                           ' TypDefName: Test11  (02000012)
                                                           ' Flags     : [Public] [AutoLayout] [Class] [Sealed] [AnsiClass]  (00000101)
                                                           Assert.Equal(TypeAttributes.Public Or TypeAttributes.AutoLayout Or TypeAttributes.Class Or TypeAttributes.Sealed Or TypeAttributes.AnsiClass, test11.TypeDefFlags)

                                                           attributes = test11.GetAttributes()
                                                           Assert.Equal(2, attributes.Length)
                                                           Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes(0).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""Test11"")", attributes(1).ToString())

                                                           Assert.Equal(4, test11.GetMembers().Length)

                                                           Dim ctor = DirectCast(test11.GetMethod(".ctor"), PEMethodSymbol)

                                                           ' MethodName: .ctor (0600000F)
                                                           ' Flags     : [Public] [ReuseSlot] [SpecialName] [RTSpecialName] [.ctor]  (00001886)
                                                           ' ImplFlags : [Runtime] [Managed]  (00000003)
                                                           ' CallCnvntn: [DEFAULT]
                                                           ' hasThis 
                                                           ' ReturnType: Void
                                                           ' 2 Arguments
                                                           '     Argument #1:  Object
                                                           '     Argument #2:  I

                                                           Assert.Equal(MethodAttributes.Public Or MethodAttributes.ReuseSlot Or MethodAttributes.SpecialName Or MethodAttributes.RTSpecialName, ctor.MethodFlags)
                                                           Assert.Equal(MethodImplAttributes.Runtime, ctor.ImplementationAttributes)
                                                           Assert.Equal(Microsoft.Cci.CallingConvention.Default Or Microsoft.Cci.CallingConvention.HasThis, ctor.CallingConvention)
                                                           Assert.Equal("Sub Test11..ctor(TargetObject As System.Object, TargetMethod As System.IntPtr)", ctor.ToTestDisplayString())

                                                           Dim begin = test11.GetMember(Of PEMethodSymbol)("BeginInvoke")

                                                           ' MethodName: BeginInvoke (06000011)
                                                           ' Flags     : [Public] [Virtual] [CheckAccessOnOverride] [NewSlot]  (000001c6)
                                                           ' ImplFlags : [Runtime] [Managed]  (00000003)
                                                           ' CallCnvntn: [DEFAULT]
                                                           ' hasThis 
                                                           ' ReturnType: Class System.IAsyncResult
                                                           ' 2 Arguments
                                                           '     Argument #1:  Class System.AsyncCallback
                                                           '     Argument #2:  Object
                                                           Assert.Equal(MethodAttributes.Public Or MethodAttributes.Virtual Or MethodAttributes.CheckAccessOnOverride Or MethodAttributes.NewSlot, begin.MethodFlags)
                                                           Assert.Equal(MethodImplAttributes.Runtime, begin.ImplementationAttributes)
                                                           Assert.Equal(Microsoft.Cci.CallingConvention.Default Or Microsoft.Cci.CallingConvention.HasThis, begin.CallingConvention)
                                                           Assert.Equal("Function Test11.BeginInvoke(DelegateCallback As System.AsyncCallback, DelegateAsyncState As System.Object) As System.IAsyncResult", begin.ToTestDisplayString())

                                                           Dim [end] = test11.GetMember(Of PEMethodSymbol)("EndInvoke")

                                                           ' MethodName: EndInvoke (06000012)
                                                           ' Flags     : [Public] [Virtual] [CheckAccessOnOverride] [NewSlot]  (000001c6)
                                                           ' ImplFlags : [Runtime] [Managed]  (00000003)
                                                           ' CallCnvntn: [DEFAULT]
                                                           ' hasThis 
                                                           ' ReturnType: Void
                                                           ' 1 Arguments
                                                           '     Argument #1:  Class System.IAsyncResult

                                                           Assert.Equal(MethodAttributes.Public Or MethodAttributes.Virtual Or MethodAttributes.CheckAccessOnOverride Or MethodAttributes.NewSlot, [end].MethodFlags)
                                                           Assert.Equal(MethodImplAttributes.Runtime, [end].ImplementationAttributes)
                                                           Assert.Equal(Microsoft.Cci.CallingConvention.Default Or Microsoft.Cci.CallingConvention.HasThis, [end].CallingConvention)
                                                           Assert.Equal("Sub Test11.EndInvoke(DelegateAsyncResult As System.IAsyncResult)", [end].ToTestDisplayString())

                                                           Dim invoke = test11.GetMember(Of PEMethodSymbol)("Invoke")

                                                           ' MethodName: Invoke (06000010)
                                                           ' Flags     : [Public] [Virtual] [CheckAccessOnOverride] [NewSlot]  (000001c6)
                                                           ' ImplFlags : [Runtime] [Managed]  (00000003)
                                                           ' CallCnvntn: [DEFAULT]
                                                           ' hasThis 
                                                           ' ReturnType: Void
                                                           ' No arguments.

                                                           Assert.Equal(MethodAttributes.Public Or MethodAttributes.Virtual Or MethodAttributes.CheckAccessOnOverride Or MethodAttributes.NewSlot, invoke.MethodFlags)
                                                           Assert.Equal(MethodImplAttributes.Runtime, invoke.ImplementationAttributes)
                                                           Assert.Equal(Microsoft.Cci.CallingConvention.Default Or Microsoft.Cci.CallingConvention.HasThis, invoke.CallingConvention)
                                                           Assert.Equal("Sub Test11.Invoke()", invoke.ToTestDisplayString())

                                                           Dim itest13 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("ITest13")
                                                           Assert.Equal(TypeKind.Interface, itest13.TypeKind)

                                                           Dim m13 = DirectCast(itest13.GetMembers()(0), PEMethodSymbol)

                                                           ' MethodName: M13 (06000001)
                                                           ' Flags     : [Public] [Virtual] [CheckAccessOnOverride] [NewSlot] [Abstract]  (000005c6)
                                                           ' ImplFlags : [IL] [Managed]  (00000000)
                                                           ' CallCnvntn: [DEFAULT]
                                                           ' hasThis 
                                                           ' ReturnType: Void
                                                           ' 1 Arguments
                                                           '     Argument #1:  I4
                                                           ' 1 Parameters
                                                           '     (1) ParamToken : (08000001) Name : x flags: [none] (00000000)

                                                           Assert.Equal(MethodAttributes.Public Or MethodAttributes.Virtual Or MethodAttributes.CheckAccessOnOverride Or MethodAttributes.NewSlot Or MethodAttributes.Abstract, m13.MethodFlags)
                                                           Assert.Equal(MethodImplAttributes.IL, m13.ImplementationAttributes)
                                                           Assert.Equal(Microsoft.Cci.CallingConvention.HasThis, m13.CallingConvention)
                                                           Assert.Equal("Sub ITest13.M13(x As System.Int32)", m13.ToTestDisplayString())

                                                           Dim itest14 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("ITest14")
                                                           Assert.Equal(TypeKind.Interface, itest14.TypeKind)
                                                           Assert.Equal(6, itest14.GetMembers().Length)
                                                           Assert.Equal("Sub ITest14.M14()", itest14.GetMembers()(0).ToTestDisplayString())
                                                           Assert.Equal("Sub ITest14.set_P6(Value As System.Int32)", itest14.GetMembers()(1).ToTestDisplayString())
                                                           Assert.Equal("Sub ITest14.add_E4(obj As System.Action)", itest14.GetMembers()(2).ToTestDisplayString())
                                                           Assert.Equal("Sub ITest14.remove_E4(obj As System.Action)", itest14.GetMembers()(3).ToTestDisplayString())
                                                           Assert.Equal("WriteOnly Property ITest14.P6 As System.Int32", itest14.GetMembers()(4).ToTestDisplayString())
                                                           Assert.Equal("Event ITest14.E4 As System.Action", itest14.GetMembers()(5).ToTestDisplayString())

                                                           Dim itest16 = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("ITest16")
                                                           Assert.Equal(TypeKind.Interface, itest16.TypeKind)
                                                           Assert.Equal("Sub ITest16.M16()", itest16.GetMembers()(0).ToTestDisplayString())

                                                           Dim itest17 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest17")
                                                           Assert.Equal(TypeKind.Interface, itest17.TypeKind)

                                                           Dim metadata = DirectCast([module], PEModuleSymbol).Module

                                                           Dim methodNames = metadata.GetMethodsOfTypeOrThrow(itest17.Handle).AsEnumerable().Select(
                                                               Function(rid) metadata.GetMethodDefNameOrThrow(rid)).ToArray()

                                                           Assert.Equal(3, methodNames.Length)
                                                           Assert.Equal("M17", methodNames(0))
                                                           Assert.Equal("_VtblGap1_4", methodNames(1))
                                                           Assert.Equal("M19", methodNames(2))

                                                           Dim gapMethodDef = metadata.GetMethodsOfTypeOrThrow(itest17.Handle).AsEnumerable().ElementAt(1)
                                                           Dim name As String = Nothing
                                                           Dim implFlags As MethodImplAttributes = Nothing
                                                           Dim flags As MethodAttributes = Nothing
                                                           Dim rva As Integer = Nothing

                                                           metadata.GetMethodDefPropsOrThrow(gapMethodDef, name, implFlags, flags, rva)

                                                           Assert.Equal(MethodAttributes.Public Or MethodAttributes.RTSpecialName Or MethodAttributes.SpecialName, flags)
                                                           Assert.Equal(MethodImplAttributes.IL Or MethodImplAttributes.Runtime, implFlags)

                                                           Dim signatureHeader As SignatureHeader = Nothing
                                                           Dim mrEx As BadImageFormatException = Nothing
                                                           Dim paramInfo = New MetadataDecoder(DirectCast([module], PEModuleSymbol), itest17).GetSignatureForMethod(gapMethodDef, allowByRefReturn:=False, signatureHeader:=signatureHeader, metadataException:=mrEx)
                                                           Assert.Null(mrEx)
                                                           Assert.Equal(CByte(SignatureCallingConvention.Default) Or CByte(SignatureAttributes.Instance), signatureHeader.RawValue)
                                                           Assert.Equal(1, paramInfo.Length)
                                                           Assert.Equal(SpecialType.System_Void, paramInfo(0).Type.SpecialType)
                                                           Assert.False(paramInfo(0).IsByRef)
                                                           Assert.True(paramInfo(0).CustomModifiers.IsDefault)

                                                           Assert.Equal(2, itest17.GetMembers().Length)
                                                           Dim m17 = itest17.GetMember(Of PEMethodSymbol)("M17")

                                                           ' MethodName: M17 (06000013)
                                                           ' Flags     : [Public] [Virtual] [CheckAccessOnOverride] [NewSlot] [Abstract]  (000005c6)
                                                           ' ImplFlags : [IL] [Managed]  (00000000)
                                                           ' CallCnvntn: [DEFAULT]
                                                           ' hasThis 
                                                           ' ReturnType: Void
                                                           ' No arguments.
                                                           Assert.Equal(MethodAttributes.Public Or MethodAttributes.Virtual Or MethodAttributes.CheckAccessOnOverride Or MethodAttributes.NewSlot Or MethodAttributes.Abstract, m17.MethodFlags)
                                                           Assert.Equal(MethodImplAttributes.IL, m17.ImplementationAttributes)
                                                           Assert.Equal(Microsoft.Cci.CallingConvention.Default Or Microsoft.Cci.CallingConvention.HasThis, m17.CallingConvention)
                                                           Assert.Equal("Sub ITest17.M17()", m17.ToTestDisplayString())

                                                           Dim itest18 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest18")
                                                           Assert.Equal(TypeKind.Interface, itest18.TypeKind)
                                                           Assert.False(metadata.GetMethodsOfTypeOrThrow(itest18.Handle).AsEnumerable().Any())

                                                           Dim itest19 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest19")
                                                           Dim m20 = itest19.GetMember(Of PEMethodSymbol)("M20")

                                                           ' 6 Arguments
                                                           '     Argument #1:  ByRef I4
                                                           '     Argument #2:  ByRef I4
                                                           '     Argument #3:  ByRef I4
                                                           '     Argument #4:  ByRef I4
                                                           '     Argument #5:  I4
                                                           '     Argument #6:  I4
                                                           ' 6 Parameters
                                                           '     (1) ParamToken : (08000008) Name : x flags: [none] (00000000)
                                                           '     (2) ParamToken : (08000009) Name : y flags: [none]  (00000000)
                                                           '     (3) ParamToken : (0800000a) Name : z flags: [In]  (00000001)
                                                           '     (4) ParamToken : (0800000b) Name : u flags: [In] [Out]  (00000003)
                                                           '     (5) ParamToken : (0800000c) Name : v flags: [Optional]  (00000010)
                                                           '     (6) ParamToken : (0800000d) Name : w flags: [Optional] [HasDefault]  (00001010) Default: (I4) 34

                                                           Dim param = DirectCast(m20.Parameters(0), PEParameterSymbol)
                                                           Assert.True(param.IsByRef)
                                                           Assert.Equal(ParameterAttributes.None, param.ParamFlags)

                                                           param = DirectCast(m20.Parameters(1), PEParameterSymbol)
                                                           Assert.True(param.IsByRef)
                                                           Assert.Equal(ParameterAttributes.None, param.ParamFlags)

                                                           param = DirectCast(m20.Parameters(2), PEParameterSymbol)
                                                           Assert.True(param.IsByRef)
                                                           Assert.Equal(ParameterAttributes.In, param.ParamFlags)

                                                           param = DirectCast(m20.Parameters(3), PEParameterSymbol)
                                                           Assert.True(param.IsByRef)
                                                           Assert.Equal(ParameterAttributes.In Or ParameterAttributes.Out, param.ParamFlags)

                                                           param = DirectCast(m20.Parameters(4), PEParameterSymbol)
                                                           Assert.False(param.IsByRef)
                                                           Assert.Equal(ParameterAttributes.Optional, param.ParamFlags)
                                                           Assert.Null(param.ExplicitDefaultConstantValue)

                                                           param = DirectCast(m20.Parameters(5), PEParameterSymbol)
                                                           Assert.False(param.IsByRef)
                                                           Assert.Equal(ParameterAttributes.Optional Or ParameterAttributes.HasDefault, param.ParamFlags)
                                                           Assert.Equal(34, param.ExplicitDefaultValue)

                                                           Assert.False(m20.ReturnValueIsMarshalledExplicitly)

                                                           Dim m21 = itest19.GetMember(Of PEMethodSymbol)("M21")

                                                           ' 1 Arguments
                                                           '     Argument #1:  I4
                                                           ' 2 Parameters
                                                           '     (0) ParamToken : (0800000e) Name :  flags: [HasFieldMarshal]  (00002000)
                                                           '         NATIVE_TYPE_LPWSTR 
                                                           '     (1) ParamToken : (0800000f) Name : x flags: [HasFieldMarshal]  (00002000)
                                                           '         NATIVE_TYPE_U4 

                                                           param = DirectCast(m21.Parameters(0), PEParameterSymbol)
                                                           Assert.Equal(ParameterAttributes.HasFieldMarshal, param.ParamFlags)
                                                           Assert.Equal(System.Runtime.InteropServices.UnmanagedType.U4, CType(param.MarshallingDescriptor(0), System.Runtime.InteropServices.UnmanagedType))

                                                           Assert.True(m21.ReturnValueIsMarshalledExplicitly)
                                                           Assert.Equal(System.Runtime.InteropServices.UnmanagedType.LPWStr, CType(m21.ReturnValueMarshallingDescriptor(0), System.Runtime.InteropServices.UnmanagedType))

                                                           Dim itest21 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest21")
                                                           Dim p1 = itest21.GetMember(Of PEPropertySymbol)("P1")

                                                           Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility)
                                                           Assert.True(p1.HasSpecialName)
                                                           Assert.False(p1.HasRuntimeSpecialName)

                                                           Dim get_P1 = itest21.GetMember(Of PEMethodSymbol)("get_P1")
                                                           Dim set_P1 = itest21.GetMember(Of PEMethodSymbol)("set_P1")

                                                           Assert.Same(p1.GetMethod, get_P1)
                                                           Assert.Same(p1.SetMethod, set_P1)

                                                           Dim itest22 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest22")
                                                           Dim p2 = itest22.GetMember(Of PEPropertySymbol)("P2")

                                                           Dim get_P2 = itest22.GetMember(Of PEMethodSymbol)("get_P2")
                                                           Dim set_P2 = itest22.GetMember(Of PEMethodSymbol)("set_P2")

                                                           Assert.Same(p2.GetMethod, get_P2)
                                                           Assert.Same(p2.SetMethod, set_P2)

                                                           Dim itest23 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest23")
                                                           Dim p3 = itest23.GetMember(Of PEPropertySymbol)("P3")

                                                           Dim get_P3 = itest23.GetMember(Of PEMethodSymbol)("get_P3")

                                                           Assert.Same(p3.GetMethod, get_P3)
                                                           Assert.Null(p3.SetMethod)

                                                           Dim itest24 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest24")
                                                           Dim p4 = itest24.GetMember(Of PEPropertySymbol)("P4")

                                                           Assert.Equal(2, itest24.GetMembers().Length)
                                                           Assert.False(p4.HasSpecialName)
                                                           Assert.False(p4.HasRuntimeSpecialName)
                                                           Assert.Equal(CByte(SignatureKind.Property) Or CByte(SignatureAttributes.Instance), CByte(p4.CallingConvention))

                                                           Dim set_P4 = itest24.GetMember(Of PEMethodSymbol)("set_P4")

                                                           Assert.Null(p4.GetMethod)
                                                           Assert.Same(p4.SetMethod, set_P4)

                                                           Dim itest25 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest25")
                                                           Dim e1 = itest25.GetMember(Of PEEventSymbol)("E1")

                                                           Assert.True(e1.HasSpecialName)
                                                           Assert.False(e1.HasRuntimeSpecialName)

                                                           Dim add_E1 = itest25.GetMember(Of PEMethodSymbol)("add_E1")
                                                           Dim remove_E1 = itest25.GetMember(Of PEMethodSymbol)("remove_E1")

                                                           Assert.Same(e1.AddMethod, add_E1)
                                                           Assert.Same(e1.RemoveMethod, remove_E1)

                                                           Dim itest26 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("ITest26")
                                                           Dim e2 = itest26.GetMember(Of PEEventSymbol)("E2")

                                                           Assert.Equal(3, itest26.GetMembers().Length)
                                                           Assert.False(e2.HasSpecialName)
                                                           Assert.False(e2.HasRuntimeSpecialName)

                                                           Dim add_E2 = itest26.GetMember(Of PEMethodSymbol)("add_E2")
                                                           Dim remove_E2 = itest26.GetMember(Of PEMethodSymbol)("remove_E2")

                                                           Assert.Same(e2.AddMethod, add_E2)
                                                           Assert.Same(e2.RemoveMethod, remove_E2)
                                                       End Sub
            Dim expected_M5 = <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldnull
  IL_0003:  callvirt   "Sub ITest25.add_E1(System.Action)"
  IL_0008:  nop
  IL_0009:  ret
}
]]>
            Dim expected_M6 = <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldnull
  IL_0003:  callvirt   "Sub ITest26.remove_E2(System.Action)"
  IL_0008:  nop
  IL_0009:  ret
}
]]>
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugExe,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})

            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            verifier.VerifyDiagnostics()
            verifier.VerifyIL("UsePia4.M5", expected_M5)
            verifier.VerifyIL("UsePia4.M6", expected_M6)

            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugExe,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})

            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            verifier.VerifyDiagnostics()
            verifier.VerifyIL("UsePia4.M5", expected_M5)
            verifier.VerifyIL("UsePia4.M6", expected_M6)
        End Sub

        <Fact()>
        Public Sub LocalTypeMetadata_GenericParameters()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
Public Interface I1
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public Interface I2
    Sub M(Of T1, T2 As I1, T3 As New, T4 As Structure, T5 As Class, T6 As T1)
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Interface I3
    Inherits I2
End Interface
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0, verify:=False)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)

                                                           Dim type1 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I1")
                                                           Assert.Equal(TypeKind.Interface, type1.TypeKind)

                                                           Dim type2 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I2")
                                                           Assert.Equal(TypeKind.Interface, type2.TypeKind)
                                                           Dim method = type2.GetMember(Of PEMethodSymbol)("M")
                                                           Dim tp = method.TypeParameters
                                                           Assert.Equal(6, tp.Length)

                                                           Dim t1 = tp(0)
                                                           Assert.Equal("T1", t1.Name)
                                                           Assert.False(t1.HasConstructorConstraint)
                                                           Assert.False(t1.HasValueTypeConstraint)
                                                           Assert.False(t1.HasReferenceTypeConstraint)
                                                           Assert.Equal(0, t1.ConstraintTypes.Length)
                                                           Assert.Equal(VarianceKind.None, t1.Variance)

                                                           Dim t2 = tp(1)
                                                           Assert.Equal("T2", t2.Name)
                                                           Assert.False(t2.HasConstructorConstraint)
                                                           Assert.False(t2.HasValueTypeConstraint)
                                                           Assert.False(t2.HasReferenceTypeConstraint)
                                                           Assert.Equal(1, t2.ConstraintTypes.Length)
                                                           Assert.Same(type1, t2.ConstraintTypes(0))
                                                           Assert.Equal(VarianceKind.None, t2.Variance)

                                                           Dim t3 = tp(2)
                                                           Assert.Equal("T3", t3.Name)
                                                           Assert.True(t3.HasConstructorConstraint)
                                                           Assert.False(t3.HasValueTypeConstraint)
                                                           Assert.False(t3.HasReferenceTypeConstraint)
                                                           Assert.Equal(0, t3.ConstraintTypes.Length)
                                                           Assert.Equal(VarianceKind.None, t3.Variance)

                                                           Dim t4 = tp(3)
                                                           Assert.Equal("T4", t4.Name)
                                                           Assert.False(t4.HasConstructorConstraint)
                                                           Assert.True(t4.HasValueTypeConstraint)
                                                           Assert.False(t4.HasReferenceTypeConstraint)
                                                           Assert.Equal(0, t4.ConstraintTypes.Length)
                                                           Assert.Equal(VarianceKind.None, t4.Variance)

                                                           Dim t5 = tp(4)
                                                           Assert.Equal("T5", t5.Name)
                                                           Assert.False(t5.HasConstructorConstraint)
                                                           Assert.False(t5.HasValueTypeConstraint)
                                                           Assert.True(t5.HasReferenceTypeConstraint)
                                                           Assert.Equal(0, t5.ConstraintTypes.Length)
                                                           Assert.Equal(VarianceKind.None, t5.Variance)

                                                           Dim t6 = tp(5)
                                                           Assert.Equal("T6", t6.Name)
                                                           Assert.False(t6.HasConstructorConstraint)
                                                           Assert.False(t6.HasValueTypeConstraint)
                                                           Assert.False(t6.HasReferenceTypeConstraint)
                                                           Assert.Equal(1, t6.ConstraintTypes.Length)
                                                           Assert.Same(t1, t6.ConstraintTypes(0))
                                                           Assert.Equal(VarianceKind.None, t6.Variance)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugDll,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), SystemCoreRef})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator, verify:=False)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugDll,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True), SystemCoreRef})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator, verify:=False)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub NewWithoutCoClass()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
Public Interface I
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Structure S
    Function F() As I
        Return New I()
    End Function
End Structure
]]></file>
                           </compilation>
            Dim errors = <errors>
BC30375: 'New' cannot be used on an interface.
        Return New I()
               ~~~~~~~
</errors>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            compilation0.AssertTheseDiagnostics()
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, <errors/>)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, <errors/>)
        End Sub

        <Fact()>
        Public Sub NewCoClassWithoutGiud()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
<CoClass(GetType(C))>
Public Interface I
    Property P As Integer
End Interface
Public Class C
    Public Sub New(o As Object)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Structure S
    Function F() As I
        Return New I() With {.P = 2}
    End Function
End Structure
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31543: Interop type 'C' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.GuidAttribute' attribute.
        Return New I() With {.P = 2}
               ~~~~~~~~~~~~~~~~~~~~~
</errors>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            compilation0.AssertTheseDiagnostics()
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
        End Sub

        <Fact()>
        Public Sub NewCoClassWithGiud()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
<CoClass(GetType(C))>
Public Interface I
    Property P As Integer
End Interface
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public MustInherit Class C
    Protected Sub New(o As Object)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Structure S
    Function F() As I
        Return New I() With {.P = 2}
    End Function
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Assert.Equal(1, [module].GetReferencedAssemblySymbols().Length)
                                                           Dim i = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim attr = i.GetAttributes("System.Runtime.InteropServices", "CoClassAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.CoClassAttribute(GetType(Object))", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithReferences(
                sources1,
                references:={MscorlibRef, SystemRef, compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            verifier.VerifyIL("S.F", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  3
  IL_0000:  ldstr      "f9c2d51d-4f44-45f0-9eda-c9d599b58278"
  IL_0005:  newobj     "Sub System.Guid..ctor(String)"
  IL_000a:  call       "Function System.Type.GetTypeFromCLSID(System.Guid) As System.Type"
  IL_000f:  call       "Function System.Activator.CreateInstance(System.Type) As Object"
  IL_0014:  castclass  "I"
  IL_0019:  dup
  IL_001a:  ldc.i4.2
  IL_001b:  callvirt   "Sub I.set_P(Integer)"
  IL_0020:  ret
}
]]>)
            compilation1 = CreateCompilationWithReferences(
                sources1,
                references:={MscorlibRef_v4_0_30316_17626, SystemRef, compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            verifier.VerifyIL("S.F", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  3
  IL_0000:  ldstr      "f9c2d51d-4f44-45f0-9eda-c9d599b58278"
  IL_0005:  newobj     "Sub System.Guid..ctor(String)"
  IL_000a:  call       "Function System.Runtime.InteropServices.Marshal.GetTypeFromCLSID(System.Guid) As System.Type"
  IL_000f:  call       "Function System.Activator.CreateInstance(System.Type) As Object"
  IL_0014:  castclass  "I"
  IL_0019:  dup
  IL_001a:  ldc.i4.2
  IL_001b:  callvirt   "Sub I.set_P(Integer)"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub NewCoClassWithGiud_Generic()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
<CoClass(GetType(C(Of)))>
Public Interface I
End Interface
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public MustInherit Class C(Of T)
    Protected Sub New(o As Object)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Function F() As I
        Return New I()
    End Function
End Module
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, <errors>
BC31450: Type 'C(Of )' cannot be used as an implementing class.
        Return New I()
               ~~~~~~~
</errors>)
        End Sub

        ''' <summary>
        ''' Report error attempting to instantiate NoPIA CoClass with arguments.
        ''' Note: Dev11 silently drops any arguments and does not report an error.
        ''' </summary>
        <Fact()>
        Public Sub NewCoClassWithArguments()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
<CoClass(GetType(C))>
Public Interface I
    Property P As Integer
End Interface
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public Class C
    Public Sub New(o As Object)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Structure S
    Function F() As I
        Return New I("")
    End Function
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            compilation0.AssertTheseDiagnostics()
            ' No errors for /r:_.dll
            Dim compilation1 = CreateCompilationWithReferences(
                sources1,
                references:={MscorlibRef, SystemRef, MetadataReference.CreateFromImage(compilation0.EmitToArray())})
            compilation1.AssertTheseDiagnostics()
            ' Error for /l:_.dll
            compilation1 = CreateCompilationWithReferences(
                sources1,
                references:={MscorlibRef, SystemRef, compilation0.EmitToImageReference(embedInteropTypes:=True)})
            compilation1.AssertTheseDiagnostics(<errors>
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
        Return New I("")
               ~~~~~~~~~
</errors>)
            ' Verify the unused argument is available in the SemanticModel.
            Dim syntaxTree = compilation1.SyntaxTrees(0)
            Dim model = compilation1.GetSemanticModel(syntaxTree)
            Dim node = DirectCast(syntaxTree.FindNodeOrTokenByKind(SyntaxKind.StringLiteralExpression).AsNode(), ExpressionSyntax)
            Dim expr = model.GetTypeInfo(node)
            Assert.Equal(expr.Type.SpecialType, SpecialType.System_String)
        End Sub

        <Fact()>
        Public Sub NewCoClassMissingWellKnownMembers()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
<CoClass(GetType(C))>
Public Interface I
End Interface
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public Class C
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Namespace System
    Public Class Guid
        Private Sub New()
        End Sub
    End Class
    Public Class Activator
    End Class
End Namespace
]]></file>
                               <file name="b.vb"><![CDATA[
Structure S
    Function F() As I
        Dim x As New I()
        Return x
    End Function
End Structure
]]></file>
                           </compilation>
            Dim errors = <errors>
BC35000: Requested operation is not available because the runtime library function 'System.Activator.CreateInstance' is not defined.
        Dim x As New I()
                 ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Guid..ctor' is not defined.
        Dim x As New I()
                 ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Type.GetTypeFromCLSID' is not defined.
        Dim x As New I()
                 ~~~~~~~
</errors>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            compilation0.AssertTheseDiagnostics()
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
        End Sub

        ' See C# AddHandler_Simple and RemoveHandler_Simple.
        <Fact()>
        Public Sub AddRemoveHandler()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

Public Delegate Sub D()

<ComEventInterface(GetType(IE), GetType(Integer))>
Public Interface I1
    Event E As D
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
Public Interface I2
    Inherits I1
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public Interface IE
    Sub E
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Class C
    Sub Add(x As I1)
        AddHandler x.E, AddressOf M
    End Sub
    Sub Remove(Of T As {Structure, I2})(x As T)
        RemoveHandler x.E, AddressOf M
    End Sub
    Sub M()
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(2, references.Length)
                                                           Assert.Equal("mscorlib", references(0).Name)
                                                           Assert.Equal("System.Core", references(1).Name)

                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I1")
                                                           Dim attributes = type.GetAttributes()
                                                           Assert.Equal(3, attributes.Length)
                                                           Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes(0).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.ComEventInterfaceAttribute(GetType(IE), GetType(IE))", attributes(1).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""I1"")", attributes(2).ToString())

                                                           type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("IE")
                                                           attributes = type.GetAttributes()
                                                           Assert.Equal(3, attributes.Length)
                                                           Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes(0).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.GuidAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")", attributes(1).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute", attributes(2).ToString())

                                                           Dim method = type.GetMember(Of PEMethodSymbol)("E")
                                                           Assert.NotNull(method)
                                                       End Sub
            Dim expectedAdd = <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  4
  IL_0000:  nop
  IL_0001:  ldtoken    "I1"
  IL_0006:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000b:  ldstr      "E"
  IL_0010:  newobj     "Sub System.Runtime.InteropServices.ComAwareEventInfo..ctor(System.Type, String)"
  IL_0015:  ldarg.1
  IL_0016:  ldarg.0
  IL_0017:  ldftn      "Sub C.M()"
  IL_001d:  newobj     "Sub D..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Sub System.Runtime.InteropServices.ComAwareEventInfo.AddEventHandler(Object, System.Delegate)"
  IL_0027:  nop
  IL_0028:  ret
}
]]>
            Dim expectedRemove = <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  4
  IL_0000:  nop
  IL_0001:  ldtoken    "I1"
  IL_0006:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000b:  ldstr      "E"
  IL_0010:  newobj     "Sub System.Runtime.InteropServices.ComAwareEventInfo..ctor(System.Type, String)"
  IL_0015:  ldarg.1
  IL_0016:  box        "T"
  IL_001b:  ldarg.0
  IL_001c:  ldftn      "Sub C.M()"
  IL_0022:  newobj     "Sub D..ctor(Object, System.IntPtr)"
  IL_0027:  callvirt   "Sub System.Runtime.InteropServices.ComAwareEventInfo.RemoveEventHandler(Object, System.Delegate)"
  IL_002c:  nop
  IL_002d:  ret
}
]]>
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugDll,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), SystemCoreRef})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            verifier.VerifyIL("C.Add", expectedAdd)
            verifier.VerifyIL("C.Remove(Of T)", expectedRemove)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugDll,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True), SystemCoreRef})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            verifier.VerifyIL("C.Add", expectedAdd)
            verifier.VerifyIL("C.Remove(Of T)", expectedRemove)
        End Sub

        <Fact()>
        Public Sub [RaiseEvent]()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

Public Delegate Sub D()

<ComEventInterface(GetType(IE), GetType(Integer))>
Public Interface I
    Event E As D
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public Interface IE
    Sub E
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Class C
    Implements I
    Friend Event E As D Implements I.E
    Sub Raise()
        RaiseEvent E
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Assert.Equal("mscorlib", references(0).Name)

                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim attributes = type.GetAttributes()
                                                           Assert.Equal(3, attributes.Length)
                                                           Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes(0).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.ComEventInterfaceAttribute(GetType(IE), GetType(IE))", attributes(1).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""I"")", attributes(2).ToString())

                                                           type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("IE")
                                                           attributes = type.GetAttributes()
                                                           Assert.Equal(3, attributes.Length)
                                                           Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes(0).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.GuidAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")", attributes(1).ToString())
                                                           Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute", attributes(2).ToString())

                                                           Dim method = type.GetMember(Of PEMethodSymbol)("E")
                                                           Assert.NotNull(method)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugDll,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugDll,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <WorkItem(837420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837420")>
        <Fact()>
        Public Sub BC31556ERR_SourceInterfaceMustBeInterface()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

Public Delegate Sub D()

<ComEventInterface(GetType(Object()), GetType(Object))>
Public Interface I1
    Event E As D
End Interface

<ComEventInterface(GetType(Object()), GetType(Object))>
Public Interface I2
    Event E As D
End Interface

<ComEventInterface(Nothing, Nothing)>
Public Interface I3
    Event E As D
End Interface

<ComEventInterface(Nothing, Nothing)>
Public Interface I4
    Event E As D
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M1(x1 As I1)
        AddHandler x1.E, AddressOf H
    End Sub
    Sub M2(x2 As I2)
    End Sub
    Sub M3(x3 As I3)
    End Sub
    Sub M4(x4 As I4)
        AddHandler x4.E, AddressOf H
    End Sub
    Sub H()
    End Sub
End Class
]]></file>
                           </compilation>
            ' Note: Dev12 reports errors for all four interfaces,
            ' even though only I1.E and I4.E are referenced.
            Dim errors = <errors>
BC31556: Interface 'I1' has an invalid source interface which is required to embed event 'Event E As D'.
        AddHandler x1.E, AddressOf H
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31556: Interface 'I4' has an invalid source interface which is required to embed event 'Event E As D'.
        AddHandler x4.E, AddressOf H
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            compilation0.AssertTheseDiagnostics()
            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                sources1,
                references:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), SystemCoreRef})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, <errors/>)
            compilation1 = CreateCompilationWithMscorlibAndReferences(
                sources1,
                references:={compilation0.EmitToImageReference(embedInteropTypes:=True), SystemCoreRef})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, <errors/>)
        End Sub

        <Fact()>
        Public Sub BC31557ERR_EventNoPIANoBackingMember()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

Public Delegate Sub D()

<ComEventInterface(GetType(IE), GetType(Object))>
Public Interface I
    Event E As D
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public Interface IE
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M1(x As I)
        AddHandler x.E, AddressOf M2
    End Sub
    Sub M2()
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31557: Source interface 'IE' is missing method 'E', which is required to embed event 'Event E As D'.
        AddHandler x.E, AddressOf M2
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            compilation0.AssertTheseDiagnostics()
            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                sources1,
                references:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), SystemCoreRef})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, <errors/>)
            compilation1 = CreateCompilationWithMscorlibAndReferences(
                sources1,
                references:={compilation0.EmitToImageReference(embedInteropTypes:=True), SystemCoreRef})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, <errors/>)
        End Sub

        ' See C# MissingComImport test.
        <Fact()>
        Public Sub BC31543ERR_NoPIAAttributeMissing2_ComImport()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
Public Delegate Sub D()
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Event D As D
End Interface
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Sub M(o As I)
        AddHandler o.D, Nothing
    End Sub
End Module
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31543: Interop type 'I' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.ComImportAttribute' attribute.
        AddHandler o.D, Nothing
        ~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            Dim errorsMetadataOnly = <errors>
BC31543: Interop type 'I' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.ComImportAttribute' attribute.
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errorsMetadataOnly)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errorsMetadataOnly)
        End Sub

        ' See C# MissingGuid test.
        <Fact()>
        Public Sub BC31543ERR_NoPIAAttributeMissing2_Guid()
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
Public Delegate Sub D()
<ComImport()>
Public Interface I
    Event D As D
End Interface
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Sub M(o As I)
        AddHandler o.D, Nothing
    End Sub
End Module
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31543: Interop type 'I' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.GuidAttribute' attribute.
        AddHandler o.D, Nothing
        ~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            Dim errorsMetadataOnly = <errors>
BC31543: Interop type 'I' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.GuidAttribute' attribute.
</errors>
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            compilation1.AssertTheseDiagnostics()
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errorsMetadataOnly)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errorsMetadataOnly)
        End Sub

        <Fact()>
        Public Sub InterfaceTypeAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Public Interface I1
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
<InterfaceType(CShort(ComInterfaceType.InterfaceIsIUnknown))>
Public Interface I2
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Implements I1, I2
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I1")
                                                           Dim attr = type.GetAttributes("System.Runtime.InteropServices", "InterfaceTypeAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)", attr.ToString())
                                                           type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I2")
                                                           attr = type.GetAttributes("System.Runtime.InteropServices", "InterfaceTypeAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.InterfaceTypeAttribute(1)", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub BestFitMappingAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
<BestFitMapping(True)>
Public Interface I1
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
<BestFitMapping(False)>
Public Interface I2
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Implements I1, I2
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I1")
                                                           Dim attr = type.GetAttributes("System.Runtime.InteropServices", "BestFitMappingAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.BestFitMappingAttribute(True)", attr.ToString())
                                                           type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I2")
                                                           attr = type.GetAttributes("System.Runtime.InteropServices", "BestFitMappingAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.BestFitMappingAttribute(False)", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub FlagsAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<Flags()>
Public Enum E
    A = 0
End Enum
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Sub M(x As E)
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("E")
                                                           Dim attr = type.GetAttributes("System", "FlagsAttribute").Single()
                                                           Assert.Equal("System.FlagsAttribute", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub DefaultMemberAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Reflection
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
<DefaultMember("M")>
Public Interface I
    Function M() As Integer()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Sub M(x As I)
        x.M()
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim attr = type.GetAttributes("System.Reflection", "DefaultMemberAttribute").Single()
                                                           Assert.Equal("System.Reflection.DefaultMemberAttribute(""M"")", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub LCIDConversionAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    <LCIDConversion(123)>
    Sub M()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Implements I
    Private Sub M() Implements I.M
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim method = type.GetMember(Of PEMethodSymbol)("M")
                                                           Dim attr = method.GetAttributes("System.Runtime.InteropServices", "LCIDConversionAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.LCIDConversionAttribute(123)", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub DispIdAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    <DispId(124)>
    Sub M()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Implements I
    Private Sub M() Implements I.M
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim method = type.GetMember(Of PEMethodSymbol)("M")
                                                           Dim attr = method.GetAttributes("System.Runtime.InteropServices", "DispIdAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.DispIdAttribute(124)", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub ParamArrayAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Sub M(ParamArray x As Integer())
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Implements I
    Private Sub M(ParamArray x As Integer()) Implements I.M
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim method = type.GetMember(Of PEMethodSymbol)("M")
                                                           Dim param = method.Parameters(0)
                                                           Assert.Equal(0, param.GetAttributes().Length)
                                                           Assert.True(param.IsParamArray)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub DateTimeConstantAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Sub M(<[Optional](), DateTimeConstant(987654321)> x As DateTime)
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Sub M(x As I)
        x.M()
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim method = type.GetMember(Of PEMethodSymbol)("M")
                                                           Assert.Equal(New Date(987654321), method.Parameters(0).ExplicitDefaultValue)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            compilation1.AssertTheseDiagnostics(<errors>
BC30455: Argument not specified for parameter 'x' of 'Sub M(x As Date)'.
        x.M()
          ~
                                                    </errors>)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub DecimalConstantAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Sub M1(<[Optional](), DecimalConstant(0, 0, Integer.MinValue, -2, -3)> x As Decimal)
    Sub M2(<[Optional](), DecimalConstant(0, 0, UInteger.MaxValue, 2, 3)> x As Decimal)
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Sub M(x As I)
        x.M1()
        x.M2()
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim method = type.GetMember(Of PEMethodSymbol)("M1")
                                                           Assert.Equal(39614081275578912866186559485D, method.Parameters(0).ExplicitDefaultValue)
                                                           method = type.GetMember(Of PEMethodSymbol)("M2")
                                                           Assert.Equal(79228162495817593528424333315D, method.Parameters(0).ExplicitDefaultValue)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            compilation1.AssertTheseDiagnostics(<errors>
BC30455: Argument not specified for parameter 'x' of 'Sub M1(x As Decimal)'.
        x.M1()
          ~~
BC30455: Argument not specified for parameter 'x' of 'Sub M2(x As Decimal)'.
        x.M2()
          ~~
                                                    </errors>)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub DefaultParameterValueAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Sub M(<[Optional](), DefaultParameterValue(123.456)> x As Decimal)
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Sub M(x As I)
        x.M()
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndReferences(
                sources0,
                references:={SystemRef})
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(2, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim method = type.GetMember(Of PEMethodSymbol)("M")
                                                           Dim attr = method.Parameters(0).GetAttributes("System.Runtime.InteropServices", "DefaultParameterValueAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.DefaultParameterValueAttribute(123.456)", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            compilation1.AssertTheseDiagnostics(<errors>
BC30455: Argument not specified for parameter 'x' of 'Sub M(x As Decimal)'.
        x.M()
          ~
                                                    </errors>)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub UnmanagedFunctionPointerAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, SetLastError:=True)>
Public Delegate Sub D()
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Sub M(x As D)
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("D")
                                                           Dim attr = type.GetAttributes("System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute").Single()
                                                           Assert.Equal("System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.StdCall, SetLastError:=True)", attr.ToString())
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub PreserveSigAttribute()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    <PreserveSig()>
    Sub M()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Structure S
    Sub M(x As I)
        x.M()
    End Sub
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                           Dim type = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I")
                                                           Dim method = type.GetMember(Of PEMethodSymbol)("M")
                                                           Assert.Equal(MethodImplAttributes.IL Or MethodImplAttributes.PreserveSig, CType(method.ImplementationAttributes, MethodImplAttributes))
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        ' See C# TypeNameConflict1 test.
        <Fact()>
        Public Sub BC31552ERR_DuplicateLocalTypes3()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I1
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58272")>
Public Interface I2
    Inherits I1
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58256")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58273")>
Public Interface I1
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58274")>
Public Interface I3
    Inherits I1
End Interface
]]></file>
                           </compilation>
            Dim sources2 = <compilation name="2">
                               <file name="a.vb"><![CDATA[
Module M
    Sub M(x As I2, y As I3)
    End Sub
End Module
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31552: Cannot embed interop type 'I1' found in both assembly '0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and '1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider disabling the embedding of interop types.
</errors>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            VerifyEmitDiagnostics(compilation0)
            Dim compilation1 = CreateCompilationWithMscorlib(sources1)
            VerifyEmitDiagnostics(compilation1)
            ' No errors for /r:0.dll /l:1.dll.
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=False), New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
            ' Errors for /l:0.dll /l:1.dll.
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), New VisualBasicCompilationReference(compilation1, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True), compilation1.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
        End Sub

        ' See C# TypeNameConflict2 test.
        <Fact()>
        Public Sub BC31560ERR_LocalTypeNameClash2()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I0
    Sub M1(o As I1)
    Sub M2(o As I2)
    Sub M3(o As I3)
    Sub M4(o As I4)
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58272")>
Public Interface I1
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58273")>
Public Interface I2
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58274")>
Public Interface I3
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58275")>
Public Interface I4
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Module M
    Sub M(o As I0)
        o.M1(Nothing)
        o.M2(Nothing)
        o.M3(Nothing)
    End Sub
End Module
Class I1
End Class
Delegate Sub I2()
Structure I3
End Structure
Structure I4
End Structure
]]></file>
                           </compilation>
            ' Note: Dev11 does not report any errors although the
            ' generated assembly fails peverify in these cases.
            Dim errors = <errors>
BC31560: Embedding the interop type 'I1' from assembly '0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' causes a name clash in the current assembly. Consider disabling the embedding of interop types.
BC31560: Embedding the interop type 'I2' from assembly '0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' causes a name clash in the current assembly. Consider disabling the embedding of interop types.
BC31560: Embedding the interop type 'I3' from assembly '0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' causes a name clash in the current assembly. Consider disabling the embedding of interop types.
</errors>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            VerifyEmitDiagnostics(compilation0)
            ' No errors for /r:0.dll.
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=False)})
            VerifyEmitDiagnostics(compilation1)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=False)})
            VerifyEmitDiagnostics(compilation1)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
            ' Errors for /l:0.dll.
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
            compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
        End Sub

        <Fact()>
        Public Sub NoIndirectReference()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Sub M()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Public Class A
    Public Shared F As Object
    Private Shared Sub M(o As I)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources2 = <compilation name="2">
                               <file name="a.vb"><![CDATA[
Class B
    Private Shared Sub M(o As I)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(1, references.Length)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation1)
            AssertTheseDiagnostics(verifier, (<errors/>))
            ' No errors for /r:0.dll /r:1.dll.
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=False), New VisualBasicCompilationReference(compilation1, embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2)
            AssertTheseDiagnostics(verifier, (<errors/>))
            ' Errors for /l:0.dll /r:1.dll.
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), New VisualBasicCompilationReference(compilation1, embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), compilation1.EmitToImageReference(embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True), New VisualBasicCompilationReference(compilation1, embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True), compilation1.EmitToImageReference(embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub IndirectReference()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Sub M()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Public Class A
    Public Shared F As Object
    Private Shared Sub M(o As I)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources2 = <compilation name="2">
                               <file name="a.vb"><![CDATA[
Class B
    Private Shared F = A.F
    Private Shared Sub M(o As I)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC40059: A reference was created to embedded interop assembly '0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' because of an indirect reference to that assembly from assembly '1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider changing the 'Embed Interop Types' property on either assembly.
</errors>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Dim references = [module].GetReferencedAssemblySymbols()
                                                           Assert.Equal(2, references.Length)
                                                           Assert.Equal("1", references(1).Name)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation1)
            AssertTheseDiagnostics(verifier, (<errors/>))
            ' No errors for /r:0.dll /r:1.dll.
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=False), New VisualBasicCompilationReference(compilation1, embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2)
            AssertTheseDiagnostics(verifier, (<errors/>))
            ' Errors for /l:0.dll /r:1.dll.
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), New VisualBasicCompilationReference(compilation1, embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, errors)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True), compilation1.EmitToImageReference(embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, errors)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True), New VisualBasicCompilationReference(compilation1, embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, errors)
            compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources2,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True), compilation1.EmitToImageReference(embedInteropTypes:=False)})
            verifier = CompileAndVerify(compilation2, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, errors)
        End Sub

        <Fact()>
        Public Sub ImplementedInterfacesAndTheirMembers_1()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I1
    Sub M1()
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58272")>
Public Interface I2
    Inherits I1
    Sub M2()
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58273")>
Public Interface I3
    Inherits I2
    Sub M3()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Interface I
    Inherits I3
End Interface
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            CompileAndVerify(compilation0)
            compilation0.AssertTheseDiagnostics()
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Assert.Equal(1, [module].GetReferencedAssemblySymbols().Length)
                                                           Dim i1 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I1")
                                                           Dim m1 = i1.GetMember(Of PEMethodSymbol)("M1")
                                                           Dim i2 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I2")
                                                           Dim m2 = i2.GetMember(Of PEMethodSymbol)("M2")
                                                           Dim i3 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I3")
                                                           Dim m3 = i3.GetMember(Of PEMethodSymbol)("M3")
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            CompileAndVerify(compilation1, symbolValidator:=validator)
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            CompileAndVerify(compilation2, symbolValidator:=validator)
        End Sub

        ' See C# ImplementedInterfacesAndTheirMembers_2
        ' and ExplicitInterfaceImplementation tests.
        <Fact()>
        Public Sub ImplementedInterfacesAndTheirMembers_2()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I1
    Sub M1()
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58272")>
Public Interface I2
    Inherits I1
    Sub M2()
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58273")>
Public Interface I3
    Inherits I2
    Sub M3()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Implements I3
    Sub M() Implements I1.M1, I2.M2, I3.M3
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            CompileAndVerify(compilation0)
            compilation0.AssertTheseDiagnostics()
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Assert.Equal(1, [module].GetReferencedAssemblySymbols().Length)
                                                           Dim i1 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I1")
                                                           Dim m1 = i1.GetMember(Of PEMethodSymbol)("M1")
                                                           Dim i2 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I2")
                                                           Dim m2 = i2.GetMember(Of PEMethodSymbol)("M2")
                                                           Dim i3 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I3")
                                                           Dim m3 = i3.GetMember(Of PEMethodSymbol)("M3")
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            CompileAndVerify(compilation1, symbolValidator:=validator)
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            CompileAndVerify(compilation2, symbolValidator:=validator)
        End Sub

        <Fact()>
        Public Sub ImplementedInterfacesAndTheirMembers_3()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I1
    Sub M1()
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58272")>
Public Interface I2
    Inherits I1
    Sub M2()
End Interface
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58273")>
Public Interface I3
    Inherits I2
    Sub M3()
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M(o As I3)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            CompileAndVerify(compilation0)
            compilation0.AssertTheseDiagnostics()
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Assert.Equal(1, [module].GetReferencedAssemblySymbols().Length)
                                                           Dim i1 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I1")
                                                           Assert.Equal(0, i1.GetMembers().Length)
                                                           Dim i2 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I2")
                                                           Assert.Equal(0, i2.GetMembers().Length)
                                                           Dim i3 = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("I3")
                                                           Assert.Equal(0, i3.GetMembers().Length)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            CompileAndVerify(compilation1, symbolValidator:=validator)
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                additionalRefs:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            CompileAndVerify(compilation2, symbolValidator:=validator)
        End Sub

        <Fact()>
        Public Sub EmbedEnum()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58271")>
Public Interface I
    Function F() As E
End Interface
Public Enum E
    A
    B = 3
End Enum
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Class C
    Private Const F As E = E.B
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))
            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                           Assert.Equal(1, [module].GetReferencedAssemblySymbols().Length)
                                                           Dim e = [module].GlobalNamespace.GetMember(Of PENamedTypeSymbol)("E")
                                                           Dim f = e.GetMember(Of PEFieldSymbol)("A")
                                                           Assert.Equal(f.ConstantValue, 0)
                                                           f = e.GetMember(Of PEFieldSymbol)("B")
                                                           Assert.Equal(f.ConstantValue, 3)
                                                           f = e.GetMember(Of PEFieldSymbol)("value__")
                                                           Assert.False(f.HasConstantValue)
                                                       End Sub
            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                sources1,
                references:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
            compilation1 = CreateCompilationWithMscorlibAndReferences(
                sources1,
                references:={compilation0.EmitToImageReference(embedInteropTypes:=True)})
            verifier = CompileAndVerify(compilation1, symbolValidator:=validator)
            AssertTheseDiagnostics(verifier, (<errors/>))
        End Sub

        <Fact()>
        Public Sub ErrorType1()
            Dim pia1 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("1.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
Public Interface I1
End Interface
]]></file>
                       </compilation>
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
Public Interface I2
    Inherits I1
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M(o As I2)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31539: Cannot find the interop type that matches the embedded type 'I1'. Are you missing an assembly reference?
</errors>

            Dim piaCompilation1 = CreateCompilationWithMscorlib(pia1)
            CompileAndVerify(piaCompilation1)

            Dim piaCompilation2 = CreateCompilationWithMscorlibAndReferences(
                pia2,
                references:={New VisualBasicCompilationReference(piaCompilation1, embedInteropTypes:=True)})
            CompileAndVerify(piaCompilation2)

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, errors)

            Dim compilation2 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={piaCompilation2.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
        End Sub

        <Fact()>
        Public Sub ErrorType2()
            Dim pia1 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("1.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
Public Interface I1
End Interface
]]></file>
                       </compilation>
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
<ComEventInterface(GetType(I1), GetType(Object))>
Public Interface I2
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M(o As I2)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31539: Cannot find the interop type that matches the embedded type 'I1'. Are you missing an assembly reference?
</errors>

            Dim piaCompilation1 = CreateCompilationWithMscorlib(pia1)
            CompileAndVerify(piaCompilation1)

            Dim piaCompilation2 = CreateCompilationWithMscorlibAndReferences(
                pia2,
                references:={New VisualBasicCompilationReference(piaCompilation1, embedInteropTypes:=True)})
            CompileAndVerify(piaCompilation2)

            Dim fullName = MetadataTypeName.FromFullName("I1")
            Dim isNoPiaLocalType = False

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, errors)

            Dim assembly = compilation1.SourceModule.GetReferencedAssemblySymbols()(1)
            Dim [module] = assembly.Modules(0)
            Assert.IsType(Of MissingMetadataTypeSymbol.TopLevel)([module].LookupTopLevelMetadataType(fullName))
            Assert.Null(assembly.GetTypeByMetadataName(fullName.FullName))

            Dim compilation2 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={piaCompilation2.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)

            assembly = compilation2.SourceModule.GetReferencedAssemblySymbols()(1)
            [module] = assembly.Modules(0)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(DirectCast([module], PEModuleSymbol).LookupTopLevelMetadataType(fullName, isNoPiaLocalType))
            Assert.True(isNoPiaLocalType)
            Assert.IsType(Of MissingMetadataTypeSymbol.TopLevel)([module].LookupTopLevelMetadataType(fullName))
            Assert.Null(assembly.GetTypeByMetadataName(fullName.FullName))

            Dim compilation3 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2)})
            CompileAndVerify(compilation3)

            assembly = compilation3.SourceModule.GetReferencedAssemblySymbols()(1)
            [module] = assembly.Modules(0)
            Assert.IsType(Of MissingMetadataTypeSymbol.TopLevel)([module].LookupTopLevelMetadataType(fullName))
            Assert.Null(assembly.GetTypeByMetadataName(fullName.FullName))

            Dim compilation4 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={MetadataReference.CreateFromImage(piaCompilation2.EmitToArray())})
            CompileAndVerify(compilation4)

            assembly = compilation4.SourceModule.GetReferencedAssemblySymbols()(1)
            [module] = assembly.Modules(0)
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(DirectCast([module], PEModuleSymbol).LookupTopLevelMetadataType(fullName, isNoPiaLocalType))
            Assert.True(isNoPiaLocalType)
            Assert.IsType(Of MissingMetadataTypeSymbol.TopLevel)([module].LookupTopLevelMetadataType(fullName))
            Assert.Null(assembly.GetTypeByMetadataName(fullName.FullName))
        End Sub

        <Fact()>
        Public Sub ErrorType3()
            Dim pia1 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("1.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
Public Interface I1
End Interface
]]></file>
                       </compilation>
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
<ComEventInterface(GetType(I1), GetType(Object))>
Public Interface I2
    Sub M2()
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim o As I2 = Nothing
        o.M2()
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC31539: Cannot find the interop type that matches the embedded type 'I1'. Are you missing an assembly reference?
        o.M2()
        ~~~~~~
</errors>

            Dim piaCompilation1 = CreateCompilationWithMscorlib(pia1)
            CompileAndVerify(piaCompilation1)

            Dim piaCompilation2 = CreateCompilationWithMscorlibAndReferences(
                pia2,
                references:={New VisualBasicCompilationReference(piaCompilation1, embedInteropTypes:=True)})
            'CompileAndVerify(piaCompilation2, emitOptions:=EmitOptions.RefEmitBug)

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)

            Dim compilation2 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={piaCompilation2.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)

            Dim compilation3 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2)})
            CompileAndVerify(compilation3, verify:=False)

            Dim compilation4 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={MetadataReference.CreateFromImage(piaCompilation2.EmitToArray())})
            CompileAndVerify(compilation4, verify:=False)
        End Sub

        <Fact()>
        Public Sub ErrorType4()
            Dim pia1 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("1.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
Public Interface I1
End Interface
]]></file>
                       </compilation>
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
<ComEventInterface(GetType(IList(Of List(Of I1))), GetType(Object))>
Public Interface I2
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M(o As I2)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC36924: Type 'List(Of I1)' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
</errors>

            Dim piaCompilation1 = CreateCompilationWithMscorlib(pia1)
            CompileAndVerify(piaCompilation1)

            Dim piaCompilation2 = CreateCompilationWithMscorlibAndReferences(
                pia2,
                references:={New VisualBasicCompilationReference(piaCompilation1, embedInteropTypes:=True)})
            CompileAndVerify(piaCompilation2)

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, errors)

            Dim compilation2 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={piaCompilation2.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)
        End Sub

        <Fact()>
        Public Sub ErrorType5()
            Dim pia1 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("1.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
Public Interface I1
End Interface
]]></file>
                       </compilation>
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
<ComEventInterface(GetType(IList(Of List(Of I1))), GetType(Object))>
Public Interface I2
    Sub M2()
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim o As I2 = Nothing
        o.M2()
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC36924: Type 'List(Of I1)' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
        o.M2()
        ~~~~~~
</errors>

            Dim piaCompilation1 = CreateCompilationWithMscorlib(pia1)
            CompileAndVerify(piaCompilation1)

            Dim piaCompilation2 = CreateCompilationWithMscorlibAndReferences(
                pia2,
                references:={New VisualBasicCompilationReference(piaCompilation1, embedInteropTypes:=True)})
            'CompileAndVerify(piaCompilation2, emitOptions:=EmitOptions.RefEmitBug)

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)

            Dim compilation2 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={piaCompilation2.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)
        End Sub

        <Fact()>
        Public Sub ErrorType6()
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
Public Interface I2
    Inherits I1
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M(o As I2)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC30002: Type 'I1' is not defined.
</errors>
            Dim piaCompilation2 = CreateCompilationWithMscorlib(pia2)
            'CompileAndVerify(piaCompilation2, emitOptions:=EmitOptions.RefEmitBug)
            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, errors)
        End Sub

        <Fact()>
        Public Sub ErrorType7()
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
Public Interface I2
    Sub M2(o As I1)
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation name="Consumer">
                               <file name="a.vb"><![CDATA[
Class C
    Sub M(o As I2)
        o.M2(Nothing)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC30002: Type 'I1' is not defined.
        o.M2(Nothing)
        ~~~~~~~~~~~~~
</errors>
            Dim piaCompilation2 = CreateCompilationWithMscorlib(pia2)
            'CompileAndVerify(piaCompilation2, emitOptions:=EmitOptions.RefEmitBug)
            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)
        End Sub

        <Fact()>
        Public Sub ErrorType8()
            Dim pia1 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("1.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
Public Interface I1
End Interface
]]></file>
                       </compilation>
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
<ComEventInterface(GetType(List(Of I1)), GetType(Object))>
Public Interface I2
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M(o As I2)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC36924: Type 'List(Of I1)' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
</errors>

            Dim piaCompilation1 = CreateCompilationWithMscorlib(pia1)
            CompileAndVerify(piaCompilation1)

            Dim piaCompilation2 = CreateCompilationWithMscorlibAndReferences(
                pia2,
                references:={New VisualBasicCompilationReference(piaCompilation1, embedInteropTypes:=True)})
            CompileAndVerify(piaCompilation2)

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1, errors)

            Dim compilation2 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={piaCompilation2.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2, errors)

            Dim compilation3 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2)})
            CompileAndVerify(compilation3)

            Dim compilation4 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={MetadataReference.CreateFromImage(piaCompilation2.EmitToArray())})
            CompileAndVerify(compilation4)
        End Sub

        <Fact()>
        Public Sub ErrorType9()
            Dim pia1 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("1.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
Public Interface I1
End Interface
]]></file>
                       </compilation>
            Dim pia2 = <compilation>
                           <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("2.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58258")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58280")>
<ComEventInterface(GetType(List(Of I1)), GetType(Object))>
Public Interface I2
    Sub M2()
End Interface
]]></file>
                       </compilation>
            Dim consumer = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim o As I2 = Nothing
        o.M2()
    End Sub
End Class
]]></file>
                           </compilation>
            Dim errors = <errors>
BC36924: Type 'List(Of I1)' cannot be used across assembly boundaries because it has a generic type parameter that is an embedded interop type.
        o.M2()
        ~~~~~~
</errors>

            Dim piaCompilation1 = CreateCompilationWithMscorlib(pia1)
            CompileAndVerify(piaCompilation1)

            Dim piaCompilation2 = CreateCompilationWithMscorlibAndReferences(
                pia2,
                references:={New VisualBasicCompilationReference(piaCompilation1, embedInteropTypes:=True)})
            'CompileAndVerify(piaCompilation2, emitOptions:=EmitOptions.RefEmitBug)

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2, embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation1, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation1)

            Dim compilation2 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={piaCompilation2.EmitToImageReference(embedInteropTypes:=True)})
            VerifyEmitDiagnostics(compilation2, errors)
            VerifyEmitMetadataOnlyDiagnostics(compilation2)

            Dim compilation3 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={New VisualBasicCompilationReference(piaCompilation2)})
            CompileAndVerify(compilation3, verify:=False)

            Dim compilation4 = CreateCompilationWithMscorlibAndReferences(
                consumer,
                references:={MetadataReference.CreateFromImage(piaCompilation2.EmitToArray())})
            CompileAndVerify(compilation4, verify:=False)
        End Sub

        <Fact(), WorkItem(673546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673546")>
        Public Sub MissingComAwareEventInfo()
            Dim sources0 = <compilation name="0">
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

Public Delegate Sub D()

<ComEventInterface(GetType(IE), GetType(Integer))>
Public Interface I1
    Event E As D
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
Public Interface I2
    Inherits I1
End Interface

<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public Interface IE
    Sub E
End Interface
]]></file>
                           </compilation>
            Dim sources1 = <compilation name="1">
                               <file name="a.vb"><![CDATA[
Class C
    Sub Add(x As I1)
        AddHandler x.E, Sub() System.Console.WriteLine()
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            Dim verifier = CompileAndVerify(compilation0)
            AssertTheseDiagnostics(verifier, (<errors/>))

            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
                sources1,
                options:=TestOptions.DebugDll,
                additionalRefs:={New VisualBasicCompilationReference(compilation0, embedInteropTypes:=True)})

            AssertTheseEmitDiagnostics(compilation1,
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.InteropServices.ComAwareEventInfo..ctor' is not defined.
        AddHandler x.E, Sub() System.Console.WriteLine()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        Private Shared Sub AssertTheseDiagnostics(verifier As CompilationVerifier, diagnostics As XElement)
            verifier.Diagnostics.AssertTheseDiagnostics(diagnostics)
        End Sub

        <Fact()>
        Public Sub DefaultValueWithoutOptional_01()
            Dim sources1 = <![CDATA[
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly pia
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 0E 47 65 6E 65 72 61 6C 50 49 41 2E 64 6C   // ...GeneralPIA.dl
                                                                                                                 6C 00 00 )                                        // l..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
}
.module pia.dll
// MVID: {FDF1B1F7-A867-40B9-83CD-3F75B2D2B3C2}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY

.class interface public abstract auto ansi import IA
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 45 41 44 42 45 45 46 2D 43 41 46 45   // ..$DEADBEEF-CAFE
                                                                                                  2D 42 41 42 45 2D 42 41 41 44 2D 44 45 41 44 43   // -BABE-BAAD-DEADC
                                                                                                  30 44 45 30 30 30 30 00 00 )                      // 0DE0000..
  .method public newslot abstract strict virtual 
          instance void  M(int32 x) cil managed
  {
    .param [1] = int32(0x0000000C)
  } // end of method IA::M

} // end of class IA
]]>.Value
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
    Public Class B
        Implements IA
        Sub M(x As Integer) Implements IA.M
        End Sub
    End Class
]]></file>
                           </compilation>
            Dim reference1 = CompileIL(sources1, appendDefaultHeader:=False, embedInteropTypes:=True)
            CompileAndVerify(sources2, additionalRefs:={reference1}, symbolValidator:=
                                                Sub([module] As ModuleSymbol)
                                                    DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                    Dim ia = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("IA")
                                                    Dim m = CType(ia.GetMember("M"), MethodSymbol)
                                                    Dim p = DirectCast(m.Parameters(0), PEParameterSymbol)
                                                    Assert.False(p.IsMetadataOptional)
                                                    Assert.Equal(ParameterAttributes.HasDefault, p.ParamFlags)
                                                    Assert.Equal(CObj(&H0000000C), p.ExplicitDefaultConstantValue.Value)
                                                    Assert.False(p.HasExplicitDefaultValue)
                                                    Assert.Throws(GetType(InvalidOperationException), Sub()
                                                                                                          Dim tmp = p.ExplicitDefaultValue
                                                                                                      End Sub)
                                                End Sub).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub DefaultValueWithoutOptional_02()
            Dim sources1 = <![CDATA[
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly pia
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 0E 47 65 6E 65 72 61 6C 50 49 41 2E 64 6C   // ...GeneralPIA.dl
                                                                                                                 6C 00 00 )                                        // l..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
}
.module pia.dll
// MVID: {FDF1B1F7-A867-40B9-83CD-3F75B2D2B3C2}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY

.class interface public abstract auto ansi import IA
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 45 41 44 42 45 45 46 2D 43 41 46 45   // ..$DEADBEEF-CAFE
                                                                                                  2D 42 41 42 45 2D 42 41 41 44 2D 44 45 41 44 43   // -BABE-BAAD-DEADC
                                                                                                  30 44 45 30 30 30 30 00 00 )                      // 0DE0000..
  .method public newslot abstract strict virtual 
          instance void  M(valuetype [mscorlib]System.DateTime x) cil managed
  {
  .param [1]
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 B1 68 DE 3A 00 00 00 00 00 00 )             // ...h.:......
  } // end of method IA::M

} // end of class IA
]]>.Value
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
    Public Class B
        Implements IA
        Sub M(x As System.DateTime) Implements IA.M
        End Sub
    End Class
]]></file>
                           </compilation>
            Dim reference1 = CompileIL(sources1, appendDefaultHeader:=False, embedInteropTypes:=True)
            CompileAndVerify(sources2, additionalRefs:={reference1}, symbolValidator:=
                                                Sub([module] As ModuleSymbol)
                                                    DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                                    Dim ia = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("IA")
                                                    Dim m = CType(ia.GetMember("M"), MethodSymbol)
                                                    Dim p = DirectCast(m.Parameters(0), PEParameterSymbol)
                                                    Assert.False(p.IsMetadataOptional)
                                                    Assert.Equal(ParameterAttributes.None, p.ParamFlags)
                                                    Assert.Equal("System.Runtime.CompilerServices.DateTimeConstantAttribute(987654321)", p.GetAttributes().Single().ToString())
                                                    Assert.Null(p.ExplicitDefaultConstantValue)
                                                    Assert.False(p.HasExplicitDefaultValue)
                                                    Assert.Throws(GetType(InvalidOperationException), Sub()
                                                                                                          Dim tmp = p.ExplicitDefaultValue
                                                                                                      End Sub)
                                                End Sub).VerifyDiagnostics()
        End Sub


        <Fact, WorkItem(8088, "https://github.com/dotnet/roslyn/issues/8088")>
        Public Sub ParametersWithoutNames()
            Dim sources =
<compilation>
    <file name="a.vb">
Public Class Program
    Sub M(x As I1) 
        x.M1(1, 2, 3)
    End Sub

    Sub M1(value As Integer) 
    End Sub

    Sub M2(Param As Integer) 
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                                                                         {
                                                                            AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.ParametersWithoutNames).
                                                                                GetReference(display:="ParametersWithoutNames.dll", embedInteropTypes:=True)
                                                                         },
                                                                         options:=TestOptions.ReleaseDll)


            AssertParametersWithoutNames(compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("I1").GetMember(Of MethodSymbol)("M1").Parameters, False)

            CompileAndVerify(compilation,
                             symbolValidator:=
                                Sub([module] As ModuleSymbol)
                                    DirectCast([module], PEModuleSymbol).Module.PretendThereArentNoPiaLocalTypes()
                                    AssertParametersWithoutNames([module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("I1").GetMember(Of MethodSymbol)("M1").Parameters, True)

                                    Dim p As PEParameterSymbol
                                    p = DirectCast([module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("Program").GetMember(Of MethodSymbol)("M").Parameters(0), PEParameterSymbol)
                                    Assert.Equal("x", DirectCast([module], PEModuleSymbol).Module.GetParamNameOrThrow(p.Handle))
                                    Assert.Equal("x", p.Name)
                                    Assert.Equal("x", p.MetadataName)
                                    p = DirectCast([module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("Program").GetMember(Of MethodSymbol)("M1").Parameters(0), PEParameterSymbol)
                                    Assert.Equal("value", DirectCast([module], PEModuleSymbol).Module.GetParamNameOrThrow(p.Handle))
                                    Assert.Equal("value", p.Name)
                                    Assert.Equal("value", p.MetadataName)
                                    p = DirectCast([module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("Program").GetMember(Of MethodSymbol)("M2").Parameters(0), PEParameterSymbol)
                                    Assert.Equal("Param", DirectCast([module], PEModuleSymbol).Module.GetParamNameOrThrow(p.Handle))
                                    Assert.Equal("Param", p.Name)
                                    Assert.Equal("Param", p.MetadataName)
                                End Sub).VerifyDiagnostics()
        End Sub

        Private Shared Sub AssertParametersWithoutNames(parameters As ImmutableArray(Of ParameterSymbol), isEmbedded As Boolean)
            Assert.True(DirectCast(parameters(0), PEParameterSymbol).Handle.IsNil)

            Dim p1 = DirectCast(parameters(1), PEParameterSymbol)
            Assert.True(p1.IsMetadataOptional)
            Assert.False(p1.Handle.IsNil)
            Assert.True(DirectCast(p1.ContainingModule, PEModuleSymbol).Module.MetadataReader.GetParameter(p1.Handle).Name.IsNil)

            Dim p2 = DirectCast(parameters(2), PEParameterSymbol)
            If isEmbedded Then
                Assert.True(p2.Handle.IsNil)
            Else
                Assert.True(DirectCast(p2.ContainingModule, PEModuleSymbol).Module.MetadataReader.GetParameter(p2.Handle).Name.IsNil)
            End If

            For Each p In parameters
                Assert.Equal("Param", p.Name)
                Assert.Equal("", p.MetadataName)
            Next
        End Sub
    End Class

End Namespace
