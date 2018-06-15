' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic

    Public Class CodeNamespaceTests
        Inherits AbstractCodeNamespaceTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
            Dim code =
<Code>
Namespace $$N : End Namespace
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=13, absoluteOffset:=13, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=13, absoluteOffset:=13, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint2()
            Dim code =
<Code>
Namespace $$N :
End Namespace
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=13, absoluteOffset:=13, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=13, absoluteOffset:=13, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=13)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint3()
            Dim code =
<Code>
Namespace $$N ' N
End Namespace
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=17, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=17, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=17, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=15)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=15)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint4()
            Dim code =
<Code>
Namespace $$N
End Namespace
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint5()
            Dim code =
<Code>
Namespace $$N

End Namespace
</Code>

            ' Note: TextPoint.AbsoluteCharOffset throws in VS 2012 for vsCMPartNavigate

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint6()
            Dim code =
<Code>
Namespace $$N
    Class C
    End Class
End Namespace
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=17, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint7()
            Dim code =
<Code>
Namespace $$N

    Class C
    End Class

End Namespace
</Code>

            ' Note: TextPoint.AbsoluteCharOffset throws in VS 2012 for vsCMPartNavigate

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=11, absoluteOffset:=11, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, lineLength:=0)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=11)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
            Dim code =
<Code>
Namespace $$N : End Namespace
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=15, absoluteOffset:=15, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint2()
            Dim code =
<Code>
Namespace $$N
End Namespace
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=13, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=14, absoluteOffset:=26, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=14, absoluteOffset:=26, lineLength:=13)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint3()
            Dim code =
<Code>
Namespace $$N

End Namespace
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=14, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=14, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=14, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=27, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=27, lineLength:=13)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint4()
            Dim code =
<Code>
Namespace $$N
    Class C
    End Class
End Namespace
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=39, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=39, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=39, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=4, lineOffset:=14, absoluteOffset:=52, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=4, lineOffset:=14, absoluteOffset:=52, lineLength:=13)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint5()
            Dim code =
<Code>
Namespace $$N

    Class C
    End Class

End Namespace
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=41, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=41, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=6, lineOffset:=1, absoluteOffset:=41, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=6, lineOffset:=14, absoluteOffset:=54, lineLength:=13)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=6, lineOffset:=14, absoluteOffset:=54, lineLength:=13)))
        End Sub

#End Region

#Region "Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment1()
            Dim code =
<Code>
' Goo
Namespace $$N
End Namespace
</Code>

            Dim result = " Goo"

            TestComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment2()
            Dim code =
<Code>
' Goo
' Bar
Namespace $$N
End Namespace
</Code>

            Dim result = " Goo" & vbCrLf &
                         " Bar"

            TestComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment3()
            Dim code =
<Code>
' Goo

' Bar
Namespace $$N
End Namespace
</Code>

            Dim result = " Bar"

            TestComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment4()
            Dim code =
<Code>
Namespace N1
End Namespace ' Goo

' Bar
Namespace $$N2
End Namespace
</Code>

            Dim result = " Bar"

            TestComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment5()
            Dim code =
<Code>
' Goo
''' &lt;summary&gt;Bar&lt;/summary&gt;
Namespace $$N
End Namespace
</Code>

            Dim result = ""

            TestComment(code, result)
        End Sub

#End Region

#Region "DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment1()
            Dim code =
<Code>
''' &lt;summary&gt;
''' Goo
''' &lt;/summary&gt;
''' &lt;remarks&gt;&lt;/remarks&gt;
Namespace $$N
End Namespace
</Code>

            Dim result =
" <summary>" & vbCrLf &
" Goo" & vbCrLf &
" </summary>" & vbCrLf &
" <remarks></remarks>"

            TestDocComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment2()
            Dim code =
<Code>
'''     &lt;summary&gt;
''' Hello World
''' &lt;/summary&gt;
Namespace $$N
End Namespace
</Code>

            Dim result =
"     <summary>" & vbCrLf &
" Hello World" & vbCrLf &
" </summary>"

            TestDocComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment3()
            Dim code =
<Code>
''' &lt;summary&gt;
''' Goo
''' &lt;/summary&gt;
' Bar
''' &lt;remarks&gt;&lt;/remarks&gt;
Namespace $$N
End Namespace
</Code>

            Dim result =
" <remarks></remarks>"

            TestDocComment(code, result)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment4()
            Dim code =
<Code>
Namespace N1
    ''' &lt;summary&gt;
    ''' Goo
    ''' &lt;/summary&gt;
    ''' &lt;remarks&gt;&lt;/remarks&gt;
    Namespace $$N2
    End Namespace
End Namespace
</Code>

            Dim result =
" <summary>" & vbCrLf &
" Goo" & vbCrLf &
" </summary>" & vbCrLf &
" <remarks></remarks>"

            TestDocComment(code, result)
        End Sub

#End Region

#Region "Set Comment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment1() As Task
            Dim code =
<Code>
' Goo

' Bar
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
' Goo

