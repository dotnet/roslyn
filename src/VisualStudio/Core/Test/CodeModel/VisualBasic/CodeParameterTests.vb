' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_NoModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S1($$p1 As Integer)
   End Sub

End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=31)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_ByValModifier()
            Dim code =
<Code>
Public Class C1

   Public Sub S2(ByVal $$p2 As Integer)
   End Sub

End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=24, absoluteOffset:=41, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_ByRefModifier()
            Dim code =
<Code>
Public Class C1

   Public Sub S3(ByRef $$p3 As Integer)
   End Sub

End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=24, absoluteOffset:=41, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=37)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_OptionalByValModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S4(Optional ByVal $$p4 As Integer = 0)
   End Sub

End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=50, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_ByValParamArrayModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S5(ByVal ParamArray $$p5() As Integer)
   End Sub

End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=35, absoluteOffset:=52, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=50)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_TypeCharacter()
            Dim code =
<Code>
Public Class C1

   Public Sub S6($$p6%)
   End Sub

End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=35, lineLength:=21)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_NoModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S1($$p1 As Integer)
   End Sub

End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=31, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=31, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=31, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=31, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=37, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=31, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=31, absoluteOffset:=48, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=31, absoluteOffset:=48, lineLength:=31)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_ByValModifier()
            Dim code =
<Code>
Public Class C1

   Public Sub S2(ByVal $$p2 As Integer)
   End Sub

End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=43, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_ByRefModifier()
            Dim code =
<Code>
Public Class C1

   Public Sub S3(ByRef $$p3 As Integer)
   End Sub

End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=26, absoluteOffset:=43, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=37, absoluteOffset:=54, lineLength:=37)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_OptionalByValModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S4(Optional ByVal $$p4 As Integer = 0)
   End Sub

End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=35, absoluteOffset:=52, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_ByValParamArrayModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S5(ByVal ParamArray $$p5() As Integer)
   End Sub

End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=39, absoluteOffset:=56, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=50, absoluteOffset:=67, lineLength:=50)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_TypeCharacter()
            Dim code =
<Code>
Public Class C1

   Public Sub S6($$p6%)
   End Sub

End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=38, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=38, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=38, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=38, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=38, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=38, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=38, lineLength:=21)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=38, lineLength:=21)))
        End Sub

