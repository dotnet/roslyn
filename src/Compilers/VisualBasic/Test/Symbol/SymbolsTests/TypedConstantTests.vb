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
    End Class
End Namespace
