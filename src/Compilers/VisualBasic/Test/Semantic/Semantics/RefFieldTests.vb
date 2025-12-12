' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class RefFieldTests
        Inherits BasicTestBase

        ''' <summary>
        ''' Ref field in ref struct.
        ''' </summary>
        <Fact, WorkItem(65392, "https://github.com/dotnet/roslyn/issues/65392")>
        Public Sub RefField_01()
            Dim sourceA =
"public ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}"
            Dim compA = CreateCSharpCompilation(GetUniqueName(), sourceA, referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Net70))
            compA.VerifyDiagnostics()
            Dim refA = compA.EmitToImageReference()

            Dim sourceB =
"Imports System
Module Program
    Sub Main()
        Dim s = New S(Of Integer)()
        Console.WriteLine(s.F)
    End Sub
End Module"

            Dim compB = CreateCompilation(sourceB, references:={refA})
            compB.AssertTheseDiagnostics(<expected>
BC30668: 'S(Of Integer)' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'.
        Dim s = New S(Of Integer)()
                    ~~~~~~~~~~~~~
BC30656: Field 'F' is of an unsupported type.
        Console.WriteLine(s.F)
                          ~~~
</expected>)

            Dim field = compB.GetMember(Of FieldSymbol)("S.F")
            VerifyFieldSymbol(field, "S(Of T).F As T modreq(?)")
            Assert.NotNull(field.GetUseSiteErrorInfo())
            Assert.True(field.HasUnsupportedMetadata)
        End Sub

        <Fact, WorkItem(65392, "https://github.com/dotnet/roslyn/issues/65392")>
        Public Sub CanUsePassThroughRefStructInstances()
            Dim source =
"Imports System
Module Program
    Sub Main()
        Dim s = ""123"".AsSpan()
        Dim s2 As ReadOnlySpan(Of Char) = ""123"".AsSpan()
        Dim s3 = MemoryExtensions.AsSpan(""123"")
    End Sub
End Module"

            Dim comp = CreateCompilation(source, targetFramework:=TargetFramework.Net60)
            comp.AssertTheseDiagnostics(<expected>
BC30668: 'ReadOnlySpan(Of Char)' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'.
        Dim s2 As ReadOnlySpan(Of Char) = "123".AsSpan()
                  ~~~~~~~~~~~~~~~~~~~~~
</expected>)

            comp = CreateCompilation(source, targetFramework:=TargetFramework.Net70)
            comp.AssertTheseDiagnostics(<expected>
BC30668: 'ReadOnlySpan(Of Char)' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'.
        Dim s2 As ReadOnlySpan(Of Char) = "123".AsSpan()
                  ~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ''' <summary>
        ''' Ref field in class.
        ''' </summary>
        <Fact>
        Public Sub RefField_02()
            Dim sourceA =
".class public A<T>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .field public !T & modopt(object) modopt(int8) F
}"
            Dim refA = CompileIL(sourceA)

            Dim sourceB =
"Imports System
Module Program
    Sub Main()
        Dim a = New A(Of Integer)()
        Console.WriteLine(a.F)
    End Sub
End Module"

            Dim comp = CreateCompilation(sourceB, references:={refA})
            comp.AssertTheseDiagnostics(
<expected>
BC30656: Field 'F' is of an unsupported type.
        Console.WriteLine(a.F)
                          ~~~
</expected>)

            Dim field = comp.GetMember(Of FieldSymbol)("A.F")
            VerifyFieldSymbol(field, "A(Of T).F As T modopt(System.SByte) modopt(System.Object) modreq(?)")
            Assert.NotNull(field.GetUseSiteErrorInfo())
            Assert.True(field.HasUnsupportedMetadata)
        End Sub

        Private Shared Sub VerifyFieldSymbol(field As FieldSymbol, expectedDisplayString As String)
            Assert.Equal(CodeAnalysis.RefKind.None, DirectCast(field, IFieldSymbol).RefKind)
            Assert.Empty(DirectCast(field, IFieldSymbol).RefCustomModifiers)
            Assert.Equal(expectedDisplayString, field.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub MemberRefMetadataDecoder_FindFieldBySignature()
            Dim sourceA =
".class public sealed R<T> extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field private !0 F1
  .field public !0& F1
  .field private !0& modopt(int32) F2
  .field public !0& modopt(object) F2
  .field private int32& F3
  .field public int8& F3
}"
            Dim refA = CompileIL(sourceA)

            Dim sourceB =
"class B
{
    static object F1() => new R<object>().F1;
    static object F2() => new R<object>().F2;
    static int F3() => new R<object>().F3;
}"
            Dim compB = CreateCSharpCompilation(GetUniqueName(), sourceB, referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Net70, {refA}))
            compB.VerifyDiagnostics()
            Dim refB = compB.EmitToImageReference()

            Dim comp = CreateCompilation("", references:={refA, refB})
            comp.AssertNoDiagnostics()

            ' Call MemberRefMetadataDecoder.FindFieldBySignature() indirectly from MetadataDecoder.GetSymbolForILToken().
            Dim [module] = DirectCast(comp.GetReferencedAssemblySymbol(refB).Modules(0), PEModuleSymbol)
            Dim decoder = New MetadataDecoder([module])
            Dim reader = [module].Module.MetadataReader
            Dim fieldReferences = reader.MemberReferences.
                Where(Function(handle)
                          Dim name = reader.GetString(reader.GetMemberReference(handle).Name)
                          Return name = "F1" OrElse name = "F2" OrElse name = "F3"
                      End Function).
                Select(Function(handle) decoder.GetSymbolForILToken(handle)).
                ToArray()

            Dim containingType = fieldReferences(0).ContainingType
            Dim fieldMembers = containingType.GetMembers().WhereAsArray(Function(m) m.Kind = SymbolKind.Field)
            Dim expectedMembers =
                {
                "R(Of System.Object).F1 As System.Object",
                "R(Of System.Object).F1 As System.Object modreq(?)",
                "R(Of System.Object).F2 As System.Object modopt(System.Int32) modreq(?)",
                "R(Of System.Object).F2 As System.Object modopt(System.Object) modreq(?)",
                "R(Of System.Object).F3 As System.Int32 modreq(?)",
                "R(Of System.Object).F3 As System.SByte modreq(?)"
                }
            AssertEx.Equal(expectedMembers, fieldMembers.Select(Function(f) f.ToTestDisplayString()))

            Dim expectedReferences =
                {
                "R(Of System.Object).F1 As System.Object modreq(?)",
                "R(Of System.Object).F2 As System.Object modopt(System.Object) modreq(?)",
                "R(Of System.Object).F3 As System.SByte modreq(?)"
                }
            AssertEx.Equal(expectedReferences, fieldReferences.Select(Function(f) f.ToTestDisplayString()))
        End Sub

    End Class

End Namespace
