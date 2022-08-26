' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests

Namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE

    Public Class LoadingMetadataTokens
        Inherits BasicTestBase

        <Fact>
        Public Sub LoadPESymbols()
            Dim compilation = CreateCompilation("
Public Class C

    Public f As Integer = 0

    Public Property P As Integer

    Public Sub M(ByVal p As Integer)
    End Sub

    Public Sub GM(Of T)()
    End Sub

    Public Event E As System.Action

End Class

Public Structure S

End Structure
")
            CompileAndVerify(compilation, symbolValidator:=Sub([module] As ModuleSymbol)

                                                               Dim peModule = CType([module], PEModuleSymbol)

                                                               Dim assembly = peModule.ContainingAssembly
                                                               Assert.Equal(536870913, assembly.MetadataToken)

                                                               Dim class1 = [module].GlobalNamespace.GetTypeMember("C")
                                                               Assert.Equal(33554434, class1.MetadataToken)

                                                               Dim field = class1.GetMember("f")
                                                               Assert.Equal(67108865, field.MetadataToken)

                                                               Dim [property] = class1.GetMember("P")
                                                               Assert.Equal(385875969, [property].MetadataToken)

                                                               Dim method = class1.GetMember("M")
                                                               Assert.Equal(100663300, method.MetadataToken)

                                                               Dim parameter = method.GetParameters().Single()
                                                               Assert.Equal(134217730, parameter.MetadataToken)

                                                               Dim genericMethod = class1.GetMember(Of MethodSymbol)("GM")
                                                               Assert.Equal(100663301, genericMethod.MetadataToken)

                                                               Dim typeParameter = genericMethod.TypeParameters.Single()
                                                               Assert.Equal(704643073, typeParameter.MetadataToken)

                                                               Dim event1 = class1.GetMember("E")
                                                               Assert.Equal(335544321, event1.MetadataToken)

                                                               Dim struct1 = [module].GlobalNamespace.GetTypeMember("S")
                                                               Assert.Equal(33554435, struct1.MetadataToken)

                                                           End Sub)
        End Sub

        <Fact>
        Public Sub LoadSourceSymbols()
            Dim compilation = CreateCompilation("
Public Class C

    Public f As Integer = 0

    Public Property P As Integer

    Public Sub M(ByVal p As Integer)
    End Sub

    Public Sub GM(Of T)()
    End Sub

    Public Event E As System.Action

End Class

Public Structure S

End Structure
")

            Dim assembly = compilation.Assembly
            Assert.Equal(0, assembly.MetadataToken)

            Dim class1 = compilation.GlobalNamespace.GetTypeMember("C")
            Assert.Equal(0, class1.MetadataToken)

            Dim field = class1.GetMember("f")
            Assert.Equal(0, field.MetadataToken)

            Dim [property] = class1.GetMember("P")
            Assert.Equal(0, [property].MetadataToken)

            Dim method = class1.GetMember("M")
            Assert.Equal(0, method.MetadataToken)

            Dim parameter = method.GetParameters().Single()
            Assert.Equal(0, parameter.MetadataToken)

            Dim genericMethod = class1.GetMember(Of MethodSymbol)("GM")
            Assert.Equal(0, genericMethod.MetadataToken)

            Dim typeParameter = genericMethod.TypeParameters.Single()
            Assert.Equal(0, typeParameter.MetadataToken)

            Dim event1 = class1.GetMember("E")
            Assert.Equal(0, event1.MetadataToken)

            Dim struct1 = compilation.GlobalNamespace.GetTypeMember("S")
            Assert.Equal(0, struct1.MetadataToken)
        End Sub
    End Class

End Namespace
