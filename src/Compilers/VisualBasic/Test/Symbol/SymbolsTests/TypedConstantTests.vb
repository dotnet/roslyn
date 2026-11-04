' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols

    Public Class TypedConstantTests
        Inherits BasicTestBase

        Private ReadOnly _compilation As VisualBasicCompilation

        Private ReadOnly _namedType As NamedTypeSymbol

        Private ReadOnly _systemType As NamedTypeSymbol

        Private ReadOnly _arrayType As ArrayTypeSymbol

        Public Sub New()
            _compilation = VisualBasicCompilation.Create("goo")
            _namedType = _compilation.GetSpecialType(SpecialType.System_Byte)
            _systemType = _compilation.GetWellKnownType(WellKnownType.System_Type)
            _arrayType = _compilation.CreateArrayTypeSymbol(_compilation.GetSpecialType(SpecialType.System_Object))
        End Sub

        <Fact()>
        Public Sub Conversions()
            Dim common As TypedConstant = New TypedConstant(_systemType, TypedConstantKind.Type, _namedType)
            Dim lang As TypedConstant = CType(common, TypedConstant)
            Dim common2 As TypedConstant = lang

            Assert.Equal(common.Value, lang.Value)
            Assert.Equal(common.Kind, lang.Kind)
            AssertEx.Equal(Of Object)(common.Type, lang.Type)

            Assert.Equal(common.Value, common2.Value)
            Assert.Equal(common.Kind, common2.Kind)
            Assert.Equal(common.Type, common2.Type)

            Dim commonArray As TypedConstant = New TypedConstant(_arrayType,
                                                                             {New TypedConstant(_systemType, TypedConstantKind.Type, _namedType)}.AsImmutableOrNull())
            Dim langArray As TypedConstant = CType(commonArray, TypedConstant)
            Dim commonArray2 As TypedConstant = langArray

            Assert.Equal(commonArray.Values.Single(), langArray.Values.Single())
            Assert.Equal(commonArray.Kind, langArray.Kind)
            AssertEx.Equal(Of Object)(commonArray.Type, langArray.Type)

            Assert.Equal(commonArray.Values, commonArray2.Values)
            Assert.Equal(commonArray.Kind, commonArray2.Kind)
            Assert.Equal(commonArray.Type, commonArray2.Type)

            Assert.Equal(common2, CType(lang, TypedConstant))
            Assert.IsType(Of Microsoft.CodeAnalysis.TypedConstant)(common2)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74326")>
        Public Sub ToVisualBasicString_FlagsEnum_ExcludesZeroMember()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
&lt;System.Flags&gt; Enum E
    None = 0
    A = 1
    B = 2
    C = 4
End Enum
&lt;System.Flags&gt; Enum U As UInteger
    None = 0
    A = 1
    B = 2
    C = 4
End Enum
    </file>
</compilation>)
            Dim signedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("E")
            Dim unsignedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("U")

            Assert.Equal("E.A Or E.C", New TypedConstant(signedType, TypedConstantKind.Enum, 5).ToVisualBasicString())
            Assert.Equal("U.A Or U.C", New TypedConstant(unsignedType, TypedConstantKind.Enum, 5UI).ToVisualBasicString())
            Assert.Equal("E.None", New TypedConstant(signedType, TypedConstantKind.Enum, 0).ToVisualBasicString())
        End Sub

        <Fact>
        Public Sub ToVisualBasicString_IncludeTypeCharacter()
            Assert.Equal("42UI", New TypedConstant(_compilation.GetSpecialType(SpecialType.System_UInt32), TypedConstantKind.Primitive, 42UI).ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
            Assert.Equal("42L", New TypedConstant(_compilation.GetSpecialType(SpecialType.System_Int64), TypedConstantKind.Primitive, 42L).ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
            Assert.Equal("42UL", New TypedConstant(_compilation.GetSpecialType(SpecialType.System_UInt64), TypedConstantKind.Primitive, 42UL).ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
            Assert.Equal("26.2R", New TypedConstant(_compilation.GetSpecialType(SpecialType.System_Double), TypedConstantKind.Primitive, 26.2).ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
            Assert.Equal("3.14F", New TypedConstant(_compilation.GetSpecialType(SpecialType.System_Single), TypedConstantKind.Primitive, 3.14F).ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
            Assert.Equal("12.5D", New TypedConstant(_compilation.GetSpecialType(SpecialType.System_Decimal), TypedConstantKind.Primitive, 12.5D).ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
        End Sub

        <Fact>
        Public Sub ToVisualBasicString_IncludeTypeCharacter_FormatsArrayElements()
            Dim floatType = _compilation.GetSpecialType(SpecialType.System_Single)
            Dim arrayType = _compilation.CreateArrayTypeSymbol(floatType)
            Dim values = New TypedConstant(arrayType,
                {
                    New TypedConstant(floatType, TypedConstantKind.Primitive, 0.5F),
                    New TypedConstant(floatType, TypedConstantKind.Primitive, 1.5F)
                }.AsImmutableOrNull())

            Assert.Equal("{0.5, 1.5}", values.ToVisualBasicString())
            Assert.Equal("{0.5F, 1.5F}", values.ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
        End Sub

        <Fact>
        Public Sub ToVisualBasicString_IncludeTypeCharacter_LeavesOtherValuesUnchanged()
            Assert.Equal("""text""", New TypedConstant(_compilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, "text").ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
            Assert.Equal("42I", New TypedConstant(_compilation.GetSpecialType(SpecialType.System_Int32), TypedConstantKind.Primitive, 42).ToVisualBasicString(TypedConstantFormattingOptions.IncludeTypeCharacter))
        End Sub
    End Class
End Namespace