#End Region

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code><![CDATA[
Class C
    Sub Goo($$s As String)
    End Sub
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
    Sub Goo(<Out()> s As String)
    End Sub
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Out"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code><![CDATA[
Class C
    Sub Goo(<Out()> $$s As String)
    End Sub
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
    Sub Goo(<Goo()> <Out()> s As String)
    End Sub
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Goo"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute3() As Task
            Dim code =
<Code><![CDATA[
Class C
    Sub Goo(s As String, ' Comment after implicit line continuation
            $$i As Integer)
    End Sub
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
    Sub Goo(s As String, ' Comment after implicit line continuation
            <Out()> i As Integer)
    End Sub
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Out"})
        End Function

#End Region

#Region "FullName tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_NoModifiers()
            Dim code =
<Code>
Class C
    Sub Goo($$s As String)
    End Sub
End Class
</Code>

            TestFullName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_Array()
            Dim code =
<Code>
Class C
    Sub Goo($$s() As String)
    End Sub
End Class
</Code>

            TestFullName(code, "s()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName_TypeCharacter()
            Dim code =
<Code>
Class C
    Sub Goo($$s% As String)
    End Sub
End Class
</Code>

            TestFullName(code, "s%")
        End Sub

#End Region

#Region "Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_NoModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S1($$p1 As Integer)
   End Sub

End Class
</Code>

            TestName(code, "p1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ByValModifier()
            Dim code =
<Code>
Public Class C1

   Public Sub S2(ByVal $$p2 As Integer)
   End Sub

End Class
</Code>

            TestName(code, "p2")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ByRefModifier()
            Dim code =
<Code>
Public Class C1

   Public Sub S3(ByRef $$p3 As Integer)
   End Sub

End Class
</Code>

            TestName(code, "p3")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_OptionalByValModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S4(Optional ByVal $$p4 As Integer = 0)
   End Sub

End Class
</Code>

            TestName(code, "p4")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ByValParamArrayModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S5(ByVal ParamArray $$p5() As Integer)
   End Sub

End Class
</Code>

            TestName(code, "p5")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_TypeCharacter()
            Dim code =
<Code>
Public Class C1

   Public Sub S6($$p6%)
   End Sub

End Class
</Code>

            TestName(code, "p6")
        End Sub

#End Region

#Region "Kind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestKind1()
            Dim code =
<Code>
Class C
    Sub Goo($$s As String)
    End Sub
End Class
</Code>

            TestKind(code, EnvDTE.vsCMElement.vsCMElementParameter)
        End Sub

#End Region

#Region "ParameterKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_In()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_Ref()
            Dim code =
<Code>
Class C
    Sub M(ByRef $$s As String)
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_ParamArray()
            Dim code =
<Code>
Class C
    Sub M(ParamArray $$s() As String)
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_Optional()
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Goo")
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterKind_OptionalAndRef()
            Dim code =
<Code>
Class C
    Sub M(Optional ByRef $$s As String = "Goo")
    End Sub
End Class
</Code>

            TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Sub

#End Region

#Region "Parent tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParent1()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            TestParent(code, IsElement("M", kind:=EnvDTE.vsCMElement.vsCMElementFunction))
        End Sub

#End Region

#Region "Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
            Dim code =
<Code>
Class C
    Public Sub Goo(Optional i$$ As Integer = 0) { }
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "Integer",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType2()
            Dim code =
<Code>
Class C
    Public Sub Goo(Optional $$s$ = 0) { }
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "String",
                             .AsFullName = "System.String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Sub

#End Region

#Region "DefaultValue tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDefaultValue1()
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Goo")
    End Sub
End Class
</Code>

            TestDefaultValue(code, """Goo""")
        End Sub

#End Region

#Region "Set ParameterKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_In() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_None() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindNone)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Out() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindOut, ThrowsArgumentException(Of EnvDTE80.vsCMParameterKind)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Ref() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByRef s As String)
    End Sub
End Class
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Params() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ParamArray s As String)
    End Sub
End Class
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Optional() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional s As String)
    End Sub
End Class
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_Same() As Task
            Dim code =
<Code>
Class C
    Sub M(ByRef $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByRef s As String)
    End Sub
End Class
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetParameterKind_OptionalByref() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional ByRef s As String)
    End Sub
End Class
</Code>
            Await TestSetParameterKind(code, expected, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef Or EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional)
        End Function

#End Region

#Region "Set DefaultValue tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDefaultValue1() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional s As String = "Goo")
    End Sub
End Class
</Code>
            Await TestSetDefaultValue(code, expected, """Goo""")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDefaultValue_ReplaceExisting() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Bar")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional s As String = "Goo")
    End Sub
End Class
</Code>
            Await TestSetDefaultValue(code, expected, """Goo""")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDefaultValue_None() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Bar")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            Await TestSetDefaultValue(code, expected, "")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDefaultValue_OptionalMissing() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>
            Await TestSetDefaultValue(code, expected, """Goo""", ThrowsArgumentException(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
Class C
    Public Sub Goo(Optional i$$ As Integer = 0)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Goo(Optional i As System.Nullable(Of Byte)(,) = 0)
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, "Byte?(,)")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
Class C
    Public Sub Goo(Optional $$s$ = "Goo")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Goo(Optional s$ As Integer = "Goo")
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
Class C
    Public Sub Goo(i$$ As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Goo(i As String,
                   j As Integer)
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
            Dim code =
<Code>
Class C
    Public Sub Goo(i$$ As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Goo(i As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

#End Region

#Region "IParameterKind.GetParameterPassingMode tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_NoModifier()
            Dim code =
<Code>
Class C
    Sub Goo($$s As String)
    End Sub
End Class
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_ByRefModifier()
            Dim code =
<Code>
Class C
    Sub Goo(ByRef $$s As String)
    End Sub
End Class
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_ParamArrayModifier()
            Dim code =
<Code>
Class C
    Sub Goo(ParamArray $$s As String())
    End Sub
End Class
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_OptionalModifier()
            Dim code =
<Code>
Class C
    Sub Goo(Optional $$s As String = "Goo")
    End Sub
End Class
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterPassingMode_OptionalAndByRefModifiers()
            Dim code =
<Code>
Class C
    Sub Goo(Optional ByRef $$s As String = "Goo")
    End Sub
End Class
</Code>

            TestGetParameterPassingMode(code, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Sub

#End Region

#Region "IParmeterKind.SetParameterPassingMode tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_NoModifier_In() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_NoModifier_InOut() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByRef s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_NoModifier_Out() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeOut, ThrowsArgumentException(Of PARAMETER_PASSING_MODE)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ByValModifier_In() As Task
            Dim code =
<Code>
Class C
    Sub M(ByVal $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByVal s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ByValModifier_InOut() As Task
            Dim code =
<Code>
Class C
    Sub M(ByVal $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByRef s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ByValModifier_Out() As Task
            Dim code =
<Code>
Class C
    Sub M(ByVal $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByVal s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeOut, ThrowsArgumentException(Of PARAMETER_PASSING_MODE)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ByRefModifier_In() As Task
            Dim code =
<Code>
Class C
    Sub M(ByRef $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ByRefModifier_InOut() As Task
            Dim code =
<Code>
Class C
    Sub M(ByRef $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByRef s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ByRefModifier_Out() As Task
            Dim code =
<Code>
Class C
    Sub M(ByRef $$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ByRef s As String)
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeOut, ThrowsArgumentException(Of PARAMETER_PASSING_MODE)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ParamArrayModifier_In() As Task
            Dim code =
<Code>
Class C
    Sub M(ParamArray $$s As String())
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ParamArray s As String())
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ParamArrayModifier_InOut() As Task
            Dim code =
<Code>
Class C
    Sub M(ParamArray $$s As String())
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ParamArray s As String())
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut, ThrowsArgumentException(Of PARAMETER_PASSING_MODE)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_ParamArrayModifier_Out() As Task
            Dim code =
<Code>
Class C
    Sub M(ParamArray $$s As String())
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(ParamArray s As String())
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeOut, ThrowsArgumentException(Of PARAMETER_PASSING_MODE)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_OptionalModifier_In() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "hello")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional s As String = "hello")
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeIn)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_OptionalModifier_InOut() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "hello")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional ByRef s As String = "hello")
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeInOut)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterPassingMode_OptionalModifier_Out() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "hello")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(Optional s As String = "hello")
    End Sub
End Class
</Code>

            Await TestSetParameterPassingMode(code, expected, PARAMETER_PASSING_MODE.cmParameterTypeOut, ThrowsArgumentException(Of PARAMETER_PASSING_MODE)())
        End Function

#End Region

#Region "IParameterKind.GetParameterArrayCount tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayCount_0()
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            TestGetParameterArrayCount(code, 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayCount_1()
            Dim code =
<Code>
Class C
    Sub M($$s As String())
    End Sub
End Class
</Code>

            TestGetParameterArrayCount(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayCount_2()
            Dim code =
<Code>
Class C
    Sub M($$s As String()())
    End Sub
End Class
</Code>

            TestGetParameterArrayCount(code, 2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayCount_1_Multi()
            Dim code =
<Code>
Class C
    Sub M($$s As String(,,))
    End Sub
End Class
</Code>

            TestGetParameterArrayCount(code, 1)
        End Sub

#End Region

#Region "IParameterKind.GetParameterArrayDimensions tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_0_1()
            Dim code =
<Code>
Class C
    Sub M($$s As String())
    End Sub
End Class
</Code>

            TestGetParameterArrayDimensions(code, index:=0, expected:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_0_2()
            Dim code =
<Code>
Class C
    Sub M($$s As String(,))
    End Sub
End Class
</Code>

            TestGetParameterArrayDimensions(code, index:=0, expected:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_0_3()
            Dim code =
<Code>
Class C
    Sub M($$s As String(,,))
    End Sub
End Class
</Code>

            TestGetParameterArrayDimensions(code, index:=0, expected:=3)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_1_1()
            Dim code =
<Code>
Class C
    Sub M($$s As String(,,)())
    End Sub
End Class
</Code>

            TestGetParameterArrayDimensions(code, index:=1, expected:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_1_2()
            Dim code =
<Code>
Class C
    Sub M($$s As String(,,)(,))
    End Sub
End Class
</Code>

            TestGetParameterArrayDimensions(code, index:=1, expected:=2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Test_IParameterKind_GetParameterArrayDimensions_2_1()
            Dim code =
<Code>
Class C
    Sub M($$s As String(,,)(,)())
    End Sub
End Class
</Code>

            TestGetParameterArrayDimensions(code, index:=2, expected:=1)
        End Sub

#End Region

#Region "IParmeterKind.SetParameterArrayDimensions tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_None_0() As Task
            ' The C# implementation had a weird behavior where it wold allow setting array dimensions
            ' to 0 to create an array with a single rank.

            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String())
    End Sub
End Class
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_None_1() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String())
    End Sub
End Class
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_None_2() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String(,))
    End Sub
End Class
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_1_2() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String())
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String(,))
    End Sub
End Class
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function Test_IParameterKind_SetParameterArrayDimensions_1_2_WithInnerArray() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String()())
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Sub M(s As String(,)())
    End Sub
End Class
</Code>

            Await TestSetParameterArrayDimensions(code, expected, dimensions:=2)
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

    End Class
End Namespace