Namespace N
End Namespace
</Code>

            Await TestSetComment(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment2() As Task
            Dim code =
<Code>
' Goo
''' &lt;summary&gt;Bar&lt;/summary&gt;
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
' Goo
''' &lt;summary&gt;Bar&lt;/summary&gt;
' Bar
Namespace N
End Namespace
</Code>

            Await TestSetComment(code, expected, "Bar")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetComment3() As Task
            Dim code =
<Code>
' Goo

' Bar
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
' Goo

' Blah
Namespace N
End Namespace
</Code>

            Await TestSetComment(code, expected, "Blah")
        End Function

#End Region

#Region "Set DocComment tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_Nothing1() As Task
            Dim code =
<Code>
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
Namespace N
End Namespace
</Code>

            Await TestSetDocComment(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_Nothing2() As Task
            Dim code =
<Code>
''' &lt;summary&gt;
''' Goo
''' &lt;/summary&gt;
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
Namespace N
End Namespace
</Code>

            Await TestSetDocComment(code, expected, Nothing)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_InvalidXml1() As Task
            Dim code =
<Code>
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
''' &lt;doc&gt;&lt;summary&gt;Blah&lt;/doc&gt;
Namespace N
End Namespace
</Code>

            Await TestSetDocComment(code, expected, "<doc><summary>Blah</doc>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment_InvalidXml2() As Task
            Dim code =
<Code>
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
''' &lt;doc___&gt;&lt;summary&gt;Blah&lt;/summary&gt;&lt;/doc___&gt;
Namespace N
End Namespace
</Code>

            Await TestSetDocComment(code, expected, "<doc___><summary>Blah</summary></doc___>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment1() As Task
            Dim code =
<Code>
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
''' &lt;summary&gt;Hello World&lt;/summary&gt;
Namespace N
End Namespace
</Code>

            Await TestSetDocComment(code, expected, "<summary>Hello World</summary>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment2() As Task
            Dim code =
<Code>
''' &lt;summary&gt;Hello World&lt;/summary&gt;
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
''' &lt;summary&gt;Blah&lt;/summary&gt;
Namespace N
End Namespace
</Code>

            Await TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment3() As Task
            Dim code =
<Code>
' Goo
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
' Goo
''' &lt;summary&gt;Blah&lt;/summary&gt;
Namespace N
End Namespace
</Code>

            Await TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment4() As Task
            Dim code =
<Code>
''' &lt;summary&gt;FogBar&lt;/summary&gt;
' Goo
Namespace $$N
End Namespace
</Code>

            Dim expected =
<Code>
''' &lt;summary&gt;Blah&lt;/summary&gt;
' Goo
Namespace N
End Namespace
</Code>

            Await TestSetDocComment(code, expected, "<summary>Blah</summary>")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetDocComment5() As Task
            Dim code =
<Code>
Namespace N1
    Namespace $$N2
    End Namespace
End Namespace
</Code>

            Dim expected =
<Code>
Namespace N1
    ''' &lt;summary&gt;Hello World&lt;/summary&gt;
    Namespace N2
    End Namespace
End Namespace
</Code>

            Await TestSetDocComment(code, expected, "<summary>Hello World</summary>")
        End Function

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_SameName() As Task
            Dim code =
<Code>
Namespace N$$
    Class C
    End Class
End Namespace
</Code>

            Dim expected =
<Code>
Namespace N
    Class C
    End Class
End Namespace
</Code>

            Await TestSetName(code, expected, "N", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_NewName() As Task
            Dim code =
<Code>
Namespace N$$
    Class C
    End Class
End Namespace
</Code>

            Dim expected =
<Code>
Namespace N2
    Class C
    End Class
End Namespace
</Code>

            Await TestSetName(code, expected, "N2", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_SimpleNameToDottedName() As Task
            Dim code =
<Code>
Namespace N1$$
    Class C
    End Class
End Namespace
</Code>

            Dim expected =
<Code>
Namespace N2.N3
    Class C
    End Class
End Namespace
</Code>

            Await TestSetName(code, expected, "N2.N3", NoThrow(Of String)())
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_DottedNameToDottedName() As Task
            Dim code =
<Code>
Namespace N1.N2$$
    Class C
    End Class
End Namespace
</Code>

            Dim expected =
<Code>
Namespace N3.N4
    Class C
    End Class
End Namespace
</Code>

            Await TestSetName(code, expected, "N3.N4", NoThrow(Of String)())
        End Function

#End Region

#Region "Remove tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemove1() As Task
            Dim code =
<Code>
Namespace $$Goo
    Class C
    End Class
End Namespace
</Code>

            Dim expected =
<Code>
Namespace Goo
End Namespace
</Code>

            Await TestRemoveChild(code, expected, "C")
        End Function

#End Region

        <WorkItem(858153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858153")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestChildren1()
            Dim code =
<Code>
Namespace N$$
    Class C1
    End Class
    
    Class C2
    End Class

    Class C3
    End Class
End Namespace
</Code>

            TestChildren(code,
                         IsElement("C1", EnvDTE.vsCMElement.vsCMElementClass),
                         IsElement("C2", EnvDTE.vsCMElement.vsCMElementClass),
                         IsElement("C3", EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <WorkItem(150349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub NoChildrenForInvalidMembers()
            Dim code =
<Code>
Namespace N$$
    Sub M()
    End Sub
    Function M() As Integer
    End Function
    Property P As Integer
    Event E()
End Sub
</Code>

            TestChildren(code, NoElements)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

    End Class

End Namespace
