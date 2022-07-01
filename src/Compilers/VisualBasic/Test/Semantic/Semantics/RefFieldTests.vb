' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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
        <WorkItem(62121, "https://github.com/dotnet/roslyn/issues/62121")>
        <Fact>
        Public Sub RefField_01()
            Dim sourceA =
"public ref struct S<T>
{
    public ref T F;
    public S(ref T t) { F = ref t; }
}"
            Dim compA = CreateCSharpCompilation(GetUniqueName(), sourceA, parseOptions:=New CSharp.CSharpParseOptions(languageVersion:=CSharp.LanguageVersion.Preview))
            compA.VerifyDiagnostics()
            Dim refA = compA.EmitToImageReference()

            Dim sourceB1 =
"Imports System
Module Program
    Sub Main()
        Dim s = New S(Of Integer)()
        Console.WriteLine(s)
    End Sub
End Module"

            Dim compB = CreateCompilation(sourceB1, references:={refA})
            compB.AssertTheseDiagnostics(<expected>
BC30668: 'S(Of Integer)' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'.
        Dim s = New S(Of Integer)()
                    ~~~~~~~~~~~~~
</expected>)

            Dim field = compB.GetMember(Of FieldSymbol)("S.F")
            VerifyFieldSymbol(field, "S(Of T).F As T", Microsoft.CodeAnalysis.RefKind.None, {})
            Assert.Equal(ERRID.ERR_UnsupportedField1, field.GetUseSiteErrorInfo().Code)

            Dim sourceB2 =
"Imports System
Module Program
    Sub Main()
        Dim s = New S(Of Integer)()
        Console.WriteLine(s.F)
    End Sub
End Module"

            compB = CreateCompilation(sourceB2, references:={refA})
            compB.AssertTheseDiagnostics(<expected>
BC30668: 'S(Of Integer)' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'.
        Dim s = New S(Of Integer)()
                    ~~~~~~~~~~~~~
BC30656: Field 'Public F As T' is of an unsupported type.
        Console.WriteLine(s.F)
                          ~~~
</expected>)

            field = compB.GetMember(Of FieldSymbol)("S.F")
            VerifyFieldSymbol(field, "S(Of T).F As T", Microsoft.CodeAnalysis.RefKind.None, {})
            Assert.Equal(ERRID.ERR_UnsupportedField1, field.GetUseSiteErrorInfo().Code)
        End Sub

        ''' <summary>
        ''' Ref field in class.
        ''' </summary>
        <WorkItem(62121, "https://github.com/dotnet/roslyn/issues/62121")>
        <Fact>
        Public Sub RefField_02()
            Dim sourceA =
".class public A<T>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .field public !0& modopt(object) modopt(int8) F
}"
            Dim refA = CompileIL(sourceA)

            Dim sourceB1 =
"Imports System
Module Program
    Sub Main()
        Dim a = New A(Of Integer)()
        Console.WriteLine(a)
    End Sub
End Module"

            Dim comp = CreateCompilation(sourceB1, references:={refA})
            comp.AssertTheseDiagnostics(<expected></expected>)

            Dim field = comp.GetMember(Of FieldSymbol)("A.F")
            VerifyFieldSymbol(field, "A(Of T).F As T", Microsoft.CodeAnalysis.RefKind.None, {})
            Assert.Equal(ERRID.ERR_UnsupportedField1, field.GetUseSiteErrorInfo().Code)

            Dim sourceB2 =
"Imports System
Module Program
    Sub Main()
        Dim a = New A(Of Integer)()
        Console.WriteLine(a.F)
    End Sub
End Module"

            comp = CreateCompilation(sourceB2, references:={refA})
            comp.AssertTheseDiagnostics(<expected>
BC30656: Field 'Public F As T' is of an unsupported type.
        Console.WriteLine(a.F)
                          ~~~
                                        </expected>)

            field = comp.GetMember(Of FieldSymbol)("A.F")
            VerifyFieldSymbol(field, "A(Of T).F As T", Microsoft.CodeAnalysis.RefKind.None, {})
            Assert.Equal(ERRID.ERR_UnsupportedField1, field.GetUseSiteErrorInfo().Code)
        End Sub

        Private Shared Sub VerifyFieldSymbol(field As FieldSymbol, expectedDisplayString As String, expectedRefKind As RefKind, expectedRefCustomModifiers As String())
            Assert.Equal(expectedRefKind, field.RefKind)
            Assert.Equal(expectedRefCustomModifiers, field.RefCustomModifiers.SelectAsArray(Function(m) m.Modifier.ToTestDisplayString()))
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
            Dim compB = CreateCSharpCompilation(GetUniqueName(), sourceB, parseOptions:=New CSharp.CSharpParseOptions(languageVersion:=CSharp.LanguageVersion.Preview), referencedAssemblies:={MscorlibRef, refA})
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
                "R(Of System.Object).F1 As System.Object",
                "R(Of System.Object).F2 As System.Object",
                "R(Of System.Object).F2 As System.Object",
                "R(Of System.Object).F3 As System.Int32",
                "R(Of System.Object).F3 As System.SByte"
                }
            AssertEx.Equal(expectedMembers, fieldMembers.Select(Function(f) f.ToTestDisplayString()))

            ' PEFieldSymbol.RefKind is RefKind.None and RefCustomModifiers is empty in all cases,
            ' including ref fields, so we cannot differentiate fields that differ by RefKind or
            ' RefCustomModifiers in MemberRefMetadataDecoder.FindFieldBySignature().
            Dim expectedReferences =
                {
                "R(Of System.Object).F1 As System.Object",
                "R(Of System.Object).F2 As System.Object",
                "R(Of System.Object).F3 As System.SByte"
                }
            AssertEx.Equal(expectedReferences, fieldReferences.Select(Function(f) f.ToTestDisplayString()))
        End Sub

    End Class

End Namespace
