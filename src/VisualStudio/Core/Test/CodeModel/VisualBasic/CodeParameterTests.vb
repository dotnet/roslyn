' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_NoModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S1($$p1 As Integer)
   End Sub

End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_ByValModifier() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S2(ByVal $$p2 As Integer)
   End Sub

End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_ByRefModifier() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S3(ByRef $$p3 As Integer)
   End Sub

End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_OptionalByValModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S4(Optional ByVal $$p4 As Integer = 0)
   End Sub

End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_ByValParamArrayModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S5(ByVal ParamArray $$p5() As Integer)
   End Sub

End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetStartPoint_TypeCharacter() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S6($$p6%)
   End Sub

End Class
</Code>

            Await TestGetStartPoint(code,
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
        End Function

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_NoModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S1($$p1 As Integer)
   End Sub

End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_ByValModifier() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S2(ByVal $$p2 As Integer)
   End Sub

End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_ByRefModifier() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S3(ByRef $$p3 As Integer)
   End Sub

End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_OptionalByValModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S4(Optional ByVal $$p4 As Integer = 0)
   End Sub

End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_ByValParamArrayModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S5(ByVal ParamArray $$p5() As Integer)
   End Sub

End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestGetEndPoint_TypeCharacter() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S6($$p6%)
   End Sub

End Class
</Code>

            Await TestGetEndPoint(code,
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
        End Function

#End Region

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code><![CDATA[
Class C
    Sub Foo($$s As String)
    End Sub
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
    Sub Foo(<Out()> s As String)
    End Sub
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Out"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code><![CDATA[
Class C
    Sub Foo(<Out()> $$s As String)
    End Sub
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
    Sub Foo(<Foo()> <Out()> s As String)
    End Sub
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Foo"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute3() As Task
            Dim code =
<Code><![CDATA[
Class C
    Sub Foo(s As String, ' Comment after implicit line continuation
            $$i As Integer)
    End Sub
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
    Sub Foo(s As String, ' Comment after implicit line continuation
            <Out()> i As Integer)
    End Sub
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Out"})
        End Function

#End Region

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_NoModifiers() As Task
            Dim code =
<Code>
Class C
    Sub Foo($$s As String)
    End Sub
End Class
</Code>

            Await TestFullName(code, "s")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_Array() As Task
            Dim code =
<Code>
Class C
    Sub Foo($$s() As String)
    End Sub
End Class
</Code>

            Await TestFullName(code, "s()")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName_TypeCharacter() As Task
            Dim code =
<Code>
Class C
    Sub Foo($$s% As String)
    End Sub
End Class
</Code>

            Await TestFullName(code, "s%")
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_NoModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S1($$p1 As Integer)
   End Sub

End Class
</Code>

            Await TestName(code, "p1")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ByValModifier() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S2(ByVal $$p2 As Integer)
   End Sub

End Class
</Code>

            Await TestName(code, "p2")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ByRefModifier() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S3(ByRef $$p3 As Integer)
   End Sub

End Class
</Code>

            Await TestName(code, "p3")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_OptionalByValModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S4(Optional ByVal $$p4 As Integer = 0)
   End Sub

End Class
</Code>

            Await TestName(code, "p4")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ByValParamArrayModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S5(ByVal ParamArray $$p5() As Integer)
   End Sub

End Class
</Code>

            Await TestName(code, "p5")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_TypeCharacter() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S6($$p6%)
   End Sub

End Class
</Code>

            Await TestName(code, "p6")
        End Function

#End Region

#Region "Kind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestKind1() As Task
            Dim code =
<Code>
Class C
    Sub Foo($$s As String)
    End Sub
End Class
</Code>

            Await TestKind(code, EnvDTE.vsCMElement.vsCMElementParameter)
        End Function

#End Region

#Region "ParameterKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_In() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_Ref() As Task
            Dim code =
<Code>
Class C
    Sub M(ByRef $$s As String)
    End Sub
End Class
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Function


        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_ParamArray() As Task
            Dim code =
<Code>
Class C
    Sub M(ParamArray $$s() As String)
    End Sub
End Class
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_Optional() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Foo")
    End Sub
End Class
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterKind_OptionalAndRef() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional ByRef $$s As String = "Foo")
    End Sub
End Class
</Code>

            Await TestParameterKind(code, EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindRef)
        End Function

#End Region

#Region "Parent tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParent1() As Task
            Dim code =
<Code>
Class C
    Sub M($$s As String)
    End Sub
End Class
</Code>

            Await TestParent(code, IsElement("M", kind:=EnvDTE.vsCMElement.vsCMElementFunction))
        End Function

#End Region

#Region "Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType1() As Task
            Dim code =
<Code>
Class C
    Public Sub Foo(Optional i$$ As Integer = 0) { }
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "Integer",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestType2() As Task
            Dim code =
<Code>
Class C
    Public Sub Foo(Optional $$s$ = 0) { }
End Class
</Code>

            Await TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "String",
                             .AsFullName = "System.String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Function

#End Region

#Region "DefaultValue tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDefaultValue1() As Task
            Dim code =
<Code>
Class C
    Sub M(Optional $$s As String = "Foo")
    End Sub
End Class
</Code>

            Await TestDefaultValue(code, """Foo""")
        End Function

#End Region

#Region "Set ParameterKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
    Sub M(Optional s As String = "Foo")
    End Sub
End Class
</Code>
            Await TestSetDefaultValue(code, expected, """Foo""")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
    Sub M(Optional s As String = "Foo")
    End Sub
End Class
</Code>
            Await TestSetDefaultValue(code, expected, """Foo""")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
            Await TestSetDefaultValue(code, expected, """Foo""", ThrowsArgumentException(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
Class C
    Public Sub Foo(Optional i$$ As Integer = 0)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Foo(Optional i As System.Nullable(Of Byte)(,) = 0)
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, "Byte?(,)")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
Class C
    Public Sub Foo(Optional $$s$ = "Foo")
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Foo(Optional s$ As Integer = "Foo")
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
Class C
    Public Sub Foo(i$$ As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Foo(i As String,
                   j As Integer)
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
            Dim code =
<Code>
Class C
    Public Sub Foo(i$$ As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Sub Foo(i As Integer,
                   j As Integer)
    End Sub
End Class
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

    End Class
End Namespace

